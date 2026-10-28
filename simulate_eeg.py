"""Publish a simulated EEG stream: Gaussian noise plus a 10 Hz rhythm on every channel (different phases)."""

import time

import numpy as np
from pylsl import StreamInfo, StreamOutlet, local_clock

from bci import config

CHUNK_SIZE = 10 # samples per channel per push
NOISE_STD_UV = 10.0
RHYTHM_FREQUENCY_HZ = 10.0 # fake EEG rhythm frequency
RHYTHM_AMPLITUDE_UV = 10.0 # fake EEG rhythm amplitude

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

random_generator = np.random.default_rng()
# each channel gets its own phase so that channels are not identical
rhythm_phases = random_generator.uniform(0, 2 * np.pi, config.EEG_CHANNEL_COUNT)
chunk_period_s = CHUNK_SIZE / config.EEG_SAMPLING_RATE_HZ # seconds per chunk
sample_index = 0
next_push_time = local_clock()
print(f"Streaming '{config.EEG_STREAM_NAME}': {config.EEG_CHANNEL_COUNT} channels at "
      f"{config.EEG_SAMPLING_RATE_HZ} Hz. Press Ctrl+C to stop.")

while True:
    times_s = np.arange(sample_index, sample_index + CHUNK_SIZE) / config.EEG_SAMPLING_RATE_HZ

    # shape (CHUNK_SIZE, channel count): rows are samples, columns are channels
    rhythm = RHYTHM_AMPLITUDE_UV * np.sin(2 * np.pi * RHYTHM_FREQUENCY_HZ * times_s[:, None] + rhythm_phases)

    # Inputs are (mean, std, shape). The output is shape (CHUNK_SIZE, channel count)
    noise = random_generator.normal(0, NOISE_STD_UV, (CHUNK_SIZE, config.EEG_CHANNEL_COUNT))

    # each push is a chunk of CHUNK_SIZE samples for all EEG_CHANNEL_COUNT channels, each channel has a 10 Hz sinusoid + different Gaussian noise and different phase
    outlet.push_chunk((rhythm + noise).astype(np.float32))
    sample_index += CHUNK_SIZE

    # absolute deadlines: loop overhead does not accumulate
    next_push_time += chunk_period_s
    time.sleep(max(0.0, next_push_time - local_clock()))