"""Stage an explicit public Algorand read profile; never change canonical wallet DNA.

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
PREFIX = "algorand-public"
KEYS = ["CQL_ALGORAND_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES__AlgorandOASIS"]
spec = importlib.util.spec_from_file_location("configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


def candidate(before, source):
    if set(source) != {"schema", "algodUrl", "indexerUrl", "genesisId", "genesisHash", "consistency", "accounts", "sourceOwner"} or type(source["schema"]) is not int or source["schema"] != 1:
        raise ValueError("Exact anonymous source schema required; wallet/signer fields forbidden.")
    if (source["algodUrl"] != "https://mainnet-api.algonode.cloud/" or
            source["indexerUrl"] != "https://mainnet-idx.algonode.cloud/" or
            source["genesisId"] != "mainnet-v1.0" or
            source["genesisHash"] != "wGHE2Pwdvd7S12BL5FaOP20EGYesN73ktiC1qzkkit8=" or
            source["consistency"] != "Current" or
            source["accounts"] != ['AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAY5HFKQ', 'AAAAAAAAAATLOVN6WSNYBWSCZGEO4PS5BKS7KGB4PPJXA6WCTIII35HJZE', 'AAAAAAAAACV43GFSJHXLCG7ZLTNYZJJLUDOQT35CK4ZB74U3CGSAMNCUBA'] or
            not isinstance(source["sourceOwner"], str) or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("Retained current-account Algorand catalogue differs.")
    if "CQL_ALGORAND_PUBLIC_SOURCE_JSON" in before:
        raise ValueError("Existing Algorand query profile requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "AlgorandAddress" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit AlgorandAddress queryable policy before enrollment.")
    after = copy.deepcopy(before)
    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if ("AlgorandOASIS" in admission["queryOnlyProviders"] or
            "AlgorandOASIS" in admission["ordinaryProviders"] or
            any(row["provider"] == "AlgorandOASIS" for row in admission["nativeProviders"]) or
            any(row["provider"] == "AlgorandOASIS" for row in admission["queryTariffs"])):
        raise ValueError("Existing Algorand admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append("AlgorandOASIS")
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = "AlgorandOASIS"
    admission["queryTariffs"].append(tariff)
    examples = json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}"))
    if "AlgorandOASIS" in examples or KEYS[2] in before:
        raise ValueError("Existing Algorand examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["AlgorandAddress"], "scalar": "String", "operator": "In",
                "value": source["accounts"][:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Algorand current public accounts (" + str(count) + " accounts)",
            "request": {"provider": "AlgorandOASIS", **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Algorand public metadata + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "AlgorandOASIS"],
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
    source = json.loads((ROOT / "LocalState/COSMIC/HostedAlgorandPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
    after = candidate(before, source)
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    review = {"profiles": {"AlgorandOASIS": source},
        "examples": {"AlgorandOASIS": json.loads(after[KEYS[2]])}}
    target = ROOT / "TestResults/AlgorandPublicSourceCandidateCurrent/algorand-prepared-review.json"
    target.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
    print("Protected Algorand candidate prepared: three values/four presets; DNA unchanged; no mutation or deployment.")


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
