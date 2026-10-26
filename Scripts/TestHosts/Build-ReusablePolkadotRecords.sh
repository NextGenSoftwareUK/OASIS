#!/usr/bin/env bash
set -euo pipefail

# Use the retained WSL Rust/Binaryen installation and a single fixed target.
# This compiles only; it never starts a node, signs, uploads or deploys code.
repo="$(cd -- "$(dirname -- "$0")/../.." && pwd)"
contract="$repo/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/contracts/oasis"
[[ $# -le 1 ]] || { echo 'Expected only records, psp34, psp34-escrow or token-escrow.' >&2; exit 1; }
case "${1:-records}" in
    records) manifest="$contract/Cargo.toml"; crate='oasis_records'; name='oasis-records' ;;
    psp34) manifest="$contract/../psp34/Cargo.toml"; crate='oasis_psp34'; name='oasis-psp34' ;;
    psp34-escrow) manifest="$contract/../psp34/Cargo.toml"; crate='oasis_psp34'; name='oasis-psp34-escrow' ;;
    token-escrow) manifest="$contract/../token-escrow/Cargo.toml"; crate='oasis_token_escrow'; name='oasis-token-escrow' ;;
    *) echo 'Unknown contract target; expected records, psp34, psp34-escrow or token-escrow.' >&2; exit 1 ;;
esac
target="$repo/TestResults/PolkadotCurrent/record-contract-target"
rustup="${OASIS_RUSTUP:-$HOME/.cargo/bin/rustup}"
[[ -x "$rustup" ]] || { echo 'Reuse the installed rustup runtime.' >&2; exit 1; }
[[ "$(wasm-opt --version)" == 'wasm-opt version 120' ]] || { echo 'Expected retained Binaryen120.' >&2; exit 1; }
[[ "$(wasm-metadce --version)" == 'wasm-metadce version 120' ]] || { echo 'Expected retained Binaryen120 metadce.' >&2; exit 1; }
cd "$contract" # Cargo discovers the target-specific .cargo/config.toml here.
"$rustup" run 1.88.0 cargo build --offline --locked --release --no-default-features \
    --manifest-path "$manifest" --target wasm32-unknown-unknown --jobs 2 --target-dir "$target"
raw="$target/wasm32-unknown-unknown/release/$crate.wasm"
pruned="$target/$name-pruned.wasm"
artifact="$target/$name.wasm"
optimized="$target/$name-optimized.wasm"
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
