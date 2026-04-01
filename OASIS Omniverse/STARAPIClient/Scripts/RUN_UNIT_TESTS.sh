#!/usr/bin/env bash
# Run STAR API Client unit tests. Linux equivalent of RUN_UNIT_TESTS.bat.
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
echo "Running STAR API Client unit tests [Release]..."
dotnet test ../TestProjects/NextGenSoftware.OASIS.STARAPI.Client.UnitTests/NextGenSoftware.OASIS.STARAPI.Client.UnitTests.csproj -c Release --no-restore
EXIT_CODE=$?
if [[ $EXIT_CODE -ne 0 ]]; then echo "Unit tests failed with exit code $EXIT_CODE"; fi
echo ""
exit $EXIT_CODE
