#!/usr/bin/env bash
# Run unit, integration, and test harness. Linux equivalent of RUN_ALL_TESTS.bat.
# Uses real APIs (WEB5 :5556, WEB4 :5555) unless STARAPI_INTEGRATION_USE_FAKE=true / STARAPI_HARNESS_USE_FAKE_SERVER=true.
set -e
cd "$(dirname "${BASH_SOURCE[0]}")"
export STARAPI_INTEGRATION_USE_FAKE=false
export STARAPI_HARNESS_USE_FAKE_SERVER=false
TOTAL_EXIT=0

echo "========== 1/3 Unit tests =========="
dotnet test ../TestProjects/NextGenSoftware.OASIS.STARAPI.Client.UnitTests/NextGenSoftware.OASIS.STARAPI.Client.UnitTests.csproj -c Release --no-restore || TOTAL_EXIT=1

echo ""
echo "========== 2/3 Integration tests (real APIs: WEB5 :5556, WEB4 :5555) =========="
dotnet test ../TestProjects/NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests/NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests.csproj -c Release --no-restore || TOTAL_EXIT=1

echo ""
echo "========== 3/3 Test harness (real APIs: WEB5 :5556, WEB4 :5555) =========="
dotnet run --project ../TestProjects/NextGenSoftware.OASIS.STARAPI.Client.TestHarness/NextGenSoftware.OASIS.STARAPI.Client.TestHarness.csproj -c Release || TOTAL_EXIT=1

echo ""
if [[ $TOTAL_EXIT -eq 0 ]]; then
  echo "All tests passed."
else
  echo "One or more test runs failed."
fi
echo ""
exit $TOTAL_EXIT
