"""Stage an explicit public Polkadot read profile; never change canonical wallet DNA.

Default prepares a protected candidate only. Apply uses the existing journalled
publisher without triggering deployment. Unknown outcomes must be reconciled.
"""
from pathlib import Path
import copy
import importlib.util
import json
import sys
import uuid

ROOT = Path(__file__).resolve().parents[2]
PREFIX = "polkadot-public"
KEYS = ["CQL_POLKADOT_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES_JSON"]
spec = importlib.util.spec_from_file_location("configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


def candidate(before, source):
    if set(source) != {"schema", "rpcUrl", "genesisHash", "blockHashes", "sourceOwner"} or type(source["schema"]) is not int or source["schema"] != 1:
        raise ValueError("Exact anonymous source schema required; wallet/signer fields forbidden.")
    if (source["rpcUrl"] != "https://rpc.polkadot.io/" or source["genesisHash"] != "0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3" or
            source["blockHashes"] != ["0x72c606b7188cead0ec845a42ec33eb28417dc3d8c46d4c234ef48883d2a3c4ee", "0xedc6be196a342694de69b7835fcf585ca875313faa2591021c8d8bdd08e30788", "0xe23b02b4ea9cf23aae2d6f22248f7cd7c3af345f08feb6ce5e3a135d0cd4e08b"] or
            not isinstance(source["sourceOwner"], str) or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("Retained verified Polkadot mainnet catalogue differs.")
    if "CQL_POLKADOT_PUBLIC_SOURCE_JSON" in before:
        raise ValueError("Existing Polkadot query profile requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "PolkadotBlockHash" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit PolkadotBlockHash queryable policy before enrollment.")
    after = copy.deepcopy(before)
    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if ("PolkadotOASIS" in admission["queryOnlyProviders"] or
            "PolkadotOASIS" in admission["ordinaryProviders"] or
            any(row["provider"] == "PolkadotOASIS" for row in admission["nativeProviders"]) or
            any(row["provider"] == "PolkadotOASIS" for row in admission["queryTariffs"])):
        raise ValueError("Existing Polkadot admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append("PolkadotOASIS")
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = "PolkadotOASIS"
    admission["queryTariffs"].append(tariff)
    examples = json.loads(before["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
    if "PolkadotOASIS" in examples:
        raise ValueError("Existing Polkadot examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["PolkadotBlockHash"], "scalar": "String", "operator": "In",
                "value": source["blockHashes"][:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Polkadot public headers (" + str(count) + " headers)",
            "request": {"provider": "PolkadotOASIS", **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Polkadot public metadata + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "PolkadotOASIS"],
                "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2, "take": 25}})
    examples["PolkadotOASIS"] = rows
    after[KEYS[0]] = configuration.compact(source)
    after[KEYS[1]] = configuration.compact(admission)
    after[KEYS[2]] = configuration.compact(examples)
    if {key for key in after if before.get(key) != after[key]} != set(KEYS):
        raise ValueError("Unexpected configuration changes.")
    if after["OASIS_DNA_JSON"] != before["OASIS_DNA_JSON"]:
        raise ValueError("Canonical DNA must remain byte-for-byte unchanged.")
    return after


def prepare():
    state = configuration.STATE
    snapshot = state / (PREFIX + "-before-variables.dpapi")
    before = configuration.variables()
    if snapshot.exists():
        if configuration.protected_read(snapshot.name) != before:
            raise RuntimeError("Configuration changed since snapshot; reconcile first.")
    else:
        configuration.write_new(snapshot, configuration.verification.protected_token(configuration.compact(before).encode()))
    source = json.loads((ROOT / "LocalState/COSMIC/HostedPolkadotPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
    after = candidate(before, source)
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    print("Protected Polkadot candidate prepared: three values/four presets; DNA unchanged; no mutation or deployment.")


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["apply"]:
        configuration.apply_values(PREFIX, KEYS)
    else:
        raise SystemExit("Use no arguments to prepare, or apply for the reviewed candidate.")
