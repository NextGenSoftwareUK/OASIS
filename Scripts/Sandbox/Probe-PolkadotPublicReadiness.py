"""Bounded anonymous Polkadot header probe, not provider/CQL acceptance."""
import argparse
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import Request, build_opener, HTTPRedirectHandler

ENDPOINT = "https://polkadot-rpc.publicnode.com/"
GENESIS = "0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3"


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ValueError("Redirects are not accepted")


def compact(number):
    if type(number) is not int or not 0 <= number <= 0xffffffff:
        raise ValueError("UInt32 SCALE input required")
    if number < 64:
        return bytes([number << 2])
    if number < 16384:
        return ((number << 2) | 1).to_bytes(2, "little")
    if number < 1 << 30:
        return ((number << 2) | 2).to_bytes(4, "little")
    return b"\x03" + number.to_bytes(4, "little")


def hex_bytes(value, size=None):
    if not isinstance(value, str) or not re.fullmatch(r"0x(?:[0-9a-f]{2})+", value):
        raise ValueError("Canonical lowercase byte hex required")
    raw = bytes.fromhex(value[2:])
    if size is not None and len(raw) != size:
        raise ValueError("Unexpected native byte width")
    return raw


def header_hash(header):
    if set(header) != {"parentHash", "number", "stateRoot", "extrinsicsRoot", "digest"}:
        raise ValueError("Exact header schema required")
    number = header["number"]
    if not isinstance(number, str) or not re.fullmatch(r"0x(?:0|[1-9a-f][0-9a-f]{0,7})", number):
        raise ValueError("Canonical UInt32 block number required")
    logs = header["digest"]["logs"]
    if set(header["digest"]) != {"logs"} or not isinstance(logs, list) or len(logs) > 64:
        raise ValueError("Bounded digest required")
    encoded = (hex_bytes(header["parentHash"], 32) + compact(int(number, 16))
               + hex_bytes(header["stateRoot"], 32) + hex_bytes(header["extrinsicsRoot"], 32)
               + compact(len(logs)) + b"".join(hex_bytes(log) for log in logs))
    return "0x" + hashlib.blake2b(encoded, digest_size=32).hexdigest()


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON field")
        result[key] = value
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--endpoint", choices=[ENDPOINT, "https://rpc.polkadot.io/"], default=ENDPOINT)
    parser.add_argument("--validate-dotnet", action="store_true", help="Validate the retained .NET probe locally, with no HTTP calls")
    args = parser.parse_args()
    if args.validate_dotnet:
        folder = Path(__file__).resolve().parents[2] / "TestResults/PolkadotPublicSourceCandidateCurrent"
        path = folder / "headers-dotnet.json"
        raw = path.read_bytes()
        if len(raw) > 262144:
            raise ValueError("Probe evidence exceeds256KiB")
        snapshot = json.loads(raw.decode("utf-8-sig"), object_pairs_hook=unique)
        if (snapshot["passed"] is not True or snapshot["genesis"] != GENESIS
                or len(snapshot["records"]) != 3 or len(snapshot["requests"]) != 5):
            raise ValueError("Complete three-header source probe required")
        prior = None
        for row in snapshot["records"]:
            if header_hash(row["header"]) != row["hash"]:
                raise ValueError("SCALE Blake2b identity differs")
            if prior is None:
                if row["hash"] != snapshot["finalizedHead"]:
                    raise ValueError("Reported finalized anchor differs")
            elif (prior["header"]["parentHash"] != row["hash"]
                  or int(prior["header"]["number"], 16) != int(row["header"]["number"], 16) + 1):
                raise ValueError("Header ancestry differs")
            prior = row
        summary = {"passed": True, "records": 3, "nativeRequests": 5, "headerHashesVerified": 3,
                   "grandpaProof": False, "cqlAcceptance": False,
                   "sourceEvidenceSha256": hashlib.sha256(raw).hexdigest()}
        (folder / "validated-headers.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(summary))
        return 0
    endpoint = args.endpoint
    report = {"endpoint": endpoint, "utc": datetime.now(timezone.utc).isoformat(),
              "scope": "header readiness, no GRANDPA proof or CQL acceptance", "requests": []}
    opener = build_opener(NoRedirect())

    def rpc(method, params):
        request_id = len(report["requests"]) + 1
        payload = json.dumps({"jsonrpc": "2.0", "id": request_id, "method": method, "params": params}).encode()
        request = Request(endpoint, payload, {"Content-Type": "application/json", "Accept": "application/json"})
        with opener.open(request, timeout=20) as response:
            raw = response.read(262145)
        if len(raw) > 262144:
            raise ValueError("Response exceeds256KiB")
        report["requests"].append({"method": method, "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()})
        value = json.loads(raw, object_pairs_hook=unique)
        if value.get("jsonrpc") != "2.0" or value.get("id") != request_id or "error" in value:
            raise ValueError("RPC identity/error mismatch")
        return value["result"]

    try:
        if rpc("chain_getBlockHash", [0]) != GENESIS:
            raise ValueError("Polkadot genesis differs")
        selected = rpc("chain_getFinalizedHead", [])
        rows = []
        for index in range(3):
            hex_bytes(selected, 32)
            header = rpc("chain_getHeader", [selected])
            if header_hash(header) != selected:
                raise ValueError("SCALE Blake2b header identity differs")
            if rows and int(header["number"], 16) != int(rows[-1]["header"]["number"], 16) - 1:
                raise ValueError("Parent block number differs")
            rows.append({"hash": selected, "header": header})
            selected = header["parentHash"]
        report.update(passed=True, records=rows)
    except Exception as error:
        report.update(passed=False, errorType=type(error).__name__, error=str(error))
    filename = "readiness.json" if endpoint == ENDPOINT else "readiness-official.json"
    path = Path(__file__).resolve().parents[2] / "TestResults/PolkadotPublicSourceCandidateCurrent" / filename
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: value for key, value in report.items() if key != "records"}, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
