"""Stellar header feasibility controls, no network or installation."""
import base64
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec=importlib.util.spec_from_file_location("stellar_probe",Path(__file__).with_name("Probe-StellarPublicReadiness.py"))
probe=importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)
ROW={'hash': 'c8deae88416e676c30358bef81cfbb7aa78546a7713d3c252653591e3c3340ce', 'prev_hash': '4cc75f196c333bcc325fc90d1a42ea39209513e7ad1696c1866573d90a74d768', 'sequence': 65247993, 'header_xdr': 'AAAAHUzHXxlsMzvMMl/JDRpC6jkglRPnrRaWwYZlc9kKdNdoTjCNVOqHMrsnmJcg+6EgreyCuLzPXHltEH+IHs+MjOcAAAAAaucH9QAAAAAAAAABAAAAAF60jCXLtH/CB/4KI+zln4jBc1nQ2p1AsXCUSYH47yvBAAAAQPxzrzJSy0nzXXUEARyz7KU3gQ0FB4fXpFo1bqlKhl2aD4Ky2/j0ZQ+GYJFO+u5+70JMLLmNoE3cZFqJLASI4QrB4X9kxbbO4mML52LWT5bR/MJRhcrswSZJ39UEEgG+NnU7Qirq8CWbNLxKij5BKHr4mR9ZGlFjsuyiPtSqWwIYA+Oa+Q6iHrPseVthAABjr1Pzg0MAAAEWAAAAAG8bf/IAAABkAExLQAAAA+iBCC8MsOtZLjU5HN1q6iFT5QcllPG/wOgBXBU4AGA+Xtr7PVWPOF0ni85riuBo+cQjwG9WX4OxeFTqunFDjGefrB3T50jbEIwtgiwPv0zF/m7rZ8gSe+zSmUC5KzCTkfAy/CmIHD6wNBmWE0N/i2Yg76Dtg7RN1j5wnDbOBCfdhAAAAAA=', 'protocolVersion': 29}


class HeaderTests(unittest.TestCase):
    def test_public_golden_xdr_hash_and_parent(self):
        self.assertEqual(ROW,probe.validate(copy.deepcopy(ROW)))

    def test_mutated_xdr_rejected(self):
        row=copy.deepcopy(ROW)
        raw=bytearray(base64.b64decode(row["header_xdr"]))
        raw[-1]^=1
        row["header_xdr"]=base64.b64encode(raw).decode()
        with self.assertRaisesRegex(ValueError,"hash or fixed parent"):
            probe.validate(row)

    def test_substituted_parent_rejected(self):
        row=copy.deepcopy(ROW);row["prev_hash"]="0"*64
        with self.assertRaises(ValueError):probe.validate(row)

    def test_noncanonical_or_short_xdr_rejected(self):
        for value in (ROW["header_xdr"]+"\n",base64.b64encode(b"x"*35).decode()):
            row=copy.deepcopy(ROW);row["header_xdr"]=value
            with self.subTest(value=value),self.assertRaises(ValueError):probe.validate(row)

    def test_api_sequence_is_bounded_unsigned_integer(self):
        for value in (True,0,-1,1<<32,1.5,"123"):
            row=copy.deepcopy(ROW);row["sequence"]=value
            with self.subTest(value=value),self.assertRaises(ValueError):probe.validate(row)

    def test_noncanonical_hash_rejected(self):
        for value in (ROW["hash"].upper(),"a"*63,"0x"+ROW["hash"]):
            row=copy.deepcopy(ROW);row["hash"]=value
            with self.subTest(value=value),self.assertRaises(ValueError):probe.validate(row)

    def test_duplicate_json_rejected(self):
        with self.assertRaises(ValueError):
            json.loads('{"hash":"a","hash":"b"}',object_pairs_hook=probe.unique)


if __name__=="__main__":unittest.main()
