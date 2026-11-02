"""One bounded anonymous Wormhole operation catalogue read; no dispatch or retries."""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
ENDPOINT = "https://api.wormholescan.io/api/v1/operations?pageSize=3"

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

def text(value, maximum=256):
    if not isinstance(value, str) or not 1 <= len(value) <= maximum or any(ord(c) < 33 or ord(c) > 126 for c in value):
        raise ValueError("Bounded native identifier required")
    return value

def operation(record):
    import re
    chain = record["emitterChain"]
    address = record["emitterAddress"]["hex"]
    sequence = record["sequence"]
    if type(chain) is not int or not 1 <= chain <= 65535:
        raise ValueError("Exact native uint16 emitter chain required")
    if not isinstance(address, str) or not re.fullmatch("[0-9a-f]{64}", address):
        raise ValueError("Canonical32byte emitter required")
    if not isinstance(sequence, str) or not re.fullmatch("0|[1-9][0-9]{0,19}", sequence) or int(sequence) > (1 << 64) - 1:
        raise ValueError("Canonical exact uint64 sequence required")
    identity = str(chain) + "/" + address + "/" + sequence
    if record["id"] != identity:
        raise ValueError("Operation identity differs from emitter tuple")
    return {"operationId": identity, "emitterChain": str(chain), "emitterAddress": address, "sequence": sequence}

def validate(value):
    if not isinstance(value, dict) or "error" in value or not isinstance(value.get("operations"), list) or len(value["operations"]) != 3:
        raise ValueError("Three native operation observations required")
    rows = [operation(record) for record in value["operations"]]
    if len({row["operationId"] for row in rows}) != 3:
        raise ValueError("Unique native operation identities required")
    return rows

def vaa(record):
    import base64
    row = operation(record)
    envelope = record["vaa"]
    encoded = envelope["raw"]
    if not isinstance(encoded, str) or not 1 <= len(encoded) <= 32768:
        raise ValueError("Bounded VAA required")
    raw = base64.b64decode(encoded, validate=True)
    if base64.b64encode(raw).decode() != encoded or len(raw) < 57 or raw[0] != 1:
        raise ValueError("Canonical version1 VAA required")
    count = raw[5]
    offset = 6 + 66 * count
    if count == 0 or len(raw) < offset + 51:
        raise ValueError("Complete signature framing and body required")
    indices = [raw[6 + 66 * i] for i in range(count)]
    if indices != sorted(set(indices)):
        raise ValueError("Strictly increasing guardian indices required")
    guardian = int.from_bytes(raw[1:5], "big")
    if type(envelope["guardianSetIndex"]) is not int or envelope["guardianSetIndex"] != guardian or envelope["isDuplicated"] is not False:
        raise ValueError("Exact guardian-set framing and unambiguous indexed observation required")
    body = raw[offset:]
    chain = int.from_bytes(body[8:10], "big")
    address = body[10:42].hex()
    sequence = int.from_bytes(body[42:50], "big")
    if row["operationId"] != str(chain) + "/" + address + "/" + str(sequence):
        raise ValueError("VAA body tuple differs from API operation")
    return {**row, "guardianSetIndex": str(guardian), "signatureCount": count,
            "timestamp": str(int.from_bytes(body[:4], "big")), "nonce": str(int.from_bytes(body[4:8], "big")),
            "consistencyLevel": body[50], "payloadBytes": len(body) - 51,
            "bodySha256": hashlib.sha256(body).hexdigest(), "vaaBytes": len(raw),
            "signatureVerified": False, "independentFinalityVerified": False}

def qualify_captured():
    target = ROOT / "TestResults/WormholePublicSourceCurrent"
    original = json.loads((target / "response.json").read_bytes(), object_pairs_hook=unique)
    proof = json.loads((target / "qualification.json").read_text())
    if proof.get("passed") is not True or proof.get("sha256") != hashlib.sha256((target / "response.json").read_bytes()).hexdigest():
        raise ValueError("Original successful catalogue required")
    rows = [vaa(record) for record in original["operations"]]
    with (target / "point-reads.intent.json").open("x", encoding="utf-8") as stream:
        json.dump({"ids": [row["operationId"] for row in rows], "maximumCalls": 3, "nativeWrites": 0}, stream)
    report = {"passed": False, "rows": [], "attempts": [], "nativeWrites": 0}
    try:
        opener = build_opener(NoRedirect)
        for index, row in enumerate(rows):
            attempt = {"operationId": row["operationId"]}; report["attempts"].append(attempt)
            request = Request("https://api.wormholescan.io/api/v1/operations/" + row["operationId"], headers={"Accept": "application/json"})
            try:
                response = opener.open(request, timeout=15)
            except HTTPError as error:
                response = error
            with response:
                attempt["httpStatus"] = response.status
                raw = response.read(262145)
            with (target / ("point-" + str(index) + ".json")).open("xb") as stream:
                stream.write(raw)
            attempt.update(bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())
            if len(raw) > 262144 or attempt["httpStatus"] != 200:
                raise ValueError("Exact point read failed")
            point = vaa(json.loads(raw, object_pairs_hook=unique))
            if point["operationId"] != row["operationId"] or point["bodySha256"] != row["bodySha256"]:
                raise ValueError("Point identity or original observed VAA body changed")
            report["rows"].append(point)
        report["passed"] = True
    except Exception as error:
        report["errorType"] = type(error).__name__
    with (target / "point-qualification.json").open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps({"passed": report["passed"], "rows": len(report["rows"]), "statuses": [a.get("httpStatus") for a in report["attempts"]]}))
    return 0 if report["passed"] else 1

def main():
    target = ROOT / "TestResults/WormholePublicSourceCurrent"
    target.mkdir(parents=True, exist_ok=True)
    with (target / "qualification.intent.json").open("x", encoding="utf-8") as stream:
        json.dump({"endpoint": ENDPOINT, "readOnlySearch": True,
                   "utc": datetime.now(timezone.utc).isoformat()}, stream)
    report = {"passed": False, "nativeWrites": 0, "policy": "indexer-reported-operation-state"}
    try:
        request = Request(ENDPOINT, headers={"Accept": "application/json"})
        try:
            response = build_opener(NoRedirect).open(request, timeout=15)
        except HTTPError as error:
            response = error
        with response:
            report["httpStatus"] = response.status
            raw = response.read(262145)
        with (target / "response.json").open("xb") as stream:
            stream.write(raw)
        report.update(bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())
        if len(raw) > 262144 or report["httpStatus"] != 200:
            raise ValueError("Native response failed or exceeded byte budget")
        report["rows"] = validate(json.loads(raw, object_pairs_hook=unique))
        report["passed"] = True
    except Exception as error:
        report["errorType"] = type(error).__name__
    with (target / "qualification.json").open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps({"passed": report["passed"], "httpStatus": report.get("httpStatus"), "rows": len(report.get("rows", [])), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1

if __name__ == "__main__":
    import sys
    if sys.argv[1:] == ["--qualify-captured"]:
        raise SystemExit(qualify_captured())
    if sys.argv[1:]:
        raise SystemExit("Use no arguments for first catalogue, or --qualify-captured for exact point reads.")
    raise SystemExit(main())
