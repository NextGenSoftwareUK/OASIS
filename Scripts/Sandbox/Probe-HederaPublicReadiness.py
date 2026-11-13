"""Qualify explicit latest-indexed account observations; not consensus proof."""
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import Request, build_opener, HTTPRedirectHandler

ENDPOINT = "https://mainnet.mirrornode.hedera.com/api/v1/"
ACCOUNTS = ["0.0.2", "0.0.3", "0.0.98"]


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


def account_id(value):
    if not isinstance(value, str) or not re.fullmatch(r"(0|[1-9][0-9]{0,19})\.(0|[1-9][0-9]{0,19})\.([1-9][0-9]{0,19})", value):
        raise ValueError("Canonical numeric account ID required")
    if any(int(part) >= 1 << 64 for part in value.split(".")):
        raise ValueError("Account ID component exceeds UInt64")
    return value


def balance(value):
    if type(value) is not int or not 0 <= value < 1 << 63:
        raise ValueError("Exact nonnegative Int64 tinybar balance required")
    return str(value)


def timestamp(value):
    if not isinstance(value, str) or not re.fullmatch(r"(0|[1-9][0-9]{0,19})\.[0-9]{9}", value):
        raise ValueError("Exact seconds/nanoseconds timestamp required")
    if int(value.split(".")[0]) >= 1 << 64:
        raise ValueError("Timestamp seconds exceed UInt64")
    return value


def account(body, expected):
    expected = account_id(expected)
    if account_id(body["account"]) != expected or body["deleted"] is not False:
        raise ValueError("Account identity or deleted state differs")
    if not isinstance(body["transactions"], list) or len(body["transactions"]) > 1:
        raise ValueError("Bounded account response required")
    native = body["balance"]
    return {"account": expected, "balanceTinybars": balance(native["balance"]),
            "balanceTimestamp": timestamp(native["timestamp"]), "consistency": "LatestIndexed"}


def main():
    folder = Path(__file__).resolve().parents[2] / "TestResults/HederaPublicSourceCandidateCurrent"
    folder.mkdir(parents=True, exist_ok=True)
    report = {"utc": datetime.now(timezone.utc).isoformat(), "endpoint": ENDPOINT,
              "networkBinding": "Explicit official Mainnet endpoint; account response has no independent network ID",
              "scope": "Mirror-indexed per-account observations; no freshness/shared snapshot/consensus proof",
              "sourceWrites": 0, "requests": []}
    opener = build_opener(NoRedirect())
    try:
        rows = []
        for key in ACCOUNTS:
            path = "accounts/" + account_id(key) + "?limit=1"
            with opener.open(Request(ENDPOINT + path, headers={"Accept": "application/json"}), timeout=20) as response:
                raw = response.read(262145)
            if len(raw) > 262144:
                raise ValueError("Response exceeds256KiB")
            report["requests"].append({"path": path, "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()})
            rows.append(account(json.loads(raw, object_pairs_hook=unique), key))
        report.update(passed=True, records=rows)
    except Exception as error:
        report.update(passed=False, errorType=type(error).__name__, httpStatus=getattr(error, "code", None))
    (folder / "readiness.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": report["passed"], "requests": len(report["requests"]),
                      "records": len(report.get("records", [])), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
