"""Offline composition controls using retained qualified source descriptors."""
import importlib.util
import json
from pathlib import Path
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('cross_chain_batch', Path(__file__).with_name('Prepare-CrossChainPublicBatch.py'))
batch = importlib.util.module_from_spec(spec)
spec.loader.exec_module(batch)


class BatchTests(unittest.TestCase):
    def setUp(self):
        self.before = {'OASIS_DNA_JSON': '{"OASIS":{"Sandbox":{"Enabled":true}}}',
            'PREPARED_STORAGE_ADMISSION_JSON': json.dumps({'ordinaryProviders': ['MongoDBOASIS'],
                'nativeProviders': [], 'queryOnlyProviders': ['IPFSOASIS'],
                'queryTariffs': [{'provider': 'IPFSOASIS', 'costUsd': 0, 'catalogueVersion': 'demo-v1'}]}),
            'CQL_HOSTED_QUERY_EXAMPLES_JSON': '{}', 'CQL_PUBLIC_READ_CATALOGUES_JSON': '{}',
            'OASIS_SANDBOX_NATS': 'true', 'unrelated': 'preserved'}

    def test_five_sources_share_one_admission_without_losing_existing_values(self):
        after = batch.candidate(self.before)
        self.assertEqual(set(batch.KEYS), {key for key in after if after.get(key) != self.before.get(key)})
        self.assertEqual(11, len(batch.KEYS))
        for key, value in self.before.items():
            if key not in batch.KEYS: self.assertEqual(value, after[key])
        admission = json.loads(after['PREPARED_STORAGE_ADMISSION_JSON'])
        self.assertEqual(6, len(admission['queryOnlyProviders']))
        self.assertEqual(6, len(admission['queryTariffs']))
        self.assertTrue(all(row['costUsd'] == 0 for row in admission['queryTariffs']))
        for key in batch.KEYS:
            if key.startswith('CQL_HOSTED_QUERY_EXAMPLES__'):
                self.assertEqual(4, len(json.loads(after[key])))

    def test_existing_enrollment_cannot_be_replaced(self):
        with self.assertRaises(ValueError): batch.candidate(self.before | {batch.PUBLISHERS[2].KEYS[0]: '{}'})

    def test_missing_original_qualification_prevents_batch(self):
        with patch.object(batch.PUBLISHERS[3], 'qualified_source', side_effect=RuntimeError('missing original receipt')):
            with self.assertRaisesRegex(RuntimeError, 'original receipt'): batch.candidate(self.before)

    def test_disabled_sandbox_prevents_batch(self):
        with self.assertRaises(ValueError): batch.candidate(self.before | {'OASIS_DNA_JSON': '{"OASIS":{"Sandbox":{"Enabled":false}}}'})


if __name__ == '__main__': unittest.main()
