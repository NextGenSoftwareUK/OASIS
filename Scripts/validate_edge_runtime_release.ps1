[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('SqliteMvp', 'HoloEnabled')]
    [string]$Profile = 'HoloEnabled',
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
    [string]$ArtifactsDirectory = 'artifacts/edge-release-validation',
    [string]$HostedMongoSyncReport,
    [string]$HostedMongoProcessKillReport,
    [string]$PhysicalDeviceEvidence,
    [switch]$RequirePhysicalDeviceEvidence
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$holoNetRoot = (Resolve-Path (Join-Path $repoRoot '..\holochain-client-csharp')).Path
$holoHAppRoot = Join-Path (Split-Path $repoRoot -Parent) 'OASIS-Holochain-hApp'
$artifactsPath = Join-Path $repoRoot $ArtifactsDirectory
$sourcePathsFile = Join-Path $PSScriptRoot 'holo_happ_provenance_paths.txt'
. (Join-Path $PSScriptRoot 'holo_happ_provenance.ps1')
$releaseSourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($releaseSourceCommit)) {
    throw 'Unable to resolve the release source commit before validation.'
}

function Assert-ReleaseSourceCommitUnchanged {
    $currentCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($currentCommit)) {
        throw 'Unable to resolve the current release source commit.'
    }
    if ($currentCommit -ne $releaseSourceCommit) {
        throw "Release source changed during validation: started at '$releaseSourceCommit', now at '$currentCommit'. Run the gate against one stable commit."
    }
}
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
if ($RequirePhysicalDeviceEvidence -and [string]::IsNullOrWhiteSpace($PhysicalDeviceEvidence)) {
    throw 'Physical device evidence is required. Pass -PhysicalDeviceEvidence with a completed and hashed Android/iOS acceptance document.'
}

& (Join-Path $repoRoot 'Scripts\test_our_world_device_acceptance_validator.ps1')
if ($LASTEXITCODE -ne 0) {
    throw "Physical-device evidence validator tests failed with exit code $LASTEXITCODE."
}

& (Join-Path $repoRoot 'Scripts\validate_dependency_security.ps1')
if ($LASTEXITCODE -ne 0) {
    throw "Dependency security validation failed with exit code $LASTEXITCODE."
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

function Assert-NoForbiddenRuntimeDependency {
    param(
        [Parameter(Mandatory)][string]$DependencyManifest,
        [Parameter(Mandatory)][string[]]$ForbiddenDependencies
    )
    if (!(Test-Path -LiteralPath $DependencyManifest -PathType Leaf)) {
        throw "Runtime dependency manifest was not generated: '$DependencyManifest'."
    }
    $manifest = Get-Content -LiteralPath $DependencyManifest -Raw | ConvertFrom-Json
    $libraries = @($manifest.libraries.PSObject.Properties.Name)
    foreach ($dependency in $ForbiddenDependencies) {
        $matches = @($libraries | Where-Object {
            $_.Equals($dependency, [StringComparison]::OrdinalIgnoreCase) -or
            $_.StartsWith("$dependency/", [StringComparison]::OrdinalIgnoreCase)
        })
        if ($matches.Count -gt 0) {
            throw "Runtime dependency manifest '$DependencyManifest' contains forbidden dependency '$dependency': $($matches -join ', ')."
        }
    }
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
    $sourceDigest = Get-HoloHAppSourceDigest -SourceRoot $holoHAppRoot -SourcePathsFile $sourcePathsFile
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
    EdgeStore = Join-Path $repoRoot 'Providers\Storage\NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS\NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS.csproj'
    EdgeRuntimeTests = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.Edge.Runtime.UnitTests\NextGenSoftware.OASIS.Edge.Runtime.UnitTests.csproj'
    Contracts = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.Contracts\NextGenSoftware.OASIS.Contracts.csproj'
    HyperDriveSynchronization = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.HyperDrive.Synchronization\NextGenSoftware.OASIS.HyperDrive.Synchronization.csproj'
    SharedOnet = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.ONET\NextGenSoftware.OASIS.ONET.csproj'
    EdgeRuntime = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.Edge.Runtime\NextGenSoftware.OASIS.Edge.Runtime.csproj'
    EdgeOnetRuntime = Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.Edge.ONET.Runtime\NextGenSoftware.OASIS.Edge.ONET.Runtime.csproj'
    EdgeEndpoint = Join-Path $repoRoot 'Native EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge.csproj'
    OGEngineTests = Join-Path $repoRoot 'OASIS Omniverse\OGEngineClient\TestProjects\OGEngine.Client.Tests\OGEngine.Client.Tests.csproj'
    FullEndpoint = Join-Path $repoRoot 'Native EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.csproj'
    StarCli = Join-Path $repoRoot 'STAR ODK\NextGenSoftware.OASIS.STAR.CLI\NextGenSoftware.OASIS.STAR.CLI.csproj'
    HoloUnity = Join-Path $repoRoot 'Providers\Network\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity.csproj'
    HoloUnityTests = Join-Path $repoRoot 'Providers\Network\TestProjects\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity.UnitTests\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity.UnitTests.csproj'
    HoloEdge = Join-Path $repoRoot 'Providers\Network\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge.csproj'
    HoloEdgeTests = Join-Path $repoRoot 'Providers\Network\TestProjects\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge.UnitTests\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge.UnitTests.csproj'
    HoloClient = Join-Path $holoNetRoot 'NextGenSoftware.Holochain.HoloNET.Client\NextGenSoftware.Holochain.HoloNET.Client.csproj'
    HoloClientTests = Join-Path $holoNetRoot 'NextGenSoftware.Holochain.HoloNET.Client.Tests\NextGenSoftware.Holochain.HoloNET.Client.Tests.csproj'
    HoloOrm = Join-Path $repoRoot 'HoloNET-ORM\NextGenSoftware.Holochain.HoloNET.ORM.csproj'
    Mongo = Join-Path $repoRoot 'Providers\Storage\NextGenSoftware.OASIS.API.Providers.MongoOASIS\NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj'
    HyperDriveMigration = Join-Path $repoRoot 'Tools\NextGenSoftware.OASIS.HyperDrive.Migration\NextGenSoftware.OASIS.HyperDrive.Migration.csproj'
    HyperDriveMigrationTests = Join-Path $repoRoot 'Tools\NextGenSoftware.OASIS.HyperDrive.Migration.UnitTests\NextGenSoftware.OASIS.HyperDrive.Migration.UnitTests.csproj'
    OnetTests = Join-Path $repoRoot 'ONODE\TestProjects\NextGenSoftware.OASIS.API.ONODE.Core.UnitTests\NextGenSoftware.OASIS.API.ONODE.Core.UnitTests.csproj'
    OnodeWebApiTests = Join-Path $repoRoot 'ONODE\TestProjects\NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests\NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
    OnodeCommandTests = Join-Path $repoRoot 'ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests\NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
    StarWebApiTests = Join-Path $repoRoot 'STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI.UnitTests\NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.csproj'
    OnodeWebApi = Join-Path $repoRoot 'ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj'
    ReleaseInspector = Join-Path $repoRoot 'Scripts\EdgeReleaseInspector\EdgeReleaseInspector.csproj'
}

$edgeProjects = @($projects.Contracts, $projects.HyperDriveSynchronization, $projects.SharedOnet, $projects.EdgeStore,
    $projects.EdgeRuntime, $projects.EdgeOnetRuntime, $projects.EdgeEndpoint)
if ($Profile -eq 'HoloEnabled') { $edgeProjects += @($projects.HoloUnity, $projects.HoloEdge) }
foreach ($project in $edgeProjects) {
    Assert-NoForbiddenProjectDependency $project
}
if ($Profile -eq 'HoloEnabled') { Assert-HoloHAppArtifactMatchesSource }

New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null
$generatedPatterns = @('*.nupkg', '*.snupkg', '*.trx', 'api-compatibility.json', 'sbom.spdx.json', 'oasis.happ',
    'acceptance-report.json', 'holooasis-happ-build-manifest.json', 'unity-package-build-manifest.json',
    'com.nextgensoftware.oasis.edge.tgz', 'unity-edge-validation.log', 'our-world-edge-integration.log',
    'unity-edge-android-validation.log', 'physical-device-acceptance.json', 'physical-device-evidence.zip',
    'ogengine-native-*.json', 'ogengine-native-*.zip', 'SHA256SUMS.txt')
foreach ($pattern in $generatedPatterns) {
    Get-ChildItem -LiteralPath $artifactsPath -Filter $pattern -File -ErrorAction SilentlyContinue |
        Remove-Item -Force
}
$staleDeviceEvidenceDirectory = Join-Path $artifactsPath 'physical-device-evidence'
if (Test-Path -LiteralPath $staleDeviceEvidenceDirectory) {
    Remove-Item -LiteralPath $staleDeviceEvidenceDirectory -Recurse -Force
}
[IO.File]::WriteAllBytes((Join-Path $artifactsPath 'hosted-mongo-sync.trx'), $hostedMongoSyncReportBytes)
[IO.File]::WriteAllBytes((Join-Path $artifactsPath 'hosted-mongo-process-kill.trx'), $hostedMongoProcessKillReportBytes)
if ($Profile -eq 'HoloEnabled') {
    Copy-Item -LiteralPath (Join-Path $repoRoot 'Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS/OASIS_hAPP/build-manifest.json') `
        -Destination (Join-Path $artifactsPath 'holooasis-happ-build-manifest.json') -Force
    Copy-Item -LiteralPath (Join-Path $repoRoot 'Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS/OASIS_hAPP/oasis.happ') `
        -Destination (Join-Path $artifactsPath 'oasis.happ') -Force
}

# Run serially because the shared multi-target Core project writes one XML documentation file.
Invoke-DotNet @('test', $projects.DnaTests, '--configuration', $Configuration,
    '--logger', 'trx;LogFileName=dna.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.CoreTests, '--configuration', $Configuration,
    '--logger', 'trx;LogFileName=core-hyperdrive.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.EdgeStoreTests, '--configuration', $Configuration,
    '--logger', 'trx;LogFileName=edge-store.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.EdgeRuntimeTests, '--configuration', $Configuration,
    '--logger', 'trx;LogFileName=edge-runtime.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnetTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~ONETRequestResponseTests|FullyQualifiedName~ONETProviderCapabilitySourceTests|FullyQualifiedName~ONETDiscoveryTests', '--logger', 'trx;LogFileName=onet-sync.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnetTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~OASISPersistenceTests', '--logger', 'trx;LogFileName=hyperdrive-persistence.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnetTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~AIOptimizationEngineTests', '--logger', 'trx;LogFileName=hyperdrive-ai.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnetTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~PredictiveFailoverEngineTests', '--logger', 'trx;LogFileName=hyperdrive-predictive-failover.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnetTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~QuestProgressIdempotencyTests', '--logger', 'trx;LogFileName=quest-idempotency.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OGEngineTests, '--configuration', $Configuration,
    # OGEngine's Edge graph converges on the multi-target Native Edge endpoint through
    # Edge.Runtime and Edge.ONET.Runtime. Serialize this build so MSBuild cannot schedule
    # two netstandard inner builds that write the same deps.json concurrently.
    '--maxcpucount:1',
    '--filter', 'FullyQualifiedName~EdgeClientLifecycleTests|FullyQualifiedName~NativeGameplayLifecycleTests', '--logger', 'trx;LogFileName=ogengine-edge-client.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnodeWebApiTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~HyperDrivePeerBindingControllerTests',
    '--logger', 'trx;LogFileName=onet-peer-binding.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnodeWebApiTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~HyperDriveSyncControllerTests',
    '--logger', 'trx;LogFileName=hosted-sync-api.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnodeWebApiTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~KarmaWeightingControllerTests',
    '--logger', 'trx;LogFileName=karma-weighting-policy.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnodeCommandTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~HyperDriveCommandExecutorTests',
    '--logger', 'trx;LogFileName=hosted-command-executor.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.StarWebApiTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~GeoHotSpotsControllerTests',
    '--logger', 'trx;LogFileName=web5-geohotspot-authority.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('test', $projects.OnodeWebApiTests, '--configuration', $Configuration,
    '--filter', 'FullyQualifiedName~HyperDriveOfflineSessionGrantIssuerTests|FullyQualifiedName~JwtMiddlewareTests',
    '--logger', 'trx;LogFileName=offline-session-grants.trx',
    '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('build', $projects.Mongo, '--configuration', $Configuration, '--nologo')
Invoke-DotNet @('test', $projects.HyperDriveMigrationTests, '--configuration', $Configuration,
    '--logger', 'trx;LogFileName=hyperdrive-migration.trx', '--results-directory', $artifactsPath, '--nologo')
Invoke-DotNet @('build', $projects.OnodeWebApi, '--configuration', $Configuration, '--nologo')

# Edge is an additional composition, never a reduction of the existing Full Runtime products.
Invoke-DotNet @('build', $projects.FullEndpoint, '--configuration', $Configuration, '--nologo')
Invoke-DotNet @('build', $projects.StarCli, '--configuration', $Configuration, '--nologo')

# Native games ship OGEngineClient rather than the managed Edge assemblies directly. Validate and package both
# supported deployment profiles here so a release cannot pass with a broken C ABI or unsafe AOT call path.
#
# NativeAOT/MSBuild still reaches Windows APIs with the legacy 260-character path ceiling. Building below the
# caller-selected evidence directory made a valid release depend on the length of that directory name (the failing
# Native Integrated Endpoint intermediate path was exactly 260 characters). Use a short, deterministic staging root
# keyed by the absolute evidence directory, then copy the durable reports/archives into the requested evidence set.
$nativeStageHashAlgorithm = [Security.Cryptography.SHA256]::Create()
try {
    $nativeStageHashBytes = $nativeStageHashAlgorithm.ComputeHash(
        [Text.Encoding]::UTF8.GetBytes([IO.Path]::GetFullPath($artifactsPath).ToUpperInvariant()))
}
finally {
    $nativeStageHashAlgorithm.Dispose()
}
$nativeStageKey = ([BitConverter]::ToString($nativeStageHashBytes).Replace('-', '')).Substring(0, 12)
$nativeStageParent = Join-Path $repoRoot 'artifacts\.native-aot'
$nativeArtifactsPath = Join-Path $nativeStageParent $nativeStageKey
$nativeStageParentFullPath = [IO.Path]::GetFullPath($nativeStageParent).TrimEnd([IO.Path]::DirectorySeparatorChar) +
    [IO.Path]::DirectorySeparatorChar
$nativeArtifactsFullPath = [IO.Path]::GetFullPath($nativeArtifactsPath)
if (!$nativeArtifactsFullPath.StartsWith($nativeStageParentFullPath, [StringComparison]::OrdinalIgnoreCase)) {
    throw "NativeAOT staging path escaped its verified artifacts parent: '$nativeArtifactsFullPath'."
}
if (Test-Path -LiteralPath $nativeArtifactsFullPath) {
    Remove-Item -LiteralPath $nativeArtifactsFullPath -Recurse -Force
}
$nativePublishScript = Join-Path $repoRoot 'OASIS Omniverse\OGEngineClient\Scripts\publish_and_deploy_star_api.ps1'
foreach ($nativeProfile in @('Edge', 'RemoteOnly')) {
    & $nativePublishScript -Runtime win-x64 -Profile $nativeProfile -ArtifactsDirectory $nativeArtifactsPath `
        -RunSmokeTest -ForceBuild -NoDeploy
    if ($LASTEXITCODE -ne 0) {
        throw "OGEngineClient NativeAOT $nativeProfile validation failed with exit code $LASTEXITCODE."
    }

    $profileName = $nativeProfile.ToLowerInvariant()
    $profileRoot = Join-Path $nativeArtifactsPath "$nativeProfile\win-x64"
    $profileReport = Join-Path $profileRoot 'native-profile-report.json'
    $profilePublish = Join-Path $profileRoot 'publish'
    if (!(Test-Path -LiteralPath $profileReport -PathType Leaf) -or
        !(Test-Path -LiteralPath $profilePublish -PathType Container)) {
        throw "OGEngineClient NativeAOT $nativeProfile validation did not produce its report and publish directory."
    }
    Copy-Item -LiteralPath $profileReport -Destination `
        (Join-Path $artifactsPath "ogengine-native-$profileName-win-x64.json") -Force
    Compress-Archive -Path (Join-Path $profilePublish '*') -DestinationPath `
        (Join-Path $artifactsPath "ogengine-native-$profileName-win-x64.zip") -CompressionLevel Optimal -Force
}
Remove-Item -LiteralPath $nativeArtifactsFullPath -Recurse -Force

if ($Profile -eq 'HoloEnabled') {
    Invoke-DotNet @('build', $projects.HoloClient, '--configuration', $Configuration, '--nologo')
    Invoke-DotNet @('test', $projects.HoloClientTests, '--configuration', $Configuration,
        '--logger', 'trx;LogFileName=holonet-app-authentication.trx',
        '--results-directory', $artifactsPath, '--nologo')
    Invoke-DotNet @('build', $projects.HoloOrm, '--configuration', $Configuration, '--nologo')
    Invoke-DotNet @('build', $projects.HoloUnity, '--configuration', $Configuration,
        '--framework', 'netstandard2.1', '--nologo')
    $holoUnityDependencies = Join-Path (Split-Path $projects.HoloUnity -Parent) `
        "bin\$Configuration\netstandard2.1\NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity.deps.json"
    Assert-NoForbiddenRuntimeDependency -DependencyManifest $holoUnityDependencies -ForbiddenDependencies @(
        'Microsoft.AspNetCore.App',
        'Microsoft.Data.SqlClient',
        'Microsoft.Data.SqlClient.SNI.runtime',
        'MongoDB.Driver',
        'OpenTelemetry.Extensions.Hosting',
        'OpenTelemetry.Exporter.OpenTelemetryProtocol'
    )
    Invoke-DotNet @('test', $projects.HoloUnityTests, '--configuration', $Configuration,
        '--logger', 'trx;LogFileName=holooasis-unity.trx', '--results-directory', $artifactsPath, '--nologo')
    Invoke-DotNet @('test', $projects.HoloEdgeTests, '--configuration', $Configuration,
        '--logger', 'trx;LogFileName=holooasis-edge.trx', '--results-directory', $artifactsPath, '--nologo')
}

foreach ($project in $edgeProjects) {
    Invoke-DotNet @('pack', $project, '--configuration', $Configuration, '--output', $artifactsPath, '--nologo')
}

$unityPackageDirectory = Join-Path $artifactsPath 'com.nextgensoftware.oasis.edge'
Assert-ReleaseSourceCommitUnchanged
if (Test-Path -LiteralPath $unityPackageDirectory) { Remove-Item -LiteralPath $unityPackageDirectory -Recurse -Force }
& (Join-Path $repoRoot 'Scripts\build_edge_unity_package.ps1') -Configuration $Configuration `
    -Profile $Profile -OutputDirectory $artifactsPath
if ($LASTEXITCODE -ne 0) { throw "Unity Edge package build failed with exit code $LASTEXITCODE." }
$unityPackageArchive = Join-Path $artifactsPath 'com.nextgensoftware.oasis.edge.tgz'
$unityPackageManifest = Join-Path $unityPackageDirectory 'build-manifest.json'
if (!(Test-Path -LiteralPath $unityPackageArchive -PathType Leaf) -or !(Test-Path -LiteralPath $unityPackageManifest -PathType Leaf)) {
    throw 'The Unity Edge package archive or build manifest was not generated.'
}
$reproducibilityDirectory = Join-Path $artifactsPath '_unity-package-reproducibility'
if (-not $reproducibilityDirectory.StartsWith($artifactsPath + [IO.Path]::DirectorySeparatorChar,
    [StringComparison]::OrdinalIgnoreCase)) { throw 'The reproducibility workspace escaped the release artifacts directory.' }
if (Test-Path -LiteralPath $reproducibilityDirectory) {
    Remove-Item -LiteralPath $reproducibilityDirectory -Recurse -Force
}
try {
    & (Join-Path $repoRoot 'Scripts\build_edge_unity_package.ps1') -Configuration $Configuration `
        -Profile $Profile -OutputDirectory ([IO.Path]::GetRelativePath($repoRoot, $reproducibilityDirectory))
    if ($LASTEXITCODE -ne 0) { throw "Repeat Unity package build failed with exit code $LASTEXITCODE." }
    Assert-ReleaseSourceCommitUnchanged
    $repeatArchive = Join-Path $reproducibilityDirectory 'com.nextgensoftware.oasis.edge.tgz'
    $firstHash = (Get-FileHash -LiteralPath $unityPackageArchive -Algorithm SHA256).Hash
    $repeatHash = (Get-FileHash -LiteralPath $repeatArchive -Algorithm SHA256).Hash
    if ($firstHash -ne $repeatHash) {
        throw "Unity package build is not reproducible: first SHA-256 $firstHash, repeat SHA-256 $repeatHash."
    }
}
finally {
    if (Test-Path -LiteralPath $reproducibilityDirectory) {
        Remove-Item -LiteralPath $reproducibilityDirectory -Recurse -Force
    }
}
Copy-Item -LiteralPath $unityPackageManifest -Destination (Join-Path $artifactsPath 'unity-package-build-manifest.json') -Force
& (Join-Path $repoRoot 'Scripts\validate_ogengine_unity_asset_store_package.ps1') -PackageDirectory `
    ([IO.Path]::GetRelativePath($repoRoot, $unityPackageDirectory)) -Archive `
    ([IO.Path]::GetRelativePath($repoRoot, $unityPackageArchive))
& (Join-Path $repoRoot 'Scripts\validate_edge_unity_package.ps1') -PackageDirectory `
    ([IO.Path]::GetRelativePath($repoRoot, $unityPackageDirectory)) -LogDirectory `
    ([IO.Path]::GetRelativePath($repoRoot, $artifactsPath)) -UnityEditor $UnityEditor
& (Join-Path $repoRoot 'Scripts\validate_our_world_edge_integration.ps1') -UnityEditor $UnityEditor `
    -PackageDirectory ([IO.Path]::GetRelativePath($repoRoot, $unityPackageDirectory)) -LogPath `
    ([IO.Path]::GetRelativePath($repoRoot, (Join-Path $artifactsPath 'our-world-edge-integration.log')))

$packages = @(Get-ChildItem -LiteralPath $artifactsPath -Filter '*.nupkg' -File)
$expectedPackagePrefixes = @(
    'NextGenSoftware.OASIS.Contracts.',
    'NextGenSoftware.OASIS.HyperDrive.Synchronization.',
    'NextGenSoftware.OASIS.ONET.',
    'NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS.',
    'NextGenSoftware.OASIS.Edge.Runtime.',
    'NextGenSoftware.OASIS.Edge.ONET.Runtime.',
    'NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge.'
)
if ($Profile -eq 'HoloEnabled') {
    $expectedPackagePrefixes += @('NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity.',
        'NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge.')
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
Assert-ReleaseSourceCommitUnchanged
$commit = $releaseSourceCommit
if (-not [string]::IsNullOrWhiteSpace($PhysicalDeviceEvidence)) {
    $resolvedPhysicalEvidence = (Resolve-Path -LiteralPath $PhysicalDeviceEvidence).Path
    & (Join-Path $repoRoot 'Scripts\validate_our_world_device_acceptance.ps1') `
        -EvidencePath $resolvedPhysicalEvidence -OutputPath (Join-Path $artifactsPath 'physical-device-acceptance.json')
    if ($LASTEXITCODE -ne 0) { throw "Physical-device acceptance validation failed with exit code $LASTEXITCODE." }
    $deviceEvidence = Get-Content -Raw -LiteralPath (Join-Path $artifactsPath 'physical-device-acceptance.json') | ConvertFrom-Json
    if ([string]$deviceEvidence.sourceCommit -ne $commit.ToLowerInvariant()) {
        throw "Physical-device evidence belongs to commit '$($deviceEvidence.sourceCommit)', not release commit '$commit'."
    }
    $deviceEvidenceSourceRoot = Split-Path -Parent $resolvedPhysicalEvidence
    $deviceEvidenceStaging = Join-Path $artifactsPath 'physical-device-evidence'
    if (Test-Path -LiteralPath $deviceEvidenceStaging) { Remove-Item -LiteralPath $deviceEvidenceStaging -Recurse -Force }
    New-Item -ItemType Directory -Path $deviceEvidenceStaging | Out-Null
    Copy-Item -LiteralPath $resolvedPhysicalEvidence -Destination (Join-Path $deviceEvidenceStaging 'evidence.json')
    foreach ($artifact in @($deviceEvidence.verifiedArtifacts)) {
        $source = [IO.Path]::GetFullPath((Join-Path $deviceEvidenceSourceRoot ([string]$artifact.path)))
        $destination = Join-Path $deviceEvidenceStaging ([string]$artifact.path)
        $destinationParent = Split-Path -Parent $destination
        if ($destinationParent) { New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null }
        Copy-Item -LiteralPath $source -Destination $destination -Force
    }
    Compress-Archive -Path (Join-Path $deviceEvidenceStaging '*') `
        -DestinationPath (Join-Path $artifactsPath 'physical-device-evidence.zip') -CompressionLevel Optimal -Force
    Remove-Item -LiteralPath $deviceEvidenceStaging -Recurse -Force
}
Invoke-DotNet @('run', '--project', $projects.ReleaseInspector, '--configuration', $Configuration, '--',
    $fullAssembly, $edgeAssembly, $artifactsPath, $commit, $Configuration, $Profile)

$requiredReports = @('api-compatibility.json', 'sbom.spdx.json', 'acceptance-report.json',
    'unity-package-build-manifest.json', 'com.nextgensoftware.oasis.edge.tgz',
    'unity-edge-validation.log', 'unity-edge-android-validation.log', 'our-world-edge-integration.log', 'SHA256SUMS.txt')
$requiredReports += @('ogengine-native-edge-win-x64.json', 'ogengine-native-edge-win-x64.zip',
    'ogengine-native-remoteonly-win-x64.json', 'ogengine-native-remoteonly-win-x64.zip')
if (-not [string]::IsNullOrWhiteSpace($PhysicalDeviceEvidence)) {
    $requiredReports += @('physical-device-acceptance.json', 'physical-device-evidence.zip')
}
foreach ($report in $requiredReports) {
    if (!(Test-Path -LiteralPath (Join-Path $artifactsPath $report) -PathType Leaf)) {
        throw "Required Edge release report '$report' was not generated."
    }
}

Write-Host "Edge release validation passed. Packages and provenance reports:"
$packages | Sort-Object Name | ForEach-Object { Write-Host " - $($_.FullName)" }
$requiredReports | ForEach-Object { Write-Host " - $(Join-Path $artifactsPath $_)" }
