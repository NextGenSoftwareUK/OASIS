[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PlanPath,
    [Parameter(Mandatory)][string]$PackageDirectory,
    [switch]$Publish,
    [string]$ReportPath = '',
    [string]$NuGetApiKey = $env:NUGET_API_KEY
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression

function Get-PackageEntries([byte[]]$Bytes) {
    $stream = [IO.MemoryStream]::new($Bytes, $false)
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Read)
    try {
        $entries = @{}
        foreach ($entry in $archive.Entries) {
            if ($entry.FullName -eq '.signature.p7s' -or $entry.FullName.EndsWith('/')) { continue }
            if ($entries.ContainsKey($entry.FullName)) { throw "Duplicate package entry: $($entry.FullName)" }
            $content = $entry.Open()
            $hash = [Security.Cryptography.SHA256]::Create()
            try { $entries[$entry.FullName] = [Convert]::ToBase64String($hash.ComputeHash($content)) }
            finally { $hash.Dispose(); $content.Dispose() }
        }
        return $entries
    } finally { $archive.Dispose(); $stream.Dispose() }
}

$plan = Get-Content -LiteralPath $PlanPath -Raw | ConvertFrom-Json
$packages = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter '*.nupkg' -File |
    Where-Object { $_.Name -notlike '*.symbols.nupkg' })
if ($packages.Count -ne @($plan.packages).Count) { throw 'Packed package count differs from the reviewed plan.' }
if ($Publish -and [string]::IsNullOrWhiteSpace($NuGetApiKey)) { throw 'NUGET_API_KEY is required.' }
$pending = [Collections.Generic.List[string]]::new()
$collisions = [Collections.Generic.List[string]]::new()
$reconciliation = [Collections.Generic.List[object]]::new()
$http = [Net.Http.HttpClient]::new()
try {
    # Preflight the whole immutable set before pushing any package. NuGet repository
    # signing may change .signature.p7s; every other archive entry must match exactly.
    foreach ($planned in @($plan.packages)) {
        $name = "$($planned.packageId).$($planned.version).nupkg"
        $matches = @($packages | Where-Object Name -eq $name)
        if ($matches.Count -ne 1) { throw "Missing or duplicated reviewed package: $name" }
        $localBytes = [IO.File]::ReadAllBytes($matches[0].FullName)
        $identityStream = [IO.MemoryStream]::new($localBytes, $false)
        $identityArchive = [IO.Compression.ZipArchive]::new($identityStream, [IO.Compression.ZipArchiveMode]::Read)
        try {
            $nuspecs = @($identityArchive.Entries | Where-Object FullName -like '*.nuspec')
            if ($nuspecs.Count -ne 1) { throw "Expected exactly one package manifest: $name" }
            $reader = [IO.StreamReader]::new($nuspecs[0].Open())
            try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
            if (-not $metadata) { throw "Missing package metadata: $name" }
            $actualId = $metadata.SelectSingleNode("*[local-name()='id']").InnerText
            $actualVersion = $metadata.SelectSingleNode("*[local-name()='version']").InnerText
            if ($actualId -ne $planned.packageId -or $actualVersion -ne $planned.version) {
                throw "Package manifest does not match reviewed identity: $name"
            }
            if ($Publish) {
                $repository = $metadata.SelectSingleNode("*[local-name()='repository']")
                if (-not $repository -or $repository.GetAttribute('commit') -ne $plan.sourceCommit -or
                    $repository.GetAttribute('url').TrimEnd('/') -ne 'https://github.com/NextGenSoftwareUK/OASIS') {
                    throw "Package repository provenance does not match the reviewed parent source graph: $name"
                }
            }
        } finally { $identityArchive.Dispose(); $identityStream.Dispose() }
        $localEntries = Get-PackageEntries $localBytes
        $id = $planned.packageId.ToLowerInvariant()
        $version = $planned.version.ToLowerInvariant()
        $url = "https://api.nuget.org/v3-flatcontainer/$id/$version/$id.$version.nupkg"
        $response = $http.GetAsync($url).GetAwaiter().GetResult()
        try {
            if ([int]$response.StatusCode -eq 404) { $pending.Add($matches[0].FullName); $reconciliation.Add([pscustomobject]@{packageId=$planned.packageId;version=$planned.version;status='unpublished'}); continue }
            $response.EnsureSuccessStatusCode() | Out-Null
            $remoteEntries = Get-PackageEntries ($response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult())
            if ($remoteEntries.Count -ne $localEntries.Count) { $collisions.Add("$name (archive entry coverage)"); $reconciliation.Add([pscustomobject]@{packageId=$planned.packageId;version=$planned.version;status='collision';reason='archive entry coverage'}); continue }
            $differentEntries = [Collections.Generic.List[string]]::new()
            foreach ($entry in $localEntries.Keys) {
                if (-not $remoteEntries.ContainsKey($entry) -or $remoteEntries[$entry] -ne $localEntries[$entry]) {
                    $differentEntries.Add($entry)
                }
            }
            if ($differentEntries.Count) { $collisions.Add("$name (entries: $($differentEntries -join ', '))"); $reconciliation.Add([pscustomobject]@{packageId=$planned.packageId;version=$planned.version;status='collision';entries=@($differentEntries)}); continue }
            $reconciliation.Add([pscustomobject]@{packageId=$planned.packageId;version=$planned.version;status='identical'})
            Write-Host "Verified identical published package: $name"
        } finally { $response.Dispose() }
    }
} finally { $http.Dispose() }
if ($ReportPath) {
    $reportFile = [IO.Path]::GetFullPath($ReportPath)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($reportFile)) | Out-Null
    [IO.File]::WriteAllText($reportFile, (ConvertTo-Json -InputObject @($reconciliation.ToArray()) -Depth 6), [Text.UTF8Encoding]::new($false))
}
if ($collisions.Count) {
    foreach ($collision in $collisions) { Write-Host "Immutable content collision: $collision" }
    throw "Preflight rejected $($collisions.Count) content collisions; no packages were pushed. Unpublished candidates: $($pending.Count)."
}
Write-Host "Preflight passed. Unpublished packages: $($pending.Count)"
if ($Publish) {
    foreach ($path in $pending) {
        & dotnet nuget push $path --source https://api.nuget.org/v3/index.json --api-key $NuGetApiKey
        if ($LASTEXITCODE -ne 0) { throw "Publication failed: $([IO.Path]::GetFileName($path)). Reconcile the same plan before resuming." }
    }
}
