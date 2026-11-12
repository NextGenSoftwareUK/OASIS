"""Pinned runtime verification shared by the Linux installer and native fixture."""
import argparse
import ctypes
import hashlib
import json
from pathlib import Path, PurePosixPath
import sys
import subprocess
import shutil
import tarfile
import urllib.request

PINS = Path(__file__).with_name('dapr-linux-runtime-pins.json')
WINDOWS_HASHES = {'daprd': '1013629f8224e73d64aaa45a122e16c54638ccc07c3e9d5178f5e6d575a61897',
                  'garnet': '0ce799cff60409b3cfd64a85fa19669474eef157d9b369ecc31c41e456c4f2a5'}

def process_diagnostic(log_path, exit_code, secrets):
    if log_path.stat().st_size > 8 * 1024 * 1024:
        text = 'Native log exceeds diagnostic size budget; original log retained on runner'
    else:
        text = log_path.read_text(errors='replace')
        for secret in secrets:
            if not secret:
                raise ValueError('Explicit nonempty diagnostic secrets required')
            text = text.replace(secret, '[redacted]')
    errors = [line[:2048] for line in text.splitlines()
              if any(marker in line for marker in ('fail:', 'crit:', 'Exception', 'Error:', 'Unable'))][:8]
    return {'process': log_path.name, 'exitCode': exit_code, 'errors': errors, 'tail': text[-4096:]}

def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def archive_members(archive, executable):
    members = archive.getmembers()
    if len(members) > 2000 or sum(row.size for row in members) > 512 * 1024 * 1024:
        raise RuntimeError('Runtime archive exceeds extraction budget')
    for row in members:
        path = PurePosixPath(row.name)
        if (path.is_absolute() or '..' in path.parts or '\\' in row.name
                or not (row.isfile() or row.isdir())):
            raise RuntimeError('Runtime archive contains unsafe member')
    matches = [row for row in members if row.isfile() and PurePosixPath(row.name).name == executable]
    if len(matches) != 1:
        raise RuntimeError('Runtime archive must contain one expected executable')
    return members, matches[0]

def require_native_build(library, receipt, build):
    proof = json.loads(receipt.read_text())
    if (proof.get('source') != build or library.is_symlink() or not library.is_file()
            or library.stat().st_size != proof.get('bytes') or digest(library) != proof.get('sha256')):
        raise RuntimeError('Mandatory native storage build differs from its pinned source/receipt')


def build_native_storage(root, executable, build):
    if build['useUring'] is not True:
        raise RuntimeError('The complete upstream native IO contract must remain enabled')
    source = root / 'garnet-native-source'
    source.mkdir()
    def run(*args, cwd=None):
        result = subprocess.run(args, cwd=cwd, capture_output=True, timeout=180)
        if result.returncode:
            raise RuntimeError('Pinned native SDK build failed: ' + (result.stdout + result.stderr).decode(errors='replace')[-4096:])
        return result.stdout.decode().strip()
    run('git', 'init', str(source))
    run('git', '-C', str(source), 'remote', 'add', 'origin', build['repository'])
    run('git', '-C', str(source), 'config', 'remote.origin.promisor', 'true')
    run('git', '-C', str(source), 'config', 'remote.origin.partialclonefilter', 'blob:none')
    run('git', '-C', str(source), 'fetch', '--filter=blob:none', '--depth=1', 'origin', build['commit'])
    run('git', '-C', str(source), 'sparse-checkout', 'init', '--cone')
    run('git', '-C', str(source), 'sparse-checkout', 'set', build['sourceDirectory'])
    run('git', '-C', str(source), 'checkout', '--detach', build['commit'])
    if run('git', '-C', str(source), 'rev-parse', 'HEAD') != build['commit'] or run('git', '-C', str(source), 'status', '--porcelain'):
        raise RuntimeError('Native SDK source differs from exact clean pinned commit')
    output = root / 'garnet-native-build'
    run('cmake', '-S', str(source / build['sourceDirectory']), '-B', str(output),
        '-DCMAKE_BUILD_TYPE=Release', '-DUSE_URING=ON')
    run('cmake', '--build', str(output), '--target', build['target'], '--parallel', '2')
    library = executable.parent / build['runtimePath']
    library.parent.mkdir(parents=True, exist_ok=True)
    with (output / build['library']).open('rb') as original, library.open('xb') as destination:
        shutil.copyfileobj(original, destination)
    receipt = library.with_suffix('.build.json')
    with receipt.open('x') as destination:
        json.dump({'source': build, 'bytes': library.stat().st_size, 'sha256': digest(library)}, destination)
    require_native_build(library, receipt, build)


def require_runtime(name, executable, runtime_directory=None):
    executable = Path(executable).resolve(strict=True)
    if sys.platform == 'win32':
        if digest(executable) != WINDOWS_HASHES[name]:
            raise RuntimeError('Unexpected Windows runtime executable digest')
        return executable
    if sys.platform != 'linux' or runtime_directory is None:
        raise RuntimeError('Explicit pinned Linux runtime directory required')
    pin = json.loads(PINS.read_text())['runtimes'][name]
    root = Path(runtime_directory).resolve(strict=True)
    package = root / pin['archive']
    if package.is_symlink() or package.stat().st_size != pin['bytes'] or digest(package) != pin['sha256']:
        raise RuntimeError('Linux runtime archive differs from official pin')
    with tarfile.open(package) as archive:
        _, member = archive_members(archive, pin['executable'])
        expected_path = root / name / member.name
        if executable != expected_path or expected_path.is_symlink():
            raise RuntimeError('Linux executable differs from its archive location')
        with archive.extractfile(member) as stream:
            expected = hashlib.file_digest(stream, 'sha256').hexdigest()
    if digest(executable) != expected:
        raise RuntimeError('Linux executable bytes differ from pinned archive')
    if name == 'garnet':
        build = pin['nativeBuild']
        library = executable.parent / build['runtimePath']
        require_native_build(library, library.with_suffix('.build.json'), build)
        ctypes.CDLL(str(library))  # Mandatory dependency resolution, before any source mutation.
    return executable

def install_linux(destination):
    if sys.platform != 'linux':
        raise RuntimeError('Runtime downloads are restricted to the Linux qualification runner')
    root = Path(destination).resolve()
    root.mkdir(exist_ok=False)
    result = {}
    for name, pin in json.loads(PINS.read_text())['runtimes'].items():
        package = root / pin['archive']
        with urllib.request.urlopen(pin['url'], timeout=60) as response, package.open('xb') as output:
            count = 0
            while chunk := response.read(1024 * 1024):
                count += len(chunk)
                if count > pin['bytes']:
                    raise RuntimeError('Runtime download exceeds pinned size')
                output.write(chunk)
        if count != pin['bytes'] or digest(package) != pin['sha256']:
            raise RuntimeError('Runtime archive digest/size differs')
        directory = root / name
        directory.mkdir()
        with tarfile.open(package) as archive:
            members, member = archive_members(archive, pin['executable'])
            archive.extractall(directory, members=members, filter='data')
        executable = directory / member.name
        executable.chmod(executable.stat().st_mode | 0o100)
        if name == 'garnet':
            build_native_storage(root, executable, pin['nativeBuild'])
        result[name] = str(require_runtime(name, executable, root))
    return result

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--install-linux', required=True, type=Path)
    print(json.dumps(install_linux(parser.parse_args().install_linux)))
