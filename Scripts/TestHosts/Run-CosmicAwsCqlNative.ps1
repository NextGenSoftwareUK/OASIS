#Requires -Version 7.0
param([switch]$ResumeUnwrittenSeed)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $root
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-aws-cql-native -Operation {
    $state=Join-Path $root 'LocalState/COSMIC/AwsCql'
    $results=Join-Path $root 'TestResults/AwsCqlCurrent'
    New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
    $environmentLease=[IO.File]::Open((Join-Path $state 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $priorEndpoint=$env:OASIS_AWS_CQL_ENDPOINT;$priorState=$env:OASIS_AWS_CQL_STATE
    $priorResume=$env:OASIS_AWS_CQL_RESUME_UNWRITTEN
    $startedByRun=$false
    try{
        $project='Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests.csproj'
        if(!(Test-Path (Join-Path (Split-Path $project) 'obj/project.assets.json'))){
            dotnet restore $project --source C:/Users/david/.nuget/packages /m:2 /nr:false /p:UseSharedCompilation=false -v:q *> (Join-Path $results 'native-build.log')
            if($LASTEXITCODE){throw 'Offline AWS integration assets unavailable.'}
        }
        foreach($producer in @(
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.csproj',
            'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSOASIS/NextGenSoftware.OASIS.API.Providers.AWSOASIS.csproj', $project)){
            dotnet build $producer --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false -v:q -clp:ErrorsOnly >> (Join-Path $results 'native-build.log')
            if($LASTEXITCODE){throw ('AWS producer failed: '+$producer)}
        }
        & Scripts/TestHosts/Use-CosmicDynamoEnvironment.ps1
        $startedByRun=$true
        $env:OASIS_AWS_CQL_ENDPOINT='http://127.0.0.1:18002';$env:OASIS_AWS_CQL_STATE=$state
        $env:OASIS_AWS_CQL_RESUME_UNWRITTEN=if($ResumeUnwrittenSeed){'true'}else{'false'}
        dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~NativeAwsCqlReadsThreeRecordsAndFiltersTwoWithoutScanning' --logger 'trx;LogFileName=aws-cql-native.trx' --results-directory $results *> (Join-Path $results 'aws-cql-native.log')
        if($LASTEXITCODE){throw 'Native AWS CQL failed; preserve original seed/state.'}
        [xml]$report=Get-Content (Join-Path $results 'aws-cql-native.trx')
        if($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Expected exactly one native AWS CQL case.'}
        Get-Content (Join-Path $results 'aws-cql-native.log') -Tail 5
    }
    finally{
        $env:OASIS_AWS_CQL_ENDPOINT=$priorEndpoint;$env:OASIS_AWS_CQL_STATE=$priorState
        $env:OASIS_AWS_CQL_RESUME_UNWRITTEN=$priorResume
        try{if($startedByRun){& Scripts/TestHosts/Use-CosmicDynamoEnvironment.ps1 -Stop}}
        finally{$environmentLease.Dispose()}
    }
}
