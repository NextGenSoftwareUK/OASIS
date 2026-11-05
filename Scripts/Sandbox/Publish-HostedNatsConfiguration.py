"""Prepare a free public NATS JetStream record CQL profile; apply only to the exact deployed release.

Source properties, permissions, tariffs and presets are explicit. Native account
configuration is unchanged. The shared publisher owns journals and observations.
"""
from pathlib import Path
import copy
import hashlib
import importlib.util
import ipaddress
import re
import json
import sys
import uuid
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[2]
PREFIX = "nats-public-records"
KEYS = ["CQL_NATS_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON",
        "CQL_HOSTED_QUERY_EXAMPLES__NATSJetStreamOASIS", "CQL_PUBLIC_READ_CATALOGUES_JSON"]


def module(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


configuration = module("Publish-HostedSolidConfiguration")


def candidate(before, source):
    if (set(source) != {"schema", "endpoint", "bucket", "ids", "sourceOwner"}
            or type(source["schema"]) is not int or source["schema"] != 1
            or not isinstance(source["ids"], list) or len(source["ids"]) != 3
            or not isinstance(source["bucket"], str) or not re.fullmatch(r"[A-Za-z0-9_-]{1,64}", source["bucket"])
            or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("Exact NATS source configuration required.")
    ids = source["ids"]
    if len(set(ids)) != 3 or any(uuid.UUID(id).int == 0 or str(uuid.UUID(id)) != id for id in ids):
        raise ValueError("Three distinct canonical record IDs required.")
    endpoint = urlsplit(source["endpoint"])
    host = endpoint.hostname or ""
    try:
        address = ipaddress.ip_address(host)
    except ValueError:
        address = None
    if (endpoint.scheme != "tls" or not host or host.rstrip(".") == "localhost"
            or (address is not None and address.is_loopback)
            or endpoint.port == 0
            or endpoint.username is not None or endpoint.path not in ("", "/")
            or endpoint.query or endpoint.fragment or "," in source["endpoint"]):
        raise ValueError("An explicit TLS NATS endpoint without credentials is required.")
    if KEYS[0] in before:
        raise ValueError("Existing NATS JetStream record enrollment requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    after = copy.deepcopy(before)
    admission = json.loads(before[KEYS[1]])
    provider = "NATSJetStreamOASIS"
    if (provider in admission["queryOnlyProviders"] or provider in admission["ordinaryProviders"]
            or any(row["provider"] == provider for row in admission["nativeProviders"])
            or any(row["provider"] == provider for row in admission["queryTariffs"])):
        raise ValueError("Existing NATS JetStream record admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append(provider)
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = provider
    admission["queryTariffs"].append(tariff)
    shared = json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}"))
    if provider in shared or KEYS[2] in before:
        raise ValueError("Existing NATS JetStream record examples require reconciliation.")

    catalogues = json.loads(before.get(KEYS[3], "{}"))
    if provider in catalogues:
        raise ValueError("Existing NATS public catalogue requires reconciliation.")
    catalogues[provider] = ids
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"field": "Id", "scalar": "Guid", "operator": "In",
                "value": ids[:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "NATS records (" + str(count) + " symbols)",
            "request": {"provider": provider, "maximumDurationMilliseconds": 30000, **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "NATS records + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", provider],
                "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2,
                "maximumDurationMilliseconds": 30000, "take": 25}})
    after[KEYS[0]] = configuration.compact(source)
    after[KEYS[1]] = configuration.compact(admission)
    after[KEYS[2]] = configuration.compact(rows)
    after[KEYS[3]] = configuration.compact(catalogues)
    if ({key for key in before.keys() | after.keys() if before.get(key) != after.get(key)} != set(KEYS)
            or any(key not in after for key in before)):
        raise ValueError("Exactly four enrolled values may change; existing values cannot be removed.")
    if any(len(after[key]) > 32768 for key in KEYS):
        raise ValueError("Hosted variable size budget exceeded.")
    return after


def qualified_source():
    path = ROOT / "LocalState/COSMIC/NatsHostedQualificationCurrent/source.json"
    raw = path.read_bytes()
    proof = json.loads(path.with_name("tls-qualified.acknowledged.json").read_text(encoding="utf-8"))
    source = json.loads(raw)
    if (proof.get("schema") != 1 or proof.get("sourceSha256") != hashlib.sha256(raw).hexdigest()
            or proof.get("fullIds") != source["ids"] or proof.get("subsetIds") != source["ids"][:2]
            or proof.get("readOnly") is not True):
        raise RuntimeError("Source-bound TLS and read-only qualification required before NATS enrollment.")
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
    review = {"changedKeys": KEYS, "profiles": {"NATSJetStreamOASIS": source},
        "examples": {"NATSJetStreamOASIS": json.loads(after[KEYS[2]])},
        "dnaUnchanged": before["OASIS_DNA_JSON"] == after["OASIS_DNA_JSON"], "cloudSpend": 0,
        "policy": "nats-public-records", "hostedCqlVerified": False}
    target = ROOT / "TestResults/NatsPublicSourceCurrent/prepared-review.json"
    target.parent.mkdir(parents=True, exist_ok=True)
    configuration.write_new(target, (json.dumps(review, indent=2) + "\n").encode())
    print("Protected NATS JetStream record candidate prepared: four values, four presets; no service mutation or deployment.")


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
