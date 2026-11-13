"""Stage the approved anonymous public S3 metadata source; no account or bucket writes."""
from pathlib import Path
import copy
import importlib.util
import json
import sys

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("hosted_configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)
PREFIX = "s3-public"
KEYS = ["OASIS_DNA_JSON", "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES_JSON"]


def prepare():
    state = configuration.STATE
    snapshot = state / (PREFIX + "-before-variables.dpapi")
    if snapshot.exists():
        before = configuration.protected_read(snapshot.name)
        if configuration.variables() != before:
            raise RuntimeError("Configuration changed since snapshot; reconcile before preparing.")
    else:
        before = configuration.variables()
        configuration.write_new(snapshot, configuration.verification.protected_token(configuration.compact(before).encode()))
    source = json.loads((ROOT / "LocalState/COSMIC/HostedAwsS3PublicSourceCurrent/source.json").read_text())
    if source.get("metadataOnly") is not True or source["region"] != "us-east-1" or source["bucket"] != "noaa-goes16" or len(source["keys"]) != 3:
        raise RuntimeError("The approved public metadata catalogue differs.")
    after = copy.deepcopy(before)
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise RuntimeError("Explicit hosted sandbox configuration required.")
    storage = dna["OASIS"]["StorageProviders"]
    if storage.get("AWSS3OASIS") not in (None, {}):
        raise RuntimeError("An existing S3 source requires explicit migration; never replace it.")
    storage["AWSS3OASIS"] = {"PublicObjectMetadataOnly": True, "Region": source["region"], "BucketName": source["bucket"],
                            "PublicObjectMetadataKeys": source["keys"], "PublicObjectMetadataOwner": source["owner"]}
    after["OASIS_DNA_JSON"] = configuration.compact(dna)

    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    if "AWSS3OASIS" in admission["queryOnlyProviders"] or any(item["provider"] == "AWSS3OASIS" for item in admission["queryTariffs"]):
        raise RuntimeError("S3 is already admitted; reconcile original enrollment.")
    admission["queryOnlyProviders"].append("AWSS3OASIS")
    tariff = copy.deepcopy(next(item for item in admission["queryTariffs"] if item["provider"] == "IPFSOASIS"))
    tariff["provider"] = "AWSS3OASIS"
    admission["queryTariffs"].append(tariff)
    after["PREPARED_STORAGE_ADMISSION_JSON"] = configuration.compact(admission)

    examples = json.loads(before["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
    if "AWSS3OASIS" in examples:
        raise RuntimeError("Existing S3 examples require reconciliation.")
    rows = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
                     "filters": [{"metadataPath": ["S3ObjectKey"], "scalar": "String", "operator": "In", "value": source["keys"][:count]}]}
        rows.append({"id": "public-records" if count == 3 else "public-subset",
                     "label": "Public S3 metadata (3 objects)" if count == 3 else "Public S3 metadata (2 objects)",
                     "request": {"provider": "AWSS3OASIS", **selection}})
        rows.append({"id": "mongo-public-records" if count == 3 else "mongo-public-subset",
                     "label": "S3 metadata + my Mongo records" if count == 3 else "Two-object metadata + my Mongo records",
                     "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", "AWSS3OASIS"],
                     "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2, "take": 25}})
    examples["AWSS3OASIS"] = rows
    after["CQL_HOSTED_QUERY_EXAMPLES_JSON"] = configuration.compact(examples)
    if {key for key in after if before.get(key) != after[key]} != set(KEYS):
        raise RuntimeError("Unexpected configuration changes.")
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
                            configuration.verification.protected_token(configuration.compact(after).encode()))
    print("Protected public S3 candidate prepared: three values/four presets; no service mutation.")


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["apply"]:
        configuration.apply_values(PREFIX, KEYS)
    else:
        raise SystemExit("Use no arguments to prepare, or 'apply' for the reviewed candidate.")
