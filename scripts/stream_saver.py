"""Save one block of all LSL streams with LabRecorder, stopping at the end-of-block marker."""

import re
import time
from pathlib import Path
from pylsl import StreamInlet, resolve_byprop
from bci import config
from bci.lsl_saver import LslSaver

RESOLVE_TIMEOUT_S = 5.0
SAVE_TAIL_S = 1.0


def next_run_number(session_dir: Path, file_prefix: str) -> int:
    """Return the first run number not yet used in the session folder."""
    run_numbers = [0]
    for path in session_dir.glob(f"{file_prefix}_run-*.xdf"):
        match = re.search(r"_run-(\d+)", path.name)
        if match:
            run_numbers.append(int(match.group(1)))
    return max(run_numbers) + 1


# LabRecorder selects only the streams that exist when saving starts
marker_streams = resolve_byprop("name", config.MARKER_STREAM_NAME, timeout=RESOLVE_TIMEOUT_S)
eeg_streams = resolve_byprop("name", config.EEG_STREAM_NAME, timeout=RESOLVE_TIMEOUT_S)
if not marker_streams or not eeg_streams:
    raise RuntimeError("Start the marker and EEG streams before saving")
marker_inlet = StreamInlet(marker_streams[0])

# LabRecorder writes file names in lowercase
participant = config.PARTICIPANT.lower()
session = config.SESSION.lower()
file_prefix = f"sub-{participant}_ses-{session}_task-{config.BIDS_TASK}"
relative_dir = Path(f"sub-{participant}") / f"ses-{session}" / "eeg"
run_number = next_run_number(config.SOURCEDATA_DIR / relative_dir, file_prefix)
relative_path = relative_dir / f"{file_prefix}_run-{run_number:03d}_eeg.xdf"

saver = LslSaver()
try:
    output_path = saver.start_saving(config.SOURCEDATA_DIR, relative_path)
    print(f"Saving to {output_path}. Press Enter in the marker terminal to start the block.")
    # the timeout keeps Ctrl+C responsive while waiting
    while True:
        marker_sample, _ = marker_inlet.pull_sample(timeout=1.0)
        if marker_sample is not None and marker_sample[0] == config.BLOCK_END:
            break
    # some signal after the last marker
    time.sleep(SAVE_TAIL_S)
finally:
    # stop saving even after an error or Ctrl+C
    saver.stop_saving()
    saver.close()
print(f"Block saved. Check it with: python scripts/check_block.py {output_path}")