"""Exercise the actual one-shot transport function without running the cloud orchestrator."""
import ast,hashlib,io,json,tempfile,unittest,urllib.request,urllib.error
from pathlib import Path
from unittest.mock import Mock,patch

class OpenVinoTransportTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        tree=ast.parse(Path(__file__).with_name('Qualify-OpenVinoModelServer.py').read_text(encoding='utf-8'))
        function=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='call')
        class NoRedirect(urllib.request.HTTPRedirectHandler):
            def redirect_request(self,*args):return None
        self.scope={'result':self.root,'sequence':0,'urllib':urllib,'json':json,'hashlib':hashlib,'NoRedirect':NoRedirect}
        exec(compile(ast.Module(body=[function],type_ignores=[]),'<actual-owned-ovms-transport>','exec'),self.scope)
        self.call=self.scope['call']

    def test_startup_transport_failure_is_observed_once_without_hidden_retry(self):
        for error in [ConnectionResetError('startup'),urllib.error.URLError(ConnectionRefusedError('startup')),TimeoutError('startup')]:
            with self.subTest(error=type(error).__name__),patch('urllib.request.build_opener') as factory:
                factory.return_value.open.side_effect=error
                self.assertEqual((None,None),self.call('/v2/health/ready'))
                factory.return_value.open.assert_called_once()
                record=json.loads((self.root/(str(self.scope['sequence'])+'-transport.json')).read_bytes())
                self.assertEqual('connect',record['stage']);self.assertIsNone(record['observedHttpStatus'])

    def test_response_body_reset_is_recorded_without_retry(self):
        response=Mock(status=200);response.__enter__=Mock(return_value=response);response.__exit__=Mock(return_value=False)
        response.read.side_effect=ConnectionResetError('during-body')
        with patch('urllib.request.build_opener') as factory:
            factory.return_value.open.return_value=response
            self.assertEqual((None,None),self.call('/v2'))
            factory.return_value.open.assert_called_once()
        record=json.loads((self.root/'1-transport.json').read_bytes())
        self.assertEqual('body',record['stage']);self.assertEqual(200,record['observedHttpStatus'])

    def test_success_preserves_raw_and_status_before_any_consumer_parse(self):
        raw=b'{"version":"2026.4.1"}'
        response=Mock(status=200);response.__enter__=Mock(return_value=response);response.__exit__=Mock(return_value=False)
        response.read.return_value=raw
        with patch('urllib.request.build_opener') as factory:
            factory.return_value.open.return_value=response
            self.assertEqual((200,raw),self.call('/v2'))
            factory.return_value.open.assert_called_once()
        self.assertEqual(raw,(self.root/'1-raw.json').read_bytes())
        self.assertEqual(hashlib.sha256(raw).hexdigest(),json.loads((self.root/'1-status.json').read_bytes())['sha256'])

    def test_http_failure_remains_a_failure_and_is_not_retried(self):
        error=urllib.error.HTTPError('http://127.0.0.1:28000/v2',503,'unavailable',{},io.BytesIO(b'{"error":"not-ready"}'))
        with patch('urllib.request.build_opener') as factory:
            factory.return_value.open.side_effect=error
            self.assertEqual((503,b'{"error":"not-ready"}'),self.call('/v2'))
            factory.return_value.open.assert_called_once()
        self.assertEqual(503,json.loads((self.root/'1-status.json').read_bytes())['status'])

if __name__=='__main__':unittest.main()
