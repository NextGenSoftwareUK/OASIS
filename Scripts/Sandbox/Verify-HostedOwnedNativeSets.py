"""Paced owned-fixture native-set acceptance after an exact Development rollout.
One conditional private record per provider; exact acknowledged-revision cleanup.
"""
from pathlib import Path
import argparse
import json
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
    args = parser.parse_args()
    if any(not re.fullmatch("[0-9a-f]{40}", value) for value in (args.commit, args.website)):
        parser.error("Exact backend and website source commits are required.")
    delivery = json.loads((ROOT / "LocalState/COSMIC/native-set-image-handoff.json").read_text())
    if delivery["source"] != args.commit or delivery.get("packageAccepted") is not True:
        raise RuntimeError("Wait for exact immutable package acceptance before hosted requests.")
    helpers = runpy.run_path(str(ROOT / "Scripts/Sandbox/Verify-HostedCqlDemo.py"))
    credentials = runpy.run_path(str(ROOT / "Scripts/Sandbox/HostedSessionCredentials.py"))
    state = ROOT / "LocalState/COSMIC/HostedOwnedNativeSetCurrent"
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
    write = lambda: helpers["write"](journal_path, journal)
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
        request = urllib.request.Request(BASE+path, data=json.dumps(body).encode(), headers=headers, method=method)
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
    for provider in ("MongoDBOASIS","SQLLiteDBOASIS","Neo4jOASIS"):
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
                "metaData":{"COSMIC.Demo.Dataset":"COSMIC.CQL.NativeSet.v1"}}})
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
        sql = "(SELECT * FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1) UNION ALL (SELECT * FROM "+provider+" WHERE Id = @id ORDER BY Id LIMIT 1)"
        program = "const vm=require('node:vm');const s={module:{exports:{}}};vm.runInNewContext("+json.dumps(compiler)+",s);process.stdout.write(JSON.stringify(s.module.exports.compile("+json.dumps(sql)+",{},"+json.dumps({"id":identity})+")));"
        result = subprocess.run(["node","-"], input=program, capture_output=True, text=True, check=True)
        compiled = json.loads(result.stdout)
        if compiled["path"] != "data/query-cql/set" or compiled["body"]["kind"] != "Concat":
            raise RuntimeError("Website compiler did not produce the expected native set contract.")
        actual, _ = call(compiled["path"], compiled["body"])
        result = actual["result"]
        if (result.get("provider") != provider or result.get("kind") != "Concat"
                or [str(uuid.UUID(row["id"])) for row in result["rows"]] != [identity,identity]):
            raise RuntimeError("Native set result lacks exact provenance or occurrence multiplicity.")
        journal["owned"][provider]["sqlSetAccepted"]=True
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
        journal["providers"].append({"provider":provider,"accepted":True,"rows":2,"exactCleanupVerified":True})
        write()
    call("avatar/revoke-token", {})
    journal.update(sessionRevoked=True,completed=True,
        accepted=all(row["accepted"] for row in journal["providers"]))
    write()
    for name in ("issuance.dpapi","session.dpapi"):
        (state / name).unlink()
    print(json.dumps({"source":args.commit,"providers":journal["providers"],"calls":len(journal["calls"]),
        "holonWrites":journal["holonWrites"],"sessionRevoked":True,"credentialsRemoved":True}))

if __name__ == "__main__":
    main()
