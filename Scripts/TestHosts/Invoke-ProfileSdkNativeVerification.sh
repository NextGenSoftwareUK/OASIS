#!/usr/bin/env bash
set -euo pipefail
: "${RUNNER_TEMP:?Owned CI temporary root is required}"
owned_root="$(mktemp -d "$RUNNER_TEMP/oasis-profile-sdk-XXXXXXXX")"
case "$owned_root" in "$RUNNER_TEMP"/oasis-profile-sdk-*) ;; *) exit 1 ;; esac
chmod 700 "$owned_root"
export OASIS_PROFILE_SDK_HANDOFF_ROOT="$owned_root"
node Scripts/TestHosts/Fixtures/profile-sdk-native-client.cjs >"$owned_root/client.log" 2>&1 &
sdk_pid=$!
cleanup() {
  result=$?
  trap - EXIT
  # This is the unreaped child started by this shell; its PID cannot be reused.
  if [ "$result" -ne 0 ] && kill -0 "$sdk_pid" 2>/dev/null; then
    kill "$sdk_pid"
  fi
  if wait "$sdk_pid"; then :; else result=1; fi
  if [ -s "$owned_root/client.log" ]; then cat "$owned_root/client.log"; fi
  # Remove only protocol files in this verified, freshly owned directory.
  for file in "$owned_root"/*; do
    [ -e "$file" ] || continue
    name="${file##*/}"
    if [[ "$name" == client.log || "$name" =~ ^[0-9a-f]{32}\.(request|response)\.json(\.tmp)?$ ]]; then
      rm -- "$file"
    else
      echo "Unexpected file in owned SDK handoff directory; cleanup refused."
      result=1
    fi
  done
  if rmdir -- "$owned_root"; then :; else result=1; fi
  exit "$result"
}
trap cleanup EXIT
pwsh -NoLogo -NoProfile -Command '
  & Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner profile-sdk-native-ci -Operation {
    dotnet test "OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests" --configuration Release /m:2 /nr:false /p:UseSharedCompilation=false --filter "$env:OASIS_NATIVE_PROVIDER_TEST_FILTER" --logger "trx;LogFileName=profile-sdk-native.trx" --results-directory artifacts/hyperdrive-contract-validation
    if ($LASTEXITCODE) { throw "Actual JavaScript profile SDK verification failed." }
  }
'
