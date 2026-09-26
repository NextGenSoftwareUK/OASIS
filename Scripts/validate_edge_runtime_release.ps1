[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('SqliteMvp', 'HoloEnabled')]
    [string]$Profile = 'HoloEnabled',
    [string]$ArtifactsDirectory = 'artifacts/edge-release-validation',
    [string]$HostedMongoSyncReport,
    [string]$HostedMongoProcessKillReport
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$holoNetRoot = (Resolve-Path (Join-Path $repoRoot '..\holochain-client-csharp')).Path
$holoHAppRoot = Join-Path (Split-Path $repoRoot -Parent) 'OASIS-Holochain-hApp'
$artifactsPath = Join-Path $repoRoot $ArtifactsDirectory
$hostedMongoSyncReportPath = if ([string]::IsNullOrWhiteSpace($HostedMongoSyncReport)) { $null } else {
    [IO.Path]::GetFullPath((Join-Path $repoRoot $HostedMongoSyncReport))
}
$hostedMongoProcessKillReportPath = if ([string]::IsNullOrWhiteSpace($HostedMongoProcessKillReport)) { $null } else {
    [IO.Path]::GetFullPath((Join-Path $repoRoot $HostedMongoProcessKillReport))
}
if ($null -eq $hostedMongoSyncReportPath -or !(Test-Path -LiteralPath $hostedMongoSyncReportPath -PathType Leaf)) {
    throw 'A hosted Mongo replica-set integration TRX is required. Pass -HostedMongoSyncReport after running the MongoOASIS integration suite against a transaction-capable replica set.'
}
if ($null -eq $hostedMongoProcessKillReportPath -or !(Test-Path -LiteralPath $hostedMongoProcessKillReportPath -PathType Leaf)) {
    throw 'A hosted Mongo abrupt-primary-termination TRX is required. Pass -HostedMongoProcessKillReport after running the externally coordinated replica-set process-loss test.'
}
$hostedMongoSyncReportBytes = [IO.File]::ReadAllBytes($hostedMongoSyncReportPath)
$hostedMongoProcessKillReportBytes = [IO.File]::ReadAllBytes($hostedMongoProcessKillReportPath)
$artifactsFullPath = [IO.Path]::GetFullPath($artifactsPath)
$repoFullPath = [IO.Path]::GetFullPath($repoRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (!$artifactsFullPath.StartsWith($repoFullPath, [StringComparison]::OrdinalIgnoreCase)) {
    throw "The release artifact directory must remain inside the OASIS repository: '$artifactsFullPath'."
}

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Assert-NoForbiddenProjectDependency {
    param([Parameter(Mandatory)][string]$ProjectPath)
    $content = Get-Content -LiteralPath $ProjectPath -Raw
    $forbidden = @('Microsoft.AspNetCore', 'MongoDB.Driver', 'NextGenSoftware.OASIS.API.ONODE.WebAPI')
    foreach ($dependency in $forbidden) {
        if ($content -match [regex]::Escape($dependency)) {
            throw "Edge project '$ProjectPath' references forbidden server dependency '$dependency'."
        }
    }
}

function Get-HoloHAppSourceDigest {
    param([Parameter(Mandatory)][string]$SourceRoot)
    $sourcePaths = @('Cargo.toml', 'Cargo.lock', 'package.json', 'package-lock.json', 'flake.nix', 'flake.lock', 'dnas', 'tests', 'workdir\happ.yaml')
    $files = foreach ($relativePath in $sourcePaths) {
        $path = Join-Path $SourceRoot $relativePath
        if (Test-Path -LiteralPath $path -PathType Leaf) { Get-Item -LiteralPath $path }
        elseif (Test-Path -LiteralPath $path -PathType Container) { Get-ChildItem -LiteralPath $path -File -Recurse }
        else { throw "Required Holochain hApp source path is missing: $path" }
    }
    $lines = $files | Sort-Object FullName | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($SourceRoot, $_.FullName).Replace('\', '/')
        "$relative=$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Assert-HoloHAppArtifactMatchesSource {
    $happPath = Join-Path $repoRoot 'Providers\Network\NextGenSoftware.OASIS.API.Providers.HoloOASIS\OASIS_hAPP\oasis.happ'
    $manifestPath = Join-Path $repoRoot 'Providers\Network\NextGenSoftware.OASIS.API.Providers.HoloOASIS\OASIS_hAPP\build-manifest.json'
    if (!(Test-Path -LiteralPath $holoHAppRoot -PathType Container)) {
        throw "Holochain hApp source repository is required at '$holoHAppRoot'. Checkout NextGenSoftwareUK/OASIS-Holochain-hApp beside OASIS."
    }
    if (!(Test-Path -LiteralPath $happPath -PathType Leaf) -or !(Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "HoloOASIS hApp artifact or build manifest is missing. Run Scripts/build_holooasis_happ.ps1 before release validation."
    }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $sourceCommit = (& git -C $holoHAppRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sourceCommit)) {
        throw 'Unable to resolve the Holochain hApp source commit.'
    }
    if ($manifest.sourceCommit -ne $sourceCommit) {
        throw "The bundled HoloOASIS hApp manifest belongs to commit '$($manifest.sourceCommit)', not checked-out source '$sourceCommit'."
    }
    $sourceDigest = Get-HoloHAppSourceDigest $holoHAppRoot
    $artifactDigest = (Get-FileHash -LiteralPath $happPath -Algorithm SHA256).Hash
    if ($manifest.sourceSha256 -ne $sourceDigest) {
        throw "The bundled HoloOASIS hApp was not built from the current hApp source tree. Run Scripts/build_holooasis_happ.ps1."
    }
    if ($manifest.artifactSha256 -ne $artifactDigest) {
        throw "The bundled HoloOASIS hApp hash does not match its build manifest. Rebuild it; do not package an unverified binary."
    }
}

$projects = @{
    DnaTests = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.API.DNA.UnitTests\NextGenSoftware.OASIS.API.DNA.UnitTests.csproj'
    CoreTests = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.API.Core.UnitTests\NextGenSoftware.OASIS.API.Core.UnitTests.csproj'
    EdgeStoreTests = Join-Path $repoRoot 'Providers\Storage\NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS.UnitTests\NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS.UnitTests.csproj'
    EdgeRuntimeTests = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.Edge.Runtime.UnitTests\NextGenSoftware.OASIS.Edge.Runtime.UnitTests.csproj'
    Contracts = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.Contracts\NextGenSoftware.OASIS.Contracts.csproj'
    HyperDriveSynchronization = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.HyperDrive.Synchronization\NextGenSoftware.OASIS.HyperDrive.Synchronization.csproj'
    SharedOnet = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.ONET\NextGenSoftware.OASIS.ONET.csproj'
    EdgeRuntime = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.Edge.Runtime\NextGenSoftware.OASIS.Edge.Runtime.csproj'
    EdgeOnetRuntime = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.Edge.ONET.Runtime\NextGenSoftware.OASIS.Edge.ONET.Runtime.csproj'
    EdgeEndpoint = Join-Path $repoRoot 'Native EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge.csproj'
    FullEndpoint = Join-Path $repoRoot 'Native EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.csproj'
    StarCli = Join-Path $repoRoot 'STAR ODK\NextGenSoftware.OASIS.STAR.CLI\NextGenSoftware.OASIS.STAR.CLI.csproj'
    HoloUnity = Join-Path $repoRoot 'Providers\Network\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity.csproj'
    HoloClient = Join-Path $holoNetRoot 'NextGenSoftware.Holochain.HoloNET.Client\NextGenSoftware.Holochain.HoloNET.Client.csproj'
    HoloOrm = Join-Path $holoNetRoot 'NextGenSoftware.Holochain.HoloNET.ORM\NextGenSoftware.Holochain.HoloNET.ORM.csproj'
    Mongo = Join-Path $repoRoot 'Providers\Storage\NextGenSoftware.OASIS.API.Providers.MongoOASIS\NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj'
    HyperDriveMigration = Join-Path $repoRoot 'Tools\NextGenSoftware.OASIS.HyperDrive.Migration\NextGenSoftware.OASIS.HyperDrive.Migration.csproj'
    OnetTests = Join-Path $repoRoot 'ONODE\TestProjects\NextGenSoftware.OASIS.API.ONODE.Core.UnitTests\NextGenSoftware.OASIS.API.ONODE.Core.UnitTests.csproj'
    OnodeWebApiTests = Join-Path $repoRoot 'ONODE\TestProjects\NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests\NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
    OnodeWebApi = Join-Path $repoRoot 'ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj'
    ReleaseInspector = Join-Path $repoRoot 'Scripts\EdgeReleaseInspector\EdgeReleaseInspector.csproj'
}

$edgeProjects = @($projects.Contracts, $projects.HyperDriveSynchronization, $projects.SharedOnet,
    $projects.EdgeRuntime, $projects.EdgeOnetRuntime, $projects.EdgeEndpoint)
if ($Profile -eq 'HoloEnabled') { $edgeProjects += $projects.HoloUnity }
foreach ($project in $edgeProjects) {
    Assert-NoForbiddenProjectDependency $project
}
if ($Profile -eq 'HoloEnabled') { Assert-HoloHAppArtifactMatchesSource }

New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null
$generatedPatterns = @('*.nupkg', '*.snupkg', '*.trx', 'api-compatibility.json', 'sbom.spdx.json',
    'acceptance-report.json', 'holooasis-happ-build-manifest.json', 'unity-package-build-manifest.json',
    'com.nextgensoftware.oasis.edge.tgz', 'unity-edge-validation.log', 'our-world-edge-integration.log',
    'unity-edge-android-validation.log', 'SHA256SUMS.txt')
foreach ($pattern in $generatedPatterns) {
    Get-ChildItem -LiteralPath $artifactsPath -Filter $pattern -File -ErrorAction SilentlyContinue |
        Remove-Item -Force
}
[IO.File]::WriteAllBytes((Join-Path $artifactsPath 'hosted-mongo-sync.trx'), $hostedMongoSyncReportBytes)
[IO.File]::WriteAllBytes((Join-Path $artifactsPath 'hosted-mongo-process-kill.trx'), $hostedMongoProcessKillReportBytes)
if ($Profile -eq 'HoloEnabled') {
    Copy-Item -LiteralPath (Join-Path $repoRoot 'Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS/OASIS_hAPP/build-manifest.json') `
        -Destination (Join-Path $artifactsPath 'holooasis-happ-build-manifest.json') -Force
}

# Run serially because the shared multi-target Core project writes one XML documentation file.
Invoke-DotNet @('test', $projects.DnaTests, '--configuration', $Configuration,
    '--logger', 'trx;LogFileName=dna.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.CoreTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~HyperDrive', '--logger', 'trx;LogFileName=core-hyperdrive.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.EdgeStoreTests, '--configuration', $Configuration,
    '--logger', 'trx;LogFileName=edge-store.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.EdgeRuntimeTests, '--configuration', $Configuration,
    '--logger', 'trx;LogFileName=edge-runtime.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnetTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~ONETRequestResponseTests|FullyQualifiedName~ONETProviderCapabilitySourceTests', '--logger', 'trx;LogFileName=onet-sync.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnodeWebApiTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~HyperDriveOfflineSessionGrantIssuerTests|FullyQualifiedName~JwtMiddlewareTests',
    '--logger', 'trx;LogFileName=offline-session-grants.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('build', $projects.Mongo, '--configuration', $Configuration, '--nologo')
Invoke-DotNet @('build', $projects.HyperDriveMigration, '--configuration', $Configuration, '--nologo')
Invoke-DotNet @('build', $projects.OnodeWebApi, '--configuration', $Configuration, '--nologo')

# Edge is an additional composition, never a reduction of the existing Full Runtime products.
Invoke-DotNet @('build', $projects.FullEndpoint, '--configuration', $Configuration, '--nologo')
Invoke-DotNet @('build', $projects.StarCli, '--configuration', $Configuration, '--nologo')

if ($Profile -eq 'HoloEnabled') {
    Invoke-DotNet @('build', $projects.HoloClient, '--configuration', $Configuration, '--nologo')
    Invoke-DotNet @('build', $projects.HoloOrm, '--configuration', $Configuration, '--nologo')
    Invoke-DotNet @('build', $projects.HoloUnity, '--configuration', $Configuration, '--nologo')
}

foreach ($project in $edgeProjects) {
    Invoke-DotNet @('pack', $project, '--configuration', $Configuration, '--output', $artifactsPath, '--nologo')
}

$unityPackageDirectory = Join-Path $artifactsPath 'com.nextgensoftware.oasis.edge'
if (Test-Path -LiteralPath $unityPackageDirectory) { Remove-Item -LiteralPath $unityPackageDirectory -Recurse -Force }
& (Join-Path $repoRoot 'Scripts\build_edge_unity_package.ps1') -Configuration $Configuration -OutputDirectory $artifactsPath
if ($LASTEXITCODE -ne 0) { throw "Unity Edge package build failed with exit code $LASTEXITCODE." }
$unityPackageArchive = Join-Path $artifactsPath 'com.nextgensoftware.oasis.edge.tgz'
$unityPackageManifest = Join-Path $unityPackageDirectory 'build-manifest.json'
if (!(Test-Path -LiteralPath $unityPackageArchive -PathType Leaf) -or !(Test-Path -LiteralPath $unityPackageManifest -PathType Leaf)) {
    throw 'The Unity Edge package archive or build manifest was not generated.'
}
Copy-Item -LiteralPath $unityPackageManifest -Destination (Join-Path $artifactsPath 'unity-package-build-manifest.json') -Force
& (Join-Path $repoRoot 'Scripts\validate_ogengine_unity_asset_store_package.ps1') -PackageDirectory `
    ([IO.Path]::GetRelativePath($repoRoot, $unityPackageDirectory)) -Archive `
    ([IO.Path]::GetRelativePath($repoRoot, $unityPackageArchive))
& (Join-Path $repoRoot 'Scripts\validate_edge_unity_package.ps1') -PackageDirectory `
    ([IO.Path]::GetRelativePath($repoRoot, $unityPackageDirectory)) -LogDirectory `
    ([IO.Path]::GetRelativePath($repoRoot, $artifactsPath))
& (Join-Path $repoRoot 'Scripts\validate_our_world_edge_integration.ps1')

$packages = @(Get-ChildItem -LiteralPath $artifactsPath -Filter '*.nupkg' -File)
$expectedPackagePrefixes = @(
    'NextGenSoftware.OASIS.Contracts.',
    'NextGenSoftware.OASIS.HyperDrive.Synchronization.',
    'NextGenSoftware.OASIS.ONET.',
    'NextGenSoftware.OASIS.Edge.Runtime.',
    'NextGenSoftware.OASIS.Edge.ONET.Runtime.',
    'NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge.'
)
if ($Profile -eq 'HoloEnabled') {
    $expectedPackagePrefixes += 'NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity.'
}
if ($packages.Count -ne $expectedPackagePrefixes.Count) {
    throw "Expected exactly $($expectedPackagePrefixes.Count) Edge release packages, but found $($packages.Count)."
}
foreach ($prefix in $expectedPackagePrefixes) {
    $matchingPackages = @($packages | Where-Object { $_.Name.StartsWith($prefix, [StringComparison]::Ordinal) })
    if ($matchingPackages.Count -ne 1) {
        throw "Expected exactly one release package whose filename starts with '$prefix', but found $($matchingPackages.Count)."
    }
}

$fullAssembly = Join-Path (Split-Path $projects.FullEndpoint -Parent) "bin\$Configuration\net10.0\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.dll"
$edgeAssembly = Join-Path (Split-Path $projects.EdgeEndpoint -Parent) "bin\$Configuration\netstandard2.1\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge.dll"
$commit = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($commit)) { throw 'Unable to resolve the release source commit.' }
Invoke-DotNet @('run', '--project', $projects.ReleaseInspector, '--configuration', $Configuration, '--',
    $fullAssembly, $edgeAssembly, $artifactsPath, $commit, $Configuration, $Profile)

$requiredReports = @('api-compatibility.json', 'sbom.spdx.json', 'acceptance-report.json',
    'unity-package-build-manifest.json', 'com.nextgensoftware.oasis.edge.tgz',
    'unity-edge-validation.log', 'unity-edge-android-validation.log', 'our-world-edge-integration.log', 'SHA256SUMS.txt')
foreach ($report in $requiredReports) {
    if (!(Test-Path -LiteralPath (Join-Path $artifactsPath $report) -PathType Leaf)) {
        throw "Required Edge release report '$report' was not generated."
    }
}

Write-Host "Edge release validation passed. Packages and provenance reports:"
$packages | Sort-Object Name | ForEach-Object { Write-Host " - $($_.FullName)" }
$requiredReports | ForEach-Object { Write-Host " - $(Join-Path $artifactsPath $_)" }
