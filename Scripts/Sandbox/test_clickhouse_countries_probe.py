from pathlib import Path
import importlib.util,json,unittest
spec=importlib.util.spec_from_file_location("probe",Path(__file__).with_name("Probe-ClickHouseCountriesReadiness.py"))
p=importlib.util.module_from_spec(spec);spec.loader.exec_module(p)
class CountriesTests(unittest.TestCase):
    def raw(self,rows):return json.dumps({"data":rows,"rows":len(rows)}).encode()
    def rows(self):return [{"country":key,"polygons":"1"} for key in p.KEYS]
    def test_native_three_two_and_precision(self):
        rows=self.rows();rows[0]["polygons"]="18446744073709551615"
        self.assertEqual(rows,p.validate(self.raw(rows),p.KEYS))
        self.assertEqual(2,len(p.validate(self.raw(rows[:2]),p.KEYS[:2])))
    def test_count_and_key_mismatch(self):
        for rows in (self.rows()[:2],list(reversed(self.rows())),[self.rows()[0]]*3):
            with self.assertRaises(ValueError):p.validate(self.raw(rows),p.KEYS)
    def test_numeric_corruption_rejected(self):
        for number in (1,"01","-1","0","18446744073709551616","１"):
            rows=self.rows();rows[0]["polygons"]=number
            with self.assertRaises(ValueError):p.validate(self.raw(rows),p.KEYS)
    def test_geometry_and_duplicate_or_oversize_rejected(self):
        rows=self.rows();rows[0]["coordinates"]=[1]
        for raw in (self.raw(rows),b'{"data":[],"data":[],"rows":0}',b' '*262145):
            with self.assertRaises(ValueError):p.validate(raw,p.KEYS)
if __name__=="__main__":unittest.main()
