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
    [Parameter(ParameterSetName = 'NftPublicRead')][switch]$NftPublicRead,
    [Parameter(ParameterSetName = 'NftPreparation')][switch]$NftPreparation,
    [Parameter(ParameterSetName = 'NftLifecycle')][switch]$NftLifecycle,
    [Parameter(ParameterSetName = 'NftLifecycleRecover')][switch]$NftLifecycleRecover,
    [Parameter(ParameterSetName = 'NftIntentRecovery')][switch]$NftIntentRecovery,
    [Parameter(ParameterSetName = 'NftCustodySource')][switch]$NftCustodySource,
    [Parameter(ParameterSetName = 'NftCustody')][switch]$NftCustody,
    [Parameter(ParameterSetName = 'NftCustodyRecover')][switch]$NftCustodyRecover,
    [Parameter(ParameterSetName = 'NftEscrowConstructor')][switch]$NftEscrowConstructor,
    [Parameter(ParameterSetName = 'NftEscrowDeploy')][switch]$NftEscrowDeploy,
    [Parameter(ParameterSetName = 'NftEscrowReconcile')][switch]$NftEscrowReconcile,
    [Parameter(ParameterSetName = 'NftEscrowLifecycle')][switch]$NftEscrowLifecycle,
    [Parameter(ParameterSetName = 'NftEscrowLifecycleRecover')][switch]$NftEscrowLifecycleRecover,
    [Parameter(ParameterSetName = 'NftEscrowPublic')][switch]$NftEscrowPublic,
    [Parameter(ParameterSetName = 'NftEscrowPublicRecover')][switch]$NftEscrowPublicRecover,
    [Parameter(ParameterSetName = 'TokenEscrowConstructor')][switch]$TokenEscrowConstructor,
    [Parameter(ParameterSetName = 'TokenEscrowDeploy')][switch]$TokenEscrowDeploy,
    [Parameter(ParameterSetName = 'TokenEscrowReconcile')][switch]$TokenEscrowReconcile,
    [Parameter(ParameterSetName = 'NftPublicMutation')][switch]$NftPublicMutation,
    [Parameter(ParameterSetName = 'NftPublicRecover')][switch]$NftPublicRecover,
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
$escrowPublicGate = $NftEscrowPublic -or $NftEscrowPublicRecover
$custodyGate = $NftCustody -or $NftCustodyRecover
$recordEvidence = if ($escrowPublicGate -or $custodyGate -or $CSharpStorage -or $NftPublicRead -or $NftPublicMutation -or $NftPublicRecover) { $publicEvidence } else { $evidence }
$prior = $env:OASIS_POLKADOT_LOCAL_NATIVE
$priorBridge = $env:OASIS_POLKADOT_SDK_BRIDGE
$priorEvidence = $env:OASIS_POLKADOT_RECORD_EVIDENCE
$priorRecovery = $env:OASIS_POLKADOT_RECORD_RECOVERY
$priorNftRecovery = $env:OASIS_POLKADOT_NFT_PUBLIC_RECOVERY
$priorCustodyRecovery = $env:OASIS_POLKADOT_NFT_CUSTODY_RECOVERY
$priorEscrowRecovery = $env:OASIS_POLKADOT_NFT_ESCROW_RECOVERY
try {
    $nftDeploymentPending = Join-Path $publicEvidence 'psp34-deployment-pending.json'
    if ((Test-Path -LiteralPath $nftDeploymentPending) -and !$NftReconcile) { throw 'Pending original exists: NFT deployment. Reconcile read-only, never replay.' }
    $escrowDeploymentPending = Join-Path $publicEvidence 'psp34-escrow-deployment-pending.json'
    if ((Test-Path -LiteralPath (Join-Path $publicEvidence 'psp22-escrow-deployment-pending.json')) -and !$TokenEscrowReconcile) { throw 'Pending original exists: token escrow deployment. Reconcile read-only, never replay.' }
    if ((Test-Path -LiteralPath $escrowDeploymentPending) -and !$NftEscrowReconcile) { throw 'Pending original exists: native escrow deployment. Reconcile read-only, never replay.' }
    $escrowLifecyclePending = Join-Path $publicEvidence 'psp34-escrow-lifecycle-pending.jsonl'
    if ((Test-Path -LiteralPath $escrowLifecyclePending) -and !$NftEscrowLifecycleRecover) { throw 'Pending original exists: native escrow lifecycle. Reconcile read-only, never replay.' }
    if ((Test-Path -LiteralPath (Join-Path $publicEvidence 'psp34-escrow-public-pending.jsonl')) -and !$NftEscrowPublicRecover) { throw 'Pending original exists: public native escrow. Reconcile read-only, never replay.' }
    $nftMutationPending = Join-Path $publicEvidence 'psp34-mutation-pending.jsonl'
    if ((Test-Path -LiteralPath $nftMutationPending) -and !$NftLifecycleRecover) { throw 'Pending original exists: NFT mutation. Reconcile read-only, never replay.' }
    $publicNftPending = Join-Path $publicEvidence 'psp34-public-mutation-pending.jsonl'
    if ((Test-Path -LiteralPath $publicNftPending) -and !$NftPublicRecover) { throw 'Pending original exists: public NFT mutation. Reconcile read-only, never replay.' }
    $custodyPending = Join-Path $publicEvidence 'psp34-custody-pending.jsonl'
    if ((Test-Path -LiteralPath $custodyPending) -and !$NftCustodyRecover) { throw 'Pending original exists: NFT custody. Reconcile read-only, never replay.' }
    foreach ($name in @('local-native-pending.json','local-bootstrap-pending.json','local-asset-bootstrap-pending.json',
        'local-asset-mutation-pending.json','local-asset-api-pending.json','local-asset-transfer-pending.json','local-record-contract-pending.json','local-record-owner-pending.json','local-record-sdk-pending.json','record-csharp-pending.json','public-storage-pending.jsonl')) {
        if (Test-Path -LiteralPath (Join-Path $evidence $name)) { throw "Pending original exists: $name. Reconcile read-only, never replay." }
    }
    $completedPublic = Join-Path $publicEvidence 'public-storage.completed.json'
    if ($RecoverCompleted -and !(Test-Path -LiteralPath $completedPublic)) { throw 'Read-only recovery requires the existing completed sequence; never start fresh writes.' }
    if ((Test-Path -LiteralPath (Join-Path $publicEvidence 'public-storage-pending.jsonl')) -and !$RecoverCompleted) { throw 'Pending public storage sequence exists; reconcile read-only.' }
    $env:OASIS_POLKADOT_RECORD_RECOVERY = if ($RecoverCompleted) { '1' } else { $null }
    $env:OASIS_POLKADOT_LOCAL_NATIVE = '1'
    $env:OASIS_POLKADOT_NFT_PUBLIC_RECOVERY = if ($NftPublicRecover) { '1' } else { $null }
    $env:OASIS_POLKADOT_NFT_CUSTODY_RECOVERY = if ($NftCustodyRecover) { '1' } else { $null }
    $env:OASIS_POLKADOT_NFT_ESCROW_RECOVERY = if ($NftEscrowPublicRecover) { '1' } else { $null }
    if ($escrowPublicGate -or $custodyGate -or $CSharpBackend -or $CSharpStorage -or $NftPublicRead -or $NftPublicMutation -or $NftPublicRecover) {
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
            $nativeClass = if ($escrowPublicGate) { 'PolkadotNftEscrowNativeTests' } elseif ($custodyGate) { 'PolkadotNftCustodyNativeTests' } elseif ($NftPublicMutation -or $NftPublicRecover) { 'PolkadotNftPublicMutationNativeTests' } elseif ($NftPublicRead) { 'PolkadotNftPublicReadNativeTests' } elseif ($CSharpStorage) { 'PolkadotPublicStorageNativeTests' } else { 'PolkadotRecordBackendNativeTests' }
            $reportName = if ($NftEscrowPublicRecover) { 'psp34-escrow-public-recovery-current.trx' } elseif ($NftEscrowPublic) { 'psp34-escrow-public-native-current.trx' } elseif ($NftCustodyRecover) { 'psp34-custody-recovery-current.trx' } elseif ($NftCustody) { 'psp34-custody-native-current.trx' } elseif ($NftPublicRecover) { 'psp34-public-mutation-recovery-current.trx' } elseif ($NftPublicMutation) { 'psp34-public-mutation-native-current.trx' } elseif ($NftPublicRead) { 'psp34-public-native-current.trx' } elseif ($CSharpStorage) { 'public-storage-native-current.trx' } else { 'record-backend-native-current.trx' }
            & dotnet test $unit --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter "FullyQualifiedName~$nativeClass" --logger "trx;LogFileName=$reportName" --results-directory $recordEvidence --verbosity quiet -- MSTest.DeploymentEnabled=false
            if ($LASTEXITCODE -ne 0) { throw 'Native C# gate failed; preserve pending original/evidence.' }
            [xml]$report = Get-Content -LiteralPath (Join-Path $recordEvidence $reportName)
            $expectedCases = if ($NftPublicRead) { '3' } else { '1' }
            if ($report.TestRun.ResultSummary.Counters.total -ne $expectedCases -or $report.TestRun.ResultSummary.Counters.passed -ne $expectedCases) { throw "Exactly $expectedCases executed native cases required." }
            return
        } finally { Pop-Location }
    }
    Push-Location $sdk
    try {
        if ($TokenEscrowConstructor -or $TokenEscrowDeploy -or $TokenEscrowReconcile) { & node verify-record-contract-artifact.mjs token-escrow }
        elseif ($NftEscrowConstructor -or $NftEscrowDeploy -or $NftEscrowReconcile -or $NftEscrowLifecycle -or $NftEscrowLifecycleRecover) { & node verify-record-contract-artifact.mjs psp34-escrow }
        elseif ($NftCustodySource -or $NftConstructorSimulation -or $NftDeploy -or $NftReconcile -or $NftPreparation -or $NftLifecycle -or $NftLifecycleRecover -or $NftIntentRecovery) { & node verify-record-contract-artifact.mjs psp34 }
        else { & node verify-record-contract-artifact.mjs }
        if ($LASTEXITCODE -ne 0) { throw 'Artifact admission failed.' }
        if ($TokenEscrowConstructor) { & node test-local-psp34-constructor.mjs --token-escrow }
        elseif ($TokenEscrowDeploy) { & node bootstrap-local-psp34.mjs --deploy --token-escrow }
        elseif ($TokenEscrowReconcile) { & node bootstrap-local-psp34.mjs --reconcile --token-escrow }
        elseif ($NftEscrowConstructor) { & node test-local-psp34-constructor.mjs --escrow }
        elseif ($NftEscrowDeploy) { & node bootstrap-local-psp34.mjs --deploy --escrow }
        elseif ($NftEscrowReconcile) { & node bootstrap-local-psp34.mjs --reconcile --escrow }
        elseif ($NftEscrowLifecycle) { & node test-local-psp34-lifecycle.mjs --execute-escrow }
        elseif ($NftEscrowLifecycleRecover) { & node test-local-psp34-lifecycle.mjs --reconcile-escrow }
        elseif ($NftCustodySource) { & node test-local-psp34-custody-source.mjs }
        elseif ($NftIntentRecovery) { & node test-local-psp34-recovery.mjs }
        elseif ($NftLifecycle) { & node test-local-psp34-lifecycle.mjs --execute }
        elseif ($NftLifecycleRecover) { & node test-local-psp34-lifecycle.mjs --reconcile }
        elseif ($NftPreparation) { & node test-local-psp34-preparation.mjs }
        elseif ($NftDeploy) { & node bootstrap-local-psp34.mjs --deploy }
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
    $env:OASIS_POLKADOT_NFT_PUBLIC_RECOVERY = $priorNftRecovery
    $env:OASIS_POLKADOT_NFT_CUSTODY_RECOVERY = $priorCustodyRecovery
    $env:OASIS_POLKADOT_NFT_ESCROW_RECOVERY = $priorEscrowRecovery
    $lease.Dispose()
}
