[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('OASISRuntime', 'STARRuntime', 'EdgeRuntime', 'OGEngineClient', 'NativeEndpoint', 'MCPServer', 'OurWorld', 'ODOOM', 'OQUAKE')]
    [string]$Component,
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,
    [Parameter(Mandatory)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($Component -in @('ODOOM', 'OQUAKE')) {
    # Game notes must be reviewed against the exact released source, not a moving branch
    # or a classifier dumping development commit messages into multiple sections.
    $reviewedPath = Join-Path $repoRoot "Docs/Releases/$Component/$Version.md"
    if (-not (Test-Path -LiteralPath $reviewedPath)) {
        throw "Missing reviewed $Component v$Version notes: $reviewedPath. Follow Docs/Releases/README.md."
    }
    $notes = Get-Content -LiteralPath $reviewedPath -Raw
    foreach ($heading in @("# $Component v$Version", '### Features', '### Fixes and hardening', '### Build and packaging', '## Downloads and requirements', '## Source and full changelog')) {
        if (-not $notes.Contains($heading)) { throw "Reviewed notes missing required heading: $heading" }
    }
    $resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($resolvedOutput)) -Force | Out-Null
    [IO.File]::WriteAllText($resolvedOutput, $notes.TrimEnd() + "`n", [Text.UTF8Encoding]::new($false))
    return
}
$definitions = @{
    OASISRuntime = @{
        Title = 'OASIS Runtime'; Prefixes = @('OASIS-Runtime-v', 'OASIS-Runtime-')
        Paths = @('OASIS Architecture', 'ONODE', 'Providers', 'HoloNET-ORM', 'ONET', 'Edge')
        Intro = 'The OASIS Runtime is the in-process WEB4 engine for OAPPs, native applications and the live OASIS API. It provides identity, COSMIC ORM data, providers, NFTs and GeoNFTs, inventory, ONET and HyperDrive online/offline synchronization.'
    }
    STARRuntime = @{
        Title = 'STAR ODK Runtime'; Prefixes = @('STAR-ODK-Runtime-v', 'STAR-ODK-Runtime-')
        Paths = @('STAR ODK')
        Intro = 'The STAR ODK Runtime contains the WEB5 STAR engine, STAR CLI and reusable CLI library used to create, package and run OAPPs, quests, missions, GeoHotSpots and STARNET content.'
    }
    EdgeRuntime = @{
        Title = 'OASIS Edge Runtime'; Prefixes = @('OASIS-Edge-Runtime-v')
        Paths = @('OASIS Architecture/NextGenSoftware.OASIS.Edge.Runtime', 'OASIS Architecture/NextGenSoftware.OASIS.Edge.ONET.Runtime', 'OASIS Architecture/NextGenSoftware.OASIS.HyperDrive.Synchronization', 'OASIS Architecture/NextGenSoftware.OASIS.ONET', 'Native EndPoint/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge', 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS', 'Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge', 'Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity', 'OASIS Omniverse/OGEngineClient/Edge')
        Intro = 'The OASIS Edge Runtime is the reusable durable offline-first runtime for games, mobile applications and lightweight Edge ONODEs. The release contains synchronization contracts, SQLite journalling, ONET transport, the Edge Native Endpoint, HoloOASIS adapters and the Unity package.'
    }
    OGEngineClient = @{
        Title = 'OGEngineClient'; Prefixes = @('OGEngineClient-v', 'STAR-API-CLIENT-v')
        Paths = @('OASIS Omniverse/OGEngineClient', 'ONET', 'Edge')
        Intro = 'OGEngineClient is the managed and native C/C++ game integration layer for WEB4 and WEB5. It connects games including Our World, ODOOM and OQUAKE to shared identity, inventory, GeoNFT, quest and online/offline synchronization services.'
    }
    NativeEndpoint = @{
        Title = 'OASIS API Integrated Native Endpoint'; Prefixes = @('Native-Endpoint-v', 'v')
        Paths = @('Native EndPoint', 'OASIS Architecture', 'ONODE', 'Providers')
        Intro = 'The OASIS API Integrated Native Endpoint embeds the WEB4 OASIS Runtime in an application process, providing the same provider abstraction and online/offline behavior without HTTP overhead.'
    }
    MCPServer = @{
        Title = 'OASIS MCP Server'; Prefixes = @('mcp-v')
        Paths = @('WEB6/NextGenSoftware.OASIS.MCP.Server', 'WEB6/npm', 'WEB6/NextGenSoftware.OASIS.Web6.Core', 'WEB7', 'WEB8', 'WEB9', 'WEB10')
        Intro = 'The OASIS MCP Server exposes the typed WEB4-WEB10 API surface to MCP clients through native executables, NuGet and npm distributions.'
    }
    OurWorld = @{
        Title = 'Our World'; Repository = 'NextGenSoftwareUK/Our-World'; Branch = 'main'
        Intro = 'Our World is the Unity geospatial OASIS game that connects real-world GeoNFT discovery with shared avatar identity, inventory, quests and online/offline progression.'
    }
    ODOOM = @{
        Title = 'ODOOM'; Repository = 'NextGenSoftwareUK/ODOOM'; Branch = 'trunk'
        Intro = 'ODOOM is the OASIS-integrated UZDoom game client, with shared STAR identity, inventory, quests and cross-game progression through OGEngineClient.'
    }
    OQUAKE = @{
        Title = 'OQUAKE'; Repository = 'NextGenSoftwareUK/OQUAKE'; Branch = 'master'
        Intro = 'OQUAKE is the OASIS-integrated vkQuake game client, with shared STAR identity, inventory, quests and cross-game progression through OGEngineClient.'
    }
}

$definition = $definitions[$Component]
$targetTag = switch ($Component) {
    OASISRuntime { "OASIS-Runtime-v$Version" }
    STARRuntime { "STAR-ODK-Runtime-v$Version" }
    EdgeRuntime { "OASIS-Edge-Runtime-v$Version" }
    OGEngineClient { "OGEngineClient-v$Version" }
    NativeEndpoint { "Native-Endpoint-v$Version" }
    MCPServer { "mcp-v$Version" }
    OurWorld { "v$Version" }
    ODOOM { "v$Version" }
    OQUAKE { "v$Version" }
}

$external = [bool]$definition.Repository
$releaseRepository = if ($external) { $definition.Repository } else { 'NextGenSoftwareUK/OASIS' }
if ($external) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw "gh is required to generate $Component release notes." }
    $releases = @((gh api "repos/$releaseRepository/releases?per_page=100" | ConvertFrom-Json))
    $tagCandidates = foreach ($release in $releases) {
        if (-not $release.draft -and -not $release.prerelease -and $release.tag_name -match '^v(?<version>\d+\.\d+\.\d+)$' -and $release.tag_name -ne $targetTag) {
            [pscustomobject]@{ Tag = $release.tag_name; Version = [version]$Matches.version }
        }
    }
}
else {
    $tagCandidates = foreach ($tag in @(& git -C $repoRoot tag --list)) {
        foreach ($prefix in $definition.Prefixes) {
            if ($tag -match ('^' + [regex]::Escape($prefix) + '(?<version>\d+\.\d+\.\d+)$') -and $tag -ne $targetTag) {
                [pscustomobject]@{ Tag = $tag; Version = [version]$Matches.version }
            }
        }
    }
}
$previous = @($tagCandidates | Sort-Object Version -Descending | Select-Object -First 1)
$range = if ($previous.Count -gt 0) { "$($previous[0].Tag)..HEAD" } else { 'HEAD' }
$rawCommits = if ($external) {
    if ($previous.Count -gt 0) { @((gh api "repos/$releaseRepository/compare/$($previous[0].Tag)...$($definition.Branch)" | ConvertFrom-Json).commits | ForEach-Object { "$($_.sha)`t$((($_.commit.message -split "`n", 2)[0]))" }) }
    else { @((gh api "repos/$releaseRepository/commits?sha=$($definition.Branch)&per_page=100" | ConvertFrom-Json) | ForEach-Object { "$($_.sha)`t$((($_.commit.message -split "`n", 2)[0]))" }) }
}
else {
    $logArgs = @('-C', $repoRoot, 'log', $range, '--no-merges', '--format=%H%x09%s', '--') + $definition.Paths
    @(& git @logArgs)
}
$allCommits = @($rawCommits | ForEach-Object {
    $parts = $_ -split "`t", 2
    $subject = if ($parts.Count -eq 2) { $parts[1] -replace '^\s*[-*]\s*', '' } else { '' }
    if ($subject -and $subject -notmatch '^(Promote |Merge |chore: bump submodule|chore: update submodule)') {
        [pscustomobject]@{ Sha = $parts[0]; Subject = $subject }
    }
} | Group-Object Subject | ForEach-Object { $_.Group[0] })
$maximumSummarizedCommits = 100
$commits = @($allCommits | Select-Object -First $maximumSummarizedCommits)

$features = @($commits | Where-Object Subject -match '^(feat|add|implement)|\b(add|introduc|implement|support|expand)' )
$fixes = @($commits | Where-Object { $_ -notin $features -and $_.Subject -match '^(fix|repair|restore|harden)|\b(fix|repair|correct|prevent|stabili[sz]|harden)' })
$other = @($commits | Where-Object { $_ -notin $features -and $_ -notin $fixes })
function Add-CommitSection([Text.StringBuilder]$Builder, [string]$Heading, $Items) {
    if (@($Items).Count -eq 0) { return }
    [void]$Builder.AppendLine("### $Heading")
    [void]$Builder.AppendLine()
    foreach ($item in $Items) {
        $short = $item.Sha.Substring(0, 7)
        [void]$Builder.AppendLine("- $($item.Subject) ([`$short`](https://github.com/$releaseRepository/commit/$($item.Sha)))".Replace('$short', $short))
    }
    [void]$Builder.AppendLine()
}

$builder = [Text.StringBuilder]::new()
[void]$builder.AppendLine("# $($definition.Title) v$Version")
[void]$builder.AppendLine()
[void]$builder.AppendLine($definition.Intro)
[void]$builder.AppendLine()
[void]$builder.AppendLine("## What's new in v$Version")
[void]$builder.AppendLine()
if ($commits.Count -eq 0) {
    if ($Component -eq 'MCPServer') {
        [void]$builder.AppendLine('- First comprehensive public OASIS MCP Server release, exposing 516 specific, named and typed tools across WEB4 through WEB10.')
        [void]$builder.AppendLine('- Ships native executables for Windows, Linux and macOS on x64 and arm64, plus NuGet and npm installation channels.')
        [void]$builder.AppendLine('- Supports local stdio clients and the WEB6 HTTP/SSE MCP transport, with IDE configuration and a complete command catalog.')
    }
    else {
        [void]$builder.AppendLine('- No component-path commits were found after the previous release tag; this release republishes the validated component from the coordinated source graph.')
    }
    [void]$builder.AppendLine()
}
else {
    Add-CommitSection $builder 'Features' $features
    Add-CommitSection $builder 'Fixes and hardening' $fixes
    Add-CommitSection $builder 'Other changes' $other
    if ($allCommits.Count -gt $commits.Count) {
        [void]$builder.AppendLine("The summary lists the newest $($commits.Count) of $($allCommits.Count) component changes. The full comparison below contains every commit.")
        [void]$builder.AppendLine()
    }
}
[void]$builder.AppendLine('## Downloads and requirements')
[void]$builder.AppendLine()
[void]$builder.AppendLine("[Release assets](https://github.com/$releaseRepository/releases/tag/$targetTag): select the archive matching your operating system and architecture. Do not use a Windows binary on Linux/macOS or assume a managed package proves native platform support.")
[void]$builder.AppendLine('NuGet packages resolve reusable managed dependencies; native executables, Unity packages and mobile conductor artifacts are separate distributions. Consult each archive/package README for native libraries and external-service prerequisites.')
[void]$builder.AppendLine()
[void]$builder.AppendLine('## Installation and configuration')
[void]$builder.AppendLine()
[void]$builder.AppendLine('Extract the matching binary archive or install the exact NuGet version listed in the release inventory. Preserve the archive dependency layout. For reusable libraries, integrate the documented public API with your host lifecycle rather than treating the package as a standalone application.')
[void]$builder.AppendLine('Use the public Default OASIS_DNA.json template where included, then configure the providers and endpoints required by your application. Obtain authentication through the supported application flow; never copy production credentials, tokens or private keys into public templates or client packages.')
if ($Component -eq 'EdgeRuntime') {
    [void]$builder.AppendLine('[Edge Runtime guide](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/OASIS%20Architecture/NextGenSoftware.OASIS.Edge.Runtime/README.md) and [Edge ONET guide](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/OASIS%20Architecture/NextGenSoftware.OASIS.Edge.ONET.Runtime/README.md) explain persistence, transport/identity composition, offline grants, lifecycle, configuration defaults and troubleshooting.')
}
[void]$builder.AppendLine()
[void]$builder.AppendLine('## Verification and known limitations')
[void]$builder.AppendLine()
[void]$builder.AppendLine('Build, packaging and publication checks are not proof of physical Android/iOS execution, two-device convergence or Unity IL2CPP behavior. Check the dated release inventory for actual evidence and outstanding acceptance tasks; do not infer completion from a successful upload.')
[void]$builder.AppendLine('On failure, retain the actual error code/message, check authentication, configured service availability and native dependency/architecture match, and avoid exposing secrets in logs.')
[void]$builder.AppendLine('[Verified release inventory](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/Devs/OASIS_RELEASE_STATUS.md).')
[void]$builder.AppendLine()
[void]$builder.AppendLine('## Full changelog')
[void]$builder.AppendLine()
if ($previous.Count -gt 0) {
    [void]$builder.AppendLine("[$($previous[0].Tag)...$targetTag](https://github.com/$releaseRepository/compare/$($previous[0].Tag)...$targetTag)")
}
else {
    [void]$builder.AppendLine('This is the first release under this component tag series; the commit list above is the component changelog for the initial version.')
}

if ($Component -eq 'MCPServer') {
    $readmePath = Join-Path $repoRoot 'WEB6\NextGenSoftware.OASIS.MCP.Server\README.md'
    $guide = Get-Content -LiteralPath $readmePath -Raw
    $guide = $guide -replace '^# OASIS MCP Server\s*', ''
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('## Complete package guide')
    [void]$builder.AppendLine()
    [void]$builder.Append($guide.Trim())
    [void]$builder.AppendLine()
}

$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($resolvedOutput)) -Force | Out-Null
[IO.File]::WriteAllText($resolvedOutput, $builder.ToString().TrimEnd() + "`n", [Text.UTF8Encoding]::new($false))
Write-Host "Generated $Component v$Version release notes from $range at $resolvedOutput"
