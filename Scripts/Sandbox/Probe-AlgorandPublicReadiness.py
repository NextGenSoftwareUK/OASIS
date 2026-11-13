"""Qualify explicit current-state accounts, never historical or consensus proof."""
import base64
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import Request, build_opener, HTTPRedirectHandler

ALGOD = "https://mainnet-api.algonode.cloud/"
INDEXER = "https://mainnet-idx.algonode.cloud/"
NETWORK = "mainnet-v1.0"
GENESIS = "wGHE2Pwdvd7S12BL5FaOP20EGYesN73ktiC1qzkkit8="


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ValueError("Redirects are not accepted")


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON field")
        result[key] = value
    return result


def uint64(value):
    if type(value) is not int or not 0 <= value < 1 << 64:
        raise ValueError("Exact UInt64 required")
    return value


def address(value):
    if not isinstance(value, str) or not re.fullmatch(r"[A-Z2-7]{58}", value):
        raise ValueError("Canonical Algorand address required")
    raw = base64.b32decode(value + "======")
    if (len(raw) != 36 or base64.b32encode(raw).decode().rstrip("=") != value or
            raw[-4:] != hashlib.new("sha512_256", raw[:32]).digest()[-4:]):
        raise ValueError("Algorand address checksum differs")
    return value


def network(versions, genesis):
    if (versions.get("genesis_id") != NETWORK or versions.get("genesis_hash_b64") != GENESIS or
            genesis.get("genesis-id") != NETWORK or genesis.get("genesis-hash") != GENESIS or
            type(genesis.get("round")) is not int or genesis["round"] != 0):
        raise ValueError("Both endpoints must report the explicit Mainnet genesis")


def account(response, expected):
    expected = address(expected)
    current = uint64(response["current-round"])
    row = response["account"]
    if address(row["address"]) != expected:
        raise ValueError("Account identity differs")
    observed = uint64(row["round"])
    if observed != current:
        raise ValueError("Account and response rounds differ")
    if row.get("deleted", False) is not False:
        raise ValueError("Deleted accounts are not admitted")
    status = row["status"]
    if status not in ("Offline", "Online", "NotParticipating"):
        raise ValueError("Unknown account participation status")
    amounts = {key: str(uint64(row[key])) for key in
               ("amount", "amount-without-pending-rewards", "pending-rewards", "rewards", "min-balance")}
    if int(amounts["amount"]) != int(amounts["amount-without-pending-rewards"]) + int(amounts["pending-rewards"]):
        raise ValueError("Native reward accounting differs")
    return {"address": expected, "observedRound": str(observed), "status": status, **amounts}


def main():
    folder = Path(__file__).resolve().parents[2] / "TestResults/AlgorandPublicSourceCandidateCurrent"
    folder.mkdir(parents=True, exist_ok=True)
    report = {"utc": datetime.now(timezone.utc).isoformat(), "algodUrl": ALGOD,
              "indexerUrl": INDEXER, "sourceWrites": 0, "requests": [],
              "scope": "API-reported current accounts; independently observed rounds; no shared snapshot, historical rewind or consensus proof"}
    opener = build_opener(NoRedirect())

    def get(endpoint, path):
        with opener.open(Request(endpoint + path, headers={"Accept": "application/json"}), timeout=20) as response:
            raw = response.read(262145)
        if len(raw) > 262144:
            raise ValueError("Response exceeds256KiB")
        report["requests"].append({"endpoint": endpoint, "path": path, "bytes": len(raw),
                                   "sha256": hashlib.sha256(raw).hexdigest()})
        return json.loads(raw, object_pairs_hook=unique)

    try:
        network(get(ALGOD, "versions"), get(INDEXER, "v2/blocks/0"))
        selected = get(INDEXER, "v2/accounts?limit=3&exclude=all")
        uint64(selected["current-round"])
        rows = selected["accounts"]
        if not isinstance(rows, list) or len(rows) != 3:
            raise ValueError("Exactly three catalogue records required")
        keys = [address(row["address"]) for row in rows]
        if len(set(keys)) != 3:
            raise ValueError("Unique account catalogue required")
        records = [account(get(INDEXER, "v2/accounts/" + key + "?exclude=all"), key) for key in keys]
        report.update(passed=True, network=NETWORK, genesisHash=GENESIS, records=records)
    except Exception as error:
        report.update(passed=False, errorType=type(error).__name__)
    (folder / "readiness.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": report["passed"], "requests": len(report["requests"]),
                      "records": len(report.get("records", [])), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
