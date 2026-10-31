#!/usr/bin/env bash
set -euo pipefail
name=oasis-cosmos
original=061327dc582a10e447b294027b0615a905fda764ed02ee227f7054bc8fa2ccea
recovery=/var/tmp/oasis-cosmosdb-recovery
snapshot=localhost/oasis-cosmos-preserved:loopback-cutover
umask 077
test "$(podman inspect "$name" --format '{{.Id}} {{.State.Status}}')" = "$original exited"
test ! -e "$recovery"
mkdir -m 700 "$recovery"
podman inspect "$name" > "$recovery/original-inspect.json"
mountpoint=$(podman mount "$name")
trap 'podman unmount "$original" >/dev/null 2>&1 || true' EXIT
test -d "$mountpoint/data"
(cd "$mountpoint"; find data scripts/certs -type f -print0 | sort -z | xargs -0 -r sha256sum) > "$recovery/data-before.sha256"
podman unmount "$original" >/dev/null
trap - EXIT
podman commit --quiet "$original" "$snapshot" > "$recovery/snapshot-id.txt"
test -s "$recovery/snapshot-id.txt"
podman image inspect "$snapshot" > "$recovery/snapshot-inspect.json"
# Only remove the stopped original after its writable layer has been committed.
podman rm "$original"
original_home=$(python3 -c 'import json,sys; a=json.load(open(sys.argv[1]))[0]; print(dict(v.split("=",1) for v in a["Config"]["Env"])["HOME"])' "$recovery/original-inspect.json")
original_hostname=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))[0]["Config"]["Hostname"])' "$recovery/original-inspect.json")
podman create --name "$name" --pull=never \
  --hostname "$original_hostname" --env "HOME=$original_home" \
  -p 127.0.0.1:1234:1234 -p 127.0.0.1:8080:8080 -p 127.0.0.1:8081:8081 \
  "$snapshot" > "$recovery/replacement-id.txt"
mountpoint=$(podman mount "$name")
trap 'podman unmount "$name" >/dev/null 2>&1 || true' EXIT
(cd "$mountpoint"; sha256sum --check --quiet "$recovery/data-before.sha256")
podman unmount "$name" >/dev/null
trap - EXIT
podman inspect "$name" > "$recovery/replacement-inspect.json"
python3 - "$recovery" <<'PY'
import json, pathlib, sys
p=pathlib.Path(sys.argv[1])
a=json.loads((p/'original-inspect.json').read_text())[0]
b=json.loads((p/'replacement-inspect.json').read_text())[0]
for key in ('Env','Entrypoint','Cmd','User','WorkingDir'):
    old, new = a['Config'][key], b['Config'][key]
    if key == 'Env':
        old, new = (dict(v.split('=',1) for v in values) for values in (old,new))
    if old != new:
        raise SystemExit('Preserved container configuration differs: '+key)
ports=b['HostConfig']['PortBindings']
assert set(ports)=={'1234/tcp','8080/tcp','8081/tcp'}
assert all(len(v)==1 and v[0]['HostIp']=='127.0.0.1' for v in ports.values())
print('Preserved data/certificate hashes and container configuration verified; replacement remains stopped.')
PY
du -sk "$recovery"
podman image inspect "$snapshot" --format '{{.Id}} {{.Size}}'
