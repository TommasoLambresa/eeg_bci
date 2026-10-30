"""Classify the EEG online with a trained model and publish the class probabilities."""

import argparse

import numpy as np
from pylsl import (IRREGULAR_RATE, StreamInfo, StreamInlet, StreamOutlet, proc_clocksync, proc_dejitter,
                   resolve_byprop)

from bci import config
from bci.decoder import load_model
from bci.preprocessing import bandpass

RESOLVE_TIMEOUT_S = 30.0

parser = argparse.ArgumentParser(description="Classify the EEG online and publish the class probabilities")
parser.add_argument("model_path", help="model saved by the training notebook")
arguments = parser.parse_args()
model = load_model(arguments.model_path)
pipeline = model["pipeline"]
class_names = list(pipeline.classes_)

found_streams = resolve_byprop("name", config.EEG_STREAM_NAME, timeout=RESOLVE_TIMEOUT_S)
if not found_streams:
    raise RuntimeError(f"Stream '{config.EEG_STREAM_NAME}' not found")
eeg_info = found_streams[0]
sampling_rate_hz = eeg_info.nominal_srate()
if sampling_rate_hz != model["sampling_rate_hz"]:
    raise RuntimeError(f"EEG at {sampling_rate_hz} Hz, model trained at {model['sampling_rate_hz']} Hz")
eeg_inlet = StreamInlet(eeg_info, processing_flags=proc_clocksync | proc_dejitter)

# one channel per class, in the order of the model
stream_info = StreamInfo(name=config.DECODER_STREAM_NAME, type="Probabilities", channel_count=len(class_names),
                         nominal_srate=IRREGULAR_RATE, channel_format="float32", source_id="decoder")
channels_node = stream_info.desc().append_child("channels")
for class_name in class_names:
    channels_node.append_child("channel").append_child_value("label", class_name)
outlet = StreamOutlet(stream_info)

window_samples = int(round(model["window_s"] * sampling_rate_hz))
update_samples = int(round(config.DECODER_UPDATE_S * sampling_rate_hz))
window = np.zeros((window_samples, eeg_info.channel_count()))
filter_state = None
received_samples = 0
samples_since_update = 0
print(f"Decoding {class_names} from '{config.EEG_STREAM_NAME}'. Press Ctrl+C to stop.")

while True:
    # return as soon as one update step of samples has arrived
    chunk, timestamps = eeg_inlet.pull_chunk(timeout=1.0, max_samples=update_samples)
    if not timestamps:
        continue
    filtered_chunk, filter_state = bandpass(np.asarray(chunk), sampling_rate_hz, model["band_hz"][0],
                                            model["band_hz"][1], model["filter_order"], filter_state)
    # keep only the most recent window of filtered samples
    window = np.vstack([window, filtered_chunk])[-window_samples:]
    received_samples += len(timestamps)
    samples_since_update += len(timestamps)
    if received_samples < window_samples or samples_since_update < update_samples:
        continue
    samples_since_update = 0
    # pipeline input: windows x channels x samples
    probabilities = pipeline.predict_proba(window.T[np.newaxis])[0]
    # timestamp of the newest EEG sample in the window, to measure the latency later
    outlet.push_sample(probabilities, timestamps[-1])
    print(" | ".join(f"{name} {probability:.2f}" for name, probability in zip(class_names, probabilities)), end="\r")