import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("matrix_probe", Path(__file__).with_name("Probe-MatrixPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class MatrixProbeTests(unittest.TestCase):
    def test_room_ids_are_opaque_across_versions(self):
        for room in ("!old:matrix.org", "!L58ME6ufiP49v97UIOBIpvWKEgj4912JmECPuDzlvCI"):
            self.assertEqual(room, probe.validate({"room_id": room, "servers": ["matrix.org"]})["roomId"])

    def test_large_public_room_within_budget(self):
        self.assertEqual(1169, len(probe.validate({"room_id": "!opaque", "servers": ["matrix.org"] * 1169})["servers"]))

    def test_incomplete_or_malformed_rows_rejected(self):
        for value in ({}, [], {"room_id": "!x", "servers": []},
                      {"room_id": "x", "servers": ["matrix.org"]},
                      {"room_id": "!x\n", "servers": ["matrix.org"]},
                      {"room_id": "!x", "servers": [None]},
                      {"room_id": "!x", "servers": ["x"] * 4097}):
            with self.assertRaises(ValueError):
                probe.validate(value)

    def test_duplicate_properties_rejected(self):
        with self.assertRaises(ValueError):
            probe.unique([("room_id", "!a"), ("room_id", "!b")])


if __name__ == "__main__":
    unittest.main()
