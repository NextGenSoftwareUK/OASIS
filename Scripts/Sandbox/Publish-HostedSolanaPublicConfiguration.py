"""Stage an explicit public Solana read profile; never change canonical wallet DNA.

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
PREFIX = "solana-public"
KEYS = ["CQL_SOLANA_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES_JSON"]
spec = importlib.util.spec_from_file_location("configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


def candidate(before, source):
    if set(source) != {"schema", "rpcUrl", "genesisHash", "addresses", "sourceOwner"} or source["schema"] != 1:
        raise ValueError("Explicit public source schema required; signer fields are forbidden.")
    if (source["rpcUrl"] != "https://api.mainnet-beta.solana.com" or
            source["genesisHash"] != "5eykt4UsFv8P8NJdTREpY1vzqKqZKvdpKuc147dw2N9d" or
            source["addresses"] != ["SysvarC1ock11111111111111111111111111111111",
                "SysvarRent111111111111111111111111111111111", "SysvarEpochSchedu1e111111111111111111111111"] or
            uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("Retained official public metadata catalogue differs.")
    if "CQL_SOLANA_PUBLIC_SOURCE_JSON" in before:
        raise ValueError("Existing Solana query profile requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    after = copy.deepcopy(before)
    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if ("SolanaOASIS" in admission["queryOnlyProviders"] or
            "SolanaOASIS" in admission["ordinaryProviders"] or
            any(row["provider"] == "SolanaOASIS" for row in admission["nativeProviders"]) or
            any(row["provider"] == "SolanaOASIS" for row in admission["queryTariffs"])):
        raise ValueError("Existing Solana admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append("SolanaOASIS")
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = "SolanaOASIS"
    admission["queryTariffs"].append(tariff)
    examples = json.loads(before["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
    if "SolanaOASIS" in examples:
        raise ValueError("Existing Solana examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["SolanaAccountAddress"], "scalar": "String", "operator": "In",
                "value": source["addresses"][:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Solana public account metadata (" + str(count) + " accounts)",
            "request": {"provider": "SolanaOASIS", **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Solana public metadata + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "SolanaOASIS"],
                "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2, "take": 25}})
    examples["SolanaOASIS"] = rows
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
    source = json.loads((ROOT / "LocalState/COSMIC/HostedSolanaPublicSourceCurrent/source.json").read_text())
    after = candidate(before, source)
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    print("Protected Solana candidate prepared: three values/four presets; DNA unchanged; no mutation or deployment.")


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["apply"]:
        configuration.apply_values(PREFIX, KEYS)
    else:
        raise SystemExit("Use no arguments to prepare, or apply for the reviewed candidate.")
