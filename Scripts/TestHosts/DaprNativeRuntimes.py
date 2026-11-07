"""Pinned runtime verification shared by the Linux installer and native fixture."""
import argparse
import ctypes
import hashlib
import json
from pathlib import Path, PurePosixPath
import sys
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

def require_native_file(path, pin):
    if path.is_symlink() or not path.is_file() or path.stat().st_size != pin['bytes']:
        raise RuntimeError('Mandatory native storage library size/path differs')
    data = path.read_bytes()
    expected_url = 'https://raw.githubusercontent.com/microsoft/garnet/' + pin['sourceCommit'] + '/' + pin['sourcePath']
    if (path.is_symlink() or len(data) != pin['bytes'] or pin['url'] != expected_url
            or hashlib.sha1(b'blob ' + str(len(data)).encode() + b'\0' + data).hexdigest() != pin['gitBlobSha1']):
        raise RuntimeError('Mandatory native storage library differs from pinned upstream Git object')


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
        for native in pin['nativeFiles']:
            library = executable.parent / native['path']
            require_native_file(library, native)
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
            for native in pin['nativeFiles']:
                relative = PurePosixPath(native['path'])
                if relative.is_absolute() or '..' in relative.parts:
                    raise RuntimeError('Native library path escapes runtime')
                library = executable.parent / native['path']
                library.parent.mkdir(parents=True, exist_ok=True)
                with urllib.request.urlopen(native['url'], timeout=60) as response, library.open('xb') as output:
                    data = response.read(native['bytes'] + 1)
                    if len(data) != native['bytes']:
                        raise RuntimeError('Native library download size differs')
                    output.write(data)
                require_native_file(library, native)
        result[name] = str(require_runtime(name, executable, root))
    return result

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--install-linux', required=True, type=Path)
    print(json.dumps(install_linux(parser.parse_args().install_linux)))
