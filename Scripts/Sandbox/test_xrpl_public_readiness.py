"""XRPL native header protocol controls, no network or external dependencies."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec=importlib.util.spec_from_file_location("xrpl_probe",Path(__file__).with_name("Probe-XrplPublicReadiness.py"))
probe=importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)

# Public RPC golden header, independently hashed using XRPLF's documented layout.
HEADER={'account_hash': 'C3B52124CDFCA43B88130B9D446E49F4D8AA618C7C885A60186C3B3DA2B85ED4', 'close_flags': 0, 'close_time': 846839092, 'close_time_human': '2026-Nov-01 09:04:52.000000000 UTC', 'close_time_iso': '2026-11-01T09:04:52Z', 'close_time_resolution': 10, 'closed': True, 'ledger_hash': '3AA1A57F3E24012170CD2922194620B795D9CBA8698E79A4EEFA10A26A8F6592', 'ledger_index': '108042083', 'parent_close_time': 846839091, 'parent_hash': 'EDDBA0BAB31F6EEC15E0C3913B2CB0207D11F7F8020DC77C76CFED364AF3520C', 'total_coins': '99985574693823492', 'transaction_hash': 'D425FA2B1EA2FC170A4EF426F6A3F17F548D76AEB51D56C7F3D9D915D5FF73C2'}


class XrplHeaderTests(unittest.TestCase):
    def fixture(self):
        return {"validated":True,"ledger_hash":HEADER["ledger_hash"],
                "ledger_index":HEADER["ledger_index"],"ledger":copy.deepcopy(HEADER)}

    def test_native_golden_hash_and_identity(self):
        self.assertEqual(HEADER["ledger_hash"],probe.header_hash(HEADER))
        self.assertEqual(HEADER,probe.validate(self.fixture(),HEADER["ledger_hash"]))

    def test_changed_native_field_rejected(self):
        result=self.fixture()
        result["ledger"]["close_time"]+=1
        with self.assertRaisesRegex(ValueError,"identity differs"):
            probe.validate(result)

    def test_validated_and_closed_required(self):
        for field in ("validated","closed"):
            result=self.fixture()
            if field=="validated":result[field]=False
            else:result["ledger"][field]=False
            with self.subTest(field=field),self.assertRaises(ValueError):
                probe.validate(result)

    def test_requested_identity_cannot_be_substituted(self):
        with self.assertRaisesRegex(ValueError,"Requested ledger"):
            probe.validate(self.fixture(),"0"*64)

    def test_envelope_sequence_must_match_header(self):
        result=self.fixture()
        result["ledger_index"]=int(result["ledger_index"])+1
        with self.assertRaisesRegex(ValueError,"sequence differs"):
            probe.validate(result)

    def test_unsigned_width_and_documented_decimal_forms(self):
        self.assertEqual(probe.uint(123,32),probe.uint("123",32,True))
        for value in (True,-1,1<<32,"0123","+123"," 123"):
            with self.subTest(value=value),self.assertRaises(ValueError):
                probe.uint(value,32,True)

    def test_duplicate_json_and_noncanonical_hash_rejected(self):
        with self.assertRaises(ValueError):
            json.loads('{"validated":true,"validated":true}',object_pairs_hook=probe.unique)
        for value in (HEADER["ledger_hash"].lower(),"A"*63,"0x"+HEADER["ledger_hash"]):
            with self.subTest(value=value),self.assertRaises(ValueError):
                probe.hash_text(value)


if __name__=="__main__":
    unittest.main()
