"""Qualify three linked Bitcoin mainnet headers; not CQL or consensus certification."""
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import HTTPRedirectHandler, build_opener

ENDPOINT = "https://blockstream.info/api"
GENESIS = "000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f"
LIMIT = 0xffff << (8 * (0x1d - 3))


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ValueError("Redirects are not accepted")


def hash_text(value):
    if not isinstance(value, str) or not re.fullmatch(r"[0-9a-f]{64}", value):
        raise ValueError("Canonical block hash required")
    return value


def validate_header(value, expected_hash, expected_parent):
    """Recompute the native identity and check its declared proof-of-work target."""
    hash_text(expected_hash)
    hash_text(expected_parent)
    if not isinstance(value, str) or not re.fullmatch(r"[0-9a-f]{160}", value):
        raise ValueError("Exactly 80 canonical header bytes required")
    header = bytes.fromhex(value)
    digest = hashlib.sha256(hashlib.sha256(header).digest()).digest()
    if digest[::-1].hex() != expected_hash:
        raise ValueError("Double-SHA256 identity mismatch")
    if header[4:36][::-1].hex() != expected_parent:
        raise ValueError("Previous block identity mismatch")
    bits = int.from_bytes(header[72:76], "little")
    exponent, coefficient = bits >> 24, bits & 0x7fffff
    target = (coefficient >> (8 * (3 - exponent)) if exponent <= 3
              else coefficient << (8 * (exponent - 3)))
    if bits & 0x800000 or not 0 < target <= LIMIT:
        raise ValueError("Invalid mainnet proof-of-work target")
    # The selected genesis-era sample has the pinned mainnet difficulty.
    if bits != 0x1d00ffff or int.from_bytes(digest, "little") > target:
        raise ValueError("Pinned difficulty or proof-of-work mismatch")
    return {"hash": expected_hash, "parent": expected_parent,
            "timestamp": int.from_bytes(header[68:72], "little"), "bits": bits}


def main():
    folder = Path(__file__).resolve().parents[2] / "TestResults/BitcoinPublicSourceCandidateCurrent"
    folder.mkdir(parents=True, exist_ok=True)
    report = {"utc": datetime.now(timezone.utc).isoformat(), "endpoint": ENDPOINT,
              "scope": "three genesis-era headers; no full chain/consensus or CQL proof",
              "sourceWrites": 0, "requests": []}
    opener = build_opener(NoRedirect())

    def get(path):
        with opener.open(ENDPOINT + path, timeout=20) as response:
            raw = response.read(1025)
        if len(raw) > 1024:
            raise ValueError("Response exceeds bounded header/hash contract")
        report["requests"].append({"path": path, "bytes": len(raw),
                                   "sha256": hashlib.sha256(raw).hexdigest()})
        return raw.decode("ascii")

    try:
        rows, parent = [], "0" * 64
        for height in range(3):
            block_hash = hash_text(get(f"/block-height/{height}"))
            if height == 0 and block_hash != GENESIS:
                raise ValueError("Mainnet genesis mismatch")
            row = validate_header(get(f"/block/{block_hash}/header"), block_hash, parent)
            row["height"] = height
            rows.append(row)
            parent = block_hash
        report.update(passed=True, records=rows)
    except Exception as error:
        report.update(passed=False, errorType=type(error).__name__, error=str(error))
    path = folder / "readiness.json"
    path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": report["passed"], "requests": len(report["requests"]),
                      "records": len(report.get("records", [])), "evidence": str(path)}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
