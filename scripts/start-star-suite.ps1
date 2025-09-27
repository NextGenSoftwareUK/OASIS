# Requires PowerShell 7+ for Start-Process -NoNewWindow reliability
# Launch Web4 OASIS API, Web5 STAR Web API, and STAR Web UI dev server
# Usage: pwsh -File scripts/start-star-suite.ps1

param(
    [string]$OasisApiUrl = "http://localhost:50563",
    [string]$StarApiUrl = "http://localhost:50564",
    [string]$StarUiPort = "3000"
)

$repoRoot = Resolve-Path ".."
$onodePath = Join-Path $repoRoot "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
$starApiPath = Join-Path $repoRoot "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
$starUiPath = Join-Path $repoRoot "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI/ClientApp"

Write-Host "Starting Web4 OASIS API at $OasisApiUrl" -ForegroundColor Cyan
$onodeProcess = Start-Process dotnet -ArgumentList @("run", "--urls", "$OasisApiUrl") -WorkingDirectory $onodePath -PassThru

Write-Host "Starting Web5 STAR Web API at $StarApiUrl" -ForegroundColor Cyan
$starApiProcess = Start-Process dotnet -ArgumentList @("run", "--urls", "$StarApiUrl") -WorkingDirectory $starApiPath -PassThru

Write-Host "Starting STAR Web UI dev server on port $StarUiPort" -ForegroundColor Cyan
$env:REACT_APP_API_URL = "$StarApiUrl/api"
$env:REACT_APP_WEB4_API_URL = "$OasisApiUrl/api"
$starUiProcess = Start-Process npm -ArgumentList @("start", "--", "--port", $StarUiPort) -WorkingDirectory $starUiPath -PassThru

Write-Host "Processes launched:" -ForegroundColor Green
Write-Host " OASIS API PID: $($onodeProcess.Id)"
Write-Host " STAR API PID: $($starApiProcess.Id)"
Write-Host " STAR UI PID:  $($starUiProcess.Id)"
Write-Host "Use Stop-Process -Id <PID> to terminate." -ForegroundColor Yellow
