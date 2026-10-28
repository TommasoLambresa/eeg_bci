"""Run one block of trials: send the task markers that drive the Unity game."""

import random
import time

from pylsl import IRREGULAR_RATE, StreamInfo, StreamOutlet

from bci import config

# description expected by the Unity game
stream_info = StreamInfo(name=config.MARKER_STREAM_NAME, type="Markers", channel_count=1,
                         nominal_srate=IRREGULAR_RATE, channel_format="int32",
                         source_id="GrazMI")
outlet = StreamOutlet(stream_info)


def send_marker(code: int, duration_s: float) -> None:
    """Send one marker, then wait for the duration of its phase."""
    outlet.push_sample([code])
    time.sleep(duration_s)


cue_codes = {class_name: code for code, class_name in config.CUE_CLASS_NAMES.items()}

# Create the randomized trial list
trials = []
# array [left, left, left, ..., right, right, right, ..., rest, rest, rest] (depending on the classes in config.CLASSES)
for class_name in config.CLASSES:
    trials += [class_name] * config.TRIALS_PER_CLASS
# shuffle the trials so that they are in random order
random.shuffle(trials)

# Wait for the user to start the block
input(f"{len(trials)} trials ready. Press Enter to start the block...")
send_marker(config.BASELINE, config.BASELINE_S)
for trial_number, class_name in enumerate(trials, start=1):
    print(f"trial {trial_number}/{len(trials)}: {class_name}")
    send_marker(config.FIXATION, config.FIXATION_S)
    send_marker(cue_codes[class_name], config.CUE_S)
    send_marker(config.TASK, config.TASK_S)
    send_marker(config.PAUSE, config.PAUSE_S)
# wait before exiting so that receivers get the last marker
send_marker(config.BLOCK_END, 1.0)
print("Block finished.")