"""Check a saved block: expected streams, EEG sampling rate and marker sequence."""

import sys
from collections import Counter

import numpy as np
import pyxdf

from bci import config

RATE_TOLERANCE = 0.001  # maximum relative difference between effective and nominal rate
GAP_THRESHOLD_SAMPLES = 10

streams, _ = pyxdf.load_xdf(sys.argv[1])
# an empty stream counts as missing
streams = [stream for stream in streams if len(stream["time_stamps"]) > 0]
stream_names = [stream["info"]["name"][0] for stream in streams]
streams_by_name = {stream["info"]["name"][0]: stream for stream in streams}
print(f"streams with data: {stream_names}")

problems = []
for name in (config.EEG_STREAM_NAME, config.MARKER_STREAM_NAME):
    if stream_names.count(name) != 1:
        problems.append(f"{name}: found {stream_names.count(name)} times, expected once")

if config.EEG_STREAM_NAME in streams_by_name:
    eeg = streams_by_name[config.EEG_STREAM_NAME]
    eeg_times = eeg["time_stamps"]
    channel_count = eeg["time_series"].shape[1]
    nominal_rate_hz = float(eeg["info"]["nominal_srate"][0])
    effective_rate_hz = eeg["info"]["effective_srate"]
    print(f"EEG: {channel_count} channels, {len(eeg_times)} samples, {eeg_times[-1] - eeg_times[0]:.1f} s, "
          f"effective rate {effective_rate_hz:.3f} Hz")
    if channel_count != config.EEG_CHANNEL_COUNT:
        problems.append(f"EEG: {channel_count} channels, expected {config.EEG_CHANNEL_COUNT}")
    if abs(effective_rate_hz / nominal_rate_hz - 1) > RATE_TOLERANCE:
        problems.append(f"EEG: effective rate {effective_rate_hz:.3f} Hz, nominal {nominal_rate_hz:.0f} Hz")
    gap_count = np.sum(np.diff(eeg_times) > GAP_THRESHOLD_SAMPLES / nominal_rate_hz)
    if gap_count > 0:
        problems.append(f"EEG: {gap_count} gaps")

if config.MARKER_STREAM_NAME in streams_by_name:
    markers = streams_by_name[config.MARKER_STREAM_NAME]
    codes = [int(code) for code in markers["time_series"][:, 0]]
    if codes[0] != config.BASELINE or codes[-1] != config.BLOCK_END:
        problems.append("markers: the block must start with BASELINE and end with BLOCK_END")
    # each trial is FIXATION, cue, TASK, PAUSE
    trial_codes = codes[1:-1]
    if len(trial_codes) % 4 != 0:
        problems.append(f"markers: {len(trial_codes)} trial markers, not a multiple of 4")
    trials_per_class = Counter()
    for first_index in range(0, len(trial_codes) - 3, 4):
        fixation, cue, task, pause = trial_codes[first_index:first_index + 4]
        valid_phases = (fixation, task, pause) == (config.FIXATION, config.TASK, config.PAUSE)
        if valid_phases and cue in config.CUE_CLASS_NAMES:
            trials_per_class[config.CUE_CLASS_NAMES[cue]] += 1
        else:
            problems.append(f"markers: trial {first_index // 4 + 1} has codes {[fixation, cue, task, pause]}")
    print(f"markers: {len(codes)} codes, complete trials per class {dict(trials_per_class)}")

if config.EEG_STREAM_NAME in streams_by_name and config.MARKER_STREAM_NAME in streams_by_name:
    marker_times = markers["time_stamps"]
    if eeg_times[0] > marker_times[0] or eeg_times[-1] < marker_times[-1]:
        problems.append("EEG does not cover the whole block")

if problems:
    print(f"RESULT: {len(problems)} problem(s)")
    for problem in problems:
        print(f"  - {problem}")
    sys.exit(1)
print("RESULT: OK")