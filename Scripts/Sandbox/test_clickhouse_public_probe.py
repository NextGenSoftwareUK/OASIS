import importlib.util
import json
from pathlib import Path
import unittest
spec=importlib.util.spec_from_file_location("probe",Path(__file__).with_name("Probe-ClickHousePublicReadiness.py"))
probe=importlib.util.module_from_spec(spec);spec.loader.exec_module(probe)
class ClickHouseProbeTests(unittest.TestCase):
    def payload(self,rows):return json.dumps({"data":rows,"rows":len(rows)}).encode()
    def posts(self):return [{"id":key,"title":"Public title","score":"1","tags":"cql","license":"CC BY-SA 4.0"} for key in ("4","6","9")]
    def test_exact_full_and_subset(self):
        self.assertEqual(3,len(probe.validate(self.payload(self.posts()),2)))
        self.assertEqual(2,len(probe.validate(self.payload(self.posts()[:2]),3)))
    def test_public_readonly_identity(self):
        good={"version":"25.1","user":"explorer","readonly":"2"}
        probe.validate(self.payload([good]),0)
        for key,value in (("user","default"),("readonly","0")):
            bad={**good,key:value}
            with self.assertRaises(ValueError):probe.validate(self.payload([bad]),0)
    def test_schema_is_exact(self):
        rows=[{"name":name,"type":"String"} for name in ("Id","Title","PostTypeId","Score","Tags","ContentLicense")]
        probe.validate(self.payload(rows),1)
        rows[-1]=rows[0]
        with self.assertRaises(ValueError):probe.validate(self.payload(rows),1)
    def test_duplicate_keys_rejected(self):
        with self.assertRaises(ValueError):probe.validate(b'{"data":[],"rows":0,"rows":0}',2)
    def test_native_error_and_oversize(self):
        for raw in (b'{"exception":"failure","data":[],"rows":0}',b' '*262145):
            with self.assertRaises(ValueError):probe.validate(raw,2)
    def test_missing_duplicate_and_wrong_ids(self):
        for rows in (self.posts()[:2],[self.posts()[0]]*3,list(reversed(self.posts()))):
            with self.assertRaises(ValueError):probe.validate(self.payload(rows),2)
    def test_boolean_counts_and_body_leaks_rejected(self):
        with self.assertRaises(ValueError):probe.validate(b'{"data":[],"rows":false}',2)
        rows=self.posts();rows[0]["body"]="Excluded body"
        with self.assertRaises(ValueError):probe.validate(self.payload(rows),2)
if __name__=="__main__":unittest.main()
