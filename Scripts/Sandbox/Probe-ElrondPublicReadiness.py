"""Read-only MultiversX account membership qualification, not CQL certification."""
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import urlencode
from urllib.request import Request, build_opener, HTTPRedirectHandler

API = "https://api.multiversx.com/"
ALPHABET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l"


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


def address(value):
    if not isinstance(value, str) or not re.fullmatch(r"erd1[qpzry9x8gf2tvdw0s3jn54khce6mua7l]{58}", value):
        raise ValueError("Canonical 32-byte erd Bech32 address required")
    words = [ALPHABET.index(character) for character in value[4:]]
    expanded = [ord(c) >> 5 for c in "erd"] + [0] + [ord(c) & 31 for c in "erd"]
    check = 1
    for word in expanded + words:
        top = check >> 25
        check = ((check & 0x1ffffff) << 5) ^ word
        for i, generator in enumerate((0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3)):
            if (top >> i) & 1:
                check ^= generator
    # 32 bytes occupy 52 five-bit words, with four zero padding bits.
    if check != 1 or words[-7] & 15:
        raise ValueError("Invalid address checksum or padding")
    return value


def validate_network(value):
    if not isinstance(value, dict) or value.get("id") != "mainnet" or value.get("chainId") != "1" or value.get("egldDenomination") != "18":
        raise ValueError("API-reported Mainnet identity/denomination differs")


def nonce(value):
    # The public API emits a JSON number. Do not claim UInt64 fidelity beyond
    # the safe native JSON-number domain of the indexing service.
    if type(value) is not int or not 0 <= value <= (1 << 53) - 1:
        raise ValueError("Exact safe-integer API nonce required")
    return str(value)


def validate(value, keys):
    if not 1 <= len(keys) <= 8 or len(set(keys)) != len(keys):
        raise ValueError("One to eight unique enrolled keys required")
    for key in keys:
        address(key)
    if not isinstance(value, list) or len(value) != len(keys):
        raise ValueError("Complete selected account page required")
    rows = {}
    for row in value:
        key = address(row["address"])
        if key not in keys or key in rows:
            raise ValueError("Missing, duplicate or unexpected account identity")
        balance = row["balance"]
        if not isinstance(balance, str) or not re.fullmatch(r"0|[1-9][0-9]{0,4095}", balance):
            raise ValueError("Canonical bounded exact balance string required")
        shard = row["shard"]
        if type(shard) is not int or shard not in (0, 1, 2, 4294967295):
            raise ValueError("Expected Mainnet shard or metachain identity")
        rows[key] = {"address": key, "balanceBaseUnits": balance, "nonce": nonce(row["nonce"]), "shard": str(shard)}
    return [rows[key] for key in keys]


def request_path(keys):
    for key in keys:
        address(key)
    if not 1 <= len(keys) <= 8 or len(set(keys)) != len(keys):
        raise ValueError("Bounded unique account selection required")
    return "accounts?" + urlencode({"addresses": ",".join(keys), "from": 0, "size": len(keys),
                                     "fields": "address,balance,nonce,shard"})


def main():
    root = Path(__file__).resolve().parents[2]
    source = json.loads((root / "LocalState/COSMIC/HostedElrondPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
    keys = source["addresses"]
    evidence = []

    def get(path):
        with build_opener(NoRedirect).open(Request(API + path, headers={"Accept": "application/json"}), timeout=12) as response:
            raw = response.read(65537)
            if len(raw) > 65536:
                raise ValueError("Public response exceeds64KiB")
            evidence.append({"path": path, "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()})
            return json.loads(raw, object_pairs_hook=unique)

    report = {"observedUtc": datetime.now(timezone.utc).isoformat(), "endpoint": API,
              "passed": False, "mutations": 0, "requests": evidence}
    try:
        validate_network(get("dapp/config"))
        full = validate(get(request_path(keys)), keys)
        subset = validate(get(request_path(keys[:2])), keys[:2])
        report.update(passed=True, full=full, subset=subset, consistency="LatestIndexed")
    except Exception as error:
        report["errorType"] = type(error).__name__
        report["httpStatus"] = getattr(error, "code", None)
    target = root / "TestResults/ElrondPublicSourceCandidateCurrent/qualified-readiness.json"
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": report["passed"], "rows": [len(report.get("full", [])), len(report.get("subset", []))],
                      "sha256": hashlib.sha256(target.read_bytes()).hexdigest(), "errorType": report.get("errorType"),
                      "httpStatus": report.get("httpStatus")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
