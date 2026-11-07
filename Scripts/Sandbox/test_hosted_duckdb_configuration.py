"""Controlled DuckDB qualification/enrollment guards; no hosted calls."""
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import uuid
from unittest.mock import patch

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


if __name__ == '__main__':
    unittest.main()
