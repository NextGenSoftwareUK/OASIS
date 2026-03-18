#!/usr/bin/env bash
# Run STAR API Client test harness (real APIs: WEB5 :5556, WEB4 :5555). Linux equivalent of RUN_TEST_HARNESS.bat.
set -e

# OASIS: pause before exit when run from GUI (CI: OASIS_SCRIPT_NO_PAUSE=1)
if [[ "${OASIS_SCRIPT_NO_PAUSE:-}" != "1" ]]; then
  _OASIS_TD="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  while [[ "$_OASIS_TD" != "/" ]]; do
    if [[ -f "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh" ]]; then
      # shellcheck disable=SC1091
      source "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh"
      break
    fi
    _OASIS_TD="$(dirname "$_OASIS_TD")"
  done
fi

cd "$(dirname "${BASH_SOURCE[0]}")"
export STARAPI_HARNESS_USE_FAKE_SERVER=false
echo "Running STAR API Client test harness [Release] against real APIs (WEB5 localhost:5556, WEB4 localhost:5555)..."
dotnet run --project ../TestProjects/NextGenSoftware.OASIS.STARAPI.Client.TestHarness/NextGenSoftware.OASIS.STARAPI.Client.TestHarness.csproj -c Release
EXIT_CODE=$?
if [[ $EXIT_CODE -ne 0 ]]; then echo "Test harness failed with exit code $EXIT_CODE"; fi
echo ""
exit $EXIT_CODE
