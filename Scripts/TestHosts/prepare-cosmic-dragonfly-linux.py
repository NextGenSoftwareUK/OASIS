"""Admit the pinned official Dragonfly executable without a global service installation."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tarfile
import urllib.request

BASE = Path("/var/tmp/oasis-runtimes")
ROOT = BASE / "dragonfly-2.0.2"
ARCHIVE = BASE / ".downloads/dragonfly-2.0.2-x86_64.tar.gz"
DIGEST = "eb9dfe654c69ab190cb3aa329ed3599ebe19c5e9a3a437bb7bc38ef52f665c88"
ADMISSION = ROOT / "cosmic-runtime-admission.json"
if ADMISSION.exists():
    record = json.loads(ADMISSION.read_text())
    assert record["archiveSha256"] == DIGEST
    for relative, digest in record["files"].items():
        assert hashlib.sha256((ROOT / relative).read_bytes()).hexdigest() == digest
    print("Reused independently admitted Dragonfly2.0.2 runtime")
else:
    if ROOT.exists():
        raise RuntimeError("Unadmitted original Dragonfly runtime exists; inspect without replacing it")
    ARCHIVE.parent.mkdir(exist_ok=True)
    if not ARCHIVE.exists():
        with urllib.request.urlopen("https://github.com/dragonflydb/dragonfly/releases/download/v2.0.2/dragonfly-x86_64.tar.gz", timeout=30) as response, ARCHIVE.open("xb") as output:
            output.write(response.read())
    assert ARCHIVE.stat().st_size == 17766071 and hashlib.sha256(ARCHIVE.read_bytes()).hexdigest() == DIGEST
    with tarfile.open(ARCHIVE) as source:
        members = source.getmembers()
        assert all(item.isfile() and "/" not in item.name and item.name in ["dragonfly-x86_64", "LICENSE.md"] for item in members)
        ROOT.mkdir()
        source.extractall(ROOT, filter="data")
    executable = ROOT / "dragonfly-x86_64"
    version = subprocess.run([str(executable), "--version"], check=True, capture_output=True, text=True).stdout.strip()
    assert "v2.0.2" in version
    record = dict(archiveSha256=DIGEST, archiveBytes=ARCHIVE.stat().st_size, version=version,
                  files={p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in ROOT.iterdir() if p.is_file()},
                  runtimeBytes=sum(p.stat().st_size for p in ROOT.iterdir() if p.is_file()))
    with ADMISSION.open("x") as output:
        json.dump(record, output, indent=2); output.flush(); os.fsync(output.fileno())
    assert ARCHIVE.resolve().parent == (BASE / ".downloads").resolve()
    ARCHIVE.unlink()
    print(json.dumps(record))
