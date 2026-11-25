#!/usr/bin/env bash
# Stops the local Tableland network started by start-tableland.sh <fixture-dir>.
set -euo pipefail

fixture="${1:?usage: stop-tableland.sh <fixture-dir>}"
pid_file="$fixture/tableland.pid"
[[ -f "$pid_file" ]] || exit 0
pid="$(cat "$pid_file")"
[[ "$pid" =~ ^[0-9]+$ ]] || { echo "Invalid pid file $pid_file." >&2; exit 1; }

if kill -0 "$pid" 2>/dev/null; then
  kill -TERM -- "-$pid"
  for attempt in $(seq 1 30); do
    pgrep -g "$pid" >/dev/null || break
    sleep 1
  done
  if pgrep -g "$pid" >/dev/null; then kill -KILL -- "-$pid"; fi
fi
rm -f -- "$pid_file"
