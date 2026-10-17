$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$work=Join-Path $taskRoot 'TestResults/HyperDriveTron'
$name='oasis-tron-tre'
$expected='ef2843babd111bde2709159e6eaacf2350977cccee738d62d1bcbd8833c46a12'
$pin='docker.io/tronbox/tre@sha256:cbf2973095933e3ae3237767082e29add2f9b647d7a4aebdfb7a953c6e5656ba'
function Inspect-OwnedRuntime {
    $raw=& wsl -d Ubuntu -u root -- podman inspect $name
    if($LASTEXITCODE -ne 0){throw 'Retained TRON runtime missing; no replacement/download permitted.'}
    $item=($raw -join "`n"|ConvertFrom-Json)[0]
    if($item.Id -ne $expected -or $item.ImageName -ne $pin){throw 'Retained TRON runtime identity mismatch.'}
    return $item
}
$runtime=Inspect-OwnedRuntime
if($runtime.State.Status -ne 'exited'){throw 'Retained runtime is already in use; refusing ownership.'}
if($runtime.HostConfig.Memory -ne 3GB -or $runtime.HostConfig.MemorySwap -ne 3GB){throw 'Reviewed retained3GiB memory limits required; no configuration change attempted.'}
$fence=Join-Path $work 'pending-native-outcome.json'
$expectedFence='9A6B6E580DB63E5F20A91D9A345430FA72670CC041B278E3B44E00BD0FDEBAEE'
if((Get-FileHash -LiteralPath $fence).Hash -ne $expectedFence){throw 'Protected original TRON fence differs; inspect before resuming.'}
$lease=[IO.File]::Open((Join-Path $work 'runtime-inspection.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$failures=[Collections.Generic.List[Exception]]::new();$started=$false
$priorOwner=[Environment]::GetEnvironmentVariable('OASIS_TRON_TEST_OWNED_CONTAINER')
try{
    $project=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
    & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE -ne 0){throw 'TRON inspection consumer build failed.'}
    if((Inspect-OwnedRuntime).State.Status -ne 'exited'){throw 'Retained runtime became active before ownership was acquired.'}
    $started=$true
    & wsl -d Ubuntu -u root -- bash /mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/recover-owned-tron-test-node.sh
    if($LASTEXITCODE -ne 0){throw 'Retained TRON resume failed; preserve state and inspect.'}
    $ready=$false;$readinessError=$null;$deadline=[DateTime]::UtcNow.AddSeconds(90)
    while([DateTime]::UtcNow -lt $deadline){
        if((Inspect-OwnedRuntime).State.Status -ne 'running'){throw 'Retained TRON terminated during readiness; no automatic restart.'}
        $genesis=$null
        try{
            $genesis=Invoke-RestMethod 'http://127.0.0.1:9090/wallet/getblockbynum' -Method Post -ContentType 'application/json' -Body '{"num":0}' -TimeoutSec 2
        }catch{$readinessError=$_.Exception.Message}
        if($genesis.blockID){
            if($genesis.blockID -ne '0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f'){throw 'Retained TRON genesis differs.'}
            $ready=$true;break
        }
        Start-Sleep -Milliseconds 500
    }
    if(!$ready){throw "Retained TRON readiness window elapsed; no restart permitted. Last observation: $readinessError"}
    $env:OASIS_TRON_TEST_OWNED_CONTAINER=$expected
    & dotnet test $project --no-build --no-restore --filter FullyQualifiedName~TronRealProviderRoutingTests.InspectOriginalInterruptedFixtureWithoutMutation --logger 'trx;LogFileName=tron-readonly-inspection-current.trx' --results-directory $work -v:q
    if($LASTEXITCODE -ne 0){throw 'TRON read-only inspection failed; original fence retained.'}
    if((Get-FileHash -LiteralPath $fence).Hash -ne $expectedFence){throw 'Original TRON fence changed during inspection.'}
}catch{$failures.Add($_.Exception)}finally{
    try{
        if($started){
            $runtime=Inspect-OwnedRuntime
            if($runtime.State.Status -eq 'running'){
                & wsl -d Ubuntu -u root -- bash /mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/stop-owned-tron-test-node.sh
                if($LASTEXITCODE -ne 0){throw 'Owned TRON graceful termination signal failed.'}
            }
            if((Inspect-OwnedRuntime).State.Status -ne 'exited'){throw 'Owned TRON has not exited; no escalation/restart performed.'}
        }
    }catch{$failures.Add($_.Exception)}finally{
        if($null -eq $priorOwner){Remove-Item -LiteralPath Env:OASIS_TRON_TEST_OWNED_CONTAINER -ErrorAction SilentlyContinue}
        else{[Environment]::SetEnvironmentVariable('OASIS_TRON_TEST_OWNED_CONTAINER',$priorOwner)}
        $lease.Dispose()
    }
}
if($failures.Count -eq 1){[Runtime.ExceptionServices.ExceptionDispatchInfo]::Capture($failures[0]).Throw()}
if($failures.Count -gt 1){throw [AggregateException]::new('TRON inspection/cleanup failures; original failure first.',$failures)}
