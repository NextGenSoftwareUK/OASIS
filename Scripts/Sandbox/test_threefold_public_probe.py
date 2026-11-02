from pathlib import Path
import importlib.util,json,unittest
s=importlib.util.spec_from_file_location("probe",Path(__file__).with_name("Validate-ThreeFoldPublicReadiness.py"));p=importlib.util.module_from_spec(s);s.loader.exec_module(p)
class GridTests(unittest.TestCase):
    def row(self,key):return {"nodeId":key,"farmId":2,"gridVersion":3,"country":"Belgium","city":"Ghent","publicConfig":{"ipv4":"excluded"},"total_resources":{"mru":123}}
    def test_discovery_and_point_projection(self):
        rows=[self.row(key) for key in (1,2,3)];full=p.validate(json.dumps(rows).encode())["rows"]
        for index,row in enumerate(rows):self.assertEqual(full[index],p.validate(json.dumps(row).encode(),[str(index+1)],True)["rows"][0])
        self.assertNotIn("publicConfig",full[0]);self.assertNotIn("total_resources",full[0]);self.assertFalse(full[0]["chainStateVerified"])
    def test_missing_duplicate_foreign_and_order(self):
        for rows in ([self.row(1)],[self.row(1)]*3,[self.row(3),self.row(2),self.row(1)]):
            with self.assertRaises(ValueError):p.validate(json.dumps(rows).encode())
        with self.assertRaises(ValueError):p.validate(json.dumps(self.row(2)).encode(),["1"],True)
    def test_numeric_precision_types(self):
        row=self.row(4294967295);self.assertEqual("4294967295",p.project(row)["nodeId"])
        for value in (True,1.5,0,4294967296,"1"):
            row=self.row(value)
            with self.assertRaises(ValueError):p.project(row)
    def test_geo_types_and_payload_bound(self):
        row=self.row(1);row["city"]=None
        with self.assertRaises(ValueError):p.project(row)
        for raw in (b' '*262145,b'{"nodeId":1,"nodeId":2}'):
            with self.assertRaises(ValueError):p.validate(raw)
if __name__=="__main__":unittest.main()
