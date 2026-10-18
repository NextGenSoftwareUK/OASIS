param([switch]$Routed)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/HederaRuntime'
if(Test-Path -LiteralPath (Join-Path $work 'prepared-read-pending.json')){throw 'Pending native query outcome requires reconciliation; never repeat the paid read.'}
$lease=[IO.File]::Open((Join-Path $work 'prepared-read-run.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$names=@('OASIS_HEDERA_PREPARED_READ_ACCOUNT_ID','OASIS_HEDERA_PREPARED_READ_PRIVATE_KEY')
$prior=@{};foreach($name in $names){$prior[$name]=[Environment]::GetEnvironmentVariable($name)}
try{
    if(Test-Path -LiteralPath (Join-Path $work 'prepared-read-pending.json')){throw 'Pending native query outcome requires reconciliation; never repeat the paid read.'}
    $raw=& node (Join-Path $PSScriptRoot 'inspect-reusable-hedera-store.mjs')
    if($LASTEXITCODE -ne 0){throw 'Read-only exact retained contract inspection failed.'}
    $resource=($raw -join "`n")|ConvertFrom-Json
    $requiredBalance=if($Routed){85000000}else{30000000}
    if(!$resource.mirrorOnly -or $resource.submittedTransactions -ne 0 -or [long]$resource.balanceTinybars -lt $requiredBalance){throw 'Exact retained resource and conservative indexed funding buffer required.'}
    # This export requires an EXISTING DPAPI identity and resolves no new account/key.
    & (Join-Path $PSScriptRoot 'Get-ReusableHederaTestIdentity.ps1') -CheckLedger -ExportForPreparedRead | Out-Null
    $producer=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.csproj'
    & dotnet build $producer --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE -ne 0){throw 'Current Hedera producer build failed.'}
    $output='Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests/bin/Debug/net10.0/HederaSdkBridge'
    & (Join-Path $PSScriptRoot 'Use-ReusableHederaOutputDependencies.ps1') -Provider Hedera -OutputBridge $output
    if($Routed){& (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -HederaPreparedOwners -HederaNativeRead -HederaRoutedRead}
    else{& (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -HederaPreparedOwners -HederaNativeRead}
    if($LASTEXITCODE -ne 0){throw 'Native prepared read failed; retain its original fence.'}
}finally{
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$prior[$name])}
    $lease.Dispose()
}
