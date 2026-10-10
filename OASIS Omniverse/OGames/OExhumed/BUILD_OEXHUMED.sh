#!/usr/bin/env bash
# OExhumed = Raze + OASIS running Exhumed / PowerSlave; builds the shared Raze engine from OShadowWarrior.
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec bash "$HERE/../OShadowWarrior/BUILD_OSHADOWWARRIOR.sh" "$@"
