"""Turn the decoder probabilities into leg movements and streams them for the Unity game."""

import time

import numpy as np
from pylsl import StreamInfo, StreamInlet, StreamOutlet, local_clock, resolve_byprop

from bci import config

RESOLVE_TIMEOUT_S = 30.0
LEG_CLASSES = ["left", "right"]
# channels expected by the Unity game; state_id: 0 no command, 1 left, 2 unused, 3 right
CONTROLLER_UNITY_CHANNELS = ["left_leg", "right_leg", "rest", "left", "both", "right",
                             "confidence", "density_gate", "state_id"]
STATE_IDS = {None: 0, "left": 1, "right": 3}

marker_streams = resolve_byprop("name", config.MARKER_STREAM_NAME, timeout=RESOLVE_TIMEOUT_S)
decoder_streams = resolve_byprop("name", config.DECODER_STREAM_NAME, timeout=RESOLVE_TIMEOUT_S)
if not marker_streams or not decoder_streams:
    raise RuntimeError("Start the marker stream and the decoder first")
marker_inlet = StreamInlet(marker_streams[0])
decoder_inlet = StreamInlet(decoder_streams[0])

# class of each decoder channel, from the labels written by the decoder
class_names = []
channel = decoder_inlet.info().desc().child("channels").child("channel")
while not channel.empty():
    class_names.append(channel.child_value("label"))
    channel = channel.next_sibling()

stream_info = StreamInfo(name=config.CONTROLLER_UNITY_STREAM_NAME, type="BCIControl",
                         channel_count=len(CONTROLLER_UNITY_CHANNELS), nominal_srate=config.CONTROLLER_UNITY_RATE_HZ,
                         channel_format="float32", source_id="controller_unity")
channels_node = stream_info.desc().append_child("channels")
for label in CONTROLLER_UNITY_CHANNELS:
    channels_node.append_child("channel").append_child_value("label", label)
outlet = StreamOutlet(stream_info)

period_s = 1.0 / config.CONTROLLER_UNITY_RATE_HZ
# exponential smoothing with time constant CONTROLLER_UNITY_SMOOTHING_S
smoothing_factor = 1.0 - np.exp(-period_s / config.CONTROLLER_UNITY_SMOOTHING_S)
latest_probabilities = np.full(len(class_names), 1.0 / len(class_names))
smoothed_probabilities = latest_probabilities.copy()
last_decoder_s = local_clock()
task_running = False
active_class = None
leg_positions = {leg: 0.0 for leg in LEG_CLASSES}
next_tick_s = local_clock()
print(f"Controlling the Unity legs from {class_names}. Press Ctrl+C to stop.")

while True:
    # commands only between the TASK marker and the next marker
    marker_samples, _ = marker_inlet.pull_chunk(timeout=0.0)
    for marker_sample in marker_samples:
        task_running = marker_sample[0] == config.TASK
    decoder_samples, _ = decoder_inlet.pull_chunk(timeout=0.0)
    if decoder_samples:
        latest_probabilities = np.array(decoder_samples[-1])
        last_decoder_s = local_clock()
    smoothed_probabilities += smoothing_factor * (latest_probabilities - smoothed_probabilities)
    smoothed = dict(zip(class_names, smoothed_probabilities))

    # hysteresis: a leg starts above the enter threshold and stops below the lower exit threshold
    command_allowed = task_running and local_clock() - last_decoder_s < config.CONTROLLER_UNITY_STALE_S
    below_exit = active_class is not None and smoothed[active_class] < config.CONTROLLER_UNITY_EXIT_PROBABILITY
    previous_class = active_class
    if not command_allowed or below_exit:
        active_class = None
    if command_allowed and active_class is None:
        best_class = max(LEG_CLASSES, key=smoothed.get)
        if smoothed[best_class] > config.CONTROLLER_UNITY_ENTER_PROBABILITY:
            active_class = best_class
    if active_class != previous_class:
        print(f"command: {active_class or 'none'}")

    # the active leg extends, the other one returns, both at constant speed
    for leg in LEG_CLASSES:
        if leg == active_class:
            leg_positions[leg] = min(1.0, leg_positions[leg] + period_s / config.LEG_RISE_S)
        else:
            leg_positions[leg] = max(0.0, leg_positions[leg] - period_s / config.LEG_FALL_S)

    # density_gate channel: 1 while commands are allowed
    confidence = smoothed[active_class] if active_class else max(smoothed.values())
    outlet.push_sample([leg_positions["left"], leg_positions["right"],
                        float(active_class is None), float(active_class == "left"), 0.0, float(active_class == "right"),
                        confidence, float(command_allowed), STATE_IDS[active_class]])

    # absolute deadlines: loop overhead does not accumulate
    next_tick_s += period_s
    time.sleep(max(0.0, next_tick_s - local_clock()))