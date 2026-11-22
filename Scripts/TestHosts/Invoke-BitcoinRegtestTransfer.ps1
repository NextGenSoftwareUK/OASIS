$ErrorActionPreference = 'Stop'
./Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner CI-BitcoinRegtest -Operation {
    dotnet test 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.BitcoinOASIS.IntegrationTests' --filter 'FullyQualifiedName~RegtestBroadcastsAndConfirmsSignedPayment' /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Native Bitcoin transfer verification failed.' }
}
