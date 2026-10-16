#!/usr/bin/env bash
set -euo pipefail
container=oasis-wasmd
[[ "$(podman inspect "$container" --format '{{.Id}}')" == 58a873de020d2f79ab11e7087361db25063c277a5b5a5c2cabba72263b35f8f3 ]] || exit 2
[[ "$(podman inspect "$container" --format '{{.ImageName}}')" == docker.io/cosmwasm/wasmd@sha256:3741178a4d747fd5cf281550ab4ac30d771163af1c543f9da503c411a1691773 ]] || exit 3
[[ "$(podman inspect "$container" --format '{{.State.Running}}')" == true ]] || exit 4
# The retained container's PID1 is an idle sleep, not the separately managed ledger node.
# Never let container cleanup bypass graceful wasmd shutdown, even if that earlier step failed.
podman exec "$container" sh -c 'command -v pgrep >/dev/null && ! pgrep -x wasmd >/dev/null' || {
  echo 'Owned wasmd must be confirmed stopped before closing its idle container' >&2
  exit 5
}
[[ "$(podman exec "$container" cat /proc/1/comm)" == sleep ]] || exit 6
[[ "$(podman exec "$container" sh -c 'tr "\000" " " </proc/1/cmdline')" == 'sleep infinity ' ]] || exit 7
# PID1 sleep ignores default SIGTERM. Only the verified idle holder is killed; no ledger process remains.
podman kill --signal KILL "$container"
podman wait "$container" >/dev/null
[[ "$(podman inspect "$container" --format '{{.State.Running}}')" == false ]] || exit 8
