# Start WEB4 and WEB5 APIs in serial (WEB4 first, then WEB5), run both API unit tests, then stop the APIs.
# Use run_web4_web5_unit_tests.ps1 if you do not need the APIs running.

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
Push-Location $repoRoot | Out-Null

$web4Process = $null
$web5Process = $null

try {
    Write-Host "Building APIs..." -ForegroundColor Cyan
    dotnet build "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -v q
    dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj" -c Release -v q

    Write-Host "Starting WEB4 OASIS API (serial: first)..." -ForegroundColor Cyan
    $web4Process = Start-Process -FilePath "dotnet" -ArgumentList "run --no-launch-profile --project `"ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj`" -c Release --urls `"http://localhost:5003`"" -PassThru -WindowStyle Hidden

    Write-Host "Waiting for WEB4 to be ready..." -ForegroundColor Yellow
    $web4Ready = $false
    for ($i = 0; $i -lt 120; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5003/api/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) { $web4Ready = $true; Write-Host "WEB4 is ready!" -ForegroundColor Green; break }
        } catch { Start-Sleep -Seconds 1 }
    }
    if (-not $web4Ready) {
        Write-Host "WEB4 failed to start" -ForegroundColor Red
        if ($web4Process) { Stop-Process -Id $web4Process.Id -Force -ErrorAction SilentlyContinue }
        exit 1
    }

    Write-Host "Starting WEB5 STAR API (serial: second)..." -ForegroundColor Cyan
    $web5Process = Start-Process -FilePath "dotnet" -ArgumentList "run --no-launch-profile --project `"STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj`" -c Release --urls `"http://localhost:5055`"" -PassThru -WindowStyle Hidden

    Write-Host "Waiting for WEB5 to be ready..." -ForegroundColor Yellow
    $web5Ready = $false
    for ($i = 0; $i -lt 120; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5055/api/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) { $web5Ready = $true; Write-Host "WEB5 is ready!" -ForegroundColor Green; break }
        } catch { Start-Sleep -Seconds 1 }
    }
    if (-not $web5Ready) {
        Write-Host "WEB5 failed to start" -ForegroundColor Red
        if ($web4Process) { Stop-Process -Id $web4Process.Id -Force -ErrorAction SilentlyContinue }
        if ($web5Process) { Stop-Process -Id $web5Process.Id -Force -ErrorAction SilentlyContinue }
        exit 1
    }

    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  WEB4 & WEB5 API Unit Tests (Serial)         " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host ""

    $web4Project = "ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests\NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj"
    $web5Project = "STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI.UnitTests\NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.csproj"

    Write-Host "  WEB4 (ONODE) unit tests..." -ForegroundColor Cyan
    $null = dotnet test $web4Project -m:1 -v n --logger "console;verbosity=minimal"
    $web4Exit = $LASTEXITCODE

    Write-Host ""
    Write-Host "  WEB5 (STAR) unit tests..." -ForegroundColor Cyan
    $null = dotnet test $web5Project -m:1 -v n --logger "console;verbosity=minimal"
    $web5Exit = $LASTEXITCODE

    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  Summary                                     " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  WEB4 (ONODE): $(if ($web4Exit -eq 0) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(if ($web4Exit -eq 0) { 'Green' } else { 'Red' })
    Write-Host "  WEB5 (STAR):  $(if ($web5Exit -eq 0) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(if ($web5Exit -eq 0) { 'Green' } else { 'Red' })
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host ""

    $overallExit = if ($web4Exit -ne 0 -or $web5Exit -ne 0) { 1 } else { 0 }
    exit $overallExit
}
finally {
    Write-Host "Stopping APIs..." -ForegroundColor Yellow
    if ($web5Process -and !$web5Process.HasExited) { Stop-Process -Id $web5Process.Id -Force -ErrorAction SilentlyContinue }
    if ($web4Process -and !$web4Process.HasExited) { Stop-Process -Id $web4Process.Id -Force -ErrorAction SilentlyContinue }
    Pop-Location | Out-Null
}
