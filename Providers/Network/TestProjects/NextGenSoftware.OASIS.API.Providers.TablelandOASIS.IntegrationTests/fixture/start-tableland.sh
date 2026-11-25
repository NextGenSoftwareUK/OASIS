#!/usr/bin/env bash
# Starts a disposable local Tableland network (Hardhat chain 31337 on 127.0.0.1:8545, validator on 127.0.0.1:8080)
# for the TablelandOASIS native storage suite. Local chain only: no real funds are involved.
# Usage: start-tableland.sh <fixture-dir>   (writes tableland.pid and tableland.log into <fixture-dir>)
# Stop with stop-tableland.sh <fixture-dir>.
set -euo pipefail

fixture="${1:?usage: start-tableland.sh <fixture-dir>}"
[[ -d "$fixture" ]] || { echo "Fixture directory $fixture does not exist." >&2; exit 1; }
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# @tableland/local 3.0.2 bundles Hardhat 2.22.2, which supports Node 18/20 and aborts start-up on other versions.
node_major="$(node -p 'process.versions.node.split(".")[0]')"
if [[ "$node_major" != "20" ]]; then
  echo "The local Tableland fixture requires Node 20.x (found $(node --version))." >&2
  exit 1
fi
for port in 8545 8080; do
  if (exec 3<>"/dev/tcp/127.0.0.1/$port") 2>/dev/null; then
    echo "Port $port is already in use; refusing to start a second local Tableland network." >&2
    exit 1
  fi
done

(cd "$here" && npm ci --no-audit --no-fund --loglevel=error)

# setsid makes the launcher the leader of its own process group so stop-tableland.sh can stop Hardhat and the validator with it.
setsid node "$here/node_modules/.bin/local-tableland" --silent >"$fixture/tableland.log" 2>&1 < /dev/null &
echo "$!" >"$fixture/tableland.pid"

rpc_ready() {
  curl -fsS -H 'Content-Type: application/json' -d '{"jsonrpc":"2.0","id":1,"method":"eth_chainId","params":[]}' \
    http://127.0.0.1:8545 2>/dev/null | grep -q '"result":"0x7a69"'
}
for attempt in $(seq 1 150); do
  if ! kill -0 "$(cat "$fixture/tableland.pid")" 2>/dev/null; then
    echo "local-tableland exited during start-up:" >&2
    tail -n 40 "$fixture/tableland.log" >&2
    exit 1
  fi
  if rpc_ready && curl -fsS http://127.0.0.1:8080/api/v1/health >/dev/null 2>&1; then
    echo "Local Tableland network is ready (chain 31337, registry 0xe7f1725E7734CE288F8367e1Bb143E90bb3F0512)."
    exit 0
  fi
  sleep 2
done
echo "Local Tableland network did not become ready within 300 seconds:" >&2
tail -n 40 "$fixture/tableland.log" >&2
exit 1
