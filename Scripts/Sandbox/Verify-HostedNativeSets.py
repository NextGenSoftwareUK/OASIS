"""Paced read-only native-set acceptance after an exact Development rollout.
Creates one visitor session; never imports, saves or deletes a Holon.
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
    args = parser.parse_args()
    if any(not re.fullmatch("[0-9a-f]{40}", value) for value in (args.commit, args.website)):
        parser.error("Exact backend and website source commits are required.")
    delivery = json.loads((ROOT / "LocalState/COSMIC/native-set-image-handoff.json").read_text())
    if delivery["source"] != args.commit or delivery.get("packageAccepted") is not True:
        raise RuntimeError("Wait for exact immutable package acceptance before hosted requests.")
    helpers = runpy.run_path(str(ROOT / "Scripts/Sandbox/Verify-HostedCqlDemo.py"))
    credentials = runpy.run_path(str(ROOT / "Scripts/Sandbox/HostedSessionCredentials.py"))
    state = ROOT / "LocalState/COSMIC/HostedNativeSetCurrent"
    journal_path = state / "fixture.json"
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
        "calls":[],"pending":None,"providers":[],"holonWrites":0,"completed":False}
    write = lambda: helpers["write"](journal_path, journal)
    write()
    token = None
    cookie = None

    def call(path, body):
        if journal["pending"] is not None or len(journal["calls"]) >= 9:
            raise RuntimeError("Pending outcome or request cap; no retry.")
        if journal["calls"]:
            time.sleep(1)
        operation = str(uuid.uuid4())
        journal["pending"] = {"path":path,"operationId":operation}
        write()
        headers = {"Accept":"application/json","Content-Type":"application/json","Idempotency-Key":operation}
        if token:
            headers["Authorization"] = "Bearer "+token
        if cookie:
            headers["Cookie"] = cookie
        request = urllib.request.Request(BASE+path, data=json.dumps(body).encode(), headers=headers, method="POST")
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

    issued, cookies = call("sandbox/session", {})
    avatar = issued["result"]
    bundle = credentials["bundle"](str(uuid.UUID(avatar["id"])), avatar["jwtToken"], cookies)
    token, cookie = bundle["jwtToken"], bundle["cookieHeader"]
    with (state / "session.dpapi").open("xb") as stream:
        stream.write(helpers["protected_token"](json.dumps(bundle).encode()))
    for provider in ("MongoDBOASIS","SQLLiteDBOASIS","Neo4jOASIS"):
        found, _ = call("data/query-cql", {"provider":provider,"filters":[],"orderBy":"Id","skip":0,"take":1})
        rows = found["result"]["rows"]
        if len(rows) != 1:
            journal["providers"].append({"provider":provider,"accepted":False,"reason":"No visible existing row; no fixture seeded"})
            write()
            continue
        identity = str(uuid.UUID(rows[0]["id"]))
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
        journal["providers"].append({"provider":provider,"accepted":True,"rows":2,"sourceRowMutated":False})
        write()
    call("avatar/revoke-token", {})
    journal.update(sessionRevoked=True,completed=True,
        accepted=all(row["accepted"] for row in journal["providers"]))
    write()
    for name in ("issuance.dpapi","session.dpapi"):
        (state / name).unlink()
    print(json.dumps({"source":args.commit,"providers":journal["providers"],"calls":len(journal["calls"]),
        "holonWrites":0,"sessionRevoked":True,"credentialsRemoved":True}))

if __name__ == "__main__":
    main()
