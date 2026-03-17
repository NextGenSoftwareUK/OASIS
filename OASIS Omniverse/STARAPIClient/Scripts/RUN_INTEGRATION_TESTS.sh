#!/usr/bin/env bash
# Run STAR API Client integration tests (real APIs: WEB5 :5556, WEB4 :5555). Linux equivalent of RUN_INTEGRATION_TESTS.bat.
set -e
cd "$(dirname "${BASH_SOURCE[0]}")"
export STARAPI_INTEGRATION_USE_FAKE=false
echo "Running STAR API Client integration tests [Release] against real APIs (WEB5 localhost:5556, WEB4 localhost:5555)..."
dotnet test ../TestProjects/NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests/NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests.csproj -c Release --no-restore
EXIT_CODE=$?
if [[ $EXIT_CODE -ne 0 ]]; then echo "Integration tests failed with exit code $EXIT_CODE"; fi
echo ""
exit $EXIT_CODE
