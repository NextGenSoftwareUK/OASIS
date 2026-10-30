"""Owned dev WEB4 fixture and real CQL/replication/isolation verification. No automatic write replay."""
from pathlib import Path
import argparse
import datetime as dt
import json
import os
import re
import subprocess
import shutil
import time
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/HostedSandboxCqlCurrent'
RESULTS = ROOT / 'TestResults/Web4SandboxCqlCurrent'
BASE = 'https://dev.api.web4.oasisomniverse.one/api/'
PROVIDERS = ['MongoDBOASIS', 'SQLLiteDBOASIS', 'Neo4jOASIS']


def write(path, value):
    # Preserve the last complete intent if writing the replacement is interrupted.
    temporary = path.with_suffix(path.suffix + '.new')
    with temporary.open('w', encoding='utf-8') as stream:
        stream.write(json.dumps(value, indent=2) + '\n')
        stream.flush()
        os.fsync(stream.fileno())
    temporary.replace(path)


def unwrap(data):
    inner = data.get('result')
    return inner if isinstance(inner, dict) and 'isError' in inner else data


def require_new_fixture(path):
    if path.exists() or path.with_suffix(path.suffix + '.new').exists():
        raise RuntimeError('Retained fixture or interrupted journal exists. Reconcile original outcomes; never reseed/replay.')


def outcome(path, status, key, operation, data):
    return {'path': path, 'status': status, 'requestKey': key,
            'operationId': operation or data.get('operationId'),
            'errorCode': data.get('errorCode') or data.get('code'),
            'isError': status != 200 or data.get('isError', False)}


def acknowledged_identity(path, data):
    result = data.get('result') or {}
    identity = result.get('id')
    valid = (path == 'sandbox/session' and bool(result.get('jwtToken'))) or (
        path == 'data/save-holon' and data.get('isSaved') is True)
    if not valid or not identity:
        raise RuntimeError('Mutation has no complete acknowledgement. Preserve its intent; never replay.')
    uuid.UUID(identity)
    return identity  # Only the identity is journalled; never the returned session token.


def meta(key, value):
    return {'metadataPath': [key], 'scalar': 'String', 'operator': 'Equal', 'value': value}


def query_cases(tag):
    # Five queries per provider, plus federation and three saves:19 owner calls.
    # AND/OR shares the metadata case instead of consuming another free-quota slot.
    own = [meta('demoTag', tag)]
    return [
        ('metadata-and-or', own, ['Espresso', 'Flat White'], {'any_of': [meta('category', 'coffee'),
            {'field': 'Name', 'scalar': 'String', 'operator': 'Equal', 'value': 'No matching name'}]}),
        ('name', own + [{'field': 'Name', 'scalar': 'String', 'operator': 'Equal', 'value': 'Espresso'}], ['Espresso'], {}),
        ('description', own + [{'field': 'Description', 'scalar': 'String', 'operator': 'Contains', 'value': 'coffee'}], ['Espresso', 'Flat White'], {}),
        ('paging', own, ['Flat White'], {'skip': 1, 'take': 1}),
        ('count', own + [meta('category', 'coffee')], '2', {'scalar': True})
    ]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--deployment', required=True, type=lambda value: str(uuid.UUID(value)))
    parser.add_argument('--commit', required=True)
    options = parser.parse_args()
    if not re.fullmatch('[0-9a-f]{40}', options.commit):
        parser.error('--commit must be the exact full published SHA')
    deployment_id, commit = options.deployment, options.commit
    journal_path = STATE / 'fixture.json'
    require_new_fixture(journal_path)  # Stop before even querying hosted state if an intent exists.
    cli = shutil.which('railway.exe') or shutil.which('railway.cmd') or shutil.which('railway')
    assert cli, 'Reuse the installed Railway CLI; do not install another environment.'
    deployments = json.loads(subprocess.check_output([cli, 'deployment', 'list', '-p',
        '712f50f6-3b7b-46d4-b229-44b49922f16b', '-s', 'WEB4 OASIS API', '-e', 'dev', '--limit', '10', '--json'], text=True))
    deployment = next(item for item in deployments if item['id'] == deployment_id)
    assert deployment['status'] == 'SUCCESS' and deployment['meta']['commitHash'] == commit, 'Wait for the exact verified deployment; do not create a replacement.'
    STATE.mkdir(parents=True, exist_ok=True)
    RESULTS.mkdir(parents=True, exist_ok=True)
    journal = {'deployment': deployment_id, 'commit': commit, 'createdUtc': dt.datetime.now(dt.timezone.utc).isoformat(),
               'tag': uuid.uuid4().hex, 'avatars': [], 'records': [], 'acknowledgements': [], 'pending': None, 'completed': False}
    report = {'deployment': deployment_id, 'steps': [], 'failures': []}
    write(journal_path, journal)

    def call(path, method='POST', body=None, token=None, mutation=False):
        key = (body or {}).get('operationId') or str(uuid.uuid4())
        headers = {'Accept': 'application/json', 'Idempotency-Key': key}
        if token:
            headers['Authorization'] = 'Bearer ' + token
        if body is not None:
            headers['Content-Type'] = 'application/json'
        journal['pending'] = {'path': path, 'requestKey': key, 'mutation': mutation, 'body': body}
        write(journal_path, journal)  # Intent outside disposable TestResults; never includes a bearer.
        request = urllib.request.Request(BASE + path, method=method, headers=headers,
            data=json.dumps(body).encode('utf-8') if body is not None else None)
        try:
            response = urllib.request.urlopen(request, timeout=30)
        except urllib.error.HTTPError as error:
            response = error  # A definitive HTTP error is evidence; never a success or retry.
        with response:
            status = response.status
            operation = response.headers.get('X-OASIS-Operation-Id')
            data = unwrap(json.load(response))
        recorded = outcome(path, status, key, operation, data)
        report['steps'].append(recorded)
        write(RESULTS / 'hosted-demo-verification.json', report)
        if mutation and (status != 200 or data.get('isError')):
            journal['pending']['response'] = recorded
            write(journal_path, journal)
            raise AssertionError('Mutation did not acknowledge success. Preserve its original intent and outcome; do not replay.')
        if mutation:
            # Persist the returned identity in the same journal replacement that clears
            # the pending intent, before the caller can inspect/use the acknowledgement.
            journal['pending']['response'] = recorded
            write(journal_path, journal)
            identity = acknowledged_identity(path, data)
            journal['acknowledgements'].append({**recorded, 'id': identity})
        journal['pending'] = None
        write(journal_path, journal)
        return status, data

    def session(label):
        status, result = call('sandbox/session', mutation=True)
        avatar = result.get('result', {})
        assert status == 200 and avatar.get('jwtToken'), 'Session creation failed; inspect retained outcome.'
        token = avatar['jwtToken']
        journal['avatars'].append({'label': label, 'id': avatar['id'], 'username': avatar['username']})
        write(journal_path, journal)
        status, result = call('subscription/subscriptions/me', 'GET', token=token)
        subscriptions = result.get('result', [])
        assert status == 200 and len(subscriptions) == 1 and subscriptions[0]['planId'] == 'free'
        assert not subscriptions[0].get('payAsYouGoEnabled'), 'Only free demo identities may be used.'
        journal['avatars'][-1]['expiresUtc'] = subscriptions[0].get('currentPeriodEnd')
        write(journal_path, journal)
        return token

    actor = session('owner')
    tag = journal['tag']
    fixture = [('Espresso', 'Strong coffee', 'coffee'), ('Flat White', 'Milky coffee', 'coffee'), ('Tea', 'Black tea', 'tea')]
    for name, description, category in fixture:
        intent = {'operationId': str(uuid.uuid4()), 'providerType': 'MongoDBOASIS', 'saveChildren': False,
            'autoLoadBalanceMode': 'OFF', 'autoFailOverMode': 'OFF', 'autoReplicationMode': 'ON',
            'waitForAutoReplicationResult': True, 'holon': {'name': name, 'description': description, 'isPublic': False,
                'metaData': {'category': category, 'demoTag': tag}}}
        status, result = call('data/save-holon', body=intent, token=actor, mutation=True)
        saved = result.get('result', {})
        assert result.get('isSaved') and saved.get('id'), 'Missing acknowledged saved identity; preserve journal.'
        journal['records'].append({'id': saved['id'], 'name': name, 'category': category, 'operationId': intent['operationId']})
        write(journal_path, journal)

    # Bounded read-only observation of the existing asynchronous replication contract.
    deadline = time.monotonic() + 45
    while True:
        missing = []
        for provider in PROVIDERS:
            for record in journal['records']:
                status, result = call('data/load-holon', body={'id': record['id'], 'providerType': provider,
                    'autoFailOverMode': 'OFF', 'autoLoadBalanceMode': 'OFF', 'loadChildren': False}, token=actor)
                if result.get('errorCode') == 'OASIS_RECORD_NOT_FOUND' or (status == 200 and not result.get('isError') and result.get('result') is None):
                    missing.append((provider, record['id']))
                    continue
                assert status == 200 and not result.get('isError'), 'Replication read failed; no alternate provider selected.'
                assert result['result']['id'] == record['id'] and result['result']['name'] == record['name']
                assert result['result']['createdByAvatarId'] == journal['avatars'][0]['id']
        if not missing:
            break
        assert time.monotonic() < deadline, 'Replication did not produce all exact copies within the observation deadline.'
        time.sleep(1)

    def check(label, token, provider, filters, expected, *, any_of=None, skip=0, take=10, scalar=False):
        body = {'provider': provider, 'filters': filters, 'orderBy': 'Name', 'skip': skip}
        path = 'data/query-cql/scalar' if scalar else 'data/query-cql'
        body.update({'operation': 'LongCount'} if scalar else {'take': take, 'includeMetadata': True})
        if any_of:
            body['anyOf'] = any_of
        status, result = call(path, body=body, token=token)
        payload = result.get('result') or {}
        actual = str(payload.get('value')) if scalar else sorted(row['name'] for row in payload.get('rows', []))
        if status != 200 or result.get('isError') or actual != expected:
            report['failures'].append({'label': label, 'provider': provider, 'status': status,
                'errorCode': result.get('errorCode'), 'expected': expected, 'actual': actual})

    own = [meta('demoTag', tag)]
    for provider in PROVIDERS:
        for label, filters, expected, arguments in query_cases(tag):
            check(label, actor, provider, filters, expected, **arguments)

    status, result = call('data/query-cql/federation', body={'scope': 'Named', 'providers': PROVIDERS,
        'source': {'filters': own + [meta('category', 'coffee')], 'orderBy': 'Name', 'take': 10},
        'identityMode': 'PreserveCopies', 'take': 10, 'maximumProviderAttempts': 3}, token=actor)
    rows = (result.get('result') or {}).get('rows', [])
    counts = {provider: sum(row['selected']['provider'] == provider for row in rows) for provider in PROVIDERS}
    if status != 200 or result.get('isError') or counts != {provider: 2 for provider in PROVIDERS}:
        report['failures'].append({'label': 'federation', 'status': status, 'errorCode': result.get('errorCode'), 'providerCounts': counts})
    visitor = session('other-visitor')
    for provider in PROVIDERS:
        check('visitor-isolation', visitor, provider, own, [])
    write(RESULTS / 'hosted-demo-verification.json', report)
    journal['completed'] = not report['failures']
    journal['finishedUtc'] = dt.datetime.now(dt.timezone.utc).isoformat()
    write(journal_path, journal)
    print(json.dumps({'avatars': journal['avatars'], 'records': journal['records'], 'steps': len(report['steps']), 'failures': report['failures']}, indent=2))
    assert not report['failures'], 'Hosted acceptance incomplete; preserve original fixture and fix the failing layer.'


if __name__ == '__main__':
    main()
