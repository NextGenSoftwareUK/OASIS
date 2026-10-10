[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,
    [string]$PrivateKeyEnvironmentVariable = 'OASIS_OFFLINE_GRANT_SIGNING_PRIVATE_KEY',
    [ValidateRange(1, 525600)]
    [int]$MaximumLifetimeMinutes = 10080,
    [string[]]$AllowedScopes = @(
        'hyperdrive.sync',
        'avatar.read',
        'inventory.read',
        'quest.read',
        'quest.progress',
        'geonft.collect'
    )
)

$ErrorActionPreference = 'Stop'
$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($outputPath) | Out-Null

if ([string]::IsNullOrWhiteSpace($PrivateKeyEnvironmentVariable) -or
    $PrivateKeyEnvironmentVariable -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
    throw 'PrivateKeyEnvironmentVariable must be a valid environment-variable name.'
}

$normalizedScopes = @($AllowedScopes | ForEach-Object { $_.Trim() } |
    Where-Object { $_ } | Sort-Object -Unique)
if ($normalizedScopes.Count -eq 0 -or $normalizedScopes -notcontains 'hyperdrive.sync') {
    throw "AllowedScopes must include the exact 'hyperdrive.sync' scope."
}

$key = [System.Security.Cryptography.ECDsa]::Create()
try {
    $key.GenerateKey([System.Security.Cryptography.ECCurve]::CreateFromFriendlyName('nistP256'))
    $privateKey = [Convert]::ToBase64String($key.ExportPkcs8PrivateKey())
    $publicKey = [Convert]::ToBase64String($key.ExportSubjectPublicKeyInfo())

    $verificationKey = [System.Security.Cryptography.ECDsa]::Create()
    try {
        [int]$bytesRead = 0
        $verificationKey.ImportSubjectPublicKeyInfo([Convert]::FromBase64String($publicKey), [ref]$bytesRead)
        $payload = [Text.Encoding]::UTF8.GetBytes('oasis-edge-offline-grant-key-pair-check')
        $signature = $key.SignData($payload, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
        if (!$verificationKey.VerifyData($payload, $signature, [System.Security.Cryptography.HashAlgorithmName]::SHA256)) {
            throw 'Generated signing key pair failed verification.'
        }
    }
    finally {
        $verificationKey.Dispose()
    }

    $secretPath = Join-Path $outputPath 'offline-grant-signing.secrets.env'
    $serverPath = Join-Path $outputPath 'onode-offline-grants.public.json'
    $edgePath = Join-Path $outputPath 'edge-offline-grant.public.json'

    Set-Content -LiteralPath $secretPath -Encoding utf8NoBOM -NoNewline -Value (
        "{0}={1}`n" -f $PrivateKeyEnvironmentVariable, $privateKey)

    [ordered]@{
        OfflineSessionGrants = [ordered]@{
            Enabled = $true
            SigningPrivateKeyEnvironmentVariable = $PrivateKeyEnvironmentVariable
            SigningPublicKey = $publicKey
            MaximumLifetimeMinutes = $MaximumLifetimeMinutes
            AllowedScopes = $normalizedScopes
        }
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $serverPath -Encoding utf8NoBOM

    [ordered]@{
        enableEdgeRuntime = $true
        edgeOfflineGrantPublicKey = $publicKey
        edgeOfflineGrantLifetimeMinutes = $MaximumLifetimeMinutes
        edgeOfflineScopes = $normalizedScopes
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $edgePath -Encoding utf8NoBOM

    Write-Host "Generated verified Edge offline-grant configuration in $outputPath"
    Write-Host "SECRET (never commit): $secretPath"
    Write-Host "ONODE public DNA fragment: $serverPath"
    Write-Host "Edge/Unity public config fragment: $edgePath"
}
finally {
    if ($null -ne $key) { $key.Dispose() }
}
