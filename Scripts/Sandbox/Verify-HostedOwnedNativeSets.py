"""Paced owned-fixture native-set acceptance after an exact Development rollout.
One conditional private record per provider; exact acknowledged-revision cleanup.
"""
from pathlib import Path
import argparse
import json
import os
import re
import runpy
import subprocess
import time
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
BASE = "https://dev.api.web4.oasisomniverse.one/api/"
SITE = ROOT.parent / "Web4Site"

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--deployment", required=True, type=lambda value: str(uuid.UUID(value)))
    parser.add_argument("--commit", required=True)
    parser.add_argument("--website", required=True)
    parser.add_argument("--resume-before-first-save", action="store_true")
    parser.add_argument("--selected-columns", action="store_true")
    parser.add_argument("--text-coalesce", action="store_true")
    parser.add_argument("--providers", nargs="+", choices=["MongoDBOASIS","SQLLiteDBOASIS","Neo4jOASIS"],
        default=["MongoDBOASIS","SQLLiteDBOASIS","Neo4jOASIS"])
    parser.add_argument("--fixture-name", default="HostedOwnedProjectedSetCurrent")
    args = parser.parse_args()
    if any(not re.fullmatch("[0-9a-f]{40}", value) for value in (args.commit, args.website)):
        parser.error("Exact backend and website source commits are required.")
    if args.text_coalesce and not args.selected_columns:
        parser.error("Text coalescing uses selected-column qualification and readiness gating.")
    if len(set(args.providers)) != len(args.providers):
        parser.error("Choose each provider once.")
    if not args.selected_columns and args.providers != ["MongoDBOASIS","SQLLiteDBOASIS","Neo4jOASIS"]:
        parser.error("Subset qualification requires selected-column mode.")
    if not re.fullmatch(r"HostedOwnedProjectedSet[A-Za-z0-9]{1,48}", args.fixture_name):
        parser.error("Use a bounded selected-column fixture name without path separators.")
    if args.selected_columns and args.resume_before_first_save:
        parser.error("Selected-column checks use immutable checkpoints; reconcile an interrupted fixture explicitly.")
    delivery_name = ("text-coalesce-image-handoff.json" if args.text_coalesce else
        "projected-set-image-handoff.json" if args.selected_columns else "native-set-image-handoff.json")
    delivery = json.loads((ROOT / "LocalState/COSMIC" / delivery_name).read_text())
    if delivery["source"] != args.commit or delivery.get("packageAccepted") is not True:
        raise RuntimeError("Wait for exact immutable package acceptance before hosted requests.")
    helpers = runpy.run_path(str(ROOT / "Scripts/Sandbox/Verify-HostedCqlDemo.py"))
    credentials = runpy.run_path(str(ROOT / "Scripts/Sandbox/HostedSessionCredentials.py"))
    state = ROOT / "LocalState/COSMIC" / (
        args.fixture_name if args.selected_columns else "HostedOwnedNativeSetCurrent")
    if args.selected_columns and state.exists():
        raise RuntimeError("Preserve existing selected-column fixture checkpoints; do not issue another session.")
    journal_path = state / "fixture.json"
    if not args.resume_before_first_save:
        helpers["require_new_fixture"](journal_path)
    cfg = json.loads((Path.home() / ".railway/config.json").read_text())
    request = urllib.request.Request("https://backboard.railway.com/graphql/v2",
        data=json.dumps({"query":"query($id:String!){deployment(id:$id){id status meta}}",
            "variables":{"id":args.deployment}}).encode(),
        headers={"Content-Type":"application/json","Authorization":"Bearer "+cfg["user"]["token"],
            "User-Agent":"railway-cli/5.6.21"})
    with urllib.request.urlopen(request, timeout=30) as response:
        envelope = json.load(response)
    if envelope.get("errors"):
        raise RuntimeError("Deployment observation failed; no demo request sent.")
    deployed = envelope["data"]["deployment"]
    metadata = deployed["meta"]
    if isinstance(metadata, str):
        metadata = json.loads(metadata)
    if deployed["status"] != "SUCCESS" or metadata.get("commitHash") != args.commit:
        raise RuntimeError("Exact successful rollout required; no demo request sent.")
    compiler = subprocess.check_output(["git","show",args.website+":cql-text.js"], cwd=SITE).decode()
    state.mkdir(parents=True, exist_ok=True)
    journal = {"source":args.commit,"deployment":args.deployment,"website":args.website,
        "calls":[],"pending":None,"providers":[],"holonWrites":0,"owned":{},"completed":False}
    if args.resume_before_first_save:
        if journal_path.with_suffix(".json.new").exists():
            raise RuntimeError("Reconcile the original interrupted checkpoint before resume.")
        journal=json.loads(journal_path.read_text())
        own=journal.get("owned",{}).get("MongoDBOASIS",{})
        if (journal.get("source")!=args.commit or journal.get("deployment")!=args.deployment
                or journal.get("website")!=args.website or journal.get("pending") is not None
                or journal.get("completed") or journal.get("holonWrites")!=0
                or list(journal.get("owned",{}))!=["MongoDBOASIS"] or journal.get("providers")!=[]
                or len(journal.get("calls",[]))!=2
                or journal["calls"][0].get("path")!="sandbox/session"
                or journal["calls"][1].get("path")!="data/query-cql"
                or journal["calls"][1].get("provider")!="MongoDBOASIS"
                or any(c.get("status")!=200 or c.get("isError") for c in journal["calls"])
                or own.get("saveAcknowledged") or own.get("deleteAcknowledged")
                or not own.get("saveOperationId")):
            raise RuntimeError("Only the proven before-first-save checkpoint can resume.")
        uuid.UUID(own["id"]);uuid.UUID(own["saveOperationId"])
    checkpoint = 0
    def write():
        nonlocal checkpoint
        if not args.selected_columns:
            helpers["write"](journal_path, journal)
            return
        checkpoint += 1
        target = state / ("checkpoint-" + str(checkpoint).zfill(4) + ".json")
        with target.open("x", encoding="utf-8") as stream:
            json.dump(journal, stream, indent=2)
            stream.write("\n")
            stream.flush()
            os.fsync(stream.fileno())
        if journal.get("completed"):
            with journal_path.open("x", encoding="utf-8") as stream:
                json.dump(journal, stream, indent=2)
                stream.write("\n")
                stream.flush()
                os.fsync(stream.fileno())
    if not args.resume_before_first_save:
        write()
    token = None
    cookie = None

    def call(path, body, method="POST"):
        if journal["pending"] is not None or len(journal["calls"]) >= 18:
            raise RuntimeError("Pending outcome or request cap; no retry.")
        if journal["calls"]:
            time.sleep(1)
        operation = body.get("operationId") or str(uuid.uuid4())
        if path in {"data/save-holon","data/delete-holon"}:
            body={**body,"setGlobally":False,"autoReplicationMode":"OFF","autoFailOverMode":"OFF",
                "autoLoadBalanceMode":"OFF","showDetailedSettings":False}
        journal["pending"] = {"path":path,"method":method,"operationId":operation}
        write()
        headers = {"Accept":"application/json","Content-Type":"application/json","Idempotency-Key":operation}
        if token:
            headers["Authorization"] = "Bearer "+token
        if cookie:
            headers["Cookie"] = cookie
        request = urllib.request.Request(BASE+path, data=None if method == "GET" else json.dumps(body).encode(), headers=headers, method=method)
        try:
            response = urllib.request.urlopen(request, timeout=40)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            raw = response.read()
            status = response.status
            cookies = response.headers.get_all("Set-Cookie") or []
        if path == "sandbox/session":
            with (state / "issuance.dpapi").open("xb") as stream:
                stream.write(helpers["protected_token"](json.dumps(
                    {"status":status,"body":raw.decode(),"cookies":cookies}).encode()))
        value = helpers["unwrap"](json.loads(raw))
        outcome = {"path":path,"status":status,"isError":value.get("isError",False),"operationId":operation,
            "provider":body.get("provider")}
        code = value.get("errorCode")
        if isinstance(code,str) and re.fullmatch("(?:CQL|COSMIC)_[A-Z_]{2,80}",code):
            outcome["errorCode"] = code
        if status != 200 or value.get("isError"):
            # Preserve the original failed response before interpretation, without
            # leaking provider values or losing the one outcome needed to diagnose.
            with (state / ("failure-"+operation+".dpapi")).open("xb") as stream:
                stream.write(helpers["protected_token"](raw))
        journal["calls"].append(outcome)
        journal["pending"]["outcome"] = outcome
        write()
        if status != 200 or value.get("isError"):
            raise RuntimeError("Definitive response recorded; preserve session and journal, no retry.")
        journal["pending"] = None
        write()
        return value, cookies

    if args.resume_before_first_save:
        bundle=json.loads(helpers["protected_token"]((state/"session.dpapi").read_bytes(),decrypt=True))
        credentials["validate"](bundle,bundle["ownerId"])
        if credentials["needs_renewal"](bundle):
            raise RuntimeError("Original session needs explicit rotation; do not issue another avatar.")
        token,cookie=bundle["jwtToken"],bundle["cookieHeader"]
    else:
        issued, cookies = call("sandbox/session", {})
        avatar = issued["result"]
        bundle = credentials["bundle"](str(uuid.UUID(avatar["id"])), avatar["jwtToken"], cookies)
        token, cookie = bundle["jwtToken"], bundle["cookieHeader"]
        with (state / "session.dpapi").open("xb") as stream:
            stream.write(helpers["protected_token"](json.dumps(bundle).encode()))
    if args.selected_columns:
        catalogue,_=call("data/query-cql/providers?examplesVersion=2",{},method="GET")
        descriptions=catalogue.get("result")
        if not isinstance(descriptions,list):
            raise RuntimeError("Provider discovery did not publish an explicit source list; no fixture save sent.")
        readiness=[]
        for provider in args.providers:
            matches=[row for row in descriptions if isinstance(row,dict) and row.get("provider")==provider]
            if len(matches)!=1:
                raise RuntimeError("Require one unambiguous source description per requested provider.")
            source=matches[0]
            readiness.append({key:source.get(key) for key in ("provider","available","errorCode","activationGeneration","activationHostInstance")})
        journal["readiness"]=readiness
        write()
        if any(row["available"] is not True or row["errorCode"] is not None for row in readiness):
            raise RuntimeError("Requested CQL source startup is incomplete; no fixture save sent.")
    for provider in args.providers:
        resuming=bool(args.resume_before_first_save and provider=="MongoDBOASIS")
        if resuming:
            identity=journal["owned"][provider]["id"]
        else:
            identity=str(uuid.uuid4())
            journal["owned"][provider]={"id":identity,"saveAcknowledged":False,"deleteAcknowledged":False}
            write()
        lookup={"provider":provider,"filters":[{"field":"Id","scalar":"Guid","operator":"Equal","value":identity}],
            "orderBy":"Id","take":1}
        if not resuming:
            absent,_=call("data/query-cql",lookup)
            if absent["result"].get("rows")!=[]:
                raise RuntimeError("New identity already exists; no save sent.")
        save_operation=journal["owned"][provider].get("saveOperationId") or str(uuid.uuid4())
        journal["owned"][provider]["saveOperationId"]=save_operation
        write()
        saved,_=call("data/save-holon",{"providerType":provider,"operationId":save_operation,
            "expectedRevisions":{identity:None},"saveChildren":False,"recursive":False,
            "holon":{"id":identity,"name":"COSMIC owned set acceptance "+identity,
                "isNewHolon":True,"isPublic":False,"isActive":True,
                "metaData":{"COSMIC.Demo.Dataset":"COSMIC.CQL.TextCoalesce.v1" if args.text_coalesce else "COSMIC.CQL.NativeSet.v1",
                    **({"coalescedText":None} if args.text_coalesce else {})}}})
        holon=saved.get("result") or {}
        if (saved.get("isSaved") is not True or str(holon.get("id","")).lower()!=identity
                or str(holon.get("createdByAvatarId","")).lower()!=bundle["ownerId"].lower()
                or holon.get("isPublic") is not False):
            raise RuntimeError("Save acknowledgement or ownership incomplete; preserve original fixture.")
        revisions=(holon.get("providerMetaData") or {}).get(provider) or {}
        revision=next((v for k,v in revisions.items() if k.lower()=="cosmic.providerrevisionid"),None)
        if not revision:
            raise RuntimeError("Acknowledged native revision missing; preserve original fixture.")
        revision=str(uuid.UUID(revision))
        journal["owned"][provider].update(saveAcknowledged=True,revision=revision)
        journal["holonWrites"]+=1
        write()
        selection = "Name AS Label" if args.selected_columns else "*"
        sql = "(SELECT "+selection+" FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1) UNION ALL (SELECT "+selection+" FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1)"
        parameters={"id":identity}
        if args.text_coalesce:
            parameters["replacement"]="COSMIC replacement "+identity
            sql="SELECT COALESCE(CAST(metadata.coalescedText AS STRING), @replacement) AS Label FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1"
        program = "const vm=require('node:vm');const s={module:{exports:{}}};vm.runInNewContext("+json.dumps(compiler)+",s);process.stdout.write(JSON.stringify(s.module.exports.compile("+json.dumps(sql)+",{},"+json.dumps(parameters)+")));"
        result = subprocess.run(["node","-"], input=program, capture_output=True, text=True, check=True)
        compiled = json.loads(result.stdout)
        if args.text_coalesce:
            if (compiled["path"]!="data/query-cql/projection" or len(compiled["body"].get("columns",[]))!=1
                    or compiled["body"]["columns"][0].get("function")!="TextCoalesce"
                    or compiled["body"]["columns"][0].get("nullReplacement")!=parameters["replacement"]):
                raise RuntimeError("Require the explicit typed native text-coalescing contract before dispatch.")
        elif compiled["path"] != "data/query-cql/set" or compiled["body"]["kind"] != "Concat":
            raise RuntimeError("Website compiler did not produce the expected native set contract.")
        actual, _ = call(compiled["path"], compiled["body"])
        result = actual["result"]
        if result.get("provider") != provider or not args.text_coalesce and result.get("kind") != "Concat":
            raise RuntimeError("Native result lacks exact provenance.")
        if args.text_coalesce:
            columns=result.get("columns") or []
            if (result.get("version")!="cql-projection-v1" or len(columns)!=1
                    or columns[0].get("alias")!="Label" or result.get("rows")!=[[parameters["replacement"]]]):
                raise RuntimeError("Native text coalescing did not return the exact expected explicit replacement.")
        elif args.selected_columns:
            expected = "COSMIC owned set acceptance " + identity
            columns = result.get("columns") or []
            if (result.get("version") != "cql-projected-set-v1" or len(columns) != 1
                    or columns[0].get("alias") != "Label" or result.get("rows") != [[expected],[expected]]):
                raise RuntimeError("Selected-value set must return exactly the two explicit cells, without hidden fields.")
        elif [str(uuid.UUID(row["id"])) for row in result["rows"]] != [identity,identity]:
            raise RuntimeError("Native entity set occurrence multiplicity differs.")
        journal["owned"][provider]["sqlCoalesceAccepted" if args.text_coalesce else "sqlSetAccepted"]=True
        delete_operation=str(uuid.uuid4())
        journal["owned"][provider]["deleteOperationId"]=delete_operation
        write()
        call("data/delete-holon",{"id":identity,"providerType":provider,"softDelete":False,
            "operationId":delete_operation,"expectedRevisions":{identity:revision},
            "recursive":False,"loadChildren":False},method="DELETE")
        journal["owned"][provider]["deleteAcknowledged"]=True
        write()
        absent,_=call("data/query-cql",lookup)
        if absent["result"].get("rows")!=[]:
            raise RuntimeError("Exact owned identity absence not verified.")
        journal["owned"][provider]["absenceVerified"]=True
        journal["providers"].append({"provider":provider,"accepted":True,"rows":1 if args.text_coalesce else 2,"exactCleanupVerified":True})
        write()
    call("avatar/revoke-token", {})
    journal.update(sessionRevoked=True,selectedColumns=args.selected_columns,textCoalesce=args.text_coalesce,completed=True,
        accepted=all(row["accepted"] for row in journal["providers"]))
    write()
    for name in ("issuance.dpapi","session.dpapi"):
        (state / name).unlink()
    print(json.dumps({"source":args.commit,"providers":journal["providers"],"calls":len(journal["calls"]),
        "holonWrites":journal["holonWrites"],"selectedColumns":args.selected_columns,"textCoalesce":args.text_coalesce,"sessionRevoked":True,"credentialsRemoved":True}))

if __name__ == "__main__":
    main()
