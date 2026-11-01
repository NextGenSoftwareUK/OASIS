"""Prepare one distinct, durable routing-v1 fixture; never retry an uncertain save.

Run only after the response-owned routing metadata release. This is an explicitly
planned new cohort, not replacement of original Solana sessions or operations.
"""
from pathlib import Path
import hashlib
import argparse
import importlib.util
import json
import uuid

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("routing_gate", Path(__file__).with_name("Verify-HostedHyperdriveLoadBalancing.py"))
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)
flow = gate.flow


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def expected_record(response, intent, owner):
    row = response.get("result") or {}
    if (response.get("isError") or response.get("isWarning") or response.get("isSaved") is not True
            or row.get("id") != intent["holon"]["id"] or row.get("name") != intent["holon"]["name"]
            or row.get("description") != intent["holon"]["description"]
            or row.get("createdByAvatarId") != owner or row.get("isPublic") is not False
            or type(row.get("version")) is not int or row["version"] != 1):
        raise RuntimeError("Incomplete or different save acknowledgement; retain original intent without retry.")
    return {key: row[key] for key in ("id", "name", "version", "createdByAvatarId")}


def save_once(state, owner, token):
    intent_path = state / "record-save.intent.json"
    ack_path = state / "record-save.acknowledged.json"
    if ack_path.exists():
        intent = json.loads(intent_path.read_text())
        ack = json.loads(ack_path.read_text())
        if ack.get("intentSha256") != digest(intent) or ack.get("owner") != owner:
            raise RuntimeError("Retained save intent/owner changed; no replacement write.")
        expected = gate.reviewed_record({"schema": 1, "sourceEvidenceSha256": digest(ack), "expected": ack["expected"]}, owner)
        if expected["id"] != intent["holon"]["id"] or expected["name"] != intent["holon"]["name"]:
            raise RuntimeError("Acknowledged record differs from original save intent.")
        return intent, ack
    if intent_path.exists():
        raise RuntimeError("Unacknowledged save intent; reconcile original operation, never retry.")
    identity = str(uuid.uuid4())
    intent = {"operationId": str(uuid.uuid4()), "providerType": "MongoDBOASIS",
        "expectedRevisions": {identity: None}, "saveChildren": False,
        "autoFailOverMode": "OFF", "autoLoadBalanceMode": "OFF", "autoReplicationMode": "ON",
        "holon": {"id": identity, "name": "Routing-v1 private coffee",
            "description": "Owned temporary routing baseline; normal sandbox expiry",
            "isNewHolon": True, "isActive": True, "isPublic": False}}
    flow.write_new(intent_path, json.dumps(intent).encode())
    response = flow.call("data/save-holon", token, intent, operation=intent["operationId"])
    expected = expected_record(response, intent, owner)
    ack = {"owner": owner, "intentSha256": digest(intent), "operationId": intent["operationId"], "expected": expected}
    flow.write_new(ack_path, json.dumps(ack).encode())
    return intent, ack


def verify_replicas(intent, ack, token):
    observations = []
    for provider in sorted(gate.PROVIDERS):
        response = flow.call("data/load-holon", token, {"id": ack["expected"]["id"], "providerType": provider,
            "autoFailOverMode": "OFF", "autoLoadBalanceMode": "OFF", "loadChildren": False})
        row = response.get("result") or {}
        if (response.get("isError") or response.get("isWarning")
                or type(row.get("version")) is not int
                or any(row.get(key) != value for key, value in ack["expected"].items())
                or row.get("description") != intent["holon"]["description"] or row.get("isPublic") is not False):
            raise RuntimeError("Exact private replica baseline not ready; resume reads only, never resave.")
        observations.append({"provider": provider, "expected": ack["expected"], "private": True})
    return observations


def prepare(state):
    if (state / "routing-record.acknowledged.json").exists():
        raise RuntimeError("Completed routing fixture retained; do not repeat setup.")
    state.mkdir(exist_ok=True)
    if (state / "record-save.intent.json").exists() and not (state / "owner.acknowledged.json").is_file():
        raise RuntimeError("Original save cohort lost its owner acknowledgement; no replacement session.")
    owner, token = flow.session(state, "owner")
    if flow.call("sandbox/faults", token, method="GET")["result"]:
        raise RuntimeError("Existing faults must not be changed by fixture setup.")
    intent, ack = save_once(state, owner["id"], token)
    observations = verify_replicas(intent, ack, token)
    evidence = {"saveAcknowledgementSha256": digest(ack), "replicas": observations}
    # Flush baseline evidence before publishing the input consumed by the read gate.
    proof = state / "replica-baseline.acknowledged.json"
    if proof.exists():
        if json.loads(proof.read_text()) != evidence:
            raise RuntimeError("Retained replica evidence differs; preserve it for review.")
    else:
        flow.write_new(proof, json.dumps(evidence).encode())
    document = {"schema": 1, "sourceEvidenceSha256": hashlib.sha256(proof.read_bytes()).hexdigest(), "expected": ack["expected"]}
    flow.write_new(state / "routing-record.acknowledged.json", json.dumps(document).encode())
    return {"replicas": len(observations), "record": ack["expected"]["id"], "browserAcceptance": False}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cohort", choices=["routing-v1", "routing-v2"], default="routing-v1")
    options = parser.parse_args()
    # V2 is a separately planned post-correction cohort. V1 is never overwritten.
    directory = "HostedHyperdriveRoutingCurrent" if options.cohort == "routing-v1" else "HostedHyperdriveRoutingV2Current"
    print(json.dumps(prepare(ROOT / "LocalState/COSMIC" / directory)))
