#!/usr/bin/env bash
# Build STARAPIClient for Linux or macOS and deploy the native shared library + star_api.h to ODOOM and OQuake.
# Usage: ./build-and-deploy-star-api-unix.sh [ -ForceBuild ] [ -Runtime linux-x64|osx-x64|osx-arm64 ]
# If -Runtime is omitted, it is auto-detected from uname.
# Called by BUILD_ODOOM.sh and BUILD_OQUAKE.sh on Linux and macOS.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
HEADER_PATH="$PROJECT_DIR/star_api.h"
PROJECT_PATH="$PROJECT_DIR/STARAPIClient.csproj"

OMNIVERSE_ROOT="$(cd "$PROJECT_DIR/.." && pwd)"
ODOOM_DIR="$OMNIVERSE_ROOT/ODOOM"
OQUAKE_DIR="$OMNIVERSE_ROOT/OQuake"

FORCE_BUILD=0
RUNTIME=""
prev=""
for arg in "$@"; do
  if [[ "$prev" == "-Runtime" ]]; then
    RUNTIME="$arg"
    prev=""
    continue
  fi
  case "$arg" in
    -ForceBuild|--force) FORCE_BUILD=1 ;;
    -Runtime)            prev="-Runtime"; continue ;;
    linux-x64|osx-x64|osx-arm64) [[ -z "$RUNTIME" ]] && RUNTIME="$arg" ;;
  esac
  prev=""
done

# Auto-detect runtime if not set
if [[ -z "$RUNTIME" ]]; then
  case "$(uname -s)" in
    Linux)
      RUNTIME="linux-x64"
      ;;
    Darwin)
      if [[ "$(uname -m)" == "arm64" ]]; then
        RUNTIME="osx-arm64"
      else
        RUNTIME="osx-x64"
      fi
      ;;
    *)
      echo "ERROR: Unsupported OS: $(uname -s). Set -Runtime explicitly (linux-x64, osx-x64, osx-arm64)." >&2
      exit 1
      ;;
  esac
fi

# Native library extension and names: .so on Linux, .dylib on macOS
case "$RUNTIME" in
  linux-x64)
    LIB_EXT=".so"
    LIB_NAMES=("libstar_api.so" "star_api.so")
    PLATFORM_LABEL="Linux"
    ;;
  osx-x64|osx-arm64)
    LIB_EXT=".dylib"
    LIB_NAMES=("libstar_api.dylib" "star_api.dylib")
    PLATFORM_LABEL="macOS ($RUNTIME)"
    ;;
  *)
    echo "ERROR: Unsupported runtime: $RUNTIME. Use linux-x64, osx-x64, or osx-arm64." >&2
    exit 1
    ;;
esac

PUBLISH_DIR="$PROJECT_DIR/bin/Release/net8.0/$RUNTIME/publish"
NATIVE_DIR="$PROJECT_DIR/bin/Release/net8.0/$RUNTIME/native"

# Find existing native library for timestamp check
STAR_LIB=""
for name in "${LIB_NAMES[@]}"; do
  if [[ -f "$PUBLISH_DIR/$name" ]]; then
    STAR_LIB="$PUBLISH_DIR/$name"
    break
  fi
  if [[ -f "$NATIVE_DIR/$name" ]]; then
    STAR_LIB="$NATIVE_DIR/$name"
    break
  fi
done

need_build=$FORCE_BUILD
if [[ $need_build -eq 0 && -n "$STAR_LIB" && -f "$STAR_LIB" ]]; then
  if [[ -n "$(find "$PROJECT_DIR" -maxdepth 1 \( -name "*.cs" -o -name "*.csproj" \) -newer "$STAR_LIB" 2>/dev/null)" ]]; then
    need_build=1
  fi
  if [[ $need_build -eq 0 && -f "$HEADER_PATH" ]] && [[ "$HEADER_PATH" -nt "$STAR_LIB" ]]; then
    need_build=1
  fi
fi

echo "========================================"
echo "OASIS STAR API - Build and Deploy ($PLATFORM_LABEL)"
echo "========================================"
echo ""

if [[ $need_build -eq 0 && -n "$STAR_LIB" && -f "$STAR_LIB" ]]; then
  echo "STARAPIClient unchanged (native library is up to date), skipping build."
else
  if [[ ! -f "$STAR_LIB" ]]; then
    echo "STARAPIClient not built yet or output missing; building..."
  fi
  echo "Publishing NativeAOT WEB5 STAR API wrapper for $RUNTIME..."
  dotnet publish "$PROJECT_PATH" -c Release -r "$RUNTIME" -p:PublishAot=true -p:SelfContained=true -p:NoWarn=NU1605
fi

# Locate built library after build
STAR_LIB=""
for name in "${LIB_NAMES[@]}"; do
  if [[ -f "$PUBLISH_DIR/$name" ]]; then
    STAR_LIB="$PUBLISH_DIR/$name"
    break
  fi
  if [[ -f "$NATIVE_DIR/$name" ]]; then
    STAR_LIB="$NATIVE_DIR/$name"
    break
  fi
done

if [[ -z "$STAR_LIB" || ! -f "$STAR_LIB" ]]; then
  echo "ERROR: Build did not produce ${LIB_NAMES[0]} in publish/native dir." >&2
  echo "  Checked: $PUBLISH_DIR, $NATIVE_DIR" >&2
  exit 1
fi

if [[ ! -f "$HEADER_PATH" ]]; then
  echo "ERROR: Missing star_api.h at $HEADER_PATH" >&2
  exit 1
fi

for target in "$ODOOM_DIR" "$OQUAKE_DIR"; do
  if [[ -d "$target" ]]; then
    echo "Deploying to $target"
    cp -f "$STAR_LIB" "$target/"
    cp -f "$HEADER_PATH" "$target/"
    if [[ "$target" == "$OQUAKE_DIR" && -d "$OQUAKE_DIR/Code" ]]; then
      cp -f "$STAR_LIB" "$OQUAKE_DIR/Code/"
      cp -f "$HEADER_PATH" "$OQUAKE_DIR/Code/"
    fi
  fi
done

echo ""
echo "WEB5 STAR API wrapper publish/deploy complete ($PLATFORM_LABEL)."
