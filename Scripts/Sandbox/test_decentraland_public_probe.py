import importlib.util,json,unittest
from pathlib import Path
spec=importlib.util.spec_from_file_location("probe",Path(__file__).with_name("Probe-DecentralandPublicReadiness.py"));p=importlib.util.module_from_spec(spec);spec.loader.exec_module(p)
class CatalystTests(unittest.TestCase):
    def fixtures(self):return [{"version":"v3","id":"bafy"+char*50,"type":"scene","timestamp":1770000000000,"pointers":[key],"metadata":{"display":{"title":"Public scene"},"scene":{"parcels":[key]},"contact":{"email":"excluded"}},"content":[{"file":"game.js","hash":"not-downloaded"}]} for char,key in zip("abc",p.POINTERS)]
    def test_mainnet_readiness(self):
        good={"healthy":True,"content":{"healthy":True},"configurations":{"networkId":1}};self.assertEqual(1,p.about(json.dumps(good).encode())["networkId"])
        good["configurations"]["networkId"]=True
        with self.assertRaises(ValueError):p.about(json.dumps(good).encode())
    def test_full_and_subset_with_no_raw_metadata(self):
        rows=self.fixtures();full=p.scenes(json.dumps(rows).encode(),p.POINTERS);subset=p.scenes(json.dumps(rows[:2]).encode(),p.POINTERS[:2]);self.assertEqual(full[:2],subset)
        self.assertNotIn("contact",full[0]);self.assertNotIn("content",full[0]);self.assertFalse(full[0]["cidIntegrityVerified"])
    def test_missing_duplicate_foreign_and_shared_scene(self):
        for fault in ("missing","duplicate","foreign","shared"):
            rows=self.fixtures()
            if fault=="missing":rows.pop()
            if fault=="duplicate":rows[1]["id"]=rows[0]["id"]
            if fault=="foreign":rows[0]["pointers"]=["9,9"]
            if fault=="shared":rows[0]["pointers"].append(p.POINTERS[1])
            with self.assertRaises(ValueError):p.scenes(json.dumps(rows).encode(),p.POINTERS)
    def test_precision_and_parcel_consistency(self):
        for fault in ("float","overflow","parcel"):
            rows=self.fixtures()
            if fault=="float":rows[0]["timestamp"]=1.5
            if fault=="overflow":rows[0]["timestamp"]=9223372036854775808
            if fault=="parcel":rows[0]["metadata"]["scene"]["parcels"]=["9,9"]
            with self.assertRaises(ValueError):p.scenes(json.dumps(rows).encode(),p.POINTERS)
    def test_duplicate_and_oversize_json(self):
        for raw in (b'{"healthy":true,"healthy":true}',b' '*524289):
            with self.assertRaises(ValueError):p.about(raw)
if __name__=="__main__":unittest.main()
