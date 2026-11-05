"""Prepare a free public LocalFile dataset on the existing volume;apply only after code deployment."""
from pathlib import Path
import base64
import copy
import hashlib
import importlib.util
import json
import os
import re
import subprocess
import sys
import uuid

ROOT = Path(__file__).resolve().parents[2]
PREFIX = "localfile-public"
KEYS = ["CQL_LOCALFILE_PUBLIC_SOURCE_JSON", "CQL_LOCALFILE_PUBLIC_BOOTSTRAP_JSON",
        "PREPARED_STORAGE_ADMISSION_JSON", "CQL_PUBLIC_READ_CATALOGUES_JSON",
        "CQL_HOSTED_QUERY_EXAMPLES__LocalFileOASIS"]


def module(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


configuration = module("Publish-HostedSolidConfiguration")


def identity(value):
    return isinstance(value, str) and str(uuid.UUID(value)) == value and uuid.UUID(value).int != 0


def candidate(before, source, bootstrap):
    if (set(source) != {"schema", "directory", "sourceOwner", "records"}
            or type(source["schema"]) is not int or source["schema"] != 1
            or source["directory"] != "/data/cql-local-file-demo" or not identity(source["sourceOwner"])
            or not isinstance(source["records"], list) or len(source["records"]) != 3):
        raise ValueError("Exact separately qualified public LocalFile source required.")
    hashes = {}
    for row in source["records"]:
        if (set(row) != {"id", "sha256"} or not identity(row["id"]) or row["id"] in hashes
                or not isinstance(row["sha256"], str) or not re.fullmatch("[0-9a-f]{64}", row["sha256"])):
            raise ValueError("Unique declared source identities/hashes required.")
        hashes[row["id"]] = row["sha256"]
    if set(bootstrap) != {"schema", "records"} or type(bootstrap["schema"]) is not int or bootstrap["schema"] != 1:
        raise ValueError("Exact bootstrap schema required.")
    rows = bootstrap["records"]
    if not isinstance(rows, list) or len(rows) != 3:
        raise ValueError("Complete bootstrap required.")
    seen = set()
    for row in rows:
        if (set(row) != {"id", "sha256", "contentBase64"} or row["id"] not in hashes or row["id"] in seen
                or row["sha256"] != hashes[row["id"]]):
            raise ValueError("Bootstrap identity/hash differs.")
        raw = base64.b64decode(row["contentBase64"], validate=True)
        if not raw or len(raw) > 32768 or hashlib.sha256(raw).hexdigest() != row["sha256"]:
            raise ValueError("Bootstrap bytes differ.")
        seen.add(row["id"])
    dna = json.loads(before["OASIS_DNA_JSON"])
    if dna["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("Existing sandbox mode must be enabled in DNA.")
    provider = "LocalFileOASIS"
    if any(key in before for key in (KEYS[0], KEYS[1], KEYS[4])):
        raise ValueError("Existing LocalFile source/bootstrap/presets require reconciliation.")
    admission = json.loads(before[KEYS[2]])
    if (provider in admission["queryOnlyProviders"] or provider in admission["ordinaryProviders"]
            or any(row["provider"] == provider for row in admission["nativeProviders"] + admission["queryTariffs"])):
        raise ValueError("Existing LocalFile admission must not be replaced.")
    grants = json.loads(before.get(KEYS[3], "{}"))
    if provider in grants or provider in json.loads(before.get("CQL_HOSTED_QUERY_EXAMPLES_JSON", "{}")):
        raise ValueError("Existing LocalFile grants/presets require reconciliation.")
    admission["queryOnlyProviders"].append(provider)
    tariff = copy.deepcopy(next(row for row in admission["queryTariffs"] if row["provider"] == "IPFSOASIS"))
    if tariff["costUsd"] != 0:
        raise ValueError("Only free source queries authorized.")
    tariff["provider"] = provider
    admission["queryTariffs"].append(tariff)
    ids = list(hashes)
    grants[provider] = ids
    examples = []
    for count in (3, 2):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
            "filters": [{"field": "Id", "scalar": "Guid", "operator": "In", "value": ids[:count]}]}
        suffix = "records" if count == 3 else "subset"
        examples.append({"id": "public-" + suffix, "label": "LocalFile public teaching records (" + str(count) + ")",
            "request": {"provider": provider, "maximumDurationMilliseconds": 30000, **selection}})
        examples.append({"id": "mongo-public-" + suffix, "label": "LocalFile + my private Mongo snapshots",
            "operation": "Federation", "request": {"scope": "Named", "providers": ["MongoDBOASIS", provider],
                "source": selection, "identityMode": "PreserveCopies", "maximumProviderAttempts": 2,
                "maximumDurationMilliseconds": 30000, "take": 25}})
    after = copy.deepcopy(before)
    for key, value in zip(KEYS, (source, bootstrap, admission, grants, examples)):
        after[key] = configuration.compact(value)
    if {key for key in before.keys() | after.keys() if before.get(key) != after.get(key)} != set(KEYS):
        raise ValueError("Exactly five source-specific values may change.")
    if any(len(after[key].encode()) > (131072 if key == KEYS[1] else 32768) for key in KEYS):
        raise ValueError("Variable byte budget exceeded.")
    return after


def qualified_source():
    state = ROOT / "LocalState/COSMIC/HostedLocalFilePublicDatasetCurrent"
    paths = (state / "source-config.json", state / "bootstrap.json",
             state / "export.acknowledged.json",
             ROOT / "LocalState/COSMIC/HostedLocalFileSourceCurrent/source-qualified.json")
    missing = [str(path.relative_to(ROOT)) for path in paths if not path.is_file()]
    if missing:
        raise RuntimeError("Restore the original LocalFile qualification files before preparing a rollout: "
                           + ", ".join(missing))
    raw, bundle, ack_bytes, proof_bytes = (path.read_bytes() for path in paths)
    ack = json.loads(ack_bytes)
    proof = json.loads(proof_bytes)
    if ((ack.get("sdkFull"), ack.get("sdkSubset")) != (3, 2) or ack.get("nativeSourceUnchanged") is not True
            or proof.get("sdkCounts") != [3, 2] or proof.get("shippingBootstrapAdmitted") is not True
            or any(value.get(name) != hashlib.sha256(data).hexdigest() for value in (ack, proof)
                   for name, data in (("sourceConfigSha256", raw), ("bootstrapSha256", bundle)))):
        raise RuntimeError("Exact source-bound SDK/bootstrap qualification required;no reseed.")
    return json.loads(raw), json.loads(bundle)


def prepare():
    source, bootstrap = qualified_source()
    before = configuration.variables()
    after = candidate(before, source, bootstrap)
    configuration.STATE.mkdir(parents=True, exist_ok=True)
    review_path = ROOT / "TestResults/HostedLocalFileSourceCurrent/prepared-review.json"
    review_path.parent.mkdir(parents=True, exist_ok=True)
    for suffix, value in (("before", before), ("candidate", after)):
        path = configuration.STATE / (PREFIX + "-" + suffix + "-variables.dpapi")
        if path.exists():
            if configuration.protected_read(path.name) != value:
                raise RuntimeError("Existing LocalFile preparation differs; reconcile before continuing.")
        else:
            configuration.write_new(path,
                configuration.verification.protected_token(configuration.compact(value).encode()))
    review = {"changedKeys": KEYS, "source": source, "presets": json.loads(after[KEYS[4]]),
              "sourceRows": 3, "bootstrapBytes": len(after[KEYS[1]].encode()), "dnaUnchanged": True,
              "hostingSpend": 0, "requiresExistingVolume": "/data", "hostedVerified": False}
    review_bytes = (json.dumps(review, indent=2) + "\n").encode()
    if review_path.exists():
        if review_path.read_bytes() != review_bytes:
            raise RuntimeError("Existing LocalFile review differs; reconcile before continuing.")
    else:
        configuration.write_new(review_path, review_bytes)
    print("Protected LocalFile candidate prepared:five values/four presets,no service mutation or deployment.")


def require_data_mount(*, initial_bootstrap=False):
    instances = os.environ.get("COSMIC_CQL_REVIEWED_INSTANCE_IDS", "").split(",")
    if any(not re.fullmatch(r"[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}", instance)
           for instance in instances) or len(set(instances)) != len(instances):
        raise RuntimeError("Set the active instance IDs from the reviewed Development deployment")
    if initial_bootstrap and len(instances) != 1:
        raise RuntimeError("Initial LocalFile bootstrap requires one reviewed active instance")
    for instance in instances:
        try:
            result = subprocess.run(
                [str(configuration.CLI), "ssh", *configuration.COMMON, "-d", instance,
                 "--", "cat", "/proc/self/mountinfo"],
                capture_output=True, timeout=30)
        except (OSError, subprocess.TimeoutExpired):
            raise RuntimeError("Could not inspect the reviewed instance mount") from None
        mounts = [line.partition(b" - ") for line in result.stdout.splitlines()]
        data_mounts = [(mount.split(), filesystem.split()) for mount, separator, filesystem in mounts
                       if separator and mount.split()[4:5] == [b"/data"]]
        if result.returncode or len(data_mounts) != 1:
            raise RuntimeError("The reviewed instance must expose a separate writable /data mount")
        mount, filesystem = data_mounts[0]
        if len(mount) < 6 or len(filesystem) != 3 or any(
                b"rw" not in options.split(b",") for options in (mount[5], filesystem[2])):
            raise RuntimeError("The reviewed instance must expose a separate writable /data mount")


def verify_release():
    guard = module("Prepare-TenSourcePublicBatch")
    release_ref = os.environ.get("COSMIC_CQL_REVIEWED_RELEASE_REF")
    if release_ref:
        return guard.require_matching_release(configuration, release_ref=release_ref)
    return guard.require_matching_release(configuration)


def verify_mount(*, initial_bootstrap=False, expected_deployment=None):
    deployment = verify_release()
    if not isinstance(deployment, str) or not deployment:
        raise RuntimeError("Reviewed Railway deployment has no deployment ID")
    if expected_deployment is not None and deployment != expected_deployment:
        raise RuntimeError("Current deployment is not the reviewed LocalFile settings deployment")
    require_data_mount(initial_bootstrap=initial_bootstrap)
    if verify_release() != deployment:
        raise RuntimeError("Reviewed Railway deployment changed during mount inspection")
    return deployment


def apply():
    before = configuration.protected_read(PREFIX + "-before-variables.dpapi")
    after = configuration.protected_read(PREFIX + "-candidate-variables.dpapi")
    if candidate(before, *qualified_source()) != after:
        raise RuntimeError("Reviewed source/candidate differs;no mutation allowed.")
    verify_mount(initial_bootstrap=True)
    configuration.apply_values(PREFIX, KEYS)


def verify_settings():
    expected_deployment = os.environ.get("COSMIC_CQL_SETTINGS_DEPLOYMENT_ID")
    if not expected_deployment:
        raise RuntimeError("Set the deployment ID created by the reviewed LocalFile settings change")
    deployment = verify_mount(expected_deployment=expected_deployment)
    expected = configuration.protected_read(PREFIX + "-candidate-variables.dpapi")
    for key in KEYS:
        path = configuration.STATE / f"{PREFIX}-{key}.acknowledged.json"
        if key not in expected or not path.is_file():
            raise RuntimeError(f"Missing reviewed LocalFile settings acknowledgement for {key}")
        acknowledgement = json.loads(path.read_bytes())
        digest = hashlib.sha256(expected[key].encode()).hexdigest()
        if (acknowledgement.get("key") != key or acknowledgement.get("sha256") != digest
                or acknowledgement.get("verified") is not True
                or acknowledgement.get("deploymentTriggered") is not False):
            raise RuntimeError(f"Invalid reviewed LocalFile settings acknowledgement for {key}")
    observed = configuration.variables()
    if any(observed.get(key) != expected[key] for key in KEYS):
        raise RuntimeError("Development LocalFile settings differ from the reviewed candidate")
    if observed != expected:
        raise RuntimeError("Other Development WEB4 settings changed since the LocalFile review; reconcile before acceptance")
    if verify_release() != deployment:
        raise RuntimeError("Reviewed Railway deployment changed during settings inspection")
    return deployment


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["verify-release"]:
        verify_release()
        print("Reviewed Development WEB4 deployment verified.")
    elif sys.argv[1:] == ["verify-mount"]:
        deployment = verify_mount()
        print(f"Reviewed Development WEB4 deployment {deployment} and writable /data mounts verified.")
    elif sys.argv[1:] == ["apply"]:
        apply()
    elif sys.argv[1:] == ["verify-settings"]:
        deployment = verify_settings()
        print(f"Reviewed LocalFile settings and mount verified for deployment {deployment}.")
    else:
        raise SystemExit("Use no arguments to prepare, verify-release, verify-mount or verify-settings to inspect, or apply after exact code deployment.")
