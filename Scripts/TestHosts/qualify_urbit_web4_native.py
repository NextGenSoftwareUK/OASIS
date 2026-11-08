"""Shipping MVC route -> actual SDK -> owned native book, across one restart."""
import json
import os
import re
import shutil
import subprocess
import xml.etree.ElementTree as ET

from qualify_urbit_contacts_book import load_fixture, qualify_book
from qualify_urbit_contacts_http import native_http
from qualify_urbit_contacts_install import main


def web4_phase(context, phase, code):
    dotnet = shutil.which('dotnet')
    assert dotnet is not None
    project = context['root'] / 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/NextGenSoftware.OASIS.API.Core.UnitTests.csproj'
    results = context['out'] / ('web4-' + phase)
    results.mkdir(exist_ok=False)
    command = [dotnet, 'test', str(project), '-c', 'Release', '--no-build', '--no-restore',
               '/m:2', '/nr:false', '/p:UseSharedCompilation=false',
               '--filter', 'FullyQualifiedName~UrbitNativeWeb4MvcReadsOwnedBook',
               '--results-directory', str(results), '--logger', 'trx;LogFileName=web4.trx']
    payload = dict(origin='http://127.0.0.1:80/', phase=phase, loginCode=code)
    # Named input survives sudo only for this child. Never put values in arguments,
    # reports, test parameters or the persistent runner environment.
    environment = dict(os.environ, COSMIC_URBIT_NATIVE_WEB4_INPUT=json.dumps(payload))
    result = subprocess.run(context['http_command'](command, native_web4_input=True),
                            env=environment, capture_output=True, text=True, timeout=180)
    assert code not in result.stdout and code not in result.stderr, 'Private diagnostic rejected; no output retained.'
    assert len(result.stdout.encode()) <= 65536 and len(result.stderr.encode()) <= 16384
    if result.returncode:
        (context['out'] / (phase + '-web4-error.txt')).write_text(result.stdout + result.stderr)
    result.check_returncode()
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    root = ET.parse(results / 'web4.trx').getroot()
    counters = root.find('.//t:Counters', ns).attrib
    assert counters['total'] == counters['executed'] == counters['passed'] == '1'
    tests = root.findall('.//t:UnitTestResult', ns)
    assert len(tests) == 1 and tests[0].attrib['outcome'] == 'Passed'
    assert 'UrbitNativeWeb4MvcReadsOwnedBook' in tests[0].attrib['testName']
    proof = json.loads((context['out'] / (phase + '-web4.json')).read_text())
    for key, expected in dict(phase=phase, nativeWeb4MvcQualified=True,
                              ownerFullCount=3, ownerSubsetCount=2,
                              anonymousStatus=401, forgedFieldStatus=400, foreignStatus=403, inactiveStatus=503,
                              nativePointReadCount=6, successfulQueryReceipts=2, rejectedQueryReceipts=0,
                              controlledIdentityAndOutbox=True, productionJwtQualified=False,
                              durableBillingQualified=False, cqlQualified=False, liveDeployment=False).items():
        assert proof[key] == expected, key
    assert re.fullmatch('[0-9a-f]{64}', proof['privateApiProjectionSha256'])
    return dict(nativeWeb4MvcQualified=True, privateApiProjectionSha256=proof['privateApiProjectionSha256'])


if __name__ == '__main__':
    main(on_initialized=lambda context: qualify_book(context,
         http_reader=lambda current, phase: native_http(current, phase, web4_reader=web4_phase)),
         http_transport=True, fixture_loader=load_fixture)
