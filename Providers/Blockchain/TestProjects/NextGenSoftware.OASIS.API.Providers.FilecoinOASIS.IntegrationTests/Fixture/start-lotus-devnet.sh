#!/usr/bin/env bash
# Starts a disposable local Filecoin (Lotus 2k) devnet, funds a fresh FEVM owner key from the
# genesis miner wallet and deploys one Web3CoreOASIS storage contract per conformance mode.
#
#   start-lotus-devnet.sh <fixture-dir>
#
# Writes <fixture-dir>/container (docker id, for the caller's cleanup) and
# <fixture-dir>/settings.env (FEVM RPC, chain id, owner key, contracts, storage key).
# OASIS_FILECOIN_PROOF_PARAMS may name a docker volume or host directory that caches the
# 2 KiB proof parameters between runs; Lotus verifies every file's digest either way.
# Local test FIL only: the RPC is loopback and the chain id is the Lotus devnet id.
set -euo pipefail
fixture="${1:?fixture directory required}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$here/../../../../.." && pwd)"
runtime="$root/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS"
image="${OASIS_LOTUS_IMAGE:-filecoin/lotus-all-in-one:nightly-2k}"
port="${OASIS_LOTUS_API_PORT:-11234}"
params="${OASIS_FILECOIN_PROOF_PARAMS:-$fixture/proof-params}"
rpc="http://127.0.0.1:$port/rpc/v1"
[[ -d "$fixture" && ! -e "$fixture/container" && ! -e "$fixture/settings.env" ]]
[[ "$params" == */* ]] && mkdir -p "$params"

docker_env=(-e "LOTUS_API_PORT=$port" -e "NO_PROXY=127.0.0.1,localhost" -e "no_proxy=127.0.0.1,localhost")
docker_mounts=(-v "$params:/var/tmp/filecoin-proof-parameters" -v "$here/lotus-devnet-node.sh:/lotus-devnet-node.sh:ro")
# Lotus downloads proof parameters and reads the public drand beacon; honour the host's egress proxy.
for name in HTTPS_PROXY HTTP_PROXY https_proxy http_proxy; do
  [[ -n "${!name:-}" ]] && docker_env+=(-e "$name=${!name}")
done
if [[ -n "${SSL_CERT_FILE:-}" ]]; then
  docker_mounts+=(-v "$SSL_CERT_FILE:/etc/oasis-ca.crt:ro"); docker_env+=(-e SSL_CERT_FILE=/etc/oasis-ca.crt)
fi
docker run -d --network host --user root "${docker_env[@]}" "${docker_mounts[@]}" \
  --entrypoint bash "$image" /lotus-devnet-node.sh >"$fixture/container"
container="$(cat "$fixture/container")"

rpc_call() {
  curl -fsS --max-time 5 -H 'Content-Type: application/json' \
    -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"$1\",\"params\":${2:-[]}}" "$rpc"
}
# Proof-parameter fetch, genesis, miner init and the first mined tipsets: allow 10 minutes.
for attempt in $(seq 1 200); do
  [[ "$(docker inspect -f '{{.State.Running}}' "$container")" == true ]] || { docker logs --tail 40 "$container"; exit 1; }
  if docker exec "$container" test -f /tmp/devnet/miner-initialised; then
    height="$(rpc_call eth_blockNumber 2>/dev/null | jq -r '.result // "0x0"')" || height=0x0
    (( 16#${height#0x} >= 3 )) && break
  fi
  [[ "$attempt" != 200 ]] || { echo "Lotus devnet never mined"; exit 1; }
  sleep 3
done
rpc_call eth_chainId | jq -e '.result == "0x1df5e76"' >/dev/null

[[ -d "$runtime/node_modules/ethers" && -d "$runtime/node_modules/solc" ]] \
  || npm ci --prefix "$runtime" --no-audit --no-fund --ignore-scripts
owner="$(node "$here/deploy-lotus-storage.cjs" key "$fixture")"
docker exec "$container" lotus send "$owner" 1000 >/dev/null
node "$here/deploy-lotus-storage.cjs" deploy "$fixture" "$rpc"
echo "Lotus FEVM devnet ready at $rpc (container $container)."
