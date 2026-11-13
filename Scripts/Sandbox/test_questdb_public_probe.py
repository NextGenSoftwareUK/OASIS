from pathlib import Path
import importlib.util,json,unittest
s=importlib.util.spec_from_file_location("probe",Path(__file__).with_name("Probe-QuestDbPublicReadiness.py"));p=importlib.util.module_from_spec(s);s.loader.exec_module(p)
class QuestDbProbeTests(unittest.TestCase):
    def payload(self,stage):
        rows=[["Build Information: QuestDB9.0.0"]] if stage==0 else [[key] for key in (p.KEYS if stage==1 else p.KEYS[:2])]
        return {"query":p.QUERIES[stage],"columns":[{"name":"build","type":"STRING"}] if stage==0 else [{"name":"symbol","type":"SYMBOL"}],"dataset":rows,"count":len(rows)}
    def test_identity_and_three_two(self):
        for stage in range(3):p.validate(json.dumps(self.payload(stage)).encode(),stage)
    def test_missing_duplicate_and_foreign_keys(self):
        for fault in ("missing","duplicate","foreign"):
            v=self.payload(1)
            if fault=="missing":v["dataset"].pop();v["count"]-=1
            if fault=="duplicate":v["dataset"][1]=v["dataset"][0]
            if fault=="foreign":v["dataset"][0]=["OTHER"]
            with self.assertRaises(ValueError):p.validate(json.dumps(v).encode(),1)
    def test_schema_query_and_numeric_count_mismatch(self):
        for fault in ("schema","query","count","typed-value"):
            v=self.payload(1)
            if fault=="schema":v["columns"][0]["type"]="STRING"
            if fault=="query":v["query"]="SELECT1"
            if fault=="count":v["count"]=True
            if fault=="typed-value":v["dataset"][0]=[1]
            with self.assertRaises(ValueError):p.validate(json.dumps(v).encode(),1)
    def test_error_duplicate_and_oversized_envelope(self):
        v=self.payload(1);v["error"]="failure"
        for raw in (json.dumps(v).encode(),b'{"count":0,"count":0}',b' '*262145):
            with self.assertRaises(ValueError):p.validate(raw,1)
    def test_wrong_build_identity(self):
        v=self.payload(0);v["dataset"][0]=["Other database"]
        with self.assertRaises(ValueError):p.validate(json.dumps(v).encode(),0)
if __name__=="__main__":unittest.main()
