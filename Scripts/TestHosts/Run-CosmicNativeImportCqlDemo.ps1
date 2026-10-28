#Requires -Version 7.0
[CmdletBinding()]
param([switch]$ArchiveCompletedDemo)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/NativeImportCqlCurrent'
$journal=Join-Path $root 'LocalState/COSMIC/NativeImportCqlCurrent/fixture.pending.json'
if(Test-Path -LiteralPath $journal){throw 'Reconcile the original fixture; never replay writes.'}
foreach($name in @('source.db','target.db')){foreach($suffix in @('','-wal','-shm','-journal')){
    if(Test-Path -LiteralPath (Join-Path $work ($name+$suffix))){throw 'Preserve original owned database outcome before another run.'}
}}
$report=Join-Path $work 'report.json'
if(Test-Path -LiteralPath $report){
    if(!$ArchiveCompletedDemo){throw 'Review completed evidence or specify ArchiveCompletedDemo.'}
    $completed=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
    if(!$completed.Cleaned -or !$completed.DistinctActorIds -or !$completed.SourcePreserved -or
       $completed.Created -ne 3 -or $completed.Unchanged -ne 3 -or $completed.Updated -ne 1){throw 'Only a completed verified fixture can be archived.'}
    $archive=Join-Path $work 'Archive';New-Item -ItemType Directory -Path $archive -Force|Out-Null
    $hash=(Get-FileHash -LiteralPath $report).Hash
    $destination=Join-Path $archive ($hash+'-report.json')
    if(Test-Path -LiteralPath $destination){
        if((Get-FileHash -LiteralPath $destination).Hash -ne $hash){throw 'Existing archive differs; preserve both outcomes.'}
        Remove-Item -LiteralPath $report # Identical completed evidence is already retained by content hash.
    }else{Move-Item -LiteralPath $report -Destination $destination}
    if((Get-FileHash -LiteralPath $destination).Hash -ne $hash){throw 'Archived evidence differs.'}
}
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-native-import-demo -Operation {
    $project=Join-Path $root 'Examples/CosmicCqlDemo/CosmicCqlDemo.csproj'
    if(!(Test-Path (Join-Path $root 'Examples/CosmicCqlDemo/obj/project.assets.json'))){
        & dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /m:2 /nr:false -v:q
        if($LASTEXITCODE){throw 'Reusable cached demo metadata unavailable; no automatic download.'}
    }
    & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE){throw 'Native import demo build failed.'}
    & dotnet run --project $project --no-build --no-restore -- --native-import
    if($LASTEXITCODE){throw 'Native import acceptance incomplete; preserve original journal.'}
    if((Test-Path -LiteralPath $journal) -or !(Test-Path -LiteralPath $report)){throw 'Verified cleanup/report required.'}
    Write-Output ('Completed report SHA256 '+(Get-FileHash -LiteralPath $report).Hash)
}
