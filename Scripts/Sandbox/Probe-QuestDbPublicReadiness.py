"""Qualify one canonical read-only QuestDB demo route; never retries/falls back."""
from pathlib import Path
import datetime,hashlib,json,urllib.request,urllib.error,urllib.parse
ROOT=Path(__file__).resolve().parents[2]
BASE="https://demo.questdb.io/api/v1/sql/execute"
KEYS=["BTC-USDT","ETH-USDT","SOL-USDT"]
QUERIES=["SELECT build() AS build", "SELECT DISTINCT symbol FROM trades WHERE symbol IN ('BTC-USDT','ETH-USDT','SOL-USDT') ORDER BY symbol LIMIT 4", "SELECT DISTINCT symbol FROM trades WHERE symbol IN ('BTC-USDT','ETH-USDT') ORDER BY symbol LIMIT 3"]
def unique(pairs):
    value={}
    for key,item in pairs:
        if key in value:raise ValueError("Duplicate native property")
        value[key]=item
    return value

def validate(raw,stage):
    if len(raw)>262144:raise ValueError("Native response exceeds256KiB")
    value=json.loads(raw,object_pairs_hook=unique)
    if not isinstance(value,dict) or "error" in value or value.get("query")!=QUERIES[stage]:raise ValueError("Exact successful native SQL envelope required")
    rows=value.get("dataset");count=value.get("count")
    if not isinstance(rows,list) or type(count) is not int or count!=len(rows) or any(not isinstance(row,list) or len(row)!=1 or not isinstance(row[0],str) for row in rows):raise ValueError("Complete typed native rows required")
    if stage==0:
        if value.get("columns")!=[{"name":"build","type":"STRING"}] or len(rows)!=1 or "QuestDB" not in rows[0][0] or len(rows[0][0])>4096:raise ValueError("Native QuestDB build identity required")
        return {"build":rows[0][0],"policy":"operator-reported-questdb-build"}
    expected=KEYS if stage==1 else KEYS[:2]
    if value.get("columns")!=[{"name":"symbol","type":"SYMBOL"}] or [row[0] for row in rows]!=expected:raise ValueError("Exact unique native symbol selection required")
    return [{"symbol":row[0],"table":"trades","policy":"public-demo-symbol-metadata"} for row in rows]
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args,**kwargs):return None

def main():
    state=ROOT/"LocalState/COSMIC/QuestDbPublicQualificationCurrent";out=ROOT/"TestResults/QuestDbPublicSourceCurrent";out.mkdir(exist_ok=True)
    with (state/"qualification.intent.json").open("x",encoding="utf-8") as f:json.dump({"utc":datetime.datetime.now(datetime.timezone.utc).isoformat(),"endpoint":BASE,"queries":QUERIES,"nativeWrites":0,"maximumRequests":3,"maximumResponseBytes":262144},f,indent=2)
    observations=[];projected=[];passed=False
    try:
        for stage,sql in enumerate(QUERIES):
            url=BASE+"?"+urllib.parse.urlencode({"query":sql,"count":"true"})
            request=urllib.request.Request(url,headers={"Accept":"application/json"})
            try:response=urllib.request.build_opener(NoRedirect()).open(request,timeout=15)
            except urllib.error.HTTPError as error:response=error
            with response:raw=response.read(262145);status=response.status
            with (out/f"response-{stage+1}.json").open("xb") as f:f.write(raw)
            item={"stage":stage,"status":status,"bytes":len(raw),"sha256":hashlib.sha256(raw).hexdigest()};observations.append(item)
            if status!=200:raise ValueError("Native HTTP failure; remaining queries not sent")
            projected.append(validate(raw,stage))
        if projected[1][:2]!=projected[2]:raise ValueError("Symbol subset differs")
        passed=True
    except (urllib.error.URLError,TimeoutError,ValueError,RecursionError) as error:observations.append({"errorType":type(error).__name__,"reason":str(error)[:500]})
    result={"passed":passed,"observations":observations,"projected":projected,"nativeWrites":0,"cqlAccepted":False}
    with (state/"qualification.json").open("x",encoding="utf-8") as f:json.dump(result,f,indent=2)
    print(json.dumps(result));return 0 if passed else 1
if __name__=="__main__":raise SystemExit(main())
