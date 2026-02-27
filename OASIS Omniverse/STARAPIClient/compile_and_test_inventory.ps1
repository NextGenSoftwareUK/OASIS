param(
    [string]$Runtime = "win-x64",
    [string]$BaseUrl = "http://127.0.0.1:65535/api",
    [string]$Username = "",
    [string]$Password = "",
    [string]$ApiKey = "",
    [string]$AvatarId = "",
    [string]$SendAvatarTarget = "",
    [string]$SendClanName = "",
    [bool]$RebuildClient = $true,
    [switch]$BuildOnly
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$nativeDir = Join-Path $scriptDir "bin/Release/net8.0/$Runtime/native"
$publishDir = Join-Path $scriptDir "bin/Release/net8.0/$Runtime/publish"
$testSource = Join-Path $scriptDir "test_inventory.c"
$testExe = Join-Path $scriptDir "test_inventory.exe"

# Prompt to rebuild client when not explicitly passed (default Y)
if (-not $PSBoundParameters.ContainsKey('RebuildClient'))
{
    $response = Read-Host "Rebuild STAR API client first? [Y/n]"
    if ($response -eq 'n' -or $response -eq 'N') { $RebuildClient = $false }
    else { $RebuildClient = $true }
}

if ($RebuildClient)
{
    Write-Host "Rebuilding STAR API client (dotnet publish)..." -ForegroundColor Yellow
    $csproj = Join-Path $scriptDir "STARAPIClient.csproj"
    if (!(Test-Path $csproj)) { throw "Missing project: $csproj" }
    & dotnet publish $csproj -c Release -r $Runtime -p:PublishAot=true -p:SelfContained=true -p:NoWarn=NU1605
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    Write-Host "[OK] STAR API client published." -ForegroundColor Green
}

if (!(Test-Path $testSource)) { throw "Missing test source: $testSource" }
if (!(Test-Path (Join-Path $nativeDir "star_api.lib"))) { throw "Missing import lib: $nativeDir/star_api.lib" }
if (!(Test-Path (Join-Path $publishDir "star_api.dll"))) { throw "Missing DLL: $publishDir/star_api.dll" }

$defaultVsWhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
$vsWhere = $null
if (Test-Path $defaultVsWhere)
{
    $vsWhere = $defaultVsWhere
}
else
{
    $vsWhereCommand = Get-Command vswhere.exe -ErrorAction SilentlyContinue
    if ($vsWhereCommand -ne $null)
    {
        $vsWhere = $vsWhereCommand.Source
    }
}
if ([string]::IsNullOrWhiteSpace($vsWhere)) { throw "Could not find vswhere.exe. Install Visual Studio Build Tools or add vswhere to PATH." }

$vsInstallPath = & $vsWhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if ([string]::IsNullOrWhiteSpace($vsInstallPath)) { throw "No Visual Studio installation with C++ toolchain component found." }

$vcvars = Join-Path $vsInstallPath "VC\Auxiliary\Build\vcvars64.bat"
if (!(Test-Path $vcvars)) { throw "vcvars64.bat not found at expected path: $vcvars" }

Write-Host "Using Visual Studio at: $vsInstallPath" -ForegroundColor Cyan
Write-Host "Compiling inventory test with cl.exe..." -ForegroundColor Yellow

$compileCmd = @(
    "call `"$vcvars`"",
    "cl.exe /nologo /W3 /I`"$scriptDir`" `"$testSource`" /link /LIBPATH:`"$nativeDir`" star_api.lib /OUT:`"$testExe`""
) -join " && "

Push-Location $scriptDir
try
{
    cmd.exe /c $compileCmd
}
finally
{
    Pop-Location
}

if (!(Test-Path $testExe)) { throw "Test executable was not produced: $testExe" }

Copy-Item (Join-Path $publishDir "star_api.dll") (Join-Path $scriptDir "star_api.dll") -Force
Write-Host "`n[OK] Inventory test built at: $testExe" -ForegroundColor Green

if ($BuildOnly)
{
    Write-Host "`nBuild-only mode. Skipping test execution." -ForegroundColor Yellow
    Write-Host "To run the test manually:" -ForegroundColor Yellow
    Write-Host '  .\test_inventory.exe <base_url> <username> <password> [api_key] [avatar_id] [send_avatar_target] [send_clan_name]' -ForegroundColor Cyan
    return
}

if ([string]::IsNullOrWhiteSpace($Username) -or [string]::IsNullOrWhiteSpace($Password))
{
    Write-Host "`n[WARNING] Username and password are required to run the test." -ForegroundColor Yellow
    Write-Host 'Usage: .\compile_and_test_inventory.ps1 -BaseUrl <url> -Username <user> -Password <pass> [options]' -ForegroundColor Cyan
    Write-Host "`nOr run manually:" -ForegroundColor Yellow
    Write-Host "  .\test_inventory.exe [base_url] [username] [password] [api_key] [avatar_id] [send_avatar_target] [send_clan_name]" -ForegroundColor Cyan
    return
}

Write-Host "`nRunning inventory test..." -ForegroundColor Yellow
Write-Host "Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "Username: $Username" -ForegroundColor Cyan
Write-Host "`n" -NoNewline

$testArgs = @($BaseUrl, $Username, $Password)
if (![string]::IsNullOrWhiteSpace($ApiKey)) { $testArgs += $ApiKey }
if (![string]::IsNullOrWhiteSpace($AvatarId)) { $testArgs += $AvatarId }
if (![string]::IsNullOrWhiteSpace($SendAvatarTarget)) { $testArgs += $SendAvatarTarget }
if (![string]::IsNullOrWhiteSpace($SendClanName)) { $testArgs += $SendClanName }

& $testExe $testArgs
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0)
{
    Write-Host "`n[OK] Test completed successfully!" -ForegroundColor Green
}
else
{
    Write-Host "`n[FAILED] Test failed with exit code: $exitCode" -ForegroundColor Red
}

exit $exitCode

