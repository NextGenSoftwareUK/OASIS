"""Read three Finney headers with verified hashes, not AI/subnet or CQL acceptance."""
import hashlib
import importlib.util
import json
import time
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
ENDPOINT = "https://bittensor-finney.api.onfinality.io/public"
GENESIS = "0x2f0555cc76fc2840a25a6ea3b9637146806f1f44b090c175ffde2a7e5ab36c03"

# Reuse the existing offline SCALE/Blake2b implementation, without invoking
# its Polkadot network probe or changing the accepted Polkadot source.
spec = importlib.util.spec_from_file_location("polkadot_header_codec", Path(__file__).with_name("Probe-PolkadotPublicReadiness.py"))
codec = importlib.util.module_from_spec(spec)
spec.loader.exec_module(codec)


def result(value, request_id):
    if (not isinstance(value, dict) or set(value) != {"jsonrpc", "id", "result"}
            or value["jsonrpc"] != "2.0" or type(value["id"]) is not int
            or value["id"] != request_id):
        raise ValueError("Exact successful JSON-RPC envelope required")
    return value["result"]


def verify_chain(records, finalized):
    if not isinstance(records, list) or len(records) != 3:
        raise ValueError("Three complete header records required")
    expected = finalized
    previous_number = None
    for record in records:
        codec.hex_bytes(expected, 32)
        if set(record) != {"hash", "header"} or record["hash"] != expected:
            raise ValueError("Expected parent identity required")
        if codec.header_hash(record["header"]) != expected:
            raise ValueError("SCALE Blake2b identity differs")
        number = int(record["header"]["number"], 16)
        if previous_number is not None and previous_number != number + 1:
            raise ValueError("Consecutive ancestor heights required")
        previous_number = number
        expected = record["header"]["parentHash"]


def main():
    durable = ROOT / "LocalState/COSMIC/BittensorPublicQualificationCurrent"
    output = ROOT / "TestResults/BittensorPublicSourceCurrent"
    durable.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    intent = {"endpoint": ENDPOINT, "genesis": GENESIS, "maxRequests": 5,
              "maxBytesPerResponse": 262144, "socketTimeoutSeconds": 15,
              "minimumRequestSpacingSeconds": 1, "nativeWrites": 0,
              "utc": datetime.now(timezone.utc).isoformat()}
    with (durable / "qualification.intent.json").open("x", encoding="utf-8") as stream:
        json.dump(intent, stream, indent=2)
    report = {"passed": False, "requests": [], "records": [], "nativeWrites": 0,
              "grandpaProofVerified": False, "aiInferenceVerified": False, "cqlAcceptance": False}
    opener = build_opener(codec.NoRedirect())
    last_request = None

    def rpc(method, params):
        nonlocal last_request
        if last_request is not None:
            time.sleep(max(0, 1 - (time.monotonic() - last_request)))
        last_request = time.monotonic()
        request_id = len(report["requests"]) + 1
        if request_id > 5:
            raise ValueError("Request budget exceeded")
        attempt = {"method": method, "id": request_id}
        report["requests"].append(attempt)
        payload = json.dumps({"jsonrpc": "2.0", "id": request_id, "method": method, "params": params}).encode()
        request = Request(ENDPOINT, payload, {"Content-Type": "application/json", "Accept": "application/json"})
        try:
            response = opener.open(request, timeout=15)
        except HTTPError as error:
            response = error
        with response:
            attempt["httpStatus"] = response.status
            raw = response.read(262145)
        attempt.update(bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())
        with (output / f"rpc-{request_id}.json").open("xb") as stream:
            stream.write(raw)
        if len(raw) > 262144 or attempt["httpStatus"] != 200:
            raise ValueError("Native HTTP status/byte budget rejected")
        return result(json.loads(raw, object_pairs_hook=codec.unique), request_id)

    try:
        if rpc("chain_getBlockHash", [0]) != GENESIS:
            raise ValueError("Finney genesis differs from pinned registry")
        selected = rpc("chain_getFinalizedHead", [])
        finalized = selected
        for _ in range(3):
            codec.hex_bytes(selected, 32)
            header = rpc("chain_getHeader", [selected])
            if codec.header_hash(header) != selected:
                raise ValueError("SCALE Blake2b identity differs")
            report["records"].append({"hash": selected, "header": header})
            selected = header["parentHash"]
        verify_chain(report["records"], finalized)
        report.update(passed=True, genesis=GENESIS, finalizedHead=finalized,
                      policy="node-reported-finalized-header-with-local-hash-verification")
    except Exception as error:
        report.update(errorType=type(error).__name__, error=str(error)[:512])
    for path in (durable / "qualification.json", output / "qualification.json"):
        with path.open("x", encoding="utf-8") as stream:
            json.dump(report, stream, indent=2)
    print(json.dumps({"passed": report["passed"], "requests": len(report["requests"]),
                      "records": len(report["records"]), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
