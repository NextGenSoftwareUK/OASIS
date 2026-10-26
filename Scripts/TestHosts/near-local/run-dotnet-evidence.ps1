[CmdletBinding(DefaultParameterSetName='File')]
param(
    [Parameter(Mandatory,ParameterSetName='File')] [string] $SettingsPath,
    [Parameter(Mandatory,ParameterSetName='Stdin')] [switch] $SettingsFromStdin,
    [Parameter(Mandatory)] [string] $RepositoryRoot,
    [switch] $ReadOnlyStatus,
    [switch] $ReadOnlyStorage,
    [switch] $SignedAcknowledgements,
    [switch] $CheckedRecordWrites,
    [switch] $OrdinaryRecordWrites,
    [switch] $PreparedNativeActions,
    [switch] $PreparedOrdinaryActions,
    [switch] $PreparedStorageActions,
    [switch] $ReconcilePreparedStorage,
    [switch] $PreparedLifecycleActions
)

$ErrorActionPreference = 'Stop'
if($PreparedLifecycleActions -and ($ReadOnlyStatus -or $ReadOnlyStorage -or $SignedAcknowledgements -or $CheckedRecordWrites -or $OrdinaryRecordWrites -or $PreparedNativeActions -or $PreparedOrdinaryActions -or $PreparedStorageActions -or $ReconcilePreparedStorage)){throw 'Choose one explicit native scope.'}
if($ReconcilePreparedStorage -and ($ReadOnlyStatus -or $ReadOnlyStorage -or $SignedAcknowledgements -or $CheckedRecordWrites -or $OrdinaryRecordWrites -or $PreparedNativeActions -or $PreparedOrdinaryActions -or $PreparedStorageActions)){throw 'Choose one explicit native scope.'}
if($PreparedStorageActions -and ($ReadOnlyStatus -or $ReadOnlyStorage -or $SignedAcknowledgements -or $CheckedRecordWrites -or $OrdinaryRecordWrites -or $PreparedNativeActions -or $PreparedOrdinaryActions)){throw 'Choose one explicit native scope.'}
if($PreparedOrdinaryActions -and ($ReadOnlyStatus -or $ReadOnlyStorage -or $SignedAcknowledgements -or $CheckedRecordWrites -or $OrdinaryRecordWrites -or $PreparedNativeActions)){throw 'Choose one explicit native scope.'}
if($PreparedNativeActions -and ($ReadOnlyStatus -or $ReadOnlyStorage -or $SignedAcknowledgements -or $CheckedRecordWrites -or $OrdinaryRecordWrites)){throw 'Choose one explicit native scope.'}
if($OrdinaryRecordWrites -and ($ReadOnlyStatus -or $ReadOnlyStorage -or $SignedAcknowledgements -or $CheckedRecordWrites)){throw 'Choose one explicit native scope.'}
if($ReadOnlyStatus -and $ReadOnlyStorage){throw 'Choose one explicit read-only scope.'}
if($SignedAcknowledgements -and ($ReadOnlyStatus -or $ReadOnlyStorage)){throw 'Signed verification cannot use read-only mode.'}
if($CheckedRecordWrites -and ($ReadOnlyStatus -or $ReadOnlyStorage -or $SignedAcknowledgements)){throw 'Choose one explicit native scope.'}
$settings = if($SettingsFromStdin){[Console]::In.ReadToEnd() | ConvertFrom-Json}else{Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json}
$environmentNames = @(
    'NEAROASIS_RPCENDPOINT',
    'NEAROASIS_NETWORKID',
    'NEAROASIS_CHAINID',
    'NEAROASIS_CONTRACTADDRESS',
    'NEAROASIS_ACCOUNTID',
    'NEAROASIS_PRIVATEKEY',
    'NEAROASIS_FAILED_STATUS_TRANSACTIONID',
    'OASIS_NEAR_NODE',
    'OASIS_NEAR_SDK_BRIDGE',
    'OASIS_NEAR_STORAGE_ENCRYPTION_KEY'
)
$previous=@{}
foreach($name in $environmentNames){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}

try {
    $reportName=if($PreparedLifecycleActions){'near-prepared-lifecycle-current.trx'}elseif($ReconcilePreparedStorage){'near-prepared-storage-reconciliation-current.trx'}elseif($PreparedStorageActions){'near-prepared-storage-current.trx'}elseif($PreparedOrdinaryActions){'near-prepared-ordinary-current.trx'}elseif($PreparedNativeActions){'near-prepared-native-current.trx'}elseif($OrdinaryRecordWrites){'near-ordinary-storage-current.trx'}elseif($ReadOnlyStatus){'near-readonly-status-current.trx'}elseif($ReadOnlyStorage){'near-readonly-storage-current.trx'}elseif($SignedAcknowledgements){'near-signed-ack-current.trx'}elseif($CheckedRecordWrites){'near-checked-compatibility.trx'}else{'near-provider-current.trx'}
    $reportPath=Join-Path $RepositoryRoot "TestResults/NearRuntime/$reportName"
    if(Test-Path -LiteralPath $reportPath){throw 'Preserve the original report before rerunning; refusing evidence overwrite.'}
    foreach ($name in $environmentNames) {
        if($name -eq 'OASIS_NEAR_STORAGE_ENCRYPTION_KEY'){continue}
        if(($SignedAcknowledgements -or $CheckedRecordWrites -or $OrdinaryRecordWrites -or $PreparedNativeActions -or $PreparedOrdinaryActions -or $PreparedStorageActions -or $ReconcilePreparedStorage -or $PreparedLifecycleActions) -and $name -eq 'NEAROASIS_FAILED_STATUS_TRANSACTIONID'){continue}
        if(($ReadOnlyStatus -or $ReadOnlyStorage) -and $name -eq 'NEAROASIS_PRIVATEKEY'){
            # Deliberately invalid signing material: the selected status tests must not sign.
            [Environment]::SetEnvironmentVariable($name,'read-only-no-signing-key','Process')
            continue
        }
        $value = $settings.$name
        if ([string]::IsNullOrWhiteSpace($value)) {
            throw "NEAR evidence setting '$name' is missing."
        }
        [Environment]::SetEnvironmentVariable($name, [string] $value, 'Process')
    }

    if(!$ReadOnlyStatus -and !$SignedAcknowledgements -and !$PreparedNativeActions -and !$PreparedOrdinaryActions){
    # Windows PowerShell 5.1 does not load the DPAPI assembly implicitly.
    Add-Type -AssemblyName System.Security
    $cache=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OASIS/provider-evidence/near'
    $keyPath=Join-Path $cache 'storage-key.dpapi'
    if(!(Test-Path -LiteralPath $keyPath)){
        throw 'Original retained NEAR storage key is missing; recover it before testing the existing encrypted ledger. No replacement is generated.'
    }
    $key=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    try{
        if($key.Length -ne 32){throw 'Retained NEAR storage key is invalid; refusing replacement.'}
        $env:OASIS_NEAR_STORAGE_ENCRYPTION_KEY=[Convert]::ToBase64String($key)
    }finally{[Array]::Clear($key,0,$key.Length)}
    }else{[Environment]::SetEnvironmentVariable('OASIS_NEAR_STORAGE_ENCRYPTION_KEY',$null,'Process')}

    $tests = Join-Path $RepositoryRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests.csproj'
    $results = Join-Path $RepositoryRoot 'TestResults/NearRuntime'
    New-Item -ItemType Directory -Path $results -Force | Out-Null
    $provider=Join-Path $RepositoryRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/NextGenSoftware.OASIS.API.Providers.NEAROASIS.csproj'
    foreach($project in @($provider,$tests)){
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Retained NEAR producer/consumer build failed; no dependency restore attempted.'}
    }
    & (Join-Path $RepositoryRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $RepositoryRoot -ConsumerOutput (Join-Path (Split-Path $tests) 'bin/Debug/net10.0')
    $started=[DateTime]::UtcNow
    $selection=@('--filter','TestCategory!=RetainedReadonlyInspection&TestCategory!=RetainedSignedInspection')
    if($ReadOnlyStatus){$selection=@('--filter','FullyQualifiedName~OfficialSdk_FinalizedFailedOriginalIsFailedNotCompleted|FullyQualifiedName~RetainedUnavailableOriginalRemainsErrorAndPending')}
    if($ReadOnlyStorage){$selection=@('--filter','FullyQualifiedName~RetainedReadonlyStorageTests.RetainedSearchAndNearbyRecordsRemainQueryable')}
    if($SignedAcknowledgements){$selection=@('--filter','FullyQualifiedName~NearSignedAcknowledgementTests.RealSignedCallAndSelfTransferHaveBoundFinalAcknowledgements')}
    if($CheckedRecordWrites){$selection=@('--filter','FullyQualifiedName~NearCheckedWriteNativeTests.RealSdkCheckedWritesRetainEncryptedSnapshotArchiveAndFailedOriginal')}
    if($OrdinaryRecordWrites){$selection=@('--filter','FullyQualifiedName~NearOrdinaryStorageAcknowledgementTests.RealEncryptedPutAndDeleteRequireBoundFinalOriginals')}
    if($PreparedNativeActions){$selection=@('--filter','FullyQualifiedName~NearPreparedNativeTests.RealOriginalsAreFsyncedBeforeSubmissionAndRecoveredWithoutSigningKey')}
    if($PreparedOrdinaryActions){$selection=@('--filter','FullyQualifiedName~NearPreparedNativeTests.OrdinaryInterfacesPersistOriginalsAndRecoverWithoutSigningKey')}
    if($PreparedStorageActions){$selection=@('--filter','FullyQualifiedName~NearPreparedStorageTests.PublicEncryptedHistoryAndConflictsPersistEveryOriginalBeforeSubmission')}
    if($ReconcilePreparedStorage){$selection=@('--filter','FullyQualifiedName~NearPreparedStorageTests.ReconcileFiveKnownOriginalsAndCleanOnlyTheirExactOwnedCiphertexts')}
    if($PreparedLifecycleActions){$selection=@('--filter','FullyQualifiedName~NearPreparedLifecycleTests.PublicNftBridgeWalletAndEncryptedHistoryUseOneExistingNativeOwner')}
    & dotnet test $tests --no-build --no-restore --nologo --logger "trx;LogFileName=$reportName" --results-directory $results /m:2 /nr:false --verbosity quiet @selection
    if ($LASTEXITCODE -ne 0) {
        throw "NEAR provider tests failed with exit code $LASTEXITCODE."
    }
    $report=Get-Item -LiteralPath $reportPath
    if($report.LastWriteTimeUtc -lt $started){throw 'NEAR native report is stale.'}
    [xml]$trx=Get-Content -LiteralPath $report.FullName
    $counts=$trx.TestRun.ResultSummary.Counters
    $expected=if($ReadOnlyStatus){2}elseif($ReadOnlyStorage -or $SignedAcknowledgements -or $CheckedRecordWrites -or $OrdinaryRecordWrites -or $PreparedNativeActions -or $PreparedOrdinaryActions -or $PreparedStorageActions -or $ReconcilePreparedStorage -or $PreparedLifecycleActions){1}else{19}
    if([int]$counts.total -ne $expected -or [int]$counts.passed -ne $expected -or [int]$counts.notExecuted -ne 0){throw "Expected exactly$expected passing native NEAR cases and zero skips."}
}
finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
}
