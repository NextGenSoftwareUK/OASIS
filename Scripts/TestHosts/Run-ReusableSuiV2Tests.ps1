param([ValidateSet('Healthy','Outage')][string]$Mode = 'Healthy')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$fixtureRoot = Join-Path $taskRoot 'TestResults/HyperDriveSui'
$deploymentPath = '\\wsl.localhost\Ubuntu\var\tmp\oasis-sui\deployment.json'
if (!(Test-Path -LiteralPath $deploymentPath)) { throw 'Retained protected Sui deployment is unavailable; no copy or new wallet created.' }
if (Test-Path -LiteralPath (Join-Path $fixtureRoot 'encrypted-replication-pending.json')) { throw 'Pending Sui ownership intent requires reconciliation before another run.' }
& wsl -d Ubuntu -u root -- test -f /var/tmp/oasis-sui/network/network.yaml
if ($LASTEXITCODE -ne 0) { throw 'Existing Sui ledger configuration missing; refusing genesis creation.' }
& wsl -d Ubuntu -u root -- pgrep -x sui *> $null
if ($LASTEXITCODE -eq 0) { throw 'Existing Sui runtime is in use; refusing to take ownership.' }
$project = Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
& dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'Current V2 integration consumer build failed.' }
& (Join-Path $PSScriptRoot 'Use-ReusableHederaOutputDependencies.ps1') -Provider Sui -OutputBridge 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/bin/Debug/net10.0/SuiSdkBridge'
$names = @('OASIS_SUI_TEST_RPC','OASIS_SUI_TEST_DEPLOYMENT','OASIS_SUI_TEST_OWNED_PID','OASIS_SUI_TEST_STOP_SCRIPT','OASIS_HYPERDRIVE_TEST_ROOT')
$prior = @{}
foreach ($name in $names) { $prior[$name] = [Environment]::GetEnvironmentVariable($name,'Process') }
$ownedPid = $null
$launcher = $null
try {
    # Hidden launcher, retained executable/ledger, fixed compact startup logs.
    $launcher = Start-Process -FilePath 'wsl.exe' -ArgumentList @('-d','Ubuntu','-u','root','--','bash','/mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/start-sui-test-network.sh','--reuse-only') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $fixtureRoot 'node-current.stdout.log') -RedirectStandardError (Join-Path $fixtureRoot 'node-current.stderr.log')
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    while ([DateTime]::UtcNow -lt $deadline) {
        $found = @(& wsl -d Ubuntu -u root -- pgrep -x sui 2>$null)
        if ($found.Count -eq 1) { $ownedPid = $found[0].Trim(); break }
        if ($launcher.HasExited) { throw 'Retained Sui startup terminated; inspect fixed startup logs.' }
        Start-Sleep -Milliseconds 250
    }
    if (!$ownedPid -or $ownedPid -notmatch '^[0-9]+$') { throw 'No uniquely owned Sui PID established.' }
    $executable = (& wsl -d Ubuntu -u root -- readlink -f "/proc/$ownedPid/exe").Trim()
    if ($executable -ne '/var/tmp/oasis-runtimes/sui-1.81.1/sui') { throw 'Owned runtime executable mismatch.' }
    $settings = [IO.File]::ReadAllText($deploymentPath) | ConvertFrom-Json
    $chain = $null
    $readinessError = $null
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $chain = Invoke-RestMethod 'http://127.0.0.1:9000' -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"sui_getChainIdentifier","params":[]}' -TimeoutSec 2
            if ($chain.result) { break }
        } catch { $readinessError = $_.Exception.Message }
        Start-Sleep -Milliseconds 250
    }
    if (!$chain.result) { throw "Retained Sui RPC is not ready: $readinessError" }
    # JSON-RPC reports the short legacy identifier; this provider uses the
    # official gRPC SDK's full genesis identifier. Verify at that same boundary.
    $probeInput = @{operation='probe';rpcEndpoint='http://127.0.0.1:9000';network='localnet';chainId=$settings.chainId} | ConvertTo-Json -Compress
    $probeOutput = $probeInput | & node (Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs')
    if ($LASTEXITCODE -ne 0) { throw 'Retained Sui SDK chain verification failed.' }
    $probe = ($probeOutput -join "`n") | ConvertFrom-Json
    if (!$probe.ok -or $probe.result.chainIdentifier -ne $settings.chainId) { throw 'Retained Sui SDK does not match protected deployment chain.' }
    $settings = $null
    $env:OASIS_SUI_TEST_RPC = 'http://127.0.0.1:9000'
    $env:OASIS_SUI_TEST_DEPLOYMENT = $deploymentPath
    $env:OASIS_SUI_TEST_OWNED_PID = $ownedPid
    $env:OASIS_SUI_TEST_STOP_SCRIPT = '/mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/stop-owned-sui-test-node.sh'
    $env:OASIS_HYPERDRIVE_TEST_ROOT = $fixtureRoot
    $method = if ($Mode -eq 'Healthy') { 'V2ReplicatesToSuiAndSelectsFromMeasuredRealProviderReads' } else { 'V2FailsOverToRealStoreAfterSuiFailure' }
    & dotnet test $project --no-build --no-restore --filter "FullyQualifiedName~IntegrationTests.SuiRealProviderRoutingTests.$method" --logger "trx;LogFileName=sui-retained-v2-$($Mode.ToLowerInvariant())-current.trx" --results-directory $fixtureRoot -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Current real Sui V2 verification failed; preserve ownership journal.' }
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$prior[$name],'Process') }
    $settings = $null
    if ($ownedPid) {
        & wsl -d Ubuntu -u root -- test -e "/proc/$ownedPid/exe"
        if ($LASTEXITCODE -eq 0) {
            & wsl -d Ubuntu -u root -- bash /mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/stop-owned-sui-test-node.sh $ownedPid
            if ($LASTEXITCODE -ne 0) { throw 'Owned Sui runtime did not stop; inspect before another run.' }
        }
    }
    if ($launcher -and !$launcher.HasExited -and !$launcher.WaitForExit(10000)) { throw 'Owned WSL launcher has not exited; do not restart.' }
}
