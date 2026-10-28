#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('SQLite','IPFS','SOLID','ActivityPub','Solana','SolanaMetadata')][string]$Provider='SQLite',
    [switch]$VerifyOnly,
    [switch]$ArchiveCompletedDemo,
    [switch]$ShowPlan
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runner=switch($Provider){
    'SQLite' {'Scripts/TestHosts/Run-CosmicCqlDemo.ps1'}
    'Solana' {'Scripts/TestHosts/Run-ReusableSolanaTests.ps1'}
    'SolanaMetadata' {'Scripts/TestHosts/Run-ReusableSolanaTests.ps1'}
    'IPFS' {'Scripts/TestHosts/Run-ReusableIpfsTests.ps1'}
    'SOLID' {'Scripts/TestHosts/Run-ReusableSolidReadiness.ps1'}
    'ActivityPub' {'Scripts/TestHosts/Run-ReusableActivityPubPointDemo.ps1'}
}
if($Provider -eq 'SQLite' -and $ArchiveCompletedDemo){throw 'SQLite owns its disposable fixture; protocol archive options do not apply.'}
if($ShowPlan){
    [pscustomobject]@{
        Provider=$Provider;Mode=$(if($VerifyOnly){'bounded verification'}else{'interactive until explicit shutdown'})
        Runner=(Join-Path $root $runner);Url=$(if($Provider -eq 'SQLite'){'http://127.0.0.1:5087/swagger'}elseif($Provider -in @('Solana','SolanaMetadata')){'http://127.0.0.1:5089/'}else{'http://127.0.0.1:5088/'})
        Scope=$(if($Provider -eq 'SQLite'){'Synthetic coffee queries/save/deltas/concurrency'}elseif($Provider -eq 'SolanaMetadata'){'Explicit schema3 metadata profile; one-time journaled local seed, then source-read-only reuse'}elseif($Provider -eq 'Solana'){'Declared-range Solana source / SQLite index; rows, projection and cardinality; no source writes'}elseif($Provider -eq 'ActivityPub'){'One existing public Mastodon status; GET only'}else{'One owned synthetic native point record'})
    };return
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
