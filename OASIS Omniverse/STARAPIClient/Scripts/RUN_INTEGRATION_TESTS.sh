#!/usr/bin/env bash
# Run STAR API Client integration tests (real APIs: WEB5 :5556, WEB4 :5555). Linux equivalent of RUN_INTEGRATION_TESTS.bat.
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
export STARAPI_INTEGRATION_USE_FAKE=false
echo "Running STAR API Client integration tests [Release] against real APIs (WEB5 localhost:5556, WEB4 localhost:5555)..."
dotnet test ../TestProjects/NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests/NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests.csproj -c Release --no-restore
EXIT_CODE=$?
if [[ $EXIT_CODE -ne 0 ]]; then echo "Integration tests failed with exit code $EXIT_CODE"; fi
echo ""
exit $EXIT_CODE
