#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
binary=/var/tmp/oasis-runtimes/near-sandbox-2.6.2/near-sandbox
retained=/var/tmp/oasis-near-evidence
test "$(sha256sum "$binary" | cut -d' ' -f1)" = e4a88ee5c6f6a65ac3e786b68da2fbd6044b7b77f750d91a67c89b28b005d225
test "$(sha256sum "$retained/ledger/genesis.json" | cut -d' ' -f1)" = 44cd89e1518e944f19aec5e2508550f33170ee9196158ec73012fd8e203a6713
exec 9>"$retained/environment.lock"
flock -n 9 || { echo 'Retained NEAR environment already owned.' >&2; exit 1; }
if pgrep -x near-sandbox >/dev/null; then echo 'Existing NEAR process requires review.' >&2; exit 1; fi
if test -n "$(ss -H -ltn 'sport = :3030 or sport = :24567')"; then echo 'NEAR ports occupied.' >&2; exit 1; fi
printf '%s\n' "$$" > "$root/TestResults/NearCqlCurrent/node.pid"
# Resume the original ledger only. This launcher performs no SDK writes, resets,
# initialization, deployment, signer reads or historical-journal changes.
exec "$binary" --home "$retained/ledger" run --rpc-addr 127.0.0.1:3030 --network-addr 127.0.0.1:24567
