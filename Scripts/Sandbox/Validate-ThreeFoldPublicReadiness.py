"""Offline schema/projection validator for bounded public Grid Proxy records."""
from pathlib import Path
import argparse,json

def unique(pairs):
    result={}
    for key,value in pairs:
        if key in result:raise ValueError("Duplicate native property")
        result[key]=value
    return result

def project(item):
    if not isinstance(item,dict):raise ValueError("Native node object required")
    result={}
    for field in ("nodeId","farmId","gridVersion"):
        value=item.get(field)
        if type(value) is not int or not 0<=value<=4294967295 or field=="nodeId" and value==0:raise ValueError("Canonical UInt32 node fields required")
        result[field]=str(value)
    for field in ("country","city"):
        value=item.get(field)
        if not isinstance(value,str) or len(value)>256:raise ValueError("Bounded geographic strings required")
        result[field]=value
    result["policy"]="operator-reported-public-grid-node"
    result["chainStateVerified"]=False
    return result

def validate(raw,expected=None,single=False):
    if len(raw)>262144:raise ValueError("Response exceeds256KiB")
    value=json.loads(raw,object_pairs_hook=unique)
    rows=[value] if single else value
    if not isinstance(rows,list) or len(rows)!=(len(expected) if expected is not None else 3):raise ValueError("Exact bounded node selection required")
    mapped=[project(row) for row in rows];keys=[row["nodeId"] for row in mapped]
    if len(keys)!=len(set(keys)):raise ValueError("Duplicate node identity")
    if expected is not None:
        if keys!=expected:raise ValueError("Configured node identity differs")
    elif keys!=sorted(keys,key=int):raise ValueError("Bounded discovery must use ascending node IDs")
    return {"rows":mapped}

if __name__=="__main__":
    parser=argparse.ArgumentParser();parser.add_argument("path");parser.add_argument("--ids");parser.add_argument("--single",action="store_true");args=parser.parse_args()
    value=validate(Path(args.path).read_bytes(),args.ids.split(',') if args.ids else None,args.single)
    print(json.dumps(value))
