"""Assemble pinned upstream contacts sources in cloud; never claim native install."""
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import platform
import shutil
import signal
import subprocess


def sha(data):
    return hashlib.sha256(data).hexdigest()


def blob_sha(data):
    return hashlib.sha1(b'blob ' + str(len(data)).encode() + b'\0' + data).hexdigest()


def git(directory, *args, input_bytes=None):
    environment = dict(os.environ, GIT_LFS_SKIP_SMUDGE='1', GIT_TERMINAL_PROMPT='0',
                       GIT_CONFIG_NOSYSTEM='1', GIT_CONFIG_GLOBAL='/dev/null')
    command = ['git', '-c', 'core.hooksPath=/dev/null', '-c', 'credential.helper=',
               '-C', str(directory), *args]
    with subprocess.Popen(command, env=environment, start_new_session=True,
                          stdin=subprocess.PIPE if input_bytes is not None else subprocess.DEVNULL,
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE) as process:
        try:
            stdout, stderr = process.communicate(input=input_bytes, timeout=180)
        except subprocess.TimeoutExpired:
            os.killpg(process.pid, signal.SIGKILL)
            process.communicate()
            raise
        result = subprocess.CompletedProcess(command, process.returncode, stdout, stderr)
        result.check_returncode()
        return stdout


def checkout(scratch, name, repository, commit, paths):
    assert len(commit) == 40 and all(c in '0123456789abcdef' for c in commit)
    assert repository.count('/') == 1 and all(c.isalnum() or c in '/-_' for c in repository)
    directory = scratch / name
    directory.mkdir()
    git(directory, 'init')
    git(directory, 'remote', 'add', 'origin', 'https://github.com/' + repository + '.git')
    git(directory, 'fetch', '--filter=blob:none', '--depth=1', 'origin', commit)
    assert git(directory, 'rev-parse', 'FETCH_HEAD').decode().strip() == commit
    git(directory, 'sparse-checkout', 'init', '--no-cone')
    assert all(not p.startswith('/') and '..' not in PurePosixPath(p).parts for p in paths)
    patterns = ''.join('/' + p + '\n' for p in paths).encode()
    git(directory, 'sparse-checkout', 'set', '--no-cone', '--stdin', input_bytes=patterns)
    git(directory, 'checkout', '--detach', commit)
    return directory


def main():
    assert os.environ.get('GITHUB_ACTIONS') == 'true' and platform.system() == 'Linux'
    root = Path(__file__).resolve().parents[2]
    inputs = root / 'Scripts/TestHosts/urbit-contacts-inputs.json'
    spec = json.loads(inputs.read_text())
    out = root / 'TestResults/UrbitDeskCurrent'
    out.mkdir(parents=True, exist_ok=False)
    scratch = out / 'owned-source-checkouts'
    scratch.mkdir()
    cache = root / 'LocalState/COSMIC/CloudUrbitContactsDesk'
    assert not cache.exists(), 'Never overwrite an earlier assembled source.'
    cache.mkdir(parents=True)
    desk = cache / 'desk'
    desk.mkdir()
    report = dict(source=os.environ['GITHUB_SHA'], inputsSha256=sha(inputs.read_bytes()),
                  assemblyVerified=False, installed=False, nativeContactsQualified=False,
                  cqlQualified=False, liveDeployment=False)
    (out / 'intent.json').write_text(json.dumps(report, indent=2))
    try:
        manifest = {}
        for module in spec['dependencies']:
            directory = checkout(scratch, module['name'], module['repository'], module['commit'], module['picks'])
            for entry in module['files']:
                source = directory / entry['path']
                assert source.is_file() and not source.is_symlink()
                data = source.read_bytes()
                assert len(data) == entry['bytes'] and blob_sha(data) == entry['gitBlobSha']
                target = PurePosixPath(entry['target'])
                assert not target.is_absolute() and '..' not in target.parts and str(target) not in manifest
                destination = desk / str(target)
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_bytes(data)
                manifest[str(target)] = dict(bytes=len(data), sha256=sha(data), origin=module['name'])
        app = spec['application']
        directory = checkout(scratch, 'application', app['repository'], app['commit'], ['desk/', 'peru.yaml'])
        assert sha((directory / 'peru.yaml').read_bytes()) == app['declarationSha256']
        tree = git(directory, 'ls-tree', '-rz', '--full-tree', app['commit'], '--', 'desk/')
        entries = [item.split(b'\t', 1) for item in tree.split(b'\0') if item]
        assert len(entries) == app['deskFileCount']
        total = 0
        for header, raw_path in entries:
            mode, kind, oid = header.decode().split()
            path = raw_path.decode()
            source = directory / path
            relative = PurePosixPath(path).relative_to('desk')
            assert '..' not in relative.parts
            if mode == '120000':
                approved = spec['assembly']['documentationSymlink']
                assert path == approved['path'] and source.is_symlink()
                link = os.readlink(source)
                assert link == 'AGENTS.md' and oid == approved['blobSha']
                assert blob_sha(link.encode()) == oid and len(link.encode()) == approved['bytes']
                assert source.resolve().parent == (directory / 'desk').resolve()
                assert source.resolve().is_file() and not (source.parent / link).is_symlink()
                total += len(link.encode())
                # Match upstream rsync -L: materialize this one reviewed in-desk link.
                data = source.resolve().read_bytes()
            else:
                assert mode == '100644' and kind == 'blob' and not source.is_symlink()
                data = source.read_bytes()
                assert blob_sha(data) == oid
                total += len(data)
            destination = desk / str(relative)
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(data)
            manifest[str(relative)] = dict(bytes=len(data), sha256=sha(data), origin='application')
        assert total == app['deskBytes']
        assert (desk / 'sys.kelvin').read_text().strip() == '[%zuse 408]'
        assert '%contacts' in (desk / 'desk.bill').read_text()
        assert (desk / 'app/contacts.hoon').is_file()
        provenance = (app['commit'][:7] + '\n').encode()
        (desk / 'commit.txt').write_bytes(provenance)
        manifest['commit.txt'] = dict(bytes=len(provenance), sha256=sha(provenance), origin='provenance')
        actual = sorted(str(p.relative_to(desk)) for p in desk.rglob('*') if p.is_file())
        assert actual == sorted(manifest) and not any(p.is_symlink() for p in desk.rglob('*'))
        payload = json.dumps(manifest, sort_keys=True, indent=2)
        (cache / 'files.json').write_text(payload)
        (out / 'files.json').write_text(payload)
        report.update(assemblyVerified=True, fileCount=len(manifest),
                      assembledBytes=sum(e['bytes'] for e in manifest.values()),
                      filesManifestSha256=sha(payload.encode()))
        with open(os.environ['GITHUB_OUTPUT'], 'a') as stream:
            stream.write('assembled=true\n')
    finally:
        assert scratch.resolve().parent == out.resolve() and not scratch.is_symlink()
        shutil.rmtree(scratch)
        report['temporarySourceCheckoutsRemoved'] = not scratch.exists()
        (out / 'qualification.json').write_text(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
