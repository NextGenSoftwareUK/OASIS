"""Require unowned listener ports free without confusing Linux TIME_WAIT with a listener."""
import socket
import sys


def require_free(ports):
    for port in ports:
        with socket.socket() as probe:
            # Native Linux servers use SO_REUSEADDR. Closed TCP connections may
            # retain TIME_WAIT; an active listener still prevents this bind.
            if sys.platform == 'linux':
                probe.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            probe.bind(('127.0.0.1', port))
