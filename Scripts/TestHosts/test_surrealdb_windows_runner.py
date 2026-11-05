"""Admission controls use private temporary metadata, never a database process."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('surreal_windows', Path(__file__).with_name('run-cosmic-surrealdb-windows.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class AdmissionTests(unittest.TestCase):
    def test_existing_source_cannot_be_reinitialized(self):
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory)
            source = state / 'windows-source'
            source.mkdir()
            sentinel = source / 'preserve.txt'
            sentinel.write_text('original')
            with self.assertRaisesRegex(RuntimeError, 'cannot replay'):
                runner.admit_source(state, True)
            self.assertEqual('original', sentinel.read_text())

    def test_retained_source_requires_identity_and_baseline(self):
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory)
            source = runner.admit_source(state, True)
            with self.assertRaisesRegex(RuntimeError, 'identity/baseline'):
                runner.admit_source(state, False)
            for name in ['identity.json', 'native-baseline.json']:
                (source / name).write_text('{}')
            self.assertEqual(source, runner.admit_source(state, False))

    def test_pending_write_is_preserved_and_refused(self):
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory)
            source = runner.admit_source(state, True)
            pending = source / 'seed.pending.json'
            pending.write_text('{"unknown":true}')
            with self.assertRaisesRegex(RuntimeError, 'Unresolved'):
                runner.admit_source(state, False)
            self.assertEqual('{"unknown":true}', pending.read_text())

    def test_foreign_runtime_or_store_cannot_be_adopted(self):
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory)
            source = runner.admit_source(state, True)
            manifest = source / 'environment.json'
            original = json.loads(manifest.read_text())
            for field, value in [('runtimeSha256', 'foreign'), ('store', 'foreign'), ('port', 18002)]:
                manifest.write_text(json.dumps(dict(original, **{field: value})))
                with self.assertRaisesRegex(RuntimeError, 'differs'):
                    runner.admit_source(state, False)


if __name__ == '__main__':
    unittest.main()
