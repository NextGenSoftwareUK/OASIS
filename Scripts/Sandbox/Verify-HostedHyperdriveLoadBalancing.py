"""Read-only live gate for response-owned routing; never infer from shared status.

Requires an explicitly selected acknowledged cohort and deployed route metadata.
Durable expected-record evidence lives beside the protected session, not TestResults.
No accounts, saves, faults or settings changes. A completed gate is not replayed.
"""
from pathlib import Path
import importlib.util
import json
import argparse
import uuid

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("flow", ROOT / "Scripts/Sandbox/Verify-HostedQuerySource.py")
flow = importlib.util.module_from_spec(spec)
spec.loader.exec_module(flow)
PROVIDERS = {"MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS"}


def verify_response(response, expected, requested):
    row = response.get("result") or {}
    if response.get("isError") or any(row.get(key) != expected[key] for key in ("id", "name", "version", "createdByAvatarId")):
        raise ValueError("Load did not return the acknowledged identity/content/version/owner.")
    encoded = response.get("metaData", {}).get("hyperDriveRoute")
    if not isinstance(encoded, str):
        raise ValueError("Deployed per-response route evidence is required; shared status is not a substitute.")
    route = json.loads(encoded)
    if (route.get("RequestedProvider") != requested or route.get("InitialProvider") not in PROVIDERS or
            route.get("ServingProvider") not in PROVIDERS or route.get("Succeeded") is not True or
            route.get("RequestedStrategy") != "Auto"):
        raise ValueError("Response-owned route does not match this request and enrolled sources.")
    if requested != "Default" and (route["InitialProvider"] != requested or route["ServingProvider"] != requested):
        raise ValueError("An explicit provider read selected a different provider.")
    return route



def reviewed_record(document, owner):
    """Validate retained evidence before any request; never derive a new save."""
    if document.get("schema") != 1:
        raise ValueError("Unsupported routing fixture schema.")
    expected = document.get("expected")
    digest = document.get("sourceEvidenceSha256")
    if (not isinstance(expected, dict) or set(expected) != {"id", "name", "version", "createdByAvatarId"}
            or not isinstance(digest, str) or len(digest) != 64
            or any(c not in "0123456789abcdefABCDEF" for c in digest)
            or type(expected.get("version")) is not int or expected["version"] < 1
            or not isinstance(expected.get("name"), str) or not expected["name"]
            or expected.get("createdByAvatarId") != owner):
        raise ValueError("Routing fixture must bind the acknowledged record, source hash and session owner.")
    for value in (expected["id"], owner):
        if not isinstance(value, str) or str(uuid.UUID(value)) != value or uuid.UUID(value).int == 0:
            raise ValueError("Routing fixture requires canonical nonempty identities.")
    return dict(expected)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cohort", choices=["original-solana", "routing-v1"], default="original-solana")
    options = parser.parse_args()
    state = ROOT / "LocalState/COSMIC" / ("HostedQueryAcceptance-SolanaOASIS"
        if options.cohort == "original-solana" else "HostedHyperdriveRoutingCurrent")
    report = ROOT / "TestResults/HostedCqlIntegrationCurrent" / ("hyperdrive-load-balancing-http.json"
        if options.cohort == "original-solana" else "hyperdrive-load-balancing-routing-v1-http.json")
    acknowledgement = state / "hyperdrive-load-balancing.acknowledged.json"
    if acknowledgement.exists():
        raise RuntimeError("Completed gate retained; do not repeat a green scope.")
    # This verifier owns reads only. Missing or expired sessions must never cause
    # flow.session to create a replacement or recycle an old operation identity.
    if not (state / "owner.acknowledged.json").is_file() or not (state / "owner.dpapi").is_file():
        raise RuntimeError("Prepare and acknowledge the selected cohort separately; no session created by a read gate.")
    owner = json.loads((state / "owner.acknowledged.json").read_text())["id"]
    expected = reviewed_record(json.loads((state / "routing-record.acknowledged.json").read_text()), owner)
    identity, token = flow.session(state, "owner", allow_create=False)
    if identity["id"] != owner:
        raise RuntimeError("Acknowledged owner changed while loading the fixture.")
    if flow.call("sandbox/faults", token, method="GET")["result"]:
        raise RuntimeError("Read gate requires no active avatar fault; it does not restore another fault.")
    observed = []
    # Explicit provider reads prove precedence with balancing disabled. Default
    # reads enable actual selection. No synthetic timings or global last-state used.
    for requested in [*sorted(PROVIDERS), *("Default" for _ in range(6))]:
        response = flow.call("data/load-holon", token, {"id": expected["id"], "providerType": requested,
            "autoLoadBalanceMode": "ON" if requested == "Default" else "OFF",
            "autoFailOverMode": "OFF", "loadChildren": False})
        route = verify_response(response, expected, requested)
        observed.append({"requested": requested, "route": route, "id": response["result"]["id"]})
        flow.verification.write(report, {"reads": observed, "newSaves": 0, "browserAcceptance": False})
    selected = {row["route"]["ServingProvider"] for row in observed if row["requested"] == "Default"}
    if len(selected) < 2:
        raise RuntimeError("Automatic reads did not demonstrate provider variation; preserve observations for policy investigation.")
    flow.write_new(acknowledgement, json.dumps({"reads": len(observed), "automaticProviders": sorted(selected), "newSaves": 0}).encode())
    print(json.dumps({"reads": len(observed), "automaticProviders": sorted(selected), "newSaves": 0, "browserAcceptance": False}))


if __name__ == "__main__":
    main()
