"""Run one block of trials: send the task markers that drive the Unity game, optionally saving the block."""

import argparse
import random
import re
import time
from pathlib import Path

from pylsl import IRREGULAR_RATE, StreamInfo, StreamOutlet

from bci import config
from bci.lsl_saver import LslSaver

BIDS_TASK = "calibration"
PRE_TASK_PAUSE_S = 2.0


def send_marker(code: int, duration_s: float) -> None:
    """Send one marker, then wait for the duration of its phase."""
    outlet.push_sample([code])
    time.sleep(duration_s)


def next_run_number(session_dir: Path, file_prefix: str) -> int:
    """Return the first run number not yet used in the session folder."""
    run_numbers = [0]
    for path in session_dir.glob(f"{file_prefix}_run-*.xdf"):
        match = re.search(r"_run-(\d+)", path.name)
        if match:
            run_numbers.append(int(match.group(1)))
    return max(run_numbers) + 1


parser = argparse.ArgumentParser(description="Run one block of trials; give participant and session to save it")
parser.add_argument("--participant", help="participant label, e.g. P001")
parser.add_argument("--session", help="session label, e.g. S001")
arguments = parser.parse_args()
if (arguments.participant is None) != (arguments.session is None):
    parser.error("give both --participant and --session to save, or neither")
saving = arguments.participant is not None

# description expected by the Unity game
stream_info = StreamInfo(name=config.MARKER_STREAM_NAME, type="Markers", channel_count=1,
                         nominal_srate=IRREGULAR_RATE, channel_format="int32",
                         source_id="GrazMI")
outlet = StreamOutlet(stream_info)

if saving:
    saver = LslSaver()

    # LabRecorder writes file names in lowercase
    participant = arguments.participant.lower()
    session = arguments.session.lower()
    # Create the path for the XDF file to be saved by LabRecorder (BIDS compliant)
    file_prefix = f"sub-{participant}_ses-{session}_task-{BIDS_TASK}"
    relative_dir = Path(f"sub-{participant}") / f"ses-{session}" / "eeg"
    run_number = next_run_number(config.SOURCEDATA_DIR / relative_dir, file_prefix)
    relative_path = relative_dir / f"{file_prefix}_run-{run_number:03d}_eeg.xdf"
    print(f"Saving on: run {run_number} in {config.SOURCEDATA_DIR}")
else:
    print("Saving off: give --participant and --session to save")

cue_codes = {class_name: code for code, class_name in config.CUE_CLASS_NAMES.items()}

# Create the randomized trial list
trials = []
# list [left, left, ..., right, right, ..., rest, rest, ...] (depending on the classes in config.CLASSES)
for class_name in config.CLASSES:
    trials += [class_name] * config.TRIALS_PER_CLASS
# shuffle the trials so that they are in random order
random.shuffle(trials)

# Wait for the user to start the block
input(f"{len(trials)} trials ready. Press Enter to start the block...")
try:
    if saving:
        output_path = saver.start_saving(config.SOURCEDATA_DIR, relative_path)
        print(f"Saving to {output_path}")
        # a few seconds of signal before the first marker
        time.sleep(PRE_TASK_PAUSE_S)
    send_marker(config.BASELINE, config.BASELINE_S)
    for trial_number, class_name in enumerate(trials, start=1):
        print(f"trial {trial_number}/{len(trials)}: {class_name}")
        send_marker(config.FIXATION, config.FIXATION_S)
        send_marker(cue_codes[class_name], config.CUE_S)
        send_marker(config.TASK, config.TASK_S)
        send_marker(config.PAUSE, config.PAUSE_S)
    # wait before stopping so that receivers get the last marker
    send_marker(config.BLOCK_END, 1.0)
finally:
    if saving:
        # stop saving even after an error or Ctrl+C
        saver.stop_saving()
        saver.close()
print("Block finished.")