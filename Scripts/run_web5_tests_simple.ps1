$ErrorActionPreference = "Continue"
cd "C:\Source\OASIS-master\STAR ODK\TestProjects\NextGenSoftware.OASIS.STAR.WebAPI.TestHarness"
$output = dotnet run --configuration Release -- --Web5BaseUrl="http://localhost:5556" 2>&1
$output | Out-File -FilePath "C:\Source\OASIS-master\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI\Test Results\test_results_web5.txt" -Encoding utf8
Write-Host "Tests completed. Results saved to Test Results directory."

