[CmdletBinding()]
param(
    [ValidateSet('Plan', 'Pack', 'Publish')]
    [string]$Operation = 'Plan',
    [bool]$NuGetPackages = $true,
    [bool]$OASISRuntime = $true,
    [bool]$STARRuntime = $true,
    [bool]$OGEngineClient = $true,
    [bool]$NativeEndpoint = $true,
    [bool]$MCPServer = $true,
    [bool]$OurWorld = $false,
    [bool]$ODOOM = $false,
    [bool]$OQUAKE = $false,
    [bool]$OIDE = $false,
    [bool]$ONODEManager = $false,
    [bool]$HyperDriveClient = $false,
    [bool]$AdvanceWeb4ToWeb6ApiVersions = $false,
    [int]$ApiMinorIncrement = 2,
    [string]$ReleaseNotes = '',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\global-release'),
    [string]$NuGetApiKey = $env:NUGET_API_KEY,
    [switch]$Offline
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$planPath = Join-Path $OutputDirectory 'release-plan.json'
$packageOutput = Join-Path $OutputDirectory 'nuget'

function Get-NextPatchVersion([string]$Version) {
    $match = [regex]::Match($Version, '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)')
    if (-not $match.Success) { throw "Version '$Version' is not a supported stable semantic version." }
    return '{0}.{1}.{2}' -f $match.Groups['major'].Value, $match.Groups['minor'].Value, ([int]$match.Groups['patch'].Value + 1)
}

function Set-TextPreservingUtf8Bom([string]$Path, [string]$Content) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    [IO.File]::WriteAllText($Path, $Content, [Text.UTF8Encoding]::new($hasBom))
}

function Get-AdvancedMinorVersion([string]$Version, [int]$Increment) {
    $parsed = [version]$Version
    return '{0}.{1}.0' -f $parsed.Major, ($parsed.Minor + $Increment)
}

function Get-ProjectProperty([xml]$Project, [string]$Name) {
    $nodes = @($Project.SelectNodes("//*[local-name()='$Name']"))
    $node = $nodes | Where-Object { -not $_.Condition -or $_.Condition -match "!='true'|!= 'true'" } | Select-Object -First 1
    if ($null -eq $node) { $node = $nodes | Select-Object -First 1 }
    if ($null -eq $node) { return '' }
    return [string]$node.InnerText
}

function Get-PackageProjects {
    $excluded = '[\\/](\.git|\.claude|\.codex|Archived|External Libs|Test Projects|Tests|UnitTests|IntegrationTests|Templates?|obj|bin|node_modules)[\\/]'
    Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
        Where-Object { $_.FullName -notmatch $excluded } |
        ForEach-Object {
            [xml]$xml = Get-Content -LiteralPath $_.FullName -Raw
            $id = Get-ProjectProperty $xml 'PackageId'
            if ($id) {
                [pscustomobject]@{ File = $_; Xml = $xml; PackageId = $id }
            }
        } | Sort-Object PackageId
}

function Assert-PackageMetadata($PackageProjects) {
    $required = @('PackageId', 'Description', 'Authors', 'PackageTags', 'PackageProjectUrl', 'RepositoryUrl', 'RepositoryType')
    $invalid = [Collections.Generic.List[string]]::new()
    foreach ($project in $PackageProjects) {
        $missing = @($required | Where-Object { -not (Get-ProjectProperty $project.Xml $_) })
        $licenseExpression = Get-ProjectProperty $project.Xml 'PackageLicenseExpression'
        $licenseFile = Get-ProjectProperty $project.Xml 'PackageLicenseFile'
        if (-not $licenseExpression -and -not $licenseFile) { $missing += 'PackageLicenseExpression/PackageLicenseFile' }
        if ($missing.Count -gt 0) {
            $relative = $project.File.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
            $invalid.Add("$relative missing: $($missing -join ', ')")
        }
    }
    if ($invalid.Count -gt 0) { throw "NuGet metadata validation failed:`n - $($invalid -join "`n - ")" }
}

function Get-LatestNuGetVersion([string]$PackageId) {
    if ($Offline) { return $null }
    $url = "https://api.nuget.org/v3-flatcontainer/$($PackageId.ToLowerInvariant())/index.json"
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get
        return @($response.versions | Where-Object { $_ -match '^\d+\.\d+\.\d+$' } | ForEach-Object { [version]$_ } | Sort-Object -Descending | Select-Object -First 1)[0].ToString()
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 404) { return $null }
        throw
    }
}

function Get-LatestGitHubReleaseVersion([string[]]$TagPrefixes, [string]$Repository = 'NextGenSoftwareUK/OASIS') {
    if ($Offline) { return $null }
    $headers = @{ 'User-Agent' = 'OASIS-global-release' }
    if ($env:GH_TOKEN) { $headers.Authorization = "Bearer $($env:GH_TOKEN)" }
    $releases = @(Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/releases?per_page=100" -Headers $headers) |
        Where-Object { -not $_.draft -and -not $_.prerelease }
    $versions = foreach ($release in $releases) {
        foreach ($prefix in $TagPrefixes) {
            if ($release.tag_name -match ('^' + [regex]::Escape($prefix) + '(?<version>\d+\.\d+\.\d+)$')) {
                [version]$Matches.version
            }
        }
    }
    $latest = @($versions | Sort-Object -Descending | Select-Object -First 1)
    if ($latest.Count -eq 0) { return $null }
    return $latest[0].ToString()
}

function Get-NextOptionalReleaseVersion([string]$Repository, [string[]]$TagPrefixes, [bool]$AllowAnySemanticTag = $false) {
    $publishedVersion = Get-LatestGitHubReleaseVersion $TagPrefixes $Repository
    if (-not $publishedVersion -and -not $Offline -and $AllowAnySemanticTag) {
        $headers = @{ 'User-Agent' = 'OASIS-global-release' }
        if ($env:GH_TOKEN) { $headers.Authorization = "Bearer $($env:GH_TOKEN)" }
        $releases = @(Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/releases?per_page=100" -Headers $headers) |
            Where-Object { -not $_.draft -and -not $_.prerelease }
        $versions = @($releases | ForEach-Object {
            $match = [regex]::Match($_.tag_name, '\d+\.\d+\.\d+')
            if ($match.Success) { [version]$match.Value }
        } | Sort-Object -Descending)
        if ($versions.Count -gt 0) { $publishedVersion = $versions[0].ToString() }
    }
    if (-not $publishedVersion) { return '1.0.0' }
    return Get-NextPatchVersion $publishedVersion
}

function Get-NextReleaseVersion([string]$SourceVersion, [string[]]$TagPrefixes) {
    $publishedVersion = Get-LatestGitHubReleaseVersion $TagPrefixes
    $baseline = if ($publishedVersion -and [version]$publishedVersion -gt [version]$SourceVersion) { $publishedVersion } else { $SourceVersion }
    return Get-NextPatchVersion $baseline
}

function Get-SourceVersion($Project) {
    $version = Get-ProjectProperty $Project.Xml 'Version'
    if (-not $version) { $version = Get-ProjectProperty $Project.Xml 'PackageVersion' }
    if (-not $version) { $version = '1.0.0' }
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "$($Project.PackageId) has unsupported source version '$version'." }
    return $version
}

function Set-ProjectVersionForPacking([string]$Path, [string]$Version) {
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -match '<Version>[^<]+</Version>') {
        $content = [regex]::Replace($content, '<Version>[^<]+</Version>', "<Version>$Version</Version>")
    }
    else {
        $content = [regex]::Replace($content, '(?s)(<PropertyGroup(?:\s[^>]*)?>)', "`$1`r`n    <Version>$Version</Version>", 1)
    }
    Set-TextPreservingUtf8Bom $Path $content
}

function Get-PlannedPackageVersion($Project) {
    $sourceVersion = Get-SourceVersion $Project
    $publishedVersion = Get-LatestNuGetVersion $Project.PackageId
    if (-not $publishedVersion) { return $sourceVersion }
    $baseline = if ([version]$publishedVersion -gt [version]$sourceVersion) { $publishedVersion } else { $sourceVersion }
    return Get-NextPatchVersion $baseline
}

function Get-BootLoaderVersions {
    $path = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.OASISBootLoader\OASISBootLoader.cs'
    $content = Get-Content -LiteralPath $path -Raw
    $names = @('OASISRuntimeVersion', 'STARRuntimeVersion', 'OASISAPIVersion', 'STARAPIVersion', 'WEB6APIVersion', 'WEB7APIVersion', 'WEB8APIVersion', 'WEB9APIVersion', 'WEB10APIVersion')
    $result = [ordered]@{}
    foreach ($name in $names) {
        $match = [regex]::Match($content, "public static string $name \{ get; set; \} = `"(?<version>\d+\.\d+\.\d+)`";")
        if (-not $match.Success) { throw "Could not read $name from OASISBootLoader.cs." }
        $result[$name] = $match.Groups['version'].Value
    }
    return $result
}

function Set-Web4ToWeb6VersionsAndHistory($Versions) {
    if ($ApiMinorIncrement -lt 2) { throw 'ApiMinorIncrement must be at least 2.' }
    $bootLoaderPath = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.OASISBootLoader\OASISBootLoader.cs'
    $content = Get-Content -LiteralPath $bootLoaderPath -Raw
    $updates = [ordered]@{
        OASISAPIVersion = Get-AdvancedMinorVersion $Versions.OASISAPIVersion $ApiMinorIncrement
        STARAPIVersion = Get-AdvancedMinorVersion $Versions.STARAPIVersion $ApiMinorIncrement
        WEB6APIVersion = Get-AdvancedMinorVersion $Versions.WEB6APIVersion $ApiMinorIncrement
    }
    foreach ($entry in $updates.GetEnumerator()) {
        $pattern = "(?m)(public static string $($entry.Key) \{ get; set; \} = `")\d+\.\d+\.\d+(`";)"
        $content = [regex]::Replace($content, $pattern, "`${1}$($entry.Value)`${2}", 1)
    }
    Set-TextPreservingUtf8Bom $bootLoaderPath $content

    $date = Get-Date -Format 'dd/MM/yy'
    $summary = if ($ReleaseNotes) { $ReleaseNotes } else { 'Global release containing the latest tested OASIS platform improvements, fixes, packages and runtime artifacts.' }
    $histories = @(
        @{ Path = 'ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\OASIS API RELEASE HISTORY.md'; Version = $updates.OASISAPIVersion; Name = 'WEB4 OASIS API' },
        @{ Path = 'STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI\STAR API RELEASE HISTORY.md'; Version = $updates.STARAPIVersion; Name = 'WEB5 STAR API' },
        @{ Path = 'WEB6\NextGenSoftware.OASIS.Web6.WebAPI\WEB6 API RELEASE HISTORY.md'; Version = $updates.WEB6APIVersion; Name = 'WEB6 OASIS AI API' }
    )
    foreach ($history in $histories) {
        $path = Join-Path $repoRoot $history.Path
        $existing = Get-Content -LiteralPath $path -Raw
        $headingEnd = $existing.IndexOf("`n", $existing.IndexOf("`n") + 1) + 1
        $entry = "`n----------------------------------------------------------------------------------------------------------------------------`n## $($history.Version) ($( $date ))`n`n- $summary`n- Published by the automated OASIS global release process after CI validation.`n"
        $updated = $existing.Insert($headingEnd, $entry)
        Set-TextPreservingUtf8Bom $path $updated
    }
    return $updates
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$bootVersions = Get-BootLoaderVersions
$webVersions = $null
if ($AdvanceWeb4ToWeb6ApiVersions) {
    $webVersions = Set-Web4ToWeb6VersionsAndHistory $bootVersions
    $unchanged = Get-BootLoaderVersions
    foreach ($name in @('WEB7APIVersion', 'WEB8APIVersion', 'WEB9APIVersion', 'WEB10APIVersion')) {
        if ($unchanged[$name] -ne $bootVersions[$name]) { throw "$name changed, but global release automation may only advance WEB4-WEB6." }
    }
}

$packageProjects = @()
$packagePlan = @()
if ($NuGetPackages) {
    $packageProjects = @(Get-PackageProjects)
    Assert-PackageMetadata $packageProjects
    $packagePlan = @($packageProjects | ForEach-Object {
        [pscustomobject]@{
            packageId = $_.PackageId
            version = Get-PlannedPackageVersion $_
            project = $_.File.FullName.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
        }
    })
}

$components = [ordered]@{
    nugetPackages = $NuGetPackages
    oasisRuntime = $OASISRuntime
    starRuntime = $STARRuntime
    ogEngineClient = $OGEngineClient
    nativeEndpoint = $NativeEndpoint
    mcpServer = $MCPServer
    ourWorld = $OurWorld
    odoom = $ODOOM
    oquake = $OQUAKE
    oide = $OIDE
    onodeManager = $ONODEManager
    hyperDriveClient = $HyperDriveClient
    advanceWeb4ToWeb6ApiVersions = $AdvanceWeb4ToWeb6ApiVersions
}
$releaseVersions = [ordered]@{
    oasisRuntime = Get-NextReleaseVersion $bootVersions.OASISRuntimeVersion @('OASIS-Runtime-v')
    starRuntime = Get-NextReleaseVersion $bootVersions.STARRuntimeVersion @('STAR-ODK-Runtime-v')
    ogEngineClient = Get-NextReleaseVersion (Get-SourceVersion ([pscustomobject]@{ Xml = [xml](Get-Content -LiteralPath (Join-Path $repoRoot 'OASIS Omniverse\OGEngineClient\OGEngineClient.csproj') -Raw); PackageId = 'NextGenSoftware.OGEngine.Client' })) @('OGEngineClient-v', 'STAR-API-CLIENT-v')
    nativeEndpoint = Get-NextReleaseVersion (Get-SourceVersion ([pscustomobject]@{ Xml = [xml](Get-Content -LiteralPath (Join-Path $repoRoot 'Native EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.csproj') -Raw); PackageId = 'NextGenSoftware.OASIS.API.Native.Integrated.EndPoint' })) @('Native-Endpoint-v', 'v')
    mcpServer = Get-NextReleaseVersion (Get-SourceVersion ([pscustomobject]@{ Xml = [xml](Get-Content -LiteralPath (Join-Path $repoRoot 'WEB6\NextGenSoftware.OASIS.MCP.Server\NextGenSoftware.OASIS.MCP.Server.csproj') -Raw); PackageId = 'NextGenSoftware.OASIS.MCP.Server' })) @('mcp-v')
    ourWorld = Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/Our-World' @('v', 'Our-World-v') $true
    odoom = Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/ODOOM' @('ODOOM_v.', 'odoom-v', 'v') $true
    oquake = Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/OQUAKE' @('OQUAKE_v', 'oquake-v', 'v') $true
    oide = Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/OIDE' @('v', 'oide-v')
    onodeManager = Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/OASIS' @('onode-manager-v')
    hyperDriveClient = Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/OASIS-HyperDrive-Client' @('v', 'hyperdrive-client-v')
}

$plan = [ordered]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    sourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
    operation = $Operation
    components = $components
    releaseVersions = $releaseVersions
    apiVersions = if ($webVersions) { $webVersions } else { 'unchanged' }
    packages = $packagePlan
}
$plan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $planPath
Write-Host "Release plan: $planPath"
Write-Host "NuGet packages: $($packagePlan.Count)"

if ($Operation -eq 'Plan') { exit 0 }

if ($NuGetPackages) {
    New-Item -ItemType Directory -Path $packageOutput -Force | Out-Null
    $originalProjects = @{}
    try {
        foreach ($package in $packagePlan) {
            $projectPath = Join-Path $repoRoot $package.project
            if (-not $originalProjects.ContainsKey($projectPath)) {
                $originalProjects[$projectPath] = [IO.File]::ReadAllBytes($projectPath)
                Set-ProjectVersionForPacking $projectPath $package.version
            }
        }
        foreach ($package in $packagePlan) {
            $projectPath = Join-Path $repoRoot $package.project
            & dotnet pack $projectPath -c Release -o $packageOutput --nologo
            if ($LASTEXITCODE -ne 0) { throw "Packing $($package.packageId) failed." }
        }
    }
    finally {
        foreach ($entry in $originalProjects.GetEnumerator()) {
            [IO.File]::WriteAllBytes($entry.Key, $entry.Value)
        }
    }
}

if ($Operation -eq 'Pack') { exit 0 }
if (-not $NuGetApiKey -and $NuGetPackages) { throw 'NUGET_API_KEY is required for Publish.' }
if ($NuGetPackages) {
    Get-ChildItem -LiteralPath $packageOutput -Filter '*.nupkg' |
        Where-Object { $_.Name -notlike '*.symbols.nupkg' } |
        ForEach-Object {
            & dotnet nuget push $_.FullName --source https://api.nuget.org/v3/index.json --api-key $NuGetApiKey --skip-duplicate
            if ($LASTEXITCODE -ne 0) { throw "Publishing $($_.Name) failed." }
        }
}

Write-Host 'Selected NuGet packages published. Runtime/GitHub components are built and published by global-release.yml.'
