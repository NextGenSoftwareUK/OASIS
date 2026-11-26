"""Run one Hoon strand on a local pier through Vere's conn.sock (khan) and require its result.

Usage: urbit-thread.py <urbit-binary> <pier> <expected-result-regex> <hoon>
Prints the first regex group (or the whole result) and exits non-zero on any other reply.
"""
import os
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[5] / 'Scripts' / 'TestHosts'))
from urbit_conn import thread  # noqa: E402

PREFIX = '=/  m  (strand ,vase)  ;<  =bowl  bind:m  get-bowl  '


def main():
    binary, pier, expected, body = sys.argv[1:5]
    binary = str(Path(binary).resolve())
    # Unix socket paths are limited to 108 bytes; connect relative to the pier's .urb directory.
    os.chdir(Path(pier) / '.urb')
    reply = re.sub(r'\s+', ' ', thread(binary, Path('conn.sock'), PREFIX + body, timeout=600)).strip()
    match = re.fullmatch(r'\[1 %avow 0 %noun ' + expected + r'\]', reply)
    if match is None:
        sys.exit('Urbit strand rejected: ' + reply[:2000])
    print(match.group(1) if match.groups() else reply)


if __name__ == '__main__':
    main()
