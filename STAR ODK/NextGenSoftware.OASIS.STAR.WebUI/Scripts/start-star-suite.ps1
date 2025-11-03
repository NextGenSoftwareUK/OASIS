# Requires PowerShell 7+ for Start-Process -NoNewWindow reliability
# Launch Web4 OASIS API, Web5 STAR Web API, and STAR Web UI dev server
# Usage: pwsh -File scripts/start-star-suite.ps1

param(
    [string]$OasisApiUrl = "http://localhost:50563",
    [string]$StarApiUrl = "http://localhost:50564",
    [string]$StarUiPort = "3000"
)

$repoRoot = Resolve-Path "../../../"
$onodePath = Join-Path $repoRoot "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
$starApiPath = Join-Path $repoRoot "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
$starUiPath = Join-Path $repoRoot "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI/ClientApp"

Write-Host "🌐 Starting Web4 OASIS API at $OasisApiUrl" -ForegroundColor Green
$onodeProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$onodePath'; Write-Host 'Web4 OASIS API Starting...' -ForegroundColor Green; dotnet run --urls $OasisApiUrl" -PassThru

Write-Host "⭐ Starting Web5 STAR Web API at $StarApiUrl" -ForegroundColor Magenta  
$starApiProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$starApiPath'; Write-Host 'Web5 STAR API Starting...' -ForegroundColor Magenta; dotnet run --urls $StarApiUrl" -PassThru

Write-Host "🎨 Starting STAR Web UI dev server on port $StarUiPort" -ForegroundColor Blue
$starUiProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$starUiPath'; `$env:REACT_APP_API_URL='$StarApiUrl/api'; `$env:REACT_APP_WEB4_API_URL='$OasisApiUrl/api'; Write-Host 'STAR Web UI Starting...' -ForegroundColor Blue; npm start -- --port $StarUiPort" -PassThru

Write-Host ""
Write-Host "✅ All services launched in separate windows!" -ForegroundColor Green
Write-Host "📊 Process IDs: OASIS=$($onodeProcess.Id), STAR=$($starApiProcess.Id), UI=$($starUiProcess.Id)" -ForegroundColor Gray
Write-Host ""
Write-Host "🔗 Access URLs:" -ForegroundColor Cyan
Write-Host "   • Web4 OASIS API: $OasisApiUrl" -ForegroundColor White
Write-Host "   • Web5 STAR API:  $StarApiUrl" -ForegroundColor White  
Write-Host "   • STAR Web UI:    http://localhost:$StarUiPort" -ForegroundColor White
Write-Host ""
Write-Host "💡 Each service runs in its own PowerShell window." -ForegroundColor Yellow
Write-Host "Press any key to exit launcher..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
