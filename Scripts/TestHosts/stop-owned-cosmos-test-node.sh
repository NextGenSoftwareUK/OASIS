#!/usr/bin/env bash
set -euo pipefail
container=oasis-wasmd
expected=58a873de020d2f79ab11e7087361db25063c277a5b5a5c2cabba72263b35f8f3
[[ "$(podman inspect "$container" --format '{{.Id}}')" == "$expected" ]] || exit 2
[[ "$(podman inspect "$container" --format '{{.ImageName}}')" == docker.io/cosmwasm/wasmd@sha256:3741178a4d747fd5cf281550ab4ac30d771163af1c543f9da503c411a1691773 ]] || exit 3
[[ "$(podman inspect "$container" --format '{{.State.Running}}')" == true ]] || exit 4
podman exec "$container" sh -c '
  test -f /root/.wasmd/config/genesis.json && command -v pgrep >/dev/null && command -v pkill >/dev/null || exit 5
  if pgrep -x wasmd >/dev/null; then pkill -INT -x wasmd
  else status=$?; test "$status" -eq 1 || exit "$status"; fi
'
for attempt in {1..100}; do
  if podman exec "$container" pgrep -x wasmd >/dev/null; then :
  else status=$?; [[ "$status" == 1 ]] && exit 0; echo 'Owned process inspection failed' >&2; exit "$status"; fi
  sleep 0.1
done
echo 'Owned wasmd node has not exited; refusing escalation' >&2
exit 5
