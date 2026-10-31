#!/usr/bin/env bash
set -euo pipefail
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/../.." && pwd)
contract="$repo_root/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/contracts/oasis"
target=/var/tmp/oasis-cosmos-contract-target
task_cargo_bin="${OASIS_CARGO_BIN:-${CARGO_HOME:-$HOME/.cargo}/bin}"
export PATH="$task_cargo_bin:$PATH"
[[ "$(rustc --version)" == 'rustc 1.99.0 '* ]] || { echo 'Use the existing pinned Rust 1.99.0 compiler' >&2; exit 1; }
[[ -d "$(rustc --print sysroot)/lib/rustlib/src/rust/library/std" ]] || {
  echo 'The matching rust-src component must be installed once in this toolchain' >&2; exit 1;
}
mkdir -p "$target"
[[ "$(realpath "$target")" == /var/tmp/oasis-cosmos-contract-target ]]
exec 9>"$target/build.lock"
flock -n 9 || { echo 'The reusable Cosmos contract build is already active' >&2; exit 1; }
cd "$contract"
# wasmvm rejects bulk-memory instructions. Disabling them only for the contract
# leaves those instructions in the precompiled standard library. Rebuild std and
# compiler builtins with the same features, in the single existing target cache.
# build-std and LLVM bulk-memory-opt are unstable interfaces: pin this compiler
# and require real wasmd deployment validation before accepting a new compiler.
CARGO_TARGET_DIR="$target" RUSTC_BOOTSTRAP=1 \
  RUSTFLAGS='-C target-feature=-bulk-memory,-bulk-memory-opt,-reference-types,-multivalue,-nontrapping-fptoint,-sign-ext' \
  cargo build --release --target wasm32-unknown-unknown --locked --offline \
    -Z build-std=std,panic_abort -j 2
printf '%s\n' "$target/wasm32-unknown-unknown/release/oasis_cosmos_storage.wasm"
