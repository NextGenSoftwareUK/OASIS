[CmdletBinding(DefaultParameterSetName = 'Crud')]
param(
    [Parameter(ParameterSetName = 'ReadBridge')][switch]$ReadBridge,
    [Parameter(ParameterSetName = 'Preparation')][switch]$Preparation,
    [Parameter(ParameterSetName = 'OwnerBootstrap')][switch]$OwnerBootstrap,
    [Parameter(ParameterSetName = 'Execution')][switch]$Execution,
    [Parameter(ParameterSetName = 'CSharpBackend')][switch]$CSharpBackend,
    [Parameter(ParameterSetName = 'NftConstructorSimulation')][switch]$NftConstructorSimulation,
    [Parameter(ParameterSetName = 'NftDeploy')][switch]$NftDeploy,
    [Parameter(ParameterSetName = 'NftReconcile')][switch]$NftReconcile,
    [Parameter(ParameterSetName = 'CSharpStorage')][switch]$CSharpStorage,
    [Parameter(ParameterSetName = 'CSharpStorage')][switch]$RecoverCompleted
)
$ErrorActionPreference = 'Stop'
if ($RecoverCompleted -and !$CSharpStorage) { throw 'RecoverCompleted requires CSharpStorage; no other suite may enter recovery.' }
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$evidence = Join-Path $repo 'TestResults/PolkadotCurrent'
$sdk = Join-Path $repo 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/SdkBridge'
if (!(Test-Path -LiteralPath $evidence -PathType Container)) { throw 'Reuse the retained environment.' }
$lease = [IO.FileStream]::new((Join-Path $evidence 'local-native-environment.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$publicEvidence = Join-Path $repo 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot'
$recordEvidence = if ($CSharpStorage) { $publicEvidence } else { $evidence }
$prior = $env:OASIS_POLKADOT_LOCAL_NATIVE
$priorBridge = $env:OASIS_POLKADOT_SDK_BRIDGE
$priorEvidence = $env:OASIS_POLKADOT_RECORD_EVIDENCE
$priorRecovery = $env:OASIS_POLKADOT_RECORD_RECOVERY
try {
    $nftDeploymentPending = Join-Path $publicEvidence 'psp34-deployment-pending.json'
    if ((Test-Path -LiteralPath $nftDeploymentPending) -and !$NftReconcile) { throw 'Pending original exists: NFT deployment. Reconcile read-only, never replay.' }
    foreach ($name in @('local-native-pending.json','local-bootstrap-pending.json','local-asset-bootstrap-pending.json',
        'local-asset-mutation-pending.json','local-asset-api-pending.json','local-asset-transfer-pending.json','local-record-contract-pending.json','local-record-owner-pending.json','local-record-sdk-pending.json','record-csharp-pending.json','public-storage-pending.jsonl')) {
        if (Test-Path -LiteralPath (Join-Path $evidence $name)) { throw "Pending original exists: $name. Reconcile read-only, never replay." }
    }
    $completedPublic = Join-Path $publicEvidence 'public-storage.completed.json'
    if ($RecoverCompleted -and !(Test-Path -LiteralPath $completedPublic)) { throw 'Read-only recovery requires the existing completed sequence; never start fresh writes.' }
    if ((Test-Path -LiteralPath (Join-Path $publicEvidence 'public-storage-pending.jsonl')) -and !$RecoverCompleted) { throw 'Pending public storage sequence exists; reconcile read-only.' }
    $env:OASIS_POLKADOT_RECORD_RECOVERY = if ($RecoverCompleted) { '1' } else { $null }
    $env:OASIS_POLKADOT_LOCAL_NATIVE = '1'
    if ($CSharpBackend -or $CSharpStorage) {
        # Caller must hold the global provider build lease for this whole runner.
        $env:OASIS_POLKADOT_SDK_BRIDGE = Join-Path $sdk 'bridge.mjs'
        $env:OASIS_POLKADOT_RECORD_EVIDENCE = $recordEvidence
        Push-Location $repo
        try {
            $provider = 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.csproj'
            $unit = 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests.csproj'
            & dotnet build $provider --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false --verbosity quiet
            if ($LASTEXITCODE -ne 0) { throw 'Provider build failed; no restore/download attempted.' }
            & dotnet build $unit --no-restore /p:IncludePolkadotRecordNativeTests=true /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false --verbosity quiet
            if ($LASTEXITCODE -ne 0) { throw 'Native consumer build failed.' }
            $nativeClass = if ($CSharpStorage) { 'PolkadotPublicStorageNativeTests' } else { 'PolkadotRecordBackendNativeTests' }
            $reportName = if ($CSharpStorage) { 'public-storage-native-current.trx' } else { 'record-backend-native-current.trx' }
            & dotnet test $unit --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter "FullyQualifiedName~$nativeClass" --logger "trx;LogFileName=$reportName" --results-directory $recordEvidence --verbosity quiet -- MSTest.DeploymentEnabled=false
            if ($LASTEXITCODE -ne 0) { throw 'Native C# gate failed; preserve pending original/evidence.' }
            [xml]$report = Get-Content -LiteralPath (Join-Path $recordEvidence $reportName)
            if ($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1') { throw 'Exactly one executed native case required.' }
            return
        } finally { Pop-Location }
    }
    Push-Location $sdk
    try {
        if ($NftConstructorSimulation -or $NftDeploy -or $NftReconcile) { & node verify-record-contract-artifact.mjs psp34 }
        else { & node verify-record-contract-artifact.mjs }
        if ($LASTEXITCODE -ne 0) { throw 'Artifact admission failed.' }
        if ($NftDeploy) { & node bootstrap-local-psp34.mjs --deploy }
        elseif ($NftReconcile) { & node bootstrap-local-psp34.mjs --reconcile }
        elseif ($NftConstructorSimulation) { & node test-local-psp34-constructor.mjs }
        elseif ($Execution) { & node test-local-record-execution.mjs }
        elseif ($OwnerBootstrap) { & node bootstrap-local-record-owner.mjs }
        elseif ($Preparation) { & node --test test-local-record-preparation.mjs }
        elseif ($ReadBridge) { & node --test test-local-record-read-bridge.mjs }
        else { & node test-local-record-contract.mjs }
        if ($LASTEXITCODE -ne 0) { throw 'Native records test failed. Preserve original journals and fixture state.' }
    } finally { Pop-Location }
} finally {
    $env:OASIS_POLKADOT_LOCAL_NATIVE = $prior
    $env:OASIS_POLKADOT_SDK_BRIDGE = $priorBridge
    $env:OASIS_POLKADOT_RECORD_EVIDENCE = $priorEvidence
    $env:OASIS_POLKADOT_RECORD_RECOVERY = $priorRecovery
    $lease.Dispose()
}
