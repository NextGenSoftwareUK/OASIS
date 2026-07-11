#!/usr/bin/env bash
# Build and **publish** STAR CLI so RUN_STAR_CLI.sh finds star + DNA/OASIS_DNA.json together.
# (dotnet build alone only updates bin/…; an old publish/linux-x64/star without DNA beside it
#  causes "DNA invalid" / SecretKey issues.)
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/../include/pause_on_exit.inc.sh"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_DIR="$REPO_ROOT/STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
CONFIG="${1:-Release}"

detect_rid() {
  case "$(uname -s 2>/dev/null)" in
    Linux)
      case "$(uname -m 2>/dev/null)" in
        aarch64 | arm64) echo linux-arm64 ;;
        *) echo linux-x64 ;;
      esac
      ;;
    Darwin)
      case "$(uname -m 2>/dev/null)" in
        arm64) echo osx-arm64 ;;
        *) echo osx-x64 ;;
      esac
      ;;
    MINGW* | MSYS* | CYGWIN*)
      echo win-x64
      ;;
    *)
      echo linux-x64
      ;;
  esac
}

RID="${STAR_CLI_RID:-$(detect_rid)}"
OUT="$PROJECT_DIR/publish/$RID"

# Quick dev publish: many DLLs + star, but fast (~seconds). Set STAR_CLI_QUICK_PUBLISH=1
if [[ "${STAR_CLI_QUICK_PUBLISH:-}" == "1" ]]; then
  echo "Publishing STAR CLI (quick: framework-dependent, $CONFIG, $RID) -> $OUT"
  dotnet publish "$PROJECT_DIR/NextGenSoftware.OASIS.STAR.CLI.csproj" \
    -c "$CONFIG" \
    -r "$RID" \
    -o "$OUT" \
    --self-contained false \
    -p:PublishSingleFile=false
else
  echo "Publishing STAR CLI (single-file self-contained, $CONFIG, $RID) -> $OUT"
  dotnet publish "$PROJECT_DIR/NextGenSoftware.OASIS.STAR.CLI.csproj" \
    -c "$CONFIG" \
    -r "$RID" \
    -o "$OUT" \
    -p:PublishSingleFile=true \
    -p:SelfContained=true
fi

if [[ ! -f "$OUT/DNA/OASIS_DNA.json" ]]; then
  echo "ERROR: After publish, DNA/OASIS_DNA.json is missing in:"
  echo "  $OUT"
  echo "STAR CLI cannot load OASIS DNA from that folder."
  exit 1
fi

echo ""
echo "OK: $OUT/star and DNA/OASIS_DNA.json"
echo "Run: Scripts/STAR CLI/RUN_STAR_CLI.sh"
exit 0
