import socket
import sys
import unittest
from DaprNativePorts import require_free


class NativePortOwnershipTests(unittest.TestCase):
    def test_active_listener_is_rejected(self):
        with socket.socket() as listener:
            if sys.platform == 'linux':
                listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            listener.bind(('127.0.0.1', 0))
            listener.listen()
            with self.assertRaises(OSError):
                require_free((listener.getsockname()[1],))

    @unittest.skipUnless(sys.platform == 'linux', 'Linux TCP TIME_WAIT semantics')
    def test_closed_native_connection_does_not_look_like_an_active_listener(self):
        with socket.socket() as listener:
            listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            listener.bind(('127.0.0.1', 0))
            port = listener.getsockname()[1]
            listener.listen()
            with socket.create_connection(('127.0.0.1', port), timeout=2) as client:
                connection, _ = listener.accept()
                connection.close()
                self.assertEqual(client.recv(1), b'')
        require_free((port,))


if __name__ == '__main__':
    unittest.main()
