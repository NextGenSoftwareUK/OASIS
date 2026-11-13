import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("stacks_probe", Path(__file__).with_name("Probe-StacksPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class StacksProbeTests(unittest.TestCase):
    def info(self):
        return {"network_id": 1, "is_fully_synced": True, "stacks_tip_height": 9394169,
                "stacks_tip": "2440e05daa65501aa70503d61cc4f4b92e1c37770f74688bd8bab927bc8cdd5f",
                "stacks_tip_consensus_hash": "57172fc7713ec90f7c2742593a4087f66dea234e"}

    def test_explicit_tip_order_matches_node_hash_algorithm(self):
        # Captured native vector: the node accepted this derived block ID for
        # all three explicit-tip reads. Fix the expected value independently of
        # the implementation so reversing bytes or truncating SHA-512 fails.
        self.assertEqual("ccb149a1faca3a57f68fb0ed0b9474931c8036ba6d7f4eeffb9de61dd177b8b5",
                         probe.tip_from_info(self.info()))

    def test_invalid_tip_never_reaches_silent_server_fallback(self):
        for value in ("latest", "", "0x" + "a" * 64, "A" * 64, "a" * 63):
            info = self.info(); info["stacks_tip"] = value
            with self.assertRaises(ValueError): probe.tip_from_info(info)

    def test_wrong_network_or_unsynced_node_rejected(self):
        for key, value in (("network_id", True), ("network_id", 2147483648), ("is_fully_synced", False), ("stacks_tip_height", -1)):
            info = self.info(); info[key] = value
            with self.assertRaises(ValueError): probe.tip_from_info(info)

    def test_exact_full_width_amounts_do_not_round(self):
        result = probe.account({"balance": "0x" + "f" * 32, "locked": "0x" + "0" * 32,
                                "unlock_height": 0, "nonce": 18446744073709551615})
        self.assertEqual("340282366920938463463374607431768211455", result["balance"])
        self.assertEqual("18446744073709551615", result["nonce"])

    def test_incomplete_or_noncanonical_account_data_rejected(self):
        for field, value in (("balance", "0x1"), ("balance", "0x" + "F" * 32), ("locked", None),
                             ("nonce", True), ("nonce", 18446744073709551616), ("unlock_height", -1)):
            row = {"balance": "0x" + "0" * 32, "locked": "0x" + "0" * 32, "nonce": 0, "unlock_height": 0}
            row[field] = value
            with self.assertRaises(ValueError): probe.account(row)
        with self.assertRaises(ValueError): probe.account({})

    def test_duplicate_json_properties_rejected(self):
        with self.assertRaises(ValueError): probe.unique([("nonce", 1), ("nonce", 2)])


if __name__ == "__main__":
    unittest.main()
