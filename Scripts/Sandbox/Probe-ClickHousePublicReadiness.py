"""Original bounded public SQL qualification; no installation, writes or retries."""
from pathlib import Path
import datetime
import hashlib
import json
import urllib.request
import urllib.error

ROOT = Path(__file__).resolve().parents[2]
BASE = "https://play.clickhouse.com/?user=explorer"
QUERIES = [
    "SELECT version() AS version, currentUser() AS user, toString(getSetting('readonly')) AS readonly FORMAT JSON",
    "SELECT name,type FROM system.columns WHERE database='stackoverflow' AND table='posts' AND name IN ('Id','Title','PostTypeId','Score','Tags','ContentLicense') ORDER BY name LIMIT 7 FORMAT JSON",
    "SELECT toString(Id) AS id, Title AS title, toString(Score) AS score, Tags AS tags, ContentLicense AS license FROM stackoverflow.posts WHERE Id IN (4,6,9) ORDER BY Id LIMIT 4 FORMAT JSON",
    "SELECT toString(Id) AS id, Title AS title, toString(Score) AS score, Tags AS tags, ContentLicense AS license FROM stackoverflow.posts WHERE Id IN (4,6) ORDER BY Id LIMIT 3 FORMAT JSON",
]

def unique(pairs):
    result = {}
    for key,value in pairs:
        if key in result: raise ValueError("Duplicate native property")
        result[key] = value
    return result

def validate(raw, stage):
    if len(raw)>262144: raise ValueError("Response exceeds256KiB")
    value=json.loads(raw,object_pairs_hook=unique)
    if not isinstance(value,dict) or "exception" in value or not isinstance(value.get("data"),list):
        raise ValueError("Successful native JSON envelope required")
    rows=value["data"]
    if any(not isinstance(row,dict) for row in rows): raise ValueError("Native object rows required")
    if type(value.get("rows")) is not int or value["rows"]!=len(rows):
        raise ValueError("Native row count mismatch")
    if stage==0:
        if len(rows)!=1 or set(rows[0])!={"version","user","readonly"} or rows[0]["user"]!="explorer" or rows[0]["readonly"] not in ("1","2") or not isinstance(rows[0]["version"],str):
            raise ValueError("Explicit public readonly user required")
    elif stage==1:
        expected={"Id","Title","PostTypeId","Score","Tags","ContentLicense"}
        if len(rows)!=6 or any(set(r)!={"name","type"} or not isinstance(r["name"],str) or not isinstance(r["type"],str) for r in rows) or {r["name"] for r in rows}!=expected:
            raise ValueError("Selected source schema missing/ambiguous")
    else:
        expected=["4","6","9"] if stage==2 else ["4","6"]
        if len(rows)!=len(expected) or [r.get("id") for r in rows]!=expected:
            raise ValueError("Exact original post selection required")
        for row in rows:
            if set(row)!={"id","title","score","tags","license"} or any(not isinstance(x,str) for x in row.values()):
                raise ValueError("Exact public scalar projection required")
            if not row["title"] or len(row["title"])>4096 or len(row["tags"])>4096 or len(row["license"])>128:
                raise ValueError("Bounded public fields required")
    return rows

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args,**kwargs): return None

def main():
    state=ROOT/"LocalState/COSMIC/ClickHousePublicQualificationCurrent"
    results=ROOT/"TestResults/ClickHousePublicSourceCurrent"
    state.mkdir(exist_ok=True);results.mkdir(exist_ok=True)
    with (state/"qualification.intent.json").open("x",encoding="utf-8") as stream:
        json.dump({"endpoint":BASE,"utc":datetime.datetime.now(datetime.timezone.utc).isoformat(),"queries":QUERIES,"nativeWrites":0,"maxResponseBytes":262144},stream,indent=2)
    observations=[];selected=[];passed=False
    try:
        for stage,sql in enumerate(QUERIES):
            request=urllib.request.Request(BASE,data=sql.encode(),method="POST",headers={"Content-Type":"text/plain; charset=utf-8"})
            try: response=urllib.request.build_opener(NoRedirect()).open(request,timeout=15)
            except urllib.error.HTTPError as error: response=error
            with response: raw=response.read(262145);status=response.status
            with (results/f"response-{stage+1}.json").open("xb") as stream:stream.write(raw)
            row={"stage":stage,"status":status,"bytes":len(raw),"sha256":hashlib.sha256(raw).hexdigest()};observations.append(row)
            if status!=200: raise ValueError("Native HTTP failure; remaining calls not sent")
            data=validate(raw,stage);selected.append(data);row["validatedRows"]=len(data)
        if selected[2][:2]!=selected[3]:raise ValueError("Same-source subset projection changed")
        passed=True
    except (urllib.error.URLError,TimeoutError,ValueError) as error:
        observations.append({"failureType":type(error).__name__,"reason":str(error)[:500]})
    report={"passed":passed,"observations":observations,"nativeWrites":0,"cqlAccepted":False}
    with (state/"qualification.json").open("x",encoding="utf-8") as stream:json.dump(report,stream,indent=2)
    print(json.dumps(report))
    return 0 if passed else 1

if __name__=="__main__":raise SystemExit(main())
