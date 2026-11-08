"""Pure enrollment controls; no credentials, providers, Railway or native calls."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('tron_configuration',
    Path(__file__).with_name('Prepare-HostedExactKeyConfiguration.py'))
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)
ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests/Fixtures/tron-public-header-source.json'
PROVIDER = 'TRONOASIS'
SOURCE = 'CQL_TRON_PUBLIC_HEADER_QUERY_SOURCE_JSON'
READER = 'COSMIC_TRON_PUBLIC_READER_KEY'
PARTITION = 'CQL_HOSTED_QUERY_EXAMPLES__TRONOASIS'


class TronConfigurationTests(unittest.TestCase):
    def review(self):
        return configuration.verifier.reviewed_exact_key_source(PROVIDER, FIXTURE)

    def baseline(self):
        return {'OASIS_DNA_JSON': json.dumps({'OASIS': {'Sandbox': {'Enabled': True},
            'StorageProviders': {'TRONOASIS': {'PrivateKey': 'inert-wallet-marker'}}}}),
            configuration.ADMISSION: json.dumps({'ordinaryProviders': ['MongoDBOASIS'],
                'nativeProviders': [], 'queryOnlyProviders': ['IPFSOASIS'], 'storageTariffs': [],
                'queryTariffs': [{'provider': 'IPFSOASIS', 'costUsd': 0, 'catalogueVersion': 'test-v1'}],
                'routes': [{'path': 'retained'}]}),
            configuration.GRANTS: json.dumps({'IPFSOASIS': ['retained-public-id']}),
            configuration.EXAMPLES: json.dumps({'IPFSOASIS': ['retained-example']}),
            READER: 'controlled-reader-marker', 'UNRELATED_SETTING': 'retained'}

    def test_retained_catalogue_produces_height_presets_and_distinct_public_ids(self):
        original = FIXTURE.read_bytes()
        self.assertEqual('7f1c8fe3c35ef3c45ee31be6eaef72f34f8d10185a28e2177ace267822e89c02',
                         hashlib.sha256(original).hexdigest())
        review = self.review()
        self.assertEqual('TronBlockHeight', review['requiredQueryableMetadataKey'])
        self.assertEqual(3, len(set(review['publicIds'])))
        self.assertEqual(['a87726c3-9e6c-83c1-b5a9-a16117120d23',
                          '3c627b14-07f6-83e4-ac4f-2d5c1a50aa51',
                          'ead3d916-0c33-83d4-a156-84ef80f34b5a'], review['publicIds'])
        self.assertEqual(json.loads(original), json.loads(review['sourceBinding'][SOURCE]))
        self.assertEqual(4, len(review['presets']))
        for native, paired, keys in [(review['presets'][0], review['presets'][1], ['1','2','3']),
                                     (review['presets'][2], review['presets'][3], ['1','2'])]:
            predicate = native['request']['filters'][0]
            self.assertEqual({'metadataPath':['TronBlockHeight'], 'scalar':'String', 'operator':'In', 'value':keys}, predicate)
            self.assertEqual(predicate, paired['request']['source']['filters'][0])
            self.assertEqual(['MongoDBOASIS', PROVIDER], paired['request']['providers'])
            self.assertEqual('PreserveCopies', paired['request']['identityMode'])
        self.assertEqual(original, FIXTURE.read_bytes())

    def test_candidate_preserves_wallet_dna_key_existing_providers_and_input(self):
        review = self.review()
        baseline = self.baseline()
        original = copy.deepcopy(baseline)
        bound = baseline | review['sourceBinding']
        after = configuration.candidate(bound, PROVIDER, review)
        self.assertEqual(original, baseline)
        self.assertEqual({SOURCE, configuration.ADMISSION, configuration.GRANTS, PARTITION},
            {key for key in after if baseline.get(key) != after[key]})
        self.assertEqual(baseline['OASIS_DNA_JSON'], after['OASIS_DNA_JSON'])
        self.assertEqual(baseline[READER], after[READER])
        self.assertEqual(baseline[configuration.EXAMPLES], after[configuration.EXAMPLES])
        self.assertEqual(review['publicIds'], json.loads(after[configuration.GRANTS])[PROVIDER])
        admission = json.loads(after[configuration.ADMISSION])
        self.assertEqual(['IPFSOASIS', PROVIDER], admission['queryOnlyProviders'])
        self.assertEqual([], admission['storageTariffs'])
        self.assertEqual(0, admission['queryTariffs'][-1]['costUsd'])
        self.assertNotIn(baseline[READER], after[PARTITION])
        self.assertNotIn(baseline[READER], after[SOURCE])

    def test_reader_format_matches_sdk_boundary_and_never_uses_anonymous_access(self):
        review = self.review()
        before = self.baseline() | review['sourceBinding']
        for invalid in [None, '', ' ', 'two,keys', 'bad\r\nheader', 'é', 'x'*257]:
            with self.subTest(invalid=repr(invalid)), self.assertRaisesRegex(ValueError, 'protected source token'):
                configuration.candidate(before | {READER: invalid}, PROVIDER, review)
        for valid in ['x', 'x'*256]:
            with self.subTest(length=len(valid)):
                self.assertEqual(valid, configuration.candidate(before | {READER: valid}, PROVIDER, review)[READER])

    def test_encryption_requires_existing_explicit_queryable_height_policy(self):
        review = self.review()
        before = self.baseline() | review['sourceBinding']
        for flag in ['Rijndael256EncryptionEnabled', 'QuantumEncryptionEnabled']:
            dna = json.loads(before['OASIS_DNA_JSON'])
            encryption = {flag: True, 'AdditionalQueryableKeys': []}
            dna['OASIS']['Security'] = {'HolonDataEncryption': encryption}
            bound = before | {'OASIS_DNA_JSON': json.dumps(dna)}
            with self.subTest(flag=flag), self.assertRaisesRegex(ValueError, 'explicitly queryable'):
                configuration.candidate(bound, PROVIDER, review)
            encryption['AdditionalQueryableKeys'] = ['TronBlockHeight']
            bound['OASIS_DNA_JSON'] = json.dumps(dna)
            self.assertEqual(bound['OASIS_DNA_JSON'], configuration.candidate(bound, PROVIDER, review)['OASIS_DNA_JSON'])

    def test_existing_provider_bindings_and_write_tariffs_require_reconciliation(self):
        review = self.review()
        before = self.baseline() | review['sourceBinding']
        for field in ['ordinaryProviders', 'queryOnlyProviders', 'nativeProviders', 'queryTariffs', 'storageTariffs']:
            admission = json.loads(before[configuration.ADMISSION])
            admission[field].append(PROVIDER if field.endswith('Providers') and field != 'nativeProviders' else {'provider': PROVIDER})
            with self.subTest(field=field), self.assertRaisesRegex(ValueError, 'reconciliation'):
                configuration.candidate(before | {configuration.ADMISSION: json.dumps(admission)}, PROVIDER, review)
        for key, value in [(configuration.GRANTS, json.dumps({PROVIDER: review['publicIds']})),
                           (configuration.EXAMPLES, json.dumps({PROVIDER: []})), (PARTITION, '[]')]:
            with self.subTest(setting=key), self.assertRaisesRegex(ValueError, 'reconciliation'):
                configuration.candidate(before | {key: value}, PROVIDER, review)

    def test_source_and_free_tariff_must_match_the_review(self):
        review = self.review()
        before = self.baseline() | review['sourceBinding']
        with self.assertRaisesRegex(ValueError, 'do not match'):
            configuration.candidate(before | {SOURCE:'{}'}, PROVIDER, review)
        for cost in [1, True]:
            admission = json.loads(before[configuration.ADMISSION])
            admission['queryTariffs'][0]['costUsd'] = cost
            with self.subTest(cost=cost), self.assertRaisesRegex(ValueError, 'zero-cost'):
                configuration.candidate(before | {configuration.ADMISSION:json.dumps(admission)}, PROVIDER, review)

    def test_changed_catalogues_credentials_and_duplicate_fields_are_rejected(self):
        source = json.loads(FIXTURE.read_bytes())
        changes = [{'schema':True}, {'schema':1.0}, {'rpcUrl':'https://foreign.example'},
                   {'blockHeights':['1','2','4']}, {'blockHeights':[1,2,3]},
                   {'sourceOwner':'00000000-0000-0000-0000-000000000000'},
                   {'readerKey':'inert-forbidden-field'}, {'genesisHash':'0'*64}]
        with tempfile.TemporaryDirectory(prefix='cosmic-tron-config-') as temporary:
            path = Path(temporary)/'catalogue.json'
            for change in changes:
                path.write_text(json.dumps(source | change), encoding='utf-8')
                with self.subTest(fields=list(change)), self.assertRaises(RuntimeError):
                    configuration.verifier.reviewed_exact_key_source(PROVIDER, path)
            path.write_text(json.dumps(source).replace('"schema": 1', '"schema": 1, "schema": 1'), encoding='utf-8')
            with self.assertRaises(RuntimeError):
                configuration.verifier.reviewed_exact_key_source(PROVIDER, path)


if __name__ == '__main__':
    unittest.main()
