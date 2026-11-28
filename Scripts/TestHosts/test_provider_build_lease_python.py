import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT=Path(__file__).with_name('invoke_provider_build_lease.py')
spec=importlib.util.spec_from_file_location('provider_lease',SCRIPT)
lease=importlib.util.module_from_spec(spec)
spec.loader.exec_module(lease)

class ProviderLeaseTests(unittest.TestCase):
    def test_busy_never_starts_contender(self):
        with tempfile.TemporaryDirectory(prefix='cosmic-lease-') as directory:
            workspace=Path(directory)/'workspace';workspace.mkdir()
            with lease.acquire(workspace,'holder'):
                result=subprocess.run([sys.executable,'-B',str(SCRIPT),'--owner','contender',
                    '--workspace',str(workspace),'--',sys.executable,'-c','print("OPERATION_STARTED")'],capture_output=True,text=True)
                self.assertEqual(75,result.returncode)
                self.assertNotIn('OPERATION_STARTED',result.stdout)
            with lease.acquire(workspace,'next-holder'):
                pass
    def test_failed_command_releases_lease(self):
        with tempfile.TemporaryDirectory(prefix='cosmic-lease-') as directory:
            workspace=Path(directory)/'workspace';workspace.mkdir()
            result=subprocess.run([sys.executable,'-B',str(SCRIPT),'--owner','failing',
                '--workspace',str(workspace),'--',sys.executable,'-c','raise SystemExit(7)'],capture_output=True,text=True)
            self.assertEqual(7,result.returncode)
            with lease.acquire(workspace,'after-failure'):
                pass

if __name__=='__main__':
    unittest.main()
