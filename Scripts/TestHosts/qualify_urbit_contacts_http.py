"""Real .NET authenticated HTTP reads of the owned synthetic native book."""
import json
import re
import subprocess
import shutil

from qualify_urbit_contacts_book import load_fixture, qualify_book
from qualify_urbit_contacts_install import main
from urbit_conn import thread


def native_http(context, phase):
    prefix = context['prefix']
    code_expression = '(slag 1 (scow %p .^(@p %j /(scot %p our.bowl)/code/(scot %da now.bowl)/(scot %p our.bowl))))'

    def small_atom(expression):
        # One chosen wire shape for bounded small integers, never aura/JSON guessing.
        reply = thread(context['binary'], context['connection'], prefix + '(pure:m !>(' + expression + '))')
        match = re.fullmatch(r'\[1 %avow 0 %noun ([0-9]{1,3})\]', reply.strip())
        assert match is not None, 'Native code character query did not return one bounded integer.'
        return int(match[1])

    # Match the pinned +code generator. Never persist native auth replies or code.
    length = small_atom('(lent ' + code_expression + ')')
    assert 8 <= length <= 128
    code = ''.join(chr(small_atom('(snag ' + str(i) + ' ' + code_expression + ')')) for i in range(length))
    assert re.fullmatch(r'[a-z-]{8,128}', code)
    root = context['root']
    host = root / 'Scripts/TestHosts/UrbitContactsHttpHost/bin/Release/net10.0/UrbitContactsHttpHost.dll'
    fixture = root / 'Providers/Other/TestProjects/NextGenSoftware.OASIS.API.Providers.UrbitOASIS.UnitTests/Fixtures/urbit-contacts-book-source.json'
    payload = dict(origin='http://127.0.0.1:80/', loginCode=code, fixturePath=str(fixture), phase=phase)
    dotnet = shutil.which('dotnet')
    assert dotnet is not None
    result = subprocess.run(context['http_command']([dotnet, str(host)]), input=json.dumps(payload), text=True,
                            capture_output=True, timeout=180)
    if result.returncode:
        # The fixture host emits fixed diagnostics only; it never echoes stdin,
        # login responses, request content, codes or cookies.
        assert len(result.stderr.encode()) <= 16384
        (context['out'] / (phase + '-http-error.txt')).write_text(result.stderr)
    result.check_returncode()
    assert len(result.stdout.encode()) <= 4096
    proof = json.loads(result.stdout)
    assert proof['phase'] == phase and proof['fullCount'] == 3 and proof['subsetCount'] == 2
    assert proof['anonymousDeniedStatus'] in (401, 403) and proof['nativeHttpClientQualified'] is True
    assert proof['cqlQualified'] is False and proof['liveDeployment'] is False
    assert re.fullmatch('[0-9a-f]{64}', proof['projectedSourceSha256'])
    assert proof['providerCqlQualified'] is True and proof['cqlOwnerFullCount'] == 3 and proof['cqlOwnerSubsetCount'] == 2
    assert proof['cqlForeignPrivateCount'] == 0 and proof['sharedManagerQualified'] is True
    assert proof['managerOwnerFullCount'] == 3 and proof['managerOwnerSubsetCount'] == 2
    assert proof['managerForeignPrivateCount'] == 0
    assert re.fullmatch('[0-9a-f]{64}', proof['privateCqlProjectionSha256'])
    (context['out'] / (phase + '-http.json')).write_text(json.dumps(proof, indent=2))
    return proof


if __name__ == '__main__':
    main(on_initialized=lambda context: qualify_book(context, http_reader=native_http),
         http_transport=True, fixture_loader=load_fixture)
