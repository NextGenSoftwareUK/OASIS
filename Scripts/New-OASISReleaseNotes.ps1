[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('OASISRuntime', 'STARRuntime', 'OGEngineClient', 'NativeEndpoint', 'MCPServer')]
    [string]$Component,
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,
    [Parameter(Mandatory)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
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
}

$definition = $definitions[$Component]
$targetTag = switch ($Component) {
    OASISRuntime { "OASIS-Runtime-v$Version" }
    STARRuntime { "STAR-ODK-Runtime-v$Version" }
    OGEngineClient { "OGEngineClient-v$Version" }
    NativeEndpoint { "Native-Endpoint-v$Version" }
    MCPServer { "mcp-v$Version" }
}

$tagCandidates = foreach ($tag in @(& git -C $repoRoot tag --list)) {
    foreach ($prefix in $definition.Prefixes) {
        if ($tag -match ('^' + [regex]::Escape($prefix) + '(?<version>\d+\.\d+\.\d+)$') -and $tag -ne $targetTag) {
            [pscustomobject]@{ Tag = $tag; Version = [version]$Matches.version }
        }
    }
}
$previous = @($tagCandidates | Sort-Object Version -Descending | Select-Object -First 1)
$range = if ($previous.Count -gt 0) { "$($previous[0].Tag)..HEAD" } else { 'HEAD' }
$logArgs = @('-C', $repoRoot, 'log', $range, '--no-merges', '--format=%H%x09%s', '--') + $definition.Paths
$allCommits = @(& git @logArgs | ForEach-Object {
    $parts = $_ -split "`t", 2
    $subject = if ($parts.Count -eq 2) { $parts[1] -replace '^\s*[-*]\s*', '' } else { '' }
    if ($subject -and $subject -notmatch '^(Promote |Merge |chore: bump submodule|chore: update submodule)') {
        [pscustomobject]@{ Sha = $parts[0]; Subject = $subject }
    }
} | Group-Object Subject | ForEach-Object { $_.Group[0] })
$maximumSummarizedCommits = 100
$commits = @($allCommits | Select-Object -First $maximumSummarizedCommits)

$features = @($commits | Where-Object Subject -match '^(feat|add|implement)|\b(add|introduc|implement|support|expand)' )
$fixes = @($commits | Where-Object Subject -match '^(fix|repair|restore|harden)|\b(fix|repair|correct|prevent|stabili[sz]|harden)' )
$other = @($commits | Where-Object { $_ -notin $features -and $_ -notin $fixes })
function Add-CommitSection([Text.StringBuilder]$Builder, [string]$Heading, $Items) {
    if (@($Items).Count -eq 0) { return }
    [void]$Builder.AppendLine("### $Heading")
    [void]$Builder.AppendLine()
    foreach ($item in $Items) {
        $short = $item.Sha.Substring(0, 7)
        [void]$Builder.AppendLine("- $($item.Subject) ([`$short`](https://github.com/NextGenSoftwareUK/OASIS/commit/$($item.Sha)))".Replace('$short', $short))
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
    [void]$builder.AppendLine('- No component-path commits were found after the previous release tag; this release republishes the validated component from the coordinated source graph.')
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
[void]$builder.AppendLine('## Full changelog')
[void]$builder.AppendLine()
if ($previous.Count -gt 0) {
    [void]$builder.AppendLine("[$($previous[0].Tag)...$targetTag](https://github.com/NextGenSoftwareUK/OASIS/compare/$($previous[0].Tag)...$targetTag)")
}
else {
    [void]$builder.AppendLine('This is the first release under this component tag series; the commit list above is the component changelog for the initial version.')
}

$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($resolvedOutput)) -Force | Out-Null
[IO.File]::WriteAllText($resolvedOutput, $builder.ToString().TrimEnd() + "`n", [Text.UTF8Encoding]::new($false))
Write-Host "Generated $Component v$Version release notes from $range at $resolvedOutput"
