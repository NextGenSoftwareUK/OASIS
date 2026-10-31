"""Read-only live gate for response-owned routing; never infer from shared status.

Requires the existing acknowledged Solana demo cohort and deployed route metadata.
No accounts, saves, faults or settings changes. A completed gate is not replayed.
"""
from pathlib import Path
import importlib.util
import json

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


def main():
    state = ROOT / "LocalState/COSMIC/HostedQueryAcceptance-SolanaOASIS"
    report = ROOT / "TestResults/HostedCqlIntegrationCurrent/hyperdrive-load-balancing-http.json"
    acknowledgement = state / "hyperdrive-load-balancing.acknowledged.json"
    if acknowledgement.exists():
        raise RuntimeError("Completed gate retained; do not repeat a green scope.")
    token = flow.verification.protected_token((state / "owner.dpapi").read_bytes(), decrypt=True).decode()
    proof = json.loads((ROOT / "TestResults/HostedCqlIntegrationCurrent/SolanaOASIS-client-http-flow.json").read_text())
    expected = next(row for row in proof["mongoRows"] if row["id"] == proof["ids"][0])
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
