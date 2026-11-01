"""Stage an explicit public Ton read profile; never change canonical wallet DNA.

Default prepares a protected candidate only. Apply uses the existing journalled
publisher without triggering deployment. Unknown outcomes must be reconciled.
"""
from pathlib import Path
import copy
import hashlib
import importlib.util
import json
import sys
import uuid

ROOT = Path(__file__).resolve().parents[2]
PREFIX = "ton-public"
KEYS = ["CQL_TON_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES__TONOASIS"]
spec = importlib.util.spec_from_file_location("configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


def candidate(before, source):
    if (set(source) != {"schema", "restUrl", "globalId", "blocks", "sourceOwner"}
            or type(source["schema"]) is not int or source["schema"] != 1):
        raise ValueError("Exact anonymous source schema required; wallet/signer fields forbidden.")
    if (source["restUrl"] != "https://toncenter.com/api/v2/"
            or type(source["globalId"]) is not int or source["globalId"] != -239
            or source["blocks"] != [{'fileHash': 'fm6uQx6+r+QPcjvxtYiKq2RYdJ14If7BcskBcBGpFAY=', 'seqno': 102765318, 'rootHash': 'Y9wDd3pZpST/McLYMsPERk6s7WO6+p19kJ36oNcBOc8='}, {'fileHash': '8rGzd/bbQwbuEk76O1OyobR3vZoBnR6GWVbx4AZkLRc=', 'seqno': 102765319, 'rootHash': 'gzkgCwmATjDXVh41RK0tjESsu0V+EG0ApXxQe6zsOP4='}, {'fileHash': 'Y2mAEz86fWZD6YToEZrMbPnTg9aB0ZTRqX+RFwJVjKk=', 'seqno': 102765320, 'rootHash': 'Xhj82aH+7pa4qHMEe2NCR2/O0gzJy/nAL/VrYi0cLTU='}]
            or any(type(block["seqno"]) is not int for block in source["blocks"])
            or not isinstance(source["sourceOwner"], str) or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("Retained qualified recent TON header catalogue differs.")
    if "CQL_TON_PUBLIC_SOURCE_JSON" in before:
        raise ValueError("Existing Ton query profile requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "TonBlockRootHash" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit TonBlockRootHash queryable policy before enrollment.")
    after = copy.deepcopy(before)
    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if ("TONOASIS" in admission["queryOnlyProviders"] or
            "TONOASIS" in admission["ordinaryProviders"] or
            any(row["provider"] == "TONOASIS" for row in admission["nativeProviders"]) or
            any(row["provider"] == "TONOASIS" for row in admission["queryTariffs"])):
        raise ValueError("Existing Ton admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append("TONOASIS")
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = "TONOASIS"
    admission["queryTariffs"].append(tariff)
    examples = json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}"))
    if "TONOASIS" in examples or KEYS[2] in before:
        raise ValueError("Existing Ton examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["TonBlockRootHash"], "scalar": "String", "operator": "In",
                "value": [block["rootHash"] for block in source["blocks"][:count]]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Ton historical gateway headers (" + str(count) + " headers)",
            "request": {"provider": "TONOASIS", "maximumDurationMilliseconds": 30000, **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Ton public metadata + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "TONOASIS"],
                "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2, "maximumDurationMilliseconds": 30000, "take": 25}})
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
    source_path = ROOT / "LocalState/COSMIC/HostedTonPublicSourceCurrent/source.json"
    source_bytes = source_path.read_bytes()
    acknowledgement = json.loads(source_path.with_name("source-migration.acknowledged.json").read_text(encoding="utf-8"))
    if (acknowledgement.get("verified") is not True or acknowledgement.get("sourceOwnerPreserved") is not True
            or hashlib.sha256(source_bytes).hexdigest() != acknowledgement.get("newSha256")):
        raise RuntimeError("Explicit qualified source promotion acknowledgement required; never replace the owner/catalogue.")
    source = json.loads(source_bytes)
    after = candidate(before, source)
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    review = {"profiles": {"TONOASIS": source},
        "examples": {"TONOASIS": json.loads(after[KEYS[2]])}}
    target = ROOT / "TestResults/TonPublicSourceCandidateCurrent/ton-prepared-review.json"
    target.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
    print("Protected Ton candidate prepared: three values/four presets; DNA unchanged; no mutation or deployment.")


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
