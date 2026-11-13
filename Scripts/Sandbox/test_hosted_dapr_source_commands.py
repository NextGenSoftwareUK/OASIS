import json
from pathlib import Path
from runpy import run_path
import unittest

module = run_path(str(Path(__file__).with_name('Provision-HostedDaprSource.py')))


class DaprCreationReceiptTests(unittest.TestCase):
    def setUp(self):
        self.plan = {'prefix': '12345678-1234-4234-8234-123456789ab'}
        self.value = {'schema': 1, 'sourceOwner': self.plan['prefix'] + '0',
                      'ids': [self.plan['prefix'] + suffix for suffix in ('e', '1', '2')],
                      'records': 4, 'recoveryVerified': True, 'planSha256': 'a' * 64,
                      'deployment': 'reviewed-deployment', 'instance': 'reviewed-instance'}

    def parse(self, value):
        return module['receipt'](module['MARKER'] + json.dumps(value).encode(), self.plan,
                                 'a' * 64, 'reviewed-deployment', 'reviewed-instance')

    def test_exact_source_receipt(self):
        self.assertEqual(self.parse(self.value), self.value)

    def test_foreign_or_incomplete_receipt_is_rejected(self):
        for field, value in [('instance', 'other'), ('planSha256', 'b' * 64), ('records', 3),
                             ('recoveryVerified', 1), ('schema', True), ('ids', [])]:
            with self.subTest(field=field), self.assertRaises(RuntimeError):
                self.parse(self.value | {field: value})

    def test_duplicate_receipt_is_ambiguous(self):
        line = module['MARKER'] + json.dumps(self.value).encode()
        with self.assertRaises(RuntimeError):
            module['receipt'](line + b'\n' + line, self.plan, 'a' * 64, 'reviewed-deployment', 'reviewed-instance')


if __name__ == '__main__':
    unittest.main()
