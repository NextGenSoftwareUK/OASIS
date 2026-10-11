#!/usr/bin/env bash
set -euo pipefail
pid="${1:?Explicit owned node PID required}"
[[ "$pid" =~ ^[0-9]+$ && "$pid" -gt 1 ]] || exit 2
[[ "$(readlink -f "/proc/$pid/exe")" == /var/tmp/oasis-runtimes/sui-1.81.1/sui ]] || exit 3
tr '\0' '\n' < "/proc/$pid/cmdline" | grep -Fxq '/var/tmp/oasis-sui/network' || exit 4
# This retained daemon is launched without an interactive terminal. SIGTERM is
# the process termination signal; inherited SIGINT handling may ignore Ctrl-C.
kill -TERM "$pid"
for attempt in {1..100}; do
  [[ -e "/proc/$pid/exe" ]] || exit 0
  sleep 0.1
done
echo 'Owned Sui node has not exited; refusing further signals' >&2
exit 5
