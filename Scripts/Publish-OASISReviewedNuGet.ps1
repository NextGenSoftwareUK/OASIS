[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PlanPath,
    [Parameter(Mandatory)][string]$PackageDirectory,
    [switch]$Publish,
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
$http = [Net.Http.HttpClient]::new()
try {
    # Preflight the whole immutable set before pushing any package. NuGet repository
    # signing may change .signature.p7s; every other archive entry must match exactly.
    foreach ($planned in @($plan.packages)) {
        $name = "$($planned.packageId).$($planned.version).nupkg"
        $matches = @($packages | Where-Object Name -eq $name)
        if ($matches.Count -ne 1) { throw "Missing or duplicated reviewed package: $name" }
        $localBytes = [IO.File]::ReadAllBytes($matches[0].FullName)
        $localEntries = Get-PackageEntries $localBytes
        $id = $planned.packageId.ToLowerInvariant()
        $version = $planned.version.ToLowerInvariant()
        $url = "https://api.nuget.org/v3-flatcontainer/$id/$version/$id.$version.nupkg"
        $response = $http.GetAsync($url).GetAwaiter().GetResult()
        try {
            if ([int]$response.StatusCode -eq 404) { $pending.Add($matches[0].FullName); continue }
            $response.EnsureSuccessStatusCode() | Out-Null
            $remoteEntries = Get-PackageEntries ($response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult())
            if ($remoteEntries.Count -ne $localEntries.Count) { throw "Existing immutable package differs: $name" }
            foreach ($entry in $localEntries.Keys) {
                if (-not $remoteEntries.ContainsKey($entry) -or $remoteEntries[$entry] -ne $localEntries[$entry]) {
                    throw "Existing immutable package differs: $name, entry $entry"
                }
            }
            Write-Host "Verified identical published package: $name"
        } finally { $response.Dispose() }
    }
} finally { $http.Dispose() }
Write-Host "Preflight passed. Unpublished packages: $($pending.Count)"
if ($Publish) {
    foreach ($path in $pending) {
        & dotnet nuget push $path --source https://api.nuget.org/v3/index.json --api-key $NuGetApiKey
        if ($LASTEXITCODE -ne 0) { throw "Publication failed: $([IO.Path]::GetFileName($path)). Reconcile the same plan before resuming." }
    }
}
