#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('SQLite','IPFS','SOLID','ActivityPub','Solana','SolanaMetadata','Databases','CosmosDB','Oracle','AWS','Ethereum','Polygon','Arbitrum','Google','CosmosChain','Polkadot','NEAR')][string]$Provider='SQLite',
    [switch]$VerifyOnly,
    [switch]$Federated,
    [switch]$MultiRecord,
    [switch]$ArchiveCompletedDemo,
    [switch]$ShowPlan
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
# Paired runners already own their build lease and fixture lifecycle. Dispatch
# before the single-provider lease below, so no nested lease can be acquired.
if($Provider -in @('Oracle','AWS','Ethereum','Polygon','Arbitrum','Google','CosmosChain','Polkadot','NEAR') -and !$Federated){throw 'This provider requires its explicit Mongo-paired teaching preset; specify -Federated.'}
if($Federated){
    if($Provider -notin @('CosmosDB','Oracle','AWS','Ethereum','Polygon','Arbitrum','Google','CosmosChain','Polkadot','NEAR','SolanaMetadata','IPFS','SOLID','ActivityPub')){throw 'This provider has no enrolled Mongo-paired preset.'}
    if($MultiRecord -and $Provider -notin @('IPFS','SOLID','ActivityPub')){throw 'MultiRecord is a protocol-only option.'}
    if($Provider -in @('CosmosDB','AWS','Ethereum','Polygon','Arbitrum','Google','CosmosChain','Polkadot','NEAR') -and $ArchiveCompletedDemo){throw 'This paired source preserves its original seed and evidence; omit ArchiveCompletedDemo.'}
    $pairedRunner=switch($Provider){
        'CosmosDB' {'Run-ReusableCosmosCqlDemo.ps1'}
        'Oracle' {'Run-ReusableOracleMongoTests.ps1'}
        'AWS' {'Run-CosmicAwsCqlNative.ps1'}
        'Ethereum' {'Run-CosmicEthereumCqlNative.ps1'}
        'Polygon' {'Run-CosmicEthereumCqlNative.ps1'}
        'Arbitrum' {'Run-CosmicEthereumCqlNative.ps1'}
        'Google' {'Run-CosmicFirestoreCqlNative.ps1'}
        'CosmosChain' {'Run-CosmicCosmosCqlNative.ps1'}
        'Polkadot' {'Run-CosmicPolkadotCqlNative.ps1'}
        'NEAR' {'Run-CosmicNearCqlNative.ps1'}
        'SolanaMetadata' {'Run-ReusableSolanaMongoTests.ps1'}
        default {'Run-ReusableProtocolMongoTests.ps1'}
    }
    $url=switch($Provider){
        'CosmosDB' {'http://127.0.0.1:5092/'}
        'Oracle' {'http://127.0.0.1:5090/'}
        'AWS' {'http://127.0.0.1:5096/'}
        'Ethereum' {'http://127.0.0.1:5097/'}
        'Polygon' {'http://127.0.0.1:5098/'}
        'Arbitrum' {'http://127.0.0.1:5099/'}
        'Google' {'http://127.0.0.1:5100/'}
        'CosmosChain' {'http://127.0.0.1:5101/'}
        'Polkadot' {'http://127.0.0.1:5102/'}
        'NEAR' {'http://127.0.0.1:5103/'}
        'SolanaMetadata' {'http://127.0.0.1:5091/'}
        default {'http://127.0.0.1:5088/'}
    }
    $scope=switch($Provider){
        'CosmosDB' {'Mongo + local Azure Cosmos emulator:5rows/4subset; hosted Azure remains separate'}
        'Oracle' {'Mongo + retained Oracle Database:5rows/4subset; no hosted OCI claim'}
        'AWS' {'Mongo + DynamoDB Local:5rows/4subset; bounded native keys, no hosted AWS or shared-snapshot claim'}
        'Ethereum' {'Mongo + retained local EVM chain31338:5rows/4subset; bounded keys, no mainnet or shared-snapshot claim'}
        'Polygon' {'Mongo + concrete Polygon adapter on local EVM chain31338:5rows/4subset; no mainnet or shared-snapshot claim'}
        'Arbitrum' {'Mongo + shipping Arbitrum V2 Web3Core adapter on local EVM chain31338:5rows/4subset; no rollup/mainnet/finality claim'}
        'Google' {'Mongo + retained Firestore emulator:5rows/4subset; bounded native keys, no hosted Google or shared-snapshot claim'}
        'CosmosChain' {'Mongo + retained local Cosmos blockchain:5rows/4subset; encrypted native keys, no Cosmos Hub/mainnet/snapshot claim'}
        'Polkadot' {'Mongo + retained local Polkadot chain:5rows/4subset; encrypted native keys, no public-network/snapshot claim'}
        'NEAR' {'Mongo + retained local NEAR chain:5rows/4subset; original encrypted source, no signing/mainnet/snapshot claim'}
        'SolanaMetadata' {'Mongo native + retained Solana via explicit SQLite index:2matching/0empty with read assurance; no source writes'}
        default {"Mongo + $Provider bounded native membership:5rows/4subset; per-provider provenance"}
    }
    $path=Join-Path $PSScriptRoot $pairedRunner
    if($ShowPlan){
        [pscustomobject]@{Provider=$Provider;Federated=$true;Mode=$(if($VerifyOnly){'bounded verification'}else{'interactive until explicit shutdown'});Runner=$path;Url=$url;Scope=$scope}
        return
    }
    switch($Provider){
        'CosmosDB' { & $path -Mongo -Serve:(!$VerifyOnly) }
        'AWS' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Ethereum' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Polygon' { & $path -Provider Polygon -Federated -Serve:(!$VerifyOnly) }
        'Arbitrum' { & $path -Provider Arbitrum -Federated -Serve:(!$VerifyOnly) }
        'Google' { & $path -Federated -Serve:(!$VerifyOnly) }
        'CosmosChain' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Polkadot' { & $path -Federated -Serve:(!$VerifyOnly) }
        'NEAR' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Oracle' { & $path -ArchiveCompletedDemo:$ArchiveCompletedDemo -Serve:(!$VerifyOnly) }
        'SolanaMetadata' { & $path -ArchiveCompletedDemo:$ArchiveCompletedDemo -Serve:(!$VerifyOnly) }
        default { & $path -Provider $Provider -ArchiveCompletedDemo:$ArchiveCompletedDemo -Serve:(!$VerifyOnly) }
    }
    return
}

$runner=switch($Provider){
    'SQLite' {'Scripts/TestHosts/Run-CosmicCqlDemo.ps1'}
    'Databases' {'Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1'}
    'CosmosDB' {'Scripts/TestHosts/Run-ReusableCosmosCqlDemo.ps1'}
    'Solana' {'Scripts/TestHosts/Run-ReusableSolanaTests.ps1'}
    'SolanaMetadata' {'Scripts/TestHosts/Run-ReusableSolanaTests.ps1'}
    'IPFS' {'Scripts/TestHosts/Run-ReusableIpfsTests.ps1'}
    'SOLID' {'Scripts/TestHosts/Run-ReusableSolidReadiness.ps1'}
    'ActivityPub' {'Scripts/TestHosts/Run-ReusableActivityPubPointDemo.ps1'}
}
if($Provider -in @('SQLite','Databases','CosmosDB') -and $ArchiveCompletedDemo){throw 'Database demos own their fixture lifecycle; protocol archive options do not apply.'}
if($MultiRecord -and $Provider -notin @('ActivityPub','IPFS','SOLID')){throw 'MultiRecord launcher is enrolled only for IPFS/SOLID/ActivityPub.'}
if($ShowPlan){
    [pscustomobject]@{
        Provider=$Provider;Mode=$(if($VerifyOnly){'bounded verification'}else{'interactive until explicit shutdown'})
        Runner=(Join-Path $root $runner);Url=$(if($Provider -eq 'SQLite'){'http://127.0.0.1:5087/swagger'}elseif($Provider -eq 'Databases'){'http://127.0.0.1:5088/demo'}elseif($Provider -in @('Solana','SolanaMetadata')){'http://127.0.0.1:5089/'}else{'http://127.0.0.1:5088/'})
        Scope=$(if($Provider -eq 'SQLite'){'Synthetic coffee queries/save/deltas/concurrency'}elseif($Provider -eq 'Databases'){'MongoDB/SQLite/Neo4j group: existing native CQL v1 walkthrough, lifecycle and settlement checks'}elseif($Provider -eq 'CosmosDB'){'Retained local Azure Cosmos DB emulator; native rows/projection/LongCount, controlled identity and receipts'}elseif($Provider -eq 'SolanaMetadata'){'Explicit schema3 metadata profile; one-time journaled local seed, then source-read-only reuse'}elseif($Provider -eq 'Solana'){'Declared-range Solana source / SQLite index; rows, projection and cardinality; no source writes'}elseif($Provider -eq 'ActivityPub'){if($MultiRecord){'Three existing public Mastodon statuses; bounded native membership; GET only'}else{'One existing public Mastodon status; GET only'}}elseif($MultiRecord){'Three owned encrypted native records; bounded membership'}else{'One owned synthetic native point record'})
    };return
}

# The grouped runner owns the one build lease and its native fixture lifecycle.
if($Provider -in @('Databases','CosmosDB')){
    & (Join-Path $root $runner) -Serve:(!$VerifyOnly)
    return
}

# Preserve the existing SQLite runner's assembly identity, result and cleanup gates.
# It owns its build lease; never nest another lease around it.
if($Provider -eq 'SQLite'){
    & (Join-Path $root $runner) -NoBuild -Serve:(!$VerifyOnly)
    return
}

# Use the existing worktree, cached dependencies and one native environment per provider.
# Run one interactive host at a time. The lease lasts through graceful fixture teardown.
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner "cosmic-demo-$($Provider.ToLowerInvariant())" -Operation {
    switch($Provider){
        'SolanaMetadata' {
            & (Join-Path $root $runner) -IndexedMetadataDemo -ArchiveCompletedDemo:$ArchiveCompletedDemo -ServeIndexedDemo:(!$VerifyOnly)
        }
        'Solana' {
            & (Join-Path $root $runner) -IndexedHttpDemo -ArchiveCompletedDemo:$ArchiveCompletedDemo -ServeIndexedDemo:(!$VerifyOnly)
        }
        'IPFS' {
            & (Join-Path $root $runner) -CqlInteractive:(!$MultiRecord) -CqlKeySet:$MultiRecord -ArchiveCompletedDemo:$ArchiveCompletedDemo -ServePointDemo:(!$VerifyOnly)
        }
        'SOLID' {
            & (Join-Path $root $runner) -CqlInteractive:(!$MultiRecord) -CqlKeySet:$MultiRecord -ArchiveCompletedDemo:$ArchiveCompletedDemo -ServePointDemo:(!$VerifyOnly) -RequireCleanShutdown
        }
        'ActivityPub' {
            & (Join-Path $root $runner) -MultiRecord:$MultiRecord -ArchiveCompletedReport:$ArchiveCompletedDemo -ServePointDemo:(!$VerifyOnly)
        }
    }
}
