"""Launch the programs of a block in their own windows, in the order their streams need (Windows only)."""

import subprocess
import sys
from pathlib import Path

from pylsl import resolve_byprop, resolve_streams

from bci import config

SCRIPTS_DIR = Path(__file__).resolve().parent.parent / "scripts"
READY_TIMEOUT_S = 30.0
DISCOVERY_WAIT_S = 1.0


def launch(eeg_simulated: bool, model_path: str | Path | None, save: bool) -> list[subprocess.Popen]:
    """Start markers, EEG, decoder and saver, each after the stream of the previous one is visible.

    Without the simulator the EEG must come from the amplifier software, already running.
    No model path means no decoder. Returns the processes to pass to stop().
    """
    # program, its arguments, and the stream it publishes (None: nothing to wait for)
    programs = [("stream_markers.py", [], config.MARKER_STREAM_NAME)]
    if eeg_simulated:
        programs.append(("stream_eeg_simulated.py", [], config.EEG_STREAM_NAME))
    if model_path is not None:
        programs.append(("stream_decoder.py", [str(model_path)], config.DECODER_STREAM_NAME))
    if save:
        programs.append(("stream_saver.py", [], None))

    # launching twice would duplicate the streams
    running_names = {stream.name() for stream in resolve_streams(wait_time=DISCOVERY_WAIT_S)}
    duplicated_names = running_names & {stream_name for _, _, stream_name in programs}
    if duplicated_names:
        raise RuntimeError(f"Streams already running: {sorted(duplicated_names)}. Stop them first")
    if not eeg_simulated and config.EEG_STREAM_NAME not in running_names:
        raise RuntimeError(f"Stream '{config.EEG_STREAM_NAME}' not found: start the amplifier software first")

    processes = []
    for script_name, arguments, stream_name in programs:
        # cmd /k keeps the window open when the program ends, so errors stay readable
        command = ["cmd", "/k", sys.executable, str(SCRIPTS_DIR / script_name), *arguments]
        processes.append(subprocess.Popen(command, creationflags=subprocess.CREATE_NEW_CONSOLE))
        if stream_name is not None and not resolve_byprop("name", stream_name, timeout=READY_TIMEOUT_S):
            print(f"Stream '{stream_name}' not found: read the error in the {script_name} window, then call stop()")
            return processes

    if save:
        print("Wait for 'Saving to' in the stream_saver window, then press Enter in the stream_markers window.")
    else:
        print("Press Enter in the stream_markers window to start the block.")
    return processes


def stop(processes: list[subprocess.Popen]) -> None:
    """Close the windows and the programs running in them."""
    for process in processes:
        # /T also ends the program started by cmd, /F forces it
        subprocess.run(["taskkill", "/T", "/F", "/PID", str(process.pid)], capture_output=True)