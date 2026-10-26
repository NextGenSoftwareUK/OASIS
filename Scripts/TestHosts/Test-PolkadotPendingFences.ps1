#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$evidence = Join-Path $repo 'TestResults/PolkadotCurrent'
if (!(Microsoft.PowerShell.Management\Test-Path -LiteralPath $evidence -PathType Container)) {
    throw 'Reuse the retained coordination directory; this test creates no environment.'
}

# Simulate only presence checks. Never create/delete a real pending original.
# Every case must throw before any network, build, SDK or transaction operation.
function Invoke-RestMethod { throw 'Unexpected RPC before pending fence' }
function node { throw 'Unexpected SDK before pending fence' }
function dotnet { throw 'Unexpected build before pending fence' }

$paths = @('local-record-contract-pending.json','local-record-owner-pending.json',
    'local-record-sdk-pending.json','record-csharp-pending.json') |
    ForEach-Object { Join-Path $evidence $_ }
$paths += Join-Path $repo 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot/public-storage-pending.jsonl'
$paths += Join-Path $repo 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot/psp34-deployment-pending.json'
$cases = 0
foreach ($runner in @('Run-ReusablePolkadotNativeTests.ps1','Run-ReusablePolkadotRecordTests.ps1')) {
    foreach ($path in $paths) {
        $simulatedPending = [IO.Path]::GetFullPath($path)
        Set-Item Function:Test-Path -Value ({
            param([string]$LiteralPath, [string]$PathType)
            if ($LiteralPath -match '-pending\.(json|jsonl)$') {
                return [IO.Path]::GetFullPath($LiteralPath) -eq $simulatedPending
            }
            $arguments = @{ LiteralPath = $LiteralPath }
            if ($PathType) { $arguments.PathType = $PathType }
            Microsoft.PowerShell.Management\Test-Path @arguments
        }.GetNewClosure())
        $rejected = $false
        try { & (Join-Path $PSScriptRoot $runner) }
        catch {
            if ($_.Exception.Message -notmatch '^Pending (original|public storage sequence) exists') { throw }
            $rejected = $true
        }
        if (!$rejected) { throw "$runner admitted unresolved original: $path" }
        $cases++
    }
}
Write-Output "Polkadot cross-runner pending fences: $cases passed; no RPC/build/signing or fixture writes."
