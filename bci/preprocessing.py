"""Read saved blocks, filter the EEG and cut epochs around the task markers."""

from pathlib import Path

import numpy as np
import pyxdf
from scipy import signal

from bci import config


def load_block(path: str | Path) -> tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray]:
    """Return EEG samples (samples x channels), EEG times, marker codes and marker times."""
    streams, _ = pyxdf.load_xdf(str(path))
    streams_by_name = {stream["info"]["name"][0]: stream for stream in streams}
    eeg = streams_by_name[config.EEG_STREAM_NAME]
    markers = streams_by_name[config.MARKER_STREAM_NAME]
    marker_codes = markers["time_series"][:, 0].astype(int)
    return eeg["time_series"], eeg["time_stamps"], marker_codes, markers["time_stamps"]


def bandpass(eeg: np.ndarray, sampling_rate_hz: float, low_hz: float, high_hz: float, order: int,
             filter_state: np.ndarray | None = None) -> tuple[np.ndarray, np.ndarray]:
    """Causal Butterworth band-pass along time (rows).

    Offline, filter a whole recording and ignore the returned state. Online, pass the state returned by
    the previous call (None the first time) to filter chunk after chunk as if it were one signal.
    """
    sos = signal.butter(order, [low_hz, high_hz], btype="bandpass", fs=sampling_rate_hz, output="sos")
    if filter_state is None:
        filter_state = np.zeros((sos.shape[0], 2, eeg.shape[1]))
    return signal.sosfilt(sos, eeg, axis=0, zi=filter_state)


def task_epochs(eeg: np.ndarray, eeg_times: np.ndarray, marker_codes: np.ndarray, marker_times: np.ndarray,
                start_s: float, end_s: float) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    """Cut one epoch per TASK marker, from start_s to end_s around it, labelled with the preceding cue.

    Returns epochs (epochs x samples x channels), their class names and the epoch times in seconds.
    """
    sample_count = int(round((end_s - start_s) * config.EEG_SAMPLING_RATE_HZ))
    epochs = []
    labels = []
    cued_class = None
    for code, marker_time in zip(marker_codes, marker_times):
        if code in config.CUE_CLASS_NAMES:
            cued_class = config.CUE_CLASS_NAMES[code]
        elif code == config.TASK and cued_class is not None:
            epoch_start = marker_time + start_s
            first_index = np.searchsorted(eeg_times, epoch_start)
            # skip epochs that start before or end after the saved signal
            if eeg_times[0] <= epoch_start and first_index + sample_count <= len(eeg_times):
                epochs.append(eeg[first_index:first_index + sample_count])
                labels.append(cued_class)
            cued_class = None
    epoch_times_s = start_s + np.arange(sample_count) / config.EEG_SAMPLING_RATE_HZ
    return np.stack(epochs), np.array(labels), epoch_times_s


def band_power(epochs: np.ndarray, epoch_times_s: np.ndarray, window_s: tuple[float, float]) -> np.ndarray:
    """Mean power of each epoch and channel inside a time window."""
    in_window = (epoch_times_s >= window_s[0]) & (epoch_times_s < window_s[1])
    return np.mean(epochs[:, in_window, :] ** 2, axis=1)


def erd_percent(epochs: np.ndarray, epoch_times_s: np.ndarray, reference_window_s: tuple[float, float],
                activity_window_s: tuple[float, float]) -> np.ndarray:
    """Event-related desynchronization of each epoch and channel: 100 (activity / reference - 1)."""
    reference_power = band_power(epochs, epoch_times_s, reference_window_s)
    activity_power = band_power(epochs, epoch_times_s, activity_window_s)
    return 100 * (activity_power / reference_power - 1)


def sliding_windows(epochs: np.ndarray, labels: np.ndarray, window_samples: int,
                    step_samples: int) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    """Cut overlapping windows inside each epoch.

    Returns windows (windows x channels x samples, the pyriemann layout), their labels and the
    index of the epoch they come from, so that windows of one trial stay in the same fold.
    """
    windows = []
    window_labels = []
    window_trials = []
    for trial_index, (epoch, label) in enumerate(zip(epochs, labels)):
        for first_sample in range(0, epoch.shape[0] - window_samples + 1, step_samples):
            windows.append(epoch[first_sample:first_sample + window_samples].T)
            window_labels.append(label)
            window_trials.append(trial_index)
    return np.stack(windows), np.array(window_labels), np.array(window_trials)
