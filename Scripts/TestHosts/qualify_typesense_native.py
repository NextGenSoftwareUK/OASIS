"""Cloud-only native Typesense CQL qualification; never adopts an existing source."""
import hashlib
import json
import os
from pathlib import Path
import secrets
import subprocess
import time
import urllib.error
import urllib.request
import uuid
import xml.etree.ElementTree as ET

from owned_docker_resources import missing_resource

IMAGE = 'typesense/typesense:29.0@sha256:316b7e71c21f7e5e5caa8daa150e1b3f2be8c876081ee1f77bc2d92cd7f137d0'
PROJECT = 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.IntegrationTests.csproj'
LABEL = 'oasis.typesense.qualification.owner'


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


def main():
    assert os.environ.get('GITHUB_ACTIONS') == 'true', 'Cloud-only qualification.'
    run, attempt = os.environ['GITHUB_RUN_ID'], os.environ['GITHUB_RUN_ATTEMPT']
    assert run.isdecimal() and attempt.isdecimal()
    owner = 'typesense-' + run + '-' + attempt
    container, volume = owner, owner + '-data'
    root = Path(__file__).resolve().parents[2]
    out = root / 'TestResults/TypesenseNativeCurrent'
    out.mkdir(parents=True, exist_ok=True)
    endpoint = 'http://127.0.0.1:58108'
    report = {'source': os.environ['GITHUB_SHA'], 'owner': owner, 'image': IMAGE,
              'nativePassed': False, 'restartPassed': False, 'hostedAccepted': False}

    def docker(*args, check=True, timeout=60, env=None):
        result = subprocess.run(['docker', *args], capture_output=True, text=True,
                                timeout=timeout, env=env)
        if check and result.returncode:
            raise RuntimeError('Owned Typesense Docker operation failed: ' + result.stderr[-1000:])
        return result

    def inspect(kind, name):
        result = docker(kind, 'inspect', name, check=False)
        if missing_resource(kind, name, result.returncode, result.stderr):
            return None
        if result.returncode:
            raise RuntimeError('Owned resource state could not be established.')
        return json.loads(result.stdout)[0]

    def owned(kind, name):
        state = inspect(kind, name)
        if state is not None:
            labels = state['Config']['Labels'] if kind == 'container' else state['Labels']
            assert labels.get(LABEL) == owner, 'Refuse unrelated resource.'
        return state

    assert inspect('container', container) is None
    assert inspect('volume', volume) is None
    with (out / 'intent.json').open('x') as stream:
        json.dump(report | {'container': container, 'volume': volume}, stream, indent=2)

    # Compile before allocating the native runtime. The caller holds the build lease.
    subprocess.run(['dotnet', 'build', PROJECT, '/m:2', '/nr:false',
                    '/p:UseSharedCompilation=false'], cwd=root, check=True)
    bootstrap = secrets.token_hex(32)
    print('::add-mask::' + bootstrap, flush=True)
    transport = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())

    def request(method, path, key=None, data=None, expected=200):
        headers = {'Content-Type': 'application/json'}
        if key:
            headers['X-TYPESENSE-API-KEY'] = key
        body = None if data is None else json.dumps(data).encode()
        req = urllib.request.Request(endpoint + path, data=body, headers=headers, method=method)
        try:
            with transport.open(req, timeout=10) as response:
                assert response.status == expected
                raw = response.read(1024 * 1024 + 1)
                assert len(raw) <= 1024 * 1024
                return json.loads(raw)
        except urllib.error.HTTPError as error:
            # Explicit denied-operation checks never become successful reads.
            if error.code == expected:
                error.close()
                return None
            raise

    def ready():
        deadline = time.monotonic() + 60
        while True:
            try:
                result = request('GET', '/health')
                if result == {'ok': True}:
                    return
                assert result == {'ok': False}, 'Unexpected native health response.'
            except urllib.error.HTTPError as error:
                if error.code != 503:
                    raise
                error.close()
            except urllib.error.URLError as error:
                if not isinstance(error.reason, ConnectionRefusedError):
                    raise
            if time.monotonic() >= deadline:
                raise TimeoutError('Owned Typesense startup deadline exceeded.')
            time.sleep(1)

    touched = False
    try:
        touched = True
        docker('volume', 'create', '--label', LABEL + '=' + owner, volume)
        env = dict(os.environ, TYPESENSE_API_KEY=bootstrap)
        docker('create', '--name', container, '--label', LABEL + '=' + owner,
               '--publish', '127.0.0.1:58108:8108', '--memory', '1g',
               '--mount', 'type=volume,source=' + volume + ',target=/data',
               '--env', 'TYPESENSE_API_KEY', IMAGE, '--data-dir', '/data', timeout=180, env=env)
        docker('start', container)
        ready()
        request('POST', '/collections', bootstrap, {'name': 'oasis_holons', 'fields': [
            {'name': 'name', 'type': 'string'}, {'name': 'deletedDate', 'type': 'string'},
            {'name': 'createdByAvatarId', 'type': 'string'}, {'name': 'isPublic', 'type': 'bool'}]}, expected=201)
        actor = str(uuid.uuid4())
        ids = [str(uuid.uuid4()) for _ in range(5)]
        for index, identity in enumerate(ids[:4]):
            request('POST', '/collections/oasis_holons/documents', bootstrap,
                    {'id': identity, 'name': 'Owned native teaching record ' + str(index),
                     'createdByAvatarId': actor, 'isPublic': True,
                     'deletedDate': '2026-11-08T00:00:00Z' if index == 3 else '0001-01-01T00:00:00Z'}, expected=201)
        access = request('POST', '/keys', bootstrap, {'description': 'Owned CQL read-only qualification',
            'actions': ['collections:get', 'documents:get'], 'collections': ['oasis_holons']}, expected=201)
        reader = access['value']
        print('::add-mask::' + reader, flush=True)
        # The actual CQL client receives no write-capable key.
        request('POST', '/collections/oasis_holons/documents', reader,
                {'id': ids[4], 'name': 'Must never be created'}, expected=401)
        source = out / 'source.json'
        source.write_text(json.dumps({'Endpoint': endpoint, 'Actor': actor,
            'LiveIds': ids[:3], 'DeletedId': ids[3], 'MissingId': ids[4]}), encoding='utf-8')
        environment = dict(os.environ, COSMIC_TYPESENSE_CQL_SOURCE=str(source), COSMIC_TYPESENSE_CQL_KEY=reader)
        for phase in ['native', 'restart']:
            if phase == 'restart':
                state = owned('container', container)
                assert state['State']['Running']
                docker('stop', '--time', '20', container)
                state = owned('container', container)
                assert not state['State']['Running'] and state['State']['ExitCode'] == 0
                docker('start', container)
                ready()
            subprocess.run(['dotnet', 'test', PROJECT, '--no-build', '--no-restore',
                '--filter', 'TestCategory=TypesenseCqlNative', '--results-directory', str(out),
                '--logger', 'trx;LogFileName=' + phase + '.trx'], cwd=root, env=environment, check=True)
            trx = out / (phase + '.trx')
            ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
            xml = ET.parse(trx)
            counts = xml.find('.//t:Counters', ns).attrib
            assert all(counts.get(k) == '1' for k in ['total', 'executed', 'passed'])
            assert counts.get('notExecuted') == '0'
            rows = xml.findall('.//t:UnitTestResult', ns)
            assert len(rows) == 1 and rows[0].attrib['outcome'] == 'Passed'
            assert rows[0].attrib['testName'] == 'ExistingNativeRecordsSupportThreeTwoPagingAndSourcePreservation'
            report[phase + 'Passed'] = True
            report[phase + 'TrxSha256'] = hashlib.sha256(trx.read_bytes()).hexdigest()
        report['seedCreates'] = 4
        report['restrictedReaderWriteDenied'] = True
    finally:
        if touched:
            state = owned('container', container)
            if state is not None:
                if state['State']['Running']:
                    docker('stop', '--time', '20', container)
                state = owned('container', container)
                report['finalExitCode'] = state['State']['ExitCode']
                assert not state['State']['Running'] and not state['State']['OOMKilled']
                docker('rm', '-v', container)
            if owned('volume', volume) is not None:
                docker('volume', 'rm', volume)
            assert inspect('container', container) is None and inspect('volume', volume) is None
            report['ownedCleanupVerified'] = True
        (out / 'qualification.json').write_text(json.dumps(report, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
