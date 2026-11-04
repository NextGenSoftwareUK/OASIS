"""Prepare a free public Connext message CQL profile; apply only to the exact deployed release.

Source properties, permissions, tariffs and presets are explicit. Native account
configuration is unchanged. The shared publisher owns journals and observations.
"""
from pathlib import Path
import copy
import hashlib
import importlib.util
import json
import sys
import uuid

ROOT = Path(__file__).resolve().parents[2]
PREFIX = "connext-public"
KEYS = ["CQL_CONNEXT_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON",
        "CQL_HOSTED_QUERY_EXAMPLES__ConnextOASIS"]
EXPECTED_EVENTS = [
    {
        "transferId": "0x7c0c4ca4f09d21feac9ab324a5bb35ffb6f23723319568a109e1073cadee42ce",
        "transactionHash": "0xa752ce7e2af748be17a6f69230a920f043d0d1e91b7af0b4d6a2c6940127fcbb",
        "blockHash": "0x0f8957904d6f8b4e9a7b73322c2052fd21bc3cc29ee980b9067c86f321cb58ca",
        "blockNumber": 23276094,
        "logIndex": 106
    },
    {
        "transferId": "0x1b22bd0c1c12485069ce19c7bc3f6298e86aeb6460006e3e38887783bf267f66",
        "transactionHash": "0x271782eebbb0ad943a6af3431011268498a647acdb5c481c7abe19ddefb9f501",
        "blockHash": "0x2e2d08d26f630009df53f0ad0d9db500b9256a799065b9df058f654c97bf8c2b",
        "blockNumber": 23274467,
        "logIndex": 558
    },
    {
        "transferId": "0x0cce04c1f5dc197942f34807e4285c1f7a4f91c22d81d1855e2d13e28bc51144",
        "transactionHash": "0xa9eda28762f127e9f22ff12aed793c02c94e7cd6c7e3d9f937ce71965dcb3cd3",
        "blockHash": "0x1ca527f718e5a1f2e5f3692acb8c855efa37a660e4eb6fbdcbd0d1be1b235c7a",
        "blockNumber": 23263842,
        "logIndex": 551
    }
]
EXPECTED_KEYS = [event["transferId"] for event in EXPECTED_EVENTS]


def module(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


configuration = module("Publish-HostedSolidConfiguration")


def candidate(before, source):
    if (set(source) != {"schema", "events", "sourceOwner"}
            or type(source["schema"]) is not int or source["schema"] != 1
            or source["events"] != EXPECTED_EVENTS or not isinstance(source["sourceOwner"], str)
            or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("The exact qualified anonymous Connext message catalogue is required.")
    if KEYS[0] in before:
        raise ValueError("Existing Connext message enrollment requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "ConnextTransferId" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit ConnextTransferId queryable policy before enrollment.")

    after = copy.deepcopy(before)
    admission = json.loads(before[KEYS[1]])
    provider = "ConnextOASIS"
    if (provider in admission["queryOnlyProviders"] or provider in admission["ordinaryProviders"]
            or any(row["provider"] == provider for row in admission["nativeProviders"])
            or any(row["provider"] == provider for row in admission["queryTariffs"])):
        raise ValueError("Existing Connext message admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append(provider)
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = provider
    admission["queryTariffs"].append(tariff)
    shared = json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}"))
    if provider in shared or KEYS[2] in before:
        raise ValueError("Existing Connext message examples require reconciliation.")

    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["ConnextTransferId"], "scalar": "String", "operator": "In",
                "value": EXPECTED_KEYS[:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Connext indexed Ethereum origin events (" + str(count) + " profiles)",
            "request": {"provider": provider, "maximumDurationMilliseconds": 30000, **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Connext indexed Ethereum origin events + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", provider],
                "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2,
                "maximumDurationMilliseconds": 30000, "take": 25}})
    after[KEYS[0]] = configuration.compact(source)
    after[KEYS[1]] = configuration.compact(admission)
    after[KEYS[2]] = configuration.compact(rows)
    if ({key for key in before.keys() | after.keys() if before.get(key) != after.get(key)} != set(KEYS)
            or any(key not in after for key in before)):
        raise ValueError("Exactly three enrolled values may change; existing values cannot be removed.")
    if any(len(after[key]) > 32768 for key in KEYS):
        raise ValueError("Hosted variable size budget exceeded.")
    return after


def qualified_source():
    path = ROOT / "LocalState/COSMIC/ConnextPublicQualificationCurrent/source.json"
    raw = path.read_bytes()
    proof = json.loads(path.with_name("native-qualified.acknowledged.json").read_text(encoding="utf-8"))
    if (proof.get("schema") != 1 or proof.get("nativeQualified") is not True
            or proof.get("sourceSha256") != hashlib.sha256(raw).hexdigest()
            or proof.get("nativeCounts") != [3, 2]):
        raise RuntimeError("Original source-bound native qualification acknowledgement is required.")
    local = json.loads(path.with_name("web4-local-qualified.acknowledged.json").read_text(encoding="utf-8"))
    if (proof.get("policy") != "ethereum-indexed-origin-event"
            or local.get("schema") != 1 or local.get("web4LocalQualified") is not True
            or local.get("sourceSha256") != hashlib.sha256(raw).hexdigest()
            or local.get("policy") != "ethereum-indexed-origin-event"
            or local.get("nativeCounts") != [3, 2] or local.get("federationCounts") != [5, 4]
            or local.get("foreignPrivate") != 0 or local.get("foreignPublic") != 3
            or local.get("savedPrivateCopies") != 2
            or local.get("fixtureCleaned") is not True):
        raise RuntimeError("Original source-bound Connext local HTTP/privacy qualification required.")
    return json.loads(raw)


def prepare():
    source = qualified_source()
    before = configuration.variables()
    after = candidate(before, source)
    state = configuration.STATE
    state.mkdir(parents=True, exist_ok=True)
    snapshot = state / (PREFIX + "-before-variables.dpapi")
    if snapshot.exists():
        if configuration.protected_read(snapshot.name) != before:
            raise RuntimeError("Baseline changed; reconcile before another candidate.")
    else:
        configuration.write_new(snapshot, configuration.verification.protected_token(configuration.compact(before).encode()))
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    review = {"changedKeys": KEYS, "profiles": {"ConnextOASIS": source},
        "examples": {"ConnextOASIS": json.loads(after[KEYS[2]])},
        "dnaUnchanged": before["OASIS_DNA_JSON"] == after["OASIS_DNA_JSON"], "cloudSpend": 0,
        "policy": "ethereum-indexed-origin-event", "independentFinalityProof": False}
    target = ROOT / "TestResults/ConnextPublicSourceCurrent/prepared-review.json"
    target.parent.mkdir(parents=True, exist_ok=True)
    configuration.write_new(target, (json.dumps(review, indent=2) + "\n").encode())
    print("Protected Connext message candidate prepared: three values, four presets; no service mutation or deployment.")


def apply():
    before = configuration.protected_read(PREFIX + "-before-variables.dpapi")
    after = configuration.protected_read(PREFIX + "-candidate-variables.dpapi")
    if candidate(before, qualified_source()) != after:
        raise RuntimeError("Reviewed candidate/source differs; no mutation permitted.")
    module("Prepare-TenSourcePublicBatch").require_matching_release(configuration)
    configuration.apply_values(PREFIX, KEYS)


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["apply"]:
        apply()
    else:
        raise SystemExit("Use no arguments to prepare, or apply for the exact reviewed deployed release.")
