#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$upstashRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$upstashResource=Join-Path $env:LOCALAPPDATA 'OASIS/ReusableProviderTests/Upstash'
$upstashReceipt=Get-Content -LiteralPath (Join-Path $upstashResource 'resource.json') -Raw|ConvertFrom-Json
if([DateTimeOffset]::UtcNow -ge [DateTimeOffset]$upstashReceipt.ExpiresByUtc){throw 'Retained scratch resource expired; this runner never provisions a replacement.'}
Push-Location $upstashRoot
try {
    & (Join-Path $upstashRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'Upstash native core storage' -Operation {
        $projects=@(
            'OASIS Architecture/NextGenSoftware.OASIS.API.Core/NextGenSoftware.OASIS.API.Core.csproj',
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.UpstashOASIS/NextGenSoftware.OASIS.API.Providers.UpstashOASIS.csproj',
            'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.UpstashOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.UpstashOASIS.UnitTests.csproj')
        foreach($project in $projects){
            dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Cached Upstash build failed.'}
        }
        $consumer=Join-Path $upstashRoot 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.UpstashOASIS.UnitTests/bin/Debug/net10.0'
        $before=. (Join-Path $upstashRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $upstashRoot -ConsumerOutput $consumer -VerifySourceDocuments
        $producer=Join-Path $upstashRoot 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.UpstashOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.UpstashOASIS.dll'
        $consumed=Join-Path $consumer 'NextGenSoftware.OASIS.API.Providers.UpstashOASIS.dll'
        $producerHash=(Get-FileHash -LiteralPath $producer).Hash
        if((Get-FileHash -LiteralPath $consumed).Hash -ne $producerHash){throw 'Upstash producer/consumer mismatch.'}
        $providerDocuments=Assert-SourceDocuments -AssemblyPath $producer -ProjectRoot (Join-Path $upstashRoot 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.UpstashOASIS')
        $testDocuments=Assert-SourceDocuments -AssemblyPath (Join-Path $consumer 'NextGenSoftware.OASIS.API.Providers.UpstashOASIS.UnitTests.dll') -ProjectRoot (Join-Path $upstashRoot 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.UpstashOASIS.UnitTests')
        $names=@('OASIS_UPSTASH_STORAGE_CONFORMANCE','OASIS_UPSTASH_CONFORMANCE_URL','OASIS_UPSTASH_CONFORMANCE_TOKEN','OASIS_UPSTASH_CONFORMANCE_STORAGE_KEY')
        $prior=@{};foreach($name in $names){$prior[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
        $credentialBytes=$null;$storageKey=$null
        try {
            $credentialBytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $upstashResource 'credentials.dpapi')),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
            $configuration=[Text.Encoding]::UTF8.GetString($credentialBytes)|ConvertFrom-Json
            $storageKey=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $upstashResource 'storage-key.dpapi')),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
            if($storageKey.Length -ne 32){throw 'Persisted Upstash storage key must be 32 bytes.'}
            $env:OASIS_UPSTASH_STORAGE_CONFORMANCE='1'
            $env:OASIS_UPSTASH_CONFORMANCE_URL=$configuration.Url
            $env:OASIS_UPSTASH_CONFORMANCE_TOKEN=$configuration.Token
            $env:OASIS_UPSTASH_CONFORMANCE_STORAGE_KEY=[Convert]::ToBase64String($storageKey)
            dotnet test $projects[3] --no-build --no-restore --filter 'FullyQualifiedName~ShippingProviderPassesTheSharedStorageSuite' -v:normal
            if($LASTEXITCODE){throw 'Upstash native acceptance failed; preserve records/checkpoints, never replay automatically.'}
            $after=& (Join-Path $upstashRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $upstashRoot -ConsumerOutput $consumer -VerifySourceDocuments
            if((Get-FileHash -LiteralPath $producer).Hash -ne $producerHash -or (Get-FileHash -LiteralPath $consumed).Hash -ne $producerHash){throw 'Upstash cohort changed during native verification.'}
            $evidence=Join-Path $upstashRoot 'TestResults/HyperDrive-Providers/UpstashCurrent'
            New-Item -ItemType Directory -Path $evidence -Force|Out-Null
            @{Before=$before;After=$after;ProviderSha256=$producerHash;ProviderDocuments=$providerDocuments;ConsumerDocuments=$testDocuments;ObservedUtc=[DateTime]::UtcNow.ToString('O');SourceScope='Local source-matched working cohort; not an immutable release certification'}|ConvertTo-Json -Depth 12|Set-Content (Join-Path $evidence 'identities.json')
        }
        finally {
            foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$prior[$name],'Process')}
            if($credentialBytes){[Security.Cryptography.CryptographicOperations]::ZeroMemory($credentialBytes)}
            if($storageKey){[Security.Cryptography.CryptographicOperations]::ZeroMemory($storageKey)}
        }
    }
}
finally {Pop-Location}
