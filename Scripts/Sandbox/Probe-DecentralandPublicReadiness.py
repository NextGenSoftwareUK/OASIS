"""Original bounded Catalyst readiness/scene reads. Never downloads referenced assets."""
from pathlib import Path
import datetime,hashlib,json,re,urllib.request,urllib.error
ROOT=Path(__file__).resolve().parents[2]
BASE="https://peer.decentraland.org"
POINTERS=["0,0","-53,71","-100,-50"]
def unique(pairs):
    result={}
    for key,value in pairs:
        if key in result:raise ValueError("Duplicate native property")
        result[key]=value
    return result

def parse(raw):
    if len(raw)>524288:raise ValueError("Response exceeds512KiB")
    return json.loads(raw,object_pairs_hook=unique)

def about(raw):
    root=parse(raw)
    if not isinstance(root,dict) or root.get("healthy") is not True:raise ValueError("Healthy Catalyst required")
    content=root.get("content");config=root.get("configurations")
    if not isinstance(content,dict) or content.get("healthy") is not True or not isinstance(config,dict) or type(config.get("networkId")) is not int or config["networkId"]!=1:
        raise ValueError("Healthy reported mainnet content required")
    return {"networkId":1,"healthy":True,"identityPolicy":"operator-reported-catalyst-mainnet"}

def scenes(raw,pointers):
    rows=parse(raw)
    if not isinstance(rows,list) or len(rows)!=len(pointers):raise ValueError("Exact distinct scene selection required")
    found={};ids=set()
    for row in rows:
        if not isinstance(row,dict) or row.get("type")!="scene" or row.get("version")!="v3":raise ValueError("v3 scene manifests required")
        identity=row.get("id")
        if not isinstance(identity,str) or not re.fullmatch(r"(?:b[a-z2-7]{20,120}|Qm[1-9A-HJ-NP-Za-km-z]{44})",identity) or identity in ids:raise ValueError("Distinct bounded reported entity IDs required")
        ids.add(identity)
        keys=row.get("pointers")
        if not isinstance(keys,list) or not keys or len(keys)>4096 or any(not isinstance(key,str) or not re.fullmatch(r"-?(?:0|[1-9][0-9]{0,2}),-?(?:0|[1-9][0-9]{0,2})",key) for key in keys) or len(keys)!=len(set(keys)):
            raise ValueError("Unique bounded scene parcel pointers required")
        selected=[key for key in keys if key in pointers]
        if len(selected)!=1 or selected[0] in found:raise ValueError("Missing/foreign/shared selected scene")
        timestamp=row.get("timestamp")
        if type(timestamp) is not int or not 0<=timestamp<=9223372036854775807:raise ValueError("Exact nonnegative Int64 timestamp required")
        metadata=row.get("metadata")
        if not isinstance(metadata,dict) or not isinstance(metadata.get("display"),dict) or not isinstance(metadata.get("scene"),dict):raise ValueError("Exact public scene metadata required")
        title=metadata["display"].get("title");parcels=metadata["scene"].get("parcels")
        if not isinstance(title,str) or not title or len(title)>1024 or not isinstance(parcels,list) or any(not isinstance(key,str) for key in parcels) or len(parcels)!=len(set(parcels)) or set(parcels)!=set(keys):raise ValueError("Consistent public title/parcel projection required")
        found[selected[0]]={"pointer":selected[0],"entityId":identity,"title":title,"timestamp":str(timestamp),"parcelCount":str(len(keys)),"cidIntegrityVerified":False,"policy":"operator-reported-active-scene"}
    if set(found)!=set(pointers):raise ValueError("Complete pointer coverage required")
    return [found[key] for key in pointers]

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args,**kwargs):return None

def main():
    state=ROOT/"LocalState/COSMIC/DecentralandPublicQualificationCurrent";out=ROOT/"TestResults/DecentralandPublicSourceCurrent";out.mkdir(exist_ok=True)
    with (state/"qualification.intent.json").open("x",encoding="utf-8") as f:json.dump({"utc":datetime.datetime.now(datetime.timezone.utc).isoformat(),"base":BASE,"pointers":POINTERS,"maximumRequests":3,"maxResponseBytes":524288,"nativeWrites":0,"assetsDownloaded":0},f,indent=2)
    calls=[("GET","/about",None),("POST","/content/entities/active",{"pointers":POINTERS}),("POST","/content/entities/active",{"pointers":POINTERS[:2]})]
    observations=[];projected=[];passed=False
    try:
        for index,(method,path,body) in enumerate(calls):
            request=urllib.request.Request(BASE+path,method=method,data=json.dumps(body).encode() if body else None,headers={"Content-Type":"application/json"} if body else {})
            try:response=urllib.request.build_opener(NoRedirect()).open(request,timeout=15)
            except urllib.error.HTTPError as error:response=error
            with response:raw=response.read(524289);status=response.status
            with (out/f"response-{index+1}.json").open("xb") as f:f.write(raw)
            item={"method":method,"path":path,"status":status,"bytes":len(raw),"sha256":hashlib.sha256(raw).hexdigest()};observations.append(item)
            if status!=200:raise ValueError("Native HTTP failure; remaining requests not sent")
            data=about(raw) if index==0 else scenes(raw,POINTERS if index==1 else POINTERS[:2]);projected.append(data)
        if projected[1][:2]!=projected[2]:raise ValueError("Active pointer observation changed between selections")
        passed=True
    except (urllib.error.URLError,TimeoutError,ValueError,RecursionError) as error:observations.append({"errorType":type(error).__name__,"reason":str(error)[:500]})
    result={"passed":passed,"observations":observations,"projected":projected,"nativeWrites":0,"assetsDownloaded":0,"cqlAccepted":False}
    with (state/"qualification.json").open("x",encoding="utf-8") as f:json.dump(result,f,indent=2)
    print(json.dumps(result));return 0 if passed else 1
if __name__=="__main__":raise SystemExit(main())
