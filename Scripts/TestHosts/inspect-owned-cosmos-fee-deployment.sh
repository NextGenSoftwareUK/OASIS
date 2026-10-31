#!/usr/bin/env bash
set -euo pipefail
container=oasis-wasmd
[[ "$(podman inspect "$container" --format '{{.Id}}')" == 58a873de020d2f79ab11e7087361db25063c277a5b5a5c2cabba72263b35f8f3 ]] || exit 2
[[ "$(podman inspect "$container" --format '{{.ImageName}}')" == docker.io/cosmwasm/wasmd@sha256:3741178a4d747fd5cf281550ab4ac30d771163af1c543f9da503c411a1691773 ]] || exit 3
[[ "$(podman inspect "$container" --format '{{.State.Running}}')" == true ]] || exit 4
binding="$(podman port "$container" 26657/tcp)"
[[ "$binding" == '0.0.0.0:26657' || "$binding" == '127.0.0.1:26657' ]] || exit 5
# Inspect the running process's executable inode, not a potentially replaced installed file.
# No signing key, private account file or genesis content is emitted.
podman exec "$container" sh -ec '
  task_pid="$(pgrep -x wasmd)"
  case "$task_pid" in ""|*[!0-9]*) exit 6;; esac
  printf "binary_sha256: "
  sha256sum "/proc/$task_pid/exe" | cut -d " " -f 1
  printf "genesis_sha256: "
  sha256sum /root/.wasmd/config/genesis.json | cut -d " " -f 1
  "/proc/$task_pid/exe" version --long
  test "$task_pid" = "$(pgrep -x wasmd)"
'
