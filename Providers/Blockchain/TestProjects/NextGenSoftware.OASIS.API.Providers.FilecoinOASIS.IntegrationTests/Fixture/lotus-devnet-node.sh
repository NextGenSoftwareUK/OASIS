#!/usr/bin/env bash
# Runs inside the official filecoin/lotus-all-in-one 2k image: a single-miner local
# Filecoin devnet (2 KiB sectors, chain id 31415926) with the FEVM Ethereum RPC enabled.
# Proof parameters are fetched and digest-verified by Lotus itself (no TRUST_PARAMS).
set -euo pipefail
: "${LOTUS_API_PORT:?}"
export LOTUS_FEVM_ENABLEETHRPC=true LOTUS_SKIP_GENESIS_CHECK=_yes_
work=/tmp/devnet
mkdir -p "$work"
lotus fetch-params 2048 >"$work/fetch-params.log" 2>&1
lotus-seed --sector-dir "$work/presealed" pre-seal --sector-size 2KiB --num-sectors 2 >"$work/seed.log" 2>&1
lotus-seed genesis new "$work/localnet.json"
lotus-seed genesis add-miner "$work/localnet.json" "$work/presealed/pre-seal-t01000.json"
lotus daemon --lotus-make-genesis="$work/devgen.car" --genesis-template="$work/localnet.json" --bootstrap=false --api "$LOTUS_API_PORT" \
  >"$work/daemon.log" 2>&1 &
lotus wait-api --timeout 180s
lotus wallet import --as-default "$work/presealed/pre-seal-t01000.key"
lotus-miner init --genesis-miner --actor=t01000 --sector-size=2KiB --pre-sealed-sectors="$work/presealed" \
  --pre-sealed-metadata="$work/presealed/pre-seal-t01000.json" --nosync >"$work/miner-init.log" 2>&1
touch "$work/miner-initialised"
exec lotus-miner run --nosync
