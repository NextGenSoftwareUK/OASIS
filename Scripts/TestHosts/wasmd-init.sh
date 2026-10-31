#!/usr/bin/env sh
set -eu
# Run inside the single reusable oasis-wasmd container. Public development key:
# never fund it on a public network or use it for real user assets.
chain=oasis-cosmos-local
home=/root/.wasmd
if [ ! -f "$home/config/genesis.json" ]; then
  wasmd init oasis-local --chain-id "$chain" --home "$home" >/dev/null 2>&1
  mnemonic="abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon art"
  printf '%s\n' "$mnemonic" | wasmd keys add owner --recover --keyring-backend test --home "$home" >/dev/null 2>&1
  wasmd genesis add-genesis-account owner 1000000000000stake --keyring-backend test --home "$home"
  wasmd genesis gentx owner 100000000stake --chain-id "$chain" --keyring-backend test --home "$home" >/dev/null 2>&1
  wasmd genesis collect-gentxs --home "$home" >/dev/null 2>&1
fi
# The host owns node lifetime through podman exec -d; do not spawn an orphan
# process here or use a stale PID file as proof the node is live.
wasmd keys show owner -a --keyring-backend test --home "$home"
