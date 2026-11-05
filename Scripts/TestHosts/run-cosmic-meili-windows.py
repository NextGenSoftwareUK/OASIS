"""Owned pinned Meilisearch fixture: journal writes once; preserve the retained source."""
import argparse
import datetime
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import secrets
import socket
import subprocess
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/RecoveryLaptopMeili1561'
RESULTS = ROOT / 'TestResults/MeilisearchCqlNativeCurrent'
DIGEST = 'ac6d2506cafe862c93a6add533e125a19852339ec21303413120bde84aad3a48'


def write_new(path, value):
    raw = value if isinstance(value, bytes) else json.dumps(value, indent=2).encode()
    with path.open('xb') as file:
        file.write(raw)
        file.flush()
        os.fsync(file.fileno())


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


def teaching_documents(source):
    return [{'id': item, 'name': 'Meilisearch CQL teaching ' + str(index),
        'description': 'Owned local native fixture', 'isPublic': False, 'isActive': True,
        'createdByAvatarId': source['Actor'], 'parentHolonId': source['Actor'],
        'deletedDate': '2026-11-05T00:00:00Z' if item == source['DeletedId'] else '0001-01-01T00:00:00'}
        for index, item in enumerate(source['LiveIds'] + [source['DeletedId']])]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--initialize', action='store_true')
    args = parser.parse_args()
    raw = (STATE / 'source.json').read_bytes()
    source = json.loads(raw)
    if source['Endpoint'] != 'http://127.0.0.1:17700':
        raise RuntimeError('The fixed owned loopback source is required.')
    executable = STATE / 'meilisearch-windows-amd64.exe'
    if hashlib.sha256(executable.read_bytes()).hexdigest() != DIGEST:
        raise RuntimeError('Pinned native runtime differs.')
    spec = importlib.util.spec_from_file_location('protection', ROOT / 'Scripts/Sandbox/Verify-HostedCqlDemo.py')
    protection = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(protection)
    source_hash = hashlib.sha256(raw).hexdigest()
    if args.initialize:
        if (STATE / 'data.ms').exists():
            raise RuntimeError('Existing native data requires reconciliation, never reseeding.')
        write_new(STATE / 'bootstrap.intent.json', {'sourceSha256': source_hash, 'source': source,
                  'runtimeSha256': DIGEST, 'maximumIndexCreates': 1, 'maximumDocumentBatches': 1})
        key = secrets.token_urlsafe(32)
        write_new(STATE / 'master-key.dpapi', protection.protected_token(key.encode()))
    else:
        ack = json.loads((STATE / 'bootstrap.acknowledged.json').read_bytes())
        if ack['sourceSha256'] != source_hash or not (STATE / 'data.ms').is_dir():
            raise RuntimeError('Matching retained acknowledged source required.')
        key = protection.protected_token((STATE / 'master-key.dpapi').read_bytes(), decrypt=True).decode()
    with socket.socket() as check:
        if check.connect_ex(('127.0.0.1', 17700)) == 0:
            raise RuntimeError('The native port is already owned by another process.')
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())

    def call(path, body=None, method='GET'):
        request = urllib.request.Request(source['Endpoint'] + path, method=method,
            data=None if body is None else json.dumps(body).encode(),
            headers={'Authorization': 'Bearer ' + key, 'Content-Type': 'application/json'})
        with opener.open(request, timeout=10) as response:
            payload = response.read(65537)
            if len(payload) > 65536:
                raise RuntimeError('Native setup response exceeds budget.')
            return json.loads(payload)

    def mutation(label, path, body):
        write_new(STATE / (label + '.intent.json'), {'sourceSha256': source_hash, 'method': 'POST',
                  'path': path, 'bodySha256': hashlib.sha256(json.dumps(body).encode()).hexdigest()})
        response = call(path, body, 'POST')  # Exactly one dispatch; never replay after uncertainty.
        write_new(STATE / (label + '.response.json'), response)
        task = response.get('taskUid')
        if type(task) is not int:
            raise RuntimeError('Native task acknowledgement required.')
        until = time.monotonic() + 60
        while True:
            observed = call('/tasks/' + str(task))
            if observed.get('uid') != task:
                raise RuntimeError('Observed native task identity differs.')
            if observed.get('status') == 'succeeded':
                write_new(STATE / (label + '.acknowledged.json'), observed)
                return
            if observed.get('status') == 'failed' or time.monotonic() >= until:
                write_new(STATE / (label + '.unsettled.json'), observed)
                raise RuntimeError('Native mutation did not settle successfully; reconcile original task.')
            time.sleep(.25)  # Poll the acknowledged asynchronous task; never resubmit a mutation.

    RESULTS.mkdir(parents=True, exist_ok=True)
    label = 'initial' if args.initialize else 'retained'
    write_new(STATE / (label + '-run.intent.json'), {'sourceSha256': source_hash, 'runtimeSha256': DIGEST})
    env = dict(os.environ, MEILI_MASTER_KEY=key, MEILI_NO_ANALYTICS='true')
    with (STATE / (label + '-server.log')).open('xb') as log:
        process = subprocess.Popen([str(executable), '--http-addr', '127.0.0.1:17700',
            '--db-path', str(STATE / 'data.ms'), '--no-analytics', '--max-indexing-memory', '256MiB',
            '--max-indexing-threads', '2'], cwd=STATE, env=env, stdout=log, stderr=log,
            creationflags=subprocess.CREATE_NO_WINDOW)
        try:
            until = time.monotonic() + 30
            while True:
                if process.poll() is not None:
                    raise RuntimeError('Owned native server exited before readiness; inspect retained log.')
                try:
                    health = call('/health')
                except urllib.error.HTTPError:
                    raise
                except urllib.error.URLError:
                    if time.monotonic() >= until:
                        raise RuntimeError('Owned native server did not become ready.') from None
                    time.sleep(.25)
                    continue
                if health != {'status': 'available'}:
                    raise RuntimeError('Unexpected native health response.')
                break
            version = call('/version')
            if version.get('pkgVersion') != '1.56.1':
                raise RuntimeError('Native server version differs from pinned runtime.')
            write_new(STATE / (label + '-identity.json'), version)
            if args.initialize:
                mutation('create-index', '/indexes', {'uid': 'oasis_holons', 'primaryKey': 'id'})
                documents = teaching_documents(source)
                mutation('create-documents', '/indexes/oasis_holons/documents', documents)
                write_new(STATE / 'bootstrap.acknowledged.json', {'sourceSha256': source_hash, 'saved': 4})
            expected = teaching_documents(source)
            original = json.loads((STATE / 'create-documents.intent.json').read_bytes())
            if original['bodySha256'] != hashlib.sha256(json.dumps(expected).encode()).hexdigest():
                raise RuntimeError('Reconstructed teaching payload differs from original dispatch intent.')
            def verify_original_source():
                for document in expected:
                    observed = call('/indexes/oasis_holons/documents/' + document['id'])
                    if observed != document:
                        raise RuntimeError('Retained native payload differs from original acknowledged batch.')
            verify_original_source()
            test_env = dict(env, COSMIC_MEILISEARCH_CQL_SOURCE=str(STATE / 'source.json'), COSMIC_MEILISEARCH_CQL_KEY=key)
            project = ROOT / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.IntegrationTests'
            completed = subprocess.run(['dotnet', 'test', str(project), '--no-build', '--no-restore',
                '--filter', 'TestCategory=MeilisearchCqlNative', '--logger', 'trx;LogFileName=' + label + '.trx',
                '--results-directory', str(RESULTS), '-v:q'], cwd=ROOT, env=test_env)
            counters = ET.parse(RESULTS / (label + '.trx')).find('.//{*}Counters').attrib
            write_new(STATE / (label + '-run.result.json'), {'testExitCode': completed.returncode,
                'sourceSha256': source_hash, 'seededThisRun': args.initialize, 'counters': counters})
            if completed.returncode or any(counters.get(name) != expected for name, expected in
                    {'total': '1', 'passed': '1', 'failed': '0', 'notExecuted': '0'}.items()):
                raise RuntimeError('Native CQL test failed; preserve original source and evidence.')
            verify_original_source()
            write_new(STATE / (label + '-original-source-verified.json'), {'sourceSha256': source_hash,
                'originalBatchSha256': original['bodySha256'], 'matchedDocuments': 4, 'seededThisRun': args.initialize})
        finally:
            if process.poll() is None:
                process.terminate()
            process.wait(timeout=15)
            write_new(STATE / (label + '-process-stopped.json'), {'pid': process.pid, 'exitCode': process.returncode,
                'method': 'owned Windows process termination after completed or failed run; not crash-durability proof'})
    print('Native Meilisearch run completed; source and original mutation receipts retained.')


if __name__ == '__main__':
    main()
