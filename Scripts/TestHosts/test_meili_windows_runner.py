import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('runner', Path(__file__).with_name('run-cosmic-meili-windows.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class Process:
    pid = 123
    returncode = None

    def __init__(self, timeout):
        self.timeout = timeout
        self.terminations = 0

    def poll(self):
        return self.returncode

    def terminate(self):
        self.terminations += 1

    def wait(self, timeout):
        if self.timeout:
            raise subprocess.TimeoutExpired(['owned-native-test'], timeout)
        self.returncode = 1


class StopEvidenceTests(unittest.TestCase):
    def test_stop_timeout_keeps_original_operation_error_and_unknown_receipt(self):
        with tempfile.TemporaryDirectory(prefix='cosmic-meili-stop-') as folder, patch.object(runner, 'STATE', Path(folder)):
            original = RuntimeError('original readiness failure')
            process = Process(timeout=True)
            with self.assertRaises(ExceptionGroup) as caught:
                runner.stop_owned_process(process, 'controlled', 'source-hash', original)
            self.assertIs(caught.exception.exceptions[0], original)
            self.assertIsInstance(caught.exception.exceptions[1], subprocess.TimeoutExpired)
            self.assertEqual(process.terminations, 1)
            self.assertFalse((Path(folder) / 'controlled-process-stopped.json').exists())
            self.assertEqual(json.loads((Path(folder) / 'controlled-process-stop-unknown.json').read_bytes())['pid'], 123)

    def test_stop_timeout_after_success_is_still_a_failure(self):
        with tempfile.TemporaryDirectory(prefix='cosmic-meili-stop-') as folder, patch.object(runner, 'STATE', Path(folder)):
            with self.assertRaises(subprocess.TimeoutExpired):
                runner.stop_owned_process(Process(timeout=True), 'controlled', 'source-hash')
            self.assertFalse((Path(folder) / 'controlled-process-stopped.json').exists())

    def test_completed_stop_records_owned_identity_and_does_not_overwrite(self):
        with tempfile.TemporaryDirectory(prefix='cosmic-meili-stop-') as folder, patch.object(runner, 'STATE', Path(folder)):
            process = Process(timeout=False)
            runner.stop_owned_process(process, 'controlled', 'source-hash')
            path = Path(folder) / 'controlled-process-stopped.json'
            self.assertEqual(json.loads(path.read_bytes())['pid'], 123)
            original = path.read_bytes()
            with self.assertRaises(FileExistsError):
                runner.stop_owned_process(process, 'controlled', 'source-hash')
            self.assertEqual(path.read_bytes(), original)


if __name__ == '__main__':
    unittest.main()
