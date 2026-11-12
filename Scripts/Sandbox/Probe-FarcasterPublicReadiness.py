"""Qualify three explicit public display messages; no signer, login or retries."""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
ENDPOINT = "https://hub.pinata.cloud"
FIDS = (1, 3, 99)


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        raise ValueError("Redirect rejected")


def unique(pairs):
    value = {}
    for key, item in pairs:
        if key in value:
            raise ValueError("Duplicate property")
        value[key] = item
    return value


def validate(value, fid):
    if not isinstance(value, dict) or "errCode" in value or "error" in value:
        raise ValueError("Native user-data message required")
    data = value.get("data")
    if not isinstance(data, dict):
        raise ValueError("Message data required")
    if (type(data.get("fid")) is not int or data["fid"] != fid
            or data.get("type") != "MESSAGE_TYPE_USER_DATA_ADD"
            or data.get("network") != "FARCASTER_NETWORK_MAINNET"):
        raise ValueError("Exact mainnet FID and message type required")
    timestamp = data.get("timestamp")
    if type(timestamp) is not int or not 0 <= timestamp <= (1 << 32) - 1:
        raise ValueError("Native uint32 Farcaster timestamp required")
    body = data.get("userDataBody")
    if not isinstance(body, dict) or body.get("type") != "USER_DATA_TYPE_DISPLAY":
        raise ValueError("Requested display message required")
    display = body.get("value")
    if not isinstance(display, str) or len(display.encode("utf-8")) > 4096:
        raise ValueError("Bounded display text required")
    return {"fid": str(fid), "display": display, "timestamp": str(timestamp),
            "policy": "hub-reported-display-message", "signatureVerified": False}


def main():
    target = ROOT / "TestResults/FarcasterPublicSourceCurrent"
    target.mkdir(parents=True, exist_ok=True)
    with (target / "qualification.intent.json").open("x", encoding="utf-8") as stream:
        json.dump({"endpoint": ENDPOINT, "fids": FIDS, "userDataType": 2,
                   "utc": datetime.now(timezone.utc).isoformat()}, stream)
    report = {"passed": False, "nativeWrites": 0, "rows": [], "attempts": []}
    try:
        opener = build_opener(NoRedirect)
        for fid in FIDS:
            attempt = {"fid": str(fid)}
            report["attempts"].append(attempt)
            request = Request(ENDPOINT + f"/v1/userDataByFid?fid={fid}&user_data_type=2",
                              headers={"Accept": "application/json"})
            try:
                response = opener.open(request, timeout=15)
            except HTTPError as error:
                response = error
            with response:
                attempt["httpStatus"] = response.status
                raw = response.read(65537)
            if len(raw) > 65536:
                raise ValueError("Native response byte budget exceeded")
            with (target / f"fid-{fid}.json").open("xb") as stream:
                stream.write(raw)
            attempt["bytes"] = len(raw)
            attempt["sha256"] = hashlib.sha256(raw).hexdigest()
            if attempt["httpStatus"] != 200:
                raise ValueError("Native read failed")
            report["rows"].append(validate(json.loads(raw, object_pairs_hook=unique), fid))
        report["passed"] = True
    except Exception as error:
        report["errorType"] = type(error).__name__
    with (target / "qualification.json").open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps({"passed": report["passed"], "rows": len(report["rows"]),
                      "statuses": [item.get("httpStatus") for item in report["attempts"]]}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
