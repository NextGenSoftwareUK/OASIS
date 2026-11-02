"""One bounded anonymous alias qualification; no login, joins or retries."""
import hashlib
import json
import argparse
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import quote
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
ENDPOINT = "https://matrix.org"
ALIASES = ("#matrix:matrix.org", "#synapse:matrix.org", "#element:matrix.org")


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


def validate(value):
    if not isinstance(value, dict):
        raise ValueError("Object required")
    room = value.get("room_id")
    servers = value.get("servers")
    # Matrix v12 IDs have no domain; IDs are opaque across room versions.
    if not isinstance(room, str) or not room.startswith("!") or not 2 <= len(room) <= 255 or any(ord(c) <= 32 for c in room):
        raise ValueError("Room ID required")
    if not isinstance(servers, list) or not 1 <= len(servers) <= 4096:
        raise ValueError("Bounded server list required")
    if any(not isinstance(server, str) or not server or len(server) > 255 for server in servers):
        raise ValueError("Server names required")
    return {"roomId": room, "servers": servers}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--continue-validated-first", action="store_true")
    args = parser.parse_args()
    target = ROOT / "TestResults/MatrixPublicSourceCurrent"
    target.mkdir(parents=True, exist_ok=True)
    # Exclusive intent precedes network I/O. Preserve outcomes; never turn a
    # failed attempt into a success by overwriting it or changing the cohort.
    intent = target / "qualification.intent.json"
    report_path = target / "qualification.json"
    if args.continue_validated_first:
        previous = json.loads(report_path.read_text(encoding="utf-8"))
        original = json.loads(intent.read_text(encoding="utf-8"))
        raw = (target / "alias-0.json").read_bytes()
        if (original["endpoint"] != ENDPOINT or original["aliases"] != list(ALIASES)
                or previous["passed"] or previous["rows"] or len(previous["attempts"]) != 1
                or previous["attempts"][0]["alias"] != ALIASES[0]
                or previous["attempts"][0]["httpStatus"] != 200
                or hashlib.sha256(raw).hexdigest() != previous["attempts"][0]["sha256"]):
            raise ValueError("Original first-response continuation evidence differs")
        first = validate(json.loads(raw, object_pairs_hook=unique))
        intent = target / "continuation.intent.json"
        report_path = target / "continuation.json"
    with intent.open("x", encoding="utf-8") as stream:
        json.dump({"endpoint": ENDPOINT, "aliases": ALIASES,
                   "utc": datetime.now(timezone.utc).isoformat()}, stream)
    report = {"passed": False, "mutations": 0, "rows": [], "attempts": []}
    if args.continue_validated_first:
        report["rows"].append({"alias": ALIASES[0], **first})
        report["reusedFirstResponseSha256"] = hashlib.sha256(raw).hexdigest()
    try:
        opener = build_opener(NoRedirect)
        for index, alias in enumerate(ALIASES):
            if args.continue_validated_first and index == 0:
                continue
            attempt = {"alias": alias}
            report["attempts"].append(attempt)
            request = Request(ENDPOINT + "/_matrix/client/v3/directory/room/" + quote(alias, safe=""),
                              headers={"Accept": "application/json"})
            try:
                response = opener.open(request, timeout=15)
            except HTTPError as error:
                attempt["httpStatus"] = error.code
                response = error
            with response:
                attempt["httpStatus"] = response.status
                raw = response.read(65537)
            if len(raw) > 65536:
                raise ValueError("Response budget exceeded")
            with (target / f"alias-{index}.json").open("xb") as stream:
                stream.write(raw)
            attempt["sha256"] = hashlib.sha256(raw).hexdigest()
            attempt["bytes"] = len(raw)
            if attempt["httpStatus"] != 200:
                raise ValueError("Native alias lookup failed")
            report["rows"].append({"alias": alias, **validate(json.loads(raw, object_pairs_hook=unique))})
        report["passed"] = True
    except Exception as error:
        report["errorType"] = type(error).__name__
    with report_path.open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps({"passed": report["passed"], "rows": len(report["rows"]),
                      "statuses": [item.get("httpStatus") for item in report["attempts"]]}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
