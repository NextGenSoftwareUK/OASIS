[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$bootLoader = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.OASISBootLoader\OASISBootLoader.cs'
$histories = @(
    (Join-Path $repoRoot 'ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\OASIS API RELEASE HISTORY.md'),
    (Join-Path $repoRoot 'STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI\STAR API RELEASE HISTORY.md'),
    (Join-Path $repoRoot 'WEB6\NextGenSoftware.OASIS.Web6.WebAPI\WEB6 API RELEASE HISTORY.md')
)
$protected = @($bootLoader) + $histories
$before = @{}
foreach ($path in $protected) { $before[$path] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }

$testOutput = Join-Path ([IO.Path]::GetTempPath()) ("oasis-global-release-test-" + [guid]::NewGuid())
try {
    & (Join-Path $PSScriptRoot 'Invoke-OASISGlobalRelease.ps1') -Operation Plan -Offline -OutputDirectory $testOutput
    if ($LASTEXITCODE -ne 0) { throw 'Default offline release plan failed.' }
    $plan = Get-Content -LiteralPath (Join-Path $testOutput 'release-plan.json') -Raw | ConvertFrom-Json
    if ($plan.packages.Count -lt 200) { throw "Expected at least 200 first-party packages, found $($plan.packages.Count)." }
    if ($plan.components.advanceWeb4ToWeb6ApiVersions) { throw 'API advancement must default to false.' }
    foreach ($component in @('nugetPackages', 'oasisRuntime', 'starRuntime', 'ogEngineClient', 'nativeEndpoint', 'mcpServer')) {
        if (-not $plan.components.$component) { throw "$component must default to true." }
    }
    foreach ($component in @('ourWorld', 'odoom', 'oquake', 'oide', 'onodeManager', 'hyperDriveClient')) {
        if ($plan.components.$component) { throw "$component must default to false." }
        if (-not $plan.releaseVersions.$component) { throw "$component must have a planned version even when excluded." }
    }
    foreach ($path in $protected) {
        $after = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        if ($after -ne $before[$path]) { throw "Default planning unexpectedly changed $path." }
    }
    $content = Get-Content -LiteralPath $bootLoader -Raw
    foreach ($expected in @('OASISAPIVersion { get; set; } = "5.2.0"', 'STARAPIVersion { get; set; } = "3.2.0"', 'WEB6APIVersion { get; set; } = "3.2.0"')) {
        if (-not $content.Contains($expected)) { throw "Missing expected WEB4-WEB6 version: $expected" }
    }
    foreach ($expected in @('WEB7APIVersion { get; set; } = "1.0.0"', 'WEB8APIVersion { get; set; } = "1.0.0"', 'WEB9APIVersion { get; set; } = "1.0.0"', 'WEB10APIVersion { get; set; } = "1.0.0"')) {
        if (-not $content.Contains($expected)) { throw "WEB7-WEB10 version changed unexpectedly: $expected" }
    }
    Write-Host "Global release automation tests passed for $($plan.packages.Count) packages."
}
finally {
    if (Test-Path -LiteralPath $testOutput) { Remove-Item -LiteralPath $testOutput -Recurse -Force }
}
