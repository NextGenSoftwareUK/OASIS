"""One bounded original Tableland version observation; no retry or native writes."""
from pathlib import Path
import gzip
import hashlib
import json
import os
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
URL = "https://tableland.network/api/v1/version"
LIMIT = 262144


def parse_version(raw):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("Duplicate native field.")
            result[key] = value
        return result
    if len(raw) > LIMIT:
        raise ValueError("Native version body exceeds budget.")
    value = json.loads(raw, object_pairs_hook=unique)
    if not isinstance(value, dict):
        raise ValueError("Version object required.")
    for key in ("git_commit", "git_branch", "git_state", "git_summary", "build_date", "binary_version"):
        text = value.get(key)
        if not isinstance(text, str) or not text or len(text) > 256:
            raise ValueError("Documented version fields required.")
    return {key: value[key] for key in
        ("git_commit", "git_branch", "git_state", "git_summary", "build_date", "binary_version")}


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, msg, headers, newurl):
        return None


def write_new(path, value):
    with path.open("xb") as stream:
        stream.write(value)
        stream.flush()
        os.fsync(stream.fileno())


def main():
    state = ROOT / "LocalState/COSMIC/TablelandPublicQualificationCurrent"
    results = ROOT / "TestResults/TablelandPublicSourceCurrent"
    results.mkdir(parents=True, exist_ok=True)
    write_new(state / "version.intent.json", json.dumps({"schema": 1, "method": "GET",
        "url": URL, "timeoutSeconds": 15, "bodyLimit": LIMIT, "retry": False,
        "redirect": False, "nativeWrites": 0}).encode())
    report = {"schema": 1, "url": URL, "nativeWrites": 0, "qualified": False}
    raw = b""
    try:
        # Default transport identity; no custom browser signature, credentials or cookies.
        opener = urllib.request.build_opener(NoRedirect())
        with opener.open(URL, timeout=15) as response:
            report["status"] = response.status
            raw = response.read(LIMIT + 1)
        report["version"] = parse_version(raw)
        report["qualified"] = True
    except urllib.error.HTTPError as error:
        report["status"] = error.code
        raw = error.read(LIMIT + 1)
        report["error"] = "HTTPError"
    except Exception as error:
        # Retain the exact outcome category; do not infer DNS/TLS from URLError.
        report["error"] = type(error).__name__
    report["bytes"] = len(raw)
    report["sha256"] = hashlib.sha256(raw).hexdigest()
    write_new(results / "version-response.bin", raw)
    write_new(state / "version-response.bin.gz", gzip.compress(raw, mtime=0))
    write_new(state / "version.report.json", (json.dumps(report, indent=2) + "\n").encode())
    print(json.dumps(report))


if __name__ == "__main__":
    main()
