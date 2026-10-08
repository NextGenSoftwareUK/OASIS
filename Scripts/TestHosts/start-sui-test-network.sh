#!/usr/bin/env bash
set -euo pipefail
umask 077
runtime=/var/tmp/oasis-runtimes/sui-1.81.1/sui
test_root=/var/tmp/oasis-sui
config="$test_root/network"
[[ -x "$runtime" ]] || { echo 'Run install-sui-runtime.sh once first' >&2; exit 1; }
mkdir -p "$config"
[[ "$(realpath "$config")" == /var/tmp/oasis-sui/network ]] || { echo 'Unexpected test-network path' >&2; exit 1; }
exec 9>"$test_root/network.lock"
flock -n 9 || { echo 'Reusable Sui test network is already active' >&2; exit 1; }
if [[ ! -f "$config/network.yaml" ]]; then
    # Never force regeneration of an existing reusable ledger.
    "$runtime" genesis --quiet --working-dir "$config" --with-faucet --committee-size 1 \
        --epoch-duration-ms 3600000 > "$test_root/genesis.log" 2>&1
fi
export RUST_LOG="${RUST_LOG:-warn}"
exec "$runtime" start --quiet --network.config "$config" \
    --with-faucet=127.0.0.1:9123 --fullnode-rpc-port 9000
