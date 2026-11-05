"""Qualify the configured Chroma HTTPS read routes without writes or deployment."""
import argparse
import hashlib
import ipaddress
import json
import os
from pathlib import Path
import socket
from urllib.parse import quote, urlsplit
from urllib.request import HTTPRedirectHandler, ProxyHandler, Request, build_opener
from uuid import UUID

MAX_BODY = 4 * 1024 * 1024


def canonical_id(value):
    if not isinstance(value, str) or str(UUID(value)) != value or UUID(value).int == 0:
        raise ValueError('Canonical nonempty UUID required.')
    return value


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError('Duplicate JSON property.')
        result[key] = value
    return result


def validate_source(source):
    if set(source) != {'origin', 'tenant', 'database', 'collectionId', 'ids'}:
        raise ValueError('Exact source configuration required.')
    origin = urlsplit(source['origin'])
    if (origin.scheme != 'https' or not origin.hostname or origin.username is not None or origin.password is not None
            or origin.query or origin.fragment or origin.path or origin.port not in (None, 443)):
        raise ValueError('HTTPS origin without credentials, path or custom port required.')
    for key in ('tenant', 'database'):
        value = source[key]
        if not isinstance(value, str) or not value or len(value) > 128 or any(ord(c) < 33 for c in value):
            raise ValueError('Explicit bounded tenant and database required.')
    canonical_id(source['collectionId'])
    ids = source['ids']
    if not isinstance(ids, list) or len(ids) != 3 or len(set(map(canonical_id, ids))) != 3:
        raise ValueError('Three distinct admitted teaching IDs required.')
    return source


def validate_rows(value, expected):
    ids, documents = value.get('ids'), value.get('documents')
    if (not isinstance(ids, list) or not isinstance(documents, list)
            or len(ids) != len(expected) or len(documents) != len(ids)
            or set(map(canonical_id, ids)) != set(expected) or len(set(ids)) != len(ids)):
        raise ValueError('Exact native ID/document selection required.')
    for key, text in zip(ids, documents):
        if not isinstance(text, str) or len(text.encode('utf-8')) > 1024 * 1024:
            raise ValueError('Bounded native document required.')
        row = json.loads(text, object_pairs_hook=unique)
        if (row.get('Id') != key or row.get('IsPublic') is not True
                or row.get('IsActive') is not True or row.get('IsDeleted') is not False):
            raise ValueError('Matching live public teaching holon required.')
    return {'count': len(ids)}


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


def qualify(source, token, send):
    validate_source(source)
    if not token or any(ord(c) < 33 or ord(c) > 126 for c in token):
        raise ValueError('Scoped bearer credential required.')
    base = source['origin'] + '/api/v2/tenants/' + quote(source['tenant'], safe='')
    base += '/databases/' + quote(source['database'], safe='') + '/collections/'
    collection = send('GET', base + 'oasis_holons', None, token)
    if collection.get('name') != 'oasis_holons' or collection.get('id') != source['collectionId']:
        raise ValueError('Activation differs from reviewed collection identity.')
    counts = []
    for selected in (source['ids'], source['ids'][:2]):
        payload = {'ids': selected, 'include': ['documents'], 'limit': len(selected) + 1, 'offset': 0}
        value = send('POST', base + source['collectionId'] + '/get', payload, token)
        counts.append(validate_rows(value, selected)['count'])
    return {'nativeCounts': counts, 'requests': 3, 'writes': 0,
            'credentialMutationDenialVerified': False, 'cqlAccepted': False}


def send(method, url, payload, token):
    # Reject local/private destinations before any credential-bearing request.
    host = urlsplit(url).hostname
    addresses = socket.getaddrinfo(host, 443, type=socket.SOCK_STREAM)
    if not addresses or any(not ipaddress.ip_address(item[4][0]).is_global for item in addresses):
        raise ValueError('Public HTTPS destination required.')
    data = None if payload is None else json.dumps(payload).encode('utf-8')
    request = Request(url, data=data, method=method, headers={
        'Authorization': 'Bearer ' + token, 'Accept': 'application/json', 'Content-Type': 'application/json'})
    with build_opener(ProxyHandler({}), NoRedirect()).open(request, timeout=15) as response:
        raw = response.read(MAX_BODY + 1)
        if response.status != 200 or len(raw) > MAX_BODY:
            raise ValueError('Successful bounded native response required.')
    value = json.loads(raw, object_pairs_hook=unique)
    if not isinstance(value, dict):
        raise ValueError('Native object envelope required.')
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('receipt_directory', type=Path)
    args = parser.parse_args()
    raw = args.source.read_bytes()
    source = validate_source(json.loads(raw, object_pairs_hook=unique))
    token = os.environ['CQL_CHROMA_QUERY_BEARER_TOKEN']
    state = args.receipt_directory.resolve()
    state.mkdir(parents=True, exist_ok=True)
    digest = hashlib.sha256(raw).hexdigest()
    with (state / 'qualification.intent.json').open('x', encoding='utf-8') as file:
        json.dump({'sourceSha256': digest, 'maximumRequests': 3, 'writes': 0}, file)
    try:
        proof = qualify(source, token, send)
    except Exception as error:
        with (state / 'qualification.failed.json').open('x', encoding='utf-8') as file:
            json.dump({'sourceSha256': digest, 'failureType': type(error).__name__}, file)
        raise SystemExit('Qualification failed; retained failure receipt. No automatic retry.') from None
    with (state / 'qualification.acknowledged.json').open('x', encoding='utf-8') as file:
        json.dump({'sourceSha256': digest, **proof}, file)
    print('Chroma HTTPS native read routes qualified (3/2); credential denial and CQL gates remain open.')


if __name__ == '__main__':
    main()
