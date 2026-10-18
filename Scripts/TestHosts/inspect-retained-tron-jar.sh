#!/usr/bin/env bash
set -euo pipefail
# Offline inspection only. Reuse the stopped retained container; never launch Java,
# copy/extract its JAR, create a container, inspect keys, or modify its ledger/config.
name=oasis-tron-tre
[[ "$(podman inspect "$name" --format '{{.Id}}')" == ef2843babd111bde2709159e6eaacf2350977cccee738d62d1bcbd8833c46a12 ]] || exit 2
[[ "$(podman inspect "$name" --format '{{.ImageName}}')" == docker.io/tronbox/tre@sha256:cbf2973095933e3ae3237767082e29add2f9b647d7a4aebdfb7a953c6e5656ba ]] || exit 3
[[ "$(podman inspect "$name" --format '{{.State.Status}}')" == exited ]] || exit 4
mount_path="$(podman mount "$name")"
trap 'podman unmount "$name" >/dev/null' EXIT
[[ "$mount_path" == /var/lib/containers/storage/overlay/*/merged ]] || exit 5
jar_path="$mount_path/tron/FullNode/FullNode.jar"
[[ -f "$jar_path" && "$(realpath "$jar_path")" == "$jar_path" ]] || exit 6
[[ $# -eq 0 || ( $# -eq 1 && "$1" == --fee-rules ) ]] || exit 8
python3 - "$jar_path" "${1:-identity}" <<'PY'
import hashlib, json, pathlib, sys, zipfile
path = pathlib.Path(sys.argv[1])
with path.open('rb') as stream:
    digest = hashlib.file_digest(stream, 'sha256').hexdigest()
with zipfile.ZipFile(path) as archive:
    if len(archive.infolist()) > 100000:
        raise ValueError('Unexpected archive size; inspect manually')
    info = archive.getinfo('META-INF/MANIFEST.MF')
    if info.file_size > 65536:
        raise ValueError('Unexpected manifest size')
    manifest = archive.read(info).decode('utf-8')
    fields = {}
    for line in manifest.splitlines():
        if ': ' in line:
            key, value = line.split(': ', 1)
            if key in {'Manifest-Version', 'Main-Class', 'Implementation-Version', 'Build-Revision', 'Git-Commit'}:
                fields[key] = value
    candidates = [entry.filename for entry in archive.infolist()
                  if entry.filename.startswith('org/tron/') and
                  (entry.filename.endswith('/Version.class') or entry.filename.endswith('/git.properties'))]
    version_info = archive.getinfo('org/tron/program/Version.class')
    if version_info.file_size > 65536:
        raise ValueError('Unexpected Version class size')
    version_data = archive.read(version_info)
    result = {'jarSha256': digest, 'jarBytes': path.stat().st_size,
                      'manifest': fields, 'versionMetadataEntries': candidates,
                      'versionClassSha256': hashlib.sha256(version_data).hexdigest(),
                      'versionClassBase64': __import__('base64').b64encode(version_data).decode('ascii'),
                      'nodeStarted': False, 'extractedFiles': 0, 'feeModelReviewed': False}
    if sys.argv[2] == '--fee-rules':
        entries = ['org/tron/core/db/BandwidthProcessor.class', 'org/tron/core/db/Manager.class',
                   'org/tron/core/actuator/TransferActuator.class', 'org/tron/core/actuator/CreateAccountActuator.class',
                   'org/tron/core/db/ResourceProcessor.class']
        result['feeClassMaterials'] = []
        for name in entries:
            info = archive.getinfo(name)
            if info.file_size > 262144:
                raise ValueError('Fee class exceeds reviewed diagnostic bound')
            data = archive.read(info)
            result['feeClassMaterials'].append({'entry': name, 'sha256': hashlib.sha256(data).hexdigest(),
                                               'base64': __import__('base64').b64encode(data).decode('ascii')})
    print(json.dumps(result))
PY
[[ "$(podman inspect "$name" --format '{{.State.Status}}')" == exited ]] || exit 7
