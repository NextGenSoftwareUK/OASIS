"""Admit/reuse pinned official PostgREST; no service or database is created here."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tarfile
import urllib.request

ROOT = Path('/var/tmp/oasis-runtimes/postgrest-16.5')
ARCHIVE = ROOT.parent / '.downloads/postgrest-16.5-linux-static-x86-64.tar.xz'
DIGEST = '5099e7ada38f9f6846cd87c14c5f158b7c165f1e9959665ad412e61f4dceba95'
RECEIPT = ROOT / 'cosmic-runtime-admission.json'
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
if RECEIPT.exists():
    record = json.loads(RECEIPT.read_text())
    assert record['archiveSha256'] == DIGEST
    assert sha(ROOT / 'postgrest') == record['binarySha256']
    print('Reused hash-admitted PostgREST16.5')
else:
    if ROOT.exists():
        raise RuntimeError('Unadmitted runtime directory exists; preserve and inspect')
    ARCHIVE.parent.mkdir(parents=True, exist_ok=True)
    if not ARCHIVE.exists():
        with urllib.request.urlopen('https://github.com/PostgREST/postgrest/releases/download/v16.5/postgrest-v16.5-linux-static-x86-64.tar.xz', timeout=60) as response, ARCHIVE.open('xb') as output:
            while block := response.read(1024 * 1024): output.write(block)
    assert ARCHIVE.stat().st_size == 4520424 and sha(ARCHIVE) == DIGEST
    with tarfile.open(ARCHIVE) as source:
        members = source.getmembers()
        assert len(members) == 1 and members[0].name == 'postgrest' and members[0].isfile()
        ROOT.mkdir()
        source.extractall(ROOT, filter='data')
    (ROOT / 'postgrest').chmod(0o755)
    with RECEIPT.open('x') as output:
        json.dump({'version': '16.5', 'archiveSha256': DIGEST, 'binarySha256': sha(ROOT / 'postgrest')}, output, indent=2)
        output.flush(); os.fsync(output.fileno())
    ARCHIVE.unlink()
    print('Admitted PostgREST16.5; verified archive removed')
subprocess.run([str(ROOT / 'postgrest'), '--version'], check=True)
print(json.dumps({'binarySha256': sha(ROOT / 'postgrest'), 'runtimeBytes': sum(p.stat().st_size for p in ROOT.iterdir() if p.is_file())}))
