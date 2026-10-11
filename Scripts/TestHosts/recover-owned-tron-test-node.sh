#!/usr/bin/env bash
set -euo pipefail
name=oasis-tron-tre
pin='docker.io/tronbox/tre@sha256:cbf2973095933e3ae3237767082e29add2f9b647d7a4aebdfb7a953c6e5656ba'
[[ "$(podman inspect "$name" --format '{{.ImageName}}')" == "$pin" ]] || exit 1
[[ "$(podman inspect "$name" --format '{{.State.Status}}')" == exited ]] || {
  echo 'Recovery requires the existing owned container to be exited' >&2; exit 1;
}
source_script="$(dirname "$(realpath "$0")")/tron-retained-ledger-start.sh"
mount_path="$(podman mount "$name")"
trap 'podman unmount "$name" >/dev/null' EXIT
[[ "$mount_path" == /var/lib/containers/storage/overlay/*/merged ]] || exit 1
target="$mount_path/tron/quickstart"
[[ "$(realpath "$target")" == "$target" ]] || exit 1
[[ -s "$mount_path/tron/FullNode/fullnode.conf" && -d "$mount_path/tron/FullNode/output-directory" ]] || exit 1
[[ "$(tail -n 1 "$mount_path/tron/FullNode/fullnode.conf")" == '}' ]] || {
  echo 'Retained config is not finalized; refusing to modify it' >&2; exit 1;
}
# Deploy only the repeatable launcher. Preserve the initialized config and complete ledger.
install -m 755 "$source_script" "$target"
podman unmount "$name" >/dev/null
trap - EXIT
podman start "$name"
