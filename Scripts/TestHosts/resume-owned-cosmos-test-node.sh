#!/usr/bin/env bash
set -euo pipefail
container=oasis-wasmd
[[ "$(podman inspect "$container" --format '{{.Id}}')" == 58a873de020d2f79ab11e7087361db25063c277a5b5a5c2cabba72263b35f8f3 ]] || exit 2
[[ "$(podman inspect "$container" --format '{{.ImageName}}')" == docker.io/cosmwasm/wasmd@sha256:3741178a4d747fd5cf281550ab4ac30d771163af1c543f9da503c411a1691773 ]] || exit 3
[[ "$(podman inspect "$container" --format '{{.State.Running}}')" == true ]] || exit 4
# Resume only the node that this fixture deliberately stopped. Never replace a live node or initialize a ledger.
if podman exec "$container" pgrep -x wasmd >/dev/null; then
  echo 'Retained Cosmos node is already running; refusing duplicate start' >&2
  exit 5
else
  status=$?; [[ "$status" == 1 ]] || exit "$status"
fi
podman exec "$container" test -f /root/.wasmd/config/genesis.json
podman exec -d "$container" wasmd start --home /root/.wasmd --rpc.laddr tcp://0.0.0.0:26657
