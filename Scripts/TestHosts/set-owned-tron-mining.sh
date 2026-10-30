#!/usr/bin/env bash
set -euo pipefail
name=oasis-tron-tre
[[ "$(podman inspect "$name" --format '{{.Id}}')" == ef2843babd111bde2709159e6eaacf2350977cccee738d62d1bcbd8833c46a12 ]] || exit 2
[[ "$(podman inspect "$name" --format '{{.ImageName}}')" == docker.io/tronbox/tre@sha256:cbf2973095933e3ae3237767082e29add2f9b647d7a4aebdfb7a953c6e5656ba ]] || exit 3
[[ "$(podman inspect "$name" --format '{{.State.Status}}')" == running ]] || exit 4
[[ "${1:-}" == start || "${1:-}" == drain ]] || exit 5
pid=$(podman inspect "$name" --format '{{.State.Pid}}')
[[ "$pid" =~ ^[1-9][0-9]*$ ]] || exit 6
# Reuse host Python in only the owned container's network namespace. No tool
# install or additional exposed administrative port is necessary.
nsenter -t "$pid" -n /usr/bin/python3 - "$1" <<'PY'
import json, sys, urllib.request
def rpc(method, params):
    body=json.dumps(dict(jsonrpc='2.0', id=1, method=method, params=params)).encode()
    request=urllib.request.Request('http://127.0.0.1:8545/tre', data=body,
                                  headers={'Content-Type':'application/json'})
    with urllib.request.urlopen(request, timeout=15) as response:
        value=json.load(response)
    if value.get('id') != 1 or 'error' in value or 'result' not in value:
        raise RuntimeError('Owned TRE administrative request failed: '+str(value))
    return value['result']
if rpc('tre_blockTime', ['0x1' if sys.argv[1]=='start' else '0x0']) is not True:
    raise RuntimeError('Mining interval was not acknowledged')
if sys.argv[1]=='drain':
    # TRE defaults to transaction-triggered blocks. Snapshot flushing starts
    # with the following block; drain two empty blocks before graceful stop.
    # No business transaction is signed, retried or replayed here.
    result=rpc('tre_mine', [{'blocks':'0x2'}])
    if not isinstance(result, str):
        raise RuntimeError('Empty-block drain was not acknowledged')
print('Owned TRE mining '+sys.argv[1]+' acknowledged')
PY
