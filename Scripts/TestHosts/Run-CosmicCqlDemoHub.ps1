#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('SQLite','IPFS','SOLID','ActivityPub','Solana','SolanaMetadata','Databases','CosmosDB')][string]$Provider='SQLite',
    [switch]$VerifyOnly,
    [switch]$MultiRecord,
    [switch]$ArchiveCompletedDemo,
    [switch]$ShowPlan
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
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
