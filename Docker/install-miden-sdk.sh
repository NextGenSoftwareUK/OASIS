#!/bin/sh
# Install the locked official SDK beside the published bridge, never at service startup.
set -eu
bridge=$1
test -f "$bridge/package.json"
test -f "$bridge/package-lock.json"
test -f "$bridge/worker.mjs"
test -f "$bridge/oasis_kv.masm"
cache=$(mktemp -d "${TMPDIR:-/tmp}/oasis-miden-npm.XXXXXXXX")
cleanup() {
  case "$cache" in "${TMPDIR:-/tmp}"/oasis-miden-npm.*) rm -rf -- "$cache" ;; *) exit 1 ;; esac
}
trap cleanup EXIT HUP INT TERM
npm ci --prefix "$bridge" --omit=dev --no-audit --no-fund --cache "$cache"
(cd "$bridge" && node --input-type=module -e '
  import {readFileSync} from "node:fs";
  const expected = JSON.parse(readFileSync("package.json", "utf8")).dependencies["@miden-sdk/miden-sdk"];
  const actual = JSON.parse(readFileSync("node_modules/@miden-sdk/miden-sdk/package.json", "utf8")).version;
  if (actual !== expected) throw new Error("Miden SDK revision differs from the locked bridge contract.");
  const sdk = await import("@miden-sdk/miden-sdk");
  for (const name of ["AccountBuilder", "AccountComponent", "AccountId", "AccountStorageMode", "AuthSecretKey", "MidenClient", "Note", "StorageMap", "StorageSlot", "Word"])
    if (sdk[name] === undefined) throw new Error("Missing shipped Miden SDK export: " + name);
  console.log("Verified the installed Miden SDK " + actual + " and bridge exports.");
')
