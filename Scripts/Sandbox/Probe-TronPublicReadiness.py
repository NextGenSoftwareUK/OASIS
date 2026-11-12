"""Bounded anonymous SolidityNode header qualification; no signing or SDK."""
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import Request, build_opener, HTTPRedirectHandler

ENDPOINT = "https://api.trongrid.io/walletsolidity/getblock"
GENESIS = "00000000000000001ebf88508a03865c71d452e25f4d51194196a1d22b6653dc"
HEIGHTS = (1, 2, 3)


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        raise ValueError("Redirects are not accepted")


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property")
        result[key] = value
    return result


def nonnegative_int64(value):
    if type(value) is not int or not 0 <= value <= (1 << 63) - 1:
        raise ValueError("Exact nonnegative int64 required")
    return str(value)


def hash32(value):
    if not isinstance(value, str) or not re.fullmatch(r"[0-9a-f]{64}", value):
        raise ValueError("Canonical 32-byte hex hash required")
    return value


def request(height):
    # The native API supports signed int64 block heights. Keep the qualifier
    # bounded to explicit nonnegative heights, never a scan or latest selector.
    nonnegative_int64(height)
    return {"id_or_num": str(height), "detail": False}


def validate(response, height):
    request(height)
    if not isinstance(response, dict) or "Error" in response or "transactions" in response:
        raise ValueError("Complete header-only response required")
    block_id = hash32(response["blockID"])
    if int(block_id[:16], 16) != height:
        raise ValueError("Block ID height differs from requested height")
    raw = response["block_header"]["raw_data"]
    if not isinstance(raw, dict):
        raise ValueError("Native header required")
    if height == 0:
        # Protobuf zero omission is specific to the genesis response. Do not
        # manufacture timestamps or apply this rule to ordinary block headers.
        if block_id != GENESIS or ("number" in raw and (type(raw["number"]) is not int or raw["number"] != 0)):
            raise ValueError("API-reported Mainnet genesis differs")
        return {"height": "0", "blockId": block_id}
    if nonnegative_int64(raw["number"]) != str(height):
        raise ValueError("Native header height differs")
    timestamp = nonnegative_int64(raw["timestamp"])
    witness = raw["witness_address"]
    if not isinstance(witness, str) or not re.fullmatch(r"41[0-9a-f]{40}", witness):
        raise ValueError("Canonical native witness address required")
    return {"height": str(height), "blockId": block_id,
            "parentHash": hash32(raw["parentHash"]), "transactionRoot": hash32(raw["txTrieRoot"]),
            "timestampMilliseconds": timestamp, "witnessAddress": witness,
            "consistency": "SolidityNodeSolidified"}


def read(height):
    req = Request(ENDPOINT, data=json.dumps(request(height)).encode(), method="POST",
                  headers={"Content-Type": "application/json", "Accept": "application/json"})
    with build_opener(NoRedirect).open(req, timeout=12) as response:
        raw = response.read(65537)
        if len(raw) > 65536:
            raise ValueError("Header response byte budget exceeded")
        value = validate(json.loads(raw, object_pairs_hook=unique), height)
        return value


def main():
    target = Path(__file__).resolve().parents[2] / "TestResults/TronPublicSourceCandidateCurrent/qualified-readiness.json"
    report = {"observedUtc": datetime.now(timezone.utc).isoformat(), "endpoint": ENDPOINT,
              "passed": False, "mutations": 0}
    try:
        genesis = read(0)
        rows = [read(height) for height in HEIGHTS]
        prior = genesis
        for row in rows:
            if row["parentHash"] != prior["blockId"]:
                raise ValueError("Selected adjacent header linkage differs")
            prior = row
        report.update(passed=True, genesis=genesis, rows=rows)
    except Exception as error:
        report["errorType"] = type(error).__name__
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": report["passed"], "rows": len(report.get("rows", [])),
                      "sha256": hashlib.sha256(target.read_bytes()).hexdigest(), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
