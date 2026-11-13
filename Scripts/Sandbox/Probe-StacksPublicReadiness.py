"""One public mainnet info observation; qualify captured explicit-tip reads next."""
import hashlib
import argparse
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import HTTPRedirectHandler, Request, build_opener
from urllib.parse import quote, urlencode

ROOT = Path(__file__).resolve().parents[2]
BASE = "https://api.hiro.so"
PRINCIPALS = ("SP000000000000000000002Q6VF78", "SP000000000000000000002Q6VF78.pox-4", "SP000000000000000000002Q6VF78.pox-5")


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


def uint64(value):
    if type(value) is not int or not 0 <= value <= (1 << 64) - 1:
        raise ValueError("Exact uint64 required")
    return str(value)


def tip_from_info(value):
    if (not isinstance(value, dict) or type(value.get("network_id")) is not int
            or value["network_id"] != 1 or value.get("is_fully_synced") is not True):
        raise ValueError("Synchronized mainnet info required")
    block = value.get("stacks_tip")
    consensus = value.get("stacks_tip_consensus_hash")
    if (not isinstance(block, str) or not re.fullmatch(r"[0-9a-f]{64}", block)
            or not isinstance(consensus, str) or not re.fullmatch(r"[0-9a-f]{40}", consensus)):
        raise ValueError("Canonical unprefixed block/consensus hashes required")
    uint64(value.get("stacks_tip_height"))
    # Exact StacksBlockId::new order from the reference node: block first,
    # then consensus. SHA-512/256 is NOT SHA-512 truncated to32bytes.
    return hashlib.new("sha512_256", bytes.fromhex(block + consensus)).hexdigest()


def account(value):
    if not isinstance(value, dict) or set(value) != {"balance", "locked", "unlock_height", "nonce"}:
        raise ValueError("Complete proof=0 account response required")
    result = {}
    for field in ("balance", "locked"):
        text = value[field]
        if not isinstance(text, str) or not re.fullmatch(r"0x[0-9a-f]{32}", text):
            raise ValueError("Native fixed-width uint128 hex required")
        result[field] = str(int(text[2:], 16))
    for field in ("unlock_height", "nonce"):
        result[field] = uint64(value[field])
    return result


def qualify_captured_tip():
    folder = ROOT / "TestResults/StacksPublicSourceCurrent"
    raw = (folder / "info-response.json").read_bytes()
    proof = json.loads((folder / "info.observed.json").read_text(encoding="utf-8"))
    if proof.get("nativeAvailable") is not True or proof.get("sha256") != hashlib.sha256(raw).hexdigest():
        raise ValueError("Original successful info observation required")
    info = json.loads(raw, object_pairs_hook=unique)
    tip = tip_from_info(info)
    # Every tip string must be exactly64lowercase hex. A malformed selector
    # can make the node silently choose its current tip instead.
    assert re.fullmatch(r"[0-9a-f]{64}", tip)
    with (folder / "qualification.intent.json").open("x", encoding="utf-8") as stream:
        json.dump({"endpoint": BASE, "tip": tip, "infoSha256": proof["sha256"],
                   "principals": PRINCIPALS, "nativeWrites": 0, "retryAllowed": False}, stream)
    report = {"qualified": False, "nativeWrites": 0, "tip": tip, "rows": [], "attempts": []}
    try:
        opener = build_opener(NoRedirect)
        for index, principal in enumerate(PRINCIPALS):
            attempt = {"principal": principal}
            report["attempts"].append(attempt)
            request = Request(BASE + "/v2/accounts/" + quote(principal, safe="") + "?" + urlencode({"proof": "0", "tip": tip}),
                              headers={"Accept": "application/json"})
            try:
                response = opener.open(request, timeout=15)
            except HTTPError as error:
                response = error
            with response:
                attempt["httpStatus"] = response.status
                body = response.read(65537)
            if len(body) > 65536:
                raise ValueError("Account response byte budget exceeded")
            with (folder / f"account-{index}.json").open("xb") as stream:
                stream.write(body)
            attempt["sha256"] = hashlib.sha256(body).hexdigest()
            if attempt["httpStatus"] != 200:
                raise ValueError("Explicit-tip account read failed")
            report["rows"].append({"principal": principal, **account(json.loads(body, object_pairs_hook=unique))})
        report["qualified"] = True
    except Exception as error:
        report["errorType"] = type(error).__name__
        if getattr(error, "reason", None) is not None:
            report["reasonType"] = type(error.reason).__name__
    with (folder / "qualification.json").open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps({"qualified": report["qualified"], "rows": len(report["rows"]), "tip": tip,
                      "statuses": [row.get("httpStatus") for row in report["attempts"]]}))
    return 0 if report["qualified"] else 1


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--qualify-captured-tip", action="store_true")
    if parser.parse_args().qualify_captured_tip:
        return qualify_captured_tip()
    folder = ROOT / "TestResults/StacksPublicSourceCurrent"
    folder.mkdir(parents=True, exist_ok=True)
    with (folder / "info.intent.json").open("x", encoding="utf-8") as stream:
        json.dump({"endpoint": BASE, "route": "/v2/info", "utc": datetime.now(timezone.utc).isoformat(),
                   "nativeWrites": 0, "retryAllowed": False}, stream)
    report = {"nativeAvailable": False, "cqlQualified": False, "nativeWrites": 0}
    try:
        request = Request(BASE + "/v2/info", headers={"Accept": "application/json"})
        try:
            response = build_opener(NoRedirect).open(request, timeout=15)
        except HTTPError as error:
            response = error
        with response:
            report["httpStatus"] = response.status
            raw = response.read(65537)
        if len(raw) > 65536:
            raise ValueError("Native response byte budget exceeded")
        with (folder / "info-response.json").open("xb") as stream:
            stream.write(raw)
        report["bytes"] = len(raw)
        report["sha256"] = hashlib.sha256(raw).hexdigest()
        if report["httpStatus"] != 200:
            raise ValueError("Native info lookup failed")
        value = json.loads(raw, object_pairs_hook=unique)
        if not isinstance(value, dict):
            raise ValueError("Native info object required")
        report["rootFields"] = list(value)
        report["nativeAvailable"] = True
    except Exception as error:
        report["errorType"] = type(error).__name__
        if getattr(error, "reason", None) is not None:
            report["reasonType"] = type(error.reason).__name__
    with (folder / "info.observed.json").open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps(report))
    return 0 if report["nativeAvailable"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
