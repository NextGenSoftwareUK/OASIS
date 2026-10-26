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
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenEscrowConstructor,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenEscrowDeploy,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenEscrowReconcile,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$Psp22TokenConstructor,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$Psp22TokenDeploy,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$Psp22TokenReconcile,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenEscrowV1Reconcile,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenEscrowPreparation,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenEscrowLifecycle,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenEscrowLifecycleRecover,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenPublicApproval,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenPublicApprovalRecover,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenPublic,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenPublicRecover,
    [Parameter(ParameterSetName = 'TokenSdk')][switch]$TokenPublicReleaseVerifiedLock,
    [Parameter(ParameterSetName = 'NftPublicMutation')][switch]$NftPublicMutation,
    [Parameter(ParameterSetName = 'NftPublicRecover')][switch]$NftPublicRecover,
    [Parameter(ParameterSetName = 'CSharpStorage')][switch]$CSharpStorage,
    [Parameter(ParameterSetName = 'CSharpStorage')][switch]$RecoverCompleted,
    [ValidateSet('1','2')][string]$TokenPublicCohort='1'
)
$ErrorActionPreference = 'Stop'
# Group related token SDK modes rather than exceeding PowerShell's 32-set
# command limit. Keep their exclusive-mode invariant before filesystem/RPC.
if (@(@($TokenEscrowConstructor,$TokenEscrowDeploy,$TokenEscrowReconcile,
    $Psp22TokenConstructor,$Psp22TokenDeploy,$Psp22TokenReconcile,$TokenEscrowV1Reconcile,$TokenEscrowPreparation,
    $TokenEscrowLifecycle,$TokenEscrowLifecycleRecover,$TokenPublicApproval,$TokenPublicApprovalRecover,$TokenPublic,$TokenPublicRecover,$TokenPublicReleaseVerifiedLock) | Where-Object { $_ }).Count -gt 1) {
    throw 'Select only one token SDK mode.'
}
if ($RecoverCompleted -and !$CSharpStorage) { throw 'RecoverCompleted requires CSharpStorage; no other suite may enter recovery.' }
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$evidence = Join-Path $repo 'TestResults/PolkadotCurrent'
$sdk = Join-Path $repo 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/SdkBridge'
if (!(Test-Path -LiteralPath $evidence -PathType Container)) { throw 'Reuse the retained environment.' }
$lease = [IO.FileStream]::new((Join-Path $evidence 'local-native-environment.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$publicEvidence = Join-Path $repo 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot'
$escrowPublicGate = $NftEscrowPublic -or $NftEscrowPublicRecover
$tokenPublicGate = $TokenPublic -or $TokenPublicRecover -or $TokenPublicReleaseVerifiedLock
$custodyGate = $NftCustody -or $NftCustodyRecover
$recordEvidence = if ($tokenPublicGate -or $escrowPublicGate -or $custodyGate -or $CSharpStorage -or $NftPublicRead -or $NftPublicMutation -or $NftPublicRecover) { $publicEvidence } else { $evidence }
$prior = $env:OASIS_POLKADOT_LOCAL_NATIVE
$priorBridge = $env:OASIS_POLKADOT_SDK_BRIDGE
$priorEvidence = $env:OASIS_POLKADOT_RECORD_EVIDENCE
$priorRecovery = $env:OASIS_POLKADOT_RECORD_RECOVERY
$priorNftRecovery = $env:OASIS_POLKADOT_NFT_PUBLIC_RECOVERY
$priorCustodyRecovery = $env:OASIS_POLKADOT_NFT_CUSTODY_RECOVERY
$priorEscrowRecovery = $env:OASIS_POLKADOT_NFT_ESCROW_RECOVERY
$priorTokenRecovery = $env:OASIS_POLKADOT_TOKEN_PUBLIC_RECOVERY
$priorTokenRelease = $env:OASIS_POLKADOT_TOKEN_PUBLIC_RELEASE
$priorTokenCohort = $env:OASIS_POLKADOT_TOKEN_PUBLIC_COHORT
try {
    $tokenPrefix=if($TokenPublicCohort -eq '1'){'psp22-public'}else{'psp22-public-postfix'}
    foreach($prefix in @('psp22-public','psp22-public-postfix')) {
        if ((Test-Path -LiteralPath (Join-Path $publicEvidence ($prefix+'-approval-pending.jsonl'))) -and !($TokenPublicApprovalRecover -and $prefix -eq $tokenPrefix)) { throw 'Pending original exists: public token approval. Reconcile read-only, never replay.' }
        if ((Test-Path -LiteralPath (Join-Path $publicEvidence ($prefix+'-pending.jsonl'))) -and !(($TokenPublicRecover -or $TokenPublicReleaseVerifiedLock) -and $prefix -eq $tokenPrefix)) { throw 'Pending original exists: public token custody. Reconcile read-only, never replay.' }
    }
    $nftDeploymentPending = Join-Path $publicEvidence 'psp34-deployment-pending.json'
    if ((Test-Path -LiteralPath $nftDeploymentPending) -and !$NftReconcile) { throw 'Pending original exists: NFT deployment. Reconcile read-only, never replay.' }
    $escrowDeploymentPending = Join-Path $publicEvidence 'psp34-escrow-deployment-pending.json'
    if ((Test-Path -LiteralPath (Join-Path $publicEvidence 'psp22-escrow-deployment-pending.json')) -and !$TokenEscrowReconcile) { throw 'Pending original exists: token escrow deployment. Reconcile read-only, never replay.' }
    if ((Test-Path -LiteralPath (Join-Path $publicEvidence 'psp22-test-token-deployment-pending.json')) -and !$Psp22TokenReconcile) { throw 'Pending original exists: PSP22 acceptance asset deployment. Reconcile read-only, never replay.' }
    if ((Test-Path -LiteralPath (Join-Path $publicEvidence 'psp22-escrow-v1-deployment-pending.json')) -and !$TokenEscrowV1Reconcile) { throw 'Pending original exists: retired token escrow deployment. Reconcile read-only, never replay.' }
    if ((Test-Path -LiteralPath (Join-Path $publicEvidence 'psp22-escrow-lifecycle-pending.jsonl')) -and !$TokenEscrowLifecycleRecover) { throw 'Pending original exists: native PSP22 escrow lifecycle. Reconcile read-only, never replay.' }
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
    $env:OASIS_POLKADOT_TOKEN_PUBLIC_RECOVERY = if ($TokenPublicRecover) { '1' } else { $null }
    $env:OASIS_POLKADOT_TOKEN_PUBLIC_RELEASE = if ($TokenPublicReleaseVerifiedLock) { '1' } else { $null }
    $env:OASIS_POLKADOT_TOKEN_PUBLIC_COHORT = $TokenPublicCohort
    if ($tokenPublicGate -or $escrowPublicGate -or $custodyGate -or $CSharpBackend -or $CSharpStorage -or $NftPublicRead -or $NftPublicMutation -or $NftPublicRecover) {
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
            $nativeClass = if ($tokenPublicGate) { 'PolkadotTokenEscrowNativeTests' } elseif ($escrowPublicGate) { 'PolkadotNftEscrowNativeTests' } elseif ($custodyGate) { 'PolkadotNftCustodyNativeTests' } elseif ($NftPublicMutation -or $NftPublicRecover) { 'PolkadotNftPublicMutationNativeTests' } elseif ($NftPublicRead) { 'PolkadotNftPublicReadNativeTests' } elseif ($CSharpStorage) { 'PolkadotPublicStorageNativeTests' } else { 'PolkadotRecordBackendNativeTests' }
            $reportName = if ($TokenPublicReleaseVerifiedLock) { 'psp22-public-release-current.trx' } elseif ($TokenPublicRecover) { 'psp22-public-recovery-current.trx' } elseif ($TokenPublic) { 'psp22-public-native-current.trx' } elseif ($NftEscrowPublicRecover) { 'psp34-escrow-public-recovery-current.trx' } elseif ($NftEscrowPublic) { 'psp34-escrow-public-native-current.trx' } elseif ($NftCustodyRecover) { 'psp34-custody-recovery-current.trx' } elseif ($NftCustody) { 'psp34-custody-native-current.trx' } elseif ($NftPublicRecover) { 'psp34-public-mutation-recovery-current.trx' } elseif ($NftPublicMutation) { 'psp34-public-mutation-native-current.trx' } elseif ($NftPublicRead) { 'psp34-public-native-current.trx' } elseif ($CSharpStorage) { 'public-storage-native-current.trx' } else { 'record-backend-native-current.trx' }
            if($tokenPublicGate -and $TokenPublicCohort -eq '2'){$reportName=$reportName.Replace('psp22-public-','psp22-public-postfix-')}
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
        if ($TokenEscrowV1Reconcile) { & node verify-record-contract-artifact.mjs token-escrow-v1 }
        elseif ($Psp22TokenConstructor -or $Psp22TokenDeploy -or $Psp22TokenReconcile) { & node verify-record-contract-artifact.mjs psp22-test-token }
        elseif ($TokenEscrowConstructor -or $TokenEscrowDeploy -or $TokenEscrowReconcile -or $TokenEscrowPreparation -or $TokenEscrowLifecycle -or $TokenEscrowLifecycleRecover -or $TokenPublicApproval -or $TokenPublicApprovalRecover) { & node verify-record-contract-artifact.mjs token-escrow }
        elseif ($NftEscrowConstructor -or $NftEscrowDeploy -or $NftEscrowReconcile -or $NftEscrowLifecycle -or $NftEscrowLifecycleRecover) { & node verify-record-contract-artifact.mjs psp34-escrow }
        elseif ($NftCustodySource -or $NftConstructorSimulation -or $NftDeploy -or $NftReconcile -or $NftPreparation -or $NftLifecycle -or $NftLifecycleRecover -or $NftIntentRecovery) { & node verify-record-contract-artifact.mjs psp34 }
        else { & node verify-record-contract-artifact.mjs }
        if ($LASTEXITCODE -ne 0) { throw 'Artifact admission failed.' }
        if ($TokenPublicApproval) { & node test-local-psp22-lifecycle.mjs --approve-public }
        elseif ($TokenPublicApprovalRecover) { & node test-local-psp22-lifecycle.mjs --reconcile-public-approval }
        elseif ($TokenEscrowV1Reconcile) { & node bootstrap-local-psp34.mjs --reconcile --token-escrow-v1 }
        elseif ($Psp22TokenConstructor) { & node test-local-psp34-constructor.mjs --psp22-test-token }
        elseif ($Psp22TokenDeploy) { & node bootstrap-local-psp34.mjs --deploy --psp22-test-token }
        elseif ($Psp22TokenReconcile) { & node bootstrap-local-psp34.mjs --reconcile --psp22-test-token }
        elseif ($TokenEscrowConstructor) { & node test-local-psp34-constructor.mjs --token-escrow }
        elseif ($TokenEscrowDeploy) { & node bootstrap-local-psp34.mjs --deploy --token-escrow }
        elseif ($TokenEscrowReconcile) { & node bootstrap-local-psp34.mjs --reconcile --token-escrow }
        elseif ($TokenEscrowPreparation) { & node test-local-psp22-preparation.mjs }
        elseif ($TokenEscrowLifecycle) { & node test-local-psp22-lifecycle.mjs --execute }
        elseif ($TokenEscrowLifecycleRecover) { & node test-local-psp22-lifecycle.mjs --reconcile }
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
    $env:OASIS_POLKADOT_TOKEN_PUBLIC_RECOVERY = $priorTokenRecovery
    $env:OASIS_POLKADOT_TOKEN_PUBLIC_RELEASE = $priorTokenRelease
    $env:OASIS_POLKADOT_TOKEN_PUBLIC_COHORT = $priorTokenCohort
    $lease.Dispose()
}
