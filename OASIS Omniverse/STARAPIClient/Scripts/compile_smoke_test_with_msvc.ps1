param(
    [string]$Runtime = "win-x64",
    [switch]$Run
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$nativeDir = Join-Path $scriptDir "bin/Release/net8.0/$Runtime/native"
$publishDir = Join-Path $scriptDir "bin/Release/net8.0/$Runtime/publish"
$smokeSource = Join-Path $scriptDir "smoke_test.c"
$smokeExe = Join-Path $scriptDir "smoke_test.exe"

if (!(Test-Path $smokeSource)) { throw "Missing smoke test source: $smokeSource" }
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

Write-Host "Using Visual Studio at: $vsInstallPath"
Write-Host "Compiling smoke test with cl.exe..."

$compileCmd = @(
    "call `"$vcvars`"",
    "cl.exe /nologo /W3 /I`"$scriptDir`" `"$smokeSource`" /link /LIBPATH:`"$nativeDir`" star_api.lib /OUT:`"$smokeExe`""
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

if (!(Test-Path $smokeExe)) { throw "Smoke test executable was not produced: $smokeExe" }

Copy-Item (Join-Path $publishDir "star_api.dll") (Join-Path $scriptDir "star_api.dll") -Force
Write-Host "Smoke test built at: $smokeExe"

if ($Run)
{
    Write-Host "Running smoke test..."
    & $smokeExe
}

