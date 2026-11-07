import base64
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('waku_publisher', Path(__file__).with_name('Publish-HostedWakuConfiguration.py'))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class WakuConfigurationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.hashes = ['0x' + value * 64 for value in '123']
        self.owner = 'a5808c3e-7031-47e7-92de-48a659568f1b'
        self.original = b'SQLite format 3\0' + bytes(64)
        self.receipt = {'archive': 'store-qualified.sqlite3', 'sha256': hashlib.sha256(self.original).hexdigest(),
                        'bytes': len(self.original), 'messageHashes': self.hashes}
        (self.root / 'store-qualified.sqlite3').write_bytes(self.original)
        (self.root / 'archive-qualified.json').write_text(json.dumps(self.receipt))
        (self.root / 'hosted-runtime-qualified.json').write_text(json.dumps({
            'archiveSha256': self.receipt['sha256'], 'originalArchiveBytes': len(self.original),
            'nativeRows': 3, 'nativeStarts': 2, 'originalSourceUnchanged': True, 'acknowledgementUnchanged': True}))
        (self.root / 'waku-public-descriptor.json').write_text(json.dumps({'Actor': self.owner, 'MessageHashes': self.hashes}))
        self.enterContext(patch.object(publisher, 'SOURCE', self.root))
        self.enterContext(patch.object(publisher.configuration, 'STATE', self.root))
        self.before = {
            'OASIS_DNA_JSON': '{"OASIS":{"Sandbox":{"Enabled":true}}}',
            'PREPARED_STORAGE_ADMISSION_JSON': json.dumps({'ordinaryProviders': ['MongoDBOASIS'],
                'nativeProviders': [], 'queryOnlyProviders': ['IPFSOASIS'],
                'queryTariffs': [{'provider': 'IPFSOASIS', 'costUsd': 0, 'catalogueVersion': 'demo-read-v1'}]}),
            'CQL_PUBLIC_READ_CATALOGUES_JSON': '{"IPFSOASIS":["retained"]}',
            'CQL_HOSTED_QUERY_EXAMPLES_JSON': '{"IPFSOASIS":["retained"]}', 'unrelated': 'retained'}

    def test_candidate_reuses_shared_admission_and_four_exact_presets(self):
        after = publisher.candidate(self.before)
        self.assertEqual(set(publisher.KEYS), {key for key in after if after.get(key) != self.before.get(key)})
        self.assertEqual('retained', after['unrelated'])
        self.assertEqual(self.before['CQL_HOSTED_QUERY_EXAMPLES_JSON'], after['CQL_HOSTED_QUERY_EXAMPLES_JSON'])
        binding = json.loads(after['CQL_WAKU_STORE_QUERY_SOURCE_JSON'])
        self.assertEqual({'sourceOwner': self.owner, 'messageHashes': self.hashes}, binding)
        presets = json.loads(after['CQL_HOSTED_QUERY_EXAMPLES__WakuOASIS'])
        self.assertEqual(4, len(presets))
        self.assertEqual(3, len(presets[0]['request']['filters'][0]['value']))
        self.assertEqual(2, len(presets[2]['request']['filters'][0]['value']))
        self.assertEqual(self.original, base64.b64decode(json.loads(after[publisher.KEYS[1]])['archiveBase64']))

    def test_candidate_refuses_changed_original_archive(self):
        (self.root / 'store-qualified.sqlite3').write_bytes(self.original + b'changed')
        with self.assertRaisesRegex(ValueError, 'differs'):
            publisher.candidate(self.before)

    def test_candidate_requires_original_native_restart_qualification(self):
        path = self.root / 'hosted-runtime-qualified.json'
        value = json.loads(path.read_bytes()); value['nativeStarts'] = 1
        path.write_text(json.dumps(value))
        with self.assertRaisesRegex(RuntimeError, 'restore and restart'):
            publisher.candidate(self.before)

    def test_existing_runtime_setting_prevents_reenrollment(self):
        with self.assertRaisesRegex(RuntimeError, 'reconciliation'):
            publisher.candidate(self.before | {'OASIS_SANDBOX_WAKU': 'true'})

    def test_descriptor_cannot_substitute_a_different_message(self):
        path = self.root / 'waku-public-descriptor.json'
        path.write_text(json.dumps({'Actor': self.owner, 'MessageHashes': self.hashes[:2] + ['0x' + '4' * 64]}))
        with self.assertRaisesRegex(RuntimeError, 'original archive'):
            publisher.candidate(self.before)

    def test_apply_rechecks_the_original_deployment_before_mutation(self):
        after = publisher.candidate(self.before)
        with (patch.object(publisher.configuration, 'protected_read', side_effect=[self.before, after]),
              patch.object(publisher, 'verify_mount', return_value='original'),
              patch.object(publisher.mount, 'verify_release', return_value='changed'),
              patch.object(publisher.configuration, 'apply_values') as apply):
            publisher.apply()
            callback = apply.call_args.kwargs['before_mutation']
            with self.assertRaisesRegex(RuntimeError, 'changed before'):
                callback()

    def test_settings_verification_requires_exact_deployment_before_ssh(self):
        with (patch.dict(publisher.os.environ, {'COSMIC_CQL_SETTINGS_DEPLOYMENT_ID': 'expected'}),
              patch.object(publisher, 'verify_mount', side_effect=RuntimeError('deployment changed')),
              patch.object(publisher.subprocess, 'run') as ssh):
            with self.assertRaisesRegex(RuntimeError, 'deployment changed'):
                publisher.verify_settings()
            ssh.assert_not_called()


if __name__ == '__main__':
    unittest.main()
