import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('dapr_publisher', Path(__file__).with_name('Publish-HostedDaprConfiguration.py'))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class DaprEnrollmentTests(unittest.TestCase):
    def setUp(self):
        self.plan = {'prefix': '12345678-1234-4234-8234-123456789ab', 'sidecarToken': 'a' * 64}
        self.before = {'OASIS_DNA_JSON': '{"OASIS":{"Sandbox":{"Enabled":true}}}',
                       'PREPARED_STORAGE_ADMISSION_JSON': json.dumps({'ordinaryProviders': ['MongoDBOASIS'],
                           'nativeProviders': [], 'queryOnlyProviders': ['IPFSOASIS'],
                           'queryTariffs': [{'provider': 'IPFSOASIS', 'costUsd': 0}]}),
                       'CQL_PUBLIC_READ_CATALOGUES_JSON': '{"IPFSOASIS":["retained"]}',
                       'UNRELATED_SETTING': 'retained'}
        descriptor = {'Actor': self.plan['prefix'] + '0', 'LiveIds': [self.plan['prefix'] + c for c in 'e12'],
                      'DeletedId': self.plan['prefix'] + '3', 'MissingId': self.plan['prefix'] + '4',
                      'Store': 'cosmic-teaching', 'GrpcEndpoint': 'http://127.0.0.1:54862/'}
        with tempfile.TemporaryDirectory(prefix='cosmic-dapr-enrollment-') as directory:
            file = Path(directory) / 'catalogue.json'
            file.write_text(json.dumps(descriptor), encoding='utf-8')
            self.review = publisher.preparation.verifier.reviewed_exact_key_source('DaprOASIS', file)

    def test_exact_eight_setting_scope_preserves_existing_sources(self):
        original = dict(self.before)
        after = publisher.candidate(self.before, self.plan, self.review)
        self.assertEqual(self.before, original)
        self.assertEqual({k for k in after if after.get(k) != original.get(k)}, set(publisher.KEYS))
        self.assertEqual(after['UNRELATED_SETTING'], 'retained')
        self.assertEqual(json.loads(after['CQL_PUBLIC_READ_CATALOGUES_JSON'])['IPFSOASIS'], ['retained'])

    def test_existing_reader_or_runtime_cannot_be_replaced(self):
        for key in publisher.RUNTIME:
            with self.subTest(key=key), self.assertRaises(RuntimeError):
                publisher.candidate(self.before | {key: 'existing'}, self.plan, self.review)

    def test_foreign_catalogue_cannot_be_enrolled(self):
        with self.assertRaises(ValueError):
            publisher.candidate(self.before, self.plan | {'prefix': '22345678-1234-4234-8234-123456789ab'}, self.review)


if __name__ == '__main__':
    unittest.main()
