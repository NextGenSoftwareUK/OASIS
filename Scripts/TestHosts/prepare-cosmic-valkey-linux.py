"""Admit one reusable Valkey runtime; never install or start a global service."""
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import tarfile
import urllib.request

BASE = Path("/var/tmp/oasis-runtimes")
ROOT = BASE / "valkey-9.0.4"
PACKAGES = {
    "valkey-tools_9.0.4-0ubuntu0.2_amd64.deb": "c71098433c7aa46478c88c6e791985694df9bf039aa446f3f56551e331853b14",
    "valkey-server_9.0.4-0ubuntu0.2_amd64.deb": "bdfdc4f5ce03c307229d7dbfa45292258c5951093362ac881128680c810d6bad",
}
ADMISSION = ROOT / "cosmic-runtime-admission.json"
if ADMISSION.exists():
    record = json.loads(ADMISSION.read_text())
    assert record["packages"] == PACKAGES
    for relative, digest in record["files"].items():
        assert hashlib.sha256((ROOT / relative).read_bytes()).hexdigest() == digest
    assert (ROOT / "usr/bin/valkey-server").is_symlink()
    assert os.readlink(ROOT / "usr/bin/valkey-server") == "valkey-check-rdb"
    print("Reused independently admitted Valkey9.0.4 runtime")
else:
    if ROOT.exists():
        raise RuntimeError("Unadmitted Valkey runtime exists; preserve and inspect before continuing")
    downloads = BASE / ".downloads"
    downloads.mkdir(exist_ok=True)
    archives = []
    for name, digest in PACKAGES.items():
        archive = downloads / name
        if not archive.exists():
            with urllib.request.urlopen("https://archive.ubuntu.com/ubuntu/pool/main/v/valkey/" + name, timeout=30) as response, archive.open("xb") as output:
                output.write(response.read())
        assert hashlib.sha256(archive.read_bytes()).hexdigest() == digest
        archives.append(archive)
    # Validate the distribution's multicall server link before creating our runtime.
    payload = subprocess.run(["dpkg-deb", "--fsys-tarfile", str(archives[1])], check=True, capture_output=True).stdout
    with tarfile.open(fileobj=io.BytesIO(payload)) as package:
        member = package.getmember("./usr/bin/valkey-server")
        assert member.issym() and member.linkname == "valkey-check-rdb"
    payload = subprocess.run(["dpkg-deb", "--fsys-tarfile", str(archives[0])], check=True, capture_output=True).stdout
    files = {}
    with tarfile.open(fileobj=io.BytesIO(payload)) as package:
        for relative in ["usr/bin/valkey-check-rdb", "usr/bin/valkey-cli", "usr/share/doc/valkey-tools/copyright"]:
            member = package.getmember("./" + relative)
            assert member.isfile()
            target = ROOT / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            with package.extractfile(member) as source, target.open("xb") as output:
                output.write(source.read())
            target.chmod(member.mode)
            files[relative] = hashlib.sha256(target.read_bytes()).hexdigest()
    (ROOT / "usr/bin/valkey-server").symlink_to("valkey-check-rdb")
    version = subprocess.run([str(ROOT / "usr/bin/valkey-server"), "--version"], check=True, capture_output=True, text=True).stdout.strip()
    assert version.startswith("Valkey server v=9.0.4 ")
    record = dict(packages=PACKAGES, files=files, version=version,
                  runtimeBytes=sum(p.stat().st_size for p in ROOT.rglob("*") if p.is_file() and not p.is_symlink()))
    with ADMISSION.open("x") as output:
        json.dump(record, output, indent=2)
        output.flush(); os.fsync(output.fileno())
    for archive in archives:
        assert archive.resolve().parent == downloads.resolve()
        archive.unlink()
    print(json.dumps(record))
