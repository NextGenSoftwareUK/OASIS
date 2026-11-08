import json
from pathlib import Path
from runpy import run_path
import tempfile
import unittest

module = run_path(str(Path(__file__).with_name('Retire-HostedDaprSource.py')))


class RetirementReceiptTests(unittest.TestCase):
    def setUp(self):
        self.operation = '12345678-1234-4234-8234-123456789abc'
        self.plan = {'prefix': '12345678-1234-4234-8234-123456789ab'}
        self.proof = {'schema': 1, 'originalOperation': self.operation, 'sourceOwner': self.plan['prefix'] + '0',
                      'originalStorageSha256': 'a' * 64, 'originalSaveResponseAcknowledged': False,
                      'originalSaveOutcome': 'unknown', 'priorGenerationAdmitted': False, 'businessWrites': 0,
                      'retiredPath': '/data/COSMIC/RetiredDaprSources/' + self.operation + '/source',
                      'files': 15, 'sourceFilesSha256': 'b' * 64, 'deployment': 'deployment', 'instance': 'instance'}

    def parse(self, proof):
        return module['validate_receipt'](proof, self.plan, 'deployment', 'instance', 'a' * 64, self.operation)

    def test_unknown_original_outcome_is_preserved(self):
        self.assertEqual(self.parse(self.proof), self.proof)

    def test_changed_or_fabricated_outcomes_are_refused(self):
        for key, value in [('originalSaveOutcome', 'applied'), ('businessWrites', 1), ('businessWrites', False),
                           ('priorGenerationAdmitted', True), ('originalSaveResponseAcknowledged', True),
                           ('sourceFilesSha256', 'x' * 64), ('files', 257), ('retiredPath', '/data/other'),
                           ('originalOperation', '12345678-1234-4234-8234-123456789abd')]:
            with self.subTest(key=key), self.assertRaises((RuntimeError, ValueError)):
                self.parse(self.proof | {key: value})

    def test_local_archive_preserves_state_in_the_checked_workspace(self):
        with tempfile.TemporaryDirectory(prefix='cosmic-dapr-local-archive-') as directory:
            root = Path(directory).resolve()
            state_root = root / 'LocalState/COSMIC'
            state = state_root / 'HostedDaprSourceCurrent'
            state.mkdir(parents=True)
            (state / 'source-plan.dpapi').write_bytes(b'original protected plan')
            (state / 'retirement.acknowledged.json').write_text(json.dumps({'native': self.proof}), encoding='utf-8')
            globals_ = module['archive_local'].__globals__
            old = {key: globals_[key] for key in ('ROOT', 'STATE_ROOT', 'STATE')}
            def write(file, value):
                with file.open('xb') as stream:
                    stream.write(value)
            try:
                globals_.update(ROOT=root, STATE_ROOT=state_root, STATE=state)
                module['archive_local'](write)
                archive = state_root / ('HostedDaprSourceRetired-' + self.operation)
                self.assertFalse(state.exists())
                self.assertEqual((archive / 'source-plan.dpapi').read_bytes(), b'original protected plan')
                self.assertTrue((archive / 'archive-local.acknowledged.json').is_file())
            finally:
                globals_.update(old)


if __name__ == '__main__':
    unittest.main()
