import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('elastic_runner', Path(__file__).with_name('run-cosmic-elasticsearch-windows.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class ShutdownEvidenceTests(unittest.TestCase):
    def test_prior_shutdown_cannot_certify_current_run(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'native.log'
            prior = b'[cosmic-owned-local] stopped\n[cosmic-owned-local] closed\n'
            path.write_bytes(prior + b'current run started\n')
            self.assertFalse(runner.current_node_closed(path, len(prior)))
            with path.open('ab') as f:
                f.write(b'[cosmic-owned-local] stopped\n[cosmic-owned-local] closed\n')
            self.assertTrue(runner.current_node_closed(path, len(prior)))

    def test_truncated_log_cannot_certify_shutdown(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'native.log'
            path.write_bytes(b'new log')
            with self.assertRaisesRegex(RuntimeError, 'lineage changed'):
                runner.current_node_closed(path, 100)


if __name__ == '__main__':
    unittest.main()
