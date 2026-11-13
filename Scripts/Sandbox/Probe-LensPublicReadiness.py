"""Qualify a bounded, anonymous Lens v3 account projection; never mutate Lens."""
import hashlib
import json
import re
import time
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
ENDPOINT = "https://api.lens.xyz/graphql"
SDK_REVISION = "342abd5efb3b12d306c89101a00e60f722df7f33"
FIELDS = "address owner username { value } metadata { name bio }"
DISCOVERY = "query { accounts(request: { pageSize: TEN }) { items { " + FIELDS + " } } }"
BULK = "query($request: AccountsBulkRequest!) { accountsBulk(request: $request) { " + FIELDS + " } }"
BYTE_LIMIT = 1048576


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


def address(value):
    if not isinstance(value, str) or not re.fullmatch(r"0x[0-9a-fA-F]{40}", value):
        raise ValueError("Exact EVM address required")
    return value.lower()


def optional_text(value, limit):
    if value is not None and (not isinstance(value, str) or len(value.encode("utf-8")) > limit):
        raise ValueError("Bounded nullable text required")
    return value


def account(value):
    if not isinstance(value, dict) or set(value) != {"address", "owner", "username", "metadata"}:
        raise ValueError("Complete selected account required")
    username = value["username"]
    if username is not None:
        if not isinstance(username, dict) or set(username) != {"value"}:
            raise ValueError("Complete selected username required")
        username = optional_text(username["value"], 1024)
        if username is None:
            raise ValueError("Non-null username value required")
    metadata = value["metadata"]
    if metadata is not None:
        if not isinstance(metadata, dict) or set(metadata) != {"name", "bio"}:
            raise ValueError("Complete selected metadata required")
        metadata = {"name": optional_text(metadata["name"], 4096),
                    "bio": optional_text(metadata["bio"], 16384)}
    return {"address": address(value["address"]), "owner": address(value["owner"]),
            "username": username, "metadata": metadata}


def rows(value, field, expected=None):
    # A GraphQL HTTP200 can contain errors and partial data. Never accept that.
    if not isinstance(value, dict) or "errors" in value or not isinstance(value.get("data"), dict):
        raise ValueError("Successful GraphQL data required")
    data = value["data"]
    if set(data) != {field}:
        raise ValueError("Exact selected root required")
    items = data[field]
    if field == "accounts":
        if not isinstance(items, dict) or set(items) != {"items"}:
            raise ValueError("Complete discovery page required")
        items = items["items"]
    if not isinstance(items, list) or not 1 <= len(items) <= 10:
        raise ValueError("Bounded nonempty account array required")
    result = [account(item) for item in items]
    keys = [item["address"] for item in result]
    if len(keys) != len(set(keys)):
        raise ValueError("Duplicate account identity")
    if expected is not None and set(keys) != set(expected):
        raise ValueError("Exact requested key set required")
    return sorted(result, key=lambda item: item["address"])


def read_bounded(response, started):
    chunks = []
    size = 0
    while True:
        if time.monotonic() - started > 15:
            raise ValueError("Native elapsed-time budget exceeded")
        chunk = response.read1(min(16384, BYTE_LIMIT + 1 - size))
        if not chunk:
            return b"".join(chunks)
        size += len(chunk)
        if size > BYTE_LIMIT:
            raise ValueError("Native byte budget exceeded")
        chunks.append(chunk)


def main():
    durable = ROOT / "LocalState/COSMIC/LensPublicQualificationCurrent"
    output = ROOT / "TestResults/LensPublicSourceCurrent"
    durable.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    intent = {"endpoint": ENDPOINT, "sdkRevision": SDK_REVISION, "maxRequests": 3,
              "maxBytesPerResponse": BYTE_LIMIT, "timeoutSecondsPerRequest": 15,
              "selection": "first3-by-address-within-first-score-ranked-page-of10",
              "nativeWrites": 0, "utc": datetime.now(timezone.utc).isoformat()}
    # Durable exclusive intent survives TestResults cleanup. A failed attempt is consumed.
    with (durable / "qualification.intent.json").open("x", encoding="utf-8") as stream:
        json.dump(intent, stream, indent=2)
    report = {"passed": False, "nativeWrites": 0, "attempts": []}
    opener = build_opener(NoRedirect)

    def fetch(label, query, variables):
        attempt = {"label": label}
        report["attempts"].append(attempt)
        request = Request(ENDPOINT, data=json.dumps({"query": query, "variables": variables}).encode(),
                          headers={"Accept": "application/json", "Content-Type": "application/json"},
                          method="POST")
        started = time.monotonic()
        try:
            response = opener.open(request, timeout=15)
        except HTTPError as error:
            response = error
        with response:
            attempt["httpStatus"] = response.status
            raw = read_bounded(response, started)
        attempt["elapsedSeconds"] = round(time.monotonic() - started, 3)
        attempt["bytes"] = len(raw)
        attempt["sha256"] = hashlib.sha256(raw).hexdigest()
        with (output / (label + ".json")).open("xb") as stream:
            stream.write(raw)
        if len(raw) > BYTE_LIMIT or attempt["elapsedSeconds"] > 15:
            raise ValueError("Native response budget exceeded")
        if attempt["httpStatus"] != 200:
            raise ValueError("Native HTTP request failed")
        return json.loads(raw, object_pairs_hook=unique)

    try:
        discovered = rows(fetch("discovery", DISCOVERY, {}), "accounts")
        if len(discovered) < 3:
            raise ValueError("At least3real accounts required")
        keys = [item["address"] for item in discovered[:3]]
        first = rows(fetch("bulk-three", BULK, {"request": {"addresses": keys}}), "accountsBulk", keys)
        second = rows(fetch("bulk-two", BULK, {"request": {"addresses": keys[:2]}}), "accountsBulk", keys[:2])
        report.update(passed=True, addresses=keys, counts=[len(first), len(second)],
                      policy="indexer-reported-public-account-metadata", consensusVerified=False)
    except Exception as error:
        report["errorType"] = type(error).__name__
        report["error"] = str(error)[:512]
    for target in (durable / "qualification.json", output / "qualification.json"):
        with target.open("x", encoding="utf-8") as stream:
            json.dump(report, stream, indent=2)
    print(json.dumps({"passed": report["passed"], "attempts": len(report["attempts"]),
                      "counts": report.get("counts"), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
