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
$paths += Join-Path $repo 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot/psp34-mutation-pending.jsonl'
$paths += Join-Path $repo 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot/psp34-public-mutation-pending.jsonl'
$paths += Join-Path $repo 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot/psp34-custody-pending.jsonl'
$paths += Join-Path $repo 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot/psp34-escrow-deployment-pending.json'
$paths += Join-Path $repo 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot/psp34-escrow-lifecycle-pending.jsonl'
$paths += Join-Path $repo 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot/psp34-escrow-public-pending.jsonl'
$cases = 0
$runners = @(
    @{ Name='Run-ReusablePolkadotNativeTests.ps1'; Arguments=@{} },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{} },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftDeploy=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftPublicRead=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftPreparation=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftLifecycle=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftLifecycleRecover=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftIntentRecovery=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftCustodySource=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftCustody=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftCustodyRecover=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftEscrowConstructor=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftEscrowDeploy=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftEscrowReconcile=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftEscrowLifecycle=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftEscrowLifecycleRecover=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftEscrowPublic=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftEscrowPublicRecover=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftPublicMutation=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftPublicRecover=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftConstructorSimulation=$true } },
    @{ Name='Run-ReusablePolkadotRecordTests.ps1'; Arguments=@{ NftReconcile=$true } }
)
foreach ($entry in $runners) {
    $runner=$entry.Name
    $arguments=$entry.Arguments
    foreach ($path in $paths) {
        # Reconciliation may inspect its own original, but may not bypass other
        # unresolved sequences. Its read-only path is verified by the native gate.
        if ($arguments.ContainsKey('NftReconcile') -and $path.EndsWith('psp34-deployment-pending.json')) { continue }
        if ($arguments.ContainsKey('NftLifecycleRecover') -and $path.EndsWith('psp34-mutation-pending.jsonl')) { continue }
        if ($arguments.ContainsKey('NftPublicRecover') -and $path.EndsWith('psp34-public-mutation-pending.jsonl')) { continue }
        if ($arguments.ContainsKey('NftCustodyRecover') -and $path.EndsWith('psp34-custody-pending.jsonl')) { continue }
        if ($arguments.ContainsKey('NftEscrowReconcile') -and $path.EndsWith('psp34-escrow-deployment-pending.json')) { continue }
        if ($arguments.ContainsKey('NftEscrowLifecycleRecover') -and $path.EndsWith('psp34-escrow-lifecycle-pending.jsonl')) { continue }
        if ($arguments.ContainsKey('NftEscrowPublicRecover') -and $path.EndsWith('psp34-escrow-public-pending.jsonl')) { continue }
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
        try { & (Join-Path $PSScriptRoot $runner) @arguments }
        catch {
            if ($_.Exception.Message -notmatch '^Pending (original|public storage sequence) exists') { throw }
            $rejected = $true
        }
        if (!$rejected) { throw "$runner admitted unresolved original: $path" }
        $cases++
    }
}
Write-Output "Polkadot cross-runner pending fences: $cases passed; no RPC/build/signing or fixture writes."
