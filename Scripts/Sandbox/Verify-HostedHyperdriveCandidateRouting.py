"""Read-only acceptance for request-scoped Default candidate selection.

Auto scoring need not rotate. This gate proves eligibility/override routing,
not score optimality, latency or round-robin fairness. No settings or saves.
"""
import importlib.util
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("routing", Path(__file__).with_name("Verify-HostedHyperdriveLoadBalancing.py"))
routing = importlib.util.module_from_spec(spec)
spec.loader.exec_module(routing)
flow = routing.flow


def verify_candidate(response, expected, eligible, *, providers=routing.PROVIDERS):
    if eligible not in providers:
        raise ValueError("The candidate provider is outside the reviewed scope.")
    return routing.verify_response(response, expected, "Default", providers={eligible})


def verify_localfile(visitor):
    provider = "LocalFileOASIS"
    state = ROOT / "LocalState/COSMIC/HostedLocalFilePublicDatasetCurrent"
    reviewed = json.loads((state / "point-routing-reviewed-record.json").read_bytes())
    catalogue = json.loads((state / "local-source.json").read_bytes())
    expected = reviewed["expected"]
    record_id = expected["id"]
    if (reviewed["schema"] != 1 or reviewed["provider"] != provider
            or expected["createdByAvatarId"] != catalogue["sourceOwner"]
            or not any(entry["id"] == expected["id"] and entry["sha256"].lower() == reviewed["sourceEvidenceSha256"]
                       for entry in catalogue["records"])):
        raise ValueError("The reviewed point-load record must match the retained LocalFile catalogue.")
    files = list((state / "source").rglob(record_id + ".json"))
    if len(files) != 1:
        raise ValueError("Exactly one retained LocalFile record is required.")
    raw = files[0].read_bytes()
    if hashlib.sha256(raw).hexdigest() != reviewed["sourceEvidenceSha256"]:
        raise ValueError("The retained LocalFile record digest changed.")
    row = json.loads(raw)
    if (row.get("IsPublic") is not True or row.get("IsDeleted") is not False
            or expected != {key[0].lower() + key[1:]: row[key]
                            for key in ("Id", "Name", "Version", "CreatedByAvatarId")}):
        raise ValueError("The retained LocalFile record must be public and match the expected fields.")
    visitor = flow.acknowledged_readiness_visitor(visitor)
    deployment = flow.require_readiness_release()
    ack = state / f"point-routing-{deployment}.acknowledged.json"
    if ack.exists():
        raise RuntimeError("Completed LocalFile routing gate retained.")
    _, token = flow.session(visitor, "visitor", allow_create=False, allow_renewal=False)
    remaining = flow.call("subscription/usage/current", token, method="GET").get("dailyCallsRemaining")
    if type(remaining) is not int or (remaining != -1 and remaining < 1):
        raise RuntimeError("The retained visitor needs one remaining daily call for the point read.")
    response = flow.call("data/load-holon", token, {"id": expected["id"], "providerType": "Default",
        "autoLoadBalanceMode": "ON", "autoLoadBalanceProviders": provider,
        "autoFailOverMode": "OFF", "loadChildren": False})
    route = verify_candidate(response, expected, provider, providers={provider})
    if flow.require_readiness_release() != deployment:
        raise RuntimeError("The reviewed deployment changed during the LocalFile point read.")
    evidence = {"deployment": deployment, "id": expected["id"], "route": route, "newSaves": 0}
    flow.write_new(ack, json.dumps(evidence).encode())
    print(json.dumps(evidence))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--localfile-visitor", type=Path, help="Existing acknowledged WEB4 readiness visitor.")
    options = parser.parse_args()
    if options.localfile_visitor is not None:
        verify_localfile(options.localfile_visitor)
        return
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
