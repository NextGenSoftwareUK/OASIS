"""Qualify the discovered public countries table; preserve earlier absent-table failure."""
from pathlib import Path
import importlib.util
import json
import hashlib
import urllib.request
import urllib.error
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location("base_probe",Path(__file__).with_name("Probe-ClickHousePublicReadiness.py"))
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
KEYS=["France","Germany","Italy"]
QUERIES=["SELECT name AS country,toString(length(coordinates)) AS polygons FROM blogs.countries WHERE name IN ('France','Germany','Italy') ORDER BY name LIMIT 4 FORMAT JSON",
    "SELECT name AS country,toString(length(coordinates)) AS polygons FROM blogs.countries WHERE name IN ('France','Germany') ORDER BY name LIMIT 3 FORMAT JSON"]
def validate(raw,keys):
    if len(raw)>262144:raise ValueError("Oversized native body")
    value=json.loads(raw,object_pairs_hook=base.unique)
    if not isinstance(value,dict) or "exception" in value or not isinstance(value.get("data"),list):raise ValueError("Successful JSON required")
    rows=value["data"]
    if type(value.get("rows")) is not int or value["rows"]!=len(rows) or len(rows)!=len(keys):raise ValueError("Complete selection required")
    if any(not isinstance(row,dict) or set(row)!={"country","polygons"} for row in rows):raise ValueError("Exact public projection required")
    if [row["country"] for row in rows]!=keys:raise ValueError("Exact ordered native keys required")
    for row in rows:
        number=row["polygons"]
        if not isinstance(number,str) or not number.isascii() or not number.isdecimal() or str(int(number))!=number or not 1<=int(number)<=18446744073709551615:raise ValueError("Canonical UInt64 polygon count required")
    return rows

def main():
    state=ROOT/"LocalState/COSMIC/ClickHousePublicQualificationCurrent";results=ROOT/"TestResults/ClickHousePublicSourceCurrent"
    schema=json.loads((state/"countries-schema.json").read_text(encoding="utf-8"))
    if schema.get("columns")!=[{"name":"name","type":"String"},{"name":"coordinates","type":"MultiPolygon"}]:raise ValueError("Qualified schema required")
    with (state/"countries.intent.json").open("x",encoding="utf-8") as f:json.dump({"queries":QUERIES,"nativeWrites":0,"source":"blogs.countries","priorFailurePreserved":True},f,indent=2)
    observations=[];selection=[];passed=False
    try:
        for index,sql in enumerate(QUERIES):
            request=urllib.request.Request(base.BASE,data=sql.encode(),method="POST",headers={"Content-Type":"text/plain; charset=utf-8"})
            try:response=urllib.request.build_opener(base.NoRedirect()).open(request,timeout=15)
            except urllib.error.HTTPError as error:response=error
            with response:raw=response.read(262145);status=response.status
            with (results/f"countries-{index+1}.json").open("xb") as f:f.write(raw)
            row={"status":status,"bytes":len(raw),"sha256":hashlib.sha256(raw).hexdigest()};observations.append(row)
            if status!=200:raise ValueError("Native HTTP failure; no remaining calls")
            records=validate(raw,KEYS if index==0 else KEYS[:2]);selection.append(records);row["rows"]=len(records)
        if selection[0][:2]!=selection[1]:raise ValueError("Subset observation changed")
        passed=True
    except (urllib.error.URLError,TimeoutError,ValueError) as error:observations.append({"errorType":type(error).__name__,"reason":str(error)[:500]})
    report={"passed":passed,"source":"blogs.countries","observations":observations,"rows":selection,"nativeWrites":0,"cqlAccepted":False}
    with (state/"countries.json").open("x",encoding="utf-8") as f:json.dump(report,f,indent=2)
    print(json.dumps(report));return 0 if passed else 1
if __name__=="__main__":raise SystemExit(main())
