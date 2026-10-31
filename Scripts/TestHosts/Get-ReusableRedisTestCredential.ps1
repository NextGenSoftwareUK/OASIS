[CmdletBinding()]
param([switch]$CreateIfMissing)
$ErrorActionPreference='Stop'
$redisCredentialDirectory=Join-Path $env:LOCALAPPDATA 'OASIS/ReusableProviderTests/Redis'
$redisCredentialPath=Join-Path $redisCredentialDirectory 'test-auth.dpapi'
if(!(Test-Path -LiteralPath $redisCredentialPath)){
    if(!$CreateIfMissing){throw 'Existing protected Redis test credential required; no implicit replacement.'}
    $redisRandom=[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
    $redisPlain=[Text.Encoding]::ASCII.GetBytes([Convert]::ToHexString($redisRandom))
    try{$redisCipher=[Security.Cryptography.ProtectedData]::Protect($redisPlain,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)}
    finally{[Array]::Clear($redisRandom,0,$redisRandom.Length);[Array]::Clear($redisPlain,0,$redisPlain.Length)}
    [IO.Directory]::CreateDirectory($redisCredentialDirectory)|Out-Null
    $redisCredentialFile=[IO.File]::Open($redisCredentialPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$redisCredentialFile.Write($redisCipher,0,$redisCipher.Length);$redisCredentialFile.Flush($true)}finally{$redisCredentialFile.Dispose()}
}
$redisDecoded=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($redisCredentialPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
try{
    if($redisDecoded.Length -ne 64 -or @($redisDecoded|Where-Object{!($_ -ge 48 -and $_ -le 57 -or $_ -ge 65 -and $_ -le 70)}).Count){throw 'Protected Redis credential format invalid; preserve and review, never overwrite.'}
    $redisSecure=[Security.SecureString]::new()
    foreach($redisCharacter in $redisDecoded){$redisSecure.AppendChar([char]$redisCharacter)}
    $redisSecure.MakeReadOnly()
    return $redisSecure # Never return plaintext or place it on a command line.
}finally{[Array]::Clear($redisDecoded,0,$redisDecoded.Length)}
