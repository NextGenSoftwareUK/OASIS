"""Explicit hosted SOLID enrollment; protected snapshots and observed per-key acknowledgements.

Default prepares only. 'apply' writes the six reviewed values without triggering
a deployment. Interrupted writes require reconciliation, never automatic replay.
"""
from pathlib import Path
import base64
import copy
import hashlib
import importlib.util
import json
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / "LocalState/COSMIC/HostedSourceRolloutCurrent"
SOURCE = ROOT / "LocalState/COSMIC/HostedSolidSourceCurrent"
CLI = (Path.home() / "AppData/Local/Microsoft/WinGet/Packages/"
       "OpenJS.NodeJS.LTS_Microsoft.Winget.Source_8wekyb3d8bbwe/"
       "node-v24.19.0-win-x64/node_modules/@railway/cli/bin/railway.exe")
COMMON = ["-p", "712f50f6-3b7b-46d4-b229-44b49922f16b", "-e", "dev",
          "-s", "5123199a-43c4-4439-9921-e10e2aec7841"]
KEYS = ["OASIS_DNA_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES_JSON",
        "OASIS_SOLID_STORAGE_KEY", "OASIS_SANDBOX_SOLID_BOOTSTRAP_JSON", "OASIS_SANDBOX_SOLID"]
spec = importlib.util.spec_from_file_location("hosted_verification", ROOT / "Scripts/Sandbox/Verify-HostedCqlDemo.py")
verification = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verification)


def compact(value):
    return json.dumps(value, separators=(",", ":"))


def write_new(path, data):
    with path.open("xb") as stream:
        stream.write(data)
        stream.flush()
        os.fsync(stream.fileno())


def variables():
    result = subprocess.run([str(CLI), "variables", *COMMON, "--json"], capture_output=True)
    if result.returncode:
        raise RuntimeError("Hosted variable observation failed; output withheld.")
    return json.loads(result.stdout)


def protected_read(name):
    return json.loads(verification.protected_token((STATE / name).read_bytes(), decrypt=True))


def prepare():
    snapshot = STATE / "solid-before-variables.dpapi"
    if snapshot.exists():
        before = protected_read(snapshot.name)
        if variables() != before:
            raise RuntimeError("Configuration changed since snapshot; reconcile before preparation.")
    else:
        before = variables()
        write_new(snapshot, verification.protected_token(compact(before).encode()))

    # The exact retained export and native probe must agree before enrollment.
    bundle_bytes = (SOURCE / "source-bundle.json").read_bytes()
    acknowledgement = json.loads((SOURCE / "export.acknowledged.json").read_text())
    if hashlib.sha256(bundle_bytes).hexdigest() != acknowledgement["bundleSha256"]:
        raise RuntimeError("Original export hash differs.")
    bundle = json.loads(bundle_bytes)
    proof = json.loads((SOURCE / "native-runtime-proof.json").read_text(encoding="utf-8-sig"))
    if bundle["root"] != "http://localhost:13000/" or len(bundle["records"]) != 3 or not all(
            proof.get(name) is True for name in ("freshPassed", "restartPassed", "ackUnchanged", "containerAbsent")):
        raise RuntimeError("Native source readiness proof is incomplete.")
    if bundle["schema"] != 1 or proof["nativeCss"] != "7.1.8":
        raise RuntimeError("Source or native runtime version differs.")
    native_ack_bytes = (SOURCE / "native-bootstrap.acknowledged.json").read_bytes()
    native_ack = json.loads(native_ack_bytes)
    captured = {"schema": 1, "root": bundle["root"], "records": [
        {name: record[name] for name in ("id", "sha256", "turtleBase64")} for record in bundle["records"]]}
    if (hashlib.sha256(native_ack_bytes).hexdigest() != proof["ackSha256"].lower()
            or native_ack["version"] != "7.1.8"
            or native_ack["digest"] != hashlib.sha256(compact(captured).encode()).hexdigest()):
        raise RuntimeError("Native proof belongs to a different source bundle.")

    after = copy.deepcopy(before)
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise RuntimeError("Existing host must explicitly enable the sandbox.")
    storage = dna["OASIS"]["StorageProviders"]
    if storage.get("SOLIDOASIS") not in (None, {}):
        raise RuntimeError("An existing SOLID source requires explicit migration, not replacement.")
    storage["SOLIDOASIS"] = {
        "PodRoot": bundle["root"], "AuthenticationMode": "Anonymous", "EnableCqlRecordKeySets": True,
        "StorageKeyEnvironmentVariable": "OASIS_SOLID_STORAGE_KEY", "NativeImportContainers": ""
    }
    after["OASIS_DNA_JSON"] = compact(dna)

    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if "SOLIDOASIS" in admission["queryOnlyProviders"] or any(
            item["provider"] == "SOLIDOASIS" for item in admission["queryTariffs"]):
        raise RuntimeError("SOLID is already admitted; reconcile enrollment.")
    admission["queryOnlyProviders"].append("SOLIDOASIS")
    tariff = copy.deepcopy(next(item for item in admission["queryTariffs"] if item["provider"] == "IPFSOASIS"))
    tariff["provider"] = "SOLIDOASIS"
    admission["queryTariffs"].append(tariff)
    after["PREPARED_STORAGE_ADMISSION_JSON"] = compact(admission)

    examples = json.loads(before["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
    if "SOLIDOASIS" in examples:
        raise RuntimeError("Existing SOLID examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
                     "filters": [{"metadataPath": ["SolidRecordId"], "scalar": "String", "operator": "In",
                                  "value": [item["id"] for item in bundle["records"][:count]]}]}
        rows.append({"id": "public-records" if count == 3 else "public-subset",
                     "label": "Read three public SOLID records" if count == 3 else "Filter to two public SOLID records",
                     "request": {"provider": "SOLIDOASIS", **selection}})
        rows.append({"id": "mongo-public-records" if count == 3 else "mongo-public-subset",
                     "label": "SOLID records + my Mongo records" if count == 3 else "Two-record subset + my Mongo records",
                     "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "SOLIDOASIS"],
                     "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2, "take": 25}})
    examples["SOLIDOASIS"] = rows
    after["CQL_HOSTED_QUERY_EXAMPLES_JSON"] = compact(examples)

    key = verification.protected_token((ROOT / "LocalState/COSMIC/SolidCurrent/storage-key.dpapi").read_bytes(), decrypt=True)
    if len(key) != 32:
        raise RuntimeError("Retained original SOLID key differs; never replace it.")
    after["OASIS_SOLID_STORAGE_KEY"] = base64.b64encode(key).decode()
    after["OASIS_SANDBOX_SOLID_BOOTSTRAP_JSON"] = compact(bundle)
    after["OASIS_SANDBOX_SOLID"] = "true"
    if {name for name in after if before.get(name) != after[name]} != set(KEYS):
        raise RuntimeError("Candidate changed unexpected configuration keys.")
    write_new(STATE / "solid-candidate-variables.dpapi", verification.protected_token(compact(after).encode()))
    print("Protected SOLID candidate prepared: six values, four public presets; no service mutation.")


def apply_values(prefix, keys):
    """One mutation/observation owner shared by explicit hosted source publishers."""
    before = protected_read(prefix + "-before-variables.dpapi")
    after = protected_read(prefix + "-candidate-variables.dpapi")
    expected = copy.deepcopy(before)
    for name in keys:
        intent = STATE / (prefix + "-" + name + ".intent.json")
        if intent.exists():
            raise RuntimeError("Existing mutation intent; reconcile instead of replaying.")
        if variables() != expected:
            raise RuntimeError("Configuration changed concurrently; no mutation sent.")
        digest = hashlib.sha256(after[name].encode()).hexdigest()
        write_new(intent, compact({"key": name, "sha256": digest, "skipDeploys": True}).encode())
        result = subprocess.run([str(CLI), "variable", "set", name, *COMMON, "--stdin", "--skip-deploys", "--json"],
                                input=after[name].encode(), capture_output=True)
        if result.returncode:
            raise RuntimeError("Mutation outcome needs reconciliation; original intent retained.")
        expected[name] = after[name]
        if variables() != expected:
            raise RuntimeError("Observed configuration differs; do not resubmit.")
        write_new(STATE / (prefix + "-" + name + ".acknowledged.json"),
                  compact({"key": name, "sha256": digest, "verified": True, "deploymentTriggered": False}).encode())
    print(str(len(keys)) + " values observed exactly; no deployment triggered.")


def apply():
    apply_values("solid", KEYS)


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["apply"]:
        apply()
    else:
        raise SystemExit("Use no arguments to prepare, or 'apply' for the reviewed candidate.")
