"""Prepare a free public D1 Worker symbol CQL profile; apply only to the exact deployed release.

Source properties, permissions, tariffs and presets are explicit. Native account
configuration is unchanged. The shared publisher owns journals and observations.
"""
from pathlib import Path
import copy
import hashlib
import importlib.util
import ipaddress
import json
import sys
import uuid
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[2]
PREFIX = "d1-worker-symbols"
KEYS = ["CQL_D1_WORKER_SYMBOL_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON",
        "CQL_HOSTED_QUERY_EXAMPLES__CloudflareD1OASIS"]
EXPECTED_KEYS = ["BTC-USDT", "ETH-USDT", "SOL-USDT"]


def module(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


configuration = module("Publish-HostedSolidConfiguration")


def validate_endpoint(url):
    endpoint = urlsplit(url)
    host = endpoint.hostname
    if (endpoint.scheme != "https" or not host or host.rstrip(".") == "localhost"
            or endpoint.path != "/symbols" or endpoint.username is not None
            or endpoint.query or endpoint.fragment):
        raise ValueError("The qualified public HTTPS D1 Worker endpoint is required.")
    try:
        address = ipaddress.ip_address(host)
    except ValueError:
        if "." not in host or host.rstrip(".").endswith(".localhost"):
            raise ValueError("A public D1 Worker host is required.")
    else:
        if not address.is_global:
            raise ValueError("A public D1 Worker address is required.")

def candidate(before, source):
    if (set(source) != {"schema", "endpoint", "symbols", "sourceOwner"}
            or type(source["schema"]) is not int or source["schema"] != 1
            or source["symbols"] != EXPECTED_KEYS
            or not isinstance(source["endpoint"], str)
            or not isinstance(source["sourceOwner"], str)
            or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("The exact qualified D1 Worker symbol catalogue is required.")
    validate_endpoint(source["endpoint"])
    if KEYS[0] in before:
        raise ValueError("Existing D1 Worker symbol enrollment requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "D1Symbol" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit D1Symbol queryable policy before enrollment.")

    after = copy.deepcopy(before)
    admission = json.loads(before[KEYS[1]])
    provider = "CloudflareD1OASIS"
    if (provider in admission["queryOnlyProviders"] or provider in admission["ordinaryProviders"]
            or any(row["provider"] == provider for row in admission["nativeProviders"])
            or any(row["provider"] == provider for row in admission["queryTariffs"])):
        raise ValueError("Existing D1 Worker symbol admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append(provider)
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = provider
    admission["queryTariffs"].append(tariff)
    shared = json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}"))
    if provider in shared or KEYS[2] in before:
        raise ValueError("Existing D1 Worker symbol examples require reconciliation.")

    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["D1Symbol"], "scalar": "String", "operator": "In",
                "value": EXPECTED_KEYS[:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "D1 symbols (" + str(count) + " symbols)",
            "request": {"provider": provider, "maximumDurationMilliseconds": 30000, **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "D1 symbols + my Mongo records",
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
    path = ROOT / "LocalState/COSMIC/D1WorkerHostedQualificationCurrent/source.json"
    raw = path.read_bytes()
    proof = json.loads(path.with_name("worker-https-qualified.acknowledged.json").read_text(encoding="utf-8"))
    if (proof.get("schema") != 1
            or proof.get("sourceSha256") != hashlib.sha256(raw).hexdigest()
            or proof.get("fullSymbols") != EXPECTED_KEYS
            or proof.get("subsetSymbols") != EXPECTED_KEYS[:2]
            or proof.get("duplicateStatus") != 400 or proof.get("unenrolledStatus") != 400
            or proof.get("writeStatus") != 405):
        raise RuntimeError("Source-bound HTTPS qualification is required before D1 enrollment.")
    return json.loads(raw)

def prepare():
    source = qualified_source()
    before = configuration.variables()
    after = candidate(before, source)
    state = configuration.STATE
    state.mkdir(parents=True, exist_ok=True)
    snapshot = state / (PREFIX + "-before-variables.dpapi")
    if snapshot.exists():
        if configuration.protected_read(snapshot.name) != before:
            raise RuntimeError("Baseline changed; reconcile before another candidate.")
    else:
        configuration.write_new(snapshot, configuration.verification.protected_token(configuration.compact(before).encode()))
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    review = {"changedKeys": KEYS, "profiles": {"CloudflareD1OASIS": source},
        "examples": {"CloudflareD1OASIS": json.loads(after[KEYS[2]])},
        "dnaUnchanged": before["OASIS_DNA_JSON"] == after["OASIS_DNA_JSON"], "cloudSpend": 0,
        "policy": "d1-worker-symbols", "hostedCqlVerified": False}
    target = ROOT / "TestResults/D1PublicSourceCurrent/prepared-review.json"
    target.parent.mkdir(parents=True, exist_ok=True)
    configuration.write_new(target, (json.dumps(review, indent=2) + "\n").encode())
    print("Protected D1 Worker symbol candidate prepared: three values, four presets; no service mutation or deployment.")


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
