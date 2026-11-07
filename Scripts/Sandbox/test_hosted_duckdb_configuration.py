"""Controlled DuckDB qualification/enrollment guards; no hosted calls."""
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import uuid
from unittest.mock import Mock, patch

spec = importlib.util.spec_from_file_location('duckdb_publisher', Path(__file__).with_name('Publish-HostedDuckDbConfiguration.py'))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class DuckDbConfigurationTests(unittest.TestCase):
    def setUp(self):
        directory = self.enterContext(tempfile.TemporaryDirectory())
        self.root = Path(directory)
        self.enterContext(patch.object(publisher, 'SOURCE', self.root))
        self.enterContext(patch.object(publisher.configuration, 'STATE', self.root))
        self.raw = b'controlled qualified source bytes; native SDK proof is a separate CI gate'
        (self.root / 'source.duckdb').write_bytes(self.raw)
        self.seed = {'Actor': str(uuid.UUID(int=1)), 'Ids': [str(uuid.UUID(int=i)) for i in (2, 3, 4)],
                     'DeletedId': str(uuid.UUID(int=5))}
        seed = json.dumps(self.seed).encode()
        (self.root / 'demo-seed.json').write_bytes(seed)
        self.proof = {'sourceSha256': hashlib.sha256(self.raw).hexdigest(),
            'seedSha256': hashlib.sha256(seed).hexdigest(), 'nativeCounts': [3, 2, 0], 'activations': 2,
            'sourceUnchanged': True, 'walAbsent': True, 'conclusion': 'success', 'passed': 1, 'skipped': 0,
            'sdkVersion': '1.1.3', 'platform': 'linux-x64'}
        self.write_proof()
        descriptor, _ = publisher.qualified_source()
        (self.root / 'duckdb-public-descriptor.json').write_text(json.dumps(descriptor))
        self.before = {'OASIS_DNA_JSON': '{"OASIS":{"Sandbox":{"Enabled":true}}}',
            'PREPARED_STORAGE_ADMISSION_JSON': json.dumps({'ordinaryProviders': ['MongoDBOASIS'],
                'nativeProviders': [], 'queryOnlyProviders': ['IPFSOASIS'],
                'queryTariffs': [{'provider': 'IPFSOASIS', 'costUsd': 0, 'catalogueVersion': 'demo-read-v1'}]}),
            'CQL_PUBLIC_READ_CATALOGUES_JSON': '{"IPFSOASIS":["retained"]}',
            'CQL_HOSTED_QUERY_EXAMPLES_JSON': '{"IPFSOASIS":["retained"]}', 'unrelated': 'retained'}

    def write_proof(self):
        (self.root / 'hosted-runtime-qualified.json').write_text(json.dumps(self.proof))

    def test_only_six_settings_change_with_exact_original_ids_and_read_only_path(self):
        after = publisher.candidate(self.before)
        self.assertEqual(set(publisher.KEYS), {key for key in after if after.get(key) != self.before.get(key)})
        self.assertTrue(all(after[key] == value for key, value in self.before.items()
                            if key not in publisher.KEYS))
        self.assertEqual(publisher.CONNECTION, after[publisher.KEYS[2]])
        self.assertEqual(self.seed['Ids'], json.loads(after['CQL_PUBLIC_READ_CATALOGUES_JSON'])['DuckDBOASIS'])
        presets = json.loads(after[publisher.KEYS[5]])
        self.assertEqual(4, len(presets))
        self.assertEqual(self.seed['Ids'], presets[0]['request']['filters'][0]['value'])
        self.assertEqual(self.seed['Ids'][:2], presets[2]['request']['filters'][0]['value'])
        self.assertNotIn(self.seed['DeletedId'], json.dumps(presets))

    def test_changed_native_bytes_or_seed_are_not_requalified(self):
        (self.root / 'source.duckdb').write_bytes(self.raw + b'changed')
        with self.assertRaisesRegex(RuntimeError, 'qualification'):
            publisher.candidate(self.before)
        (self.root / 'source.duckdb').write_bytes(self.raw)
        (self.root / 'demo-seed.json').write_text('{}')
        with self.assertRaisesRegex(RuntimeError, 'qualification'):
            publisher.candidate(self.before)

    def test_wal_is_preserved_and_refused(self):
        wal = self.root / 'source.duckdb.wal'; wal.write_bytes(b'pending')
        with self.assertRaisesRegex(RuntimeError, 'WAL'):
            publisher.candidate(self.before)
        self.assertEqual(b'pending', wal.read_bytes())

    def test_incomplete_linux_native_proof_cannot_enroll(self):
        for key, value in [('skipped', 1), ('nativeCounts', [3, 2]), ('platform', 'windows-x64'),
                           ('sourceUnchanged', False), ('walAbsent', False), ('sdkVersion', 'unqualified')]:
            original = self.proof[key]; self.proof[key] = value; self.write_proof()
            with self.assertRaisesRegex(RuntimeError, 'qualification'):
                publisher.candidate(self.before)
            self.proof[key] = original

    def test_existing_connection_or_enrollment_is_never_overwritten(self):
        for key in publisher.KEYS[:3]:
            with self.assertRaisesRegex(RuntimeError, 'reconciliation'):
                publisher.candidate(self.before | {key: 'existing'})
        before = dict(self.before)
        before['CQL_PUBLIC_READ_CATALOGUES_JSON'] = '{"DuckDBOASIS":[]}'
        with self.assertRaisesRegex(ValueError, 'reconciliation'):
            publisher.candidate(before)

    def test_changed_reviewed_ids_cannot_replace_original_catalogue(self):
        path = self.root / 'duckdb-public-descriptor.json'
        value = json.loads(path.read_bytes()); value['LiveIds'].reverse(); path.write_text(json.dumps(value))
        with self.assertRaisesRegex(RuntimeError, 'catalogue differs'):
            publisher.candidate(self.before)

    def live_settings(self):
        after = publisher.candidate(self.before)
        for key in publisher.KEYS:
            (self.root / f'{publisher.PREFIX}-{key}.acknowledged.json').write_text(json.dumps({
                'key': key, 'verified': True, 'deploymentTriggered': False,
                'sha256': hashlib.sha256(after[key].encode()).hexdigest()}))
        deployment = str(uuid.UUID(int=7))
        self.enterContext(patch.dict(publisher.os.environ, {
            'COSMIC_CQL_SETTINGS_DEPLOYMENT_ID': deployment,
            'COSMIC_CQL_REVIEWED_INSTANCE_IDS': str(uuid.UUID(int=8))}))
        self.enterContext(patch.object(publisher, 'verify_mount', return_value=deployment))
        self.enterContext(patch.object(publisher.configuration, 'protected_read', return_value=after))
        self.variables = self.enterContext(patch.object(publisher.configuration, 'variables', return_value=after))
        self.release = self.enterContext(patch.object(publisher.mount, 'verify_release', return_value=deployment))
        self.inspect = self.enterContext(patch.object(publisher.subprocess, 'run'))
        return after, deployment

    def test_live_settings_refuse_a_stale_ack_before_source_inspection(self):
        after, _ = self.live_settings()
        key = publisher.KEYS[0]
        path = self.root / f'{publisher.PREFIX}-{key}.acknowledged.json'
        ack = json.loads(path.read_bytes()); ack['sha256'] = '0' * 64; path.write_text(json.dumps(ack))
        with self.assertRaisesRegex(RuntimeError, 'acknowledgement differs'):
            publisher.verify_settings()
        self.inspect.assert_not_called()
        self.variables.assert_not_called()

    def test_unrelated_live_configuration_drift_blocks_source_inspection(self):
        after, _ = self.live_settings()
        self.variables.return_value = after | {'unrelated': 'changed elsewhere'}
        with self.assertRaisesRegex(RuntimeError, 'configuration differs'):
            publisher.verify_settings()
        self.inspect.assert_not_called()

    def test_live_receipt_requires_actual_source_hash_and_stable_deployment(self):
        after, deployment = self.live_settings()
        bundle = json.loads(after[publisher.KEYS[1]])
        receipt = {'schema': 1, 'digest': hashlib.sha256(after[publisher.KEYS[1]].encode()).hexdigest(),
                   'sha256': bundle['sha256'], 'bytes': bundle['bytes']}
        body = {'receipt': receipt, 'sha256': '0' * 64, 'bytes': bundle['bytes']}
        self.inspect.return_value = Mock(returncode=0, stdout=json.dumps(body).encode())
        with self.assertRaisesRegex(RuntimeError, 'source differs'):
            publisher.verify_settings()
        body['sha256'] = bundle['sha256']
        self.inspect.return_value.stdout = json.dumps(body).encode()
        self.assertEqual(deployment, publisher.verify_settings())
        self.release.return_value = str(uuid.UUID(int=9))
        with self.assertRaisesRegex(RuntimeError, 'deployment changed'):
            publisher.verify_settings()


if __name__ == '__main__':
    unittest.main()
