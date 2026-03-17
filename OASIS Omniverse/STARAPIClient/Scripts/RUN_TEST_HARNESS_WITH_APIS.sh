#!/usr/bin/env bash
# Start WEB4 and WEB5 APIs, run test harness, then stop APIs. Linux equivalent of RUN_TEST_HARNESS_WITH_APIS.bat.
set -e
cd "$(dirname "${BASH_SOURCE[0]}")"
if [[ ! -f "run_test_harness_with_apis.ps1" ]]; then
  echo "Error: run_test_harness_with_apis.ps1 not found in $(pwd)" >&2
  exit 1
fi
exec pwsh -NoProfile -ExecutionPolicy Bypass -File "run_test_harness_with_apis.ps1"
