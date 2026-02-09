<#
.SYNOPSIS
  Ensures BuilderNative.dll (and DLLs in the same folder) are in build\Editor so Ultimate Doom Builder can open maps.
  Called by BUILD ODOOM.bat.
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$EditorDir,
    [string]$UltimateDoomBuilderRoot = "C:\Source\UltimateDoomBuilder"
)

$EditorDir = $EditorDir.TrimEnd('\')
if (-not (Test-Path "$EditorDir\Builder.exe")) { return }

$need = "BuilderNative.dll"
if (Test-Path "$EditorDir\$need") { return }

$searchPaths = @(
    (Join-Path $UltimateDoomBuilderRoot "Build"),
    (Join-Path $UltimateDoomBuilderRoot "Build\x64"),
    (Join-Path $UltimateDoomBuilderRoot "Build\Native"),
    (Join-Path $EditorDir "x64"),
    (Join-Path $EditorDir "Native")
)

$foundDir = $null
foreach ($d in $searchPaths) {
    $path = Join-Path $d $need
    if (Test-Path $path) {
        $foundDir = $d
        break
    }
}
if (-not $foundDir) {
    Write-Host "[ODOOM] WARNING: BuilderNative.dll not found. Build the full Ultimate Doom Builder solution (Release x64), ensure the native project outputs to Build or Build\x64, then re-run BUILD ODOOM.bat." -ForegroundColor Yellow
    return
}

Write-Host "[ODOOM] Copying BuilderNative and same-folder DLLs from $foundDir into build\Editor..."
Get-ChildItem -Path $foundDir -Filter "*.dll" -ErrorAction SilentlyContinue | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $EditorDir -Force
}
Write-Host "[ODOOM] Done. Builder should now be able to open maps." -ForegroundColor Green
