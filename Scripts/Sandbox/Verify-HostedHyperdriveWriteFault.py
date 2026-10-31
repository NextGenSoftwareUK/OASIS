"""Prove conditional create-only writes do not change authority after an offline error.

Uses an explicitly selected, durable demo cohort. Unique IDs/operation keys are recorded before
one save per authority. Unknown writes are never retried. Restore owned faults even
on failure; preserve any unexpected records for investigation and normal expiry.
"""
from pathlib import Path
import importlib.util
import json
import argparse
import uuid

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("failover", ROOT / "Scripts/Sandbox/Verify-HostedHyperdriveFailover.py")
failover = importlib.util.module_from_spec(spec)
spec.loader.exec_module(failover)
flow = failover.flow
SAFE_REJECTIONS = {"HYPERDRIVE_PROVIDER_OFFLINE", "COSMIC_WRITE_OWNERSHIP_LOOKUP_FAILED"}


def rejected(outcome):
    return (outcome.get("isError") is True and outcome.get("isSaved") is False
        and outcome.get("errorCode") in SAFE_REJECTIONS and outcome.get("returnedId") is None)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cohort", choices=["original-solana", "negative-v1"], default="original-solana",
        help="negative-v1 is a distinct explicitly planned test, never replacement/replay of original IDs.")
    parser.add_argument("--resume-reviewed-rejections", action="store_true")
    options = parser.parse_args()
    state = ROOT / "LocalState/COSMIC" / ("HostedQueryAcceptance-SolanaOASIS" if options.cohort == "original-solana"
        else "HostedHyperdriveWriteFaultCurrent")
    state.mkdir(exist_ok=True)
    if (state / "hyperdrive-write-fault.acknowledged.json").exists():
        raise RuntimeError("Completed write-fault gate retained; do not repeat a green scope.")
    intent = state / "hyperdrive-write-fault.intent.json"
    resume = options.resume_reviewed_rejections
    if intent.exists() and not resume:
        raise RuntimeError("Existing write-fault intent; reconcile instead of replaying.")
    if options.cohort == "negative-v1":
        identity, token = flow.session(state, "owner")
        owner = identity["id"]
    else:
        owner = json.loads((state / "owner.acknowledged.json").read_text())["id"]
        token = flow.verification.protected_token((state / "owner.dpapi").read_bytes(), decrypt=True).decode()
    if flow.call("sandbox/faults", token, method="GET")["result"]:
        raise RuntimeError("Existing avatar faults must not be changed.")
    if resume:
        retained = json.loads(intent.read_text())
        if retained["owner"] != owner or [row["provider"] for row in retained["records"]] != failover.PROVIDERS:
            raise RuntimeError("Retained owner/authority intent differs.")
        records = retained["records"]
    else:
        records = [{"provider": provider, "id": str(uuid.uuid4()), "operation": str(uuid.uuid4())}
            for provider in failover.PROVIDERS]
    ids = [record["id"] for record in records]

    def absent():
        for provider in failover.PROVIDERS:
            rows = flow.call("data/query-cql", token, {"provider": provider, "orderBy": "Id", "take": 3,
                "filters": [{"field": "Id", "scalar": "Guid", "operator": "In", "value": ids}]})["result"]["rows"]
            if rows:
                raise RuntimeError("A write-fault identity exists; preserve it and investigate, never delete/replay automatically.")

    absent()
    if not resume:
        flow.write_new(intent, json.dumps({"owner": owner, "records": records, "purpose": "offline conditional authority fence"}).encode())
    results = []
    report = ROOT / "TestResults/HostedCqlIntegrationCurrent" / ("hyperdrive-write-fault-http.json"
        if options.cohort == "original-solana" else "hyperdrive-write-fault-negative-v1-http.json")
    for record in records:
        provider = record["provider"]
        prefix = "write-fault-" + provider
        prior = state / (prefix + ".response.json")
        if prior.exists():
            outcome = json.loads(prior.read_text())
            if not rejected(outcome) or not (state / (prefix + ".restored.json")).exists():
                raise RuntimeError("Existing outcome is not an acknowledged safe rejection/restoration; do not resume.")
            results.append({"provider": provider, "id": record["id"], "errorCode": outcome["errorCode"],
                "isSaved": False, "reconciledAfterRestore": True})
            continue
        flow.write_new(state / (prefix + ".intent.json"), json.dumps(record).encode())
        try:
            fault = flow.call("sandbox/providers/" + provider + "/offline?seconds=30", token)
            flow.write_new(state / (prefix + ".offline.json"), json.dumps({"expires": fault["result"]}).encode())
            if provider not in flow.call("sandbox/faults", token, method="GET")["result"]:
                raise RuntimeError("Owned fault is not active; no write sent.")
            status, saved = failover.raw_request(token, "data/save-holon", {
                "operationId": record["operation"], "providerType": provider,
                "expectedRevisions": {record["id"]: None}, "saveChildren": False,
                "autoFailOverMode": "ON", "autoLoadBalanceMode": "ON", "autoReplicationMode": "ON",
                "holon": {"id": record["id"], "name": "Offline authority fence probe",
                    "description": "Owned temporary negative test; must not be committed",
                    "isNewHolon": True, "isActive": True, "isPublic": False}})
            # Capture the returned outcome before checking it. A timeout or incomplete
            # response leaves the original intent and must never cause another save.
            flow.write_new(state / (prefix + ".response.json"), json.dumps({"status": status,
                "isError": saved.get("isError"), "isSaved": saved.get("isSaved"),
                "errorCode": saved.get("errorCode"), "returnedId": (saved.get("result") or {}).get("id")}).encode())
            if not rejected({"isError": saved.get("isError"), "isSaved": saved.get("isSaved"),
                    "errorCode": saved.get("errorCode"), "returnedId": (saved.get("result") or {}).get("id")}):
                raise RuntimeError("Offline conditional authority was not explicitly rejected; preserve outcome for investigation.")
            if provider not in flow.call("sandbox/faults", token, method="GET")["result"]:
                raise RuntimeError("Fault expired during save; acceptance unproven.")
            results.append({"provider": provider, "id": record["id"], "errorCode": saved["errorCode"], "isSaved": False})
        finally:
            flow.call("sandbox/providers/" + provider + "/online", token)
            if provider in flow.call("sandbox/faults", token, method="GET")["result"]:
                raise RuntimeError("Owned fault restoration not observed.")
            flow.write_new(state / (prefix + ".restored.json"), b'{"restored":true}')
            flow.verification.write(report, {"results": results, "browserAcceptance": False})
    absent()
    flow.verification.write(report, {"results": results, "allProviderRows": 0, "absenceProviders": failover.PROVIDERS,
        "restored": True, "browserAcceptance": False, "uncertainCommitRecovery": False})
    flow.write_new(state / "hyperdrive-write-fault.acknowledged.json", json.dumps({"cases": 3, "allProviderRows": 0,
        "restored": True, "scope": "offline authority/preflight rejection; not uncertain-commit recovery"}).encode())
    print(json.dumps({"cases": 3, "allProviderRows": 0, "restored": True, "browserAcceptance": False,
        "errorCodes": [row["errorCode"] for row in results], "uncertainCommitRecovery": False}))


if __name__ == "__main__":
    main()
