# eeg_bci

Real-time EEG brain-computer interface: knee extension motor imagery controls the legs of an avatar in a Unity game.

## Setup

```
conda create -n eeg_bci python=3.12
conda activate eeg_bci
pip install -r requirements.txt
```

## Data folder

Saved blocks go to `SAVE_DIR` in `bci/config.py`: by default `data/` inside the repository, which git ignores. Change it on your computer if needed; the path must not contain spaces.
