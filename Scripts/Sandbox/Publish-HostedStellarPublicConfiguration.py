"""Stage an explicit public Stellar read profile; never change canonical wallet DNA.

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
PREFIX = "stellar-public"
KEYS = ["CQL_STELLAR_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES__StellarOASIS"]
spec = importlib.util.spec_from_file_location("configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


def candidate(before, source):
    if set(source) != {"schema", "horizonUrl", "networkPassphrase", "ledgers", "sourceOwner"} or type(source["schema"]) is not int or source["schema"] != 1:
        raise ValueError("Exact anonymous source schema required; wallet/signer fields forbidden.")
    if (source["horizonUrl"] != "https://horizon.stellar.org/" or
            source["networkPassphrase"] != "Public Global Stellar Network ; September 2015" or
            source["ledgers"] != [{'sequence': 65247993, 'hash': 'c8deae88416e676c30358bef81cfbb7aa78546a7713d3c252653591e3c3340ce'}, {'sequence': 65247992, 'hash': '4cc75f196c333bcc325fc90d1a42ea39209513e7ad1696c1866573d90a74d768'}, {'sequence': 65247991, 'hash': 'd898b8941c4cb2d0c03d5cf5fac6fb18b52888102fdb3283c26b779a039054d9'}] or
            not isinstance(source["sourceOwner"], str) or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("Retained verified Stellar mainnet catalogue differs.")
    if "CQL_STELLAR_PUBLIC_SOURCE_JSON" in before:
        raise ValueError("Existing Stellar query profile requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "StellarLedgerSequence" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit StellarLedgerSequence queryable policy before enrollment.")
    after = copy.deepcopy(before)
    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if ("StellarOASIS" in admission["queryOnlyProviders"] or
            "StellarOASIS" in admission["ordinaryProviders"] or
            any(row["provider"] == "StellarOASIS" for row in admission["nativeProviders"]) or
            any(row["provider"] == "StellarOASIS" for row in admission["queryTariffs"])):
        raise ValueError("Existing Stellar admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append("StellarOASIS")
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = "StellarOASIS"
    admission["queryTariffs"].append(tariff)
    examples = json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}"))
    if "StellarOASIS" in examples or KEYS[2] in before:
        raise ValueError("Existing Stellar examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["StellarLedgerSequence"], "scalar": "String", "operator": "In",
                "value": [str(row["sequence"]) for row in source["ledgers"][:count]]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Stellar public ledger headers (" + str(count) + " headers)",
            "request": {"provider": "StellarOASIS", **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Stellar public metadata + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "StellarOASIS"],
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
    source = json.loads((ROOT / "LocalState/COSMIC/HostedStellarPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
    after = candidate(before, source)
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    review = {"profiles": {"StellarOASIS": source},
        "examples": {"StellarOASIS": json.loads(after[KEYS[2]])}}
    target = ROOT / "TestResults/StellarPublicSourceCandidateCurrent/stellar-prepared-review.json"
    target.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
    print("Protected Stellar candidate prepared: three values/four presets; DNA unchanged; no mutation or deployment.")


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
