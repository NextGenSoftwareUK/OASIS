[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sdkRoot = Join-Path $repoRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.ArweaveOASIS/SdkBridge'
$bridge = Join-Path $sdkRoot 'bridge.mjs'
$runtimeRoot = Join-Path $repoRoot 'Scripts/TestHosts/arweave-local'
$runtimeScript = Join-Path $runtimeRoot 'node_modules/arlocal/bin/index.js'
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
$runRoot = Join-Path $tempBase ("oasis-arweave-evidence-{0}" -f [Guid]::NewGuid().ToString('N'))
$stdoutLog = Join-Path $runRoot 'arlocal.stdout.log'
$stderrLog = Join-Path $runRoot 'arlocal.stderr.log'
$gateway = 'http://127.0.0.1:1985'
$runtime = $null

New-Item -ItemType Directory -Path $runRoot -Force | Out-Null

try {
    & npm ci --prefix $sdkRoot --ignore-scripts --no-audit --no-fund | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "arweave-js npm ci failed with exit code $LASTEXITCODE" }
    & npm ci --prefix $runtimeRoot --no-audit --no-fund | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "ArLocal npm ci failed with exit code $LASTEXITCODE" }

    $runtime = Start-Process -FilePath 'node' -ArgumentList $runtimeScript, '1985' -WorkingDirectory $runRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($runtime.HasExited) { throw "ArLocal exited during startup.`n$(Get-Content $stdoutLog -Raw)`n$(Get-Content $stderrLog -Raw)" }
        try {
            $info = Invoke-RestMethod "$gateway/info" -TimeoutSec 2
            if ($info.network) { $ready = $true; break }
        } catch {}
        Start-Sleep -Milliseconds 250
    }
    if (-not $ready) { throw 'ArLocal did not become ready.' }

    $walletRequest = @{ operation = 'generateWallet'; gateway = $gateway } | ConvertTo-Json -Compress
    $generated = ($walletRequest | & node $bridge | ConvertFrom-Json)
    if ($LASTEXITCODE -ne 0 -or -not $generated.ok) { throw 'Official arweave-js wallet generation failed.' }
    Invoke-WebRequest "$gateway/mint/$($generated.address)/1000000000000000" -TimeoutSec 10 | Out-Null

    $env:OASIS_ARWEAVE_WALLET_JSON = $generated.wallet | ConvertTo-Json -Compress
    $env:OASIS_ARWEAVE_GATEWAY = $gateway
    $env:OASIS_ARWEAVE_SDK_BRIDGE = $bridge
    $env:OASIS_ARWEAVE_NODE = 'node'
    $tests = Join-Path $repoRoot 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.IntegrationTests.csproj'
    & dotnet test $tests --nologo --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw "Arweave provider tests failed with exit code $LASTEXITCODE" }
}
finally {
    Remove-Item Env:OASIS_ARWEAVE_WALLET_JSON, Env:OASIS_ARWEAVE_GATEWAY, Env:OASIS_ARWEAVE_SDK_BRIDGE, Env:OASIS_ARWEAVE_NODE -ErrorAction SilentlyContinue
    if ($runtime -and -not $runtime.HasExited) {
        Stop-Process -Id $runtime.Id -Force -ErrorAction SilentlyContinue
        $runtime.WaitForExit(5000) | Out-Null
    }
    $resolvedRunRoot = [IO.Path]::GetFullPath($runRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ($resolvedRunRoot.StartsWith($tempBase + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedRunRoot)) {
        Remove-Item -LiteralPath $resolvedRunRoot -Recurse -Force
    }
}
