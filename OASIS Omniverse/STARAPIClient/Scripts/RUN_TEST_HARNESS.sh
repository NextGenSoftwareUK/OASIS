#!/usr/bin/env bash
# Run STAR API Client test harness (real APIs: WEB5 :5556, WEB4 :5555). Linux equivalent of RUN_TEST_HARNESS.bat.
set -e
cd "$(dirname "${BASH_SOURCE[0]}")"
export STARAPI_HARNESS_USE_FAKE_SERVER=false
echo "Running STAR API Client test harness [Release] against real APIs (WEB5 localhost:5556, WEB4 localhost:5555)..."
dotnet run --project ../TestProjects/NextGenSoftware.OASIS.STARAPI.Client.TestHarness/NextGenSoftware.OASIS.STARAPI.Client.TestHarness.csproj -c Release
EXIT_CODE=$?
if [[ $EXIT_CODE -ne 0 ]]; then echo "Test harness failed with exit code $EXIT_CODE"; fi
echo ""
exit $EXIT_CODE
