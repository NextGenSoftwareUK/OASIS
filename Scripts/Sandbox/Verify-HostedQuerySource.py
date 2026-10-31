"""Explicit normal-visitor HTTP acceptance. No uncertain mutation is retried.

Creates at most two journalled demo sessions for this provider cohort, protects
their bearers with existing Windows DPAPI, and reuses acknowledged sessions.
"""
from pathlib import Path
import argparse
import base64
import datetime
import importlib.util
import json
import os
import subprocess
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
BASE = "https://dev.api.web4.oasisomniverse.one/api/"
spec = importlib.util.spec_from_file_location("verification", ROOT / "Scripts/Sandbox/Verify-HostedCqlDemo.py")
verification = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verification)


def write_new(path, data):
    with path.open("xb") as stream:
        stream.write(data)
        stream.flush()
        os.fsync(stream.fileno())


def call(route, token=None, body=None, method="POST", operation=None):
    headers = {"Accept": "application/json", "Idempotency-Key": operation or str(uuid.uuid4())}
    if token:
        headers["Authorization"] = "Bearer " + token
    if body is not None:
        headers["Content-Type"] = "application/json"
    request = urllib.request.Request(BASE + route, method=method, headers=headers,
                                    data=json.dumps(body).encode() if body is not None else None)
    try:
        response = urllib.request.urlopen(request, timeout=45)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        raw = response.read(2 * 1024 * 1024 + 1)
        if len(raw) > 2 * 1024 * 1024:
            raise RuntimeError("HTTP proof exceeds its response budget.")
        if not raw:
            raise RuntimeError("HTTP acceptance failed with an empty response: " + str(response.status))
        data = json.loads(raw)
        if isinstance(data, dict):
            data = verification.unwrap(data)
        if response.status != 200 or isinstance(data, dict) and data.get("isError"):
            code = data.get("errorCode") or data.get("code") if isinstance(data, dict) else None
            raise RuntimeError("HTTP acceptance failed: " + str(response.status) + "/" + str(code))
        return data


def session(state, label):
    ack = state / (label + ".acknowledged.json")
    token_file = state / (label + ".dpapi")
    intent = state / (label + ".intent.json")
    if ack.exists():
        token = verification.protected_token(token_file.read_bytes(), decrypt=True).decode()
        claims = json.loads(base64.urlsafe_b64decode(token.split('.')[1] + '==='))
        if claims["exp"] <= datetime.datetime.now(datetime.timezone.utc).timestamp():
            raise RuntimeError("Acknowledged session expired; no automatic replacement. Preserve its cohort and explicitly plan a fresh acceptance cohort.")
        return json.loads(ack.read_text()), token
    if intent.exists() or token_file.exists():
        raise RuntimeError("Unacknowledged session creation; reconcile, never create another avatar.")
    operation = str(uuid.uuid4())
    write_new(intent, json.dumps({"route": "sandbox/session", "operationId": operation, "label": label}).encode())
    result = call("sandbox/session", operation=operation)
    identity = verification.acknowledged_identity("sandbox/session", result)
    avatar = result["result"]
    write_new(token_file, verification.protected_token(avatar["jwtToken"].encode()))
    recorded = {"id": identity, "username": avatar["username"], "label": label, "operationId": operation}
    write_new(ack, json.dumps(recorded).encode())
    return recorded, avatar["jwtToken"]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("provider", choices=["SOLIDOASIS", "AWSS3OASIS"])
    parser.add_argument("--prepare", action="store_true")
    parser.add_argument("--cohort", choices=["current", "fixed-client"], default="current",
                        help="An explicitly planned fresh test identity after retaining a failed/expired cohort; never automatic.")
    options = parser.parse_args()
    suffix = options.provider + ("" if options.cohort == "current" else "-" + options.cohort)
    if options.prepare:
        frontend = ROOT.parent / "Web4Site"
        if not all((frontend / name).is_file() for name in ("cql-demo-preparation.js", "cql-playground.js")):
            raise RuntimeError("Existing published frontend modules are required before creating any test session.")
    state = ROOT / "LocalState/COSMIC" / ("HostedQueryAcceptance-" + suffix)
    state.mkdir(exist_ok=True)
    results = ROOT / "TestResults/HostedCqlIntegrationCurrent"
    avatar, token = session(state, "owner")
    discovery = call("data/query-cql/providers?examplesVersion=2", token, method="GET")
    source = next(row for row in discovery["result"] if row["provider"] == options.provider)
    if not source["available"] or not source["federationAvailable"]:
        raise RuntimeError("Hosted native source is not ready; no private snapshot was created.")
    examples = source["examples"]
    leaf = [next(row for row in examples if row["id"] == key) for key in ("public-records", "public-subset")]
    native = [call("data/query-cql", token, row["request"]) for row in leaf]
    for response, count in zip(native, (3, 2)):
        if len(response["result"]["rows"]) != count:
            raise RuntimeError("Native source selection count differs.")
    verification.write(results / (suffix + "-native-http.json"),
                       {"avatar": avatar, "source": source, "native": native, "browserAcceptance": False})
    if options.prepare:
        report = results / (suffix + "-client-http-flow.json")
        if report.exists():
            raise RuntimeError("Client-flow proof already exists; review the original outcome rather than resaving.")
        payload = {"base": BASE, "token": token, "ownerId": avatar["id"], "state": str(state), "report": str(report), "source": source,
                   "preparationModule": str(ROOT.parent / "Web4Site/cql-demo-preparation.js"),
                   "pairedThree": next(row for row in examples if row["id"] == "mongo-public-records"),
                   "pairedTwo": next(row for row in examples if row["id"] == "mongo-public-subset")}
        run = subprocess.run(["node", str(ROOT / "Scripts/Sandbox/Verify-HostedSourceFlow.cjs")],
                             input=json.dumps(payload).encode(), capture_output=True)
        if run.returncode:
            raise RuntimeError("Client-flow failed; inspect preserved preparation intents. " + run.stderr.decode()[:500])
        print(run.stdout.decode().strip())
        proof = json.loads(report.read_text())
        foreign, foreign_token = session(state, "foreign")
        public = call("data/query-cql", foreign_token, leaf[0]["request"])
        private = call("data/query-cql", foreign_token, {"provider": "MongoDBOASIS", "orderBy": "Id", "take": 2,
                       "filters": [{"field": "Id", "scalar": "Guid", "operator": "In", "value": proof["ids"]}]})
        if len(public["result"]["rows"]) != 3 or private["result"]["rows"]:
            raise RuntimeError("Independent visitor visibility differs.")
        verification.write(results / (suffix + "-isolation-http.json"),
                           {"avatar": foreign, "public": public, "foreignPrivateIds": private, "browserAcceptance": False})
    print(json.dumps({"provider": options.provider, "nativeRows": [3, 2], "browserAcceptance": False}))


if __name__ == "__main__":
    main()
