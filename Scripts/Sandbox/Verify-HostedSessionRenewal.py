"""Owned normal-visitor cookie/rotation/revocation proof; never replays an intent."""
import argparse
import base64
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import urllib.request
from http.cookies import SimpleCookie

ROOT = Path(__file__).resolve().parents[2]
def load(name):
    spec = importlib.util.spec_from_file_location(name.replace('-', '_'), Path(__file__).with_name(name + '.py'))
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module); return module


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--expected-commit', required=True)
    parser.add_argument('--deployment', required=True)
    parser.add_argument('--reconcile-insecure-issuance', action='store_true',
                        help='Explicitly rotate the original sealed insecure-cookie issuance; never create another session.')
    options = parser.parse_args()
    configuration = load('Publish-HostedSolidConfiguration')
    helper = load('Verify-HostedQuerySource')
    observed = subprocess.run([str(configuration.CLI), 'deployment', 'list', *configuration.COMMON,
                               '--json', '--limit', '1'], capture_output=True, timeout=35)
    if observed.returncode:
        raise RuntimeError('Deployment observation failed; no session created.')
    row = json.loads(observed.stdout)[0]
    if (row['id'] != options.deployment or row['status'] != 'SUCCESS'
            or row.get('meta', {}).get('commitHash') != options.expected_commit):
        raise RuntimeError('Wait for the exact verified deployment; no session created or replacement job sent.')
    state = ROOT / 'LocalState/COSMIC/SandboxSessionRenewalCurrent'
    results = ROOT / 'TestResults/SandboxSessionRenewalCurrent'
    if (state / 'flow.acknowledged.json').exists():
        raise RuntimeError('Completed lifecycle proof must not be replayed.')
    if (state / 'flow.intent.json').exists() != options.reconcile_insecure_issuance:
        raise RuntimeError('Existing issuance requires explicit reconciliation; fresh issuance cannot use reconciliation.')
    preflights = []
    for origin, trusted in [(helper.BROWSER_ORIGIN, True), ('https://session-proof.invalid', False)]:
        request = urllib.request.Request(helper.BASE + 'sandbox/session', method='OPTIONS', headers={
            'Origin': origin, 'Access-Control-Request-Method': 'POST',
            'Access-Control-Request-Headers': 'authorization,content-type,idempotency-key'})
        with urllib.request.build_opener(helper.NoRedirect()).open(request, timeout=20) as response:
            assert response.status == 204
            assert response.headers.get('Access-Control-Allow-Origin') == (origin if trusted else '*')
            assert response.headers.get('Access-Control-Allow-Credentials') == ('true' if trusted else None)
            assert 'origin' in response.headers.get('Vary', '').lower()
            preflights.append({'origin': origin, 'trustedCookies': trusted, 'status': response.status})
    state.mkdir(parents=True, exist_ok=True)
    if options.reconcile_insecure_issuance:
        # Only the precisely diagnosed failed issuance can enter this path. The
        # original cookie is never represented as a successful secure issuance.
        if (state / 'owner.acknowledged.json').exists() or (state / 'owner.session.dpapi').exists():
            raise RuntimeError('Issuance reconciliation already progressed; inspect original rotation outcome.')
        helper.session_credentials.no_pending_rotation(state, 'owner')
        captured = json.loads(helper.verification.protected_token(
            (state / 'owner.auth-response.dpapi').read_bytes(), decrypt=True))
        if captured['status'] != 200:
            raise RuntimeError('Original issuance was not acknowledged by the server.')
        result = helper.verification.unwrap(json.loads(base64.b64decode(captured['bodyBase64'])))
        identity = helper.verification.acknowledged_identity('sandbox/session', result)
        issued = result['result']
        cookies = []
        for header in captured['cookies']:
            jar = SimpleCookie(); jar.load(header)
            if 'refreshToken' in jar:
                cookie = jar['refreshToken']
                if (not cookie.value or cookie['secure'] or not cookie['httponly']
                        or cookie['path'] != '/' or cookie['samesite'].lower() != 'lax'
                        or cookie['domain']):
                    raise RuntimeError('Original cookie differs from the diagnosed missing-Secure outcome.')
                cookies.append('refreshToken=' + cookie.coded_value)
        if len(cookies) != 1:
            raise RuntimeError('Original issuance has no unique refresh credential.')
        credentials = helper.session_credentials.validate({'schema': 1, 'ownerId': identity,
            'jwtToken': issued['jwtToken'], 'cookieHeader': cookies[0]}, identity)
        avatar = {'id': identity}
        helper.write_new(state / 'issuance-reconciliation.intent.json', json.dumps({
            'ownerId': identity, 'deployment': options.deployment,
            'originalSecure': False, 'action': 'normal rotation, no replacement avatar'}).encode())
        initial_faults = None
    else:
        helper.write_new(state / 'flow.intent.json', json.dumps({'commit': options.expected_commit,
            'deployment': options.deployment, 'scope': 'one owned avatar; no provider saves'}).encode())
        avatar, token = helper.session(state, 'owner')
        credential_path = state / 'owner.session.dpapi'
        credentials = helper.session_credentials.validate(json.loads(helper.verification.protected_token(
            credential_path.read_bytes(), decrypt=True)), avatar['id'])
        initial_faults = helper.call('sandbox/faults', token, method='GET')
    renewed = helper.session_credentials.rotate(state, 'owner', credentials, helper.call,
        helper.verification.protected_token, helper.write_new)
    assert renewed['ownerId'] == avatar['id'] and renewed['cookieHeader'] != credentials['cookieHeader']
    renewed_faults = helper.call('sandbox/faults', renewed['jwtToken'], method='GET')
    if initial_faults is not None:
        assert renewed_faults['result'] == initial_faults['result']
    else:
        repeated_faults = helper.call('sandbox/faults', renewed['jwtToken'], method='GET')
        assert renewed_faults['result'] == repeated_faults['result']
        helper.write_new(state / 'issuance-reconciliation.acknowledged.json', json.dumps({
            'ownerId': avatar['id'], 'sameAvatarRenewed': True, 'newCookieSecure': True}).encode())
    helper.write_new(state / 'revoke.intent.json', json.dumps({'ownerId': avatar['id'],
        'cookieHeaderSha256': hashlib.sha256(renewed['cookieHeader'].encode()).hexdigest()}).encode())
    revoked = []
    def capture_revoke(status, raw, cookies):
        helper.write_new(state / 'revoke.response.dpapi', helper.verification.protected_token(json.dumps({
            'status': status, 'bodyBase64': base64.b64encode(raw).decode(), 'cookies': cookies}).encode()))
        revoked.append((status, cookies))
    helper.call('avatar/revoke-token', renewed['jwtToken'], body={},
                cookie_header=renewed['cookieHeader'], capture_auth=capture_revoke)
    assert len(revoked) == 1 and revoked[0][0] == 200
    deleted = []
    for header in revoked[0][1]:
        jar = SimpleCookie(); jar.load(header)
        if 'refreshToken' in jar:
            cookie = jar['refreshToken']; deleted.append(cookie)
    assert len(deleted) == 1 and not deleted[0].value and deleted[0]['path'] == '/'
    assert deleted[0]['httponly'] and deleted[0]['secure']
    helper.write_new(state / 'revoke.acknowledged.json', json.dumps({'ownerId': avatar['id'],
        'normalRevokeSucceeded': True, 'cookieCleared': True}).encode())
    rejected = []
    def capture_rejected(status, raw, cookies):
        helper.write_new(state / 'revoked-refresh.response.dpapi', helper.verification.protected_token(json.dumps({
            'status': status, 'bodyBase64': base64.b64encode(raw).decode(), 'cookies': cookies}).encode()))
        rejected.append((status, raw))
    try:
        helper.call('avatar/refresh-token', body={}, cookie_header=renewed['cookieHeader'], capture_auth=capture_rejected)
    except RuntimeError as rejection:
        if not rejected:
            raise RuntimeError("Refresh failed before an authoritative rejection was captured.") from rejection
    else:
        raise RuntimeError('Revoked refresh credential was accepted.')
    assert len(rejected) == 1 and rejected[0][0] < 500
    denied = helper.verification.unwrap(json.loads(rejected[0][1]))
    assert denied.get('isError') is True and not denied.get('result')
    proof = {'commit': options.expected_commit, 'deployment': options.deployment, 'avatarId': avatar['id'],
        'preflights': preflights, 'secureHttpOnlyCookie': True, 'sameAvatarRenewed': True,
        'faultScopePreserved': initial_faults is not None,
        'originalInsecureIssuanceReconciled': options.reconcile_insecure_issuance, 'normalRevokeSucceeded': True, 'revokedRefreshRejected': True,
        'providerSaves': 0, 'browserInteractionVerified': False, 'realTokenExpiryWaited': False,
        'scope': 'normal HTTPS cookie protocol; expiry-triggered client behavior has separate controlled-clock tests'}
    encoded = json.dumps(proof, indent=2).encode() + b'\n'
    helper.write_new(results / 'live-session-protocol.json', encoded)
    helper.write_new(state / 'flow.acknowledged.json', json.dumps({'ownerId': avatar['id'],
        'accepted': True, 'proofSha256': hashlib.sha256(encoded).hexdigest(), 'providerSaves': 0}).encode())
    print(json.dumps(proof))

if __name__ == '__main__':
    main()
