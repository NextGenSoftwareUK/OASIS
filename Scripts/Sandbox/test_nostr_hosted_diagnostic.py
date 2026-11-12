"""Pure diagnostic bounds/redaction checks; no native requests or credentials."""
import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('nostr_diagnostic', Path(__file__).with_name('Diagnose-HostedNostrReads.py'))
diagnostic = importlib.util.module_from_spec(spec)
spec.loader.exec_module(diagnostic)


class NostrDiagnosticTests(unittest.TestCase):
    def test_operator_projection_excludes_all_raw_surrounding_values(self):
        text = b'Bearer secret\nCQL native failure provider=NostrOASIS phase=query stage=connect category=http/websocket httpStatus=503 websocket=NotAWebSocket\nhttps://user:password@private/?token=secret'
        self.assertEqual(diagnostic.safe_operator_events(text), [{'phase': 'query', 'stage': 'connect',
            'categories': 'http/websocket', 'httpStatus': 503, 'webSocketError': 'NotAWebSocket'}])

    def test_unknown_category_or_other_provider_does_not_pass(self):
        self.assertEqual(diagnostic.safe_operator_events(b'CQL native failure provider=NostrOASIS phase=query stage=connect category=secret httpStatus=503 websocket=Unknown'), [])
        self.assertEqual(diagnostic.safe_operator_events(b'CQL native failure provider=Other phase=query stage=connect category=http httpStatus=503 websocket=Unknown'), [])
        self.assertEqual(diagnostic.safe_operator_events(b'CQL native failure provider=NostrOASIS phase=query stage=connect category=http httpStatus=503 websocket=SecretToken'), [])

    def test_only_the_original_bounded_native_request_is_selected(self):
        keys = ['a' * 64, 'b' * 64, 'c' * 64]
        request = {'provider': 'NostrOASIS', 'filters': [{'field': None, 'metadataPath': ['NostrEventId'], 'scalar': 'String',
            'operator': 'In', 'value': keys[:2]}], 'orderBy': 'None', 'take': 25,
            'maximumDurationMilliseconds': 30000, 'includeMetadata': True,
            'anyOf': [], 'anyChildren': [], 'anyDescendants': [], 'descending': False, 'skip': 0, 'graph': None}
        native = {'native': [{'result': {'rows': [1, 2, 3]}}, {'result': {'rows': [1, 2]}}],
                  'source': {'examples': [{'id': 'public-subset', 'request': request}]}}
        self.assertEqual(diagnostic.validate_request(native, {'eventIds': keys}), request)
        for field, value in [('provider', 'MongoDBOASIS'), ('take', 10000), ('maximumDurationMilliseconds', 300000),
                             ('anyOf', [['extra']]), ('graph', {'depth': 1})]:
            changed = copy.deepcopy(native); changed['source']['examples'][0]['request'][field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                diagnostic.validate_request(changed, {'eventIds': keys})


if __name__ == '__main__':
    unittest.main()
