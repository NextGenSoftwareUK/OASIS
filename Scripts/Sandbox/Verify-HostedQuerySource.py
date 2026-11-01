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

import HostedSessionCredentials as session_credentials

ROOT = Path(__file__).resolve().parents[2]
BASE = "https://dev.api.web4.oasisomniverse.one/api/"
BROWSER_ORIGIN = "https://web4.oasisomniverse.one"
spec = importlib.util.spec_from_file_location("verification", ROOT / "Scripts/Sandbox/Verify-HostedCqlDemo.py")
verification = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verification)


def write_new(path, data):
    with path.open("xb") as stream:
        stream.write(data)
        stream.flush()
        os.fsync(stream.fileno())


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, url):
        return None


def call(route, token=None, body=None, method="POST", operation=None, *, cookie_header=None, capture_auth=None):
    headers = {"Accept": "application/json", "Origin": BROWSER_ORIGIN, "Idempotency-Key": operation or str(uuid.uuid4())}
    if token:
        headers["Authorization"] = "Bearer " + token
    if cookie_header:
        headers["Cookie"] = cookie_header
    if body is not None:
        headers["Content-Type"] = "application/json"
    request = urllib.request.Request(BASE + route, method=method, headers=headers,
                                    data=json.dumps(body).encode() if body is not None else None)
    try:
        response = urllib.request.build_opener(NoRedirect()).open(request, timeout=45)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        raw = response.read(2 * 1024 * 1024 + 1)
        if capture_auth is not None:
            capture_auth(response.status, raw, response.headers.get_all("Set-Cookie") or [])
        if len(raw) > 2 * 1024 * 1024:
            raise RuntimeError("HTTP proof exceeds its response budget.")
        if (response.headers.get("Access-Control-Allow-Origin") != BROWSER_ORIGIN
                or response.headers.get("Access-Control-Allow-Credentials") != "true"):
            raise RuntimeError("The exact hosted browser credential origin was not acknowledged.")
        if not raw:
            raise RuntimeError("HTTP acceptance failed with an empty response: " + str(response.status))
        data = json.loads(raw)
        if isinstance(data, dict):
            data = verification.unwrap(data)
        if response.status != 200 or isinstance(data, dict) and data.get("isError"):
            code = data.get("errorCode") or data.get("code") if isinstance(data, dict) else None
            raise RuntimeError("HTTP acceptance failed: " + str(response.status) + "/" + str(code))
        return data


def session(state, label, *, allow_create=True):
    ack = state / (label + ".acknowledged.json")
    token_file = state / (label + ".dpapi")
    intent = state / (label + ".intent.json")
    session_file = state / (label + ".session.dpapi")
    if ack.exists():
        acknowledged = json.loads(ack.read_text())
        if acknowledged.get("credentialsSchema") == 1:
            session_credentials.no_pending_rotation(state, label)
            credential = session_credentials.validate(json.loads(verification.protected_token(
                session_file.read_bytes(), decrypt=True)), acknowledged["id"])
            if session_credentials.claims(credential["jwtToken"])["exp"] <= datetime.datetime.now(datetime.timezone.utc).timestamp():
                credential = session_credentials.rotate(state, label, credential, call, verification.protected_token, write_new)
            return acknowledged, credential["jwtToken"]
        # Retained unversioned cohorts own only their original JWT. Never guess a
        # refresh format or create a replacement identity for an expired legacy fixture.
        token = verification.protected_token(token_file.read_bytes(), decrypt=True).decode()
        claims = session_credentials.claims(token)
        if claims["exp"] <= datetime.datetime.now(datetime.timezone.utc).timestamp():
            raise RuntimeError("Acknowledged legacy session expired without retained cookie credentials; reconcile the same cohort explicitly.")
        return acknowledged, token
    if not allow_create:
        raise RuntimeError("Selected read-only cohort has no acknowledged session; no replacement created.")
    if intent.exists() or token_file.exists() or session_file.exists():
        raise RuntimeError("Unacknowledged session creation; reconcile, never create another avatar.")
    operation = str(uuid.uuid4())
    write_new(intent, json.dumps({"route": "sandbox/session", "operationId": operation, "label": label}).encode())
    captured = []
    def capture(status, raw, cookies):
        write_new(state / (label + ".auth-response.dpapi"), verification.protected_token(json.dumps({
            "status": status, "bodyBase64": base64.b64encode(raw).decode(), "cookies": cookies}).encode()))
        captured.append((status, cookies))
    result = call("sandbox/session", operation=operation, capture_auth=capture)
    identity = verification.acknowledged_identity("sandbox/session", result)
    avatar = result["result"]
    if len(captured) != 1 or captured[0][0] != 200:
        raise RuntimeError("Session cookie issuance outcome is incomplete; no replacement avatar.")
    credential = session_credentials.bundle(identity, avatar["jwtToken"], captured[0][1])
    write_new(session_file, verification.protected_token(json.dumps(credential).encode()))
    recorded = {"id": identity, "username": avatar["username"], "label": label, "operationId": operation, "credentialsSchema": 1}
    write_new(ack, json.dumps(recorded).encode())
    return recorded, avatar["jwtToken"]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("provider", choices=["SOLIDOASIS", "AWSS3OASIS", "SolanaOASIS", "GoogleCloudOASIS", "EthereumOASIS", "PolygonOASIS", "ArbitrumOASIS", "CosmosBlockChainOASIS", "PolkadotOASIS", "NEAROASIS", "BaseOASIS", "AvalancheOASIS", "BNBChainOASIS", "OptimismOASIS", "LineaOASIS", "ScrollOASIS", "ZkSyncOASIS", "AbstractOASIS", "MonadOASIS", "SeiOASIS", "BerachainOASIS", "BitcoinOASIS", "XRPLOASIS", "StellarOASIS", "AlgorandOASIS", "AptosOASIS", "HashgraphOASIS", "SuiOASIS", "ElrondOASIS", "RadixOASIS", "StarknetOASIS", "CardanoOASIS", "ArweaveOASIS"])
    parser.add_argument("--prepare", action="store_true")
    parser.add_argument("--cohort", choices=["current", "fixed-client", "post-startup-fix"], default="current",
                        help="An explicitly planned fresh test identity after retaining a failed/expired cohort; never automatic.")
    parser.add_argument("--continue-settled-read", action="store_true",
                        help="Explicitly finish the original qualified cohort; preserve prior proofs and forbid saves.")
    options = parser.parse_args()
    if options.continue_settled_read and not options.prepare:
        parser.error("--continue-settled-read requires --prepare for the original client-flow gate")
    suffix = options.provider + ("" if options.cohort == "current" else "-" + options.cohort)
    if options.prepare:
        frontend = ROOT.parent / "Web4Site"
        if not all((frontend / name).is_file() for name in ("cql-demo-preparation.js", "cql-playground.js")):
            raise RuntimeError("Existing published frontend modules are required before creating any test session.")
    state = ROOT / "LocalState/COSMIC" / ("HostedQueryAcceptance-" + suffix)
    if options.continue_settled_read and not (state / "settlement-continuation/continuation-source.json").is_file():
        raise RuntimeError("Original durable continuation manifest required; no session or replacement created.")
    if (state / "cql-http-acceptance.acknowledged.json").exists():
        raise RuntimeError("This cohort already completed acceptance; retain its acknowledged outcome.")
    if not options.continue_settled_read and (ROOT / "TestResults/HostedCqlIntegrationCurrent" / (suffix + "-client-http-flow.json.progress.json")).exists():
        raise RuntimeError("Original partial client flow exists; reconcile explicitly before another execution.")
    state.mkdir(exist_ok=True)
    results = ROOT / "TestResults/HostedCqlIntegrationCurrent"
    avatar, token = session(state, "owner", allow_create=not options.continue_settled_read)
    discovery = call("data/query-cql/providers?examplesVersion=2", token, method="GET")
    source = next(row for row in discovery["result"] if row["provider"] == options.provider)
    if not source["available"] or not source["federationAvailable"]:
        raise RuntimeError("Hosted native source is not ready; no private snapshot was created.")
    examples = source["examples"]
    leaf = [next(row for row in examples if row["id"] == key) for key in ("public-records", "public-subset")]
    if options.continue_settled_read:
        original = json.loads((state / "settlement-continuation/original-native-http.json").read_text(encoding="utf-8-sig"))
        if original["avatar"]["id"] != avatar["id"] or original["source"]["provider"] != options.provider:
            raise RuntimeError("Original native proof belongs to a different cohort.")
        if [len(row["result"]["rows"]) for row in original["native"]] != [3, 2]:
            raise RuntimeError("Original native selection proof differs.")
    else:
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
        if options.continue_settled_read:
            payload["resumeManifest"] = str(state / "settlement-continuation/continuation-source.json")
        elif Path(str(report) + ".progress.json").exists():
            raise RuntimeError("An original partial flow exists; reconcile explicitly instead of rerunning preparation.")
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
    print(json.dumps({"provider": options.provider, "nativeRows": [3, 2], "nativeProofReused": options.continue_settled_read, "browserAcceptance": False}))


if __name__ == "__main__":
    main()
