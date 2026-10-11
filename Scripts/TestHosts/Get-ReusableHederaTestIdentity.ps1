param()
$ErrorActionPreference = 'Stop'
# One current-user DPAPI-protected identity, outside source control. Never print the key.
$taskRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$bridgeRoot = Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge'
$fixtureRoot = Join-Path $env:LOCALAPPDATA 'OASIS/ReusableProviderTests/Hedera'
$fixturePath = Join-Path $fixtureRoot 'testnet-identity.dpapi'
Add-Type -AssemblyName System.Security
if (Test-Path -LiteralPath $fixturePath) {
    $encrypted = [IO.File]::ReadAllBytes($fixturePath)
    $plain = [Security.Cryptography.ProtectedData]::Unprotect($encrypted, $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
    try { $identity = [Text.Encoding]::UTF8.GetString($plain) | ConvertFrom-Json }
    finally { [Array]::Clear($plain, 0, $plain.Length) }
} else {
    if (-not (Test-Path -LiteralPath (Join-Path $bridgeRoot 'node_modules/@hiero-ledger/sdk'))) {
        throw 'Reuse requires the existing pinned Hedera SDK. No installation was attempted.'
    }
    Push-Location $bridgeRoot
    try {
        $identityJson = & node --input-type=module -e 'import {PrivateKey} from "@hiero-ledger/sdk"; const key=PrivateKey.generateECDSA(); console.log(JSON.stringify({network:"testnet",privateKey:key.toStringDer(),evmAddress:"0x"+key.publicKey.toEvmAddress()}));'
        if ($LASTEXITCODE -ne 0) { throw 'Existing SDK could not generate the test identity.' }
        $identity = $identityJson | ConvertFrom-Json
        $plain = [Text.Encoding]::UTF8.GetBytes($identityJson)
        try { $encrypted = [Security.Cryptography.ProtectedData]::Protect($plain, $null, [Security.Cryptography.DataProtectionScope]::CurrentUser) }
        finally { [Array]::Clear($plain, 0, $plain.Length); $identityJson = $null }
        [IO.Directory]::CreateDirectory($fixtureRoot) | Out-Null
        # CreateNew prevents accidentally overwriting a concurrently created reusable key.
        $stream = [IO.File]::Open($fixturePath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $stream.Write($encrypted, 0, $encrypted.Length) }
        finally { $stream.Dispose() }
    } finally { Pop-Location }
}
if ($identity.network -ne 'testnet' -or $identity.evmAddress -notmatch '^0x[0-9a-fA-F]{40}$') {
    throw 'Reusable test identity has an invalid network or public address.'
}
# This is a key alias only. Funding/account creation must be confirmed on the ledger.
[pscustomobject]@{ Network = 'testnet'; EvmAddress = $identity.evmAddress; CredentialFile = $fixturePath; Funded = 'Not verified' }
$identity = $null
