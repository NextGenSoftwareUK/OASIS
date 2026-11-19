"""Reuse a hash-admitted official PocketBase runtime; never overlay an unknown directory."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import urllib.request
import zipfile

VERSION = '0.40.6'
ROOT = Path('/var/tmp/oasis-runtimes/pocketbase-' + VERSION)
ARCHIVE = ROOT.parent / '.downloads/pocketbase-0.40.6-linux-amd64.zip'
DIGEST = '0e1f609158ed945f809f8a02ae012aa0427129c66ebb63c1aee76f2c0309373f'
RECEIPT = ROOT / 'cosmic-runtime-admission.json'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

if RECEIPT.exists():
    record = json.loads(RECEIPT.read_text())
    assert record['version'] == VERSION and record['archiveSha256'] == DIGEST
    for name, digest in record['files'].items():
        assert sha(ROOT / name) == digest, name
    print('Reused hash-admitted PocketBase' + VERSION)
else:
    if ROOT.exists():
        raise RuntimeError('Unadmitted runtime directory exists; preserve and inspect before continuing')
    ARCHIVE.parent.mkdir(parents=True, exist_ok=True)
    if not ARCHIVE.exists():
        with urllib.request.urlopen('https://github.com/pocketbase/pocketbase/releases/download/v0.40.6/pocketbase_0.40.6_linux_amd64.zip', timeout=60) as response, ARCHIVE.open('xb') as output:
            while block := response.read(1024 * 1024):
                output.write(block)
    assert ARCHIVE.stat().st_size == 12824710 and sha(ARCHIVE) == DIGEST
    with zipfile.ZipFile(ARCHIVE) as source:
        assert set(source.namelist()) == {'pocketbase', 'LICENSE.md', 'CHANGELOG.md'}, source.namelist()
        ROOT.mkdir()
        source.extractall(ROOT)
    (ROOT / 'pocketbase').chmod(0o755)
    record = {'version': VERSION, 'archiveSha256': DIGEST,
              'files': {p.name: sha(p) for p in ROOT.iterdir() if p.is_file()}}
    with RECEIPT.open('x') as output:
        json.dump(record, output, indent=2)
        output.flush(); os.fsync(output.fileno())
    ARCHIVE.unlink()
    print('Admitted PocketBase' + VERSION + '; verified archive removed')
subprocess.run([str(ROOT / 'pocketbase'), '--version'], check=True)
print(json.dumps({'runtimeBytes': sum(p.stat().st_size for p in ROOT.rglob('*') if p.is_file()), 'binarySha256': sha(ROOT / 'pocketbase')}))
