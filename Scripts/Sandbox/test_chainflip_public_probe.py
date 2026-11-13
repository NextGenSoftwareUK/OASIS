import importlib.util
import json
import unittest
from pathlib import Path
spec = importlib.util.spec_from_file_location("probe", Path(__file__).with_name("Probe-ChainflipPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)

class ChainflipProbeTests(unittest.TestCase):
    def fixture(self):
        path = probe.ROOT / "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests/Fixtures/chainflip-swap.json"
        return json.loads(path.read_text(encoding="utf-8-sig"))

    def test_existing_real_fixture_keeps_exact_quantities_and_native_identity(self):
        row = probe.validate(self.fixture(), "500000")
        self.assertEqual(row["depositAmount"], "530649")
        self.assertEqual(row["depositWitnessedAtMilliseconds"], "1746793380000")
        self.assertFalse(row["independentFinalityVerified"])

    def test_wrong_or_noncanonical_identity_is_not_substituted(self):
        with self.assertRaises(ValueError): probe.validate(self.fixture(), "500001")
        for bad in [500000, "0500000", "18446744073709551616"]:
            value = self.fixture(); value["swapId"] = bad
            with self.assertRaises(ValueError): probe.validate(value, str(bad))

    def test_quantity_is_exact_bounded_decimal_text(self):
        value = self.fixture(); value["deposit"]["amount"] = "9" * 78
        self.assertEqual(probe.validate(value, "500000")["depositAmount"], "9" * 78)
        for bad in [1, 1.5, "-1", "01", "1e4", "9" * 79]:
            value = self.fixture(); value["deposit"]["amount"] = bad
            with self.assertRaises(ValueError): probe.validate(value, "500000")

    def test_timestamp_does_not_coerce_or_round(self):
        for bad in [True, "123", 1.5, -1, 1 << 63]:
            value = self.fixture(); value["deposit"]["witnessedAt"] = bad
            with self.assertRaises(ValueError): probe.validate(value, "500000")

    def test_refund_is_an_observation_not_a_successful_destination_claim(self):
        value = self.fixture(); value["refundEgress"] = {"amount": "100"}
        self.assertTrue(probe.validate(value, "500000")["refundObserved"])
        value["state"] = "WAITING"
        with self.assertRaises(ValueError): probe.validate(value, "500000")

    def test_error_missing_and_unbounded_fields_fail(self):
        value = self.fixture(); value["error"] = False
        with self.assertRaises(ValueError): probe.validate(value, "500000")
        value = self.fixture(); del value["swap"]
        with self.assertRaises(KeyError): probe.validate(value, "500000")
        value = self.fixture(); value["deposit"]["txRef"] = "x" * 257
        with self.assertRaises(ValueError): probe.validate(value, "500000")

    def test_duplicate_json_properties_fail(self):
        with self.assertRaises(ValueError): json.loads('{"swapId":"1","swapId":"2"}', object_pairs_hook=probe.unique)


    def test_absent_external_deposit_keeps_swap_amounts_without_fabricating_zero(self):
        value = self.fixture(); del value['deposit']
        result = probe.validate(value, '500000')
        self.assertFalse(result['externalDepositObserved'])
        self.assertIsNone(result['depositAmount'])
        self.assertIsNone(result['depositTxRef'])
        self.assertEqual(result['swapAmounts']['originalInputAmount'], '529627')

    def test_present_deposit_requires_complete_object(self):
        for bad in [None, [], 'unavailable', {}]:
            value = self.fixture(); value['deposit'] = bad
            with self.assertRaises((ValueError, KeyError)): probe.validate(value, '500000')
