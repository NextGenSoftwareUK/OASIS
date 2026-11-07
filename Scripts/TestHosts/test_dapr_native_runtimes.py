"""Offline integrity/path boundary controls; never downloads or launches runtimes."""
import io
import hashlib
import json
from pathlib import Path
import tarfile
import tempfile
import unittest
from unittest.mock import Mock, patch
import DaprNativeRuntimes as runtime

class RuntimeTests(unittest.TestCase):
    def fixture(self, root):
        archive = root / 'runtime.tar.gz'
        value = b'controlled runtime bytes'
        with tarfile.open(archive, 'w:gz') as package:
            member = tarfile.TarInfo('bin/daprd')
            member.size = len(value)
            package.addfile(member, io.BytesIO(value))
        executable = root / 'daprd/bin/daprd'
        executable.parent.mkdir(parents=True)
        executable.write_bytes(value)
        pin = root / 'pins.json'
        pin.write_text(json.dumps({'runtimes': {'daprd': {'archive': archive.name,
            'bytes': archive.stat().st_size, 'sha256': runtime.digest(archive), 'executable': 'daprd'}}}))
        return pin, archive, executable

    def test_linux_executable_must_match_trusted_archive_and_exact_location(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            pin, _, executable = self.fixture(root)
            with patch.object(runtime, 'PINS', pin), patch.object(runtime.sys, 'platform', 'linux'):
                self.assertEqual(executable, runtime.require_runtime('daprd', executable, root))
                copied = root / 'other'
                copied.write_bytes(executable.read_bytes())
                with self.assertRaisesRegex(RuntimeError, 'location'):
                    runtime.require_runtime('daprd', copied, root)
                executable.write_bytes(b'changed binary')
                with self.assertRaisesRegex(RuntimeError, 'bytes differ'):
                    runtime.require_runtime('daprd', executable, root)

    def test_archive_corruption_prevents_executable_acceptance(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            pin, archive, executable = self.fixture(root)
            archive.write_bytes(b'changed archive')
            with patch.object(runtime, 'PINS', pin), patch.object(runtime.sys, 'platform', 'linux'):
                with self.assertRaisesRegex(RuntimeError, 'archive differs'):
                    runtime.require_runtime('daprd', executable, root)

    def test_archive_links_and_parent_escape_are_rejected(self):
        for name, kind in [('../escape', tarfile.REGTYPE), ('/escape', tarfile.REGTYPE),
                           ('link', tarfile.SYMTYPE), ('hardlink', tarfile.LNKTYPE)]:
            with self.subTest(name=name):
                member = tarfile.TarInfo(name)
                member.type = kind
                archive = Mock()
                archive.getmembers.return_value = [member]
                with self.assertRaisesRegex(RuntimeError, 'unsafe'):
                    runtime.archive_members(archive, 'daprd')

    def test_duplicate_executables_and_expanded_size_are_rejected(self):
        archive = Mock()
        archive.getmembers.return_value = [tarfile.TarInfo('a/daprd'), tarfile.TarInfo('b/daprd')]
        with self.assertRaisesRegex(RuntimeError, 'one expected'):
            runtime.archive_members(archive, 'daprd')
        oversized = tarfile.TarInfo('daprd')
        oversized.size = 513 * 1024 * 1024
        archive.getmembers.return_value = [oversized]
        with self.assertRaisesRegex(RuntimeError, 'budget'):
            runtime.archive_members(archive, 'daprd')

    def test_native_diagnostics_redact_before_tail_truncation(self):
        with tempfile.TemporaryDirectory() as directory:
            log = Path(directory) / 'native.log'
            secret = 'sensitive-token-' + 'a' * 64
            log.write_text('prefix' * 1000 + secret + 'x' * 4070)
            result = runtime.process_diagnostic(log, 1, (secret,))
            self.assertNotIn(secret, result['tail'])
            self.assertNotIn('aaaa', result['tail'])
            self.assertLessEqual(len(result['tail']), 4096)
            self.assertEqual(1, result['exitCode'])
            self.assertIn(secret, log.read_text())

    def test_long_stack_trace_keeps_leading_native_error(self):
        with tempfile.TemporaryDirectory() as directory:
            log = Path(directory) / 'native.log'
            log.write_text('fail: NativeDevice IOException SECRET ' + ' frame' * 1500)
            result = runtime.process_diagnostic(log, 1, ('SECRET',))
            self.assertIn('NativeDevice IOException [redacted]', result['errors'][0])
            self.assertNotIn('SECRET', json.dumps(result))

    def test_native_library_requires_exact_upstream_object_and_binding(self):
        with tempfile.TemporaryDirectory() as directory:
            library = Path(directory) / 'libnative_device.so'
            data = b'controlled native library bytes'
            library.write_bytes(data)
            pin = {'bytes': len(data), 'sourceCommit': '1' * 40, 'sourcePath': 'native/library',
                   'gitBlobSha1': hashlib.sha1(b'blob ' + str(len(data)).encode() + b'\0' + data).hexdigest()}
            pin['url'] = 'https://raw.githubusercontent.com/microsoft/garnet/' + pin['sourceCommit'] + '/' + pin['sourcePath']
            runtime.require_native_file(library, pin)
            with self.assertRaisesRegex(RuntimeError, 'Git object'):
                runtime.require_native_file(library, pin | {'url': 'https://example.invalid/library'})
            library.write_bytes(b'x' * len(data))
            with self.assertRaisesRegex(RuntimeError, 'Git object'):
                runtime.require_native_file(library, pin)
            library.unlink()
            with self.assertRaisesRegex(RuntimeError, 'size/path'):
                runtime.require_native_file(library, pin)

    def test_laptop_installer_refuses_download_before_network_access(self):
        with patch.object(runtime.sys, 'platform', 'win32'), patch.object(runtime.urllib.request, 'urlopen') as network:
            with self.assertRaisesRegex(RuntimeError, 'restricted'):
                runtime.install_linux(Path('must-not-create'))
            network.assert_not_called()

if __name__ == '__main__':
    unittest.main()
