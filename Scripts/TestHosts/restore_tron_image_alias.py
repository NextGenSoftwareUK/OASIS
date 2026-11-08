"""One incident-scoped registry alias restoration; no image layers or service deployment."""
import base64
import hashlib
import json
import os
from pathlib import Path
import urllib.parse
import urllib.request

CURRENT = 'sha256:e2a83122b1992aae4f998a08423085264c32c0f7385812fc8efdb882f00b3abd'
PRIOR = 'sha256:c9e3748ba13de0e0134c7e0046e1b30840d27ab6cc56ff056fdf4f3b187530b5'
REPOSITORY = 'nextgensoftwareuk/oasis-web4'
MEDIA = ', '.join(['application/vnd.oci.image.index.v1+json',
                   'application/vnd.oci.image.manifest.v1+json',
                   'application/vnd.docker.distribution.manifest.list.v2+json',
                   'application/vnd.docker.distribution.manifest.v2+json'])


def require_original_alias(digest):
    if digest not in (CURRENT, PRIOR):
        raise RuntimeError('Alias has a different writer; no registry write is permitted.')
    return digest == CURRENT


def main():
    assert os.environ.get('GITHUB_ACTIONS') == 'true', 'Cloud-only registry reconciliation.'
    out = Path('TestResults/TronImageAlias')
    out.mkdir(parents=True, exist_ok=False)
    credentials = base64.b64encode((os.environ['GHCR_USER'] + ':' + os.environ['GHCR_TOKEN']).encode()).decode()
    query = urllib.parse.urlencode({'service': 'ghcr.io', 'scope': 'repository:' + REPOSITORY + ':pull,push'})
    request = urllib.request.Request('https://ghcr.io/token?' + query, headers={'Authorization': 'Basic ' + credentials})
    with urllib.request.urlopen(request, timeout=25) as response:
        token = json.load(response)['token']
    base = 'https://ghcr.io/v2/' + REPOSITORY + '/manifests/'
    headers = {'Authorization': 'Bearer ' + token, 'Accept': MEDIA}

    def manifest(reference):
        with urllib.request.urlopen(urllib.request.Request(base + reference, headers=headers), timeout=25) as response:
            body = response.read(1048577)
            assert 0 < len(body) <= 1048576
            digest = 'sha256:' + hashlib.sha256(body).hexdigest()
            assert response.headers['Docker-Content-Digest'] == digest
            media = response.headers['Content-Type'].split(';', 1)[0]
            assert media in MEDIA.split(', ')
            return digest, media, body

    digest, media, body = manifest(PRIOR)
    assert digest == PRIOR
    current, _, _ = manifest('sandbox')
    change = require_original_alias(current)
    (out / 'intent.json').write_text(json.dumps({'expectedCurrent': CURRENT, 'prior': PRIOR,
        'observed': current, 'alias': 'sandbox', 'manifestBytes': len(body), 'writeRequired': change,
        'liveDeployment': False}), encoding='utf-8')
    if change:
        # Serialized with full builds by the workflow's existing build-web4-image
        # concurrency group; reject any intervening alias change before the PUT.
        assert manifest('sandbox')[0] == CURRENT
        request = urllib.request.Request(base + 'sandbox', headers={
            'Authorization': 'Bearer ' + token, 'Content-Type': media}, data=body, method='PUT')
        with urllib.request.urlopen(request, timeout=25) as response:
            assert response.status == 201 and response.headers['Docker-Content-Digest'] == PRIOR
    assert manifest('sandbox')[0] == PRIOR
    proof = {'alias': 'sandbox', 'restoredDigest': PRIOR, 'previousUnexpectedDigest': CURRENT,
             'registryWrites': int(change), 'imageLayerDownloads': 0, 'liveDeployment': False}
    (out / 'acknowledged.json').write_text(json.dumps(proof), encoding='utf-8')
    print(json.dumps(proof))


if __name__ == '__main__':
    main()
