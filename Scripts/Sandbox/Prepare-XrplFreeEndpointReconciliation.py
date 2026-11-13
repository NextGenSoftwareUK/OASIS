"""Prepare one reviewed endpoint-only correction; never apply or retry a mutation."""
import copy
import importlib.util
import json
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PREFIX = "xrpl-public-free-endpoint"
KEY = "CQL_XRPL_PUBLIC_SOURCE_JSON"
SPEC = importlib.util.spec_from_file_location("configuration", Path(__file__).with_name("Publish-HostedSolidConfiguration.py"))
configuration = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(configuration)


def candidate(before, replacement):
    original = json.loads(before[KEY])
    fields = {"schema", "rpcUrl", "networkId", "ledgerHashes", "sourceOwner"}
    if set(original) != fields or set(replacement) != fields:
        raise ValueError("Exact original/replacement source schema required")
    for source in (original, replacement):
        if (type(source["schema"]) is not int or source["schema"] != 1
                or type(source["networkId"]) is not int or source["networkId"] != 0
                or not isinstance(source["sourceOwner"], str) or uuid.UUID(source["sourceOwner"]).int == 0):
            raise ValueError("Preserved explicit Mainnet identity required")
    if (original["rpcUrl"] != "https://xrplcluster.com/"
            or replacement["rpcUrl"] != "https://s2.ripple.com:51234/"
            or {k: v for k, v in original.items() if k != "rpcUrl"}
            != {k: v for k, v in replacement.items() if k != "rpcUrl"}):
        raise ValueError("Only the qualified free endpoint may change")
    admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
    tariffs = [v for v in admission["queryTariffs"] if v["provider"] == "XRPLOASIS"]
    if (admission["queryOnlyProviders"].count("XRPLOASIS") != 1
            or "XRPLOASIS" in admission["ordinaryProviders"]
            or any(v["provider"] == "XRPLOASIS" for v in admission["nativeProviders"])
            or len(tariffs) != 1 or tariffs[0]["costUsd"] != 0):
        raise ValueError("Existing independent free read-only admission required")
    after = copy.deepcopy(before)
    after[KEY] = configuration.compact(replacement)
    if {key for key in after if after[key] != before.get(key)} != {KEY}:
        raise ValueError("Exactly one source setting must change")
    return after


def main():
    before = configuration.variables()
    if before != configuration.protected_read("xrpl-public-candidate-variables.dpapi"):
        raise RuntimeError("Original acknowledged state changed; reconcile before preparing")
    source = json.loads((ROOT / "LocalState/COSMIC/HostedXrplPublicSourceCurrent/source-free-endpoint.json").read_text(encoding="utf-8"))
    after = candidate(before, source)
    for suffix, value in (("before", before), ("candidate", after)):
        configuration.write_new(configuration.STATE / (PREFIX + "-" + suffix + "-variables.dpapi"),
                                configuration.verification.protected_token(configuration.compact(value).encode()))
    print("Prepared one free-endpoint change; original identity, admission, presets and DNA preserved. No mutation.")


if __name__ == "__main__":
    main()
