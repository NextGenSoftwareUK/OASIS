#requires -Version 7.5
[CmdletBinding()]
param([switch]$CreateIfMissing,[string]$EmptyStoreProof)
$ErrorActionPreference='Stop'
$redisStorageDirectory=Join-Path $env:LOCALAPPDATA 'OASIS/ReusableProviderTests/Redis'
$redisStoragePath=Join-Path $redisStorageDirectory 'storage-key.dpapi'
if(!(Test-Path -LiteralPath $redisStoragePath)){
    if(!$CreateIfMissing){throw 'Existing Redis storage key required; never implicitly replace it.'}
    $redisExpectedProof=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../TestResults/RedisCurrent/private-provider-empty.json'))
    if(!$EmptyStoreProof -or [IO.Path]::GetFullPath($EmptyStoreProof) -cne $redisExpectedProof){throw 'Exact readonly native admission proof required before first key initialization.'}
    $redisProof=Get-Content -LiteralPath $redisExpectedProof -Raw|ConvertFrom-Json -DateKind String
    $redisProofAge=[DateTimeOffset]::UtcNow-[DateTimeOffset]::Parse($redisProof.observedUtc,[Globalization.CultureInfo]::InvariantCulture)
    if($redisProof.endpoint -cne '127.0.0.1:18679' -or $redisProof.database -ne 0 -or $redisProof.count -ne 0 -or $redisProofAge.TotalSeconds -lt 0 -or $redisProofAge.TotalSeconds -gt 60){throw 'Fresh empty dedicated native DB proof required; preserve existing data and recover its key instead.'}
    $redisStorageBytes=[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
    try{$redisStorageCipher=[Security.Cryptography.ProtectedData]::Protect($redisStorageBytes,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)}
    finally{[Array]::Clear($redisStorageBytes,0,$redisStorageBytes.Length)}
    [IO.Directory]::CreateDirectory($redisStorageDirectory)|Out-Null
    $redisStorageFile=[IO.File]::Open($redisStoragePath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$redisStorageFile.Write($redisStorageCipher,0,$redisStorageCipher.Length);$redisStorageFile.Flush($true)}finally{$redisStorageFile.Dispose()}
}
$redisStorageDecoded=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($redisStoragePath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
try{
    if($redisStorageDecoded.Length -ne 32){throw 'Invalid persisted storage key; preserve it, never overwrite.'}
    $redisStorageSecure=ConvertTo-SecureString ([Convert]::ToBase64String($redisStorageDecoded)) -AsPlainText -Force
    return $redisStorageSecure
}finally{[Array]::Clear($redisStorageDecoded,0,$redisStorageDecoded.Length)}
