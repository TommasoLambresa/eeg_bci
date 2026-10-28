"""Event marker codes shared by the task, the recordings and the Unity game.

Values are fixed by the Unity game; code 7 is reserved and must not be reused.
"""

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