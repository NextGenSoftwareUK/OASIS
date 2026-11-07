"""Controlled owned NATS enrollment guards; no hosted calls or native source writes."""
import importlib.util
import hashlib
import json
import os
from pathlib import Path
import tempfile
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

    def mounted_settings(self):
        root = Path(self.enterContext(tempfile.TemporaryDirectory()))
        self.enterContext(patch.object(publisher.configuration, 'STATE', root))
        after = publisher.candidate(self.before, 'x' * 43)
        self.enterContext(patch.object(publisher, 'candidate', return_value=after))
        self.enterContext(patch.object(publisher.configuration, 'protected_read',
            side_effect=lambda name: self.before if '-before-' in name else after))
        self.variables = self.enterContext(patch.object(publisher.configuration, 'variables', return_value=after))
        deployment = '11111111-1111-4111-8111-111111111111'
        self.enterContext(patch.dict(os.environ, {'COSMIC_CQL_SETTINGS_DEPLOYMENT_ID': deployment,
            'COSMIC_CQL_REVIEWED_INSTANCE_IDS': '22222222-2222-4222-8222-222222222222'}))
        self.enterContext(patch.object(publisher, 'verify_mount', return_value=deployment))
        self.release = self.enterContext(patch.object(publisher.mount, 'verify_release', return_value=deployment))
        for key in publisher.KEYS:
            (root / f'{publisher.PREFIX}-{key}.acknowledged.json').write_text(json.dumps({
                'key': key, 'verified': True, 'deploymentTriggered': False,
                'sha256': hashlib.sha256(after[key].encode()).hexdigest()}))
        observed = {'schema': 1, 'verified': True, 'tlsCertificateSha256': 'a' * 64,
            'digest': hashlib.sha256(after[publisher.EXTRA[1]].encode()).hexdigest(),
            'sourceSha256': hashlib.sha256(after[publisher.native.KEYS[0]].encode()).hexdigest()}
        self.inspect = self.enterContext(patch.object(publisher.subprocess, 'run'))
        self.inspect.return_value.returncode = 0
        self.inspect.return_value.stdout = json.dumps(observed).encode()
        return root, after, deployment, observed

    def test_stale_setting_ack_stops_before_mounted_inspection(self):
        root, _, _, _ = self.mounted_settings()
        (root / f'{publisher.PREFIX}-{publisher.KEYS[0]}.acknowledged.json').write_text('{}')
        with self.assertRaisesRegex(RuntimeError, 'acknowledgement'): publisher.verify_settings()
        self.inspect.assert_not_called()

    def test_live_configuration_drift_stops_before_inspection(self):
        self.mounted_settings()
        self.variables.return_value = self.before
        with self.assertRaisesRegex(RuntimeError, 'Live NATS configuration'): publisher.verify_settings()
        self.inspect.assert_not_called()

    def test_mounted_source_receipt_rejects_changed_tls_identity(self):
        root, _, deployment, observed = self.mounted_settings()
        self.assertEqual(deployment, publisher.verify_settings())
        receipt = root / f'{publisher.PREFIX}-{deployment}-mounted-source.acknowledged.json'
        self.assertEqual(observed, json.loads(receipt.read_bytes()))
        observed['tlsCertificateSha256'] = 'b' * 64
        self.inspect.return_value.stdout = json.dumps(observed).encode()
        with self.assertRaisesRegex(RuntimeError, 'identity changed'): publisher.verify_settings()

    def test_changed_deployment_cannot_record_mounted_acceptance(self):
        root, _, _, _ = self.mounted_settings()
        self.release.return_value = 'changed'
        with self.assertRaisesRegex(RuntimeError, 'changed during inspection'): publisher.verify_settings()
        self.assertFalse(list(root.glob('*-mounted-source.acknowledged.json')))


if __name__ == '__main__': unittest.main()
