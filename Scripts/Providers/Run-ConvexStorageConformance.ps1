#Requires -Version 7.0
[CmdletBinding()]
param([switch]$VerifyPaginationOnly,[switch]$InterfaceOnly,[switch]$RerunVerifiedInterface,[switch]$InspectPublicationFailure,[switch]$CleanupPublicationFailure)
if((@($VerifyPaginationOnly,$InterfaceOnly,$RerunVerifiedInterface,$InspectPublicationFailure,$CleanupPublicationFailure)|Where-Object IsPresent).Count -gt 1){throw 'Choose exactly one explicit run/recovery mode.'}
$ErrorActionPreference='Stop'
$convexRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $convexRoot
try {
    $convexRuntimeWindows=(Join-Path $convexRoot 'Scripts/TestHosts/convex_runtime.py').Replace('\','/')
    $convexRuntimeScript=& wsl -d Ubuntu -u root --exec wslpath -u $convexRuntimeWindows
    if($LASTEXITCODE -or !$convexRuntimeScript){throw 'Cannot locate the existing runtime helper.'}
    $convexRuntimeScript=$convexRuntimeScript.Trim()
    $convexRuntime=& wsl -d Ubuntu -u root -- python3 $convexRuntimeScript status
    if($LASTEXITCODE -or !(($convexRuntime|ConvertFrom-Json).active)){throw 'Resume the retained Convex runtime; this runner never downloads or creates a substitute.'}
    & (Join-Path $convexRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'Convex native storage conformance' -Operation {
        $convexProjects=@(
            'OASIS Architecture/NextGenSoftware.OASIS.API.Core/NextGenSoftware.OASIS.API.Core.csproj',
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.ConvexOASIS/NextGenSoftware.OASIS.API.Providers.ConvexOASIS.csproj',
            'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests.csproj')
        foreach($convexProject in $convexProjects){
            & dotnet build $convexProject --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Cached Convex producer/consumer build failed.'}
        }
        $convexConsumer='Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests/bin/Debug/net10.0'
        $convexBefore=. (Join-Path $convexRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $convexRoot -ConsumerOutput $convexConsumer -VerifySourceDocuments
        $convexProducer=Join-Path $convexRoot 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.ConvexOASIS/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.ConvexOASIS.dll'
        $convexConsumed=Join-Path $convexRoot "$convexConsumer/NextGenSoftware.OASIS.API.Providers.ConvexOASIS.dll"
        $convexOwnHash=(Get-FileHash -LiteralPath $convexProducer).Hash
        if((Get-FileHash -LiteralPath $convexConsumed).Hash -ne $convexOwnHash){throw 'Consumer loaded a different Convex producer.'}
        $convexOwnedDocs=Assert-SourceDocuments -AssemblyPath $convexProducer -ProjectRoot (Join-Path $convexRoot 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.ConvexOASIS')
        $convexConsumerDocs=Assert-SourceDocuments -AssemblyPath (Join-Path $convexRoot "$convexConsumer/NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests.dll") -ProjectRoot (Join-Path $convexRoot 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests')
        $convexKeyPath=Join-Path $env:LOCALAPPDATA 'OASIS/ReusableProviderTests/Convex/storage-key.dpapi'
        if(!(Test-Path -LiteralPath $convexKeyPath)){throw 'Configure the separately persisted protected test storage key before dispatch.'}
        $convexStorage=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($convexKeyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
        try {
            if($convexStorage.Length -ne 32){throw 'Preserve invalid protected storage key.'}
            $convexAdmin=& wsl -d Ubuntu -u root -- cat /root/oasis-runtimes/convex/credentials/admin-key
            if($LASTEXITCODE -or !$convexAdmin){throw 'Protected Convex administrator identity is unavailable.'}
            $convexAdmin=$convexAdmin.Trim()
            if($RerunVerifiedInterface){
                $convexBody=@{path='oasis/records:list';args=@{prefix='';cursor=$null};format='json'}|ConvertTo-Json -Compress
                $convexEmpty=Invoke-RestMethod http://127.0.0.1:13210/api/query -Method Post -ContentType application/json -Headers @{Authorization=('Convex '+$convexAdmin)} -Body $convexBody
                if($convexEmpty.status -ne 'success' -or $convexEmpty.value.keys.Count -ne 0 -or $convexEmpty.value.complete -ne $true -or $null -ne $convexEmpty.value.cursor){throw 'Retained native table is not independently empty; preserve original records.'}
                $convexMoves=@()
                foreach($convexMode in @('async','sync')){
                    $convexPath=Join-Path $convexRoot "TestResults/ProviderConformanceCurrent/ConvexOASIS/$convexMode/report.json"
                    $convexReport=Get-Content -LiteralPath $convexPath -Raw|ConvertFrom-Json
                    $convexPrevious=Join-Path (Split-Path $convexPath) 'report-verified-previous.json'
                    if(!$convexReport.Succeeded -or !$convexReport.FixtureCleanupComplete -or $convexReport.SuiteVersion -ne 'core-storage-v2' -or $convexReport.Mode -ne 'native' -or $convexReport.Provider -ne 'ConvexOASIS' -or $convexReport.Passed.Count -ne 26 -or (Test-Path -LiteralPath ($convexPath+'.next')) -or (Test-Path -LiteralPath $convexPrevious)){throw 'Review original native report/previous checkpoint before another run.'}
                    if(!([IO.Path]::GetFullPath($convexPath)).StartsWith((Join-Path $convexRoot 'TestResults/ProviderConformanceCurrent/ConvexOASIS/'),[StringComparison]::OrdinalIgnoreCase)){throw 'Report path escaped owned evidence.'}
                    $convexMoves+=@{source=$convexPath;target=$convexPrevious}
                }
                foreach($convexMove in $convexMoves){Move-Item -LiteralPath $convexMove.source -Destination $convexMove.target}
            }
            $env:OASIS_CONVEX_STORAGE_CONFORMANCE='1'
            $env:OASIS_CONVEX_CONFORMANCE_URL='http://127.0.0.1:13210'
            $env:OASIS_CONVEX_CONFORMANCE_STORAGE_KEY=[Convert]::ToBase64String($convexStorage)
            $env:OASIS_CONVEX_CONFORMANCE_ADMIN_KEY=$convexAdmin
            if($InspectPublicationFailure -or $CleanupPublicationFailure){$env:OASIS_CONVEX_RECOVERY_ACTION=if($CleanupPublicationFailure){'cleanup'}else{'inspect'}}
            $convexFilter=if($VerifyPaginationOnly){'FullyQualifiedName~NativeListingSpansPagesAndCleansOnlyItsFixture'}elseif($InspectPublicationFailure -or $CleanupPublicationFailure){'FullyQualifiedName~InspectOrCleanupPreservedPublicationFixture'}elseif($RerunVerifiedInterface -or $InterfaceOnly){'FullyQualifiedName~ShippingProviderPassesTheSharedStorageSuite'}else{'FullyQualifiedName~ConvexStorageConformanceTests'}
            & dotnet test $convexProjects[3] --no-build --no-restore --filter $convexFilter -v:normal
            if($LASTEXITCODE){throw 'Native Convex conformance failed; preserve original records/reports.'}
            $convexAfter=& (Join-Path $convexRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $convexRoot -ConsumerOutput $convexConsumer -VerifySourceDocuments
            if((Get-FileHash -LiteralPath $convexProducer).Hash -ne $convexOwnHash -or (Get-FileHash -LiteralPath $convexConsumed).Hash -ne $convexOwnHash){throw 'Convex producer cohort changed during verification.'}
            $convexReceipt=Join-Path $convexRoot 'TestResults/HyperDrive-Providers/ConvexCurrent/identities.json'
            if($InspectPublicationFailure -or $CleanupPublicationFailure){$convexReceipt=Join-Path (Split-Path $convexReceipt) 'recovery-identities.json'}
            New-Item -ItemType Directory -Path (Split-Path $convexReceipt) -Force|Out-Null
            @{before=$convexBefore;after=$convexAfter;providerSha256=$convexOwnHash;providerSourceDocuments=$convexOwnedDocs;consumerSourceDocuments=$convexConsumerDocs;scope='Local source-matched cohort including owner working Core; not immutable release certification'}|ConvertTo-Json -Depth 12|Set-Content -LiteralPath $convexReceipt
        } finally {
            [Security.Cryptography.CryptographicOperations]::ZeroMemory($convexStorage)
            $convexAdmin=$null
            foreach($convexVariable in @('OASIS_CONVEX_STORAGE_CONFORMANCE','OASIS_CONVEX_CONFORMANCE_URL','OASIS_CONVEX_CONFORMANCE_STORAGE_KEY','OASIS_CONVEX_CONFORMANCE_ADMIN_KEY','OASIS_CONVEX_RECOVERY_ACTION')){Remove-Item -LiteralPath ('Env:'+ $convexVariable) -ErrorAction SilentlyContinue}
        }
    }
} finally {Pop-Location}
