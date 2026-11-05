import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('quest_browser_runner', Path(__file__).with_name('run-cosmic-questdb-browser.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class SourceAdmissionTests(unittest.TestCase):
    def test_changed_actor_or_network_config_is_rejected(self):
        for changed in ['source.json', 'conf/server.conf']:
            with self.subTest(changed=changed), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                (root / 'conf').mkdir()
                (root / 'source.json').write_bytes(b'{"actor":"original"}')
                (root / 'conf/server.conf').write_bytes(b'http.bind.to=127.0.0.1:29000\n')
                admission = {'sourceSha256': hashlib.sha256((root / 'source.json').read_bytes()).hexdigest(),
                             'configSha256': hashlib.sha256((root / 'conf/server.conf').read_bytes()).hexdigest()}
                (root / 'source.admission.json').write_text(json.dumps(admission),encoding='utf-8')
                runner.verify_source_admission(root)
                (root / changed).write_bytes(b'changed')
                with self.assertRaisesRegex(RuntimeError, 'identity or configuration changed'):
                    runner.verify_source_admission(root)

    def test_missing_admission_cannot_be_recreated_implicitly(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with self.assertRaises(FileNotFoundError):
                runner.verify_source_admission(root)
            self.assertEqual([], list(root.iterdir()))


if __name__ == '__main__':
    unittest.main()
