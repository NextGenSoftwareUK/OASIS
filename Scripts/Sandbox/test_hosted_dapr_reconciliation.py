import json
from pathlib import Path
from runpy import run_path
import unittest

module = run_path(str(Path(__file__).with_name('Reconcile-HostedDaprSource.py')))


class DaprRecoveryReceiptTests(unittest.TestCase):
    def setUp(self):
        self.plan = {'prefix': '12345678-1234-4234-8234-123456789ab'}
        self.proof = {'schema': 1, 'mode': 'inspect', 'planSha256': 'a' * 64,
                      'deployment': 'reviewed-deployment', 'instance': 'reviewed-instance',
                      'sourceOwner': self.plan['prefix'] + '0', 'records': 4, 'recoveries': 2,
                      'businessWrites': 0, 'originalSaveResponseAcknowledged': False,
                      'originalOperation': '12345678-1234-4234-8234-123456789abc',
                      'inspectionSha256': 'b' * 64, 'originalStorageSha256': 'c' * 64}

    def parse(self, value):
        return module['receipt'](module['MARKER'] + json.dumps(value).encode(), 'inspect', self.plan,
                                 'a' * 64, 'reviewed-deployment', 'reviewed-instance')

    def test_complete_original_read_only_receipt(self):
        self.assertEqual(self.parse(self.proof), self.proof)

    def test_incomplete_or_foreign_recovery_is_rejected(self):
        for key, value in [('records', 3), ('recoveries', 1), ('businessWrites', 1), ('businessWrites', False),
                           ('originalSaveResponseAcknowledged', True), ('instance', 'other'),
                           ('inspectionSha256', ''), ('mode', 'acknowledge'), ('schema', True)]:
            with self.subTest(key=key), self.assertRaises(RuntimeError):
                self.parse(self.proof | {key: value})

    def test_duplicate_receipt_is_refused(self):
        line = module['MARKER'] + json.dumps(self.proof).encode()
        with self.assertRaises(RuntimeError):
            module['receipt'](line + b'\n' + line, 'inspect', self.plan, 'a' * 64, 'reviewed-deployment', 'reviewed-instance')


if __name__ == '__main__':
    unittest.main()
