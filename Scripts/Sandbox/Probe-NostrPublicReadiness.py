"""Capture bounded anonymous relay reads; cryptographic/CQL qualification remains separate.

One discovery and exact three/two event-ID subscriptions, never publish/auth/retry.
Original intent and every completed raw outcome are retained before interpretation.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]
RELAY = "wss://relay.damus.io/"
HEX = re.compile(r"[0-9a-f]{64}")


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property.")
        result[key] = value
    return result


def events(raw, subscription, expected=None):
    messages = json.loads(raw.decode('utf-8-sig'), object_pairs_hook=unique_object)
    if not isinstance(messages, list) or not 1 <= len(messages) <= 8:
        raise ValueError("A bounded completed event selection is required.")
    rows = []
    for text in messages:
        frame = json.loads(text, object_pairs_hook=unique_object)
        if not isinstance(frame, list) or len(frame) != 3 or frame[:2] != ['EVENT', subscription]:
            raise ValueError("Unexpected event envelope.")
        row = frame[2]
        if (not isinstance(row, dict) or not isinstance(row.get('id'), str) or not HEX.fullmatch(row['id'])
                or not isinstance(row.get('pubkey'), str) or not HEX.fullmatch(row['pubkey'])
                or type(row.get('kind')) is not int or row['kind'] != 1
                or type(row.get('created_at')) is not int or not 0 <= row['created_at'] <= 2**53 - 1
                or not isinstance(row.get('sig'), str) or not re.fullmatch(r'[0-9a-f]{128}', row['sig'])
                or not isinstance(row.get('content'), str) or len(row['content'].encode('utf-8')) > 32768
                or not isinstance(row.get('tags'), list)):
            raise ValueError("Bounded kind-1 event structure required; signature validation still pending.")
        if any(not isinstance(tag, list) or not tag or any(not isinstance(v, str) for v in tag) for tag in row['tags']):
            raise ValueError("String-array tags required.")
        rows.append(row)
    ids = [row['id'] for row in rows]
    if len(set(ids)) != len(ids) or expected is not None and set(ids) != set(expected):
        raise ValueError("Complete unique exact event-ID selection required.")
    return rows


def write_new(path, raw):
    with path.open('xb') as stream:
        stream.write(raw)
        stream.flush()
        os.fsync(stream.fileno())


def main():
    state = ROOT / 'LocalState/COSMIC/HostedNostrPublicSourceCurrent'
    results = ROOT / 'TestResults/NostrPublicSourceCurrent'
    state.mkdir(exist_ok=True)
    results.mkdir(exist_ok=True)
    write_new(state / 'native-availability.intent.json', json.dumps({
        'relay': RELAY, 'discovery': {'kinds': [1], 'limit': 3},
        'sourceWrites': 0, 'retryAllowed': False, 'cryptoQualified': False}).encode())
    report = {'relay': RELAY, 'availabilityPassed': False, 'cryptoQualified': False,
              'cqlAcceptance': False, 'sourceWrites': 0, 'calls': []}
    def read(selection, expected=None):
        number = len(report['calls']) + 1
        subscription = 'cosmic-' + uuid.uuid4().hex[:16]
        request = {'relay': RELAY, 'subscription': subscription, 'filter': selection}
        outcome = subprocess.run(['pwsh', '-NoProfile', '-File', str(Path(__file__).with_name('Read-NostrPublicEvents.ps1'))],
                                 input=json.dumps(request).encode(), capture_output=True, timeout=45)
        # Preserve failure output too. Nothing is displayed as trusted profile text.
        raw = outcome.stdout
        write_new(results / (str(number) + '-events.json'), raw)
        write_new(results / (str(number) + '-transport.stderr.txt'), outcome.stderr)
        entry = {'filter': selection, 'subscription': subscription, 'exitCode': outcome.returncode,
                 'bytes': len(raw), 'sha256': hashlib.sha256(raw).hexdigest()}
        report['calls'].append(entry)
        if outcome.returncode:
            raise RuntimeError('Relay transport failed; raw outcome retained, no retry.')
        return events(raw, subscription, expected)
    try:
        discovered = read({'kinds': [1], 'limit': 3})
        if len(discovered) != 3:
            raise ValueError('Three discovery events required.')
        ids = [row['id'] for row in discovered]
        for count in (3, 2):
            read({'ids': ids[:count], 'kinds': [1], 'limit': count}, ids[:count])
        report['availabilityPassed'] = True
        report['eventIds'] = ids
        # No source.json/owner is minted until signatures and projection are qualified.
    except Exception as error:
        report['failureType'] = type(error).__name__
    write_new(results / 'availability.json', (json.dumps(report, indent=2) + '\n').encode())
    write_new(state / 'native-availability.observed.json', (json.dumps(report, indent=2) + '\n').encode())
    print(json.dumps({key: report[key] for key in ['availabilityPassed', 'cryptoQualified', 'cqlAcceptance', 'sourceWrites']}))
    if not report['availabilityPassed']:
        raise SystemExit('Original outcome retained; reconcile, never automatically retry or change relay.')


if __name__ == '__main__':
    main()
