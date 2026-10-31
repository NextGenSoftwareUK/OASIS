#Requires -Version 7.5
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/TonCqlCurrent'
New-Item -ItemType Directory -Force -Path $results | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-ton-native-contract -Operation {
    $prior=[Environment]::GetEnvironmentVariable('NODE_PATH')
    try {
        # Reuse one pinned dependency tree for the generated CommonJS bindings.
        # The search path is scoped to this process and restored on exit.
        $env:NODE_PATH=Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/SdkBridge/node_modules'
        & node (Join-Path $PSScriptRoot 'compile-cosmic-ton.cjs') *> (Join-Path $results 'contract-build.log')
        if($LASTEXITCODE){throw 'Native TVM contract compilation failed.'}
        & node --test --test-reporter=tap (Join-Path $PSScriptRoot 'test-cosmic-ton-contract.cjs') *> (Join-Path $results 'contract-tests.tap')
        if($LASTEXITCODE){throw 'Native TVM contract invariant failed; inspect retained TAP.'}
        Get-Content (Join-Path $results 'contract-tests.tap') -Tail 9
    } finally {[Environment]::SetEnvironmentVariable('NODE_PATH',$prior)}
}
