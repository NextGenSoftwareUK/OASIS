"""Build one pinned official KeyDB release with existing tools, then keep only reusable executables."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tarfile
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument("--resume-build", action="store_true")
options = parser.parse_args()

BASE = Path("/var/tmp/oasis-runtimes")
ROOT = BASE / "keydb-6.3.4"
BUILD = Path("/var/tmp/oasis-keydb-build-6.3.4")
COMMIT = "7e7e5e57d25fe246a8201f0acf5e7363c0bf1e14"
ARCHIVE = BASE / (".downloads/keydb-" + COMMIT + ".tar.gz")
DIGEST = "08e6dc019bbf7b5b46f2b6879ad87f34db801302c3f37a51f53436abee840ac6"
RESULTS = Path("/mnt/c/Source/OASIS-cosmic/TestResults/KeyDbCqlCurrent")
ADMISSION = ROOT / "cosmic-runtime-admission.json"
if ADMISSION.exists():
    record = json.loads(ADMISSION.read_text())
    assert record["sourceCommit"] == COMMIT and record["archiveSha256"] == DIGEST
    for relative, digest in record["files"].items():
        assert hashlib.sha256((ROOT / relative).read_bytes()).hexdigest() == digest
    print("Reused pinned independently built KeyDB6.3.4 runtime")
else:
    if ROOT.exists() or BUILD.exists() and not options.resume_build:
        raise RuntimeError("Original unfinished KeyDB build/runtime exists; inspect before explicit resume")
    if options.resume_build and not BUILD.is_dir():
        raise RuntimeError("No original build to resume")
    if not ARCHIVE.exists():
        ARCHIVE.parent.mkdir(exist_ok=True)
        with urllib.request.urlopen("https://codeload.github.com/Snapchat/KeyDB/tar.gz/" + COMMIT, timeout=30) as response, ARCHIVE.open("xb") as output:
            output.write(response.read())
    assert hashlib.sha256(ARCHIVE.read_bytes()).hexdigest() == DIGEST
    RESULTS.mkdir(parents=True, exist_ok=True)
    if not options.resume_build:
        BUILD.mkdir()
        # Only this exact official archive is extracted to the new owned directory.
        with tarfile.open(ARCHIVE) as source:
            assert all(member.name.startswith("KeyDB-" + COMMIT + "/") or member.name == "KeyDB-" + COMMIT for member in source.getmembers())
            source.extractall(BUILD, filter="data")
        with (BUILD / "build-intent.json").open("x") as output:
            json.dump(dict(sourceCommit=COMMIT, archiveSha256=DIGEST, archiveBytes=ARCHIVE.stat().st_size,
                           command=["make", "-j2", "MALLOC=libc", "BUILD_TLS=yes", "ENABLE_FLASH=no", "keydb-server", "keydb-cli"]), output)
            output.flush(); os.fsync(output.fileno())
    else:
        intent = json.loads((BUILD / "build-intent.json").read_text())
        assert intent["sourceCommit"] == COMMIT and intent["archiveSha256"] == DIGEST
        # Resume a stopped dependency failure in place, never overlay or patch vendor source.
        with tarfile.open(ARCHIVE) as source:
            for member in source.getmembers():
                if member.isfile():
                    with source.extractfile(member) as original:
                        assert hashlib.sha256((BUILD / member.name).read_bytes()).digest() == hashlib.sha256(original.read()).digest(), member.name
    project = BUILD / ("KeyDB-" + COMMIT)
    with (RESULTS / "native-runtime-build.log").open("a") as log:
        subprocess.run(["make", "-C", str(project / "src"), "-j2", "MALLOC=libc", "BUILD_TLS=yes", "ENABLE_FLASH=no", "keydb-server", "keydb-cli"],
                       stdout=log, stderr=subprocess.STDOUT, check=True)
    version = subprocess.run([str(project / "src/keydb-server"), "--version"], check=True, capture_output=True, text=True).stdout.strip()
    assert version.startswith("KeyDB server v=6.3.4 ")
    files = {}
    for relative in ["src/keydb-server", "src/keydb-cli", "COPYING"]:
        target = ROOT / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(project / relative, target)
        files[relative] = hashlib.sha256(target.read_bytes()).hexdigest()
    record = dict(sourceCommit=COMMIT, archiveSha256=DIGEST, archiveBytes=ARCHIVE.stat().st_size,
                  files=files, version=version, compiler=subprocess.run(["g++", "--version"], check=True, capture_output=True, text=True).stdout.splitlines()[0],
                  runtimeBytes=sum(p.stat().st_size for p in ROOT.rglob("*") if p.is_file()),
                  disposableBuildBytes=sum(p.stat().st_size for p in BUILD.rglob("*") if p.is_file()))
    with ADMISSION.open("x") as output:
        json.dump(record, output, indent=2); output.flush(); os.fsync(output.fileno())
    # A successful build has ended. Remove only this owned, exact disposable source.
    assert BUILD.resolve() == Path("/var/tmp/oasis-keydb-build-6.3.4")
    assert (BUILD / "build-intent.json").is_file() and project.is_dir()
    shutil.rmtree(BUILD)
    assert ARCHIVE.resolve().parent == (BASE / ".downloads").resolve()
    ARCHIVE.unlink()
    print(json.dumps(record))
