"""Pin official registry metadata for the admitted exact PouchDB package graph.

Downloads bounded version metadata only, never package archives or executables.
Each version has one durable request intent/response cache; incomplete outcomes
need explicit engineering reconciliation, not automatic HTTP retries.
"""
import base64
import concurrent.futures
import hashlib
import json
import os
from pathlib import Path
import re
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
LOCK = ROOT / "Docker/PouchDbRuntime/package-lock.json"
CACHE = ROOT / "LocalState/COSMIC/RecoveryLaptopPouchDBCql/hosted-preparation/registry-metadata"


def write_new(path, value):
    with path.open("x", encoding="utf-8") as handle:
        json.dump(value, handle, separators=(",", ":"))
        handle.flush()
        os.fsync(handle.fileno())


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise RuntimeError("Unexpected registry metadata redirect.")


def fetch(pair):
    name, version = pair
    if not re.fullmatch(r"(?:@[A-Za-z0-9._~-]+/)?[A-Za-z0-9._~-]+", name) or not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+(?:[-+][A-Za-z0-9.+-]+)?", version):
        raise ValueError("An exact native package identity is required.")
    key = hashlib.sha256((name + "@" + version).encode()).hexdigest()
    intent, outcome = CACHE / (key + ".intent.json"), CACHE / (key + ".json")
    url = "https://registry.npmjs.org/" + urllib.parse.quote(name, safe="@") + "/" + version
    if outcome.exists():
        recorded = json.loads(outcome.read_text(encoding="utf-8"))
        if recorded["name"] != name or recorded["version"] != version or recorded["url"] != url:
            raise RuntimeError("Registry metadata cache ownership differs.")
        raw = base64.b64decode(recorded["responseBase64"], validate=True)
        if hashlib.sha256(raw).hexdigest() != recorded["responseSha256"]:
            raise RuntimeError("Registry metadata cache hash differs.")
    else:
        if intent.exists():
            raise RuntimeError("Incomplete metadata request must be reconciled: " + name + "@" + version)
        write_new(intent, {"name": name, "version": version, "url": url})
        opener = urllib.request.build_opener(NoRedirect)
        with opener.open(urllib.request.Request(url, headers={"Accept": "application/json"}), timeout=20) as response:
            if response.status != 200 or response.geturl() != url:
                raise RuntimeError("Registry version response identity differs.")
            raw = response.read(262145)
            if not raw or len(raw) > 262144:
                raise RuntimeError("Registry version metadata exceeds its bound.")
        write_new(outcome, {"name": name, "version": version, "url": url,
                           "responseBase64": base64.b64encode(raw).decode(), "responseSha256": hashlib.sha256(raw).hexdigest()})
    metadata = json.loads(raw)
    if metadata["name"] != name or metadata["version"] != version:
        raise RuntimeError("Official registry returned a different package/version.")
    dist = metadata["dist"]
    uri = urllib.parse.urlsplit(dist["tarball"])
    if uri.scheme != "https" or uri.hostname != "registry.npmjs.org" or uri.username or uri.password or uri.query or uri.fragment:
        raise RuntimeError("Official package archive origin differs.")
    integrity = dist.get("integrity")
    if not isinstance(integrity, str) or not re.fullmatch(r"sha(?:1|256|384|512)-[A-Za-z0-9+/]+=*", integrity):
        raise RuntimeError("Official integrity metadata is required: " + name + "@" + version)
    return pair, {"resolved": dist["tarball"], "integrity": integrity}, len(raw)


def main():
    if (CACHE.parent / "registry-pins.ack.json").exists():
        raise RuntimeError("Preserve the original completed registry pin receipt.")
    original = LOCK.read_bytes()
    lock = json.loads(original)
    packages = lock["packages"]
    identities = {}
    for key, value in packages.items():
        if not key:
            continue
        if not key.startswith("node_modules/") or value.get("extraneous"):
            raise ValueError("Only the admitted native dependency graph may be pinned.")
        identities[key] = (key.rsplit("node_modules/", 1)[1], value["version"])
    CACHE.mkdir(parents=True, exist_ok=True)
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as workers:
        rows = list(workers.map(fetch, sorted(set(identities.values()))))
    captured = {pair: value for pair, value, _ in rows}
    for key, pair in identities.items():
        packages[key].update(captured[pair])
    updated = (json.dumps(lock, indent=2) + "\n").encode()
    if LOCK.read_bytes() != original:
        raise RuntimeError("Lockfile changed during metadata capture.")
    LOCK.write_bytes(updated)
    receipt = {"exactPackagePaths": len(identities), "uniqueVersions": len(rows), "metadataBytes": sum(size for _, _, size in rows),
               "beforeSha256": hashlib.sha256(original).hexdigest(), "afterSha256": hashlib.sha256(updated).hexdigest(),
               "packageArchivesDownloaded": 0, "nativeInstallations": 0}
    write_new(CACHE.parent / "registry-pins.ack.json", receipt)
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
