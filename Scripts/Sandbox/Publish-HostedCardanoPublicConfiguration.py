"""Stage an explicit public Cardano read profile; never change canonical wallet DNA.

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
PREFIX = "cardano-public"
KEYS = ["CQL_CARDANO_PUBLIC_SOURCE_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES__CardanoOASIS"]
spec = importlib.util.spec_from_file_location("configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


def candidate(before, source):
    if (set(source) != {"schema", "restUrl", "networkMagic", "blockHashes", "sourceOwner"}
            or type(source["schema"]) is not int or source["schema"] != 1):
        raise ValueError("Exact anonymous source schema required; wallet/signer fields forbidden.")
    if (source["restUrl"] != "https://api.koios.rest/api/v1/"
            or source["networkMagic"] != "764824073"
            or source["blockHashes"] != ['fb9087c9f1408a7bbd7b022fd294ab565fec8dd3a8ef091567482722a1fa4e30', '60188a8dcb6db0d80628815be2cf626c4d17cb3e826cebfca84adaff93ad492a', 'c6646214a1f377aa461a0163c213fc6b86a559a2d6ebd647d54c4eb00aaab015']
            or not isinstance(source["sourceOwner"], str) or uuid.UUID(source["sourceOwner"]).int == 0):
        raise ValueError("Retained historical Cardano header catalogue differs.")
    if "CQL_CARDANO_PUBLIC_SOURCE_JSON" in before:
        raise ValueError("Existing Cardano query profile requires reconciliation, never replacement.")
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Explicit sandbox required.")
    encryption = (dna["OASIS"].get("Security") or {}).get("HolonDataEncryption") or {}
    if ((encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and "CardanoBlockHash" not in (encryption.get("AdditionalQueryableKeys") or [])):
        raise ValueError("Encrypted snapshots require an explicit CardanoBlockHash queryable policy before enrollment.")
    after = copy.deepcopy(before)
    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if ("CardanoOASIS" in admission["queryOnlyProviders"] or
            "CardanoOASIS" in admission["ordinaryProviders"] or
            any(row["provider"] == "CardanoOASIS" for row in admission["nativeProviders"]) or
            any(row["provider"] == "CardanoOASIS" for row in admission["queryTariffs"])):
        raise ValueError("Existing Cardano admission requires explicit reconciliation.")
    admission["queryOnlyProviders"].append("CardanoOASIS")
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free public demo reads are authorized.")
    tariff["provider"] = "CardanoOASIS"
    admission["queryTariffs"].append(tariff)
    examples = json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}"))
    if "CardanoOASIS" in examples or KEYS[2] in before:
        raise ValueError("Existing Cardano examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"metadataPath": ["CardanoBlockHash"], "scalar": "String", "operator": "In",
                "value": source["blockHashes"][:count]}]}
        suffix = "records" if count == 3 else "subset"
        rows.append({"id": "public-" + suffix, "label": "Cardano historical indexed headers (" + str(count) + " headers)",
            "request": {"provider": "CardanoOASIS", **selection}})
        rows.append({"id": "mongo-public-" + suffix, "label": "Cardano public metadata + my Mongo records",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "CardanoOASIS"],
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
    source = json.loads((ROOT / "LocalState/COSMIC/HostedCardanoPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
    after = candidate(before, source)
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    review = {"profiles": {"CardanoOASIS": source},
        "examples": {"CardanoOASIS": json.loads(after[KEYS[2]])}}
    target = ROOT / "TestResults/CardanoPublicSourceCandidateCurrent/cardano-prepared-review.json"
    target.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
    print("Protected Cardano candidate prepared: three values/four presets; DNA unchanged; no mutation or deployment.")


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
