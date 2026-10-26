param([switch]$InspectInterruptedRun,[switch]$InspectStorageRecords,[switch]$VerifySignedAcknowledgements,[switch]$VerifyCheckedWrites,[switch]$VerifyOrdinaryWrites)
$ErrorActionPreference='Stop'
if($VerifyOrdinaryWrites -and ($InspectInterruptedRun -or $InspectStorageRecords -or $VerifySignedAcknowledgements -or $VerifyCheckedWrites)){throw 'Choose one explicit native verification scope.'}
if($InspectStorageRecords -and !$InspectInterruptedRun){throw 'Storage inspection requires explicit read-only inspection mode.'}
if($VerifySignedAcknowledgements -and $InspectInterruptedRun){throw 'Signed verification is not read-only inspection.'}
if($VerifyCheckedWrites -and ($InspectInterruptedRun -or $VerifySignedAcknowledgements)){throw 'Choose one explicit native verification scope.'}
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if($VerifyOrdinaryWrites -and (Test-Path -LiteralPath (Join-Path $root 'Docs/WIP/HyperDrive-Providers/NativeEvidence/near-ordinary-storage.complete.json'))){
    throw 'Original ordinary storage cohort is already complete; reconcile its retained receipts, never replay.'
}
if(!$InspectInterruptedRun -and !$VerifySignedAcknowledgements -and !$VerifyCheckedWrites -and !$VerifyOrdinaryWrites -and (Test-Path -LiteralPath (Join-Path $root 'TestResults/NearRuntime/near-provider-current.trx'))){
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
  if test -n "$(find "$root" -maxdepth 1 -name '*.pending.json' -print -quit)" || test -e "$repo/TestResults/NearRuntime/near-checked-sdk.pending.json" || test -e "$repo/TestResults/NearRuntime/near-signed-ack.pending.json" || test -e "$repo/TestResults/NearRuntime/near-ordinary-storage.pending.json"; then return 0; fi
  # A negative-fixture submission is also an original mutation, not just the C# writes.
  # Invalid/unreadable state must preserve the node rather than permit another run.
  if ! "$node" -e 'const f=require("node:fs"),p=process.argv[1];if(f.existsSync(p)&&JSON.parse(f.readFileSync(p,"utf8")).state!=="verified-failed")process.exit(1)' "$root/failed-status-current.json"; then return 0; fi
  return 1
}
if test "$OASIS_NEAR_READ_ONLY_INSPECTION" != 1 && pending_original; then echo 'Original NEAR mutation evidence remains; reconcile before startup.' >&2; exit 1; fi
test "$(sha256sum "$root/ledger/genesis.json" | cut -d' ' -f1)" = 44cd89e1518e944f19aec5e2508550f33170ee9196158ec73012fd8e203a6713
cmp "$root/run-evidence.mjs" "$repo/Scripts/TestHosts/near-local/run-evidence.mjs"
"$node" -e 'const c=JSON.parse(require("node:fs").readFileSync(process.argv[1],"utf8")),expected=process.env.OASIS_NEAR_READ_ONLY_INSPECTION==="1"||process.env.OASIS_NEAR_SIGNED_ACK==="1"||process.env.OASIS_NEAR_CHECKED_WRITE==="1"||process.env.OASIS_NEAR_ORDINARY_WRITE==="1"?5:50;if(c.gc_num_epochs_to_keep!==expected)throw new Error("Unexpected retained NEAR retention for selected scope.")' "$root/ledger/config.json"
pid=''
cleanup() {
  code=$?
  trap - EXIT
  if test -n "$pid" && kill -0 "$pid" 2>/dev/null; then
    if test "$OASIS_NEAR_READ_ONLY_INSPECTION" != 1 && pending_original; then
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
node_log="$root/node-current.log"
if test "$OASIS_NEAR_READ_ONLY_INSPECTION" = 1; then node_log="$root/node-inspect-current.log"; fi
if test "$OASIS_NEAR_SIGNED_ACK" = 1; then node_log="$root/node-signed-ack.log"; test ! -e "$node_log" || { echo 'Preserve original signed node log before another run.' >&2; exit 1; }; fi
if test "$OASIS_NEAR_CHECKED_WRITE" = 1; then node_log="$root/node-checked-compatibility.log"; test ! -e "$node_log" || { echo 'Preserve original checked node log before another run.' >&2; exit 1; }; fi
if test "$OASIS_NEAR_ORDINARY_WRITE" = 1; then node_log="$root/node-ordinary-storage.log"; test ! -e "$node_log" || { echo 'Preserve original ordinary-storage node log before another run.' >&2; exit 1; }; fi
"$runtime" --home "$root/ledger" run --rpc-addr 127.0.0.1:3030 --network-addr 127.0.0.1:24567 >"$node_log" 2>&1 &
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
if test "$OASIS_NEAR_SIGNED_ACK" = 1 || test "$OASIS_NEAR_CHECKED_WRITE" = 1 || test "$OASIS_NEAR_ORDINARY_WRITE" = 1; then
  "$node" "$repo/Scripts/TestHosts/near-local/run-signed-ack.mjs"
elif test "$OASIS_NEAR_READ_ONLY_INSPECTION" = 1; then
  "$node" "$repo/Scripts/TestHosts/near-local/inspect-interrupted.mjs"
else
  "$node" run-evidence.mjs
fi
'@
# Transport this public script as bytes, avoiding Windows native-argument quoting
# changing Bash quotes/substitutions before its own variable assignments execute.
$inspectionValue=if($InspectInterruptedRun){'1'}else{'0'}
$storageInspectionValue=if($InspectStorageRecords){'1'}else{'0'}
$signedAckValue=if($VerifySignedAcknowledgements){'1'}else{'0'}
$ordinaryWriteValue=if($VerifyOrdinaryWrites){'1'}else{'0'}
$checkedWriteValue=if($VerifyCheckedWrites){'1'}else{'0'}
$nearShell="export OASIS_NEAR_READ_ONLY_INSPECTION=$inspectionValue`nexport OASIS_NEAR_READ_ONLY_STORAGE=$storageInspectionValue`nexport OASIS_NEAR_SIGNED_ACK=$signedAckValue`nexport OASIS_NEAR_CHECKED_WRITE=$checkedWriteValue`nexport OASIS_NEAR_ORDINARY_WRITE=$ordinaryWriteValue`n"+$nearShell
$nearEncoded=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(($nearShell -replace "`r`n","`n")))
& wsl.exe -d Ubuntu -- bash -lc "printf '%s' '$nearEncoded' | base64 -d | bash"
if($LASTEXITCODE -ne 0){throw 'Retained NEAR verification did not pass; inspect original evidence before rerunning.'}
