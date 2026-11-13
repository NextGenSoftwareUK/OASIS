"""Read-only live gate for response-owned routing; never infer from shared status.

Requires an explicitly selected acknowledged cohort and deployed route metadata.
Durable expected-record evidence lives beside the protected session, not TestResults.
No accounts, saves, faults or settings changes. A completed gate is not replayed.
"""
from pathlib import Path
import importlib.util
import json
import argparse
from collections import Counter
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("flow", ROOT / "Scripts/Sandbox/Verify-HostedQuerySource.py")
flow = importlib.util.module_from_spec(spec)
spec.loader.exec_module(flow)
PROVIDERS = {"MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS"}


def verify_response(response, expected, requested, *, providers=PROVIDERS, strategy="Auto"):
    row = response.get("result") or {}
    if response.get("isError") or any(row.get(key) != expected[key] for key in ("id", "name", "version", "createdByAvatarId")):
        raise ValueError("Load did not return the acknowledged identity/content/version/owner.")
    encoded = response.get("metaData", {}).get("hyperDriveRoute")
    if not isinstance(encoded, str):
        raise ValueError("Deployed per-response route evidence is required; shared status is not a substitute.")
    route = json.loads(encoded)
    if (route.get("RequestedProvider") != requested or route.get("InitialProvider") not in providers or
            route.get("ServingProvider") != route.get("InitialProvider") or route.get("Succeeded") is not True or
            route.get("RequestedStrategy") != strategy):
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


def verify_round_robin(state, revision):
    if len(revision) != 40 or any(c not in "0123456789abcdef" for c in revision):
        raise ValueError("An exact lowercase WEB4 release SHA is required.")
    config_spec = importlib.util.spec_from_file_location(
        "railway_configuration", Path(__file__).with_name("Publish-HostedSolidConfiguration.py"))
    config = importlib.util.module_from_spec(config_spec)
    config_spec.loader.exec_module(config)

    def serving_deployment():
        result = subprocess.run([str(config.CLI), "deployment", "list", *config.COMMON,
                                 "--limit", "1", "--json"], capture_output=True, check=True)
        deployments = json.loads(result.stdout)
        if len(deployments) != 1 or deployments[0].get("status") != "SUCCESS" or (
                deployments[0].get("meta", {}).get("commitHash") != revision):
            raise RuntimeError("The reviewed WEB4 release must be serving.")
        return str(uuid.UUID(deployments[0]["id"]))

    state = state.resolve(strict=True)
    deployment = serving_deployment()
    attempt = state / f"hyperdrive-round-robin-{deployment}.started.json"
    acknowledgement = state / f"hyperdrive-round-robin-{deployment}.acknowledged.json"
    if attempt.exists() or acknowledgement.exists():
        raise RuntimeError("A completed or uncertain RoundRobin attempt is retained.")
    owner = json.loads((state / "owner.acknowledged.json").read_text())["id"]
    expected = reviewed_record(json.loads((state / "routing-record.acknowledged.json").read_text()), owner)
    identity, token = flow.session(state, "owner", allow_create=False, allow_renewal=False)
    if identity["id"] != owner or flow.call("sandbox/faults", token, method="GET")["result"]:
        raise RuntimeError("The acknowledged owner and a fault-free baseline are required.")
    remaining = flow.call("subscription/usage/current", token, method="GET").get("dailyCallsRemaining")
    if type(remaining) is not int or (remaining != -1 and remaining < 6):
        raise RuntimeError("The acknowledged owner needs six remaining daily calls for this read gate.")
    providers = {"MongoDBOASIS", "SQLLiteDBOASIS"}
    flow.write_new(attempt, json.dumps({"deployment": deployment, "revision": revision}).encode())
    selected = Counter()
    for _ in range(6):
        response = flow.call("data/load-holon", token, {
            "id": expected["id"], "providerType": "Default", "loadChildren": False,
            "autoLoadBalanceMode": "ON", "autoLoadBalanceProviders": ",".join(sorted(providers)),
            "loadBalancingStrategy": "RoundRobin", "autoFailOverMode": "OFF",
        })
        route = verify_response(response, expected, "Default", providers=providers, strategy="RoundRobin")
        selected[route["ServingProvider"]] += 1
    if selected != Counter({provider: 3 for provider in providers}) or serving_deployment() != deployment:
        raise RuntimeError("The reviewed deployment did not serve three reads from each provider.")
    evidence = {"deployment": deployment, "revision": revision, "servingCounts": dict(selected), "newSaves": 0}
    flow.write_new(acknowledgement, json.dumps(evidence).encode())
    print(json.dumps(evidence))

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cohort", choices=["original-solana", "routing-v1", "routing-v2"], default="original-solana")
    parser.add_argument("--round-robin-state", type=Path, help="Existing acknowledged owner and shared record.")
    parser.add_argument("--expected-web4-revision", help="Exact deployed WEB4 release SHA for RoundRobin.")
    options = parser.parse_args()
    if options.round_robin_state is not None:
        if not options.expected_web4_revision:
            parser.error("--expected-web4-revision is required for RoundRobin")
        verify_round_robin(options.round_robin_state, options.expected_web4_revision)
        return
    if options.expected_web4_revision:
        parser.error("--expected-web4-revision requires --round-robin-state")
    state = ROOT / "LocalState/COSMIC" / ("HostedQueryAcceptance-SolanaOASIS"
        if options.cohort == "original-solana" else "HostedHyperdriveRoutingV2Current" if options.cohort == "routing-v2" else "HostedHyperdriveRoutingCurrent")
    report = ROOT / "TestResults/HostedCqlIntegrationCurrent" / f"hyperdrive-auto-selection-{options.cohort}-http.json"
    acknowledgement = state / "hyperdrive-auto-selection.acknowledged.json"
    if acknowledgement.exists() or (state / "hyperdrive-load-balancing.acknowledged.json").exists():
        raise RuntimeError("Completed gate retained; do not repeat a green scope.")
    # This verifier owns reads only. Missing or expired sessions must never cause
    # flow.session to create a replacement or recycle an old operation identity.
    if not (state / "owner.acknowledged.json").is_file():
        raise RuntimeError("Prepare and acknowledge the selected cohort separately; no session created by a read gate.")
    owner = json.loads((state / "owner.acknowledged.json").read_text())["id"]
    expected = reviewed_record(json.loads((state / "routing-record.acknowledged.json").read_text()), owner)
    identity, token = flow.session(state, "owner", allow_create=False, allow_renewal=False)
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
    # Auto ranks providers by score. Repeated selection is valid when one provider
    # remains highest ranked. Distribution requires a separate RoundRobin gate.
    flow.write_new(acknowledgement, json.dumps({"reads": len(observed), "strategy": "Auto", "automaticProviders": sorted(selected), "newSaves": 0}).encode())
    print(json.dumps({"reads": len(observed), "automaticProviders": sorted(selected), "newSaves": 0, "browserAcceptance": False}))


if __name__ == "__main__":
    main()
