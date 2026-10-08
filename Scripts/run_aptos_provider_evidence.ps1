[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
$runRoot = Join-Path $tempRoot ("oasis-aptos-provider-evidence-{0}" -f [Guid]::NewGuid().ToString('N'))
$cliRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OASIS/provider-evidence/aptos-cli/9.6.0'
$nodeRoot = Join-Path $runRoot 'node'
$profileRoot = Join-Path $runRoot 'profile'
$stdoutLog = Join-Path $runRoot 'localnet.stdout.log'
$stderrLog = Join-Path $runRoot 'localnet.stderr.log'
$cliZip = Join-Path $cliRoot 'aptos.zip'
$checksumFile = Join-Path $cliRoot 'SHA256SUMS'
$cli = Join-Path $cliRoot 'aptos.exe'
$localnet = $null

New-Item -ItemType Directory -Path $cliRoot, $nodeRoot, $profileRoot -Force | Out-Null

try {
    $releaseBase = 'https://github.com/aptos-labs/aptos-cli-releases/releases/download/aptos-cli-v9.6.0'
    if (-not (Test-Path -LiteralPath $cliZip)) {
        Invoke-WebRequest "$releaseBase/aptos-cli-9.6.0-x86_64-pc-windows-msvc.zip" -OutFile $cliZip
    }
    if (-not (Test-Path -LiteralPath $checksumFile)) {
        Invoke-WebRequest "$releaseBase/SHA256SUMS" -OutFile $checksumFile
    }
    $expected = (Select-String -Path $checksumFile -Pattern 'aptos-cli-9.6.0-x86_64-pc-windows-msvc.zip').Line.Split()[0].ToUpperInvariant()
    $actual = (Get-FileHash $cliZip -Algorithm SHA256).Hash
    if ($actual -ne $expected) { throw "Aptos CLI checksum mismatch: expected $expected, got $actual" }
    if (-not (Test-Path -LiteralPath $cli)) {
        Expand-Archive -Path $cliZip -DestinationPath $cliRoot -Force
    }

    $override = Join-Path $repoRoot 'Scripts/TestHosts/aptos-node-override.yaml'
    $arguments = @(
        'node', 'run-localnet', '--no-txn-stream', '--force-restart', '--assume-yes',
        '--test-dir', $nodeRoot, '--test-config-override', $override,
        '--faucet-port', '18081', '--ready-server-listen-port', '18070'
    )
    $localnet = Start-Process -FilePath $cli -ArgumentList $arguments -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog

    $ready = $false
    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        if ($localnet.HasExited) {
            throw "Aptos localnet exited during startup.`n$(Get-Content $stdoutLog -Raw)`n$(Get-Content $stderrLog -Raw)"
        }
        try {
            $ledger = Invoke-RestMethod 'http://127.0.0.1:18080/v1' -TimeoutSec 2
            $faucet = Invoke-WebRequest 'http://127.0.0.1:18081/' -TimeoutSec 2 -SkipHttpErrorCheck
            if ($ledger.chain_id -eq 4 -and $faucet.StatusCode -lt 500) { $ready = $true; break }
        } catch {}
        Start-Sleep -Milliseconds 500
    }
    if (-not $ready) { throw 'Aptos localnet did not become ready.' }

    Push-Location $profileRoot
    try {
        & $cli init --network custom --rest-url http://127.0.0.1:18080 --faucet-url http://127.0.0.1:18081 --profile oasis-local --random-seed 0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef --assume-yes | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "aptos init failed with exit code $LASTEXITCODE" }

        $config = Get-Content (Join-Path $profileRoot '.aptos/config.yaml') -Raw
        $address = ([regex]::Match($config, 'account:\s*([0-9a-fx]+)')).Groups[1].Value
        $privateKey = ([regex]::Match($config, 'private_key:\s*"?([^\r\n"]+)')).Groups[1].Value.Trim()
        if (-not $address -or -not $privateKey) { throw 'Aptos CLI profile did not contain an account and private key.' }

        $contract = Join-Path $repoRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AptosOASIS/contracts'
        & $cli move test --package-dir $contract --named-addresses "oasis=$address" | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Aptos Move tests failed with exit code $LASTEXITCODE" }
        & $cli move publish --package-dir $contract --named-addresses "oasis=$address" --profile oasis-local --assume-yes | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Aptos contract publish failed with exit code $LASTEXITCODE" }
        & $cli move run --function-id "$address::oasis::initialize" --profile oasis-local --assume-yes | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Aptos storage initialization failed with exit code $LASTEXITCODE" }

        $env:OASIS_APTOS_RPC_ENDPOINT = 'http://127.0.0.1:18080/v1'
        $env:OASIS_APTOS_CONTRACT_ADDRESS = $address
        $env:OASIS_APTOS_ACCOUNT_ADDRESS = $address
        $env:OASIS_APTOS_PRIVATE_KEY = $privateKey
        $tests = Join-Path $repoRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.AptosOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AptosOASIS.IntegrationTests.csproj'
        & dotnet test $tests --nologo --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { throw "Aptos provider tests failed with exit code $LASTEXITCODE" }
    } finally {
        Pop-Location
        Remove-Item Env:OASIS_APTOS_RPC_ENDPOINT, Env:OASIS_APTOS_CONTRACT_ADDRESS, Env:OASIS_APTOS_ACCOUNT_ADDRESS, Env:OASIS_APTOS_PRIVATE_KEY -ErrorAction SilentlyContinue
    }
} finally {
    if ($localnet -and -not $localnet.HasExited) {
        Stop-Process -Id $localnet.Id -Force -ErrorAction SilentlyContinue
        $localnet.WaitForExit(5000) | Out-Null
    }
    $resolvedRunRoot = [IO.Path]::GetFullPath($runRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ($resolvedRunRoot.StartsWith($tempRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedRunRoot)) {
        Remove-Item -LiteralPath $resolvedRunRoot -Recurse -Force
    }
}
