#!/usr/bin/env bash
set -euo pipefail

# Use the retained WSL Rust/Binaryen installation and a single fixed target.
# This compiles only; it never starts a node, signs, uploads or deploys code.
repo="$(cd -- "$(dirname -- "$0")/../.." && pwd)"
contract="$repo/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/contracts/oasis"
target="$repo/TestResults/PolkadotCurrent/record-contract-target"
rustup="${OASIS_RUSTUP:-$HOME/.cargo/bin/rustup}"
[[ -x "$rustup" ]] || { echo 'Reuse the installed rustup runtime.' >&2; exit 1; }
[[ "$(wasm-opt --version)" == 'wasm-opt version 120' ]] || { echo 'Expected retained Binaryen120.' >&2; exit 1; }
[[ "$(wasm-metadce --version)" == 'wasm-metadce version 120' ]] || { echo 'Expected retained Binaryen120 metadce.' >&2; exit 1; }
cd "$contract" # Cargo discovers the target-specific .cargo/config.toml here.
"$rustup" run 1.88.0 cargo build --offline --locked --release --no-default-features \
    --target wasm32-unknown-unknown --jobs 2 --target-dir "$target"
raw="$target/wasm32-unknown-unknown/release/oasis_records.wasm"
pruned="$target/oasis-records-pruned.wasm"
artifact="$target/oasis-records.wasm"
optimized="$target/oasis-records-optimized.wasm"
lowerer='/mnt/c/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/PlaybackEngines/WebGLSupport/BuildTools/Emscripten/binaryen/bin/wasm-opt.exe'
[[ -x "$lowerer" ]] || { echo 'Reuse the existing Unity Binaryen124 lowering tool.' >&2; exit 1; }
[[ "$("$lowerer" --version)" == *'wasm-opt version 124 '* ]] || { echo 'Unexpected retained lowering tool.' >&2; exit 1; }
wasm-metadce "$raw" --graph-file exports.json --output "$pruned" --mvp-features
wasm-opt "$pruned" -Oz --strip-debug --strip-producers --strip-target-features \
    --mvp-features --output "$optimized"
# Rust's precompiled core/linker can retain bulk-memory instructions even with
# target-cpu=mvp. Lower them with the newer already-installed Binaryen tool.
"$lowerer" "$(wslpath -w "$optimized")" --enable-bulk-memory-opt \
    --llvm-memory-copy-fill-lowering --signext-lowering --strip-target-features \
    --output "$(wslpath -w "$artifact")"
sha256sum "$artifact"
wc -c "$artifact"
