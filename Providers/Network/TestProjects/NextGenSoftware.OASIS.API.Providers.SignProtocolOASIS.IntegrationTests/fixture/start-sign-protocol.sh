#!/usr/bin/env bash
# Starts a disposable local Hardhat chain (31337 on 127.0.0.1:18545) with the Sign Protocol SP contract from
# github.com/EthSign/sign-protocol-evm at a pinned commit, for the SignProtocolOASIS native storage suite.
# Local chain only: no real funds are involved. The SP sources (SP.sol is GNU AGPLv3, interfaces/models MIT) are fetched
# at the pinned commit into the fixture directory rather than vendored into this repository.
# Usage: start-sign-protocol.sh <fixture-dir>
# Writes <fixture-dir>/sign-chain.pid, sign-chain.log and settings.env; prints the chain pid on stdout.
# Stop by sending SIGTERM to that pid.
set -euo pipefail

fixture="${1:?usage: start-sign-protocol.sh <fixture-dir>}"
[[ -d "$fixture" ]] || { echo "Fixture directory $fixture does not exist." >&2; exit 1; }
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
commit=c0d9b6d4a5e311d4402923d7365b4c73183adfb3   # sign-protocol-evm v1.1.4 (SP.sol version() == "1.1.4")
port=18545

if (exec 3<>"/dev/tcp/127.0.0.1/$port") 2>/dev/null; then
  echo "Port $port is already in use; refusing to start a second local chain." >&2
  exit 1
fi

(cd "$here" && npm ci --no-audit --no-fund --loglevel=error) >&2

checkout="$fixture/sign-protocol-evm"
git init -q "$checkout"
git -C "$checkout" fetch -q --depth 1 https://github.com/EthSign/sign-protocol-evm.git "$commit"
git -C "$checkout" -c advice.detachedHead=false checkout -q FETCH_HEAD
[[ "$(git -C "$checkout" rev-parse HEAD)" == "$commit" ]] || { echo "Sign Protocol checkout is not $commit." >&2; exit 1; }

setsid node "$here/sign-protocol-chain.cjs" "$fixture" "$checkout" "$port" >"$fixture/sign-chain.log" 2>&1 < /dev/null &
pid=$!
echo "$pid" >"$fixture/sign-chain.pid"
for attempt in $(seq 1 90); do
  if ! kill -0 "$pid" 2>/dev/null; then
    echo "Sign Protocol chain exited during start-up:" >&2
    tail -n 40 "$fixture/sign-chain.log" >&2
    exit 1
  fi
  if grep -q '^ready:' "$fixture/sign-chain.log" && [[ -s "$fixture/settings.env" ]]; then
    grep '^ready:' "$fixture/sign-chain.log" >&2
    echo "$pid"
    exit 0
  fi
  sleep 2
done
echo "Sign Protocol chain did not become ready within 180 seconds:" >&2
tail -n 40 "$fixture/sign-chain.log" >&2
kill -TERM "$pid" 2>/dev/null || true
exit 1
