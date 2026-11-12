"""Stage an explicit public Arbitrum read profile; never change canonical wallet DNA.

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
PREFIX = "arbitrum-public"
KEYS = ["CQL_ARBITRUM_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES_JSON"]
spec = importlib.util.spec_from_file_location("configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


def candidate(before, source):
    if set(source) != {"schema", "rpcUrl", "chainId", "addresses", "sourceOwner"} or type(source["schema"]) is not int or source["schema"] != 1:
        raise ValueError("Exact anonymous source schema required; wallet/signer fields forbidden.")
    if (source["rpcUrl"] != "https://arb1.arbitrum.io/rpc" or source["chainId"] != "0xa4b1" or
            source["addresses"] != ["0x" + format(value, "040x") for value in (1, 2, 3)] or
            not isinstance(source["sourceOwner"], str) or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("Retained verified Arbitrum mainnet catalogue differs.")
    if "CQL_ARBITRUM_PUBLIC_SOURCE_JSON" in before:
        raise ValueError("Existing Arbitrum query profile requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "ArbitrumAccountAddress" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit ArbitrumAccountAddress queryable policy before enrollment.")
    after = copy.deepcopy(before)
    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if ("ArbitrumOASIS" in admission["queryOnlyProviders"] or
            "ArbitrumOASIS" in admission["ordinaryProviders"] or
            any(row["provider"] == "ArbitrumOASIS" for row in admission["nativeProviders"]) or
            any(row["provider"] == "ArbitrumOASIS" for row in admission["queryTariffs"])):
        raise ValueError("Existing Arbitrum admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append("ArbitrumOASIS")
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = "ArbitrumOASIS"
    admission["queryTariffs"].append(tariff)
    examples = json.loads(before["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
    if "ArbitrumOASIS" in examples:
        raise ValueError("Existing Arbitrum examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["ArbitrumAccountAddress"], "scalar": "String", "operator": "In",
                "value": source["addresses"][:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Arbitrum public address balances (" + str(count) + " addresses)",
            "request": {"provider": "ArbitrumOASIS", **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Arbitrum public metadata + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "ArbitrumOASIS"],
                "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2, "take": 25}})
    examples["ArbitrumOASIS"] = rows
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
    source = json.loads((ROOT / "LocalState/COSMIC/HostedArbitrumPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
    after = candidate(before, source)
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    print("Protected Arbitrum candidate prepared: three values/four presets; DNA unchanged; no mutation or deployment.")


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["apply"]:
        configuration.apply_values(PREFIX, KEYS)
    else:
        raise SystemExit("Use no arguments to prepare, or apply for the reviewed candidate.")
