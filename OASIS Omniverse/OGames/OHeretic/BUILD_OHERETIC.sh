#!/usr/bin/env bash
# OHeretic = ODOOM (UZDoom + OASIS) running Heretic.
# The shared ODOOM integration detects Heretic and reports game source OHERETIC.
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec bash "$HERE/../ODOOM/BUILD_ODOOM.sh" "$@"
