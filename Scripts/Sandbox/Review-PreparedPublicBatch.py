"""Export only public profiles/presets for offline shipping-planner preflight.

Reads the existing protected seven-source candidate. No Railway/network calls,
source activation, writes to providers or deployment. Never export DNA/admission.
"""
import importlib.util
import json
from pathlib import Path
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[2]
PROFILE_KEYS = {
    "GoogleCloudOASIS": ("CQL_GCS_PUBLIC_SOURCE_JSON", {"schema", "bucket", "keys", "owner"}),
    "EthereumOASIS": ("CQL_ETHEREUM_PUBLIC_SOURCE_JSON", {"schema", "rpcUrl", "chainId", "addresses", "sourceOwner"}),
    "PolygonOASIS": ("CQL_POLYGON_PUBLIC_SOURCE_JSON", {"schema", "rpcUrl", "chainId", "addresses", "sourceOwner"}),
    "ArbitrumOASIS": ("CQL_ARBITRUM_PUBLIC_SOURCE_JSON", {"schema", "rpcUrl", "chainId", "addresses", "sourceOwner"}),
    "CosmosBlockChainOASIS": ("CQL_COSMOS_PUBLIC_SOURCE_JSON", {"schema", "restUrl", "chainId", "addresses", "sourceOwner"}),
    "PolkadotOASIS": ("CQL_POLKADOT_PUBLIC_SOURCE_JSON", {"schema", "rpcUrl", "genesisHash", "blockHashes", "sourceOwner"}),
    "NEAROASIS": ("CQL_NEAR_PUBLIC_SOURCE_JSON", {"schema", "rpcUrl", "networkId", "accounts", "sourceOwner"}),
}


def public_review(candidate):
    profiles = {}
    examples = json.loads(candidate["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
    for provider, (key, fields) in PROFILE_KEYS.items():
        source = json.loads(candidate[key])
        if set(source) != fields:
            raise ValueError("Public export refuses unexpected source fields")
        for url_key in ("rpcUrl", "restUrl"):
            if url_key in source:
                url = urlsplit(source[url_key])
                if (url.scheme != "https" or url.username or url.password or url.query or url.fragment
                        or url.path not in ("", "/", "/rpc")):
                    raise ValueError("Public export refuses credential-bearing source endpoints")
        profiles[provider] = source
    return {"profiles": profiles, "examples": {provider: examples[provider] for provider in PROFILE_KEYS}}


def main():
    spec = importlib.util.spec_from_file_location("configuration", Path(__file__).with_name("Publish-HostedSolidConfiguration.py"))
    configuration = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(configuration)
    candidate = configuration.protected_read("google-ethereum-public-polygon-arbitrum-cosmos-polkadot-near-candidate-variables.dpapi")
    review = public_review(candidate)
    path = ROOT / "TestResults/HostedCqlIntegrationCurrent/seven-source-public-review.json"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
    print("Public-only review exported: seven profiles/28presets; no network, activation or deployment.")


if __name__ == "__main__":
    main()
