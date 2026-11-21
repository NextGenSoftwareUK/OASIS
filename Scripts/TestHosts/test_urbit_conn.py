"""Frame-boundary controls; native codec/thread execution is a separate gate."""
import socket
import time
import unittest

from urbit_conn import MAX_FRAME, receive_frame


class UrbitConnFrameTests(unittest.TestCase):
    def read(self, data):
        writer, reader = socket.socketpair()
        with writer, reader:
            writer.sendall(data)
            writer.shutdown(socket.SHUT_WR)
            return receive_frame(reader, time.monotonic() + 1)

    def test_complete_frame_preserved(self):
        data = b'\0\x03\0\0\0abc'
        self.assertEqual(data, self.read(data))

    def test_truncated_header_rejected(self):
        with self.assertRaises(EOFError):
            self.read(b'\0\x03')

    def test_truncated_payload_rejected(self):
        with self.assertRaises(EOFError):
            self.read(b'\0\x03\0\0\0ab')

    def test_unknown_version_rejected(self):
        with self.assertRaises(ValueError):
            self.read(b'\x01\x01\0\0\0a')

    def test_zero_length_rejected(self):
        with self.assertRaises(ValueError):
            self.read(b'\0\0\0\0\0')

    def test_oversized_frame_rejected_before_body_read(self):
        with self.assertRaises(ValueError):
            self.read(b'\0' + (MAX_FRAME + 1).to_bytes(4, 'little'))

    def test_expired_deadline_rejected(self):
        writer, reader = socket.socketpair()
        with writer, reader, self.assertRaises(TimeoutError):
            receive_frame(reader, time.monotonic() - 1)


if __name__ == '__main__':
    unittest.main()
