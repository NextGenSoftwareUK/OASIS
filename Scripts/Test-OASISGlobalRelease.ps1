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

    $publicHistories = @(
        @{ Source = 'STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/STAR API RELEASE HISTORY.md'; Public = 'Docs/API/WEB5-STAR-API-RELEASE-HISTORY.md'; Program = 'STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Program.cs' },
        @{ Source = 'WEB6/NextGenSoftware.OASIS.Web6.WebAPI/WEB6 API RELEASE HISTORY.md'; Public = 'Docs/API/WEB6-AI-API-RELEASE-HISTORY.md'; Program = 'WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Program.cs' }
    )
    foreach ($history in $publicHistories) {
        $sourcePath = Join-Path $repoRoot $history.Source
        $publicPath = Join-Path $repoRoot $history.Public
        $programPath = Join-Path $repoRoot $history.Program
        if ((Get-FileHash -Algorithm SHA256 $sourcePath).Hash -ne (Get-FileHash -Algorithm SHA256 $publicPath).Hash) {
            throw "Public API release history is out of sync: $($history.Public)"
        }
        $expectedUrl = "https://github.com/NextGenSoftwareUK/OASIS/blob/master/$($history.Public.Replace(' ', '%20'))"
        if (-not (Select-String -LiteralPath $programPath -SimpleMatch $expectedUrl)) {
            throw "Swagger does not link to its public release history: $($history.Program)"
        }
    }
    $chronologicalHistories = @(
        @{ Path = 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS API RELEASE HISTORY.md'; Required = @('4.0.0','4.5.0','4.5.1','5.2.0') },
        @{ Path = 'Docs/API/WEB5-STAR-API-RELEASE-HISTORY.md'; Required = @('2.0.0','3.0.0','3.2.0') },
        @{ Path = 'Docs/API/WEB6-AI-API-RELEASE-HISTORY.md'; Required = @('1.0.0','2.0.0','3.2.0') }
    )
    foreach ($history in $chronologicalHistories) {
        $historyText = Get-Content -LiteralPath (Join-Path $repoRoot $history.Path) -Raw
        $versions = @([regex]::Matches($historyText, '(?m)^## (?<version>\d+\.\d+\.\d+)') | ForEach-Object { [version]$_.Groups['version'].Value })
        for ($index = 1; $index -lt $versions.Count; $index++) {
            if ($versions[$index] -lt $versions[$index - 1]) { throw "Release history is not oldest-to-newest: $($history.Path)" }
        }
        foreach ($required in $history.Required) {
            if ([version]$required -notin $versions) { throw "Release history is missing ${required}: $($history.Path)" }
        }
    }
}
finally {
    if (Test-Path -LiteralPath $testOutput) { Remove-Item -LiteralPath $testOutput -Recurse -Force }
}
