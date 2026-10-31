"""Verify the bounded inherited-read translator on the existing owned Mongo replica set."""
from pathlib import Path
import hashlib
import json
import os
import signal
import subprocess
import time
import xml.etree.ElementTree as ET
from cosmic_linux_mongo_service import owned_linux_mongo

ROOT = Path(__file__).resolve().parents[2]
RESULTS = ROOT / 'TestResults/Web4SandboxCqlCurrent'
DOTNET_ROOT = '/var/tmp/oasis-runtimes/dotnet-10.0.12'
VSTEST = '/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
DLL = ROOT / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests.dll'


def main():
    assert os.geteuid() != 0
    assert Path(DOTNET_ROOT + '/dotnet').is_file() and Path(VSTEST).is_file() and DLL.is_file()
    RESULTS.mkdir(parents=True, exist_ok=True)
    started = time.time()
    with owned_linux_mongo(RESULTS) as environment:
        with (RESULTS / 'mongo-ancestor-native.log').open('w') as log:
            process = subprocess.Popen([DOTNET_ROOT + '/dotnet', VSTEST, str(DLL),
                '--TestCaseFilter:FullyQualifiedName~MongoCqlAncestorTests', '--ResultsDirectory:' + str(RESULTS),
                '--Logger:trx;LogFileName=mongo-ancestor-native.trx'], cwd=RESULTS,
                env={**os.environ, **environment, 'DOTNET_ROOT': DOTNET_ROOT,
                    'OASIS_CQL_ANCESTOR_MONGO': environment['OASIS_MONGO_REPLICA_SET_CONNECTION']},
                stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
            try:
                assert process.wait(timeout=120) == 0, 'Native ancestor controls failed; inspect fixed evidence.'
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGTERM)
                process.wait(timeout=30)
                raise
        report = RESULTS / 'mongo-ancestor-native.trx'
        assert report.stat().st_mtime >= started
        counts = ET.parse(report).find('.//{*}Counters').attrib
        assert counts['passed'] == '4' and counts['failed'] == '0' and counts['notExecuted'] == '0'
        print(json.dumps({'counts': counts, 'sha256': hashlib.sha256(report.read_bytes()).hexdigest()}), flush=True)
    print('Owned Mongo stopped; exact owned ancestor fixture database removed.', flush=True)


if __name__ == '__main__':
    main()
