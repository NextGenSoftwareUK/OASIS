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
    verify_mount()
    configuration.apply_values(PREFIX, KEYS, reviewed_snapshots=(before, after))


def verify_settings():
    expected = os.environ.get("COSMIC_CQL_SETTINGS_DEPLOYMENT_ID")
    if not expected:
        raise RuntimeError("Set the reviewed Development PouchDB settings deployment ID.")
    deployment = verify_mount(expected)
    if configuration.variables() != configuration.protected_read(f"{PREFIX}-candidate-variables.dpapi"):
        raise RuntimeError("Development WEB4 settings differ from the protected PouchDB review.")
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
        print(f"Reviewed Development settings and mount verified for deployment {verify_settings()}.")
    else:
        raise SystemExit("Use no arguments to prepare, or select verify-mount, apply or verify-settings.")
