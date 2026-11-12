"""Bounded v6 public metadata reads; no wallet, private notes, writes or retries."""
import hashlib
import json
import re
import time
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
ENDPOINT = "https://aztec-mainnet.drpc.org"
ROLLUP = "0xe37a2663cc9c119ba41572c40fab0bbb23a5402f"
REGISTRY = "0x35b22e09ee0390539439e24f06da43d83f90e298"
ROLLUP_VERSION = 2070860440
BYTE_LIMIT = 262144


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        raise ValueError("Redirect rejected")


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property")
        result[key] = value
    return result


def integer(value):
    if type(value) is not int or not 0 <= value <= 9007199254740991:
        raise ValueError("Exact nonnegative JSON-safe integer required")
    return value


def field_hash(value):
    if not isinstance(value, str) or not re.fullmatch(r"0x[0-9a-f]{64}", value):
        raise ValueError("Canonical32byte node-reported hash required")
    return value


def envelope(value, request_id):
    if (not isinstance(value, dict) or set(value) != {"jsonrpc", "id", "result"}
            or value["jsonrpc"] != "2.0" or type(value["id"]) is not int
            or value["id"] != request_id):
        raise ValueError("Exact successful RPC envelope required")
    return value["result"]


def identity(value):
    if not isinstance(value, dict):
        raise ValueError("NodeInfo object required")
    if (value.get("nodeVersion") != "6.0.0" or type(value.get("l1ChainId")) is not int
            or value["l1ChainId"] != 1 or type(value.get("rollupVersion")) is not int
            or value["rollupVersion"] != ROLLUP_VERSION or value.get("realProofs") is not True):
        raise ValueError("Pinned v6mainnet node identity required")
    addresses = value.get("l1ContractAddresses")
    if (not isinstance(addresses, dict) or not isinstance(addresses.get("rollupAddress"), str)
            or not isinstance(addresses.get("registryAddress"), str)
            or addresses["rollupAddress"].lower() != ROLLUP
            or addresses["registryAddress"].lower() != REGISTRY):
        raise ValueError("Pinned mainnet rollup/registry required")
    return {"nodeVersion": "6.0.0", "l1ChainId": 1, "rollupVersion": ROLLUP_VERSION,
            "rollupAddress": ROLLUP, "registryAddress": REGISTRY,
            "policy": "node-reported-finalized-block-metadata", "proofVerified": False}


def block(value, expected_number):
    fields = {"header", "archive", "hash", "checkpointNumber", "indexWithinCheckpoint", "number"}
    if not isinstance(value, dict) or set(value) != fields:
        raise ValueError("Exact metadata-only BlockResponse required; bodies forbidden")
    number = integer(value["number"])
    if number != expected_number:
        raise ValueError("Returned block number differs")
    header = value["header"]
    if not isinstance(header, dict) or not isinstance(header.get("globalVariables"), dict):
        raise ValueError("Selected block header required")
    if integer(header["globalVariables"].get("blockNumber")) != number:
        raise ValueError("Header and outer block number differ")
    archive = value["archive"]
    last_archive = header.get("lastArchive")
    if not isinstance(archive, dict) or not isinstance(last_archive, dict):
        raise ValueError("Archive snapshots required")
    integer(archive.get("nextAvailableLeafIndex"))
    integer(last_archive.get("nextAvailableLeafIndex"))
    return {"number": str(number), "hash": field_hash(value["hash"]),
            "checkpointNumber": str(integer(value["checkpointNumber"])),
            "indexWithinCheckpoint": str(integer(value["indexWithinCheckpoint"])),
            "archiveRoot": field_hash(archive.get("root")),
            "lastArchiveRoot": field_hash(last_archive.get("root"))}


def main():
    state = ROOT / "LocalState/COSMIC/AztecPublicQualificationCurrent"
    manifest = state / "sdk/manifest.json"
    pinned = json.loads(manifest.read_text())
    if pinned["nodeCommit"] != "1b6de149f4fe1cde3c5fb876d3822289f490cd2a":
        raise ValueError("Pinned schema evidence differs")
    results = ROOT / "TestResults/AztecPublicSourceCurrent"
    results.mkdir(parents=True, exist_ok=True)
    intent = {"endpoint": ENDPOINT, "rollup": ROLLUP, "registry": REGISTRY,
              "version": ROLLUP_VERSION, "schemaManifestSha256": hashlib.sha256(manifest.read_bytes()).hexdigest(),
              "maxRequests": 5, "minimumRequestSpacingSeconds": 1, "responseByteLimit": BYTE_LIMIT,
              "socketTimeoutSeconds": 15, "nativeWrites": 0, "utc": datetime.now(timezone.utc).isoformat()}
    with (state / "qualification.intent.json").open("x", encoding="utf-8") as stream:
        json.dump(intent, stream, indent=2)
    report = {"passed": False, "requests": [], "records": [], "nativeWrites": 0,
              "proofVerified": False, "cqlAcceptance": False}
    opener = build_opener(NoRedirect())
    previous = None

    def rpc(method, params):
        nonlocal previous
        if previous is not None:
            time.sleep(max(0, 1 - (time.monotonic() - previous)))
        previous = time.monotonic()
        request_id = len(report["requests"]) + 1
        if request_id > 5:
            raise ValueError("Native request budget exceeded")
        attempt = {"method": method, "id": request_id}
        report["requests"].append(attempt)
        body = json.dumps({"jsonrpc": "2.0", "id": request_id, "method": method, "params": params}).encode()
        request = Request(ENDPOINT, body, {"Content-Type": "application/json", "Accept": "application/json"})
        try:
            response = opener.open(request, timeout=15)
        except HTTPError as error:
            response = error
        with response:
            attempt["httpStatus"] = response.status
            raw = response.read(BYTE_LIMIT + 1)
        attempt.update(bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())
        with (results / f"rpc-{request_id}.json").open("xb") as stream:
            stream.write(raw)
        if len(raw) > BYTE_LIMIT or attempt["httpStatus"] != 200:
            raise ValueError("Native HTTP status/byte budget failed")
        return envelope(json.loads(raw, object_pairs_hook=unique), request_id)

    try:
        report["identity"] = identity(rpc("aztec_getNodeInfo", []))
        tip = integer(rpc("aztec_getBlockNumber", ["finalized"]))
        if tip < 3:
            raise ValueError("Three existing finalized block records required")
        options = {"includeTransactions": False, "includeL1PublishInfo": False, "includeAttestations": False}
        for number in (tip, tip - 1, tip - 2):
            row = block(rpc("aztec_getBlock", [number, options]), number)
            if report["records"] and report["records"][-1]["lastArchiveRoot"] != row["archiveRoot"]:
                raise ValueError("Consecutive archive snapshots differ")
            report["records"].append(row)
        if len({row["hash"] for row in report["records"]}) != 3:
            raise ValueError("Duplicate native block identity")
        report.update(passed=True, reportedFinalizedNumber=str(tip))
    except Exception as error:
        report.update(errorType=type(error).__name__, error=str(error)[:512])
    for path in (state / "qualification.json", results / "qualification.json"):
        with path.open("x", encoding="utf-8") as stream:
            json.dump(report, stream, indent=2)
    print(json.dumps({"passed": report["passed"], "requests": len(report["requests"]),
                      "records": len(report["records"]), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
