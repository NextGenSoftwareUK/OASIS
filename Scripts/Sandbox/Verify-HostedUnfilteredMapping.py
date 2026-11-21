"""One owned Development virtual mapping; bounded calls, sealed session, no write replay."""
from pathlib import Path
import argparse
import importlib.util
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
STATE_ROOT = ROOT / "LocalState/COSMIC/HostedUnfilteredMappingCurrent"
BASE = "https://dev.api.web4.oasisomniverse.one/api/"
WEBSITE = "3d8f108f6161b8b4a0e6c8e2f80d1358fb6185f8"
helpers = runpy.run_path(str(ROOT / "Scripts/Sandbox/Verify-HostedCqlDemo.py"))
credentials = runpy.run_path(str(ROOT / "Scripts/Sandbox/HostedSessionCredentials.py"))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--deployment", required=True, type=lambda value: str(uuid.UUID(value)))
    parser.add_argument("--commit", required=True)
    parser.add_argument("--provider", required=True, choices=["MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS"])
    parser.add_argument("--snapshot", action="store_true",
        help="Verify unfiltered original-field snapshot preparation and mixed identity reconciliation.")
    args = parser.parse_args()
    provider = args.provider
    if args.snapshot:
        snapshot_proof = json.loads((ROOT / "Docs/WIP/COSMIC ORM/UNFILTERED_SNAPSHOT_SOURCE_2026-11-21.json").read_text(encoding="utf-8-sig"))
        if (not snapshot_proof.get("nativeAccepted") or snapshot_proof.get("deliverySuperseded")
                or args.commit != snapshot_proof.get("deliveryCandidate")):
            parser.error("Wait for the qualified exact snapshot delivery candidate before any hosted requests.")
    state = (ROOT / "LocalState/COSMIC/HostedUnfilteredSnapshotCurrent" if args.snapshot else STATE_ROOT) / provider
    for proof_file in (ROOT / "Docs/WIP/COSMIC ORM").glob("CQL_UNFILTERED_SNAPSHOT*HOSTED_ACCEPTANCE*.json" if args.snapshot else "CQL_UNFILTERED*HOSTED_ACCEPTANCE*.json"):
        proof = json.loads(proof_file.read_text(encoding="utf-8-sig"))
        if proof.get("provider") == provider and proof.get("queryAccepted") is True:
            raise RuntimeError("This provider case is already accepted; select only remaining cases.")
    if not re.fullmatch("[0-9a-f]{40}", args.commit):
        parser.error("Use the exact verified source SHA.")
    journal_path = state / "fixture.json"
    helpers["require_new_fixture"](journal_path)
    # Verify rollout before creating an account or a data fixture.
    cfg = json.loads((Path.home() / ".railway/config.json").read_text())
    query = {"query": "query($id:String!){deployment(id:$id){id status meta}}",
             "variables": {"id": args.deployment}}
    request = urllib.request.Request("https://backboard.railway.com/graphql/v2",
        data=json.dumps(query).encode(), headers={"Content-Type": "application/json",
        "Authorization": "Bearer " + cfg["user"]["token"], "User-Agent": "railway-cli/5.6.21"})
    with urllib.request.urlopen(request, timeout=30) as response:
        envelope = json.load(response)
    if envelope.get("errors"):
        raise RuntimeError("Rollout preflight failed; no demo request sent.")
    deployment = envelope["data"]["deployment"]
    metadata = deployment["meta"]
    if isinstance(metadata, str):
        metadata = json.loads(metadata)
    if deployment["status"] != "SUCCESS" or metadata.get("commitHash") != args.commit:
        raise RuntimeError("Wait for the exact successful rollout; no demo request sent.")
    state.mkdir(parents=True, exist_ok=True)
    mapping_id = str(uuid.uuid4())
    journal = {"schema": 1, "deployment": args.deployment, "source": args.commit,
               "websiteCompiler": WEBSITE, "snapshotCase": args.snapshot, "provider": provider, "mappingId": mapping_id, "ownerId": None,
               "calls": [], "pending": None, "completed": False}
    write = lambda: helpers["write"](journal_path, journal)
    write()
    token = None
    cookie_header = None

    def call(path, method="POST", body=None, mutation=False):
        if journal["pending"] is not None or len(journal["calls"]) >= 16:
            raise RuntimeError("Original pending outcome or request cap prevents another call.")
        if journal["calls"]:
            time.sleep(1)
        if path in {"data/save-holon", "data/delete-holon"}:
            body = {**body, "providerType": body.get("providerType") or "MongoDBOASIS", "setGlobally": False,
                    "autoReplicationMode": "OFF", "autoFailOverMode": "OFF",
                    "autoLoadBalanceMode": "OFF", "showDetailedSettings": False}
        key = (body or {}).get("operationId") or str(uuid.uuid4())
        journal["pending"] = {"path": path, "method": method, "operationId": key,
                              "mutation": mutation}
        write()
        headers = {"Accept": "application/json", "Content-Type": "application/json",
                   "Idempotency-Key": key}
        if token:
            headers["Authorization"] = "Bearer " + token
        if cookie_header:
            headers["Cookie"] = cookie_header
        request = urllib.request.Request(BASE + path, method=method, headers=headers,
            data=json.dumps(body).encode() if body is not None else None)
        try:
            response = urllib.request.urlopen(request, timeout=40)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            raw = response.read()
            status = response.status
            cookies = response.headers.get_all("Set-Cookie") or []
        if path == "sandbox/session":
            # Preserve the original authentication outcome encrypted before parsing.
            protected = helpers["protected_token"](json.dumps({
                "status": status, "body": raw.decode(), "cookies": cookies}).encode())
            with (state / "issuance.dpapi").open("xb") as stream:
                stream.write(protected)
        value = helpers["unwrap"](json.loads(raw))
        outcome = {"path": path, "method": method, "status": status,
                   "isError": value.get("isError", False), "operationId": key}
        if isinstance(value.get("errorCode"), str):
            outcome["errorCode"] = value["errorCode"]
        journal["calls"].append(outcome)
        journal["pending"]["outcome"] = outcome
        write()
        if status != 200 or value.get("isError"):
            raise RuntimeError("Definitive API refusal recorded; preserve original fixture, no retry.")
        journal["pending"] = None
        write()
        return value, cookies

    sql = ("SELECT Id, CAST(MetaData.name AS STRING) AS CustomerName FROM " + provider + " "
           "WITH VIRTUAL MAPPING '" + mapping_id + "' ORDER BY Id LIMIT 10")
    compiler = subprocess.check_output(["git", "show", WEBSITE + ":cql-text.js"],
                                      cwd=ROOT.parent / "Web4Site")
    program = ("const vm=require('node:vm');const s={module:{exports:{}}};"
        "vm.runInNewContext(" + json.dumps(compiler.decode()) + ",s);"
        "process.stdout.write(JSON.stringify(s.module.exports.compile(" + json.dumps(sql) + ")));")
    compiled = subprocess.run([r"C:/Program Files/nodejs/node.exe", "-"],
        input=program, text=True, capture_output=True, check=True)
    query = json.loads(compiled.stdout)
    if (query["path"] != "data/query-cql/projection/mixed"
            or query["body"]["virtualMappingId"] != mapping_id
            or query["body"].get("filters") != []):
        raise RuntimeError("The exact SQL compiler did not emit the intended bounded unfiltered mapping.")

    issued, cookies = call("sandbox/session", body={}, mutation=True)
    avatar = issued["result"]
    owner = str(uuid.UUID(avatar["id"]))
    token = avatar["jwtToken"]
    bundle = credentials["bundle"](owner, token, cookies)
    cookie_header = bundle["cookieHeader"]
    with (state / "session.dpapi").open("xb") as stream:
        stream.write(helpers["protected_token"](json.dumps(bundle).encode()))
    journal["ownerId"] = owner
    write()
    sources, _ = call("native-data/" + provider + "/sources", method="GET")
    expected = {"MongoDBOASIS": "customers", "SQLLiteDBOASIS": "suppliers"}.get(provider)
    choices = [row for row in sources["result"]
               if (expected is None or row["name"] == expected)
               and (row.get("estimatedRecordCount") or 0) > 0]
    if len(choices) != 1:
        raise RuntimeError("One existing approved native demo collection is required; no mapping save sent.")
    source_name = choices[0]["name"]
    journal["nativeSource"] = source_name
    write()
    prepared, _ = call("native-data/" + provider + "/virtual-mappings/prepare", body={
        "id": mapping_id, "sourceName": source_name,
        "connectionName": "oasis-sandbox-native-fixture", "fields": {"name": "name"}})
    definition = prepared["result"]
    if (definition["id"].lower() != mapping_id or definition.get("isPublic") is not False
            or definition.get("createdByAvatarId", "").lower() != owner):
        raise RuntimeError("Prepared mapping ownership or identity differs; no save sent.")
    def observed_revision(holon, authority):
        fields = (holon.get("providerMetaData") or {}).get(authority) or {}
        revision = next((v for k, v in fields.items()
                         if k.lower() == "cosmic.providerrevisionid"), None)
        if not revision:
            raise RuntimeError("Acknowledged revision missing; preserve exact fixture for reconciliation.")
        return str(uuid.UUID(revision))

    def verify_absent(identity, authority):
        absent, _ = call("data/query-cql", body={"provider": authority, "take": 1,
            "orderBy": "Id", "filters": [{"field": "Id", "scalar": "Guid",
                                         "operator": "Equal", "value": identity}]})
        if absent["result"].get("rows") != []:
            raise RuntimeError("Exact owned identity absence was not verified.")

    save_id = str(uuid.uuid4())
    journal["saveOperationId"] = save_id
    write()
    saved, _ = call("data/save-holon", body={"holon": definition,
        "operationId": save_id, "expectedRevisions": {mapping_id: None},
        "saveChildren": False, "recursive": False}, mutation=True)
    holon = saved.get("result") or {}
    if saved.get("isSaved") is not True or holon.get("id", "").lower() != mapping_id:
        raise RuntimeError("Save acknowledgement incomplete; preserve fixture, never repeat.")
    revision = observed_revision(holon, "MongoDBOASIS")
    journal["mappingRevision"] = revision
    journal["mappingSaved"] = True
    write()
    result, _ = call(query["path"], body=query["body"])
    payload = result["result"]
    rows = payload.get("rows")
    if not isinstance(rows, list) or len(rows) < 2:
        raise RuntimeError("Expected populated mixed native/stored rows; preserve fixture for diagnosis.")
    journal["unfilteredSqlCompiled"] = True
    journal["mixedRowsReturned"] = len(rows)
    journal["queryAccepted"] = True
    write()
    if args.snapshot:
        provenance = payload.get("provenance")
        if not isinstance(provenance, list) or len(provenance) != len(rows):
            raise RuntimeError("Complete row provenance is required before snapshot preparation.")
        selected = next((i for i, item in enumerate(provenance)
                         if item.get("representation") == "Virtual"
                         and item.get("alsoMaterialized") is False), None)
        if selected is None:
            raise RuntimeError("An unmaterialized native row is required; no snapshot save sent.")
        snapshot_id = str(uuid.UUID(rows[selected][0]))
        original_value = rows[selected][1]
        journal["snapshotId"] = snapshot_id
        journal["snapshotOriginalValue"] = original_value
        write()
        prepared_snapshot, _ = call(
            "native-data/" + provider + "/virtual-mappings/" + mapping_id + "/snapshot/prepare",
            body={"rowId": snapshot_id, "query": {"filters": [], "select": ["name"], "take": 200}})
        snapshot = prepared_snapshot.get("result") or {}
        if (snapshot.get("id", "").lower() != snapshot_id or snapshot.get("isPublic") is not False
                or snapshot.get("createdByAvatarId", "").lower() != owner
                or (snapshot.get("metaData") or {}).get("name") != original_value):
            raise RuntimeError("Original private snapshot fields or identity differ; no snapshot save sent.")
        snapshot_operation = str(uuid.uuid4())
        journal["snapshotSaveOperationId"] = snapshot_operation
        write()
        cached, _ = call("data/save-holon", body={"holon": snapshot,
            "providerType": provider, "operationId": snapshot_operation,
            "expectedRevisions": {snapshot_id: None}, "saveChildren": False, "recursive": False},
            mutation=True)
        cached_holon = cached.get("result") or {}
        if cached.get("isSaved") is not True or cached_holon.get("id", "").lower() != snapshot_id:
            raise RuntimeError("Snapshot save acknowledgement incomplete; never replay.")
        snapshot_revision = observed_revision(cached_holon, provider)
        journal["snapshotRevision"] = snapshot_revision
        journal["snapshotSaved"] = True
        write()
        repeated, _ = call(query["path"], body=query["body"])
        repeated_payload = repeated["result"]
        repeated_rows = repeated_payload.get("rows")
        repeated_sources = repeated_payload.get("provenance")
        if (not isinstance(repeated_rows, list) or not isinstance(repeated_sources, list)
                or len(repeated_rows) != len(rows) or len(repeated_sources) != len(repeated_rows)):
            raise RuntimeError("A stored snapshot changed the complete logical result cardinality.")
        matches = [i for i, row in enumerate(repeated_rows)
                   if str(row[0]).lower() == snapshot_id]
        if (len(matches) != 1 or repeated_rows[matches[0]][1] != original_value
                or repeated_sources[matches[0]].get("representation") != "Virtual"
                or repeated_sources[matches[0]].get("alsoMaterialized") is not True):
            raise RuntimeError("The stored snapshot did not retain one native-winning logical identity.")
        journal["snapshotIdentityReconciliationVerified"] = True
        write()
        snapshot_delete = str(uuid.uuid4())
        journal["snapshotDeleteOperationId"] = snapshot_delete
        write()
        call("data/delete-holon", method="DELETE", body={
            "id": snapshot_id, "providerType": provider, "softDelete": False,
            "operationId": snapshot_delete, "expectedRevisions": {snapshot_id: snapshot_revision},
            "recursive": False, "loadChildren": False}, mutation=True)
        verify_absent(snapshot_id, provider)
        journal["exactSnapshotCleanupVerified"] = True
        write()
    delete_id = str(uuid.uuid4())
    journal["deleteOperationId"] = delete_id
    write()
    deleted, _ = call("data/delete-holon", method="DELETE", body={
        "id": mapping_id, "providerType": "MongoDBOASIS", "softDelete": False,
        "operationId": delete_id, "expectedRevisions": {mapping_id: revision},
        "recursive": False, "loadChildren": False}, mutation=True)
    # Delete has its own endpoint acknowledgement; Save.IsSaved is not its contract.
    # The successful envelope above must still be followed by exact physical absence.
    verify_absent(mapping_id, "MongoDBOASIS")
    journal["exactMappingCleanupVerified"] = True
    write()
    call("avatar/revoke-token", body={}, mutation=True)
    journal["sessionRevoked"] = True
    journal["completed"] = True
    write()
    # Retain compact non-secret evidence; remove owned sealed credentials after revocation.
    for name in ("issuance.dpapi", "session.dpapi"):
        (state / name).unlink()
    print(json.dumps({"source": args.commit, "provider": provider, "mappingId": mapping_id,
        "calls": len(journal["calls"]), "mixedRowsReturned": len(rows),
        "exactMappingCleanupVerified": True, "sessionRevoked": True,
        "browserVerified": False, "nativeDataImported": False,
        "snapshotIdentityReconciliationVerified": journal.get("snapshotIdentityReconciliationVerified", False),
        "exactSnapshotCleanupVerified": journal.get("exactSnapshotCleanupVerified", False)}))


if __name__ == "__main__":
    main()
