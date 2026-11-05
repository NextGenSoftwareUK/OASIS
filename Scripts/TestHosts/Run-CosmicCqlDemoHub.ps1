#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('SQLite','IPFS','SOLID','ActivityPub','Solana','SolanaMetadata','Databases','CosmosDB','SurrealDB','CouchDB','SQLServer','DuckDB','LocalFile')][string]$Provider='SQLite',
    [switch]$VerifyOnly,
    [switch]$Federated,
    [switch]$RecoveryMongo,
    [switch]$ArchiveCompletedDemo,
    [switch]$ShowPlan
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ($RecoveryMongo -and $Provider -notin @('SQLServer','DuckDB','LocalFile')) {
    throw 'RecoveryMongo is available for the SQL Server, DuckDB and LocalFile presets.'
}
if ($Provider -in @('SQLServer','DuckDB','LocalFile')) {
    if (!$Federated -or $ArchiveCompletedDemo) {
        throw "$Provider requires -Federated and preserves its demo source."
    }
    $isSqlServer = $Provider -eq 'SQLServer'
    $preset = switch ($Provider) {
        'SQLServer' { @{ Runner = 'Run-CosmicSqlServerCqlNative.ps1'; Port = 5115 } }
        'DuckDB' { @{ Runner = 'Run-CosmicDuckDbCqlNative.ps1'; Port = 5116 } }
        'LocalFile' { @{ Runner = 'Run-CosmicLocalFileCqlNative.ps1'; Port = 5110 } }
    }
    $runner = Join-Path $PSScriptRoot $preset.Runner
    if ($ShowPlan) {
        [pscustomobject]@{
            Provider = $Provider
            Mode = $(if ($VerifyOnly) {'bounded verification'} else {'interactive until explicit shutdown'})
            Runner = $runner
            Url = "http://127.0.0.1:$($preset.Port)/"
            Scope = "$Provider and Mongo CQL federation over retained demo fixtures"
            RecoveryMongo = $RecoveryMongo.IsPresent
        }
        return
    }
    $runnerOptions = @{Federated=$true; RecoveryMongo=$RecoveryMongo; Serve=!$VerifyOnly}
    if ($isSqlServer) { $runnerOptions.Content = $true }
    & $runner @runnerOptions
    return
}

if ($Provider -in @('SurrealDB','CouchDB')) {
    if (!$Federated -or $ArchiveCompletedDemo) {
        throw "$Provider requires the Mongo-paired preset and preserves its original source. Use -Federated without -ArchiveCompletedDemo."
    }
    $runnerName = if ($Provider -eq 'SurrealDB') {'Run-CosmicSurrealDbLinuxDemo.ps1'} else {'Run-CosmicCouchDbLinuxDemo.ps1'}
    $runner = Join-Path $PSScriptRoot $runnerName
    if ($ShowPlan) {
        [pscustomobject]@{
            Provider = $Provider
            Mode = $(if ($VerifyOnly) {'bounded verification'} else {'interactive until explicit shutdown'})
            Runner = $runner
            Url = $(if ($Provider -eq 'SurrealDB') {'http://127.0.0.1:5133/'} else {'http://127.0.0.1:5132/'})
            Scope = "Mongo and retained authenticated native $Provider, five rows and four in the subset"
        }
        return
    }
    & $runner -Federated -Serve:(!$VerifyOnly)
    return
}
if ($Federated) { throw 'This launcher has no explicit Mongo-paired route for this provider.' }

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
if($ShowPlan){
    [pscustomobject]@{
        Provider=$Provider;Mode=$(if($VerifyOnly){'bounded verification'}else{'interactive until explicit shutdown'})
        Runner=(Join-Path $root $runner);Url=$(if($Provider -eq 'SQLite'){'http://127.0.0.1:5087/swagger'}elseif($Provider -eq 'Databases'){'http://127.0.0.1:5088/demo'}elseif($Provider -in @('Solana','SolanaMetadata')){'http://127.0.0.1:5089/'}else{'http://127.0.0.1:5088/'})
        Scope=$(if($Provider -eq 'SQLite'){'Synthetic coffee queries/save/deltas/concurrency'}elseif($Provider -eq 'Databases'){'MongoDB/SQLite/Neo4j group: existing native CQL v1 walkthrough, lifecycle and settlement checks'}elseif($Provider -eq 'CosmosDB'){'Retained local Azure Cosmos DB emulator; native rows/projection/LongCount, controlled identity and receipts'}elseif($Provider -eq 'SolanaMetadata'){'Explicit schema3 metadata profile; one-time journaled local seed, then source-read-only reuse'}elseif($Provider -eq 'Solana'){'Declared-range Solana source / SQLite index; rows, projection and cardinality; no source writes'}elseif($Provider -eq 'ActivityPub'){'One existing public Mastodon status; GET only'}else{'One owned synthetic native point record'})
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
            & (Join-Path $root $runner) -CqlInteractive -ArchiveCompletedDemo:$ArchiveCompletedDemo -ServePointDemo:(!$VerifyOnly)
        }
        'SOLID' {
            & (Join-Path $root $runner) -CqlInteractive -ArchiveCompletedDemo:$ArchiveCompletedDemo -ServePointDemo:(!$VerifyOnly) -RequireCleanShutdown
        }
        'ActivityPub' {
            & (Join-Path $root $runner) -ArchiveCompletedReport:$ArchiveCompletedDemo -ServePointDemo:(!$VerifyOnly)
        }
    }
}
