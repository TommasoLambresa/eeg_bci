"""Settings shared by all programs: marker codes and experiment parameters."""

from pathlib import Path

# Numeric codes recognized by the Unity game; 
BASELINE = 1
FIXATION = 2
CUE_LEFT = 3
CUE_RIGHT = 4
TASK = 5
PAUSE = 6
CUE_REST = 8
BLOCK_END = 99

CUE_CLASS_NAMES = {
    CUE_LEFT: "left",
    CUE_RIGHT: "right",
    CUE_REST: "rest",
}

# Calibration block: phase durations in seconds and number of trials
BASELINE_S = 3.0
FIXATION_S = 2.0
CUE_S = 1.25
TASK_S = 3.75
PAUSE_S = 2.0

CLASSES = ["left", "right", "rest"]
TRIALS_PER_CLASS = 15

# EEG stream parameters (could be real and simulated)
EEG_STREAM_NAME = "SimulatedEEG"
EEG_SAMPLING_RATE_HZ = 500
EEG_CHANNEL_COUNT = 32

# Marker stream: name expected by the Unity game
MARKER_STREAM_NAME = "GrazMI_Markers"

# Folder for saved blocks (path without spaces); data/ inside the repository is ignored by git
SAVE_DIR = Path(__file__).resolve().parent.parent / "data"
SOURCEDATA_DIR = SAVE_DIR / "sourcedata"
