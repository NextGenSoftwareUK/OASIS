[CmdletBinding()]
param(
    [ValidateRange(5,3600)][int]$DurationSeconds=65,
    [switch]$ReuseBuiltDependencies
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root 'Docs/WIP/Mongo-Test-Traffic'
New-Item -ItemType Directory -Path $evidence -Force|Out-Null
$priorSeconds=$env:OASIS_LOCAL_WORKER_SOAK_SECONDS
$priorReport=$env:OASIS_LOCAL_WORKER_SOAK_REPORT
try {
    # The test hardcodes loopback MongoDB and never reads Railway or application credentials.
    $env:OASIS_LOCAL_WORKER_SOAK_SECONDS=[string]$DurationSeconds
    $env:OASIS_LOCAL_WORKER_SOAK_REPORT=Join-Path $evidence 'local-worker-soak.json'
    $arguments=@('test',(Join-Path $root 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'),
        '--no-restore','--filter','FullyQualifiedName~HyperDriveLocalMongoSoakTests',
        '/m:2','/nr:false','/p:UseSharedCompilation=false','-v:q','-clp:ErrorsOnly')
    if($ReuseBuiltDependencies){$arguments+='/p:BuildProjectReferences=false'}
    & dotnet @arguments *> (Join-Path $evidence 'local-worker-soak-tests.log')
    $code=$LASTEXITCODE
    Get-Content (Join-Path $evidence 'local-worker-soak-tests.log') -Tail 12
    if($code -ne 0){throw "Local worker soak failed (exit $code); inspect the current test log. A previous JSON report does not establish this run passed."}
    Get-Content $env:OASIS_LOCAL_WORKER_SOAK_REPORT -Raw
} finally {
    $env:OASIS_LOCAL_WORKER_SOAK_SECONDS=$priorSeconds
    $env:OASIS_LOCAL_WORKER_SOAK_REPORT=$priorReport
}
