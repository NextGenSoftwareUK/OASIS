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
  (cd "$task_path" && node --input-type=module -e '
    import fs from "node:fs";
    const manifest = JSON.parse(fs.readFileSync("package.json", "utf8"));
    const source = fs.readFileSync("bridge.mjs", "utf8");
    const modules = [...source.matchAll(/\bfrom\s+["\x27]([^"\x27]+)["\x27]/g)].map(match => match[1]);
    for (const name of new Set(modules)) await import(name);
    for (const dependency of Object.keys(manifest.dependencies ?? {})) {
      if (!modules.some(name => name === dependency || name.startsWith(`${dependency}/`)))
        throw new Error(`Declared SDK ${dependency} has no verified bridge import`);
    }
    console.log(`${manifest.name}: SDK imports verified`);
  ')
done
