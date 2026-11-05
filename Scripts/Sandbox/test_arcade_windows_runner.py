import importlib.util
from pathlib import Path
from unittest import TestCase, main
from unittest.mock import Mock, patch
import urllib.error
import json
import tempfile
import uuid

SPEC = importlib.util.spec_from_file_location('arcade_runner',Path(__file__).parents[1] / 'TestHosts/run-cosmic-arcade-windows.py')
RUNNER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RUNNER)

class ArcadeRunnerReadiness(TestCase):
    def test_readiness_requires_native204_and_only_retries_pre_ready_states(self):
        process = Mock(); process.poll.return_value = None
        warming = urllib.error.HTTPError('http://127.0.0.1',503,'warming',None,None)
        with patch.object(RUNNER,'request',side_effect=[warming,TimeoutError(),None]) as request, patch.object(RUNNER.time,'sleep'):
            RUNNER.wait_until_ready(process,'private-value')
        self.assertEqual(3,request.call_count)
        self.assertTrue(all(call.args[0]=='/api/v1/ready' and call.kwargs=={'timeout':2} for call in request.call_args_list))

    def test_wrong_readiness_response_or_auth_failure_does_not_admit_bootstrap(self):
        process = Mock(); process.poll.return_value = None
        for value in [{},urllib.error.HTTPError('http://127.0.0.1',401,'denied',None,None)]:
            with self.subTest(kind=type(value).__name__),patch.object(RUNNER,'request') as request:
                if isinstance(value,Exception): request.side_effect=value
                else: request.return_value=value
                with self.assertRaises((RuntimeError,urllib.error.HTTPError)):
                    RUNNER.wait_until_ready(process,'private-value')
                self.assertEqual(1,request.call_count)
                if isinstance(value,urllib.error.HTTPError): value.close()

    def test_dead_process_and_deadline_fail_before_bootstrap(self):
        process = Mock(); process.poll.return_value = 1
        with patch.object(RUNNER,'request') as request,self.assertRaises(RuntimeError):
            RUNNER.wait_until_ready(process,'private-value')
        request.assert_not_called()
        process.poll.return_value=None
        with patch.object(RUNNER,'request',side_effect=TimeoutError()),patch.object(RUNNER.time,'monotonic',side_effect=[0,61]),self.assertRaises(TimeoutError):
            RUNNER.wait_until_ready(process,'private-value')

    def test_original_teaching_values_and_absent_control_are_required(self):
        id = str(uuid.uuid4()); deleted = str(uuid.uuid4()); missing = str(uuid.uuid4()); actor = str(uuid.uuid4())
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory)
            (state / 'source.json').write_text(json.dumps({'LiveIds':[id],'DeletedId':deleted,'MissingId':missing,'Actor':actor}))
            def native(key, is_deleted=False):
                return {'result':[{'oasisId':key,'doc':json.dumps({'Id':key,'Name':'ArcadeDB CQL teaching ' + uuid.UUID(key).hex,
                    'CreatedByAvatarId':actor,'ParentHolonId':actor,'IsPublic':False,'IsActive':True,
                    'DeletedDate':'2026-11-05T00:00:00Z' if is_deleted else '0001-01-01T00:00:00'})}]}
            responses = [native(id),native(deleted,True),{'result':[]}]
            with patch.object(RUNNER,'STATE',state),patch.object(RUNNER,'request',side_effect=responses):
                self.assertEqual(2,len(RUNNER.observe_source('private-value')))
            bad = native(id); bad['result'][0]['doc']='{}'
            for values in [[bad], [native(id),native(deleted,True),native(missing)]]:
                with patch.object(RUNNER,'STATE',state),patch.object(RUNNER,'request',side_effect=values),self.assertRaises(RuntimeError):
                    RUNNER.observe_source('private-value')

if __name__ == '__main__': main()
