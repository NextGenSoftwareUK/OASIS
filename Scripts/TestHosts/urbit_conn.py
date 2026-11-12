"""One bounded conn.c request using Vere's native jam/cue and newt framing.

Protocol: urbit/vere 8ddc4b786979574dbfcb655e3db1b634f658d0de,
pkg/vere/io/conn.c and pkg/vere/newt.c. No HTTP/login or legacy fallback.
"""
import socket
import subprocess
import time


MAX_FRAME = 1024 * 1024


def receive_frame(connection, deadline):
    def read_exact(count):
        result = bytearray()
        while len(result) < count:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                raise TimeoutError('Urbit conn response deadline exceeded.')
            connection.settimeout(remaining)
            chunk = connection.recv(count - len(result))
            if not chunk:
                raise EOFError('Urbit conn closed before a complete response.')
            result.extend(chunk)
        return bytes(result)

    header = read_exact(5)
    if header[0] != 0:
        raise ValueError('Unsupported newt frame version.')
    size = int.from_bytes(header[1:], 'little')
    if not 0 < size <= MAX_FRAME:
        raise ValueError('Urbit conn response exceeds the frame contract.')
    return header + read_exact(size)


def request(binary, socket_path, noun, timeout=120):
    if not 0 < timeout <= 600 or len(noun.encode()) > 65536:
        raise ValueError('Urbit conn request exceeds the operation contract.')
    deadline = time.monotonic() + timeout

    def evaluate(mode, payload):
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            raise TimeoutError('Urbit conn operation deadline exceeded.')
        result = subprocess.run([str(binary), 'eval', mode, '-n'], input=payload,
                                stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                timeout=remaining, check=True)
        if len(result.stdout) > MAX_FRAME + 5:
            raise ValueError('Native noun codec output exceeds budget.')
        return result.stdout

    jam = evaluate('--jam', (noun + '\n').encode())
    if len(jam) < 6 or jam[0] != 0 or int.from_bytes(jam[1:5], 'little') != len(jam) - 5:
        raise ValueError('Native noun codec did not produce one valid newt frame.')
    with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as connection:
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            raise TimeoutError('Urbit conn operation deadline exceeded.')
        connection.settimeout(remaining)
        connection.connect(str(socket_path))
        connection.sendall(jam)
        response = receive_frame(connection, deadline)
    return evaluate('--cue', response).decode('utf-8').strip()


def thread(binary, socket_path, hoon, timeout=120):
    # Same text noun contract as pinned Tlon backend/click-format -k.
    escaped = hoon.replace('\\', '\\\\').replace("'", "\\'").replace('\n', '  ')
    noun = "[1 %fyrd [%base %khan-eval %noun [%ted-eval '" + escaped + "']]]"
    return request(binary, socket_path, noun, timeout)
