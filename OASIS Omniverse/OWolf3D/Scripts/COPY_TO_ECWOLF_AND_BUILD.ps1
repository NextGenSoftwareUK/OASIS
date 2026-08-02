<#
.SYNOPSIS
    Copies OWolf3D STAR integration files into the ECWolf source tree and builds.
.PARAMETER BuildType
    Release (default) or Debug
.PARAMETER BatchMode
    Suppress interactive prompts
#>
param(
    [string]$BuildType  = "Release",
    [switch]$BatchMode
)

$ErrorActionPreference = "Stop"

$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$OasisDir   = Split-Path -Parent $ScriptDir
$OGLibDir   = Join-Path (Split-Path -Parent $OasisDir) "OGLib"
$StarDir    = Join-Path (Split-Path -Parent $OasisDir) "OGEngineClient"
$ECWolfSrc  = if ($env:OWOLF3D_SRC) { $env:OWOLF3D_SRC } else { "C:\Source\OWolf3D" }
$BuildDir   = Join-Path $ECWolfSrc "build-vs2019-win64"
$SrcDst     = Join-Path $ECWolfSrc "src"

Write-Host "`n=== OWolf3D — OASIS STAR Integration Build ===" -ForegroundColor Cyan
Write-Host "ECWolf source : $ECWolfSrc"
Write-Host "Build type    : $BuildType"

# ── Verify ECWolf source ───────────────────────────────────────────────────
if (-not (Test-Path $ECWolfSrc)) {
    Write-Error "ECWolf source not found at $ECWolfSrc`nSet OWOLF3D_SRC env var or clone to that path."
}

# ── Copy integration files ─────────────────────────────────────────────────
Write-Host "`n[1/4] Copying integration files..." -ForegroundColor Yellow

Copy-Item (Join-Path $OasisDir "owolf3d_ogengine_integration.h")   $SrcDst -Force
Copy-Item (Join-Path $OasisDir "owolf3d_ogengine_integration.cpp") $SrcDst -Force
Copy-Item (Join-Path $OasisDir "oasisstar.json")               $SrcDst -Force

# OGLib headers
$OGLibDst = Join-Path $SrcDst "OGLib"
if (-not (Test-Path $OGLibDst)) { New-Item -ItemType Directory -Path $OGLibDst | Out-Null }
Get-ChildItem -Path $OGLibDir -Filter "*.h" | ForEach-Object {
    Copy-Item $_.FullName $OGLibDst -Force
}

# STAR API headers + lib
foreach ($f in @("ogengine.h","star_sync.h","ogengine.lib","ogengine.dll")) {
    $src = Join-Path $StarDir $f
    if (Test-Path $src) { Copy-Item $src $SrcDst -Force }
}

Write-Host "  Files copied to $SrcDst"

# ── Patch CMakeLists.txt ────────────────────────────────────────────────────
Write-Host "`n[2/4] Patching CMakeLists.txt..." -ForegroundColor Yellow

$cmake = Join-Path $SrcDst "CMakeLists.txt"
$content = Get-Content $cmake -Raw

if ($content -notmatch "owolf3d_ogengine_integration") {
    # Insert after "zstring.cpp" in the source list
    $content = $content -replace '(\tzstring\.cpp)', "`$1`n`towolf3d_ogengine_integration.cpp"
    Set-Content $cmake $content -Encoding UTF8
    Write-Host "  Added owolf3d_ogengine_integration.cpp to CMakeLists.txt"

    # Add ogengine.lib link (Windows)
    $linkSnippet = @"

# OWolf3D: OASIS STAR API link
if(WIN32)
    target_link_libraries(engine PRIVATE "`${CMAKE_CURRENT_SOURCE_DIR}/ogengine.lib")
    target_compile_definitions(engine PRIVATE OASIS_STAR_SYNC_IN_CLIENT=1)
endif()
"@
    Add-Content $cmake $linkSnippet -Encoding UTF8
    Write-Host "  Added ogengine.lib link and OASIS_STAR_SYNC_IN_CLIENT definition"
} else {
    Write-Host "  CMakeLists.txt already patched"
}

# ── CMake configure ────────────────────────────────────────────────────────
Write-Host "`n[3/4] Configuring CMake..." -ForegroundColor Yellow

if (-not (Test-Path $BuildDir)) { New-Item -ItemType Directory -Path $BuildDir | Out-Null }

& cmake -S $ECWolfSrc -B $BuildDir `
    -G "Visual Studio 16 2019" -A x64 `
    -DCMAKE_BUILD_TYPE=$BuildType `
    -DGPL=ON

if ($LASTEXITCODE -ne 0) { throw "CMake configure failed." }

# ── Build ──────────────────────────────────────────────────────────────────
Write-Host "`n[4/4] Building ($BuildType)..." -ForegroundColor Yellow

& cmake --build $BuildDir --config $BuildType

if ($LASTEXITCODE -ne 0) { throw "Build failed." }

# ── Copy runtime files to build output ────────────────────────────────────
$OutDir = Join-Path $BuildDir "$BuildType"
$dllSrc = Join-Path $SrcDst "ogengine.dll"
if (Test-Path $dllSrc) {
    Copy-Item $dllSrc $OutDir -Force
    Write-Host "  Deployed ogengine.dll to $OutDir"
}
Copy-Item (Join-Path $OasisDir "oasisstar.json") $OutDir -Force
Write-Host "  Deployed oasisstar.json to $OutDir"

Write-Host "`n=== Build succeeded ===" -ForegroundColor Green
Write-Host "Output: $OutDir\ecwolf.exe"
