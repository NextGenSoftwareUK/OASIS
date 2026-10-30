# Read-only inspection of the existing dev WEB4 authority. Never links or changes a Railway project.
param([switch]$InspectProviderSchema)
$ErrorActionPreference = 'Stop'
trap { Write-Error ('Read-only accounting inspection failed: ' + $_.Exception.GetType().Name + '. Credentials withheld; no initialization attempted.') -ErrorAction Continue; exit 1 }
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testProject = Join-Path $taskRoot 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
$testBinary = Join-Path (Split-Path $testProject) 'bin/Debug/net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.dll'
$probeSource = Join-Path (Split-Path $testProject) 'Subscription/SandboxHostedAccountingReadinessProbe.cs'
if (!(Test-Path -LiteralPath $testBinary) -or (Get-Item -LiteralPath $testBinary).LastWriteTimeUtc -lt (Get-Item -LiteralPath $probeSource).LastWriteTimeUtc) {
    throw 'Build the existing unit project under the provider lease before running this no-build probe.'
}
$configText = railway variable list -p 712f50f6-3b7b-46d4-b229-44b49922f16b -s 'WEB4 OASIS API' -e dev --json
if ($LASTEXITCODE -ne 0) { throw 'Dev configuration read failed.' }
$configurationValues = $configText | ConvertFrom-Json
$authorityUri = [string]$configurationValues.SUBSCRIPTION_MONGODB_CONNECTION_STRING
if ([string]::IsNullOrWhiteSpace($authorityUri)) {
    $dnaValues = $configurationValues.OASIS_DNA_JSON | ConvertFrom-Json
    $authorityUri = [string]$dnaValues.OASIS.StorageProviders.MongoDBOASIS.ConnectionString
}
if ([string]::IsNullOrWhiteSpace($authorityUri)) { throw 'Configured accounting authority could not be resolved.' }
$authorityDatabase = [string]$configurationValues.SUBSCRIPTION_MONGODB_DATABASE
if ([string]::IsNullOrWhiteSpace($authorityDatabase)) { $authorityDatabase = 'oasis_subscription_accounting' }
foreach ($probeName in @('COSMIC_HOSTED_ACCOUNTING_PROBE','COSMIC_HOSTED_ACCOUNTING_URI','COSMIC_HOSTED_ACCOUNTING_DATABASE','COSMIC_HOSTED_PROVIDER_URI','COSMIC_HOSTED_PROVIDER_DATABASE')) {
    if (Test-Path "Env:$probeName") { throw 'Existing probe environment must not be overwritten.' }
}
$results = Join-Path $taskRoot 'TestResults/Web4SandboxCqlCurrent'
New-Item -ItemType Directory -Force $results | Out-Null
try {
    $env:COSMIC_HOSTED_ACCOUNTING_PROBE = 'oasis-dev-web4-read-only'
    $env:COSMIC_HOSTED_ACCOUNTING_URI = $authorityUri
    $env:COSMIC_HOSTED_ACCOUNTING_DATABASE = $authorityDatabase
    if ($InspectProviderSchema) {
        $dnaValues = $configurationValues.OASIS_DNA_JSON | ConvertFrom-Json
        $providerUri = [string]$dnaValues.OASIS.StorageProviders.MongoDBOASIS.ConnectionString
        $providerDatabase = [string]$dnaValues.OASIS.StorageProviders.MongoDBOASIS.DBName
        if ([string]::IsNullOrWhiteSpace($providerUri) -or [string]::IsNullOrWhiteSpace($providerDatabase)) { throw 'Explicit provider source is required.' }
        $env:COSMIC_HOSTED_PROVIDER_URI = $providerUri
        $env:COSMIC_HOSTED_PROVIDER_DATABASE = $providerDatabase
    }
    dotnet test $testProject --no-build --no-restore --filter FullyQualifiedName~SandboxHostedAccountingReadinessProbe --results-directory $results --logger 'trx;LogFileName=hosted-readiness.trx' -v:q *> (Join-Path $results 'hosted-readiness.log')
    $probeExit = $LASTEXITCODE
}
finally {
    Remove-Item Env:COSMIC_HOSTED_ACCOUNTING_PROBE,Env:COSMIC_HOSTED_ACCOUNTING_URI,Env:COSMIC_HOSTED_ACCOUNTING_DATABASE -ErrorAction SilentlyContinue
    Remove-Item Env:COSMIC_HOSTED_PROVIDER_URI,Env:COSMIC_HOSTED_PROVIDER_DATABASE -ErrorAction SilentlyContinue
    $authorityUri = $null; $configurationValues = $null; $configText = $null; $dnaValues = $null; $providerUri = $null
}
if ($probeExit -ne 0) { throw 'Probe failed; inspect the sanitized fixed log.' }
Get-Content -LiteralPath (Join-Path $results 'hosted-accounting-readiness.json')
