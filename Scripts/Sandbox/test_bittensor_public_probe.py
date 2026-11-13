import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("bittensor_probe", Path(__file__).with_name("Probe-BittensorPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class BittensorProbeTests(unittest.TestCase):
    def records(self):
        parent = "0x" + "01" * 32
        rows = []
        for height in (1, 2, 3):
            header = {"parentHash": parent, "number": hex(height), "stateRoot": "0x" + "02" * 32,
                      "extrinsicsRoot": "0x" + "03" * 32, "digest": {"logs": []}}
            parent = probe.codec.header_hash(header)
            rows.insert(0, {"hash": parent, "header": header})
        return rows

    def test_three_locally_hashed_ancestors(self):
        rows = self.records()
        probe.verify_chain(rows, rows[0]["hash"])

    def test_wrong_anchor_tampered_header_and_missing_parent_fail(self):
        original = self.records()
        for change in ("anchor", "content", "parent", "count"):
            rows = copy.deepcopy(original); anchor = rows[0]["hash"]
            if change == "anchor": anchor = "0x" + "ff" * 32
            if change == "content": rows[1]["header"]["stateRoot"] = "0x" + "ff" * 32
            if change == "parent": rows[1]["hash"] = "0x" + "ff" * 32
            if change == "count": rows.pop()
            with self.subTest(change=change), self.assertRaises(ValueError): probe.verify_chain(rows, anchor)

    def test_valid_envelope_and_rpc_errors_wrong_identity_rejected(self):
        self.assertEqual("x", probe.result({"jsonrpc": "2.0", "id": 1, "result": "x"}, 1))
        for value in ({"jsonrpc": "2.0", "id": True, "result": "x"},
                      {"jsonrpc": "2.0", "id": 2, "result": "x"},
                      {"jsonrpc": "2.0", "id": 1, "result": "x", "error": {}},
                      {"jsonrpc": "1.0", "id": 1, "result": "x"}, {}):
            with self.subTest(value=value), self.assertRaises(ValueError): probe.result(value, 1)

    def test_duplicate_json_and_noncanonical_header_fail(self):
        with self.assertRaises(ValueError): probe.codec.unique([("id", 1), ("id", 2)])
        rows = self.records(); rows[0]["header"]["number"] = "0x03"
        with self.assertRaises(ValueError): probe.verify_chain(rows, rows[0]["hash"])


if __name__ == "__main__":
    unittest.main()
