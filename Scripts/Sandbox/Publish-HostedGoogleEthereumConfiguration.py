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
polygon = module("polygon_public", "Publish-HostedPolygonPublicConfiguration.py")
arbitrum = module("arbitrum_public", "Publish-HostedArbitrumPublicConfiguration.py")
cosmos = module("cosmos_public", "Publish-HostedCosmosPublicConfiguration.py")
configuration = google.configuration
PREFIX = "google-ethereum-public"
KEYS = ["CQL_GCS_PUBLIC_SOURCE_JSON", "CQL_ETHEREUM_PUBLIC_SOURCE_JSON",
        "PREPARED_STORAGE_ADMISSION_JSON", "CQL_HOSTED_QUERY_EXAMPLES_JSON"]


def candidate(before, google_source, ethereum_source, polygon_source=None, arbitrum_source=None, cosmos_source=None):
    if cosmos_source is not None and (polygon_source is None or arbitrum_source is None):
        raise ValueError("Joint Cosmos rollout requires all four explicit earlier source profiles.")
    if arbitrum_source is not None and polygon_source is None:
        raise ValueError("Joint Arbitrum rollout requires the explicit Polygon source too.")
    # Compose through each provider's strict validator. Independent candidates
    # from the same old host snapshot must never overwrite each other's lists.
    after = ethereum.candidate(google.candidate(before, google_source), ethereum_source)
    expected_keys = KEYS
    if polygon_source is not None:
        after = polygon.candidate(after, polygon_source)
        expected_keys = KEYS + ["CQL_POLYGON_PUBLIC_SOURCE_JSON"]
    if arbitrum_source is not None:
        after = arbitrum.candidate(after, arbitrum_source)
        expected_keys = expected_keys + ["CQL_ARBITRUM_PUBLIC_SOURCE_JSON"]
    if cosmos_source is not None:
        after = cosmos.candidate(after, cosmos_source)
        expected_keys = expected_keys + ["CQL_COSMOS_PUBLIC_SOURCE_JSON"]
    if {key for key in after if before.get(key) != after[key]} != set(expected_keys):
        raise ValueError("Exactly the selected source profiles and shared admission/examples may change.")
    return after


def prepare(include_polygon=False, include_arbitrum=False, include_cosmos=False):
    if include_cosmos and not (include_polygon and include_arbitrum):
        raise ValueError("Explicit five-source scope required.")
    if include_arbitrum and not include_polygon:
        raise ValueError("Explicit joint scope required.")
    state = configuration.STATE
    prefix = PREFIX + ("-polygon" if include_polygon else "") + ("-arbitrum" if include_arbitrum else "") + ("-cosmos" if include_cosmos else "")
    snapshot = state / (prefix + "-before-variables.dpapi")
    before = configuration.variables()
    if snapshot.exists():
        if configuration.protected_read(snapshot.name) != before:
            raise RuntimeError("Host changed since snapshot; reconcile before preparing.")
    else:
        configuration.write_new(snapshot, configuration.verification.protected_token(configuration.compact(before).encode()))
    folders = ["HostedGcsPublicSourceCurrent", "HostedEthereumPublicSourceCurrent"]
    if include_polygon:
        folders.append("HostedPolygonPublicSourceCurrent")
    if include_arbitrum:
        folders.append("HostedArbitrumPublicSourceCurrent")
    if include_cosmos:
        folders.append("HostedCosmosPublicSourceCurrent")
    sources = [json.loads((ROOT / "LocalState/COSMIC" / folder / "source.json").read_text(encoding="utf-8-sig"))
        for folder in folders]
    after = candidate(before, *sources)
    configuration.write_new(state / (prefix + "-candidate-variables.dpapi"),
        configuration.verification.protected_token(configuration.compact(after).encode()))
    print("Protected combined candidate prepared: " + ("seven values/twenty presets" if include_cosmos else "six values/sixteen presets" if include_arbitrum else "five values/twelve presets" if include_polygon else "four values/eight presets") + "; no mutation/deployment.")


if __name__ == "__main__":
    if len(sys.argv) == 1:
        prepare()
    elif sys.argv[1:] == ["with-cosmos"]:
        prepare(include_polygon=True, include_arbitrum=True, include_cosmos=True)
    elif sys.argv[1:] == ["apply-with-cosmos"]:
        configuration.apply_values(PREFIX + "-polygon-arbitrum-cosmos", KEYS + ["CQL_POLYGON_PUBLIC_SOURCE_JSON", "CQL_ARBITRUM_PUBLIC_SOURCE_JSON", "CQL_COSMOS_PUBLIC_SOURCE_JSON"])
    elif sys.argv[1:] == ["with-arbitrum"]:
        prepare(include_polygon=True, include_arbitrum=True)
    elif sys.argv[1:] == ["apply-with-arbitrum"]:
        configuration.apply_values(PREFIX + "-polygon-arbitrum", KEYS + ["CQL_POLYGON_PUBLIC_SOURCE_JSON", "CQL_ARBITRUM_PUBLIC_SOURCE_JSON"])
    elif sys.argv[1:] == ["with-polygon"]:
        prepare(include_polygon=True)
    elif sys.argv[1:] == ["apply-with-polygon"]:
        configuration.apply_values(PREFIX + "-polygon", KEYS + ["CQL_POLYGON_PUBLIC_SOURCE_JSON"])
    elif sys.argv[1:] == ["apply"]:
        configuration.apply_values(PREFIX, KEYS)
    else:
        raise SystemExit("Use no args/with-polygon/with-arbitrum/with-cosmos to prepare, or the matching explicit apply action.")
