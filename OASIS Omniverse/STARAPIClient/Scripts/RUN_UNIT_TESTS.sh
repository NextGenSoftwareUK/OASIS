#!/usr/bin/env bash
# Run STAR API Client unit tests. Linux equivalent of RUN_UNIT_TESTS.bat.
set -e
cd "$(dirname "${BASH_SOURCE[0]}")"
echo "Running STAR API Client unit tests [Release]..."
dotnet test ../TestProjects/NextGenSoftware.OASIS.STARAPI.Client.UnitTests/NextGenSoftware.OASIS.STARAPI.Client.UnitTests.csproj -c Release --no-restore
EXIT_CODE=$?
if [[ $EXIT_CODE -ne 0 ]]; then echo "Unit tests failed with exit code $EXIT_CODE"; fi
echo ""
exit $EXIT_CODE
