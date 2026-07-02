#!/usr/bin/env bash
# Start WEB4 and WEB5 APIs, run test harness, then stop APIs. Linux equivalent of RUN_TEST_HARNESS_WITH_APIS.bat.
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
if [[ ! -f "run_test_harness_with_apis.ps1" ]]; then
  echo "Error: run_test_harness_with_apis.ps1 not found in $(pwd)" >&2
  exit 1
fi
exec pwsh -NoProfile -ExecutionPolicy Bypass -File "run_test_harness_with_apis.ps1"
