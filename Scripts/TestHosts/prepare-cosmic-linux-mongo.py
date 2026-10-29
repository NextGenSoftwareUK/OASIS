"""Acquire one pinned native Mongo daemon, retaining no installation archive."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tarfile
import urllib.request

base = Path("/var/tmp/oasis-runtimes")
destination = base / "mongodb-8.3.13"
expected = "a55780ed9bb5410ae5ea8e8c5b882cb1cd9389da689c417d6265929ad28a1f7f"
admission = destination / "cosmic-runtime-admission.json"
if admission.exists():
    record = json.loads(admission.read_text())
    assert record["archiveSha256"] == expected
    assert hashlib.sha256((destination / "mongod").read_bytes()).hexdigest() == record["executableSha256"]
    print("Reused verified Linux Mongo runtime")
else:
    if destination.exists():
        raise RuntimeError("Unadmitted runtime directory exists; preserve without overwrite")
    downloads = base / ".downloads"
    downloads.mkdir(exist_ok=True)
    archive = downloads / "mongodb-linux-x86_64-ubuntu2404-8.3.13.tgz"
    if not archive.exists():
        with urllib.request.urlopen("https://fastdl.mongodb.org/linux/" + archive.name, timeout=60) as response, archive.open("xb") as output:
            shutil.copyfileobj(response, output)
    with archive.open("rb") as stream:
        assert hashlib.file_digest(stream, "sha256").hexdigest() == expected, "Official archive digest differs"
    destination.mkdir()
    with tarfile.open(archive) as package:
        daemon = [item for item in package.getmembers() if item.isfile() and item.name.endswith("/bin/mongod")]
        assert len(daemon) == 1
        with package.extractfile(daemon[0]) as source, (destination / "mongod").open("xb") as output:
            shutil.copyfileobj(source, output)
        os.chmod(destination / "mongod", 0o755)
        for item in package.getmembers():
            name = Path(item.name).name
            if item.isfile() and (name.startswith("LICENSE") or name.startswith("NOTICE")):
                with package.extractfile(item) as source, (destination / name).open("xb") as output:
                    shutil.copyfileobj(source, output)
    version = subprocess.run([str(destination / "mongod"), "--version"], check=True, text=True, capture_output=True).stdout
    assert "v8.3.13" in version
    record = dict(archiveSha256=expected, archiveBytes=archive.stat().st_size,
                  executableSha256=hashlib.sha256((destination / "mongod").read_bytes()).hexdigest(),
                  runtimeBytes=sum(p.stat().st_size for p in destination.rglob("*") if p.is_file()), version="8.3.13")
    with admission.open("x") as output:
        json.dump(record, output, indent=2)
        output.flush()
        os.fsync(output.fileno())
    assert archive.resolve().parent == downloads.resolve()
    archive.unlink()
    print(json.dumps(record))
