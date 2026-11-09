"""Admit one pinned official Garnet framework-dependent distribution; no global service."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tarfile
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument("--finalize-extracted", action="store_true")
parser.add_argument("--admit-storage-layout", action="store_true")
options = parser.parse_args()

BASE = Path("/var/tmp/oasis-runtimes")
ROOT = BASE / "garnet-2.2.5"
ARCHIVE = BASE / ".downloads/garnet-2.2.5-linux-x64.tar.xz"
DIGEST = "f397cf0da94697ebd2fa17e7d3bc448fa5d62578f2c9693698a104a88d91c626"
ADMISSION = ROOT / "cosmic-runtime-admission.json"
def install_storage_layout(record=None):
    flat = ROOT / "net10.0/libnative_device.so"
    installed = ROOT / "net10.0/runtimes/linux-x64/native/libnative_device.so"
    if flat.exists():
        if record is not None and not options.admit_storage_layout:
            raise RuntimeError("Version-only runtime has the archive's flat storage layout; explicit --admit-storage-layout required")
        assert not installed.exists()
        assert hashlib.sha256(flat.read_bytes()).hexdigest() == "053ed531bcd3ac1e7c244ffd540cf6ffda9bc290c8934bddcff7e61052f2f67d"
        installed.parent.mkdir(parents=True, exist_ok=True)
        flat.rename(installed)
        assert not flat.exists()
        if record is not None:
            record["files"][str(installed.relative_to(ROOT))] = record["files"].pop("net10.0/libnative_device.so")
            record["storageLayout"] = "single original library at the runtime resolver's required RID path"
            with (ROOT / "storage-layout.previous-admission.json").open("x") as output:
                output.write(ADMISSION.read_text()); output.flush(); os.fsync(output.fileno())
            with (ROOT / "cosmic-runtime-admission.complete.json").open("x") as output:
                json.dump(record, output, indent=2); output.flush(); os.fsync(output.fileno())
            os.replace(ROOT / "cosmic-runtime-admission.complete.json", ADMISSION)
    assert installed.is_file()
    return record

if ADMISSION.exists():
    record = json.loads(ADMISSION.read_text())
    assert record["archiveSha256"] == DIGEST
    record = install_storage_layout(record)
    for relative, digest in record["files"].items():
        assert hashlib.sha256((ROOT / relative).read_bytes()).hexdigest() == digest
    print("Reused hash-admitted Garnet2.2.5 runtime")
else:
    if ROOT.exists() and not options.finalize_extracted:
        raise RuntimeError("Original unadmitted Garnet runtime exists; inspect before explicit finalization")
    if options.finalize_extracted and not ROOT.is_dir():
        raise RuntimeError("No original runtime to finalize")
    ARCHIVE.parent.mkdir(exist_ok=True)
    if not ARCHIVE.exists():
        with urllib.request.urlopen("https://github.com/microsoft/garnet/releases/download/v2.2.5/linux-x64-based.tar.xz", timeout=30) as response, ARCHIVE.open("xb") as output:
            output.write(response.read())
    assert ARCHIVE.stat().st_size == 5694440 and hashlib.sha256(ARCHIVE.read_bytes()).hexdigest() == DIGEST
    with tarfile.open(ARCHIVE) as source:
        assert all(member.name == "net10.0" or member.name.startswith("net10.0/") for member in source.getmembers())
        if not ROOT.exists():
            ROOT.mkdir()
            source.extractall(ROOT, filter="data")
        else:
            # Finalize the original extraction without overlaying or changing files.
            for member in source.getmembers():
                if member.isfile():
                    with source.extractfile(member) as original:
                        assert hashlib.sha256((ROOT / member.name).read_bytes()).digest() == hashlib.sha256(original.read()).digest(), member.name
    install_storage_layout()
    executable = ROOT / "net10.0/GarnetServer"
    assert executable.read_bytes()[:4] == b"\x7fELF", "The official Linux apphost must be ELF."
    version = subprocess.run([str(executable), "--version"], env=dict(os.environ, DOTNET_ROOT="/var/tmp/oasis-runtimes/dotnet-10.0.12"), check=True, capture_output=True, text=True).stdout.strip()
    assert "2.2.5" in version
    record = dict(archiveSha256=DIGEST, archiveBytes=ARCHIVE.stat().st_size, version=version, dotnetRoot="/var/tmp/oasis-runtimes/dotnet-10.0.12",
                  files={str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in ROOT.rglob("*") if p.is_file()},
                  runtimeBytes=sum(p.stat().st_size for p in ROOT.rglob("*") if p.is_file()))
    with ADMISSION.open("x") as output:
        json.dump(record, output, indent=2); output.flush(); os.fsync(output.fileno())
    assert ARCHIVE.resolve().parent == (BASE / ".downloads").resolve()
    ARCHIVE.unlink()
    print(json.dumps(record))
