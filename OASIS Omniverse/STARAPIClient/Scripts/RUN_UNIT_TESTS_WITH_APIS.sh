#!/usr/bin/env bash
# Start WEB4 and WEB5 APIs, run STAR API Client unit tests, then stop APIs. Linux equivalent of RUN_UNIT_TESTS_WITH_APIS.bat.
set -e
cd "$(dirname "${BASH_SOURCE[0]}")"
if [[ ! -f "run_unit_tests_with_apis.ps1" ]]; then
  echo "Error: run_unit_tests_with_apis.ps1 not found in $(pwd)" >&2
  exit 1
fi
exec pwsh -NoProfile -ExecutionPolicy Bypass -File "run_unit_tests_with_apis.ps1"