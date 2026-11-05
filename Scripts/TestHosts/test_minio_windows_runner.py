import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('minio_windows', Path(__file__).with_name('run-cosmic-minio-windows.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class AdmissionTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.original = runner.STATE
        runner.STATE = Path(self.directory.name)

    def tearDown(self):
        runner.STATE = self.original
        self.directory.cleanup()

    def test_initialization_refuses_each_original_outcome(self):
        for name in ['source.json', 'bootstrap.intent.json', 'bootstrap.ack.json']:
            with self.subTest(name=name):
                target = runner.STATE / name
                target.write_text('original')
                with self.assertRaisesRegex(RuntimeError, 'cannot replay'):
                    runner.validate_source_admission(True)
                self.assertEqual('original', target.read_text())
                target.unlink()

    def test_retained_requires_bucket_and_record_acknowledgements(self):
        with self.assertRaisesRegex(RuntimeError, 'Acknowledged'):
            runner.validate_source_admission(False)
        for name in ['source.json', 'bucket.ack.json', 'bootstrap.ack.json']:
            (runner.STATE / name).write_text('{}')
        runner.validate_source_admission(False)

    def test_pending_consumer_is_preserved(self):
        pending = runner.STATE / 'mongo.pending.json'
        pending.write_text('unknown')
        with self.assertRaisesRegex(RuntimeError, 'Unresolved'):
            runner.validate_source_admission(True)
        self.assertEqual('unknown', pending.read_text())

    def test_reconciliation_requires_and_preserves_the_original_pending_fixture(self):
        for name in ['source.json', 'bucket.ack.json', 'bootstrap.ack.json']:
            (runner.STATE / name).write_text('{}')
        with self.assertRaisesRegex(RuntimeError, 'pending consumer required'):
            runner.validate_source_admission(False, reconcile=True)
        pending = runner.STATE / 'mongo.pending.json'
        pending.write_text('original')
        runner.validate_source_admission(False, reconcile=True)
        self.assertEqual('original', pending.read_text())

    def test_only_one_current_phase_native_interrupt_can_certify_shutdown(self):
        log = runner.STATE / 'server.log'
        event = json.dumps({'message': 'Exiting on signal: INTERRUPT'})
        for text in ['', '{"message":"unrelated"}', event + '\n' + event]:
            log.write_text(text)
            with self.assertRaisesRegex(RuntimeError, 'shutdown'):
                runner.native_interrupted(log)
        log.write_text(event)
        runner.native_interrupted(log)


if __name__ == '__main__':
    unittest.main()
