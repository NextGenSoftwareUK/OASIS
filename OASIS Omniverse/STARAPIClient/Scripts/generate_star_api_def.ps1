<#
.SYNOPSIS
  Generates star_api.def from C# UnmanagedCallersOnly EntryPoint names.
  Ensures the import library (star_api.lib) has every native export so BUILD DOOM / BUILD QUAKE link without LNK2001.
  Run automatically by publish_and_deploy_star_api.ps1; can be run standalone to update the .def after adding new exports.
#>
param(
    [string]$ProjectDir = (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)),
    [string[]]$ExcludeSymbols = @("star_api_authenticate_with_jwt_out")  # Omitted from .lib so game forwarder provides at runtime
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectDir = [System.IO.Path]::GetFullPath($ProjectDir)
$defPath = Join-Path $projectDir "star_api.def"

$csFiles = @(
    (Join-Path $projectDir "StarApiClient.cs"),
    (Join-Path $projectDir "StarSyncExports.cs")
)
$symbols = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$regex = [regex]'UnmanagedCallersOnly\s*\(\s*EntryPoint\s*=\s*"([^"]+)"'

foreach ($cs in $csFiles) {
    if (-not (Test-Path $cs)) { continue }
    $content = Get-Content -LiteralPath $cs -Raw
    foreach ($m in $regex.Matches($content)) {
        $name = $m.Groups[1].Value.Trim()
        if ($name -and -not [string]::IsNullOrWhiteSpace($name)) {
            [void]$symbols.Add($name)
        }
    }
}

foreach ($ex in $ExcludeSymbols) {
    [void]$symbols.Remove($ex)
}

$exportList = @($symbols | Sort-Object)
$header = @"
; OASIS STAR API - export list for star_api.lib (auto-generated from C# UnmanagedCallersOnly EntryPoint names)
; Do not edit by hand. Run Scripts\generate_star_api_def.ps1 to regenerate after adding new exports.
; BUILD DOOM / BUILD QUAKE use this so star_api.lib has all symbols and link succeeds without stubs.
LIBRARY star_api
EXPORTS
"@
$body = $exportList -join "`r`n"
$content = $header.TrimEnd() + "`r`n" + $body + "`r`n"
Set-Content -LiteralPath $defPath -Value $content -NoNewline -Encoding ASCII
Write-Host "Generated star_api.def with $($exportList.Count) exports (excluded: $($ExcludeSymbols -join ', '))."
return $exportList.Count
