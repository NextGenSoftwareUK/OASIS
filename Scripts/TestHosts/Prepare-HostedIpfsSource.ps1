#Requires -Version 7.0
# Reuse the retained offline node; publish a separate, retained public demo source once.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$environmentRoot = Join-Path $root 'LocalState/COSMIC/IPFSCurrent'
$sourceRoot = Join-Path $root 'LocalState/COSMIC/HostedIpfsSourceCurrent'
$results = Join-Path $root 'TestResults/HostedCqlIntegrationCurrent'
$runtime = 'C:/Users/david/.cache/oasis-runtimes/kubo-0.43.1/ipfs.exe'
$project = Join-Path $root 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests.csproj'
if (!(Test-Path -LiteralPath "$environmentRoot/repo/config") -or !(Test-Path -LiteralPath "$environmentRoot/storage-key.dpapi")) { throw 'Retained original IPFS repository/key required.' }
if ((Get-FileHash -LiteralPath $runtime).Hash -ne 'FD0D46DF76F455D938713C98C7FCA5FA117B74C934C9FC0D37730FDB33313651') { throw 'Pinned runtime differs.' }
if (Get-NetTCPConnection -LocalPort 18547 -State Listen -ErrorAction SilentlyContinue) { throw 'Native port is already owned; do not adopt or stop that process.' }
$environmentLease = [IO.File]::Open("$environmentRoot/environment.lock", [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$prior = @{}
foreach ($name in @('IPFS_PATH', 'OASIS_IPFS_STORAGE_ENCRYPTION_KEY', 'OASIS_IPFS_TEST_RPC', 'OASIS_HOSTED_IPFS_SOURCE_ROOT')) { $prior[$name] = [Environment]::GetEnvironmentVariable($name) }
$ownedState = @{ Daemon = $null }
try {
    $env:IPFS_PATH = "$environmentRoot/repo"
    $env:OASIS_IPFS_TEST_RPC = 'http://127.0.0.1:18547'
    $env:OASIS_HOSTED_IPFS_SOURCE_ROOT = $sourceRoot
    $key = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes("$environmentRoot/storage-key.dpapi"), $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
    try {
        if ($key.Length -ne 32) { throw 'Invalid retained key; never replace it.' }
        $env:OASIS_IPFS_STORAGE_ENCRYPTION_KEY = [Convert]::ToBase64String($key)
    } finally { [Security.Cryptography.CryptographicOperations]::ZeroMemory($key) }
    & "$PSScriptRoot/Invoke-WithProviderBuildLease.ps1" -Owner cosmic-hosted-ipfs-source -Operation {
        dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q *> "$results/ipfs-host-source-build.log"
        if ($LASTEXITCODE) { throw 'Build failed; no native source operation started.' }
        # The lease invokes this block in another script scope. Share one explicit
        # owner object so teardown retains the exact process handle on every exit.
        $ownedState.Daemon = Start-Process -FilePath $runtime -ArgumentList @('daemon', '--offline') -WindowStyle Hidden -PassThru -RedirectStandardOutput "$results/ipfs-host-source-daemon.log" -RedirectStandardError "$results/ipfs-host-source-daemon.err"
        $ready = $false
        $observation = 'No native version observed'
        $deadline = [DateTime]::UtcNow.AddSeconds(20)
        do {
            if ($ownedState.Daemon.HasExited) { throw 'Owned daemon exited before readiness.' }
            try {
                $version = Invoke-RestMethod -NoProxy -UserAgent 'OASIS-IPFS-NativeTests/1.0' -Method Post -Uri "$($env:OASIS_IPFS_TEST_RPC)/api/v0/version" -TimeoutSec 2
                $ready = $version.Version -eq '0.43.1'
                $observation = 'Version=' + $version.Version
            } catch { $ready = $false; $observation = $_.Exception.Message }
            if (!$ready) { Start-Sleep -Milliseconds 200 }
        } while (!$ready -and [DateTime]::UtcNow -lt $deadline)
        if (!$ready) { throw "Bounded native readiness expired: $observation. No source publication started." }
        dotnet test $project --no-build --no-restore --filter 'Category=IPFSHostedSourceNative' --logger 'trx;LogFileName=ipfs-host-source.trx' --results-directory $results /m:2 /nr:false /p:UseSharedCompilation=false *> "$results/ipfs-host-source.log"
        if ($LASTEXITCODE) { throw 'Preserve original source intent/ACK; never automatically reseed.' }
        $source = Get-Content -LiteralPath "$sourceRoot/source.acknowledged.json" -Raw | ConvertFrom-Json
        $cids = @($source.CatalogueCid) + @($source.Records | ForEach-Object { $_.Cid })
        $priorExports = if (Test-Path -LiteralPath "$sourceRoot/source-transfer.json") { @(Get-Content -LiteralPath "$sourceRoot/source-transfer.json" -Raw | ConvertFrom-Json) } else { @() }
        $exports = @()
        for ($index = 0; $index -lt $cids.Count; $index++) {
            if ($cids[$index] -notmatch '^[A-Za-z0-9]{40,128}$') { throw 'Invalid canonical source CID.' }
            $path = "$sourceRoot/source-$index.car"
            if (Test-Path -LiteralPath $path) {
                $priorExport = @($priorExports | Where-Object { $_.Path -eq $path -and $_.Cid -eq $cids[$index] })
                if ($priorExport.Count -ne 1 -or $priorExport[0].Sha256 -ne (Get-FileHash -LiteralPath $path).Hash) { throw 'Existing export requires reconciliation; do not overwrite or adopt it.' }
            } else {
                $export = Start-Process -FilePath $runtime -ArgumentList @('dag', 'export', $cids[$index]) -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput $path -RedirectStandardError "$results/ipfs-host-source-export-$index.err"
                if ($export.ExitCode) { throw 'Preserve partial export for inspection.' }
            }
            if ((Get-Item -LiteralPath $path).Length -eq 0) { throw 'Empty CAR export is not acknowledged evidence.' }
            $exports += @{ Cid=$cids[$index]; Path=$path; Bytes=(Get-Item -LiteralPath $path).Length; Sha256=(Get-FileHash -LiteralPath $path).Hash }
        }
        $exports | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$sourceRoot/source-transfer.json" -Encoding utf8
        Write-Output 'Verified retained public source: three native rows and two-row subset; four CAR exports prepared.'
    }
} finally {
    try {
        if ($ownedState.Daemon -and !$ownedState.Daemon.HasExited) {
            Invoke-RestMethod -NoProxy -UserAgent 'OASIS-IPFS-NativeTests/1.0' -Method Post -Uri 'http://127.0.0.1:18547/api/v0/shutdown' -TimeoutSec 5 | Out-Null
            if (!$ownedState.Daemon.WaitForExit(10000)) { throw 'Owned daemon remains live; preserve repository and process.' }
        }
    } finally {
        foreach ($name in $prior.Keys) { [Environment]::SetEnvironmentVariable($name, $prior[$name]) }
        $environmentLease.Dispose()
    }
}
