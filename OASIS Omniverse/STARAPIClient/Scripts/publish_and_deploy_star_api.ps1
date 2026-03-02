param(
    [string]$Runtime = "win-x64",
    [switch]$RunSmokeTest,
    [switch]$ForceBuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir ".."
$projectPath = Join-Path $projectDir "STARAPIClient.csproj"
$publishDir = Join-Path $projectDir "bin/Release/net8.0/$Runtime/publish"
$nativeDir = Join-Path $projectDir "bin/Release/net8.0/$Runtime/native"
$dllPath = Join-Path $publishDir "star_api.dll"
$libPath = Join-Path $nativeDir "star_api.lib"
$headerPath = Join-Path $projectDir "star_api.h"

# Build only when: forced, or dll missing, or any project source (.cs / .csproj) or star_api.h is newer than star_api.dll
$needBuild = $ForceBuild
if (!$needBuild -and (Test-Path $dllPath)) {
    $dllTime = (Get-Item $dllPath).LastWriteTimeUtc
    $sources = @(Get-ChildItem -Path $projectDir -Filter "*.cs" -Recurse -File -ErrorAction SilentlyContinue) +
               @(Get-ChildItem -Path $projectDir -Filter "*.csproj" -Recurse -File -ErrorAction SilentlyContinue)
    if (Test-Path $headerPath) { $sources += Get-Item $headerPath }
    foreach ($f in $sources) {
        if ($f.LastWriteTimeUtc -gt $dllTime) {
            $needBuild = $true
            break
        }
    }
}
if (!$needBuild -and (Test-Path $dllPath)) {
    Write-Host "STARAPIClient unchanged (star_api.dll is up to date), skipping build."
} else {
    if (!(Test-Path $dllPath)) { Write-Host "STARAPIClient not built yet or output missing; building..." }
    Write-Host "Publishing NativeAOT WEB5 STAR API wrapper..."
    dotnet publish $projectPath -c Release -r $Runtime -p:PublishAot=true -p:SelfContained=true -p:NoWarn=NU1605
}

if (!(Test-Path $dllPath)) { throw "Missing output: $dllPath" }
if (!(Test-Path $libPath)) { throw "Missing output: $libPath" }
if (!(Test-Path $headerPath)) { throw "Missing header: $headerPath" }

$targets = @(
    (Join-Path $scriptDir "..\..\ODOOM"),
    (Join-Path $scriptDir "..\..\OQuake"),
    "C:\Source\UZDoom\src",
    "C:\Source\vkQuake\Quake"
)

foreach ($target in $targets)
{
    $resolvedTarget = [System.IO.Path]::GetFullPath($target)
    if (Test-Path $resolvedTarget)
    {
        Write-Host "Deploying WEB5 STAR API wrapper artifacts to $resolvedTarget"
        Copy-Item $dllPath (Join-Path $resolvedTarget "star_api.dll") -Force
        Copy-Item $libPath (Join-Path $resolvedTarget "star_api.lib") -Force
        Copy-Item $headerPath (Join-Path $resolvedTarget "star_api.h") -Force
    }
}

if ($RunSmokeTest)
{
    $smokeSource = Join-Path $projectDir "TestProjects\smoke_test.c"
    $smokeExe = Join-Path $projectDir "TestProjects\smoke_test.exe"

    Write-Host "Compiling smoke test..."
    Push-Location $projectDir
    try
    {
        if (Get-Command cl.exe -ErrorAction SilentlyContinue)
        {
            & cl.exe /nologo /W3 /I"$projectDir" "$smokeSource" /link /LIBPATH:"$nativeDir" star_api.lib /OUT:"$smokeExe"
        }
        elseif (Get-Command gcc.exe -ErrorAction SilentlyContinue)
        {
            & gcc.exe "$smokeSource" -I"$projectDir" "$libPath" -o "$smokeExe"
        }
        else
        {
            Write-Warning "No C compiler found (cl.exe/gcc.exe). Skipping smoke test compile."
            return
        }
    }
    finally
    {
        Pop-Location
    }

    if (Test-Path $smokeExe)
    {
        Write-Host "Running smoke test..."
        & $smokeExe
    }
    else
    {
        Write-Warning "Smoke test executable was not produced."
    }
}

Write-Host "WEB5 STAR API wrapper publish/deploy complete."

