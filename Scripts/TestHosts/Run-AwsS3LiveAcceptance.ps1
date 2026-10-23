#Requires -Version 7.0
[CmdletBinding()]
param([string]$Region, [string]$BucketName, [string]$ProfileName, [switch]$ApproveCloudRequests)
$ErrorActionPreference = 'Stop'
if (!$ApproveCloudRequests) { throw 'Explicit approval for potentially billable isolated test-bucket requests is required.' }
if ([string]::IsNullOrWhiteSpace($Region) -or [string]::IsNullOrWhiteSpace($BucketName)) { throw 'An existing test bucket and explicit AWS region are required.' }
if ([Environment]::Version.Major -lt 10) { throw 'Reuse the existing .NET 10 PowerShell runtime.' }
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$output = Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests/bin/Debug/net10.0'
$work = Join-Path $root 'TestResults/S3Current'
if (!(Test-Path -LiteralPath $work -PathType Container)) { throw 'Existing S3Current evidence directory required.' }
foreach ($name in @('s3-live-transport.pending.json','s3-live-transport.current.json','s3-live-transport.pending.json.next','s3-live-transport.current.json.next')) {
    if (Test-Path -LiteralPath (Join-Path $work $name)) { throw 'Preserve and review existing acceptance evidence before another run.' }
}
# Owns the producer lease for the entire bounded native run; do not nest a lease.
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'AWS S3 live transport acceptance' -Operation {
    . (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput $output -VerifySourceDocuments
    foreach ($name in @('NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests.dll','NextGenSoftware.OASIS.Providers.Shared.S3.dll')) {
        [void](Assert-SourceDocuments (Join-Path $output $name) $root)
    }
    foreach ($name in @('AWSSDK.Core.dll','AWSSDK.S3.dll','NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests.dll')) {
        [void][Reflection.Assembly]::LoadFrom((Join-Path $output $name))
    }
    $config = [Amazon.S3.AmazonS3Config]::new()
    $config.RegionEndpoint = [Amazon.RegionEndpoint]::GetBySystemName($Region)
    $config.MaxErrorRetry = 0
    $config.Timeout = [TimeSpan]::FromSeconds(10)
    if ([string]::IsNullOrWhiteSpace($ProfileName)) {
        $client = [Amazon.S3.AmazonS3Client]::new($config)
    } else {
        [Amazon.Runtime.AWSCredentials]$credentials = $null
        $profiles = [Amazon.Runtime.CredentialManagement.CredentialProfileStoreChain]::new()
        if (!$profiles.TryGetAWSCredentials($ProfileName, [ref]$credentials)) { throw 'Protected AWS profile not available; do not generate or print credentials.' }
        $client = [Amazon.S3.AmazonS3Client]::new($credentials, $config)
    }
    try {
        [NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests.AwsS3LiveTransportAcceptance]::RunAsync(
            $client, $Region, $BucketName, 'oasis-acceptance/', $work).GetAwaiter().GetResult()
        Write-Output 'Native S3 transport verified; full-provider, IAM, billing and HyperDrive acceptance remain separate.'
    } finally { $client.Dispose() }
}
