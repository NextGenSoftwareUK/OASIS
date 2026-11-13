"""Bounded anonymous XRPL header qualification, not CQL/validator consensus proof."""
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import Request, build_opener, HTTPRedirectHandler

ENDPOINT = "https://xrplcluster.com/"


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
    if not isinstance(value, str) or not re.fullmatch(r"[0-9A-F]{64}", value):
        raise ValueError("Canonical uppercase XRPL hash required")
    return value


def uint(value, width, decimal_string=False):
    # The documented ledger index accepts a JSON integer or a quoted decimal.
    if decimal_string and isinstance(value, str) and re.fullmatch(r"0|[1-9][0-9]*", value):
        value = int(value)
    if type(value) is not int or not 0 <= value < 1 << width:
        raise ValueError("Bounded unsigned header integer required")
    return value.to_bytes(width // 8, "big")


def header_hash(header):
    """XRPLF LedgerHeader.cpp/HashPrefix.h: LWR prefix and fixed-width fields."""
    encoded = (b"LWR\0" + uint(header["ledger_index"], 32, True)
               + uint(header["total_coins"], 64, True)
               + bytes.fromhex(hash_text(header["parent_hash"]))
               + bytes.fromhex(hash_text(header["transaction_hash"]))
               + bytes.fromhex(hash_text(header["account_hash"]))
               + uint(header["parent_close_time"], 32)
               + uint(header["close_time"], 32)
               + uint(header["close_time_resolution"], 8)
               + uint(header["close_flags"], 8))
    return hashlib.sha512(encoded).digest()[:32].hex().upper()


def validate(result, expected_hash=None):
    if result.get("validated") is not True:
        raise ValueError("Explicit API-reported validation required")
    header = result["ledger"]
    if header.get("closed") is not True:
        raise ValueError("Closed header required")
    observed = hash_text(result["ledger_hash"])
    if header_hash(header) != observed or header["ledger_hash"] != observed:
        raise ValueError("Native SHA512Half header identity differs")
    if expected_hash is not None and observed != expected_hash:
        raise ValueError("Requested ledger identity differs")
    if uint(result["ledger_index"], 32, True) != uint(header["ledger_index"], 32, True):
        raise ValueError("Envelope/header sequence differs")
    return header


def main():
    folder = Path(__file__).resolve().parents[2] / "TestResults/XrplPublicSourceCandidateCurrent"
    folder.mkdir(parents=True, exist_ok=True)
    report = {"utc": datetime.now(timezone.utc).isoformat(), "endpoint": ENDPOINT,
              "scope": "three linked hash-verified headers; network/validation API-reported",
              "sourceWrites": 0, "cqlAcceptance": False, "requests": []}
    opener = build_opener(NoRedirect())

    def rpc(method, parameters):
        request = Request(ENDPOINT, json.dumps({"method": method, "params": [parameters]}).encode(),
                          {"Content-Type": "application/json", "Accept": "application/json"})
        with opener.open(request, timeout=20) as response:
            raw = response.read(262145)
        if len(raw) > 262144:
            raise ValueError("Response exceeds256KiB")
        report["requests"].append({"method": method, "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()})
        root = json.loads(raw, object_pairs_hook=unique)
        result = root["result"]
        if result.get("status") != "success" or "error" in root or "error" in result:
            raise ValueError("Native RPC returned an error")
        return result

    try:
        info = rpc("server_info", {})["info"]
        if type(info.get("network_id")) is not int or info["network_id"] != 0:
            raise ValueError("Explicit API-reported Mainnet network_id0 required")
        report["networkId"] = 0
        selected, previous, rows = None, None, []
        for index in range(3):
            request = {"ledger_hash": selected} if selected else {"ledger_index": "validated"}
            result = rpc("ledger", {**request, "transactions": False, "expand": False})
            header = validate(result, selected)
            if previous and int(previous["ledger_index"]) != int(header["ledger_index"]) + 1:
                raise ValueError("Parent sequence differs")
            rows.append(header)
            previous, selected = header, hash_text(header["parent_hash"])
        report.update(passed=True, records=rows)
    except Exception as error:
        report.update(passed=False, errorType=type(error).__name__, error=str(error))
    path = folder / "readiness.json"
    path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": report["passed"], "requests": len(report["requests"]),
                      "records": len(report.get("records", [])), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
