import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('pouch_windows', Path(__file__).with_name('run-cosmic-pouchdb-windows.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class AdmissionTests(unittest.TestCase):
    def test_current_native_log_requires_exact_database_close(self):
        with tempfile.TemporaryDirectory() as directory:
            log = Path(directory) / 'server.log'
            for text in ['', '{"ready":true}', '{"stopped":true,"nativeDatabasesClosed":0}',
                         '{"stopped":true,"nativeDatabasesClosed":1}\n{"stopped":true,"nativeDatabasesClosed":1}']:
                log.write_text(text)
                with self.assertRaisesRegex(RuntimeError, 'closure'):
                    runner.validate_closed(log)
            log.write_text('{"ready":true}\n{"stopped":true,"nativeDatabasesClosed":1}')
            runner.validate_closed(log)

    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.original = runner.STATE
        runner.STATE = Path(self.directory.name)

    def tearDown(self):
        runner.STATE = self.original
        self.directory.cleanup()

    def test_existing_source_cannot_be_initialized_again(self):
        for name in ['source.json', 'bootstrap.intent.json', 'bootstrap.ack.json']:
            with self.subTest(name=name):
                target = runner.STATE / name
                target.write_text('original')
                with self.assertRaisesRegex(RuntimeError, 'cannot replay'):
                    runner.validate_admission(True)
                self.assertEqual('original', target.read_text())
                target.unlink()

    def test_retained_requires_complete_acknowledged_baseline(self):
        with self.assertRaisesRegex(RuntimeError, 'baseline required'):
            runner.validate_admission(False)
        for name in ['source.json', 'bootstrap.ack.json', 'native-baseline.json']:
            (runner.STATE / name).write_text('{}')
        runner.validate_admission(False)

    def test_unresolved_consumer_is_preserved(self):
        pending = runner.STATE / 'mongo.pending.json'
        pending.write_text('unknown')
        with self.assertRaisesRegex(RuntimeError, 'Unresolved'):
            runner.validate_admission(True)
        self.assertEqual('unknown', pending.read_text())


if __name__ == '__main__':
    unittest.main()
