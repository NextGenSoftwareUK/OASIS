import copy
import importlib.util
import json
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location("probe", Path(__file__).with_name("Probe-AxelarPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)

class AxelarProbeTests(unittest.TestCase):
    def fixture(self):
        return {"data": [{"message_id": "message-" + str(i), "status": "executed", "call": {
            "chain": "ethereum", "transactionHash": "tx-" + str(i), "block_timestamp": 123,
            "returnValues": {"destinationChain": "avalanche"}}} for i in range(3)]}

    def test_three_unique_observations(self):
        value = self.fixture()
        self.assertEqual([r["messageId"] for r in probe.validate(value)], ["message-0", "message-1", "message-2"])

    def test_duplicate_and_incomplete_results_fail(self):
        value = self.fixture(); value["data"].pop()
        with self.assertRaises(ValueError): probe.validate(value)
        value = self.fixture(); value["data"][1]["message_id"] = "message-0"
        with self.assertRaises(ValueError): probe.validate(value)

    def test_error_envelope_and_missing_fields_fail(self):
        value = self.fixture(); value["error"] = True
        with self.assertRaises(ValueError): probe.validate(value)
        value = self.fixture(); del value["data"][0]["message_id"]
        with self.assertRaises(KeyError): probe.validate(value)

    def test_timestamp_and_identifier_bounds(self):
        for bad in [True, -1, 1.5, "123", 1 << 63]:
            value = self.fixture(); value["data"][0]["call"]["block_timestamp"] = bad
            with self.assertRaises(ValueError): probe.validate(value)
        for bad in ["", "x" * 129, "invalid space"]:
            value = self.fixture(); value["data"][0]["message_id"] = bad
            with self.assertRaises(ValueError): probe.validate(value)

    def test_duplicate_json_properties_fail(self):
        with self.assertRaises(ValueError): json.loads('{"data":[],"data":[]}', object_pairs_hook=probe.unique)
