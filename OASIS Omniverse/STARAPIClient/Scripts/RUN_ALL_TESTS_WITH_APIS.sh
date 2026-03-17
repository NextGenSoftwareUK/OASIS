#!/usr/bin/env bash
# Start WEB4 and WEB5 APIs, run unit + integration + harness, then stop APIs. Linux equivalent of RUN_ALL_TESTS_WITH_APIS.bat.
set -e
cd "$(dirname "${BASH_SOURCE[0]}")"
if [[ ! -f "run_all_tests_with_apis.ps1" ]]; then
  echo "Error: run_all_tests_with_apis.ps1 not found in $(pwd)" >&2
  exit 1
fi
exec pwsh -NoProfile -ExecutionPolicy Bypass -File "run_all_tests_with_apis.ps1"
