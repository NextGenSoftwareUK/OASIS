"""Cache one digest-verified official CockroachDB runtime and supplied libraries."""
from pathlib import Path
import hashlib, json, os, subprocess, tarfile, urllib.request

ROOT = Path('/var/tmp/oasis-runtimes/cockroach-26.2.9')
URL = 'https://binaries.cockroachdb.com/cockroach-v26.2.9.linux-amd64.tgz'
DIGEST = 'e9c41b8ab520fa2cd7cb3d61a08622bd413df4fa1ef725e1e2d75cc9517722d3'
PREFIX = 'cockroach-v26.2.9.linux-amd64'
assert not ROOT.exists(), 'Runtime or unknown outcome exists; inspect/reuse, never overwrite'
ROOT.mkdir(mode=0o700)
archive = ROOT / 'runtime.tgz'
with urllib.request.urlopen(URL, timeout=60) as source, archive.open('xb') as target:
    while chunk := source.read(1024 * 1024): target.write(chunk)
assert archive.stat().st_size == 144515867
assert hashlib.sha256(archive.read_bytes()).hexdigest() == DIGEST
with tarfile.open(archive, 'r:gz') as bundle:
    for member in bundle.getmembers():
        path = Path(member.name)
        assert path.parts[0] == PREFIX and '..' not in path.parts and not path.is_absolute()
        target = ROOT.joinpath(*path.parts[1:])
        assert target.resolve().is_relative_to(ROOT)
        if member.isdir(): target.mkdir(mode=0o700, exist_ok=True)
        else:
            assert member.isfile(), 'No archive links or special files allowed'
            target.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
            with bundle.extractfile(member) as source, target.open('xb') as output:
                while chunk := source.read(1024 * 1024): output.write(chunk)
            os.chmod(target, 0o700 if target.name == 'cockroach' else 0o600)
binary = ROOT / 'cockroach'
version = subprocess.check_output([str(binary), 'version'], text=True)
assert 'v26.2.9' in version
files = {str(p.relative_to(ROOT)): {'sha256': hashlib.sha256(p.read_bytes()).hexdigest(), 'bytes': p.stat().st_size}
         for p in ROOT.rglob('*') if p.is_file() and p != archive}
manifest = {'url': URL, 'archiveSha256': DIGEST, 'version': version.strip(), 'files': files}
with (ROOT / 'runtime.json').open('x') as file: json.dump(manifest, file)
# Exact verified owned archive; preserve the one reusable extracted installation.
archive.unlink()
print(json.dumps(manifest))
