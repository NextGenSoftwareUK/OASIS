#Requires -Version 7.5
param([switch]$Storage)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/TonCqlCurrent'
New-Item -ItemType Directory -Force -Path $results | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-ton-native-contract -Operation {
    $names=@('NODE_PATH','OASIS_TON_NATIVE_ROOT','OASIS_TON_NODE','OASIS_TON_CODE_HASH')
    $prior=@{};foreach($name in $names){$prior[$name]=[Environment]::GetEnvironmentVariable($name)}
    try {
        # Reuse one pinned dependency tree for the generated CommonJS bindings.
        # The search path is scoped to this process and restored on exit.
        $env:NODE_PATH=Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/SdkBridge/node_modules'
        & node (Join-Path $PSScriptRoot 'compile-cosmic-ton.cjs') *> (Join-Path $results 'contract-build.log')
        if($LASTEXITCODE){throw 'Native TVM contract compilation failed.'}
        & node --test --test-concurrency=2 --test-reporter=tap (Join-Path $PSScriptRoot 'test-cosmic-ton-contract.cjs') (Join-Path $PSScriptRoot 'test-cosmic-ton-store.cjs') *> (Join-Path $results 'native-storage-tests.tap')
        if($LASTEXITCODE){throw 'Native TVM storage invariant failed; inspect retained TAP.'}
        Get-Content (Join-Path $results 'native-storage-tests.tap') -Tail 9
        if($Storage){
            $env:OASIS_TON_NATIVE_ROOT=$root
            $env:OASIS_TON_NODE=(Get-Command node.exe).Source
            $env:OASIS_TON_CODE_HASH=((& node -e 'const{Cell}=require("@ton/core");const fs=require("node:fs");console.log(Cell.fromBoc(fs.readFileSync(process.argv[1]))[0].hash().toString("hex"))' (Join-Path $results 'compiled/TonHolonStore_TonHolonStore.code.boc')) -join '').Trim()
            if($LASTEXITCODE){throw 'Native code identity could not be resolved.'}
            $provider=Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/NextGenSoftware.OASIS.API.Providers.TONOASIS.csproj'
            $project=Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests.csproj'
            foreach($target in @($provider,$project)){
                & dotnet build $target --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly *> (Join-Path $results ([IO.Path]::GetFileNameWithoutExtension($target)+'-native-build.log'))
                if($LASTEXITCODE){throw 'Native C# storage build failed.'}
            }
            & dotnet test $project --no-build --no-restore --filter FullyQualifiedName~TonNativeStorageTests --results-directory $results --logger 'trx;LogFileName=ton-native-storage.trx' -v:q *> (Join-Path $results 'csharp-native-storage.log')
            if($LASTEXITCODE){throw 'C# canonical native storage acceptance failed; preserve fixture.'}
            Get-Content (Join-Path $results 'csharp-native-storage.log') -Tail 5
        }
    } finally {foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$prior[$name])}}
}
