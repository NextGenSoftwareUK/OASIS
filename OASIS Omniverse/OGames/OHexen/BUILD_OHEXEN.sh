#!/usr/bin/env bash
# OHexen = ODOOM (UZDoom + OASIS) running Hexen.
# The shared ODOOM integration detects Hexen and reports game source OHEXEN.
set -e
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec bash "$HERE/../ODOOM/BUILD_ODOOM.sh" "$@"
