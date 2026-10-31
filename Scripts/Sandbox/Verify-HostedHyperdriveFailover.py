"""Bounded avatar-only fault/read gate using acknowledged Solana demo snapshots.

No new accounts, saves, routing changes or source replay. Each fault has a durable
intent and is restored in finally. Existing journals require review, not replay.
"""
from pathlib import Path
import importlib.util
import json
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("flow", ROOT / "Scripts/Sandbox/Verify-HostedQuerySource.py")
flow = importlib.util.module_from_spec(spec)
spec.loader.exec_module(flow)
PROVIDERS = ["MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS"]


def raw_request(token, route, body):
    request = urllib.request.Request(flow.BASE + route, method="POST",
        data=json.dumps(body).encode(), headers={"Content-Type": "application/json", "Accept": "application/json",
            "Authorization": "Bearer " + token, "Idempotency-Key": body.get("operationId") or str(uuid.uuid4())})
    try:
        response = urllib.request.urlopen(request, timeout=30)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        raw = response.read(1024 * 1024 + 1)
        if len(raw) > 1024 * 1024:
            raise RuntimeError("Load response exceeds evidence budget.")
        return response.status, flow.verification.unwrap(json.loads(raw))



def verify_failover_response(response, expected, requested):
    """Require this response to prove a successful move away from the faulted source."""
    row = response.get("result") or {}
    if response.get("isError") or any(row.get(key) != value for key, value in expected.items()):
        raise ValueError("Failover did not preserve the acknowledged record identity/content/version/owner.")
    encoded = response.get("metaData", {}).get("hyperDriveRoute")
    if not isinstance(encoded, str):
        raise ValueError("Failover requires per-response route evidence.")
    route = json.loads(encoded)
    if (requested not in PROVIDERS or route.get("RequestedProvider") != requested
            or route.get("InitialProvider") != requested
            or route.get("ServingProvider") not in PROVIDERS
            or route.get("ServingProvider") == requested
            or route.get("RequestedStrategy") != "Auto"
            or route.get("Succeeded") is not True):
        raise ValueError("Response does not prove failover from the faulted provider to an enrolled replica.")
    return route


def main():
    state = ROOT / "LocalState/COSMIC/HostedQueryAcceptance-SolanaOASIS"
    journal = state / "hyperdrive-failover.intent.json"
    if journal.exists():
        raise RuntimeError("Retained fault gate exists; review its outcome instead of replaying.")
    proof = json.loads((ROOT / "TestResults/HostedCqlIntegrationCurrent/SolanaOASIS-client-http-flow.json").read_text())
    owner = json.loads((state / "owner.acknowledged.json").read_text())["id"]
    token = flow.verification.protected_token((state / "owner.dpapi").read_bytes(), decrypt=True).decode()
    foreign = flow.verification.protected_token((state / "foreign.dpapi").read_bytes(), decrypt=True).decode()
    identity = proof["ids"][0]
    expected = next(row for row in proof["mongoRows"] if row["id"] == identity)
    if flow.call("sandbox/faults", token, method="GET")["result"]:
        raise RuntimeError("Existing avatar fault must not be overwritten or restored by this gate.")
    for provider in PROVIDERS:
        status, baseline = raw_request(token, "data/load-holon", {"id": identity, "providerType": provider,
            "autoFailOverMode": "OFF", "autoLoadBalanceMode": "OFF", "loadChildren": False})
        if status != 200 or baseline.get("isError") or baseline.get("result", {}).get("id") != identity:
            raise RuntimeError("Existing exact replica baseline unavailable; no fault sent.")
    flow.write_new(journal, json.dumps({"owner": owner, "id": identity, "providers": PROVIDERS,
        "purpose": "bounded avatar-only read failover", "maximumFaultSeconds": 30}).encode())
    results = []
    report = ROOT / "TestResults/HostedCqlIntegrationCurrent/hyperdrive-failover-http.json"
    for provider in PROVIDERS:
        flow.write_new(state / ("fault-" + provider + ".intent.json"), json.dumps({"provider": provider, "seconds": 30}).encode())
        try:
            fault = flow.call("sandbox/providers/" + provider + "/offline?seconds=30", token)
            flow.write_new(state / ("fault-" + provider + ".acknowledged.json"), json.dumps({"expires": fault["result"]}).encode())
            before = flow.call("sandbox/refusals", token, method="GET")["result"]
            status, blocked = raw_request(token, "data/load-holon", {"id": identity, "providerType": provider,
                "autoFailOverMode": "OFF", "autoLoadBalanceMode": "OFF", "loadChildren": False})
            if not blocked.get("isError") or blocked.get("errorCode") != "HYPERDRIVE_PROVIDER_OFFLINE":
                raise RuntimeError("Disabled failover must report the exact offline provider error.")
            loaded = flow.call("data/load-holon", token, {"id": identity, "providerType": provider,
                "autoFailOverMode": "ON", "autoLoadBalanceMode": "OFF", "loadChildren": False})
            route = verify_failover_response(loaded, {"id": identity, "name": expected["name"],
                "version": expected["version"], "createdByAvatarId": owner}, provider)
            after = flow.call("sandbox/refusals", token, method="GET")["result"]
            if after.get(provider, 0) - before.get(provider, 0) < 2:
                raise RuntimeError("No independently observed offline refusals for both attempts.")
            if flow.call("sandbox/faults", foreign, method="GET")["result"]:
                raise RuntimeError("Fault leaked into the independent visitor scope.")
            isolated = flow.call("data/query-cql", foreign, {"provider": provider, "orderBy": "Id", "take": 2,
                "filters": [{"field": "Id", "scalar": "Guid", "operator": "In", "value": proof["ids"]}]})
            if isolated["result"]["rows"]:
                raise RuntimeError("Independent visitor read foreign private records.")
            results.append({"provider": provider, "blockedCode": blocked["errorCode"],
                "refusals": after[provider] - before.get(provider, 0), "id": identity,
                "foreignPrivateRows": 0, "loaded": True, "route": route})
        finally:
            # Before-state was fault-free and this is our short-lived avatar-only fault.
            # Restoration is safe even if the preceding fault outcome is unknown.
            flow.call("sandbox/providers/" + provider + "/online", token)
            if provider in flow.call("sandbox/faults", token, method="GET")["result"]:
                raise RuntimeError("Owned fault restoration was not observed.")
            flow.write_new(state / ("fault-" + provider + ".restored.json"), b'{"restored":true}')
            flow.verification.write(report, {"results": results, "browserAcceptance": False, "newSaves": 0})
    flow.write_new(state / "hyperdrive-failover.acknowledged.json", json.dumps({"cases": len(results), "restored": True}).encode())
    print(json.dumps({"cases": len(results), "restored": True, "newSaves": 0, "browserAcceptance": False}))


if __name__ == "__main__":
    main()
