"""One paced native HoloOASIS readiness check after a verified Development rollout.
No Holon imports, saves, deletes, recovery operations or conductor resets.
"""
import argparse
import json
import os
from pathlib import Path
import re
import runpy
import time
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
BASE = "https://dev.api.web4.oasisomniverse.one/api/"

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--delivery", type=Path, default=ROOT / "LocalState/COSMIC/holo-readiness-480-startup-preserved.json")
    parser.add_argument("--state", type=Path, default=ROOT / "LocalState/COSMIC/HostedHolo480SupportedCurrent")
    parser.add_argument("--example-id", default="public-subset")
    args = parser.parse_args()
    state = args.state.resolve()
    if state.parent != (ROOT / "LocalState/COSMIC").resolve() or not state.name.startswith("HostedHolo"):
        raise RuntimeError("Use one owned Holo readiness journal under LocalState/COSMIC.")
    delivery = json.loads(args.delivery.read_text(encoding="utf-8-sig"))
    verified = [row for row in delivery.get("startupDnaChecks", []) if row.get("verified") and row["source"] == delivery["source"]]
    web4 = [row for row in verified if row["service"] == "WEB4 OASIS API"]
    if (not delivery.get("packageAccepted") or not delivery.get("developmentPushed")
            or len(verified) != 7 or len(web4) != 1 or state.exists()):
        raise RuntimeError("Require exact package, all seven running revisions and one fresh owned journal; never replay.")
    helpers = runpy.run_path(str(ROOT / "Scripts/Sandbox/Verify-HostedCqlDemo.py"))
    credentials = runpy.run_path(str(ROOT / "Scripts/Sandbox/HostedSessionCredentials.py"))
    state.mkdir()
    def write_new(path, value):
        with path.open("x", encoding="utf-8") as stream:
            json.dump(value, stream, indent=2)
            stream.write("\n")
            stream.flush()
            os.fsync(stream.fileno())
    token = cookie = None
    calls = []
    def call(path, body=None):
        nonlocal token, cookie
        if len(calls) >= 4 or any(not item.with_name(item.name.replace(".intent.", ".acknowledged.")).exists()
                                  for item in state.glob("*.intent.json")):
            raise RuntimeError("A prior outcome is unknown or the four-call cap is exhausted; no retry.")
        if calls:
            time.sleep(1)
        number = len(calls) + 1
        operation = str(uuid.uuid4())
        intent = state / (str(number) + ".intent.json")
        write_new(intent, {"path":path, "method":"GET" if body is None else "POST", "operationId":operation})
        headers = {"Accept":"application/json", "Idempotency-Key":operation, "User-Agent":"COSMIC-Holo-readiness"}
        if token:
            headers["Authorization"] = "Bearer " + token
        if cookie:
            headers["Cookie"] = cookie
        if body is not None:
            headers["Content-Type"] = "application/json"
        request = urllib.request.Request(BASE + path, headers=headers,
            data=None if body is None else json.dumps(body).encode(), method="GET" if body is None else "POST")
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
                    {"status":status, "body":raw.decode(), "cookies":cookies}).encode()))
        value = helpers["unwrap"](json.loads(raw))
        outcome = {"path":path, "status":status, "isError":bool(value.get("isError")),
            "isWarning":bool(value.get("isWarning")), "operationId":operation}
        code = value.get("errorCode")
        if isinstance(code, str) and re.fullmatch("[A-Z][A-Z0-9_]{1,96}", code):
            outcome["errorCode"] = code
        write_new(state / (str(number) + ".acknowledged.json"), outcome)
        calls.append(outcome)
        if status != 200 or outcome["isError"] or outcome["isWarning"]:
            with (state / ("failure-" + str(number) + ".dpapi")).open("xb") as stream:
                stream.write(helpers["protected_token"](raw))
            raise RuntimeError("Definitive unsuccessful response recorded; no retry.")
        return value, cookies
    proof = {"provider":"HoloOASIS", "source":delivery["source"], "deployment":web4[0]["deployment"],
        "timeoutSeconds":delivery["timeoutSeconds"], "available":False, "queryAccepted":False,
        "holonWrites":0, "piperDataTouched":False, "sessionRevoked":False,
        "intermittentJoinRootCauseResolved":False}
    failure = None
    try:
        issued, cookies = call("sandbox/session", {})
        avatar = issued["result"]
        bundle = credentials["bundle"](str(uuid.UUID(avatar["id"])), avatar["jwtToken"], cookies)
        token, cookie = bundle["jwtToken"], bundle["cookieHeader"]
        with (state / "session.dpapi").open("xb") as stream:
            stream.write(helpers["protected_token"](json.dumps(bundle).encode()))
        catalogue, _ = call("data/query-cql/providers?examplesVersion=2")
        sources = []
        def collect(value):
            if isinstance(value, dict):
                if value.get("provider") == "HoloOASIS" and "activationHostInstance" in value:
                    sources.append(value)
                for nested in value.values():
                    collect(nested)
            elif isinstance(value, list):
                for nested in value:
                    collect(nested)
        collect(catalogue["result"])
        if len(sources) != 1:
            raise RuntimeError("The current typed Holo source description must be unique.")
        source = sources[0]
        proof.update(available=source["available"], activationGeneration=source["activationGeneration"],
            activationHostInstance=source["activationHostInstance"], errorCode=source.get("errorCode"))
        if source["available"] is not True or source.get("errorCode") is not None:
            raise RuntimeError("Current Holo source is not ready; preserve evidence without recovery or reset.")
        examples = [example for example in source.get("examples", []) if example.get("id") == args.example_id
            and example.get("operation") == "Rows" and example.get("available") is True]
        if len(examples) != 1 or examples[0]["request"].get("provider") != "HoloOASIS":
            raise RuntimeError("Require one advertised, available Holo row example owned by this provider.")
        query = examples[0]["request"]
        if query.get("graph") is not None or not 1 <= query.get("take", 0) <= 25:
            raise RuntimeError("The advertised Holo example must remain bounded and graph-free.")
        proof["exampleId"] = args.example_id
        actual, _ = call("data/query-cql", query)
        result = actual["result"]
        rows = result["rows"]
        if result["provider"] != "HoloOASIS" or len(rows) != 2 or len({str(uuid.UUID(row["id"])) for row in rows}) != 2:
            raise RuntimeError("Native Holo public subset lacks its expected distinct rows or provenance.")
        proof.update(queryAccepted=True, rows=len(rows), shippingHoloProvider=True)
    except Exception as error:
        failure = type(error).__name__
    finally:
        unknown = any(not item.with_name(item.name.replace(".intent.", ".acknowledged.")).exists()
                      for item in state.glob("*.intent.json"))
        if token and not unknown:
            try:
                call("avatar/revoke-token", {})
                proof["sessionRevoked"] = True
            except Exception as error:
                failure = failure or type(error).__name__
        if proof["sessionRevoked"]:
            for name in ("issuance.dpapi", "session.dpapi"):
                path = state / name
                if path.exists():
                    path.unlink()
            proof["protectedCredentialsRemoved"] = True
        proof.update(calls=len(calls), failureKind=failure, requiresReconciliation=unknown,
            verificationScope="One exact Development startup and authenticated native public subset query; no general uptime claim.")
        write_new(state / "outcome.json", proof)
    print(json.dumps(proof))
    if failure or not proof["queryAccepted"] or not proof["sessionRevoked"]:
        raise RuntimeError("Readiness acceptance incomplete; reconcile the original owned journal, never replay.")

if __name__ == "__main__":
    main()
