[CmdletBinding()]
param(
    [string[]]$ConfigPath = @(
        'OASIS Omniverse/OGames/ODOOM/build/oasisstar.json',
        'OASIS Omniverse/OGames/OQuake/build/oasisstar.json'
    )
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$forbidden = @('jwt_token', 'refresh_token', 'beamedin_avatar')
$violations = [System.Collections.Generic.List[string]]::new()

foreach ($path in $ConfigPath) {
    $resolved = if ([IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $repoRoot $path }
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) { continue }
    $config = Get-Content -LiteralPath $resolved -Raw | ConvertFrom-Json
    foreach ($name in $forbidden) {
        if ($config.PSObject.Properties.Name -contains $name) {
            $violations.Add("$resolved contains forbidden persisted session field '$name'.")
        }
    }
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Error $_ }
    throw 'Packaged game configuration contains persisted account credentials. Remove session fields before release.'
}

Write-Host 'Packaged game configurations contain no persisted session credentials.'
