"""Anonymous account feasibility at one per-query API-reported ledger version."""
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import Request, build_opener, HTTPRedirectHandler

ENDPOINT = "https://api.mainnet.aptoslabs.com/v1/"
ACCOUNTS = ["0x" + format(number, "064x") for number in (1, 3, 4)]


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
    if not isinstance(value, str) or not re.fullmatch(r"0|[1-9][0-9]{0,19}", value) or int(value) >= 1 << 64:
        raise ValueError("Canonical UInt64 decimal string required")
    return int(value)


def hex32(value):
    if not isinstance(value, str) or not re.fullmatch(r"0x[0-9a-f]{64}", value):
        raise ValueError("Canonical 32-byte lowercase hex required")
    return value


def chain(headers, selected=None):
    if headers.get("x-aptos-chain-id") != "1":
        raise ValueError("Explicit API-reported Mainnet chain ID required")
    current = uint64(headers["x-aptos-ledger-version"])
    oldest = uint64(headers["x-aptos-ledger-oldest-version"])
    if oldest > current or (selected is not None and not oldest <= selected <= current):
        raise ValueError("Requested version outside the reported retained range")
    return current, oldest


def ledger(body, headers):
    current, oldest = chain(headers)
    if (type(body["chain_id"]) is not int or body["chain_id"] != 1 or
            uint64(body["ledger_version"]) != current or uint64(body["oldest_ledger_version"]) != oldest):
        raise ValueError("Ledger body/header identity differs")
    return current


def account(body, headers, address, selected):
    address = hex32(address)
    chain(headers, selected)
    if set(body) != {"sequence_number", "authentication_key"}:
        raise ValueError("Exact account response required")
    uint64(body["sequence_number"])
    return {"address": address, "sequenceNumber": body["sequence_number"],
            "authenticationKey": hex32(body["authentication_key"]), "selectedLedgerVersion": str(selected)}


def main():
    folder = Path(__file__).resolve().parents[2] / "TestResults/AptosPublicSourceCandidateCurrent"
    folder.mkdir(parents=True, exist_ok=True)
    report = {"utc": datetime.now(timezone.utc).isoformat(), "endpoint": ENDPOINT, "sourceWrites": 0,
              "scope": "Account state requested at one API-reported per-query version; no independent state/consensus proof or cross-provider snapshot",
              "requests": []}
    opener = build_opener(NoRedirect())

    def get(path):
        with opener.open(Request(ENDPOINT + path, headers={"Accept": "application/json"}), timeout=20) as response:
            raw = response.read(262145)
            headers = {key.lower(): value for key, value in response.headers.items() if key.lower().startswith("x-aptos-")}
        if len(raw) > 262144:
            raise ValueError("Response exceeds256KiB")
        report["requests"].append({"path": path, "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()})
        return json.loads(raw, object_pairs_hook=unique), headers

    try:
        body, headers = get("")
        selected = ledger(body, headers)
        rows = []
        for address in ACCOUNTS:
            body, headers = get("accounts/" + address + "?ledger_version=" + str(selected))
            rows.append(account(body, headers, address, selected))
        report.update(passed=True, chainId=1, selectedLedgerVersion=str(selected), records=rows)
    except Exception as error:
        report.update(passed=False, errorType=type(error).__name__, httpStatus=getattr(error, "code", None))
    (folder / "readiness.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": report["passed"], "requests": len(report["requests"]),
                      "records": len(report.get("records", [])), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
