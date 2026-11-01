"""Native header verification controls; no network or installation."""
import importlib.util
import hashlib
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("bitcoin_probe", Path(__file__).with_name("Probe-BitcoinPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)

GENESIS_HEADER = (
    "01000000" + "00" * 32
    + "3ba3edfd7a7b12b27ac72c3e67768f617fc81bc3888a51323a9fb8aa4b1e5e4a"
    + "29ab5f49ffff001d1dac2b7c")


class HeaderTests(unittest.TestCase):
    def test_known_mainnet_genesis(self):
        row = probe.validate_header(GENESIS_HEADER, probe.GENESIS, "0" * 64)
        self.assertEqual(1231006505, row["timestamp"])

    def test_mutated_header_rejected(self):
        with self.assertRaisesRegex(ValueError, "identity mismatch"):
            probe.validate_header(GENESIS_HEADER[:-2] + "00", probe.GENESIS, "0" * 64)

    def test_disconnected_parent_rejected(self):
        with self.assertRaisesRegex(ValueError, "Previous block"):
            probe.validate_header(GENESIS_HEADER, probe.GENESIS, "1" * 64)

    def test_noncanonical_or_overlong_header_rejected(self):
        for header in (GENESIS_HEADER.upper(), GENESIS_HEADER + "00", GENESIS_HEADER[:-2]):
            with self.subTest(header=header), self.assertRaises(ValueError):
                probe.validate_header(header, probe.GENESIS, "0" * 64)

    def test_noncanonical_hash_rejected(self):
        with self.assertRaises(ValueError):
            probe.validate_header(GENESIS_HEADER, probe.GENESIS.upper(), "0" * 64)

    def test_self_consistent_hash_without_valid_work_rejected(self):
        raw = bytes.fromhex(GENESIS_HEADER[:-8] + "00000000")
        identity = hashlib.sha256(hashlib.sha256(raw).digest()).digest()[::-1].hex()
        with self.assertRaisesRegex(ValueError, "proof-of-work mismatch"):
            probe.validate_header(raw.hex(), identity, "0" * 64)

    def test_negative_target_rejected_even_with_matching_identity(self):
        raw = bytearray.fromhex(GENESIS_HEADER)
        raw[72:76] = (0x1d80ffff).to_bytes(4, "little")
        identity = hashlib.sha256(hashlib.sha256(raw).digest()).digest()[::-1].hex()
        with self.assertRaisesRegex(ValueError, "Invalid mainnet"):
            probe.validate_header(raw.hex(), identity, "0" * 64)


if __name__ == "__main__":
    unittest.main()
