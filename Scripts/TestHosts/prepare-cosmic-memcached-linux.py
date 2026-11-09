"""Extract the pinned Ubuntu daemon package without installing/starting a service."""
import argparse
import hashlib
import io
import os
import tarfile
import json
from pathlib import Path
import subprocess
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument("--finalize-extracted", action="store_true")
options = parser.parse_args()
base = Path("/var/tmp/oasis-runtimes")
root = base / "memcached-1.6.40"
archive = base / ".downloads/memcached_1.6.40-1ubuntu0.3_amd64.deb"
expected = "1e5272002efa8674959ee9796356b89bb1075ccb59f12a429d1a72396b61f15a"
admission = root / "cosmic-runtime-admission.json"
if admission.exists():
    record = json.loads(admission.read_text())
    assert record["archiveSha256"] == expected
    assert hashlib.sha256((root / "usr/bin/memcached").read_bytes()).hexdigest() == record["executableSha256"]
    print("Reused verified Memcached runtime")
else:
    if root.exists() and not options.finalize_extracted:
        raise RuntimeError("Unadmitted extracted runtime exists; inspect before explicit finalization")
    if options.finalize_extracted and not root.exists():
        raise RuntimeError("No original extracted runtime to finalize")
    archive.parent.mkdir(exist_ok=True)
    if not archive.exists():
        with urllib.request.urlopen("https://archive.ubuntu.com/ubuntu/pool/main/m/memcached/" + archive.name, timeout=30) as response, archive.open("xb") as output:
            output.write(response.read())
    assert hashlib.sha256(archive.read_bytes()).hexdigest() == expected
    if not root.exists():
        subprocess.run(["dpkg-deb", "-x", str(archive), str(root)], check=True)
    # Finalizing an interrupted dependency stage never overlays files. Verify the
    # actual executable against the exact official archive before admitting it.
    payload = subprocess.run(["dpkg-deb", "--fsys-tarfile", str(archive)], check=True, capture_output=True).stdout
    with tarfile.open(fileobj=io.BytesIO(payload)) as package:
        daemon = [item for item in package.getmembers() if item.isfile() and item.name.lstrip("./") == "usr/bin/memcached"]
        assert len(daemon) == 1
        with package.extractfile(daemon[0]) as source:
            expected_executable = hashlib.sha256(source.read()).hexdigest()
    executable = root / "usr/bin/memcached"
    assert hashlib.sha256(executable.read_bytes()).hexdigest() == expected_executable
    version = subprocess.run([str(executable), "-V"], check=True, capture_output=True, text=True).stdout.strip()
    assert version == "memcached 1.6.40"
    record = dict(archiveSha256=expected, archiveBytes=archive.stat().st_size,
                  executableSha256=expected_executable, version=version,
                  runtimeBytes=sum(p.stat().st_size for p in root.rglob("*") if p.is_file()))
    with admission.open("x") as output:
        json.dump(record, output, indent=2)
        output.flush()
        os.fsync(output.fileno())
    assert archive.resolve().parent == (base / ".downloads").resolve()
    archive.unlink()
    print(json.dumps(record))
