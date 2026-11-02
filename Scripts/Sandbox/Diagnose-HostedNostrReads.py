"""One bounded same-owner read diagnostic after the exact safe-logging release.

Never prepares copies, writes holons, replaces an avatar or repeats a failed call.
Every response is captured before interpretation. Raw service logs stay in memory.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]


def module(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + '.py'))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


def safe_operator_events(raw):
    pattern = re.compile(r'CQL native failure provider=NostrOASIS phase=(query|activation) '
        r'stage=(connect|subscription|event_verification|unclassified) category=([a-z_/]{1,160}) '
        r'httpStatus=(\(null\)|[0-9]{3}) websocket=([A-Za-z]{1,64})')
    allowed = {'http', 'websocket', 'connection_refused', 'cancelled', 'invalid_data',
               'invalid_json', 'invalid_utf8', 'other', 'none'}
    socket_codes = {'none', 'unknown', 'Success', 'InvalidMessageType', 'Faulted', 'NativeError',
                    'NotAWebSocket', 'UnsupportedVersion', 'UnsupportedProtocol', 'HeaderError',
                    'ConnectionClosedPrematurely', 'InvalidState'}
    output = []
    for row in pattern.finditer(raw.decode('utf-8', errors='replace')):
        phase, stage, categories, status, socket = row.groups()
        if (any(value not in allowed for value in categories.split('/')) or socket not in socket_codes
                or status != '(null)' and not 100 <= int(status) <= 599):
            continue
        output.append({'phase': phase, 'stage': stage, 'categories': categories,
                       'httpStatus': None if status == '(null)' else int(status), 'webSocketError': socket})
    return output


def validate_request(native, source):
    if [len(row['result']['rows']) for row in native['native']] != [3, 2]:
        raise ValueError('Original complete native proof required.')
    request = next(row['request'] for row in native['source']['examples'] if row['id'] == 'public-subset')
    expected = [{'metadataPath': ['NostrEventId'], 'scalar': 'String', 'operator': 'In', 'value': source['eventIds'][:2]}]
    if (set(request) != {'provider', 'filters', 'orderBy', 'take', 'maximumDurationMilliseconds', 'includeMetadata'}
            or request.get('provider') != 'NostrOASIS' or request.get('filters') != expected
            or request.get('orderBy') != 'None' or request.get('take') != 25
            or request.get('maximumDurationMilliseconds') != 30000 or request.get('includeMetadata') is not True):
        raise ValueError('Exact original bounded public two-ID request required.')
    return request


def main():
    host = module('Publish-HostedSolidConfiguration')
    verify = module('Verify-HostedQuerySource')
    release = json.loads((ROOT / 'LocalState/COSMIC/HostedNostrReleaseCurrent/diagnostics-code-publication.acknowledged.json').read_text(encoding='utf-8'))
    deployed = subprocess.run([str(host.CLI), 'deployment', 'list', *host.COMMON, '--limit', '1', '--json'],
                              capture_output=True, check=True, timeout=45)
    latest = json.loads(deployed.stdout)[0]
    if (latest['id'] != release['deploymentId'] or latest['status'] != 'SUCCESS'
            or latest.get('meta', {}).get('commitHash') != release['commit']):
        raise RuntimeError('Wait for the exact safe-diagnostics release; no request dispatched.')
    state = ROOT / 'LocalState/COSMIC/HostedQueryAcceptance-NostrOASIS'
    results = ROOT / 'TestResults/NostrPublicSourceCurrent'
    source_path = ROOT / 'LocalState/COSMIC/HostedNostrPublicSourceCurrent/source.json'
    source_raw = source_path.read_bytes()
    qualified = json.loads(source_path.with_name('native-qualified.acknowledged.json').read_text(encoding='utf-8'))
    if qualified['sourceSha256'] != hashlib.sha256(source_raw).hexdigest():
        raise RuntimeError('Original native source changed.')
    native = json.loads((ROOT / 'TestResults/HostedCqlIntegrationCurrent/NostrOASIS-native-http.json').read_text(encoding='utf-8'))
    request = validate_request(native, json.loads(source_raw))
    owner = json.loads((state / 'owner.acknowledged.json').read_text(encoding='utf-8'))
    acks = [json.loads(line) for line in (state / 'save-acknowledgements.jsonl').read_text(encoding='utf-8').splitlines()]
    progress_raw = (ROOT / 'TestResults/HostedCqlIntegrationCurrent/NostrOASIS-client-http-flow.json.progress.json').read_bytes()
    progress = json.loads(progress_raw)['progress']
    if (owner['id'] != native['avatar']['id'] or len(acks) != 2
            or any(row['ownerId'] != owner['id'] or row['version'] != 1 or row['isSaved'] is not True for row in acks)
            or len({row['id'] for row in acks}) != 2 or progress['preparation']['saved'] != 2
            or len(progress['five']['result']['rows']) != 5 or len(progress['four']['result']['rows']) != 4
            or json.loads((state / 'preparation-intents.json').read_text(encoding='utf-8')) != {}):
        raise RuntimeError('Original acknowledged owner/copies and no-pending-save proof required.')
    operations = [str(uuid.uuid4()) for _ in range(8)]
    verify.write_new(state / 'bounded-native-diagnostic.intent.json', (json.dumps({
        'schema': 1, 'deploymentId': release['deploymentId'], 'commit': release['commit'], 'ownerId': owner['id'],
        'sourceSha256': qualified['sourceSha256'], 'maximumCalls': 8, 'operationIds': operations,
        'originalProgressSha256': hashlib.sha256(progress_raw).hexdigest(),
        'request': request, 'additionalSaves': 0, 'stopAtFirstFailure': True, 'replayAllowed': False}, indent=2) + '\n').encode())
    acknowledged, token = verify.session(state, 'owner', allow_create=False)
    if acknowledged['id'] != owner['id']:
        raise RuntimeError('Owner changed; no request dispatched.')
    observations = []
    for number, operation in enumerate(operations, 1):
        path = results / ('diagnostic-read-' + str(number) + '.json')
        try:
            response = verify.call('data/query-cql', token, request, operation=operation, evidence_path=path)
            rows = response['result']['rows']
            if (len(rows) != 2 or {row['metadata']['NostrEventId'] for row in rows} != set(request['filters'][0]['value'])
                    or any(row['isPublic'] is not True for row in rows)):
                raise ValueError('Native response selection differs.')
            observations.append({'number': number, 'status': 200, 'rows': 2})
        except Exception as error:
            status = json.loads(path.read_text(encoding='utf-8'))['status'] if path.exists() else None
            observations.append({'number': number, 'status': status, 'failureType': type(error).__name__})
            break  # No retry of the failed read and no additional provider selected.
    logs = subprocess.run([str(host.CLI), 'logs', release['deploymentId'], *host.COMMON, '--lines', '200'],
                          capture_output=True, timeout=45)
    proof = {'schema': 1, 'deploymentId': release['deploymentId'], 'observations': observations,
             'safeOperatorEvents': safe_operator_events(logs.stdout), 'logObservationExitCode': logs.returncode,
             'rawLogSha256': hashlib.sha256(logs.stdout).hexdigest(), 'additionalSaves': 0, 'ownerUnchanged': True}
    raw = (json.dumps(proof, indent=2) + '\n').encode()
    verify.write_new(results / 'bounded-native-diagnostic.json', raw)
    verify.write_new(state / 'bounded-native-diagnostic.observed.json', raw)
    print(json.dumps(proof))


if __name__ == '__main__':
    main()
