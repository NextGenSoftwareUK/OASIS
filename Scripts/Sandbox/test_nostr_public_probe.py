"""Offline envelope/selection controls; no native connection or crypto claim."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('nostr_probe', Path(__file__).with_name('Probe-NostrPublicReadiness.py'))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class NostrProbeTests(unittest.TestCase):
    def row(self, value='a'):
        return {'id': value * 64, 'pubkey': 'b' * 64, 'created_at': 1, 'kind': 1,
                'tags': [['p', 'c' * 64]], 'content': 'Public Unicode café', 'sig': 'd' * 128}

    def raw(self, rows, subscription='test'):
        return json.dumps([json.dumps(['EVENT', subscription, row], ensure_ascii=False) for row in rows]).encode()

    def test_exact_complete_set_accepts_reordered_events(self):
        rows = [self.row('a'), self.row('b')]
        result = probe.events(self.raw(rows[::-1]), 'test', [row['id'] for row in rows])
        self.assertEqual(result, rows[::-1])

    def test_duplicate_missing_unexpected_or_wrong_subscription_reject(self):
        expected = ['a' * 64, 'b' * 64]
        for rows in [[self.row()], [self.row(), self.row()], [self.row(), self.row('c')]]:
            with self.subTest(rows=len(rows)), self.assertRaises(ValueError):
                probe.events(self.raw(rows), 'test', expected)
        with self.assertRaises(ValueError):
            probe.events(self.raw([self.row()], 'foreign'), 'test')

    def test_invalid_schema_and_unbounded_values_reject(self):
        for key, value in [('kind', True), ('kind', 0), ('created_at', True), ('created_at', -1),
                           ('created_at', 2**53), ('id', 'A' * 64), ('sig', 'd' * 127),
                           ('content', 'x' * 32769), ('tags', [[None]]), ('tags', [[]])]:
            row = copy.deepcopy(self.row()); row[key] = value
            with self.subTest(key=key, value=str(value)[:20]), self.assertRaises(ValueError):
                probe.events(self.raw([row]), 'test')

    def test_duplicate_json_property_and_non_event_reject(self):
        for raw in [b'["[\"EOSE\",\"test\"]"]', b'{}', b'[]']:
            with self.assertRaises(ValueError):
                probe.events(raw, 'test')
        with self.assertRaises(ValueError):
            probe.unique_object([('id', 1), ('id', 2)])


if __name__ == '__main__':
    unittest.main()
