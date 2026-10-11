#!/usr/bin/env bash
set -euo pipefail

# Run only against the Docker publish directory. Never rebuild shared source caches.
task_publish_root="${1:?Usage: package-provider-sdk-bridges.sh /app/publish}"
[[ "$(realpath "$task_publish_root")" == /app/publish ]] || {
  echo 'SDK installation requires the Docker-owned /app/publish directory.' >&2
  exit 1
}
for task_bridge in CosmosSdkBridge HederaSdkBridge NearSdkBridge SuiSdkBridge; do
  task_path="$task_publish_root/$task_bridge"
  # Some API hosts do not reference every provider. A partial bridge is an error.
  [[ -d "$task_path" ]] || continue
  test -f "$task_path/bridge.mjs"
  test -f "$task_path/package.json"
  test -f "$task_path/package-lock.json"
  npm ci --prefix "$task_path" --omit=dev --no-audit --no-fund
  node "$(dirname "$0")/verify-provider-sdk-bridge.mjs" "$task_path"
done
