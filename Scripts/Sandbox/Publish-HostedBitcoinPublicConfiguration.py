"""Stage an explicit public Bitcoin read profile; never change canonical wallet DNA.

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
PREFIX = "bitcoin-public"
KEYS = ["CQL_BITCOIN_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES__BitcoinOASIS"]
spec = importlib.util.spec_from_file_location("configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


def candidate(before, source):
    if set(source) != {"schema", "rpcUrl", "genesisHash", "blockHashes", "sourceOwner"} or type(source["schema"]) is not int or source["schema"] != 1:
        raise ValueError("Exact anonymous source schema required; wallet/signer fields forbidden.")
    if (source["rpcUrl"] != "https://blockstream.info/api/" or source["genesisHash"] != "000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f" or
            source["blockHashes"] != ['000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f', '00000000839a8e6886ab5951d76f411475428afc90947ee320161bbf18eb6048', '000000006a625f06636b8bb6ac7b960a8d03705d1ace08b1a19da3fdcc99ddbd'] or
            not isinstance(source["sourceOwner"], str) or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("Retained verified Bitcoin mainnet catalogue differs.")
    if "CQL_BITCOIN_PUBLIC_SOURCE_JSON" in before:
        raise ValueError("Existing Bitcoin query profile requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "BitcoinBlockHash" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit BitcoinBlockHash queryable policy before enrollment.")
    after = copy.deepcopy(before)
    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if ("BitcoinOASIS" in admission["queryOnlyProviders"] or
            "BitcoinOASIS" in admission["ordinaryProviders"] or
            any(row["provider"] == "BitcoinOASIS" for row in admission["nativeProviders"]) or
            any(row["provider"] == "BitcoinOASIS" for row in admission["queryTariffs"])):
        raise ValueError("Existing Bitcoin admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append("BitcoinOASIS")
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = "BitcoinOASIS"
    admission["queryTariffs"].append(tariff)
    examples = json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}"))
    if "BitcoinOASIS" in examples or KEYS[2] in before:
        raise ValueError("Existing Bitcoin examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["BitcoinBlockHash"], "scalar": "String", "operator": "In",
                "value": source["blockHashes"][:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Bitcoin public block headers (" + str(count) + " headers)",
            "request": {"provider": "BitcoinOASIS", **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Bitcoin public metadata + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "BitcoinOASIS"],
                "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2, "take": 25}})
    after[KEYS[0]] = configuration.compact(source)
    after[KEYS[1]] = configuration.compact(admission)
    after[KEYS[2]] = configuration.compact(rows)
    if {key for key in after if before.get(key) != after[key]} != set(KEYS):
        raise ValueError("Unexpected configuration changes.")
    if after["OASIS_DNA_JSON"] != before["OASIS_DNA_JSON"]:
        raise ValueError("Canonical DNA must remain byte-for-byte unchanged.")
    if any(len(after[key]) > 32768 for key in KEYS):
        raise ValueError("Hosted variable character limit exceeded.")
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
    source = json.loads((ROOT / "LocalState/COSMIC/HostedBitcoinPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
    after = candidate(before, source)
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    review = {"profiles": {"BitcoinOASIS": source},
        "examples": {"BitcoinOASIS": json.loads(after[KEYS[2]])}}
    target = ROOT / "TestResults/BitcoinPublicSourceCandidateCurrent/bitcoin-prepared-review.json"
    target.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
    print("Protected Bitcoin candidate prepared: three values/four presets; DNA unchanged; no mutation or deployment.")


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["apply"]:
        spec = importlib.util.spec_from_file_location("release_guard", Path(__file__).with_name("Prepare-TenSourcePublicBatch.py"))
        guard = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(guard)
        guard.require_matching_release(configuration)
        configuration.apply_values(PREFIX, KEYS)
    else:
        raise SystemExit("Use no arguments to prepare, or apply for the reviewed candidate.")
