#Requires -Version 7.0
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$hub=Join-Path $PSScriptRoot 'Run-CosmicCqlDemoHub.ps1'
function Listeners {
    @(Get-NetTCPConnection -State Listen -LocalPort 5087,5088,5089,5090,5091,5092,5096,5097,5098,5099,5100,5101,5102,5103,5104,5105,5106,5108,5110,5111,18679,9000,9123,10000,3030,24567,19944,19945,26657,18084,18085,18086,18547,18002,8081,11521,27029,17687 -ErrorAction SilentlyContinue |
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
foreach($protocol in @('ActivityPub','IPFS','SOLID')){
foreach($bounded in @($false,$true)){
    $multi=& $hub -Provider $protocol -MultiRecord -VerifyOnly:$bounded -ShowPlan
    if($multi.Scope -notmatch 'Three ' -or $multi.Url -ne 'http://127.0.0.1:5088/'){
        throw 'Multi-record discovery scope mismatch.'
    }
}
}
$unsupported=$false
try{& $hub -Provider SQLite -MultiRecord -ShowPlan}catch{$unsupported=$true}
if(!$unsupported){throw 'Unenrolled collection launcher must be rejected.'}
$pairedContracts=@(
    @{Provider='Oracle';Runner='Run-ReusableOracleMongoTests.ps1';Port=5090},
    @{Provider='AWS';Runner='Run-CosmicAwsCqlNative.ps1';Port=5096},
    @{Provider='Ethereum';Runner='Run-CosmicEthereumCqlNative.ps1';Port=5097},
    @{Provider='Polygon';Runner='Run-CosmicEthereumCqlNative.ps1';Port=5098},
    @{Provider='Arbitrum';Runner='Run-CosmicEthereumCqlNative.ps1';Port=5099},
    @{Provider='Google';Runner='Run-CosmicFirestoreCqlNative.ps1';Port=5100},
    @{Provider='CosmosChain';Runner='Run-CosmicCosmosCqlNative.ps1';Port=5101},
    @{Provider='Polkadot';Runner='Run-CosmicPolkadotCqlNative.ps1';Port=5102},
    @{Provider='NEAR';Runner='Run-CosmicNearCqlNative.ps1';Port=5103},
    @{Provider='Base';Runner='Run-CosmicEthereumCqlNative.ps1';Port=5104},
    @{Provider='Avalanche';Runner='Run-CosmicEthereumCqlNative.ps1';Port=5105},
    @{Provider='Optimism';Runner='Run-CosmicEthereumCqlNative.ps1';Port=5125},
    @{Provider='Rootstock';Runner='Run-CosmicEthereumCqlNative.ps1';Port=5126},
    @{Provider='AzureBlob';Runner='Run-CosmicAzureBlobCqlNative.ps1';Port=5106},
    @{Provider='LocalFile';Runner='Run-CosmicLocalFileCqlNative.ps1';Port=5108},
    @{Provider='Sui';Runner='Run-CosmicSuiCqlNative.ps1';Port=5110},
    @{Provider='Redis';Runner='Run-CosmicRedisCqlNative.ps1';Port=5111},
    @{Provider='PostgreSQL';Runner='Run-CosmicPostgreSqlCqlNative.ps1';Port=5112},
    @{Provider='SQLServer';Runner='Run-CosmicSqlServerCqlNative.ps1';Port=5113},
    @{Provider='DuckDB';Runner='Run-CosmicDuckDbCqlNative.ps1';Port=5114},
    @{Provider='Upstash';Runner='Run-CosmicUpstashCqlNative.ps1';Port=5115},
    @{Provider='Litestream';Runner='Run-CosmicLitestreamLinuxDemo.ps1';Port=5116},
    @{Provider='Memcached';Runner='Run-CosmicMemcachedLinuxDemo.ps1';Port=5117},
    @{Provider='Valkey';Runner='Run-CosmicValKeyLinuxDemo.ps1';Port=5118},
    @{Provider='KeyDB';Runner='Run-CosmicKeyDbLinuxDemo.ps1';Port=5119},
    @{Provider='Dragonfly';Runner='Run-CosmicDragonflyLinuxDemo.ps1';Port=5120},
    @{Provider='Garnet';Runner='Run-CosmicGarnetLinuxDemo.ps1';Port=5121},
    @{Provider='PocketBase';Runner='Run-CosmicPocketBaseLinuxDemo.ps1';Port=5122},
    @{Provider='Supabase';Runner='Run-CosmicSupabaseLinuxDemo.ps1';Port=5123},
    @{Provider='Firebase';Runner='Run-CosmicFirebaseCqlNative.ps1';Port=5124},
    @{Provider='CosmosDB';Runner='Run-ReusableCosmosCqlDemo.ps1';Port=5092},
    @{Provider='SolanaMetadata';Runner='Run-ReusableSolanaMongoTests.ps1';Port=5091},
    @{Provider='IPFS';Runner='Run-ReusableProtocolMongoTests.ps1';Port=5088},
    @{Provider='SOLID';Runner='Run-ReusableProtocolMongoTests.ps1';Port=5088},
    @{Provider='ActivityPub';Runner='Run-ReusableProtocolMongoTests.ps1';Port=5088}
)
$paired=foreach($contract in $pairedContracts){
    foreach($bounded in @($false,$true)){
        $plan=& $hub -Provider $contract.Provider -Federated -VerifyOnly:$bounded -ShowPlan
        if(!$plan.Federated -or $plan.Runner -ne (Join-Path $PSScriptRoot $contract.Runner) -or
            $plan.Url -ne ('http://127.0.0.1:'+$contract.Port+'/') -or $plan.Scope -notmatch 'Mongo'){
            throw ('Paired discovery contract mismatch: '+$contract.Provider)
        }
        $expectedMode=if($bounded){'bounded verification'}else{'interactive until explicit shutdown'}
        if($plan.Mode -ne $expectedMode){throw 'Paired lifecycle mode changed.'}
        $plan
    }
}
$pairedRejections=0
foreach($provider in @('SQLite','Databases','Solana')){
    $denied=$false
    try{& $hub -Provider $provider -Federated -ShowPlan}catch{$denied=$true}
    if(!$denied){throw 'Unenrolled paired profile must reject before service start.'}
    $pairedRejections++
}
$denied=$false
try{& $hub -Provider Oracle -ShowPlan}catch{$denied=$true}
if(!$denied){throw 'Oracle requires explicit paired scope.'}
$pairedRejections++
$denied=$false
try{& $hub -Provider AWS -ShowPlan}catch{$denied=$true}
if(!$denied){throw 'AWS requires explicit paired scope.'}
$pairedRejections++
$denied=$false
try{& $hub -Provider Ethereum -ShowPlan}catch{$denied=$true}
if(!$denied){throw 'Ethereum requires explicit paired scope.'}
$pairedRejections++
$denied=$false
try{& $hub -Provider Polygon -ShowPlan}catch{$denied=$true}
if(!$denied){throw 'Polygon requires explicit paired scope.'}
$pairedRejections++
$denied=$false
try{& $hub -Provider Arbitrum -ShowPlan}catch{$denied=$true}
if(!$denied){throw 'Arbitrum requires explicit paired scope.'}
$pairedRejections++
$denied=$false
try{& $hub -Provider Google -ShowPlan}catch{$denied=$true}
if(!$denied){throw 'Google requires explicit paired scope.'}
$pairedRejections++
$denied=$false
try{& $hub -Provider CosmosChain -ShowPlan}catch{$denied=$true}
if(!$denied){throw 'CosmosChain requires explicit paired scope.'}
$pairedRejections++
$oracleArchive=& $hub -Provider Oracle -Federated -ArchiveCompletedDemo -ShowPlan
foreach ($arguments in @(@{Provider='Polkadot'},@{Provider='Polkadot';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='NEAR'},@{Provider='NEAR';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Base'},@{Provider='Base';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Avalanche'},@{Provider='Avalanche';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Optimism'},@{Provider='Optimism';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Rootstock'},@{Provider='Rootstock';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='AzureBlob'},@{Provider='AzureBlob';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='LocalFile'},@{Provider='LocalFile';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Sui'},@{Provider='Sui';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Redis'},@{Provider='Redis';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='PostgreSQL'},@{Provider='PostgreSQL';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='SQLServer'},@{Provider='SQLServer';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='DuckDB'},@{Provider='DuckDB';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Upstash'},@{Provider='Upstash';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Litestream'},@{Provider='Litestream';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Memcached'},@{Provider='Memcached';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Valkey'},@{Provider='Valkey';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='KeyDB'},@{Provider='KeyDB';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Dragonfly'},@{Provider='Dragonfly';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Garnet'},@{Provider='Garnet';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='PocketBase'},@{Provider='PocketBase';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Supabase'},@{Provider='Supabase';Federated=$true;ArchiveCompletedDemo=$true},@{Provider='Firebase'},@{Provider='Firebase';Federated=$true;ArchiveCompletedDemo=$true})) {
    $denied=$false
    try { & $hub @arguments -ShowPlan } catch { $denied=$true }
    if (!$denied) { throw 'Polkadot requires explicit paired scope and preserved source state.' }
    $pairedRejections++
}
if(!$oracleArchive.Federated){throw 'Archive option must not lose explicit paired scope.'}
$after=Listeners
if($before -cne $after){throw 'Plan-only discovery changed native/demo listeners.'}
$work=Join-Path $root 'TestResults/ProtocolCqlCurrent'
New-Item -ItemType Directory -Path $work -Force | Out-Null
@{profiles=$contracts.Count;plans=@($verified);pairedProfiles=$pairedContracts.Count;pairedPlans=@($paired);pairedRejections=$pairedRejections;archiveRejections=3;listenersUnchanged=$true} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $work 'database-hub-routing.json') -Encoding utf8
Write-Output "PASS:$($contracts.Count) single profiles/$(@($verified).Count) plans,$($pairedContracts.Count) paired profiles/$(@($paired).Count) plans,$pairedRejections paired rejections,3 archive rejections; listeners unchanged."
