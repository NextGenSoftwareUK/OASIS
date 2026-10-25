#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$ConnectionEnvironmentVariable,[string]$ExpectedDatabase,
    [string]$ExpectedSchema,[string]$ExpectedService,[switch]$ApproveCloudReadRequests
)
$ErrorActionPreference='Stop'
if(!$ApproveCloudReadRequests){throw 'Explicit approval for potentially billable Oracle cloud connection/read requests is required.'}
foreach($value in @($ConnectionEnvironmentVariable,$ExpectedDatabase,$ExpectedSchema,$ExpectedService)){
    if([string]::IsNullOrWhiteSpace($value)){throw 'Protected connection VARIABLE NAME and approved database/schema/service identities are required.'}
}
if([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($ConnectionEnvironmentVariable))){
    throw 'Configure the protected Oracle test connection outside chat; never pass its value on the command line.'
}
if([Environment]::Version.Major -lt 10){throw 'Reuse the existing .NET10 PowerShell runtime.'}
$oraclePreflightRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$oraclePreflightOutput=Join-Path $oraclePreflightRoot 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests/bin/Debug/net10.0'
$oraclePreflightWork=Join-Path $oraclePreflightRoot 'TestResults/OracleCurrent'
if(!(Test-Path -LiteralPath $oraclePreflightWork -PathType Container)){throw 'Reuse the existing Oracle evidence directory.'}
foreach($name in @('oracle-schema-preflight.current.json','oracle-schema-preflight.current.json.next')){
    if(Test-Path -LiteralPath (Join-Path $oraclePreflightWork $name)){throw 'Preserve original/interrupted preflight proof before another run.'}
}
# Own the producer lease for source-checked frozen assemblies through the entire read-only run.
# Do not wrap this entry point in another lease. It never builds/restores/provisions.
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'Oracle hosted read-only schema preflight' -Operation {
    . (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $oraclePreflightRoot -ConsumerOutput $oraclePreflightOutput -VerifySourceDocuments
    foreach($name in @('NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.dll','NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests.dll')){
        [void](Assert-SourceDocuments (Join-Path $oraclePreflightOutput $name) $oraclePreflightRoot)
    }
    foreach($name in @('Oracle.ManagedDataAccess.dll','NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests.dll')){
        [void][Reflection.Assembly]::LoadFrom((Join-Path $oraclePreflightOutput $name))
    }
    [NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests.OracleLiveSchemaPreflight]::RunAsync(
        $ConnectionEnvironmentVariable,$ExpectedDatabase,$ExpectedSchema,$ExpectedService,$oraclePreflightWork).GetAwaiter().GetResult()
    Write-Output 'Read-only target/protocol/existing-schema preflight verified; not CRUD/wallet/certificate-policy/durability/fee/HyperDrive certification.'
}
