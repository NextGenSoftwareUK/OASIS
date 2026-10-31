#!/usr/bin/env bash
set -euo pipefail
name=oasis-tron-tre
[[ "$(podman inspect "$name" --format '{{.Id}}')" == ef2843babd111bde2709159e6eaacf2350977cccee738d62d1bcbd8833c46a12 ]] || exit 2
[[ "$(podman inspect "$name" --format '{{.ImageName}}')" == docker.io/tronbox/tre@sha256:cbf2973095933e3ae3237767082e29add2f9b647d7a4aebdfb7a953c6e5656ba ]] || exit 3
[[ "$(podman inspect "$name" --format '{{.State.Status}}')" == running ]] || exit 4
podman kill --signal TERM "$name"
for attempt in {1..120}; do
  [[ "$(podman inspect "$name" --format '{{.State.Status}}')" == exited ]] && exit 0
  sleep 0.25
done
echo 'Owned TRON node has not exited; no escalation or restart performed' >&2
exit 5
