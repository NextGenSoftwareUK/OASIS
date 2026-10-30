#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
results="$root/TestResults/PolkadotCqlCurrent"
binary=/var/tmp/oasis-runtimes/substrate-contracts-node-0.42.0/substrate-contracts-node
expected=b5e30ad4bcf37ab0a875f88c690460cf68a0847c03e74045d15bb16513892991
test "$(sha256sum "$binary" | cut -d' ' -f1)" = "$expected"
test -d /var/tmp/oasis-polkadot-evidence/ledger
test -f /var/tmp/oasis-polkadot-evidence/chain-spec.json
printf '%s\n' "$$" > "$results/node.pid"
exec taskset -c 0-3 "$binary" \
  --chain /var/tmp/oasis-polkadot-evidence/chain-spec.json \
  --alice --force-authoring --base-path /var/tmp/oasis-polkadot-evidence/ledger \
  --rpc-port 19944 --listen-addr /ip4/127.0.0.1/tcp/19945 \
  --no-prometheus --no-telemetry --no-mdns --out-peers 0 --in-peers 0 \
  --offchain-worker never --state-pruning archive --blocks-pruning archive \
  --database rocksdb --name cosmic-cql-polkadot
