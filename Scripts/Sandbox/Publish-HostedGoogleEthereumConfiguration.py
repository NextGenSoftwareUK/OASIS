"""Stage Google and Ethereum together; preserve both provider enrollments.

Default is protected preparation only. Explicit apply uses the existing journalled
publisher and never triggers deployment. Requires matching tested Core/application.
"""
from pathlib import Path
import importlib.util
import json
import sys

ROOT = Path(__file__).resolve().parents[2]

def module(name, filename):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(filename))
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value

google = module("google_public", "Publish-HostedGooglePublicConfiguration.py")
ethereum = module("ethereum_public", "Publish-HostedEthereumPublicConfiguration.py")
configuration = google.configuration
PREFIX = "google-ethereum-public"
KEYS = ["CQL_GCS_PUBLIC_SOURCE_JSON", "CQL_ETHEREUM_PUBLIC_SOURCE_JSON",
        "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES_JSON"]


def candidate(before, google_source, ethereum_source):
    # Compose through each provider's strict validator. Independent candidates
    # from the same old host snapshot must never overwrite each other's lists.
    after = ethereum.candidate(google.candidate(before, google_source), ethereum_source)
    if {key for key in after if before.get(key) != after[key]} != set(KEYS):
        raise ValueError("Exactly the two source profiles and shared admission/examples may change.")
    return after


def prepare():
    state = configuration.STATE
    snapshot = state / (PREFIX + "-before-variables.dpapi")
    before = configuration.variables()
    if snapshot.exists():
        if configuration.protected_read(snapshot.name) != before:
            raise RuntimeError("Host changed since snapshot; reconcile before preparing.")
    else:
        configuration.write_new(snapshot, configuration.verification.protected_token(configuration.compact(before).encode()))
    sources = [json.loads((ROOT / "LocalState/COSMIC" / folder / "source.json").read_text(encoding="utf-8-sig"))
        for folder in ("HostedGcsPublicSourceCurrent", "HostedEthereumPublicSourceCurrent")]
    after = candidate(before, *sources)
    configuration.write_new(state / (PREFIX + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    print("Protected combined candidate prepared: four values/eight presets; no mutation/deployment.")


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["apply"]:
        configuration.apply_values(PREFIX, KEYS)
    else:
        raise SystemExit("Use no arguments to prepare, or apply for the reviewed matching release.")
