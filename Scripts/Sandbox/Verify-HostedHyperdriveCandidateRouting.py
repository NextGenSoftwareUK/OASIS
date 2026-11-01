"""Read-only acceptance for request-scoped Default candidate selection.

Auto scoring need not rotate. This gate proves eligibility/override routing,
not score optimality, latency or round-robin fairness. No settings or saves.
"""
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("routing", Path(__file__).with_name("Verify-HostedHyperdriveLoadBalancing.py"))
routing = importlib.util.module_from_spec(spec)
spec.loader.exec_module(routing)
flow = routing.flow


def verify_candidate(response, expected, eligible):
    if eligible not in routing.PROVIDERS:
        raise ValueError("Only the three reviewed replica providers are eligible.")
    route = routing.verify_response(response, expected, "Default")
    if route["InitialProvider"] != eligible or route["ServingProvider"] != eligible:
        raise ValueError("Default routing ignored this request's sole eligible provider.")
    return route


def main():
    state = ROOT / "LocalState/COSMIC/HostedHyperdriveRoutingV2Current"
    ack = state / "hyperdrive-candidate-routing.acknowledged.json"
    if ack.exists():
        raise RuntimeError("Completed candidate gate retained; do not replay.")
    owner = json.loads((state / "owner.acknowledged.json").read_text())["id"]
    expected = routing.reviewed_record(json.loads((state / "routing-record.acknowledged.json").read_text()), owner)
    identity, token = flow.session(state, "owner", allow_create=False)
    if identity["id"] != owner or flow.call("sandbox/faults", token, method="GET")["result"]:
        raise RuntimeError("Original owner and fault-free baseline required.")
    rows = []
    report = ROOT / "TestResults/HostedCqlIntegrationCurrent/hyperdrive-candidate-routing-v2-http.json"
    for eligible in sorted(routing.PROVIDERS):
        response = flow.call("data/load-holon", token, {"id": expected["id"], "providerType": "Default",
            "autoLoadBalanceMode": "ON", "autoLoadBalanceProviders": eligible,
            "autoFailOverMode": "OFF", "loadChildren": False})
        route = verify_candidate(response, expected, eligible)
        rows.append({"eligible": eligible, "route": route, "id": expected["id"]})
        flow.verification.write(report, {"reads": rows, "newSaves": 0, "settingsChanges": 0,
            "scoreOptimalityVerified": False, "browserAcceptance": False})
    flow.write_new(ack, json.dumps({"reads": len(rows), "newSaves": 0, "settingsChanges": 0,
        "scope": "request-scoped Default candidate routing, not scoring/fairness"}).encode())
    print(json.dumps({"reads": len(rows), "providers": sorted(routing.PROVIDERS), "newSaves": 0, "settingsChanges": 0}))


if __name__ == "__main__":
    main()
