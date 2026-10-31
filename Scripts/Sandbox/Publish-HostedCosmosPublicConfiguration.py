"""Stage an explicit public Cosmos read profile; never change canonical wallet DNA.

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
PREFIX = "cosmos-public"
KEYS = ["CQL_COSMOS_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES_JSON"]
spec = importlib.util.spec_from_file_location("configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


def candidate(before, source):
    if set(source) != {"schema", "restUrl", "chainId", "addresses", "sourceOwner"} or type(source["schema"]) is not int or source["schema"] != 1:
        raise ValueError("Exact anonymous source schema required; wallet/signer fields forbidden.")
    if (source["restUrl"] != "https://cosmoshub.api.kjnodes.com/" or source["chainId"] != "cosmoshub-4" or
            source["addresses"] != ["cosmosvaloper1qphf0ferqcch0jca9hlqfm3x0eds3dpkcvpafp",
                "cosmosvaloper1qr6sk28w4r6kqsg0737wzgu05505t4glezetwn",
                "cosmosvaloper1q9p73lx07tjqc34vs8jrsu5pg3q4ha534uqv4w"] or
            not isinstance(source["sourceOwner"], str) or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("Retained verified Cosmos mainnet catalogue differs.")
    if "CQL_COSMOS_PUBLIC_SOURCE_JSON" in before:
        raise ValueError("Existing Cosmos query profile requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "CosmosValidatorAddress" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit CosmosValidatorAddress queryable policy before enrollment.")
    after = copy.deepcopy(before)
    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if ("CosmosBlockChainOASIS" in admission["queryOnlyProviders"] or
            "CosmosBlockChainOASIS" in admission["ordinaryProviders"] or
            any(row["provider"] == "CosmosBlockChainOASIS" for row in admission["nativeProviders"]) or
            any(row["provider"] == "CosmosBlockChainOASIS" for row in admission["queryTariffs"])):
        raise ValueError("Existing Cosmos admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append("CosmosBlockChainOASIS")
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = "CosmosBlockChainOASIS"
    admission["queryTariffs"].append(tariff)
    examples = json.loads(before["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
    if "CosmosBlockChainOASIS" in examples:
        raise ValueError("Existing Cosmos examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["CosmosValidatorAddress"], "scalar": "String", "operator": "In",
                "value": source["addresses"][:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Cosmos public validators (" + str(count) + " validators)",
            "request": {"provider": "CosmosBlockChainOASIS", **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Cosmos public metadata + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "CosmosBlockChainOASIS"],
                "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2, "take": 25}})
    examples["CosmosBlockChainOASIS"] = rows
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
    source = json.loads((ROOT / "LocalState/COSMIC/HostedCosmosPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
    after = candidate(before, source)
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    print("Protected Cosmos candidate prepared: three values/four presets; DNA unchanged; no mutation or deployment.")


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["apply"]:
        configuration.apply_values(PREFIX, KEYS)
    else:
        raise SystemExit("Use no arguments to prepare, or apply for the reviewed candidate.")
