"""Publish a simulated EEG stream: noise plus a 10 Hz rhythm that weakens during motor imagery."""
import time

import numpy as np
from pylsl import StreamInfo, StreamInlet, StreamOutlet, local_clock, resolve_byprop
from bci import config

CHUNK_SIZE = 10 # samples per channel per push
NOISE_STD_UV = 10.0
RHYTHM_FREQUENCY_HZ = 10.0 # fake EEG rhythm frequency
RHYTHM_AMPLITUDE_UV = 10.0 # fake EEG rhythm amplitude

# parameters for the event-related desynchronization (ERD) effect
ERD_FACTOR = 0.5
RESOLVE_TIMEOUT_S = 30.0

def rhythm_amplitudes(active_class: str | None) -> np.ndarray:
    """Rhythm amplitude of each channel; the first half of the channels plays the left hemisphere."""
    amplitudes = np.full(config.EEG_CHANNEL_COUNT, RHYTHM_AMPLITUDE_UV)
    half = config.EEG_CHANNEL_COUNT // 2
    if active_class == "left":
        amplitudes[half:] *= ERD_FACTOR
    elif active_class == "right":
        amplitudes[:half] *= ERD_FACTOR
    return amplitudes

# Initialize the LSL stream for simulated EEG data
stream_info = StreamInfo(name=config.EEG_STREAM_NAME, type="EEG",
                         channel_count=config.EEG_CHANNEL_COUNT,
                         nominal_srate=config.EEG_SAMPLING_RATE_HZ,
                         channel_format="float32",
                         source_id="simulated_eeg")

# Add channel metadata to the stream info
channels_node = stream_info.desc().append_child("channels")
for channel_index in range(config.EEG_CHANNEL_COUNT):
    channel_node = channels_node.append_child("channel")
    channel_node.append_child_value("label", f"ch{channel_index + 1}")
    channel_node.append_child_value("unit", "microvolts")
    channel_node.append_child_value("type", "EEG")

# Create the LSL outlet for streaming the simulated EEG data
outlet = StreamOutlet(stream_info)

# Create an LSL inlet to receive marker events from the marker (task phases) stream
found_streams = resolve_byprop("name", config.MARKER_STREAM_NAME, timeout=RESOLVE_TIMEOUT_S)
if not found_streams:
    raise RuntimeError(f"Stream '{config.MARKER_STREAM_NAME}' not found: start the marker stream first")
marker_inlet = StreamInlet(found_streams[0])
cued_class = None
active_class = None

random_generator = np.random.default_rng()
# each channel gets its own phase so that channels are not identical
rhythm_phases = random_generator.uniform(0, 2 * np.pi, config.EEG_CHANNEL_COUNT)
chunk_period_s = CHUNK_SIZE / config.EEG_SAMPLING_RATE_HZ # seconds per chunk
sample_index = 0
next_push_time = local_clock()
print(f"Streaming '{config.EEG_STREAM_NAME}': {config.EEG_CHANNEL_COUNT} channels at "
      f"{config.EEG_SAMPLING_RATE_HZ} Hz. Press Ctrl+C to stop.")

while True:

    # Pull all available marker samples from the marker stream 
    marker_samples, _ = marker_inlet.pull_chunk(timeout=0.0)
    for marker_sample in marker_samples:
        code = marker_sample[0]
        if code in config.CUE_CLASS_NAMES:
            cued_class = config.CUE_CLASS_NAMES[code]
        elif code == config.TASK:
            active_class = cued_class
            print(f"imagery: {active_class}")
        else:
            active_class = None

    # Build a chunk of simulated EEG data according to the current active class (left/right or rest)
    times_s = np.arange(sample_index, sample_index + CHUNK_SIZE) / config.EEG_SAMPLING_RATE_HZ
    # shape (CHUNK_SIZE, channel count): rows are samples, columns are channels
    rhythm = rhythm_amplitudes(active_class) * np.sin(2 * np.pi * RHYTHM_FREQUENCY_HZ * times_s[:, None] + rhythm_phases)
    # Inputs are (mean, std, shape). The output is shape (CHUNK_SIZE, channel count)
    noise = random_generator.normal(0, NOISE_STD_UV, (CHUNK_SIZE, config.EEG_CHANNEL_COUNT))
    
    # each push is a chunk of CHUNK_SIZE samples for all EEG_CHANNEL_COUNT channels, each channel has a 10 Hz sinusoid + different Gaussian noise and different phase
    outlet.push_chunk((rhythm + noise).astype(np.float32))
    sample_index += CHUNK_SIZE

    # absolute deadlines: loop overhead does not accumulate
    next_push_time += chunk_period_s
    time.sleep(max(0.0, next_push_time - local_clock()))