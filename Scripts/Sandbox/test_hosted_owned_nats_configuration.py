"""Controlled owned NATS enrollment guards; no hosted calls or native source writes."""
import importlib.util
import json
from pathlib import Path
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('owned_nats', Path(__file__).with_name('Publish-HostedOwnedNatsConfiguration.py'))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class OwnedNatsTests(unittest.TestCase):
    def setUp(self):
        self.before = {'OASIS_DNA_JSON': '{"OASIS":{"Sandbox":{"Enabled":true}}}',
            'PREPARED_STORAGE_ADMISSION_JSON': json.dumps({'ordinaryProviders': ['MongoDBOASIS'],
                'nativeProviders': [], 'queryOnlyProviders': ['IPFSOASIS'],
                'queryTariffs': [{'provider': 'IPFSOASIS', 'costUsd': 0, 'catalogueVersion': 'demo-v1'}]}),
            'CQL_PUBLIC_READ_CATALOGUES_JSON': '{"IPFSOASIS":["retained"]}',
            'CQL_HOSTED_QUERY_EXAMPLES_JSON': '{"IPFSOASIS":["retained"]}', 'unrelated': 'retained'}

    def test_exact_eight_settings_preserve_existing_sources_and_catalogue_ids(self):
        descriptor, _ = publisher.qualified_source()
        ids = json.loads(descriptor)['ids']
        after = publisher.candidate(self.before, 'x' * 43)
        self.assertEqual(set(publisher.KEYS), {key for key in after if after.get(key) != self.before.get(key)})
        self.assertEqual('retained', after['unrelated'])
        self.assertEqual(self.before['OASIS_DNA_JSON'], after['OASIS_DNA_JSON'])
        self.assertEqual(ids, json.loads(after['CQL_PUBLIC_READ_CATALOGUES_JSON'])['NATSJetStreamOASIS'])
        presets = json.loads(after['CQL_HOSTED_QUERY_EXAMPLES__NATSJetStreamOASIS'])
        self.assertEqual(4, len(presets))
        self.assertEqual(ids, presets[0]['request']['filters'][0]['value'])
        self.assertEqual(ids[:2], presets[2]['request']['filters'][0]['value'])

    def test_existing_owned_settings_are_never_replaced(self):
        for key in publisher.EXTRA:
            with self.subTest(key=key), self.assertRaisesRegex(RuntimeError, 'reconciliation'):
                publisher.candidate(self.before | {key: 'existing'}, 'x' * 43)

    def test_invalid_password_rejected_before_qualification(self):
        with patch.object(publisher, 'qualified_source') as source:
            for password in ('short', 'x' * 42 + ' ', None):
                with self.assertRaises(ValueError): publisher.candidate(self.before, password)
            source.assert_not_called()

    def test_remote_mode_continues_to_reject_owned_loopback(self):
        descriptor, _ = publisher.qualified_source()
        with self.assertRaises(ValueError): publisher.native.candidate(self.before, json.loads(descriptor))

    def test_sandbox_disabled_cannot_enroll(self):
        with self.assertRaises(ValueError):
            publisher.candidate(self.before | {'OASIS_DNA_JSON': '{"OASIS":{"Sandbox":{"Enabled":false}}}'}, 'x' * 43)

    def test_qualification_failure_prevents_mutation(self):
        with patch.object(publisher, 'qualified_source', side_effect=RuntimeError('qualification missing')):
            with self.assertRaisesRegex(RuntimeError, 'qualification missing'):
                publisher.candidate(self.before, 'x' * 43)


if __name__ == '__main__': unittest.main()
