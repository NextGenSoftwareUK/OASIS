"""Qualify three explicit historical swap observations; no quote, dispatch or retry."""
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
ENDPOINT = "https://chainflip-swap.chainflip.io/"
IDS = ("500000", "500001", "500002")

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

def bounded_text(value, maximum=256):
    if not isinstance(value, str) or not 1 <= len(value) <= maximum or any(ord(c) < 33 or ord(c) > 126 for c in value):
        raise ValueError("Bounded visible ASCII native field required")
    return value

def decimal_text(value, maximum=78):
    if not isinstance(value, str) or not re.fullmatch("0|[1-9][0-9]{0," + str(maximum - 1) + "}", value):
        raise ValueError("Bounded canonical unsigned decimal text required")
    return value

def milliseconds(value):
    if type(value) is not int or not 0 <= value <= (1 << 63) - 1:
        raise ValueError("Exact bounded JSON millisecond integer required")
    return str(value)

def validate(value, identity):
    if not isinstance(value, dict) or "error" in value or "message" in value:
        raise ValueError("Native swap observation required")
    if value.get("swapId") != identity or decimal_text(value["swapId"], 20) != identity or int(identity) > (1 << 64) - 1:
        raise ValueError("Exact swap identity required")
    # These explicit historical candidates must be terminal observations for this
    # qualification. COMPLETED can include a refund; no destination proof follows.
    if value["state"] != "COMPLETED":
        raise ValueError("Historical completed-state observation required")
    # Not every indexed swap has an external deposit (for example protocol swaps).
    # Model absence explicitly; never replace it with a zero amount or another field.
    deposit = value.get("deposit")
    if "deposit" in value and not isinstance(deposit, dict):
        raise ValueError("Present external deposit must be a complete object")
    swap = value["swap"]
    amounts = {name: decimal_text(swap[name]) for name in (
        "originalInputAmount", "remainingInputAmount", "swappedInputAmount",
        "swappedIntermediateAmount", "swappedOutputAmount")}
    return {"swapId": identity, "state": value["state"], "swapAmounts": amounts,
            "externalDepositObserved": deposit is not None,
            "sourceChain": bounded_text(value["srcChain"], 64), "sourceAsset": bounded_text(value["srcAsset"], 64),
            "destinationChain": bounded_text(value["destChain"], 64), "destinationAsset": bounded_text(value["destAsset"], 64),
            "depositAmount": decimal_text(deposit["amount"]) if deposit is not None else None,
            "depositTxRef": bounded_text(deposit["txRef"]) if deposit is not None else None,
            "depositWitnessedAtMilliseconds": milliseconds(deposit["witnessedAt"]) if deposit is not None else None,
            "lastStatechainUpdateAtMilliseconds": milliseconds(value["lastStatechainUpdateAt"]),
            "refundObserved": isinstance(value.get("refundEgress"), dict),
            "policy": "indexer-reported-swap-state", "independentFinalityVerified": False}

def main():
    target = ROOT / "TestResults/ChainflipPublicSourceCurrent"
    target.mkdir(parents=True, exist_ok=True)
    with (target / "qualification.intent.json").open("x", encoding="utf-8") as stream:
        json.dump({"endpoint": ENDPOINT, "ids": IDS, "maximumCalls": 3, "nativeWrites": 0,
                   "utc": datetime.now(timezone.utc).isoformat()}, stream)
    report = {"passed": False, "rows": [], "attempts": [], "nativeWrites": 0}
    try:
        opener = build_opener(NoRedirect)
        for identity in IDS:
            attempt = {"swapId": identity}; report["attempts"].append(attempt)
            request = Request(ENDPOINT + "v2/swaps/" + identity, headers={"Accept": "application/json"})
            try:
                response = opener.open(request, timeout=15)
            except HTTPError as error:
                response = error
            with response:
                attempt["httpStatus"] = response.status
                raw = response.read(262145)
            with (target / ("swap-" + identity + ".json")).open("xb") as stream:
                stream.write(raw)
            attempt.update(bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())
            if len(raw) > 262144 or attempt["httpStatus"] != 200:
                raise ValueError("Native point read failed")
            report["rows"].append(validate(json.loads(raw, object_pairs_hook=unique), identity))
        report["passed"] = True
    except Exception as error:
        report["errorType"] = type(error).__name__
    with (target / "qualification.json").open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps({"passed": report["passed"], "rows": len(report["rows"]), "statuses": [a.get("httpStatus") for a in report["attempts"]], "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1

if __name__ == "__main__":
    raise SystemExit(main())
