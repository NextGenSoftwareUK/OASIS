[CmdletBinding()]
param(
    [ValidateSet('Pack', 'PublishAndUnlist', 'Unlist')]
    [string]$Operation = 'Pack',
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\Docs\Releases\NUGET_INITIAL_VERSION_REPAIR_2026-09-28.json'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\nuget-initial-version-repair'),
    [string]$NuGetApiKey = $env:NUGET_API_KEY,
    [ValidateRange(1, 60)]
    [int]$MutationDelaySeconds = 2
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$manifestPath = [IO.Path]::GetFullPath($ManifestPath)
$outputDirectory = [IO.Path]::GetFullPath($OutputDirectory)

if (-not $manifestPath.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Repair manifest must be inside the repository: $manifestPath"
}
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Repair manifest not found: $manifestPath" }
if ($Operation -in @('PublishAndUnlist', 'Unlist') -and [string]::IsNullOrWhiteSpace($NuGetApiKey)) {
    throw "NUGET_API_KEY is required for $Operation."
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$packages = @($manifest.packages)
if ($packages.Count -eq 0) { throw 'Repair manifest contains no packages.' }
$duplicateIds = @($packages | Group-Object packageId | Where-Object Count -gt 1)
if ($duplicateIds.Count -gt 0) { throw "Repair manifest contains duplicate package IDs: $($duplicateIds.Name -join ', ')" }
foreach ($package in $packages) {
    if ($package.correctVersion -ne '1.0.0') { throw "$($package.packageId) must be repaired to 1.0.0." }
    if (@($package.erroneousVersions).Count -eq 0) { throw "$($package.packageId) has no erroneous versions to unlist." }
}

function Set-ProjectVersion([string]$Path, [string]$Version) {
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -match '<Version>[^<]+</Version>') {
        $content = [regex]::Replace($content, '<Version>[^<]+</Version>', "<Version>$Version</Version>")
    }
    else {
        $content = [regex]::Replace($content, '(?s)(<PropertyGroup(?:\s[^>]*)?>)', "`$1`r`n    <Version>$Version</Version>", 1)
    }
    [IO.File]::WriteAllText($Path, $content, [Text.UTF8Encoding]::new($false))
}

function Get-ListedNuGetVersions([string]$PackageId) {
    $index = Invoke-RestMethod "https://api.nuget.org/v3/registration5-semver1/$($PackageId.ToLowerInvariant())/index.json"
    $entries = foreach ($page in $index.items) {
        $items = if ($page.items) { $page.items } else { (Invoke-RestMethod $page.'@id').items }
        foreach ($item in $items) { $item.catalogEntry }
    }
    return @($entries | Where-Object { $_.listed -ne $false } | ForEach-Object { $_.version })
}

function Invoke-NuGetUnlist([string]$PackageId, [string]$Version) {
    $uri = "https://www.nuget.org/api/v2/package/$PackageId/$Version"
    while ($true) {
        $response = Invoke-WebRequest -Uri $uri -Method Delete -Headers @{ 'X-NuGet-ApiKey' = $NuGetApiKey; 'User-Agent' = 'OASIS-NuGet-initial-version-repair' } -SkipHttpErrorCheck
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) { return }
        $retryAfter = [string]$response.Headers['Retry-After']
        if ($response.StatusCode -eq 403 -and $retryAfter -match '^\d+$') {
            $waitSeconds = [int]$retryAfter + 5
            Write-Host "NuGet quota reached while unlisting $PackageId $Version; honoring Retry-After and waiting $waitSeconds seconds."
            Start-Sleep -Seconds $waitSeconds
            continue
        }
        throw "Unlisting $PackageId $Version failed with HTTP $($response.StatusCode): $($response.Content)"
    }
}

if ($Operation -ne 'Unlist') {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    Get-ChildItem -LiteralPath $outputDirectory -Filter '*.nupkg' -File | Remove-Item -Force
    $originalProjects = @{}
    try {
    foreach ($package in $packages) {
        $projectPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $package.project))
        if (-not $projectPath.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) { throw "Project escapes repository: $($package.project)" }
        if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) { throw "Project not found: $($package.project)" }
        $xml = [xml](Get-Content -LiteralPath $projectPath -Raw)
        $declaredId = @($xml.Project.PropertyGroup.PackageId | Where-Object { $_ } | Select-Object -First 1)
        if ($declaredId.Count -eq 0) { $declaredId = @([IO.Path]::GetFileNameWithoutExtension($projectPath)) }
        if ($declaredId[0] -ne $package.packageId) {
            throw "Manifest package ID $($package.packageId) does not match $($declaredId[0]) in $($package.project)."
        }
        $originalProjects[$projectPath] = [IO.File]::ReadAllBytes($projectPath)
        Set-ProjectVersion $projectPath $package.correctVersion
    }
    foreach ($package in $packages) {
        $projectPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $package.project))
        & dotnet pack $projectPath -c Release -o $outputDirectory --nologo
        if ($LASTEXITCODE -ne 0) { throw "Packing $($package.packageId) $($package.correctVersion) failed." }
    }
    }
    finally {
        foreach ($entry in $originalProjects.GetEnumerator()) { [IO.File]::WriteAllBytes($entry.Key, $entry.Value) }
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $built = @{}
    foreach ($file in Get-ChildItem -LiteralPath $outputDirectory -Filter '*.nupkg' -File | Where-Object Name -notlike '*.symbols.nupkg') {
    $archive = [IO.Compression.ZipFile]::OpenRead($file.FullName)
    try {
        $nuspecEntry = @($archive.Entries | Where-Object FullName -like '*.nuspec')
        if ($nuspecEntry.Count -ne 1) { throw "$($file.Name) must contain exactly one nuspec." }
        $reader = [IO.StreamReader]::new($nuspecEntry[0].Open())
        try { $nuspec = [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
        $id = [string]$nuspec.package.metadata.id
        $version = [string]$nuspec.package.metadata.version
        if ($version -ne '1.0.0') { throw "$($file.Name) contains version $version, expected 1.0.0." }
        if ($built.ContainsKey($id)) { throw "Multiple repair packages were built for $id." }
        $built[$id] = $file.FullName
    }
    finally { $archive.Dispose() }
    }
    foreach ($package in $packages) {
        if (-not $built.ContainsKey($package.packageId)) { throw "No 1.0.0 package was built for $($package.packageId)." }
    }
    if ($built.Count -ne $packages.Count) { throw "Built $($built.Count) packages for a $($packages.Count)-package manifest." }
}

if ($Operation -eq 'Pack') {
    Write-Host "Validated $($built.Count) corrected NuGet packages at version 1.0.0."
    exit 0
}

# Publish every corrected package before unlisting any bad version. This keeps the repair atomic from a consumer's perspective.
if ($Operation -eq 'PublishAndUnlist') {
    foreach ($package in $packages) {
        & dotnet nuget push $built[$package.packageId] --source https://api.nuget.org/v3/index.json --api-key $NuGetApiKey --skip-duplicate
        if ($LASTEXITCODE -ne 0) { throw "Publishing $($package.packageId) 1.0.0 failed; no versions were unlisted." }
        Start-Sleep -Seconds $MutationDelaySeconds
    }
}
foreach ($package in $packages) {
    $listedVersions = @(Get-ListedNuGetVersions $package.packageId)
    foreach ($version in @($package.erroneousVersions)) {
        if ($version -notin $listedVersions) {
            Write-Host "$($package.packageId) $version is already unlisted."
            continue
        }
        Invoke-NuGetUnlist $package.packageId $version
        Write-Host "$($package.packageId) $version was unlisted successfully."
        Start-Sleep -Seconds $MutationDelaySeconds
    }
}

Write-Host "Verified the corrected package set and unlisted every manifest-recorded erroneous version."
