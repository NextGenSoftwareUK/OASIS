"""Lock the actual cloud startup health probe's readiness/error boundary."""
import ast
import os
from pathlib import Path
import subprocess
import unittest


class StartupProbeTests(unittest.TestCase):
    def test_typed_docker_absence_excludes_daemon_and_permission_failures(self):
        import runpy
        missing = runpy.run_path(str(Path(__file__).with_name('qualify_web4_combined_startup.py')))['missing_resource']
        for kind, message in [('network','Error response from daemon: network owned not found'),
                              ('container','Error: No such container: owned'),
                              ('volume','Error response from daemon: get owned: no such volume')]:
            with self.subTest(kind=kind):
                self.assertTrue(missing(kind,'owned',1,message))
                self.assertFalse(missing(kind,'other',1,message))
                self.assertFalse(missing(kind,'owned',125,message))
                self.assertFalse(missing(kind,'owned',1,'permission denied'))
                self.assertFalse(missing(kind,'owned',1,'Cannot connect to the Docker daemon'))

    def test_actual_probe_classifies_readiness_without_masking_errors(self):
        tree = ast.parse(Path(__file__).with_name('qualify_web4_combined_startup.py').read_text(encoding='utf-8-sig'))
        probe = next(n.value.value for n in ast.walk(tree) if isinstance(n, ast.Assign)
                     and any(isinstance(t, ast.Name) and t.id == 'probe' for t in n.targets))
        cases = [
            ('healthy', "return {ok:true,status:200,json:async()=>({status:'healthy',service:'OASIS API ONODE WebAPI'})}", 0),
            ('wrong-service', "return {ok:true,status:200,json:async()=>({status:'healthy',service:'other'})}", 1),
            ('forbidden', "return {ok:false,status:403}", 1),
            ('warming', "return {ok:false,status:503}", 2),
            ('connection-refused', "throw Object.assign(Error('connecting'),{cause:{code:'ECONNREFUSED'}})", 2),
            ('unexpected-network-error', "throw Object.assign(Error('network'),{cause:{code:'ENOTFOUND'}})", 1),
            ('invalid-json', "return {ok:true,status:200,json:async()=>{throw SyntaxError('invalid')}}", 1),
        ]
        for label, response, expected in cases:
            with self.subTest(label=label):
                script = 'globalThis.fetch=async()=>{' + response + '};\n' + probe
                result = subprocess.run([os.environ.get('NODE_BINARY', 'node'), '-e', script], capture_output=True, text=True, timeout=8)
                self.assertEqual(expected, result.returncode, result.stderr)


if __name__ == '__main__':
    unittest.main()
