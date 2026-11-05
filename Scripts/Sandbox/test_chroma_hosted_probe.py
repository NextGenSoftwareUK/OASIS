import importlib.util
import json
from pathlib import Path
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('chroma_probe', Path(__file__).with_name('Probe-ChromaHostedReadiness.py'))
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)


class ChromaHostedProbeTests(unittest.TestCase):
    def source(self):
        return {'origin': 'https://chroma.example.com', 'tenant': 'owned/tenant', 'database': 'teaching',
                'collectionId': '11111111-1111-4111-8111-111111111111',
                'ids': [f'22222222-2222-4222-8222-22222222222{i}' for i in range(3)]}

    def rows(self, ids):
        return {'ids': ids, 'documents': [json.dumps({'Id': key, 'IsPublic': True,
                'IsActive': True, 'IsDeleted': False}) for key in ids]}

    def test_exact_activation_and_three_two_reads_only(self):
        source = self.source()
        calls = []
        def send(method, url, body, token):
            calls.append((method, url, body))
            self.assertEqual(token, 'controlled-test-credential')
            self.assertIn('/owned%2Ftenant/', url)
            if method == 'GET':
                return {'id': source['collectionId'], 'name': 'oasis_holons'}
            self.assertEqual(body['include'], ['documents'])
            self.assertEqual(body['limit'], len(body['ids']) + 1)
            self.assertTrue(url.endswith('/' + source['collectionId'] + '/get'))
            return self.rows(list(reversed(body['ids'])))
        proof = p.qualify(source, 'controlled-test-credential', send)
        self.assertEqual(proof['nativeCounts'], [3, 2])
        self.assertEqual([item[0] for item in calls], ['GET', 'POST', 'POST'])
        self.assertFalse(proof['credentialMutationDenialVerified'])

    def test_changed_collection_stops_before_record_reads(self):
        calls = []
        def send(*args):
            calls.append(args)
            return {'id': self.source()['ids'][0], 'name': 'oasis_holons'}
        with self.assertRaises(ValueError):
            p.qualify(self.source(), 'controlled', send)
        self.assertEqual(len(calls), 1)

    def test_missing_duplicate_foreign_and_private_records_rejected(self):
        ids = self.source()['ids']
        for fault in ('missing', 'duplicate', 'foreign', 'private', 'deleted', 'mismatched'):
            value = self.rows(ids.copy())
            if fault == 'missing': value['documents'].pop()
            if fault == 'duplicate': value['ids'][1] = ids[0]
            if fault == 'foreign': value['ids'][1] = self.source()['collectionId']
            if fault in ('private', 'deleted', 'mismatched'):
                row = json.loads(value['documents'][0])
                row[{'private': 'IsPublic', 'deleted': 'IsDeleted', 'mismatched': 'Id'}[fault]] = (
                    False if fault == 'private' else True if fault == 'deleted' else ids[1])
                value['documents'][0] = json.dumps(row)
            with self.subTest(fault=fault), self.assertRaises(ValueError):
                p.validate_rows(value, ids)

    def test_invalid_source_or_credential_sends_nothing(self):
        for origin in ('http://chroma.example.com', 'https://secret@chroma.example.com',
                       'https://chroma.example.com/path', 'https://chroma.example.com?token=secret'):
            source = self.source(); source['origin'] = origin
            with self.assertRaises(ValueError):
                p.qualify(source, 'controlled', lambda *args: self.fail('Unexpected request'))
        for token in ('', 'bad\ncredential'):
            with self.assertRaises(ValueError):
                p.qualify(self.source(), token, lambda *args: self.fail('Unexpected request'))

    def test_json_duplicates_and_oversized_documents_rejected(self):
        with self.assertRaises(ValueError):
            json.loads('{"id":1,"id":2}', object_pairs_hook=p.unique)
        value = self.rows(self.source()['ids'])
        value['documents'][0] = 'x' * (1024 * 1024 + 1)
        with self.assertRaises(ValueError):
            p.validate_rows(value, self.source()['ids'])

    def test_private_or_mixed_dns_answers_rejected_before_credential_dispatch(self):
        public = (2, 1, 6, '', ('8.8.8.8', 443))
        private = (2, 1, 6, '', ('127.0.0.1', 443))
        for addresses in ([], [private], [public, private]):
            with patch.object(p.socket, 'getaddrinfo', return_value=addresses), patch.object(p, 'build_opener') as opener:
                with self.assertRaises(ValueError):
                    p.send('GET', 'https://chroma.example.com/api/v2', None, 'controlled')
                opener.assert_not_called()


if __name__ == '__main__':
    unittest.main()
