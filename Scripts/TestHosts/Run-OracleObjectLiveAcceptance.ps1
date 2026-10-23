#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$ConfigFilePath,[string]$ProfileName,[string]$RegionId,
    [string]$NamespaceName,[string]$BucketName,[switch]$ApproveCloudRequests
)
$ErrorActionPreference='Stop'
# No credential values on the command line. Existing profile refers to protected key files.
if(!$ApproveCloudRequests){throw 'Explicit approval for potentially billable test-bucket requests is required.'}
foreach($value in @($ConfigFilePath,$ProfileName,$RegionId,$NamespaceName,$BucketName)){
    if([string]::IsNullOrWhiteSpace($value)){throw 'Existing profile path/name, region, namespace and isolated test bucket are required.'}
}
if(!(Test-Path -LiteralPath $ConfigFilePath -PathType Leaf)){throw 'Existing protected OCI configuration file required; do not generate credentials.'}
if([Environment]::Version.Major -lt 10){throw 'Reuse the existing .NET 10 PowerShell runtime.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$output=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests/bin/Debug/net10.0'
$work=Join-Path $root 'TestResults/OracleObjectCurrent'
if(!(Test-Path -LiteralPath $work -PathType Container)){throw 'Existing OracleObjectCurrent work directory required.'}
foreach($name in @('oci-live-transport.pending.json','oci-live-transport.current.json','oci-live-transport.pending.json.next','oci-live-transport.current.json.next')){
    if(Test-Path -LiteralPath (Join-Path $work $name)){throw 'Preserve and review existing acceptance evidence before another run.'}
}
# Freeze all dependent consumer assemblies through the entire bounded live run.
# This entry point owns the producer lease; do not nest another lease around it.
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'Oracle live transport acceptance' -Operation {
    . (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput $output -VerifySourceDocuments
    foreach($name in @('NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests.dll','NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.dll')){
        [void](Assert-SourceDocuments (Join-Path $output $name) $root)
    }
    foreach($name in @('OCI.DotNetSDK.Common.dll','OCI.DotNetSDK.Objectstorage.dll','NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests.dll')){
        [void][Reflection.Assembly]::LoadFrom((Join-Path $output $name))
    }
    $auth=[Oci.Common.Auth.ConfigFileAuthenticationDetailsProvider]::new($ConfigFilePath,$ProfileName)
    $configuration=[Oci.Common.ClientConfiguration]::new();$configuration.TimeoutMillis=10000
    $client=[Oci.ObjectstorageService.ObjectStorageClient]::new($auth,$configuration)
    try{
        $client.SetRegion($RegionId)
        [NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests.OracleLiveTransportAcceptance]::RunAsync(
            $client,$NamespaceName,$BucketName,'oasis-acceptance/',$work).GetAwaiter().GetResult()
        Write-Output 'Native transport acceptance verified; no full-provider/cloud-cost/HyperDrive certification implied.'
    }finally{$client.Dispose()}
}
