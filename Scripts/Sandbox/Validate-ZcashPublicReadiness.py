"""Offline validation of the pinned ZecBlock v1 block-summary envelope."""
import argparse
import json
import re
from pathlib import Path

GENESIS = "00040fe8ec8471911baa1db1266ea15dd06b4a8a5c453883c000b031973dce08"


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property")
        result[key] = value
    return result


def number(value, maximum=2147483647):
    if type(value) is not int or not 0 <= value <= maximum:
        raise ValueError("Exact bounded nonnegative native integer required")
    return value


def validate(value, heights, genesis=False):
    if not isinstance(value, dict) or set(value) != {"data", "meta"}:
        raise ValueError("Successful v1data/meta envelope required")
    meta = value["meta"]
    if not isinstance(meta, dict) or meta.get("network") != "mainnet":
        raise ValueError("Explicit mainnet metadata required")
    units = meta.get("units")
    if (not isinstance(units, dict) or units.get("authoritativeMonetary") != "zatoshi"
            or units.get("authoritativeEncoding") != "decimal-string"
            or units.get("zatoshiPerZec") != "100000000"):
        raise ValueError("Pinned v1unit policy required")
    freshness = meta.get("freshness")
    if not isinstance(freshness, dict) or freshness.get("status") not in ("fresh", "stale", "unknown"):
        raise ValueError("Available observation with explicit freshness required")
    items = value["data"]
    if not isinstance(items, list) or len(items) != len(heights) or not 1 <= len(items) <= 3:
        raise ValueError("Exact bounded selection required")
    rows = []
    for item in items:
        if not isinstance(item, dict) or any(key in item for key in ("transactions", "raw", "private_notes", "shielded_balances")):
            raise ValueError("Metadata summary required; transaction/private payload forbidden")
        height = number(item.get("height"))
        block_hash = item.get("hash")
        if not isinstance(block_hash, str) or not re.fullmatch(r"[0-9a-f]{64}", block_hash):
            raise ValueError("Canonical native block hash required")
        rows.append({"height": str(height), "hash": block_hash,
                     "timestamp": str(number(item.get("timestamp"), 4294967295)),
                     "policy": "indexer-reported-public-block-summary", "proofVerified": False})
    if ({int(row["height"]) for row in rows} != set(heights)
            or len({row["height"] for row in rows}) != len(rows)
            or len({row["hash"] for row in rows}) != len(rows)):
        raise ValueError("Exact unique native identities required")
    if genesis and (heights != [0] or rows[0]["hash"] != GENESIS):
        raise ValueError("Pinned Zcash mainnet genesis differs")
    return {"rows": sorted(rows, key=lambda row: int(row["height"])),
            "network": "mainnet", "freshness": freshness["status"], "proofVerified": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("response")
    parser.add_argument("--heights", required=True)
    parser.add_argument("--genesis", action="store_true")
    args = parser.parse_args()
    raw = Path(args.response).read_bytes()
    if len(raw) > 262144:
        raise ValueError("Native byte budget exceeded")
    heights = [int(value) for value in args.heights.split(",")]
    print(json.dumps(validate(json.loads(raw, object_pairs_hook=unique), heights, args.genesis)))


if __name__ == "__main__":
    main()
