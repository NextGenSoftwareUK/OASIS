"""Prepare a free public Finney block-header CQL profile; apply only to the exact deployed release.

Source properties, permissions, tariffs and presets are explicit. Legacy provider
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
PREFIX = "bittensor-public"
KEYS = ["CQL_BITTENSOR_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON",
        "CQL_HOSTED_QUERY_EXAMPLES__BittensorOASIS"]
EXPECTED_KEYS = ['0x0f4110f124542196f0a7a9dcfc5722b513c3db75d46c5bc04d0276db1719b243', '0x99782fe217285c58836d3acde394f0bdcb7ec06bc69feb8b5ad2a79f61b15fb8', '0x69741b39d4c8dbd69e460516ae4d6e3b81b33fc1b689ba1569da40a755042175']


def module(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


configuration = module("Publish-HostedSolidConfiguration")


def candidate(before, source):
    if (set(source) != {"schema", "rpcUrl", "genesisHash", "blockHashes", "sourceOwner"}
            or type(source["schema"]) is not int or source["schema"] != 1
            or source["rpcUrl"] != "https://bittensor-finney.api.onfinality.io/public"
            or source["genesisHash"] != "0x2f0555cc76fc2840a25a6ea3b9637146806f1f44b090c175ffde2a7e5ab36c03"
            or source["blockHashes"] != EXPECTED_KEYS or not isinstance(source["sourceOwner"], str)
            or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("The exact qualified anonymous Bittensor public header catalogue is required.")
    if KEYS[0] in before:
        raise ValueError("Existing Bittensor public header enrollment requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "BittensorBlockHash" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit BittensorBlockHash queryable policy before enrollment.")

    after = copy.deepcopy(before)
    admission = json.loads(before[KEYS[1]])
    provider = "BittensorOASIS"
    if (provider in admission["queryOnlyProviders"] or provider in admission["ordinaryProviders"]
            or any(row["provider"] == provider for row in admission["nativeProviders"])
            or any(row["provider"] == provider for row in admission["queryTariffs"])):
        raise ValueError("Existing Bittensor public header admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append(provider)
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = provider
    admission["queryTariffs"].append(tariff)
    shared = json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}"))
    if provider in shared or KEYS[2] in before:
        raise ValueError("Existing Bittensor public header examples require reconciliation.")

    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["BittensorBlockHash"], "scalar": "String", "operator": "In",
                "value": source["blockHashes"][:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Bittensor finalized header metadata (" + str(count) + " headers)",
            "request": {"provider": provider, "maximumDurationMilliseconds": 30000, **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Bittensor finalized header metadata + my Mongo records",
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
    path = ROOT / "LocalState/COSMIC/HostedBittensorPublicSourceCurrent/source.json"
    raw = path.read_bytes()
    proof = json.loads(path.with_name("console.acknowledged.json").read_text(encoding="utf-8"))
    if (proof.get("schema") != 1 or proof.get("writes") is not False or proof.get("rediscovery") is not False
            or proof.get("sourceSha256") != hashlib.sha256(raw).hexdigest()
            or (proof.get("full"), proof.get("subset")) != (3, 2)):
        raise RuntimeError("Original source-bound native qualification acknowledgement is required.")
    local = json.loads(path.with_name("web4-local-qualified.acknowledged.json").read_text(encoding="utf-8"))
    if (local.get("schema") != 1 or local.get("web4LocalQualified") is not True
            or local.get("sourceSha256") != hashlib.sha256(raw).hexdigest()
            or local.get("policy") != "node-reported-finalized-header-with-local-hash-verification"
            or local.get("nativeCounts") != [3, 2] or local.get("federationCounts") != [5, 4]
            or local.get("foreignPrivate") != 0 or local.get("foreignPublic") != 3
            or local.get("savedPrivateCopies") != 2
            or local.get("fixtureCleaned") is not True):
        raise RuntimeError("Original source-bound Bittensor local HTTP/privacy qualification required.")
    return json.loads(raw)


def prepare():
    source = qualified_source()
    before = configuration.variables()
    after = candidate(before, source)
    state = configuration.STATE
    snapshot = state / (PREFIX + "-before-variables.dpapi")
    if snapshot.exists():
        if configuration.protected_read(snapshot.name) != before:
            raise RuntimeError("Baseline changed; reconcile before another candidate.")
    else:
        configuration.write_new(snapshot, configuration.verification.protected_token(configuration.compact(before).encode()))
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    review = {"changedKeys": KEYS, "profiles": {"BittensorOASIS": source},
        "examples": {"BittensorOASIS": json.loads(after[KEYS[2]])},
        "dnaUnchanged": before["OASIS_DNA_JSON"] == after["OASIS_DNA_JSON"], "cloudSpend": 0,
        "policy": "node-reported-finalized-header-with-local-hash-verification", "independentConsensusVerification": False, "reliabilityCertified": False}
    target = ROOT / "TestResults/BittensorPublicSourceCurrent/prepared-review.json"
    configuration.write_new(target, (json.dumps(review, indent=2) + "\n").encode())
    print("Protected Bittensor public header candidate prepared: three values, four presets; no service mutation or deployment.")


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
