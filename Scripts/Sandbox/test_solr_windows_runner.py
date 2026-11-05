import importlib.util
from pathlib import Path
from unittest import TestCase, main
from unittest.mock import Mock, patch
import tempfile
import urllib.error
import json
import hashlib

SPEC = importlib.util.spec_from_file_location('cosmic_solr_runner', Path(__file__).parents[1] / 'TestHosts/run-cosmic-solr-windows.py')
RUNNER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RUNNER)

class SolrRunnerControls(TestCase):
    def test_only_actual_schema_readiness_admits_source(self):
        process = Mock()
        process.poll.return_value = None
        warming = urllib.error.HTTPError('http://127.0.0.1', 503, 'loading', None, None)
        with patch.object(RUNNER, 'call', side_effect=[warming, TimeoutError(), {'uniqueKey':'id'}]) as call, patch.object(RUNNER.time, 'sleep'):
            RUNNER.wait_until_ready(process)
        self.assertEqual(3, call.call_count)
        for request in call.call_args_list:
            self.assertEqual('/solr/oasis_holons/schema/uniquekey?wt=json', request.args[0])
            self.assertEqual({'timeout':2}, request.kwargs)

    def test_readiness_does_not_hide_wrong_schema_or_http_failure(self):
        process = Mock()
        process.poll.return_value = None
        for failure in [{'uniqueKey':'wrong'}, urllib.error.HTTPError('http://127.0.0.1', 404, 'absent', None, None)]:
            with self.subTest(failure=type(failure).__name__), patch.object(RUNNER, 'call') as call:
                if isinstance(failure, Exception): call.side_effect = failure
                else: call.return_value = failure
                with self.assertRaises((RuntimeError, urllib.error.HTTPError)):
                    RUNNER.wait_until_ready(process)
                self.assertEqual(1, call.call_count)

    def test_dead_process_and_deadline_fail_readiness(self):
        process = Mock()
        process.poll.return_value = 1
        with patch.object(RUNNER, 'call') as call, self.assertRaises(RuntimeError):
            RUNNER.wait_until_ready(process)
        call.assert_not_called()
        process.poll.return_value = None
        with patch.object(RUNNER, 'call', side_effect=TimeoutError()), patch.object(RUNNER.time, 'monotonic', side_effect=[0,61]), self.assertRaises(TimeoutError):
            RUNNER.wait_until_ready(process)

    def test_dispatched_or_acknowledged_seed_never_replays(self):
        for blocked in ['seed-dispatch.intent.json', 'seed.ack.json']:
            with self.subTest(blocked=blocked), tempfile.TemporaryDirectory() as directory:
                state = Path(directory)
                (state / 'seed.intent.json').write_text('{}')
                (state / blocked).write_text('{}')
                with patch.object(RUNNER, 'STATE', state), patch.object(RUNNER, 'assert_port_free'), patch.object(RUNNER.subprocess, 'Popen') as launch, patch('sys.argv', ['runner', '--resume-unseeded']):
                    with self.assertRaises(RuntimeError): RUNNER.run()
                launch.assert_not_called()

    def test_original_seed_and_missing_control_are_verified_before_paired_reads(self):
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory)
            source = {'MissingId':'missing'}
            (state / 'source.json').write_text(json.dumps(source))
            expected = {'id':'live', 'name':'original'}
            seed = {'sourceSha256':hashlib.sha256((state / 'source.json').read_bytes()).hexdigest(), 'rows':[expected]}
            (state / 'seed.intent.json').write_text(json.dumps(seed))
            (state / 'seed.ack.json').write_text(json.dumps({'intentSha256':hashlib.sha256((state / 'seed.intent.json').read_bytes()).hexdigest()}))
            for native, absent, passes in [(expected, None, True), ({'id':'live','name':'changed'}, None, False), (expected, {'id':'missing'}, False)]:
                with self.subTest(native=native, absent=absent), patch.object(RUNNER, 'STATE', state), patch.object(RUNNER, 'call', side_effect=[{'doc':native},{'doc':absent}]):
                    if passes: self.assertEqual(1, len(RUNNER.observe_source()))
                    else:
                        with self.assertRaises(RuntimeError): RUNNER.observe_source()
            (state / 'source.json').write_text('{}')
            with patch.object(RUNNER, 'STATE', state), patch.object(RUNNER, 'call') as call:
                with self.assertRaises(RuntimeError): RUNNER.observe_source()
                call.assert_not_called()

if __name__ == '__main__': main()
