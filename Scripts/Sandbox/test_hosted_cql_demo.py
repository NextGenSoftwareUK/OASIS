"""Offline protocol checks only; these do not establish hosted demo acceptance."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('hosted_cql_demo', Path(__file__).with_name('Verify-HostedCqlDemo.py'))
demo = importlib.util.module_from_spec(spec)
spec.loader.exec_module(demo)


class HostedDemoProtocolTests(unittest.TestCase):
    def setUp(self):
        output = ROOT / 'TestResults/Web4SandboxCqlCurrent'
        output.mkdir(parents=True, exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(prefix='owned-verifier-protocol-', dir=output)
        self.addCleanup(self.temporary.cleanup)
        self.folder = Path(self.temporary.name)

    def test_body_and_header_operation_ids_and_both_error_contracts_are_preserved(self):
        actual = demo.outcome('data/query-cql', 503, 'key', None, {'operationId': 'body-op', 'code': 'SETTLEMENT_PENDING'})
        self.assertEqual('body-op', actual['operationId'])
        self.assertEqual('SETTLEMENT_PENDING', actual['errorCode'])
        self.assertTrue(actual['isError'])
        actual = demo.outcome('data/save-holon', 409, 'key', 'header-op', {'operationId': 'body-op', 'errorCode': 'CONFLICT'})
        self.assertEqual('header-op', actual['operationId'])
        self.assertEqual('CONFLICT', actual['errorCode'])

    def test_retained_and_interrupted_intents_prevent_any_hosted_call(self):
        journal = self.folder / 'fixture.json'
        for path in [journal, journal.with_suffix('.json.new')]:
            path.write_text('{"pending":{"operationId":"original"}}')
            with patch.object(demo, 'STATE', self.folder), patch('sys.argv', ['verify', '--deployment',
                    'e88fbed5-5147-49a2-8915-93bb5b0684d0', '--commit', 'a' * 40]), \
                    patch.object(demo.subprocess, 'check_output') as hosted:
                with self.assertRaisesRegex(RuntimeError, 'never reseed/replay'):
                    demo.main()
                hosted.assert_not_called()
            self.assertEqual('original', json.loads(path.read_text())['pending']['operationId'])
            path.unlink()

    def test_complete_journal_replaces_previous_document_without_leftover_temporary_file(self):
        journal = self.folder / 'fixture.json'
        demo.write(journal, {'pending': {'operationId': 'original'}})
        demo.write(journal, {'pending': None, 'records': ['acknowledged']})
        self.assertEqual({'pending': None, 'records': ['acknowledged']}, json.loads(journal.read_text()))
        self.assertFalse(journal.with_suffix('.json.new').exists())

    def test_acceptance_matrix_fits_the_actual_enrolled_free_profile(self):
        profile = json.loads((ROOT / 'Scripts/Sandbox/cql-free-admission.json').read_text())
        self.assertEqual(demo.PROVIDERS, profile['ordinaryProviders'])
        cases = demo.query_cases('fixture-tag')
        self.assertEqual(5, len(cases))
        self.assertEqual(19, 3 + len(demo.PROVIDERS) * len(cases) + 1)
        for label, filters, expected, options in cases:
            self.assertEqual('fixture-tag', filters[0]['value'])
        names = {case[0] for case in cases}
        self.assertEqual({'metadata-and-or', 'name', 'description', 'paging', 'count'}, names)
        self.assertEqual(2, len(cases[0][3]['any_of']))
        self.assertEqual('2', cases[-1][2])
        self.assertTrue(cases[-1][3]['scalar'])

    def test_only_complete_mutation_acknowledgements_can_clear_a_pending_intent(self):
        identity = 'e88fbed5-5147-49a2-8915-93bb5b0684d0'
        for path, response in [('sandbox/session', {'result': {'jwtToken': 'never-persist'}}),
                ('data/save-holon', {'isSaved': False, 'result': {'id': identity}}),
                ('data/save-holon', {'isSaved': True, 'result': {}})]:
            with self.assertRaisesRegex(RuntimeError, 'never replay'):
                demo.acknowledged_identity(path, response)
        self.assertEqual(identity, demo.acknowledged_identity('sandbox/session',
            {'result': {'id': identity, 'jwtToken': 'never-persist'}}))
        self.assertEqual(identity, demo.acknowledged_identity('data/save-holon',
            {'isSaved': True, 'result': {'id': identity}}))

    def test_result_wrapper_does_not_turn_a_provider_error_into_success(self):
        nested = {'isError': True, 'errorCode': 'CQL_QUERY_CANCELLED'}
        self.assertIs(nested, demo.unwrap({'result': nested}))
        normal = {'isError': False, 'result': {'rows': []}}
        self.assertIs(normal, demo.unwrap(normal))


if __name__ == '__main__':
    unittest.main()
