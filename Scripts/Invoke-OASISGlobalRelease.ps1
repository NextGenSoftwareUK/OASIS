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
    [ValidateSet('Automatic', 'Patch', 'Minor', 'Major', 'Manual')]
    [string]$VersionMode = 'Automatic',
    [string]$ManualVersion = '',
    [bool]$AllowBlockedPlan = $true,
    [bool]$AdvanceWeb4ToWeb6ApiVersions = $false,
    [bool]$AdvanceWeb4ApiVersion = $false,
    [bool]$AdvanceWeb5ApiVersion = $false,
    [bool]$AdvanceWeb6ApiVersion = $false,
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
$planBlockers = [Collections.Generic.List[string]]::new()
$versionDecisions = [Collections.Generic.List[object]]::new()
$selectedVersionedComponents = @($OASISRuntime, $STARRuntime, $OGEngineClient, $NativeEndpoint, $MCPServer, $OurWorld, $ODOOM, $OQUAKE, $OIDE, $ONODEManager, $HyperDriveClient) | Where-Object { $_ }
if ($VersionMode -eq 'Manual' -and $selectedVersionedComponents.Count -ne 1) {
    throw 'Manual version mode requires exactly one selected component.'
}

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
        Where-Object {
            $relative = $_.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
            ("/$relative") -notmatch $excluded
        } |
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

function Get-LatestNuGetRelease([string]$PackageId) {
    if ($Offline) { return $null }
    $url = "https://api.nuget.org/v3/registration5-semver1/$($PackageId.ToLowerInvariant())/index.json"
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get -Headers @{ 'User-Agent' = 'OASIS-global-release' }
        $entries = foreach ($page in $response.items) {
            if ($page.items) { $page.items.catalogEntry }
            else {
                $expanded = Invoke-RestMethod -Uri $page.'@id' -Method Get -Headers @{ 'User-Agent' = 'OASIS-global-release' }
                $expanded.items.catalogEntry
            }
        }
        $latest = @($entries |
            Where-Object { $_.listed -ne $false -and $_.version -match '^\d+\.\d+\.\d+$' } |
            Sort-Object { [version]$_.version } -Descending |
            Select-Object -First 1)
        if ($latest.Count -eq 0) { return $null }
        return [pscustomobject]@{ Version = $latest[0].version; Published = $latest[0].published }
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 404) { return $null }
        throw
    }
}

function Get-ReleaseLabelOverride([string]$Repository, $Commits) {
    if (-not $env:GH_TOKEN -or -not $Repository) { return $null }
    $headers = @{ Authorization="Bearer $($env:GH_TOKEN)"; 'User-Agent'='OASIS-global-release'; Accept='application/vnd.github+json' }
    $labels = foreach ($commit in @($Commits | Select-Object -First 50)) {
        try {
            $pulls = @(Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/commits/$($commit.Sha)/pulls" -Headers $headers)
            foreach ($pull in $pulls) { foreach ($label in $pull.labels) { $label.name } }
        } catch { throw "Could not inspect release labels for $Repository commit $($commit.Sha): $($_.Exception.Message)" }
    }
    foreach ($candidate in 'release:major','release:minor','release:patch','release:none') { if ($candidate -in $labels) { return $candidate } }
    return $null
}

function Get-RequestedVersion([string]$Baseline, [string[]]$Paths, [string[]]$TagPrefixes, [string]$GitRoot = $repoRoot) {
    $current = [version]$Baseline
    if ($VersionMode -eq 'Manual') {
        if ($ManualVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Manual mode requires a stable X.Y.Z ManualVersion.' }
        if ([version]$ManualVersion -le $current) { throw "Manual version $ManualVersion must be greater than $Baseline." }
        $versionDecisions.Add([pscustomobject]@{ scope=($Paths -join ', '); current=$Baseline; proposed=$ManualVersion; bump='Manual'; reason='Explicit reviewed version.' })
        return $ManualVersion
    }
    $bump = $VersionMode
    if ($bump -eq 'Automatic') {
        $tags = foreach ($tag in @(& git -C $GitRoot tag --list)) {
            foreach ($prefix in $TagPrefixes) {
                if ($tag -match ('^' + [regex]::Escape($prefix) + '(?<version>\d+\.\d+\.\d+)$')) { [pscustomobject]@{ Tag = $tag; Version = [version]$Matches.version } }
            }
        }
        $previous = @($tags | Sort-Object Version -Descending | Select-Object -First 1)
        $range = if ($previous.Count) { "$($previous[0].Tag)..HEAD" } else { 'HEAD' }
        $commits = @(& git -C $GitRoot log $range --no-merges --format='%H%x09%s' -- @Paths | ForEach-Object { $parts=$_ -split "`t",2; if($parts.Count -eq 2 -and $parts[1] -notmatch '^(Merge |Promote |chore: bump submodule|chore: update submodule)'){[pscustomobject]@{Sha=$parts[0];Subject=$parts[1]}} })
        $subjects = @($commits.Subject)
        if ($subjects.Count -eq 0) {
            $message = "Automatic versioning found no relevant changes in: $($Paths -join ', ')."
            if ($Operation -eq 'Plan' -and $AllowBlockedPlan) { $planBlockers.Add($message); return $Baseline }
            throw $message
        }
        $remote = (& git -C $GitRoot remote get-url origin 2>$null)
        $repository = if ($remote -match 'github\.com[/:](?<repo>[^/]+/[^/.]+)(?:\.git)?$') { $Matches.repo } else { $null }
        $override = Get-ReleaseLabelOverride $repository $commits
        if ($override -eq 'release:none') { $message="A release:none label blocks changes in $repository."; if($Operation -eq 'Plan' -and $AllowBlockedPlan){$planBlockers.Add($message);return $Baseline};throw $message }
        if ($override) { $bump = (Get-Culture).TextInfo.ToTitleCase($override.Split(':')[1]) }
        elseif (@($subjects | Where-Object { $_ -match 'BREAKING CHANGE|^[a-z]+(?:\([^)]+\))?!:' }).Count) { $bump = 'Major' }
        elseif (@($subjects | Where-Object { $_ -match '^(feat|add|implement)(?:\([^)]+\))?:|\b(add|introduc|implement|support|expand)' }).Count) { $bump = 'Minor' }
        else { $bump = 'Patch' }
    }
    $proposed = switch ($bump) {
        Major { '{0}.0.0' -f ($current.Major + 1) }
        Minor { '{0}.{1}.0' -f $current.Major, ($current.Minor + 1) }
        Patch { '{0}.{1}.{2}' -f $current.Major, $current.Minor, ($current.Build + 1) }
    }
    $reason = if ($VersionMode -eq 'Automatic') { "Automatic classification of commits since the previous component tag selected $bump." } else { "Explicit $bump mode." }
    $versionDecisions.Add([pscustomobject]@{ scope=($Paths -join ', '); current=$Baseline; proposed=$proposed; bump=$bump; reason=$reason })
    return $proposed
}

function Get-LatestNuGetVersion([string]$PackageId) {
    $release = Get-LatestNuGetRelease $PackageId
    if ($release) { return $release.Version }
    return $null
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
    if ($VersionMode -eq 'Automatic') {
        $headers = @{ 'User-Agent' = 'OASIS-global-release' }
        if ($env:GH_TOKEN) { $headers.Authorization = "Bearer $($env:GH_TOKEN)" }
        $releases = @(Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/releases?per_page=100" -Headers $headers) | Where-Object { -not $_.draft -and -not $_.prerelease }
        $published = @($releases | Where-Object { $_.tag_name -match [regex]::Escape($publishedVersion) } | Sort-Object published_at -Descending | Select-Object -First 1)
        $since = if ($published.Count) { [uri]::EscapeDataString(([datetime]$published[0].published_at).ToUniversalTime().ToString('o')) } else { $null }
        $commits = if ($since) { @(Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/commits?since=$since&per_page=100" -Headers $headers) } else { @() }
        $subjects = @($commits | ForEach-Object { $_.commit.message -split "`n" | Select-Object -First 1 })
        if ($subjects.Count -eq 0) {
            $message = "Automatic versioning found no changes in $Repository since its latest public release."
            if ($Operation -eq 'Plan' -and $AllowBlockedPlan) { $planBlockers.Add($message); return $publishedVersion }
            throw $message
        }
        $current = [version]$publishedVersion
        $override = Get-ReleaseLabelOverride $Repository @($commits | ForEach-Object { [pscustomobject]@{ Sha=$_.sha } })
        if ($override -eq 'release:none') { $message="A release:none label blocks changes in $Repository."; if($Operation -eq 'Plan' -and $AllowBlockedPlan){$planBlockers.Add($message);return $publishedVersion};throw $message }
        if ($override -eq 'release:major' -or (-not $override -and @($subjects | Where-Object { $_ -match 'BREAKING CHANGE|^[a-z]+(?:\([^)]+\))?!:' }).Count)) { $bump='Major'; $proposed='{0}.0.0' -f ($current.Major + 1) }
        elseif ($override -eq 'release:minor') { $bump='Minor'; $proposed='{0}.{1}.0' -f $current.Major, ($current.Minor + 1) }
        elseif ($override -eq 'release:patch') { $bump='Patch'; $proposed=Get-NextPatchVersion $publishedVersion }
        elseif (@($subjects | Where-Object { $_ -match '^(feat|add|implement)(?:\([^)]+\))?:|\b(add|introduc|implement|support|expand)' }).Count) { $bump='Minor'; $proposed='{0}.{1}.0' -f $current.Major, ($current.Minor + 1) }
        else { $bump='Patch'; $proposed=Get-NextPatchVersion $publishedVersion }
        $versionDecisions.Add([pscustomobject]@{ scope=$Repository; current=$publishedVersion; proposed=$proposed; bump=$bump; reason="Automatic classification of owning-repository commits selected $bump." })
        return $proposed
    }
    return Get-RequestedVersion $publishedVersion @('.') @()
}

function Get-NextReleaseVersion([string]$SourceVersion, [string[]]$TagPrefixes, [string[]]$Paths) {
    $publishedVersion = Get-LatestGitHubReleaseVersion $TagPrefixes
    $baseline = if ($publishedVersion -and [version]$publishedVersion -gt [version]$SourceVersion) { $publishedVersion } else { $SourceVersion }
    return Get-RequestedVersion $baseline $Paths $TagPrefixes
}

function Get-NextRegistryReleaseVersion([string]$SourceVersion, [string[]]$TagPrefixes, [string]$PackageId, [string[]]$Paths) {
    $candidates = [Collections.Generic.List[version]]::new()
    $candidates.Add([version]$SourceVersion)
    $githubVersion = Get-LatestGitHubReleaseVersion $TagPrefixes
    if ($githubVersion) { $candidates.Add([version]$githubVersion) }
    $nugetVersion = Get-LatestNuGetVersion $PackageId
    if ($nugetVersion) { $candidates.Add([version]$nugetVersion) }
    return Get-RequestedVersion (($candidates | Sort-Object -Descending | Select-Object -First 1).ToString()) $Paths $TagPrefixes
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

function Get-PlannedPackageVersion($Project, $PublishedRelease) {
    $sourceVersion = Get-SourceVersion $Project
    $publishedVersion = if ($PublishedRelease) { $PublishedRelease.Version } else { $null }
    if (-not $publishedVersion) {
        if ($Offline) { return $sourceVersion }
        return '1.0.0'
    }
    $baseline = if ([version]$publishedVersion -gt [version]$sourceVersion) { $publishedVersion } else { $sourceVersion }
    return Get-NextPatchVersion $baseline
}

function Get-PackageReleaseNotes($Project, [string]$Version, $PublishedRelease) {
    $description = Get-ProjectProperty $Project.Xml 'Description'
    $relativeProject = $Project.File.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
    $projectDirectory = Split-Path $relativeProject -Parent
    $rangeDescription = if ($PublishedRelease) { "since v$($PublishedRelease.Version)" } else { 'for this initial public release' }
    $gitArgs = @('-C', $repoRoot, 'log', '--no-merges', '--format=%H%x09%s')
    if ($PublishedRelease -and $PublishedRelease.Published) { $gitArgs += "--since=$(([datetime]$PublishedRelease.Published).ToUniversalTime().ToString('o'))" }
    $gitArgs += @('--', $projectDirectory)
    $changes = @(& git @gitArgs | ForEach-Object {
        $parts = $_ -split "`t", 2
        $subject = if ($parts.Count -eq 2) { $parts[1] -replace '^\s*[-*]\s*', '' } else { '' }
        if ($subject -and $subject -notmatch '^(Merge |Promote |chore: bump submodule|chore: update submodule)') {
            [pscustomobject]@{ Sha = $parts[0]; Subject = $subject }
        }
    } | Group-Object Subject | ForEach-Object { $_.Group[0] } | Select-Object -First 50)
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add("$($Project.PackageId) v$Version")
    $lines.Add('')
    $lines.Add($description)
    $lines.Add('')
    $lines.Add("What's new $rangeDescription")
    if ($changes.Count -eq 0) {
        $lines.Add('- Initial validated package contents and metadata for this release line.')
    }
    else {
        foreach ($change in $changes) { $lines.Add("- $($change.Subject) ($($change.Sha.Substring(0, 7)))") }
    }
    $lines.Add('')
    $lines.Add('Full changelog')
    $lines.Add("https://github.com/NextGenSoftwareUK/OASIS/commits/master/$($projectDirectory.Replace('\', '/'))")
    return $lines -join "`n"
}

function Set-ProjectReleaseNotesForPacking([string]$Path, [string]$ReleaseNotes) {
    $content = Get-Content -LiteralPath $Path -Raw
    $escaped = [Security.SecurityElement]::Escape($ReleaseNotes)
    if ($content -match '<PackageReleaseNotes>[\s\S]*?</PackageReleaseNotes>') {
        $content = [regex]::Replace($content, '<PackageReleaseNotes>[\s\S]*?</PackageReleaseNotes>', "<PackageReleaseNotes>$escaped</PackageReleaseNotes>", 1)
    }
    else {
        $content = [regex]::Replace($content, '(?s)(<PropertyGroup(?:\s[^>]*)?>)', "`$1`r`n    <PackageReleaseNotes>$escaped</PackageReleaseNotes>", 1)
    }
    Set-TextPreservingUtf8Bom $Path $content
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
    $bootLoaderPath = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.OASISBootLoader\OASISBootLoader.cs'
    $content = Get-Content -LiteralPath $bootLoaderPath -Raw
    $all = $AdvanceWeb4ToWeb6ApiVersions
    $updates = [ordered]@{}
    if ($all -or $AdvanceWeb4ApiVersion) { $updates.OASISAPIVersion = Get-RequestedVersion $Versions.OASISAPIVersion @('ONODE','OASIS Architecture','Providers','ONET','Edge') @('WEB4-v','OASIS-Runtime-v') }
    if ($all -or $AdvanceWeb5ApiVersion) { $updates.STARAPIVersion = Get-RequestedVersion $Versions.STARAPIVersion @('.') @('WEB5-v','STAR-ODK-Runtime-v') (Join-Path $repoRoot 'STAR ODK') }
    if ($all -or $AdvanceWeb6ApiVersion) { $updates.WEB6APIVersion = Get-RequestedVersion $Versions.WEB6APIVersion @('.') @('WEB6-v','mcp-v') (Join-Path $repoRoot 'WEB6') }
    foreach ($entry in $updates.GetEnumerator()) {
        $pattern = "(?m)(public static string $($entry.Key) \{ get; set; \} = `")\d+\.\d+\.\d+(`";)"
        $content = [regex]::Replace($content, $pattern, "`${1}$($entry.Value)`${2}", 1)
    }
    Set-TextPreservingUtf8Bom $bootLoaderPath $content

    $date = Get-Date -Format 'dd/MM/yy'
    $histories = @(
        @{ Key='OASISAPIVersion'; GitRoot=$repoRoot; Repository='NextGenSoftwareUK/OASIS'; Path = 'ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\OASIS API RELEASE HISTORY.md'; Name = 'WEB4 OASIS API'; Intro = 'WEB4 is the OASIS identity, data, provider, NFT, GeoNFT, inventory, ONET and HyperDrive API.'; Prefixes = @('WEB4-v','OASIS-Runtime-v', 'OASIS-Runtime-'); Paths = @('ONODE', 'OASIS Architecture', 'Providers', 'ONET', 'Edge') },
        @{ Key='STARAPIVersion'; GitRoot=(Join-Path $repoRoot 'STAR ODK'); Repository='NextGenSoftwareUK/STAR-ODK'; Path = 'STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI\STAR API RELEASE HISTORY.md'; PublicPath = 'Docs\API\WEB5-STAR-API-RELEASE-HISTORY.md'; Name = 'WEB5 STAR API'; Intro = 'WEB5 is the STAR gamification and metaverse API for OAPPs, quests, missions, GeoHotSpots, games and STARNET content.'; Prefixes = @('WEB5-v','STAR-ODK-Runtime-v', 'STAR-ODK-Runtime-'); Paths = @('.') },
        @{ Key='WEB6APIVersion'; GitRoot=(Join-Path $repoRoot 'WEB6'); Repository='NextGenSoftwareUK/OASIS-WEB6'; Path = 'WEB6\NextGenSoftware.OASIS.Web6.WebAPI\WEB6 API RELEASE HISTORY.md'; PublicPath = 'Docs\API\WEB6-AI-API-RELEASE-HISTORY.md'; Name = 'WEB6 OASIS AI API'; Intro = 'WEB6 is the unified OASIS AI, agent, orchestration, memory, MCP and model-provider API.'; Prefixes = @('WEB6-v','mcp-v'); Paths = @('.') }
    )
    foreach ($history in $histories) {
        if (-not $updates.Contains($history.Key)) { continue }
        $history.Version = $updates[$history.Key]
        $path = Join-Path $repoRoot $history.Path
        $existing = Get-Content -LiteralPath $path -Raw
        $previousTags = foreach ($tag in @(& git -C $history.GitRoot tag --list)) {
            foreach ($prefix in $history.Prefixes) {
                if ($tag -match ('^' + [regex]::Escape($prefix) + '(?<version>\d+\.\d+\.\d+)$')) { [pscustomobject]@{ Tag = $tag; Version = [version]$Matches.version } }
            }
        }
        $previous = @($previousTags | Sort-Object Version -Descending | Select-Object -First 1)
        $range = if ($previous.Count -gt 0) { "$($previous[0].Tag)..HEAD" } else { 'HEAD' }
        $logArgs = @('-C', $history.GitRoot, 'log', $range, '--no-merges', '--format=%s', '--') + $history.Paths
        $changes = @(& git @logArgs | ForEach-Object { $_ -replace '^\s*[-*]\s*', '' } | Where-Object { $_ -and $_ -notmatch '^(Merge |Promote |chore: bump submodule|chore: update submodule)' } | Select-Object -Unique | Select-Object -First 100 | ForEach-Object { "- $_" })
        if ($ReleaseNotes) { $changes = @("- $ReleaseNotes") + $changes }
        if ($changes.Count -eq 0) { $changes = @('- No API-path changes were detected after the previous release tag; this version records the validated coordinated API source graph.') }
        $changeText = $changes -join "`n"
        $changelog = if ($previous.Count -gt 0) { "https://github.com/$($history.Repository)/compare/$($previous[0].Tag)...HEAD" } else { 'Initial API release; the list above is the complete version changelog.' }
        $entry = "`n----------------------------------------------------------------------------------------------------------------------------`n## $($history.Version) ($date)`n`n$($history.Intro)`n`n### What's new in $($history.Version)`n`n$changeText`n`n### Full changelog`n`n$changelog`n`n- Published by the automated OASIS global release process after CI validation.`n"
        # Release histories are chronological documents: oldest at the top, newest at the bottom.
        $updated = $existing.TrimEnd() + "`n" + $entry
        Set-TextPreservingUtf8Bom $path $updated
        if ($history.PublicPath) {
            $publicPath = Join-Path $repoRoot $history.PublicPath
            New-Item -ItemType Directory -Path (Split-Path $publicPath) -Force | Out-Null
            [IO.File]::WriteAllText($publicPath, (Get-Content -LiteralPath $path -Raw), [Text.UTF8Encoding]::new($false))
        }
    }
    return $updates
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$bootVersions = Get-BootLoaderVersions
$webVersions = $null
if ($AdvanceWeb4ToWeb6ApiVersions -or $AdvanceWeb4ApiVersion -or $AdvanceWeb5ApiVersion -or $AdvanceWeb6ApiVersion) {
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
        $publishedRelease = Get-LatestNuGetRelease $_.PackageId
        $version = Get-PlannedPackageVersion $_ $publishedRelease
        [pscustomobject]@{
            packageId = $_.PackageId
            version = $version
            project = $_.File.FullName.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
            previousVersion = if ($publishedRelease) { $publishedRelease.Version } else { $null }
            releaseNotes = Get-PackageReleaseNotes $_ $version $publishedRelease
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
    advanceWeb4ApiVersion = $AdvanceWeb4ApiVersion
    advanceWeb5ApiVersion = $AdvanceWeb5ApiVersion
    advanceWeb6ApiVersion = $AdvanceWeb6ApiVersion
}
$releaseVersions = [ordered]@{
    oasisRuntime = if ($OASISRuntime) { Get-NextReleaseVersion $bootVersions.OASISRuntimeVersion @('OASIS-Runtime-v') @('OASIS Architecture','ONODE','Providers','ONET','Edge') } else { $bootVersions.OASISRuntimeVersion }
    starRuntime = if ($STARRuntime) { Get-NextReleaseVersion $bootVersions.STARRuntimeVersion @('STAR-ODK-Runtime-v') @('STAR ODK') } else { $bootVersions.STARRuntimeVersion }
    ogEngineClient = if ($OGEngineClient) { Get-NextReleaseVersion (Get-SourceVersion ([pscustomobject]@{ Xml = [xml](Get-Content -LiteralPath (Join-Path $repoRoot 'OASIS Omniverse\OGEngineClient\OGEngineClient.csproj') -Raw); PackageId = 'NextGenSoftware.OGEngine.Client' })) @('OGEngineClient-v', 'STAR-API-CLIENT-v') @('OASIS Omniverse/OGEngineClient','ONET','Edge') } else { 'not-selected' }
    nativeEndpoint = if ($NativeEndpoint) { Get-NextReleaseVersion (Get-SourceVersion ([pscustomobject]@{ Xml = [xml](Get-Content -LiteralPath (Join-Path $repoRoot 'Native EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.csproj') -Raw); PackageId = 'NextGenSoftware.OASIS.API.Native.Integrated.EndPoint' })) @('Native-Endpoint-v', 'v') @('Native EndPoint','OASIS Architecture','ONODE','Providers') } else { 'not-selected' }
    mcpServer = if ($MCPServer) { Get-NextRegistryReleaseVersion (Get-SourceVersion ([pscustomobject]@{ Xml = [xml](Get-Content -LiteralPath (Join-Path $repoRoot 'WEB6\NextGenSoftware.OASIS.MCP.Server\NextGenSoftware.OASIS.MCP.Server.csproj') -Raw); PackageId = 'NextGenSoftware.OASIS.MCP.Server' })) @('mcp-v') 'NextGenSoftware.OASIS.MCP.Server' @('WEB6/NextGenSoftware.OASIS.MCP.Server','WEB6/npm','WEB6/NextGenSoftware.OASIS.Web6.Core','WEB7','WEB8','WEB9','WEB10') } else { 'not-selected' }
    ourWorld = if ($OurWorld) { Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/Our-World' @('v', 'Our-World-v') $true } else { 'not-selected' }
    odoom = if ($ODOOM) { Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/ODOOM' @('ODOOM_v.', 'odoom-v', 'v') $true } else { 'not-selected' }
    oquake = if ($OQUAKE) { Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/OQUAKE' @('OQUAKE_v', 'oquake-v', 'v') $true } else { 'not-selected' }
    oide = if ($OIDE) { Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/OIDE' @('v', 'oide-v') } else { 'not-selected' }
    onodeManager = if ($ONODEManager) { Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/OASIS' @('onode-manager-v') } else { 'not-selected' }
    hyperDriveClient = if ($HyperDriveClient) { Get-NextOptionalReleaseVersion 'NextGenSoftwareUK/OASIS-HyperDrive-Client' @('v', 'hyperdrive-client-v') } else { 'not-selected' }
}

$plan = [ordered]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    sourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
    operation = $Operation
    versionSelection = [ordered]@{ mode = $VersionMode; manualVersion = $ManualVersion }
    components = $components
    releaseVersions = $releaseVersions
    apiVersions = if ($webVersions) { $webVersions } else { 'unchanged' }
    packages = $packagePlan
    blockers = @($planBlockers)
    versionDecisions = @($versionDecisions)
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
                Set-ProjectReleaseNotesForPacking $projectPath $package.releaseNotes
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
