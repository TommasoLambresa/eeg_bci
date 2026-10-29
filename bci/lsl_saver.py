"""Save LSL streams to XDF files by controlling LabRecorder through its remote control socket."""

import socket
import time
from pathlib import Path

DEFAULT_ADDRESS = ("localhost", 22345)
CONNECT_TIMEOUT_S = 2.0
COMMAND_TIMEOUT_S = 10.0
FILE_CREATION_TIMEOUT_S = 5.0
FORBIDDEN_PATH_CHARACTERS = set(" {}")


# Class for controlling LabRecorder via its remote control socket
class LslSaver:
    """Minimal client for the LabRecorder remote control socket."""

    def __init__(self, address=DEFAULT_ADDRESS):
        try:
            self.socket = socket.create_connection(address, timeout=CONNECT_TIMEOUT_S)
        except OSError as error:
            raise RuntimeError(f"LabRecorder not reachable at {address}: "
                               "is it open with remote control enabled?") from error
        # update, start and stop take about a second before LabRecorder answers
        self.socket.settimeout(COMMAND_TIMEOUT_S)

    def send(self, command):
        """Send one command and wait until LabRecorder has executed it."""
        self.socket.sendall((command + "\n").encode())
        # LabRecorder answers "OK" after each command; unread answers would make
        # close() reset the connection and discard the last command
        self.socket.recv(2)

    def start_saving(self, save_dir, relative_path):
        output_path = Path(save_dir) / relative_path
        if output_path.exists():
            raise RuntimeError(f"File already exists: {output_path}")
        root_text = Path(save_dir).as_posix().rstrip("/") + "/"
        template_text = Path(relative_path).as_posix()
        # the remote control separates its options with spaces and braces
        if FORBIDDEN_PATH_CHARACTERS & set(root_text + template_text):
            raise ValueError(f"Spaces or braces are not supported by LabRecorder: {output_path}")
        self.send("update")
        self.send("select all")
        self.send(f"filename {{root:{root_text}}} {{template:{template_text}}}")
        self.send("start")
        deadline = time.monotonic() + FILE_CREATION_TIMEOUT_S
        while not output_path.exists():
            if time.monotonic() > deadline:
                raise RuntimeError(f"File not created: {output_path}")
            time.sleep(0.1)
        return output_path

    def stop_saving(self):
        self.send("stop")

    def close(self):
        self.socket.close()
