#Requires -Version 7.0
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Limit-OwnedMongoLog.ps1')
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/MongoLogRetentionCurrent'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$log=Join-Path $work 'mongo-current.log';$tail=Join-Path $work 'mongo-current-tail.log'
try {
    if((Test-Path -LiteralPath $log) -or (Test-Path -LiteralPath $tail)){throw 'Existing test evidence requires inspection.'}
    [IO.File]::WriteAllText($log,"small`n")
    Limit-OwnedMongoLog $work
    if([IO.File]::ReadAllText($log) -ne "small`n"){throw 'Small log changed.'}
    $busy=[IO.File]::Open($log,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None)
    $rejected=$false
    try {Limit-OwnedMongoLog $work} catch {$rejected=$true} finally {$busy.Dispose()}
    if(!$rejected){throw 'Busy log was accepted.'}
    $line='{"owned":"'+('x'*1000)+'"}'+"`n"
    [IO.File]::WriteAllText($log,($line*9000))
    Limit-OwnedMongoLog $work
    if((Get-Item -LiteralPath $log).Length -ne 0){throw 'Large raw log not truncated.'}
    $retained=Get-Item -LiteralPath $tail
    if($retained.Length -gt 64KB -or $retained.Length -eq 0){throw 'Tail not bounded.'}
    foreach($entry in [IO.File]::ReadAllLines($tail)){$null=$entry | ConvertFrom-Json}
    [IO.File]::WriteAllText($log,($line*9000))
    $originalLength=(Get-Item -LiteralPath $log).Length
    $busy=[IO.File]::Open($tail,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None)
    $rejected=$false
    try {Limit-OwnedMongoLog $work} catch {$rejected=$true} finally {$busy.Dispose()}
    if(!$rejected -or (Get-Item -LiteralPath $log).Length -ne $originalLength){throw 'Tail failure did not preserve raw log.'}
    Limit-OwnedMongoLog $work
    # Only exact synthetic files, after successful assertions; failed evidence stays.
    [IO.File]::Delete($log);[IO.File]::Delete($tail)
    Write-Output 'Owned Mongo retention: small-file preservation, exclusive-access rejection, tail-failure raw preservation, bounded complete-line tail and exact fixture cleanup passed.'
} finally {$lease.Dispose()}
