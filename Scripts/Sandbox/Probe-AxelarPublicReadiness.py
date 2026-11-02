"""One bounded anonymous GMP catalogue read; no cross-chain dispatch or retries."""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
ENDPOINT = "https://api.axelarscan.io/gmp/searchGMP"

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

def validate(value):
    if not isinstance(value, dict) or value.get("error") or not isinstance(value.get("data"), list) or len(value["data"]) != 3:
        raise ValueError("Three complete native message observations required")
    rows = []
    for record in value["data"]:
        call = record["call"]
        timestamp = call["block_timestamp"]
        if type(timestamp) is not int or not 0 <= timestamp <= (1 << 63) - 1:
            raise ValueError("Exact nonnegative native timestamp required")
        rows.append({"messageId": text(record["message_id"], 128), "status": text(record["status"], 64),
                     "sourceChain": text(call["chain"], 64), "sourceTxHash": text(call["transactionHash"], 128),
                     "destinationChain": text(call["returnValues"]["destinationChain"], 64),
                     "timestamp": str(timestamp)})
    if len({row["messageId"] for row in rows}) != 3:
        raise ValueError("Unique message identities required")
    return rows

def main():
    target = ROOT / "TestResults/AxelarPublicSourceCurrent"
    target.mkdir(parents=True, exist_ok=True)
    body = json.dumps({"size": 3, "from": 0}, separators=(",", ":")).encode()
    with (target / "qualification.intent.json").open("x", encoding="utf-8") as stream:
        json.dump({"endpoint": ENDPOINT, "body": json.loads(body), "readOnlySearch": True,
                   "utc": datetime.now(timezone.utc).isoformat()}, stream)
    report = {"passed": False, "nativeWrites": 0, "policy": "indexer-reported-message-state"}
    try:
        request = Request(ENDPOINT, data=body, method="POST", headers={"Accept": "application/json", "Content-Type": "application/json"})
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
    raise SystemExit(main())
