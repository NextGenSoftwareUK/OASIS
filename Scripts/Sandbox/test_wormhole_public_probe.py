import importlib.util
import json
import unittest
from pathlib import Path
spec = importlib.util.spec_from_file_location("probe", Path(__file__).with_name("Probe-WormholePublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)

class WormholeProbeTests(unittest.TestCase):
    def fixture(self):
        return {"operations": [{"id": "2/" + "a" * 64 + "/" + str(i), "emitterChain": 2,
            "emitterAddress": {"hex": "a" * 64}, "sequence": str(i)} for i in range(3)]}

    def test_exact_three_tuples(self):
        self.assertEqual(len(probe.validate(self.fixture())), 3)

    def test_duplicate_and_truncated_catalogues(self):
        value = self.fixture(); value["operations"].pop()
        with self.assertRaises(ValueError): probe.validate(value)
        value = self.fixture(); value["operations"][1] = value["operations"][0]
        with self.assertRaises(ValueError): probe.validate(value)

    def test_tuple_mismatch_and_chain_bounds(self):
        for bad in [True, 0, 65536, "2"]:
            value = self.fixture(); value["operations"][0]["emitterChain"] = bad
            with self.assertRaises(ValueError): probe.validate(value)
        value = self.fixture(); value["operations"][0]["id"] = "different"
        with self.assertRaises(ValueError): probe.validate(value)

    def test_sequence_precision_and_canonical_form(self):
        for bad in [1, "01", "-1", str(1 << 64)]:
            value = self.fixture(); value["operations"][0]["sequence"] = bad
            with self.assertRaises(ValueError): probe.validate(value)
        value = self.fixture(); sequence = str((1 << 64) - 1)
        value["operations"][0].update(sequence=sequence, id="2/" + "a" * 64 + "/" + sequence)
        self.assertEqual(probe.validate(value)[0]["sequence"], sequence)

    def test_error_envelope_address_and_duplicate_json(self):
        value = self.fixture(); value["error"] = False
        with self.assertRaises(ValueError): probe.validate(value)
        value = self.fixture(); value["operations"][0]["emitterAddress"]["hex"] = "A" * 64
        with self.assertRaises(ValueError): probe.validate(value)
        with self.assertRaises(ValueError): json.loads('{"operations":[],"operations":[]}', object_pairs_hook=probe.unique)


    def vaa_fixture(self):
        import base64
        record = self.fixture()["operations"][0]
        body = (123).to_bytes(4, 'big') + (456).to_bytes(4, 'big') + (2).to_bytes(2, 'big') + bytes.fromhex('a' * 64) + bytes(8) + bytes([15]) + b'payload'
        raw = bytes([1]) + (7).to_bytes(4, 'big') + bytes([1, 0]) + bytes(65) + body
        record['vaa'] = {'raw': base64.b64encode(raw).decode(), 'guardianSetIndex': 7, 'isDuplicated': False}
        return record, raw

    def test_vaa_structural_identity_without_signature_claim(self):
        record, raw = self.vaa_fixture(); result = probe.vaa(record)
        self.assertEqual(result['signatureCount'], 1)
        self.assertEqual(result['payloadBytes'], 7)
        self.assertEqual(result['timestamp'], '123')
        self.assertFalse(result['signatureVerified'])

    def test_vaa_truncated_signature_or_body_and_mismatched_tuple_fail(self):
        import base64
        record, raw = self.vaa_fixture()
        for size in range(123):
            record['vaa']['raw'] = base64.b64encode(raw[:size]).decode()
            with self.assertRaises(ValueError): probe.vaa(record)
        record, raw = self.vaa_fixture(); record['sequence'] = '1'; record['id'] = '2/' + 'a' * 64 + '/1'
        with self.assertRaises(ValueError): probe.vaa(record)

    def test_vaa_guardian_header_version_or_duplicate_observation_fail(self):
        import base64
        for field, value in [('guardianSetIndex', 8), ('isDuplicated', True)]:
            record, raw = self.vaa_fixture(); record['vaa'][field] = value
            with self.assertRaises(ValueError): probe.vaa(record)
        record, raw = self.vaa_fixture(); record['vaa']['raw'] = base64.b64encode(bytes([2]) + raw[1:]).decode()
        with self.assertRaises(ValueError): probe.vaa(record)
