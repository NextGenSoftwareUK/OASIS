#!/usr/bin/env bash
# Run OQuake with ASan/UBSan-enabled vkQuake binary.
# Uses RUN_OQUAKE.sh for basedir detection and gfx.wad extraction.

set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# Run from OQuake: default vkQuake tree is sibling of OASIS repo (…/Source/OASIS/…/OQuake → …/Source/vkQuake).
if [[ -z "${VKQUAKE_SRC:-}" ]]; then
  _vk_sibling="$(cd "$HERE/../.." && pwd)/../vkQuake"
  if [[ -d "$_vk_sibling" ]]; then
    VKQUAKE_SRC="$(cd "$_vk_sibling" && pwd)"
  else
    VKQUAKE_SRC="$HOME/Source/vkQuake"
  fi
fi

ASAN_BIN_DEFAULT="$VKQUAKE_SRC/build-asan/vkquake"
VKQUAKE_BIN="${VKQUAKE_BIN:-$ASAN_BIN_DEFAULT}"

if [[ ! -x "$VKQUAKE_BIN" ]]; then
  echo "ASan binary not found or not executable: $VKQUAKE_BIN"
  echo "Build it first:"
  echo "  cd \"$VKQUAKE_SRC\""
  echo "  meson setup build-asan --buildtype=debug -Db_sanitize=address,undefined -Db_lundef=false"
  echo "  ninja -C build-asan"
  exit 1
fi

# Defaults are strict to fail fast and print useful traces.
export ASAN_OPTIONS="${ASAN_OPTIONS:-halt_on_error=1:abort_on_error=1:detect_leaks=0:strict_string_checks=1}"
export UBSAN_OPTIONS="${UBSAN_OPTIONS:-print_stacktrace=1:halt_on_error=1}"

exec env VKQUAKE_BIN="$VKQUAKE_BIN" "$HERE/RUN_OQUAKE.sh" "$@"
