#!/usr/bin/env bash
# OStrife = ODOOM (UZDoom + OASIS) running Strife.
# The shared ODOOM integration detects Strife and reports game source OSTRIFE.
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec bash "$HERE/../ODOOM/BUILD_ODOOM.sh" "$@"
