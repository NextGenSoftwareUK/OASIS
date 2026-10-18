[CmdletBinding(DefaultParameterSetName='File')]
param(
    [Parameter(Mandatory,ParameterSetName='File')] [string] $SettingsPath,
    [Parameter(Mandatory,ParameterSetName='Stdin')] [switch] $SettingsFromStdin,
    [Parameter(Mandatory)] [string] $RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$settings = if($SettingsFromStdin){[Console]::In.ReadToEnd() | ConvertFrom-Json}else{Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json}
$environmentNames = @(
    'NEAROASIS_RPCENDPOINT',
    'NEAROASIS_NETWORKID',
    'NEAROASIS_CHAINID',
    'NEAROASIS_CONTRACTADDRESS',
    'NEAROASIS_ACCOUNTID',
    'NEAROASIS_PRIVATEKEY',
    'NEAROASIS_FAILED_STATUS_TRANSACTIONID',
    'OASIS_NEAR_NODE',
    'OASIS_NEAR_SDK_BRIDGE',
    'OASIS_NEAR_STORAGE_ENCRYPTION_KEY'
)
$previous=@{}
foreach($name in $environmentNames){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}

try {
    foreach ($name in $environmentNames) {
        if($name -eq 'OASIS_NEAR_STORAGE_ENCRYPTION_KEY'){continue}
        $value = $settings.$name
        if ([string]::IsNullOrWhiteSpace($value)) {
            throw "NEAR evidence setting '$name' is missing."
        }
        [Environment]::SetEnvironmentVariable($name, [string] $value, 'Process')
    }

    # Windows PowerShell 5.1 does not load the DPAPI assembly implicitly.
    Add-Type -AssemblyName System.Security
    $cache=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OASIS/provider-evidence/near'
    New-Item -ItemType Directory -Force -Path $cache | Out-Null
    $keyPath=Join-Path $cache 'storage-key.dpapi'
    if(!(Test-Path -LiteralPath $keyPath)){
        $key=New-Object byte[] 32
        $rng=[Security.Cryptography.RandomNumberGenerator]::Create()
        try{
            $rng.GetBytes($key)
            $protected=[Security.Cryptography.ProtectedData]::Protect($key,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
            # Atomic create-only: never overwrite/rotate an existing storage key after a race.
            $file=[IO.File]::Open($keyPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try{$file.Write($protected,0,$protected.Length)}finally{$file.Dispose()}
        }finally{[Array]::Clear($key,0,$key.Length);$rng.Dispose()}
    }
    $key=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    try{
        if($key.Length -ne 32){throw 'Retained NEAR storage key is invalid; refusing replacement.'}
        $env:OASIS_NEAR_STORAGE_ENCRYPTION_KEY=[Convert]::ToBase64String($key)
    }finally{[Array]::Clear($key,0,$key.Length)}

    $tests = Join-Path $RepositoryRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests.csproj'
    $results = Join-Path $RepositoryRoot 'TestResults/NearRuntime'
    New-Item -ItemType Directory -Path $results -Force | Out-Null
    $provider=Join-Path $RepositoryRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/NextGenSoftware.OASIS.API.Providers.NEAROASIS.csproj'
    foreach($project in @($provider,$tests)){
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Retained NEAR producer/consumer build failed; no dependency restore attempted.'}
    }
    $started=[DateTime]::UtcNow
    & dotnet test $tests --no-build --no-restore --nologo --logger 'trx;LogFileName=near-provider-current.trx' --results-directory $results /m:2 /nr:false --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "NEAR provider tests failed with exit code $LASTEXITCODE."
    }
    $report=Get-Item -LiteralPath (Join-Path $results 'near-provider-current.trx')
    if($report.LastWriteTimeUtc -lt $started){throw 'NEAR native report is stale.'}
    [xml]$trx=Get-Content -LiteralPath $report.FullName
    $counts=$trx.TestRun.ResultSummary.Counters
    if([int]$counts.total -ne 17 -or [int]$counts.passed -ne 17 -or [int]$counts.notExecuted -ne 0){throw 'Expected exactly17 passing native NEAR cases and zero skips.'}
}
finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
}
