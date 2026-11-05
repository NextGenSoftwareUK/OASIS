import importlib.util
from pathlib import Path
from unittest import TestCase,main
from unittest.mock import Mock,patch
import urllib.error
import io
import json

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

if __name__=='__main__':main()
