"""Verify denied recovery access once; preserve each outcome without native activation."""
from pathlib import Path
import hashlib
import importlib.util
import json
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / "LocalState/COSMIC/HostedSourceRecoveryReleaseCurrent"
OBSERVATION = ROOT / "TestResults/CqlSourceRecoveryCurrent/deployment-observed.json"


def write_new(path, value):
    with path.open("x", encoding="utf-8") as stream:
        json.dump(value, stream, indent=2)


def main():
    intent = json.loads((STATE / "code-publication.intent.json").read_text())
    deployment = json.loads((STATE / "code-publication.acknowledged.json").read_text())
    observed = json.loads(OBSERVATION.read_text())
    if (observed.get("status") != "SUCCESS" or observed.get("commit") != intent["commit"]
            or observed.get("id") != deployment["id"]):
        raise RuntimeError("Observe the exact existing deployment before authorization checks.")
    spec = importlib.util.spec_from_file_location("query", Path(__file__).with_name("Verify-HostedQuerySource.py"))
    query = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(query)
    _, token = query.session(ROOT / "LocalState/COSMIC/HostedQueryAcceptance-ThreeFoldOASIS",
                             "owner", allow_create=False)
    results = []
    for label, bearer, expected in (("anonymous", None, 401), ("demo-user", token, 403)):
        original = STATE / ("authorization-" + label + ".intent.json")
        outcome = STATE / ("authorization-" + label + ".acknowledged.json")
        if original.exists() or outcome.exists():
            raise RuntimeError("Original authorization dispatch exists; reconcile without replay.")
        # Invalid host identity cannot activate even if an auth regression lets
        # the request reach the recovery service. No real source state is changed.
        body = {"operationId": str(uuid.uuid4()), "expectedGeneration": 0,
                "hostInstanceId": str(uuid.UUID(int=0))}
        route = "admin/cql/sources/NostrOASIS/recover"
        write_new(original, {"route": route, "body": body, "expectedStatus": expected,
                             "commit": intent["commit"], "deployment": observed["id"]})
        headers = {"Content-Type": "application/json", "Accept": "application/json",
                   "Origin": query.BROWSER_ORIGIN}
        if bearer:
            headers["Authorization"] = "Bearer " + bearer
        request = urllib.request.Request(query.BASE + route, method="POST", headers=headers,
                                         data=json.dumps(body).encode())
        try:
            response = urllib.request.build_opener(query.NoRedirect()).open(request, timeout=45)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            raw = response.read(8193)
            # Preserve receipt before parsing/asserting. Never retain credentials,
            # request headers or arbitrary server error text in evidence.
            record = {"status": response.status, "responseBytes": len(raw),
                      "responseSha256": hashlib.sha256(raw).hexdigest(), "expectedStatus": expected,
                      "bodyTruncated": len(raw) > 8192, "nativeActivationRequested": False}
            write_new(outcome, record)
        if record["bodyTruncated"] or record["status"] != expected:
            raise RuntimeError("Authorization mismatch; original outcome retained, no replay.")
        results.append({"caller": label, **record})
    summary = {"commit": intent["commit"], "deployment": observed["id"], "checks": results,
               "privilegedRecoveryVerified": False, "nativeQueries": 0, "privateSaves": 0}
    write_new(STATE / "authorization-verified.json", summary)
    print(json.dumps(summary))


if __name__ == "__main__":
    main()
