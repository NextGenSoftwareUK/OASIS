#Requires -Version 7.0
[CmdletBinding()]
param([switch]$CosmicCql)
$ErrorActionPreference='Stop'
# The original installation was removed. Restore exactly one reviewed official archive,
# never a floating latest version, alternate cache, container, or historical database.
$awsRuntimeRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))) 'TestResults/AWSProviderEvidence'
$awsRuntimePath=Join-Path $awsRuntimeRoot 'runtime'
$awsRuntimeZip=Join-Path $awsRuntimeRoot 'runtime-download.zip'
$awsRuntimeDigest='667CDE09F96F4785CEDE4D107D8066011E5EECEB5DD4566EA8C74C0D378FE919'
$awsRuntimeUrl='https://d1ni2b6xgvw0s0.cloudfront.net/v2.x/dynamodb_local_latest.zip'
$awsOwner=if($CosmicCql){'oasis-cosmic-cql-dynamo'}else{'oasis-dynamo-retained'}
$awsCqlState=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))) 'LocalState/COSMIC/AwsCql'
if(Test-Path -LiteralPath $awsRuntimePath){
    if(!$CosmicCql -or @(Get-ChildItem -LiteralPath $awsRuntimePath -Force).Count -ne 0){
        throw 'Existing runtime must be reused/reviewed; no overwrite or second installation.'
    }
}
if(Test-Path -LiteralPath $awsRuntimeZip){throw 'Existing download must be reviewed/reused; no automatic redownload.'}
$awsState=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Status}}' $awsOwner).Trim()
$awsAllowedStates=if($CosmicCql){@('configured','created','exited')}else{@('exited')}
if($LASTEXITCODE -or $awsState -notin $awsAllowedStates){throw 'The retained runtime owner must be stopped before restoring its missing bind.'}
# Restore only the runtime actually mounted by this assigned checkout. A stale
# mount into another worktree must not produce an unused second installation.
$awsMounts=@(& wsl -d Ubuntu -u root -- podman inspect --format '{{json .Mounts}}' $awsOwner | ConvertFrom-Json)
if($LASTEXITCODE){throw 'Cannot verify retained Dynamo mounts; no download or directory creation.'}
$awsRuntimeTranslation=& wsl -d Ubuntu -u root -- wslpath -a -u $awsRuntimePath.Replace('\','/')
if($LASTEXITCODE -or @($awsRuntimeTranslation).Count -ne 1 -or [string]::IsNullOrWhiteSpace($awsRuntimeTranslation)){
    throw 'Cannot resolve assigned runtime path; no download or directory creation.'
}
$awsExpectedRuntime=$awsRuntimeTranslation.Trim()
$awsDataPath=if($CosmicCql){Join-Path $awsCqlState 'dynamo-data'}else{Join-Path $awsRuntimeRoot 'retained-history-data'}
$awsDataTranslation=& wsl -d Ubuntu -u root -- wslpath -a -u $awsDataPath.Replace('\','/')
if($LASTEXITCODE -or @($awsDataTranslation).Count -ne 1 -or [string]::IsNullOrWhiteSpace($awsDataTranslation)){
    throw 'Cannot resolve assigned data path; no download or directory creation.'
}
$awsExpectedData=$awsDataTranslation.Trim()
if(@($awsMounts | Where-Object {$_.Type -eq 'bind' -and $_.Destination -eq '/dynamo' -and $_.Source -eq $awsExpectedRuntime}).Count -ne 1 -or
   @($awsMounts | Where-Object {$_.Type -eq 'bind' -and $_.Destination -eq '/dynamo-data' -and $_.Source -eq $awsExpectedData}).Count -ne 1){
    throw 'Retained Dynamo mounts belong to a different checkout; review migration before restoring. No download or directory creation.'
}
if($CosmicCql){
    $awsMigration=Get-Content -LiteralPath (Join-Path $awsCqlState 'runtime-binding-migration.intent.json') -Raw | ConvertFrom-Json
    $awsCreated=Get-Content -LiteralPath (Join-Path $awsCqlState 'runtime-binding-created.acknowledged.json') -Raw | ConvertFrom-Json
    $awsOwnerId=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.Id}}' $awsOwner).Trim()
    if($LASTEXITCODE -or $awsCreated.newContainerId -ne $awsOwnerId -or $awsMigration.targetRuntime -ne $awsRuntimePath -or
       $awsMigration.dataSha256 -ne (Get-FileHash -LiteralPath (Join-Path $awsDataPath 'shared-local-instance.db')).Hash -or
       $awsMigration.seedSha256 -ne (Get-FileHash -LiteralPath (Join-Path $awsCqlState 'demo-seed.json')).Hash){
        throw 'Reviewed CQL binding/data provenance differs; no download or execution.'
    }
}
if((Get-PSDrive C).Free -lt 1GB){throw 'Insufficient bounded restoration headroom.'}
New-Item -ItemType Directory -Path $awsRuntimeRoot -Force | Out-Null
$awsRestoreLease=[IO.File]::Open((Join-Path $awsRuntimeRoot 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try{
    Invoke-WebRequest -Uri $awsRuntimeUrl -OutFile $awsRuntimeZip -TimeoutSec 120
    if((Get-Item -LiteralPath $awsRuntimeZip).Length -ne 42714930 -or (Get-FileHash -LiteralPath $awsRuntimeZip).Hash -ne $awsRuntimeDigest){throw 'Official archive changed; preserve download for review, do not extract or repin.'}
    $awsZip=[IO.Compression.ZipFile]::OpenRead($awsRuntimeZip)
    try{
        $awsMembers=@($awsZip.Entries | Where-Object { $_.FullName -eq 'DynamoDBLocal.jar' -or $_.FullName.StartsWith('DynamoDBLocal_lib/') -or $_.FullName -match '^LICENSE[^/]*$' })
        if(@($awsMembers | Where-Object {$_.FullName -eq 'DynamoDBLocal.jar'}).Count -ne 1 -or ($awsMembers | Measure-Object Length -Sum).Sum -gt 300MB){throw 'Unexpected bounded runtime contents.'}
        foreach($awsMember in $awsMembers){
            $awsTarget=[IO.Path]::GetFullPath((Join-Path $awsRuntimePath $awsMember.FullName))
            if(!$awsTarget.StartsWith(([IO.Path]::GetFullPath($awsRuntimePath)+[IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase) -or (($awsMember.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000){throw 'Unsafe archive member.'}
        }
        if(!(Test-Path -LiteralPath $awsRuntimePath)){New-Item -ItemType Directory -Path $awsRuntimePath | Out-Null}
        foreach($awsMember in $awsMembers){
            if($awsMember.FullName.EndsWith('/')){continue}
            $awsTarget=Join-Path $awsRuntimePath $awsMember.FullName
            New-Item -ItemType Directory -Path (Split-Path $awsTarget) -Force | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($awsMember,$awsTarget,$false)
        }
    }finally{$awsZip.Dispose()}
    $awsJar=Join-Path $awsRuntimePath 'DynamoDBLocal.jar'
    if((Get-FileHash -LiteralPath $awsJar).Hash -ne '67ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620'){
        throw 'Extracted JAR differs from the pinned original runtime; preserve without execution.'
    }
    $awsVersion=(& java '-Djava.library.path=' -jar $awsJar -version 2>&1)-join "`n"
    if($LASTEXITCODE){throw 'Restored runtime version inspection failed; preserve for review.'}
    $awsProvenance=@{date=[DateTime]::UtcNow.ToString('o');source=$awsRuntimeUrl;archiveSha256=$awsRuntimeDigest;jarSha256=(Get-FileHash $awsJar).Hash;version=$awsVersion;historicalDataRestored=$false;runtimeBytes=(Get-ChildItem -LiteralPath $awsRuntimePath -Recurse -File | Measure-Object Length -Sum).Sum;purpose=$(if($CosmicCql){'Pinned runtime binding repair; retained CQL data unchanged, native qualification pending'}else{'One retained official runtime; original runtime/data absent, historical outcomes unverified'})}
    $awsProvenance | ConvertTo-Json | Set-Content (Join-Path $awsRuntimePath 'runtime-provenance.json')
    $awsRuntimeDigest | Set-Content (Join-Path $awsRuntimePath 'runtime-source-sha256.txt')
    # Exact owned archive only, after successful hash-bound extraction/version inspection.
    if((Get-FileHash -LiteralPath $awsRuntimeZip).Hash -ne $awsRuntimeDigest){throw 'Archive changed before cleanup.'}
    Remove-Item -LiteralPath $awsRuntimeZip
    $awsProvenance | ConvertTo-Json
}finally{$awsRestoreLease.Dispose()}
