"""Bounded Horizon header feasibility; full XDR decoding/CQL remain separate gates."""
import base64
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import Request, build_opener, HTTPRedirectHandler

ENDPOINT = "https://horizon.stellar.org/"
NETWORK = "Public Global Stellar Network ; September 2015"


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


def hash_text(value):
    if not isinstance(value, str) or not re.fullmatch(r"[0-9a-f]{64}", value):
        raise ValueError("Canonical lowercase ledger hash required")
    return value


def validate(row):
    identity, parent = hash_text(row["hash"]), hash_text(row["prev_hash"])
    encoded = row["header_xdr"]
    if not isinstance(encoded, str) or not 1 <= len(encoded) <= 8192:
        raise ValueError("Bounded header XDR required")
    raw = base64.b64decode(encoded, validate=True)
    if base64.b64encode(raw).decode() != encoded or not 36 <= len(raw) <= 4096:
        raise ValueError("Canonical bounded XDR required")
    if hashlib.sha256(raw).hexdigest() != identity or raw[4:36].hex() != parent:
        raise ValueError("Native header hash or fixed parent prefix differs")
    sequence = row["sequence"]
    if type(sequence) is not int or not 0 < sequence < 1 << 32:
        raise ValueError("API-reported UInt32 sequence required")
    return {"hash": identity, "prev_hash": parent, "sequence": sequence,
            "header_xdr": encoded, "protocolVersion": int.from_bytes(raw[:4], "big")}


def main():
    folder = Path(__file__).resolve().parents[2] / "TestResults/StellarPublicSourceCandidateCurrent"
    folder.mkdir(parents=True, exist_ok=True)
    report = {"utc": datetime.now(timezone.utc).isoformat(), "endpoint": ENDPOINT,
              "scope": "XDR hash/fixed parent prefix only; full decoder/consensus/CQL unverified",
              "sourceWrites": 0, "requests": []}
    opener = build_opener(NoRedirect())

    def get(path):
        with opener.open(Request(ENDPOINT + path, headers={"Accept": "application/json"}), timeout=20) as response:
            raw = response.read(262145)
        if len(raw) > 262144:
            raise ValueError("Response exceeds256KiB")
        report["requests"].append({"path": path, "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()})
        return json.loads(raw, object_pairs_hook=unique)

    try:
        root = get("")
        if root["network_passphrase"] != NETWORK:
            raise ValueError("Explicit public-network passphrase required")
        records = get("ledgers?order=desc&limit=3")["_embedded"]["records"]
        if not isinstance(records, list) or len(records) != 3:
            raise ValueError("Exactly three bounded headers required")
        rows = [validate(row) for row in records]
        for previous, current in zip(rows, rows[1:]):
            if previous["prev_hash"] != current["hash"] or previous["sequence"] != current["sequence"] + 1:
                raise ValueError("Parent linkage/API sequence differs")
        report.update(passed=True, network=NETWORK, records=rows)
    except Exception as error:
        report.update(passed=False, errorType=type(error).__name__, error=str(error))
    (folder / "readiness.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": report["passed"], "requests": len(report["requests"]),
                      "records": len(report.get("records", [])), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
