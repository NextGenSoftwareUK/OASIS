#!/usr/bin/env bash
set -euo pipefail
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/../.." && pwd)
package="$repo_root/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/contracts"
framework=/var/tmp/oasis-runtimes/sui-1.81.1/framework
[[ -f "$framework/sui-framework/Move.toml" ]] || { echo 'Install the pinned Sui framework once first' >&2; exit 1; }
mkdir -p "$package/deps"
if [[ ! -e "$package/deps/sui-framework" ]]; then ln -s "$framework/sui-framework" "$package/deps/sui-framework"; fi
if [[ ! -e "$package/deps/move-stdlib" ]]; then ln -s "$framework/move-stdlib" "$package/deps/move-stdlib"; fi
[[ "$(realpath "$package/deps/sui-framework")" == "$framework/sui-framework" ]]
[[ "$(realpath "$package/deps/move-stdlib")" == "$framework/move-stdlib" ]]
export SUI_CONFIG_DIR=/var/tmp/oasis-sui/network
# Compile against the pinned framework's published system addresses (0x1/0x2).
# Move unit tests run in-process; this does not submit to the mainnet RPC.
/var/tmp/oasis-runtimes/sui-1.81.1/sui move test --path "$package" --build-env mainnet
mkdir -p "$repo_root/TestResults/SuiRuntime"
/var/tmp/oasis-runtimes/sui-1.81.1/sui move build --path "$package" --build-env mainnet \
    --dump-bytecode-as-base64 --no-tree-shaking > "$repo_root/TestResults/SuiRuntime/storage-package.json"
