#Requires -Version 7.0
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$hub=Join-Path $PSScriptRoot 'Run-CosmicCqlDemoHub.ps1'
function Listeners {
    @(Get-NetTCPConnection -State Listen -LocalPort 5087,5088,5089,8081,27029,17687 -ErrorAction SilentlyContinue |
        Select-Object LocalPort,OwningProcess | Sort-Object LocalPort,OwningProcess) | ConvertTo-Json -Compress
}
$before=Listeners
$contracts=@(
    @{Provider='SQLite';Runner='Run-CosmicCqlDemo.ps1';Url='http://127.0.0.1:5087/swagger'},
    @{Provider='Databases';Runner='Run-CosmicCqlV1Demo.ps1';Url='http://127.0.0.1:5088/demo'},
    @{Provider='CosmosDB';Runner='Run-ReusableCosmosCqlDemo.ps1';Url='http://127.0.0.1:5088/'},
    @{Provider='IPFS';Runner='Run-ReusableIpfsTests.ps1';Url='http://127.0.0.1:5088/'},
    @{Provider='SOLID';Runner='Run-ReusableSolidReadiness.ps1';Url='http://127.0.0.1:5088/'},
    @{Provider='ActivityPub';Runner='Run-ReusableActivityPubPointDemo.ps1';Url='http://127.0.0.1:5088/'},
    @{Provider='Solana';Runner='Run-ReusableSolanaTests.ps1';Url='http://127.0.0.1:5089/'},
    @{Provider='SolanaMetadata';Runner='Run-ReusableSolanaTests.ps1';Url='http://127.0.0.1:5089/'}
)
$verified=foreach($contract in $contracts){
    foreach($bounded in @($false,$true)){
        $plan=& $hub -Provider $contract.Provider -VerifyOnly:$bounded -ShowPlan
        if($plan.Provider -ne $contract.Provider -or $plan.Runner -ne (Join-Path $PSScriptRoot $contract.Runner) -or
            $plan.Url -ne $contract.Url -or [string]::IsNullOrWhiteSpace($plan.Scope)){
            throw ('Demo discovery contract mismatch: '+$contract.Provider)
        }
        $mode=if($bounded){'bounded verification'}else{'interactive until explicit shutdown'}
        if($plan.Mode -ne $mode){throw 'Demo discovery did not retain the requested lifecycle.'}
        $plan
    }
}
foreach($provider in @('SQLite','Databases','CosmosDB')){
    $denied=$false
    try{& $hub -Provider $provider -ArchiveCompletedDemo -ShowPlan}catch{$denied=$true}
    if(!$denied){throw 'Database fixture lifecycle must not accept protocol archive options.'}
}
foreach($bounded in @($false,$true)){
    $multi=& $hub -Provider ActivityPub -MultiRecord -VerifyOnly:$bounded -ShowPlan
    if($multi.Scope -notmatch 'Three existing public Mastodon statuses' -or $multi.Url -ne 'http://127.0.0.1:5088/'){
        throw 'Multi-record discovery scope mismatch.'
    }
}
$unsupported=$false
try{& $hub -Provider SQLite -MultiRecord -ShowPlan}catch{$unsupported=$true}
if(!$unsupported){throw 'Unenrolled collection launcher must be rejected.'}
$after=Listeners
if($before -cne $after){throw 'Plan-only discovery changed native/demo listeners.'}
$work=Join-Path $root 'TestResults/ProtocolCqlCurrent'
New-Item -ItemType Directory -Path $work -Force | Out-Null
@{profiles=$contracts.Count;plans=@($verified);archiveRejections=3;listenersUnchanged=$true} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $work 'database-hub-routing.json') -Encoding utf8
Write-Output 'PASS: eight profiles, both lifecycle plans, three archive rejections; listeners unchanged.'
