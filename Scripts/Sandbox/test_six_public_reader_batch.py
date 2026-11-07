"""Offline six-reader composition and shared release boundary controls."""
import importlib.util
import json
from pathlib import Path
import unittest
from unittest.mock import Mock, patch
import HostedPublicReaderBatch as shared

spec = importlib.util.spec_from_file_location('six_batch', Path(__file__).with_name('Prepare-SixPublicReaderBatch.py'))
batch = importlib.util.module_from_spec(spec)
spec.loader.exec_module(batch)


class SixReaderTests(unittest.TestCase):
    def setUp(self):
        self.before = {'OASIS_DNA_JSON': '{"OASIS":{"Sandbox":{"Enabled":true}}}',
            'PREPARED_STORAGE_ADMISSION_JSON': json.dumps({'ordinaryProviders': ['MongoDBOASIS'],
                'nativeProviders': [], 'queryOnlyProviders': ['IPFSOASIS'],
                'queryTariffs': [{'provider': 'IPFSOASIS', 'costUsd': 0, 'catalogueVersion': 'demo-v1'}]}),
            'CQL_HOSTED_QUERY_EXAMPLES_JSON': '{}', 'OASIS_SANDBOX_NATS': 'true', 'unrelated': 'preserved'}

    def test_six_sources_compose_thirteen_values_and_twenty_four_presets(self):
        after = batch.candidate(self.before)
        self.assertEqual(13, len(batch.KEYS))
        self.assertEqual(set(batch.KEYS), {key for key in after if after.get(key) != self.before.get(key)})
        for key, value in self.before.items():
            if key not in batch.KEYS: self.assertEqual(value, after[key])
        admission = json.loads(after['PREPARED_STORAGE_ADMISSION_JSON'])
        self.assertEqual(7, len(admission['queryOnlyProviders']))
        self.assertEqual(7, len(admission['queryTariffs']))
        self.assertTrue(all(row['costUsd'] == 0 for row in admission['queryTariffs']))
        self.assertEqual(24, sum(len(json.loads(after[key])) for key in batch.KEYS
                                if key.startswith('CQL_HOSTED_QUERY_EXAMPLES__')))

    def test_original_receipt_failure_stops_composition(self):
        with patch.object(batch.PUBLISHERS[2], 'qualified_source', side_effect=RuntimeError('original receipt missing')):
            with self.assertRaisesRegex(RuntimeError, 'original receipt'): batch.candidate(self.before)

    def test_existing_source_is_not_replaced(self):
        with self.assertRaises(ValueError): batch.candidate(self.before | {batch.PUBLISHERS[0].KEYS[0]: '{}'})

    def test_disabled_sandbox_is_not_enrolled(self):
        with self.assertRaises(ValueError): batch.candidate(self.before | {'OASIS_DNA_JSON': '{"OASIS":{"Sandbox":{"Enabled":false}}}'})

    def test_failed_release_guard_prevents_publisher_call(self):
        after = batch.candidate(self.before)
        guard = Mock()
        guard.require_matching_release.side_effect = RuntimeError('release differs')
        with patch.object(batch.configuration, 'protected_read', side_effect=[self.before, after]), \
             patch.object(shared, 'load', return_value=guard), \
             patch.object(batch.configuration, 'apply_values') as publish:
            with self.assertRaisesRegex(RuntimeError, 'release differs'): batch.apply()
        publish.assert_not_called()

    def test_release_change_is_rechecked_before_actual_write(self):
        after = batch.candidate(self.before)
        guard = Mock()
        guard.require_matching_release.side_effect = ['reviewed', 'changed']
        writes = []
        def publisher(prefix, keys, **options):
            options['before_mutation']()
            writes.append(True)
        with patch.object(batch.configuration, 'protected_read', side_effect=[self.before, after]), \
             patch.object(shared, 'load', return_value=guard), \
             patch.object(batch.configuration, 'apply_values', side_effect=publisher):
            with self.assertRaisesRegex(RuntimeError, 'changed before settings mutation'): batch.apply()
        self.assertEqual([], writes)


if __name__ == '__main__': unittest.main()
