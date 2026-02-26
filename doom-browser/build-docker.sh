#!/usr/bin/env bash
# Build WASM DOOM inside Docker (emscripten/emsdk). Output goes to doom-wasm-build/.
# Requires: Docker, and doom1.wad in doom-wasm-src/src/
set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

if [[ ! -f doom-wasm-src/src/doom1.wad ]]; then
  echo "Missing doom1.wad. Copy the shareware WAD to: doom-wasm-src/src/doom1.wad"
  echo "See https://doomwiki.org/wiki/DOOM1.WAD"
  exit 1
fi

IMAGE="doom-wasm-builder"
echo "Building Docker image $IMAGE (one-time)..."
docker build -f Dockerfile.doom-wasm -t "$IMAGE" .
echo "Building WASM DOOM in Docker (this may take a few minutes)..."
docker run --rm \
  -v "$SCRIPT_DIR":/src \
  -u "$(id -u):$(id -g)" \
  "$IMAGE" \
  /bin/bash -c "
    set -e
    cd /src/doom-wasm-src
    ./scripts/clean.sh
    emmake make clean 2>/dev/null || true
    emconfigure autoreconf -fiv
    export EMCONFIGURE_JS=2
    ac_cv_exeext=.html \
    ac_cv_header_stdio_h=yes \
    ac_cv_header_stdlib_h=yes \
    ac_cv_header_string_h=yes \
    ac_cv_header_inttypes_h=yes \
    ac_cv_header_stdint_h=yes \
    ac_cv_header_strings_h=yes \
    ac_cv_header_sys_stat_h=yes \
    ac_cv_header_sys_types_h=yes \
    ac_cv_header_unistd_h=yes \
    ac_cv_header_dirent_h=yes \
    ac_cv_func_mmap=no \
    ac_cv_func_ioperm=no \
    ac_cv_have_decl_strcasecmp=yes \
    ac_cv_have_decl_strncasecmp=yes \
    ac_cv_c_undeclared_builtin_options='none needed' \
    emconfigure ./configure \
      --host=none-none-none \
      --without-libsamplerate \
      --without-libpng \
      --config-cache
    emmake make
    mkdir -p /src/doom-wasm-build
    cp -f src/websockets-doom.js src/websockets-doom.wasm src/doom1.wad /src/doom-wasm-build/
    mv /src/doom-wasm-build/websockets-doom.js /src/doom-wasm-build/doom.js
    mv /src/doom-wasm-build/websockets-doom.wasm /src/doom-wasm-build/doom.wasm
    if [[ -f src/default.cfg ]]; then cp -f src/default.cfg /src/doom-wasm-build/; fi
    echo 'Build complete. Output in doom-wasm-build/'
    ls -la /src/doom-wasm-build
  "

echo "Done. Run the launcher with: npm start"
