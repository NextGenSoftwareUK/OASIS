#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][scriptblock]$Operation,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Owner
)

$ErrorActionPreference = 'Stop'

# Core/Shared producers and consumers mutate the same outputs across sessions.
# Announcements are not mutual exclusion. Fail before running any operation when busy.
# Wrap an entire reusable runner, or a producer/consumer batch, once (never nest).
# The operation remains responsible for checking native exit codes/test evidence.
$providerBuildRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$providerBuildDirectory = Join-Path $providerBuildRoot 'TestResults/ProviderBuildCurrent'
New-Item -ItemType Directory -Path $providerBuildDirectory -Force | Out-Null
$providerBuildLeasePath = Join-Path $providerBuildDirectory 'producer-consumer.lock'
$providerBuildLease = [IO.File]::Open($providerBuildLeasePath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $providerBuildOwner = @{ Owner=$Owner; ProcessId=$PID; StartedUtc=[DateTime]::UtcNow.ToString('O'); Workspace=$providerBuildRoot } | ConvertTo-Json -Compress
    $providerBuildBytes = [Text.UTF8Encoding]::new($false).GetBytes($providerBuildOwner)
    $providerBuildLease.SetLength(0)
    $providerBuildLease.Write($providerBuildBytes, 0, $providerBuildBytes.Length)
    $providerBuildLease.Flush()
    & $Operation
}
finally {
    $providerBuildLease.Dispose()
}
