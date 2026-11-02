"""Prepare a free public Zcash block-summary CQL profile; apply only to the exact deployed release.

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
PREFIX = "zcash-public"
KEYS = ["CQL_ZCASH_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON",
        "CQL_HOSTED_QUERY_EXAMPLES__ZcashOASIS"]
EXPECTED_BLOCKS = [{'height': 1, 'hash': '0007bc227e1c57a4a70e237cad00e7b7ce565155ab49166bc57397a26d339283'}, {'height': 2, 'hash': '0002a26c902619fc964443264feb16f1e3e2d71322fc53dcb81cc5d797e273ed'}, {'height': 3, 'hash': '0006f6c4e5bb8e97d65ac9f3fc4bcd8770a6350f9eed0943d84ce9ca65eac67f'}]


def module(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


configuration = module("Publish-HostedSolidConfiguration")


def candidate(before, source):
    if (set(source) != {"schema", "apiUrl", "genesisHash", "blocks", "sourceOwner"}
            or type(source["schema"]) is not int or source["schema"] != 1
            or source["apiUrl"] != "https://api.zecblock.com/"
            or source["genesisHash"] != "00040fe8ec8471911baa1db1266ea15dd06b4a8a5c453883c000b031973dce08"
            or source["blocks"] != EXPECTED_BLOCKS or not isinstance(source["sourceOwner"], str)
            or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("The exact qualified anonymous Zcash public block-summary catalogue is required.")
    if KEYS[0] in before:
        raise ValueError("Existing Zcash public block-summary enrollment requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "ZcashBlockHash" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit ZcashBlockHash queryable policy before enrollment.")

    after = copy.deepcopy(before)
    admission = json.loads(before[KEYS[1]])
    provider = "ZcashOASIS"
    if (provider in admission["queryOnlyProviders"] or provider in admission["ordinaryProviders"]
            or any(row["provider"] == provider for row in admission["nativeProviders"])
            or any(row["provider"] == provider for row in admission["queryTariffs"])):
        raise ValueError("Existing Zcash public block-summary admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append(provider)
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = provider
    admission["queryTariffs"].append(tariff)
    shared = json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}"))
    if provider in shared or KEYS[2] in before:
        raise ValueError("Existing Zcash public block-summary examples require reconciliation.")

    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["ZcashBlockHash"], "scalar": "String", "operator": "In",
                "value": [block["hash"] for block in source["blocks"][:count]]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Zcash public block summary (" + str(count) + " headers)",
            "request": {"provider": provider, "maximumDurationMilliseconds": 30000, **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Zcash public block summary + my Mongo records",
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
    path = ROOT / "LocalState/COSMIC/HostedZcashPublicSourceCurrent/source.json"
    raw = path.read_bytes()
    proof = json.loads(path.with_name("console.acknowledged.json").read_text(encoding="utf-8"))
    if (proof.get("schema") != 1 or proof.get("writes") is not False or proof.get("nativeWrites") != 0
            or proof.get("sourceSha256") != hashlib.sha256(raw).hexdigest()
            or (proof.get("full"), proof.get("subset")) != (3, 2)):
        raise RuntimeError("Original source-bound native qualification acknowledgement is required.")
    local = json.loads(path.with_name("web4-local-qualified.acknowledged.json").read_text(encoding="utf-8"))
    if (local.get("schema") != 1 or local.get("web4LocalQualified") is not True
            or local.get("sourceSha256") != hashlib.sha256(raw).hexdigest()
            or local.get("policy") != "indexer-reported-public-block-summary"
            or local.get("nativeCounts") != [3, 2] or local.get("federationCounts") != [5, 4]
            or local.get("foreignPrivate") != 0 or local.get("foreignPublic") != 3
            or local.get("savedPrivateCopies") != 2
            or local.get("fixtureCleaned") is not True):
        raise RuntimeError("Original source-bound Zcash local HTTP/privacy qualification required.")
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
    review = {"changedKeys": KEYS, "profiles": {"ZcashOASIS": source},
        "examples": {"ZcashOASIS": json.loads(after[KEYS[2]])},
        "dnaUnchanged": before["OASIS_DNA_JSON"] == after["OASIS_DNA_JSON"], "cloudSpend": 0,
        "policy": "indexer-reported-public-block-summary", "independentConsensusVerification": False, "reliabilityCertified": False}
    target = ROOT / "TestResults/ZcashPublicSourceCurrent/prepared-review.json"
    configuration.write_new(target, (json.dumps(review, indent=2) + "\n").encode())
    print("Protected Zcash public block-summary candidate prepared: three values, four presets; no service mutation or deployment.")


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
