"""Review protected Development WEB4 settings, then explicitly stage six PouchDB values."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "LocalState/COSMIC/RecoveryLaptopPouchDBCql/hosted-preparation"
PREFIX = "pouchdb-public"
REVIEW_KEYS = (
    "OASIS_SANDBOX_POUCHDB", "OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON",
    "CQL_POUCHDB_QUERY_SOURCE_DIGEST", "CQL_HOSTED_QUERY_EXAMPLES__PouchDBOASIS",
)
KEYS = (*REVIEW_KEYS, "PREPARED_STORAGE_ADMISSION_JSON", "CQL_PUBLIC_READ_CATALOGUES_JSON")

spec = importlib.util.spec_from_file_location("web4_mount", Path(__file__).with_name("Publish-HostedLocalFileConfiguration.py"))
mount = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mount)
configuration = mount.configuration


def candidate(before):
    raw = (SOURCE / "bundle.json").read_bytes()
    proof = json.loads((SOURCE / "native-source-qualified.json").read_bytes())
    receipt = (SOURCE / "source/bootstrap.acknowledged.json").read_bytes()
    digest = hashlib.sha256(raw).hexdigest()
    if (digest != proof["sourceBundleSha256"].lower()
            or hashlib.sha256(receipt).hexdigest() != proof["originalSourceAckSha256"].lower()):
        raise RuntimeError("Retained PouchDB source or acknowledgement differs from qualification.")
    # The source parser and preset compiler remain owned by the Node preparer.
    inputs = {key: before[key] for key in (*KEYS, "OASIS_DNA_JSON", "CQL_HOSTED_QUERY_EXAMPLES_JSON") if key in before}
    observed = subprocess.run(
        ["node", "-e", "const fs = require('node:fs'); "
         "const prepare = require('./Scripts/Sandbox/Prepare-HostedPouchDbConfiguration.cjs').prepare; "
         "const input = JSON.parse(fs.readFileSync(0, 'utf8')); "
         "process.stdout.write(JSON.stringify(prepare(input.before, input.bundle)));"],
        cwd=ROOT, input=configuration.compact({"before": inputs, "bundle": raw.decode()}).encode(),
        capture_output=True, timeout=30)
    if observed.returncode:
        raise RuntimeError("PouchDB settings preparation failed; output withheld.")
    patch = json.loads(observed.stdout)
    if (set(patch) != set(KEYS) or patch[REVIEW_KEYS[2]] != digest
            or any(not isinstance(value, str) or before.get(key) == value for key, value in patch.items())):
        raise RuntimeError("Expected exactly six changed PouchDB settings.")
    return {**before, **patch}


def prepare():
    before = configuration.variables()
    after = candidate(before)
    configuration.STATE.mkdir(parents=True, exist_ok=True)
    for label, value in (("before", before), ("candidate", after)):
        path = configuration.STATE / f"{PREFIX}-{label}-variables.dpapi"
        if path.exists():
            if configuration.protected_read(path.name) != value:
                raise RuntimeError("Existing PouchDB preparation differs; reconcile before continuing.")
        else:
            configuration.write_new(path, configuration.verification.protected_token(configuration.compact(value).encode()))
    review = SOURCE / "prepared-review.json"
    data = configuration.compact({key: after[key] for key in REVIEW_KEYS}).encode()
    if review.exists():
        if review.read_bytes() != data:
            raise RuntimeError("Existing public PouchDB review differs; reconcile before continuing.")
    else:
        configuration.write_new(review, data)
    print("Six protected PouchDB settings prepared. Public source and presets are ready for review.")


def verify_mount(expected_deployment=None):
    if not re.fullmatch(r"sha256:[0-9a-f]{64}", os.environ.get("COSMIC_CQL_EXPECTED_DEPLOYMENT_IMAGE_DIGEST", "")):
        raise RuntimeError("The reviewed immutable WEB4 image digest is required.")
    return mount.verify_mount(initial_bootstrap=True, expected_deployment=expected_deployment)


def apply():
    before = configuration.protected_read(f"{PREFIX}-before-variables.dpapi")
    after = configuration.protected_read(f"{PREFIX}-candidate-variables.dpapi")
    if candidate(before) != after:
        raise RuntimeError("Protected PouchDB settings differ from the qualified candidate.")
    deployment = verify_mount()

    def require_original_deployment():
        if mount.verify_release() != deployment:
            raise RuntimeError("Reviewed Railway deployment changed before PouchDB settings staging.")

    configuration.apply_values(PREFIX, KEYS, reviewed_snapshots=(before, after),
                               before_mutation=require_original_deployment)


def verify_source_receipt(digest):
    instance = os.environ["COSMIC_CQL_REVIEWED_INSTANCE_IDS"]
    try:
        observed = subprocess.run(
            [str(configuration.CLI), "ssh", *configuration.COMMON, "-d", instance, "--",
             "head", "-c", "16385", "/data/COSMIC/SandboxPouchDB/bootstrap.acknowledged.json"],
            capture_output=True, timeout=30)
    except (OSError, subprocess.TimeoutExpired):
        raise RuntimeError("Could not inspect the PouchDB receipt on the reviewed instance.") from None
    if observed.returncode or not 0 < len(observed.stdout) <= 16384:
        raise RuntimeError("The reviewed instance has no bounded PouchDB source receipt.")
    proof = json.loads((SOURCE / "native-source-qualified.json").read_bytes())
    original_bytes = (SOURCE / "source/bootstrap.acknowledged.json").read_bytes()
    if hashlib.sha256(original_bytes).hexdigest() != proof["originalSourceAckSha256"].lower():
        raise RuntimeError("Local PouchDB receipt differs from native qualification.")
    original = json.loads(original_bytes)
    try:
        live = json.loads(observed.stdout)
    except (UnicodeDecodeError, ValueError):
        raise RuntimeError("The reviewed instance returned an invalid PouchDB receipt.") from None
    if (not isinstance(live, dict) or set(live) != {"schema", "digest", "version", "records"}
            or type(live["schema"]) is not int
            or live["schema"] != 1 or live["digest"] != digest or original["digest"] != digest
            or live["version"] != original["version"] or not isinstance(live["records"], list)
            or len(live["records"]) != 3):
        raise RuntimeError("The live PouchDB receipt differs from the reviewed source.")
    fields = ("id", "payloadSha256", "nativeSha256")
    expected = {tuple(row[key] for key in fields) for row in original["records"]}
    actual = set()
    for row in live["records"]:
        if (not isinstance(row, dict) or set(row) != {*fields, "revision"}
                or not all(isinstance(value, str) for value in row.values())
                or not re.fullmatch(r"1-[a-f0-9]{32}", row["revision"])):
            raise RuntimeError("The live PouchDB receipt has an invalid first-generation record.")
        actual.add(tuple(row[key] for key in fields))
    if len(actual) != 3 or actual != expected:
        raise RuntimeError("The live PouchDB records differ from the qualified teaching source.")


def verify_settings(reviewed_public_settings=None):
    expected = os.environ.get("COSMIC_CQL_SETTINGS_DEPLOYMENT_ID")
    if not expected:
        raise RuntimeError("Set the reviewed Development PouchDB settings deployment ID.")
    deployment = verify_mount(expected)
    candidate_settings = configuration.protected_read(f"{PREFIX}-candidate-variables.dpapi")
    if reviewed_public_settings is not None and reviewed_public_settings != {
            key: candidate_settings[key] for key in REVIEW_KEYS}:
        raise RuntimeError("The public PouchDB review differs from the protected settings candidate.")
    if configuration.variables() != candidate_settings:
        raise RuntimeError("Development WEB4 settings differ from the protected PouchDB review.")
    verify_source_receipt(candidate_settings[REVIEW_KEYS[2]])
    if mount.verify_release() != deployment:
        raise RuntimeError("Reviewed Railway deployment changed during settings inspection.")
    return deployment


if __name__ == "__main__":
    if sys.argv[1:] == []:
        prepare()
    elif sys.argv[1:] == ["apply"]:
        apply()
    elif sys.argv[1:] == ["verify-mount"]:
        print(f"Reviewed Development deployment {verify_mount()} and its writable mount verified.")
    elif sys.argv[1:] == ["verify-settings"]:
        print(f"Reviewed Development settings, mount and source receipt verified for deployment {verify_settings()}.")
    else:
        raise SystemExit("Use no arguments to prepare, or select verify-mount, apply or verify-settings.")
