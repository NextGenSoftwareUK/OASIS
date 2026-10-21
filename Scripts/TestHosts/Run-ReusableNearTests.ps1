$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if(Test-Path -LiteralPath (Join-Path $root 'TestResults/NearRuntime/near-provider-current.trx')){
    throw 'Preserve and hash-verify the original NEAR native report before another run; no node or mutation started.'
}
$rootWsl=(& wsl.exe -d Ubuntu -- wslpath -a ($root -replace '\\','/')).Trim()
if($LASTEXITCODE -ne 0 -or $rootWsl -ne '/mnt/c/Source/OASIS'){throw 'Unexpected retained NEAR repository root.'}
# No downloads, dependency installs, compilation/deployment of the contract or chain reset.
# Credentials are read only by the retained SDK worker and passed to Windows through stdin.
$nearShell=@'
set -euo pipefail
root=/var/tmp/oasis-near-evidence
repo=/mnt/c/Source/OASIS
runtime=/var/tmp/oasis-runtimes/near-sandbox-2.6.2/near-sandbox
node=/mnt/c/Source/oasis-near-node-22.22.2/bin/node
test -x "$runtime" && test -x "$node"
test "$(realpath "$root")" = "$root" && test -O "$root"
exec 9>"$root/environment.lock"
flock -n 9 || { echo 'Retained NEAR environment is already owned.' >&2; exit 1; }
test -z "$(pgrep -x near-sandbox || true)" || { echo 'Existing NEAR process must be inspected, not duplicated.' >&2; exit 1; }
test -z "$(ss -H -ltn 'sport = :3030 or sport = :24567')" || { echo 'Retained NEAR ports are occupied.' >&2; exit 1; }
pending_original() {
  if test -n "$(find "$root" -maxdepth 1 -name '*.pending.json' -print -quit)" || test -e "$repo/TestResults/NearRuntime/near-checked-sdk.pending.json"; then return 0; fi
  # A negative-fixture submission is also an original mutation, not just the C# writes.
  # Invalid/unreadable state must preserve the node rather than permit another run.
  if ! "$node" -e 'const f=require("node:fs"),p=process.argv[1];if(f.existsSync(p)&&JSON.parse(f.readFileSync(p,"utf8")).state!=="verified-failed")process.exit(1)' "$root/failed-status-current.json"; then return 0; fi
  return 1
}
if pending_original; then echo 'Original NEAR mutation evidence remains; reconcile before startup.' >&2; exit 1; fi
test "$(sha256sum "$root/ledger/genesis.json" | cut -d' ' -f1)" = 44cd89e1518e944f19aec5e2508550f33170ee9196158ec73012fd8e203a6713
cmp "$root/run-evidence.mjs" "$repo/Scripts/TestHosts/near-local/run-evidence.mjs"
"$node" -e 'const c=JSON.parse(require("node:fs").readFileSync(process.argv[1],"utf8"));if(c.gc_num_epochs_to_keep!==50)throw new Error("Native verification needs reviewed temporary retention50; restore original5after stopping.")' "$root/ledger/config.json"
pid=''
cleanup() {
  code=$?
  trap - EXIT
  if test -n "$pid" && kill -0 "$pid" 2>/dev/null; then
    if pending_original; then
      echo "Original pending NEAR mutation: retaining owned node PID $pid for reconciliation." >&2
      exit 1
    fi
    test "$(readlink -f "/proc/$pid/exe")" = "$runtime" || { echo 'Owned PID identity changed; refusing stop.' >&2; exit 1; }
    kill -INT "$pid"
    wait "$pid" || { echo 'Owned NEAR node did not exit cleanly.' >&2; exit 1; }
  fi
  exit "$code"
}
trap cleanup EXIT
export PATH="$(dirname "$node"):$PATH"
"$runtime" --home "$root/ledger" run --rpc-addr 127.0.0.1:3030 --network-addr 127.0.0.1:24567 >"$root/node-current.log" 2>&1 &
pid=$!
echo "Resumed retained NEAR node PID $pid (loopback only)."
ready=false
for attempt in $(seq 1 60); do
  kill -0 "$pid" 2>/dev/null || { echo 'Owned NEAR node exited before readiness.' >&2; exit 1; }
  if curl --silent --fail http://127.0.0.1:3030/status >/dev/null; then ready=true; break; fi
  sleep 0.5
done
test "$ready" = true || { echo 'Retained NEAR readiness timed out; no restart/reset.' >&2; exit 1; }
export OASIS_REPO_ROOT_WSL="$repo"
export OASIS_NEAR_REUSABLE_RPC_URL=http://127.0.0.1:3030
export OASIS_NEAR_REUSABLE_CHAIN_ID=oasis-near-local
export OASIS_NEAR_REUSABLE_CONTRACT_ID=test.near
export OASIS_NEAR_REUSABLE_SIGNER_PATH="$root/ledger/validator_key.json"
cd "$root"
"$node" run-evidence.mjs
'@
# Transport this public script as bytes, avoiding Windows native-argument quoting
# changing Bash quotes/substitutions before its own variable assignments execute.
$nearEncoded=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(($nearShell -replace "`r`n","`n")))
& wsl.exe -d Ubuntu -- bash -lc "printf '%s' '$nearEncoded' | base64 -d | bash"
if($LASTEXITCODE -ne 0){throw 'Retained NEAR verification did not pass; inspect original evidence before rerunning.'}
