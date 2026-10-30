"""Settings shared by all programs: marker codes and experiment parameters."""

from pathlib import Path

# Saved file name (BIDS): sub-<PARTICIPANT>_ses-<SESSION>_task-<BIDS_TASK>_run-<number>_eeg.xdf
PARTICIPANT = "p001"
SESSION = "3"
BIDS_TASK = "calibration"

# Folder for saved blocks (path without spaces);
SAVE_DIR = Path(__file__).resolve().parent.parent / "data"
SOURCEDATA_DIR = SAVE_DIR / "sourcedata"
MODELS_DIR = SAVE_DIR / "derivatives" / "models"

# Calibration block: phase durations in seconds and number of trials
BASELINE_S = 3.0
FIXATION_S = 2.0
CUE_S = 1.25
TASK_S = 3.75
PAUSE_S = 2.0

CLASSES = ["left", "right", "rest"]
TRIALS_PER_CLASS = 2

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

# EEG stream parameters (could be real and simulated)
EEG_STREAM_NAME = "SimulatedEEG"
EEG_SAMPLING_RATE_HZ = 500
EEG_CHANNEL_COUNT = 32

# Marker stream: name expected by the Unity game
MARKER_STREAM_NAME = "GrazMI_Markers"

# Decoder training; band, filter order and window length are saved with each model and reused online
DECODER_NAME = "tangent_space_lda"
DECODER_CLASSES = ["left", "right", "rest"]
DECODER_BAND_HZ = (8.0, 30.0)
DECODER_FILTER_ORDER = 4
WINDOW_S = 1.0
WINDOW_STEP_S = 0.25
# part of each imagery period used for training, relative to the TASK marker
TRAINING_START_S = 0.5
TRAINING_END_S = 3.75
FOLD_COUNT = 5

# Online decoder: output stream and how often the latest window is classified
DECODER_STREAM_NAME = "DecoderProbabilities"
DECODER_UPDATE_S = 0.05
