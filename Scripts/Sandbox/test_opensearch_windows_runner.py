import importlib.util
from pathlib import Path
from unittest import TestCase,main
from unittest.mock import Mock,patch
import urllib.error
import io
import json
import tempfile
import hashlib

SPEC=importlib.util.spec_from_file_location('opensearch_runner',Path(__file__).parents[1]/'TestHosts/run-cosmic-opensearch-windows.py')
MODULE=importlib.util.module_from_spec(SPEC);SPEC.loader.exec_module(MODULE)

def health(status='red',timed_out=True,cluster=None):
    return {'cluster_name':cluster or MODULE.CLUSTER,'status':status,'timed_out':timed_out}

def timeout(body):
    return urllib.error.HTTPError('http://127.0.0.1:19200/_cluster/health',408,'timed health wait',None,io.BytesIO(body))

class OpenSearchReadinessControls(TestCase):
    def test_documented408_health_timeout_is_readiness_state(self):
        value=health()
        with patch.object(MODULE,'request',side_effect=timeout(json.dumps(value).encode())):
            self.assertEqual(value,MODULE.readiness_health())

    def test_malformed_foreign_or_oversized408_is_failure(self):
        for body in [b'{}',json.dumps(health(cluster='foreign')).encode(),json.dumps(health(timed_out=False)).encode(),b'x'*65537]:
            with self.subTest(bytes=len(body)),patch.object(MODULE,'request',side_effect=timeout(body)),self.assertRaises(RuntimeError):
                MODULE.readiness_health()

    def test_warming_cluster_is_not_admitted_before_yellow(self):
        process=Mock();process.poll.return_value=None;process.pid=1234
        identity={'version':{'number':'3.9.0','distribution':'opensearch'},'cluster_name':MODULE.CLUSTER}
        with patch.object(MODULE,'request',return_value=identity),patch.object(MODULE,'readiness_health',side_effect=[health(),health('green',False)]),patch.object(MODULE,'assert_listener_owned',return_value=['owned']) as listeners,patch.object(MODULE.time,'sleep'):
            result=MODULE.wait_ready(process)
        self.assertEqual('green',result['health']['status']);listeners.assert_called_once_with(1234)

    def test_auth_error_and_dead_process_do_not_poll_into_success(self):
        denied=urllib.error.HTTPError('http://127.0.0.1',401,'denied',None,None)
        with patch.object(MODULE,'request',side_effect=denied),self.assertRaises(urllib.error.HTTPError):MODULE.readiness_health()
        denied.close()
        process=Mock();process.poll.return_value=1
        with patch.object(MODULE,'request') as request,self.assertRaises(RuntimeError):MODULE.wait_ready(process)
        request.assert_not_called()

    def test_original_payload_hashes_versions_and_missing_control_are_required(self):
        with tempfile.TemporaryDirectory() as directory:
            state=Path(directory);source={'LiveIds':['live'],'DeletedId':'deleted','MissingId':'absent'}
            (state/'source.json').write_text(json.dumps(source))
            source_hash=hashlib.sha256((state/'source.json').read_bytes()).hexdigest()
            raw=b'{"id":"original"}'
            for key in ['live','deleted']:
                (state/('bootstrap-'+key+'.intent.json')).write_text(json.dumps({'sourceSha256':source_hash,'payloadSha256':hashlib.sha256(raw).hexdigest()}))
                (state/('bootstrap-'+key+'.ack.json')).write_text(json.dumps({'sourceSha256':source_hash,'version':1}))
            def doc(key):return {'_index':'oasis_holons','_id':key,'found':True,'_version':1}
            missing=urllib.error.HTTPError('http://127.0.0.1',404,'absent',None,io.BytesIO(json.dumps({'_index':'oasis_holons','_id':'absent','found':False}).encode()))
            with patch.object(MODULE,'STATE',state),patch.object(MODULE,'request',side_effect=[doc('live'),doc('deleted'),missing]),patch.object(MODULE.OPENER,'open',side_effect=[io.BytesIO(raw),io.BytesIO(raw)]):
                self.assertEqual(2,len(MODULE.observe_source()))
            with patch.object(MODULE,'STATE',state),patch.object(MODULE,'request',return_value=doc('live')),patch.object(MODULE.OPENER,'open',return_value=io.BytesIO(b'changed')),self.assertRaises(RuntimeError):
                MODULE.observe_source()
            wrong=doc('live');wrong['_version']=2
            with patch.object(MODULE,'STATE',state),patch.object(MODULE,'request',return_value=wrong),self.assertRaises(RuntimeError):MODULE.observe_source()

if __name__=='__main__':main()
