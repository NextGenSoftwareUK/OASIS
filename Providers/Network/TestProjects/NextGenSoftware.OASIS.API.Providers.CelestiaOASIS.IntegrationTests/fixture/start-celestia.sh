#!/usr/bin/env bash
# Starts a disposable single-validator Celestia devnet for the native storage suite:
#   celestia-app consensus node (chain "private", 1s blocks) + celestia-node bridge node sharing its network namespace.
# Nothing is mounted from the host; all state lives in the two containers, so removing them removes the devnet.
# Prints shell assignments on stdout (eval them):
#   container="<app id> <bridge id>"  OASIS_CELESTIA_URL=...  OASIS_CELESTIA_TOKEN=...
# Every diagnostic goes to stderr. Exits non-zero if the devnet does not become ready.
set -euo pipefail

APP_IMAGE="ghcr.io/celestiaorg/celestia-app@sha256:1d8ba66155f3a920f21551442969b9e814c96a2605e9898c83ed544499213d2c" # v5.0.10
NODE_IMAGE="ghcr.io/celestiaorg/celestia-node@sha256:ad6d8d7ea8705139e695ecb90c2c3ab035ae7e89f3748fbc7b6160c1057c4615" # v0.26.4
RPC_PORT="${OASIS_CELESTIA_RPC_PORT:-16658}"
CHAIN_ID=private

log() { echo "[start-celestia] $*" >&2; }
wait_for() { # <description> <attempts> <command...>
  local what="$1" attempts="$2"; shift 2
  for _ in $(seq 1 "$attempts"); do if "$@" >/dev/null 2>&1; then return 0; fi; sleep 1; done
  log "$what never became ready"; return 1
}

app="$(docker run -d -p "127.0.0.1:${RPC_PORT}:26658" --entrypoint bash "$APP_IMAGE" -ec "
  celestia-appd init devnet --chain-id $CHAIN_ID >/dev/null 2>&1
  celestia-appd keys add validator --keyring-backend test --output json >/dev/null 2>&1
  celestia-appd keys add bridge --keyring-backend test --output json > /home/celestia/bridge-key.json 2>/dev/null
  celestia-appd genesis add-genesis-account \"\$(celestia-appd keys show validator -a --keyring-backend test)\" 1000000000000000utia
  celestia-appd genesis add-genesis-account \"\$(celestia-appd keys show bridge -a --keyring-backend test)\" 1000000000000000utia
  celestia-appd genesis gentx validator 5000000000utia --fees 500utia --keyring-backend test --chain-id $CHAIN_ID >/dev/null 2>&1
  celestia-appd genesis collect-gentxs >/dev/null 2>&1
  touch /home/celestia/genesis-ready
  exec bash /opt/entrypoint.sh start --force-no-bbr --timeout-commit 1s --grpc.enable --grpc.address 0.0.0.0:9090 --rpc.laddr tcp://0.0.0.0:26657
")"
container="$app"
fail() { log "$1"; docker logs --tail 15 "$app" >&2 || true; [[ -n "${bridge:-}" ]] && { docker logs --tail 60 "$bridge" >&2 || true; }; docker rm -fv $container >/dev/null 2>&1 || true; exit 1; }

wait_for "celestia-app genesis" 60 docker exec "$app" test -f /home/celestia/genesis-ready || fail "genesis failed"
wait_for "celestia-app block 2" 90 docker exec "$app" sh -c 'curl -fsS http://127.0.0.1:26657/status | jq -e ".result.sync_info.latest_block_height | tonumber >= 2"' || fail "consensus did not produce blocks"
genesis_hash="$(docker exec "$app" sh -c 'curl -fsS "http://127.0.0.1:26657/block?height=1" | jq -r .result.block_id.hash')"
[[ "$genesis_hash" =~ ^[0-9A-F]{64}$ ]] || fail "could not read the hash of block 1"
mnemonic="$(docker exec "$app" jq -r .mnemonic /home/celestia/bridge-key.json)"
[[ $(wc -w <<<"$mnemonic") -eq 24 ]] || fail "bridge key mnemonic missing"

bridge="$(docker run -d --network "container:$app" -e CELESTIA_CUSTOM="$CHAIN_ID:$genesis_hash" -e BRIDGE_MNEMONIC="$mnemonic" \
  --entrypoint bash "$NODE_IMAGE" -ec '
  celestia bridge init --p2p.network private --core.ip 127.0.0.1 --core.port 9090 >/dev/null 2>&1
  printf "%s\n" "$BRIDGE_MNEMONIC" | cel-key add oasis_bridge --recover --keyring-backend test --node.type bridge --p2p.network private >/dev/null 2>&1
  unset BRIDGE_MNEMONIC
  exec celestia bridge start --p2p.network private --core.ip 127.0.0.1 --core.port 9090 --rpc.addr 0.0.0.0 --rpc.port 26658 \
    --keyring.keyname oasis_bridge --keyring.backend test
')"
container="$app $bridge"

# The admin token is signed with the node's own key, so issue it only once the initialised bridge is running.
wait_for "celestia-node auth" 60 docker exec "$bridge" sh -c 'celestia bridge auth admin --p2p.network private 2>/dev/null | tail -1 | grep -Eq "^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*$"' || fail "bridge auth unavailable"
token="$(docker exec "$bridge" celestia bridge auth admin --p2p.network private 2>/dev/null | tail -1)"
url="http://127.0.0.1:${RPC_PORT}"
export token url
rpc() { curl -fsS -X POST -H "Authorization: Bearer $token" -H 'Content-Type: application/json' -d "{\"id\":1,\"jsonrpc\":\"2.0\",\"method\":\"$1\",\"params\":[]}" "$url"; }
export -f rpc
wait_for "celestia-node RPC" 120 bash -c 'rpc header.NetworkHead | jq -e ".result.header.height | tonumber >= 1"' || fail "bridge RPC never served headers"
wait_for "bridge account balance" 60 bash -c 'rpc state.Balance | jq -e ".result.amount | tonumber > 0"' || fail "bridge account is not funded"
log "devnet ready: chain $CHAIN_ID, genesis $genesis_hash, node RPC $url"

printf 'container=%q\nOASIS_CELESTIA_URL=%q\nOASIS_CELESTIA_TOKEN=%q\n' "$container" "$url" "$token"
