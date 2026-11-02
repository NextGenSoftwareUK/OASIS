"""Capture the documented public Trace recent-record response once, without writes."""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
ENDPOINT = "https://api.dataapis.io/api/v1/data-audit/recent?limit=3"


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        raise ValueError("Redirect rejected")


def unique(pairs):
    value = {}
    for key, item in pairs:
        if key in value:
            raise ValueError("Duplicate JSON property")
        value[key] = item
    return value


def main():
    target = ROOT / "TestResults/StoryTracePublicSourceCurrent"
    target.mkdir(parents=True, exist_ok=True)
    with (target / "discovery.intent.json").open("x", encoding="utf-8") as stream:
        json.dump({"endpoint": ENDPOINT, "utc": datetime.now(timezone.utc).isoformat(),
                   "nativeWrites": 0, "retryAllowed": False}, stream)
    report = {"nativeAvailable": False, "cqlQualified": False, "nativeWrites": 0}
    try:
        request = Request(ENDPOINT, headers={"Accept": "application/json"})
        try:
            response = build_opener(NoRedirect).open(request, timeout=15)
        except HTTPError as error:
            response = error
        with response:
            report["httpStatus"] = response.status
            raw = response.read(131073)
        if len(raw) > 131072:
            raise ValueError("Response byte budget exceeded")
        with (target / "discovery-response.json").open("xb") as stream:
            stream.write(raw)
        report["bytes"] = len(raw)
        report["sha256"] = hashlib.sha256(raw).hexdigest()
        if report["httpStatus"] != 200:
            raise ValueError("Public native discovery failed")
        value = json.loads(raw, object_pairs_hook=unique)
        if not isinstance(value, dict):
            raise ValueError("Native JSON object required")
        # This is discovery only. Observe one actual response shape before
        # choosing a canonical decoder; never guess between array aliases.
        report["rootFields"] = list(value)
        report["nativeAvailable"] = True
    except Exception as error:
        report["errorType"] = type(error).__name__
        if getattr(error, "reason", None) is not None:
            report["reasonType"] = type(error.reason).__name__
    with (target / "discovery.observed.json").open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps(report))
    return 0 if report["nativeAvailable"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
