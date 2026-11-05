"""Qualify an explicitly selected D1 Worker without changing Railway settings."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / "LocalState/COSMIC/D1WorkerHostedQualificationCurrent"
MAX_BODY = 4096


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def qualify(endpoint, keys):
    opener = build_opener(NoRedirect())

    def request(selected, method="GET"):
        url = endpoint + ("?" + urlencode({"symbol": selected}, doseq=True) if selected else "")
        try:
            response = opener.open(Request(url, method=method), timeout=15)
        except HTTPError as error:
            response = error
        with response:
            body = response.read(MAX_BODY + 1)
            if len(body) > MAX_BODY:
                raise RuntimeError("D1 Worker response exceeds qualification budget.")
            return response.code, body

    for selected in (keys, keys[:2]):
        status, body = request(selected)
        if status != 200 or json.loads(body) != {"symbols": selected}:
            raise RuntimeError(f"D1 Worker symbol query failed qualification (HTTP {status}).")
    for selection in ([keys[0], keys[0]], [keys[0], "UNENROLLED"], []):
        status, body = request(selection)
        if status != 400 or json.loads(body) != {"error": "Invalid symbol selection"}:
            raise RuntimeError("D1 Worker did not reject an invalid symbol selection.")
    write, _ = request([], method="POST")
    if write != 405:
        raise RuntimeError("D1 Worker did not reject POST.")
    return {"fullSymbols": keys, "subsetSymbols": keys[:2], "duplicateStatus": 400,
            "unenrolledStatus": 400, "unboundedStatus": 400, "writeStatus": write}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.parse_args()
    path = STATE / "source.json"
    raw = path.read_bytes()
    source = json.loads(raw)
    spec = importlib.util.spec_from_file_location(
        "d1_publisher", Path(__file__).with_name("Publish-HostedD1WorkerConfiguration.py"))
    publisher = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(publisher)
    publisher.validate_source(source)
    digest = hashlib.sha256(raw).hexdigest()
    with (STATE / "worker-https-qualification.intent.json").open("x", encoding="utf-8") as file:
        json.dump({"schema": 1, "sourceSha256": digest}, file)
    try:
        proof = qualify(source["endpoint"], publisher.EXPECTED_KEYS)
    except (URLError, TimeoutError, ValueError, RuntimeError) as error:
        with (STATE / "worker-https-qualification.failed.json").open("x", encoding="utf-8") as file:
            json.dump({"schema": 1, "sourceSha256": digest, "qualified": False,
                       "failureType": type(error).__name__}, file)
        raise
    with (STATE / "worker-https-qualified.acknowledged.json").open("x", encoding="utf-8") as file:
        json.dump({"schema": 1, "sourceSha256": digest, **proof}, file)
    print("Qualified D1 Worker HTTPS symbol reads (3/2).")


if __name__ == "__main__":
    main()
