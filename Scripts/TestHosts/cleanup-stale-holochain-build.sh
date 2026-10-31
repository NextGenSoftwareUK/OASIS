#!/usr/bin/env bash
set -euo pipefail
target=/nix/var/nix/builds/nix-583-3695671890/build/source/target
[[ "${1:-inspect}" == inspect || "${1:-inspect}" == --remove ]] || exit 2
[[ -d "$target" && ! -L "$target" ]] || { echo 'Expected generated build output missing or linked' >&2; exit 2; }
[[ "$(realpath "$target")" == /nix/var/nix/builds/nix-583-3695671890/build/source/target ]] || exit 2
[[ ! -e /proc/583/exe ]] || { echo 'Original build process still exists' >&2; exit 3; }
if pgrep -x cargo >/dev/null || pgrep -x rustc >/dev/null; then
    echo 'Compiler active; preserving output' >&2
    exit 3
fi
if open_files=$(lsof -t +D "$target" 2>&1); then
    echo "Files in use; preserving output: $open_files" >&2
    exit 3
else
    status=$?
    [[ "$status" == 1 && -z "$open_files" ]] || { echo "Open-file inspection failed: $open_files" >&2; exit 3; }
fi
du -sh "$target"
if [[ "${1:-inspect}" == --remove ]]; then
    # Disposable generated compilation only; preserve source, shared store and runtimes.
    rm -rf -- "$target"
    [[ ! -e "$target" ]] || exit 4
    sync
    df -h /
fi
