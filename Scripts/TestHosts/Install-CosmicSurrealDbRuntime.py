"""Install the single pinned official native runtime; never recreate an existing one."""
from pathlib import Path
import hashlib
import json
import os
import subprocess
import tarfile
import urllib.request

ROOT = Path('/var/tmp/oasis-runtimes/surrealdb-3.3.5')
URL = 'https://github.com/surrealdb/surrealdb/releases/download/v3.3.5/surreal-v3.3.5.linux-amd64.tgz'
DIGEST = '8f3bc163405085343fc6db9746a8e0cd55503c939802ad2b35a466675a9c09cf'

assert not ROOT.exists(), 'Runtime already exists or outcome unknown: inspect/reuse; never overwrite'
ROOT.mkdir(mode=0o700)
archive = ROOT / 'runtime.tgz'
with urllib.request.urlopen(URL, timeout=60) as source, archive.open('xb') as target:
    while chunk := source.read(1024 * 1024):
        target.write(chunk)
assert archive.stat().st_size == 105509109
assert hashlib.sha256(archive.read_bytes()).hexdigest() == DIGEST
with tarfile.open(archive, 'r:gz') as bundle:
    members = bundle.getmembers()
    assert len(members) == 1 and members[0].name == 'surreal' and members[0].isfile(), 'Unexpected archive layout'
    with bundle.extractfile(members[0]) as source, (ROOT / 'surreal').open('xb') as target:
        while chunk := source.read(1024 * 1024):
            target.write(chunk)
binary = ROOT / 'surreal'
os.chmod(binary, 0o700)
version = subprocess.check_output([str(binary), 'version'], text=True).strip()
assert '3.3.5' in version
manifest = {'version': version, 'url': URL, 'archiveSha256': DIGEST,
            'binarySha256': hashlib.sha256(binary.read_bytes()).hexdigest(), 'binaryBytes': binary.stat().st_size}
with (ROOT / 'runtime.json').open('x') as file:
    json.dump(manifest, file)
# Exact verified installer-owned archive; executable is the reusable retained resource.
archive.unlink()
print(json.dumps(manifest))
