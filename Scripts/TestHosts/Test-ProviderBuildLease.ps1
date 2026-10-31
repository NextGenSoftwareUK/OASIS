#Requires -Version 7.0
param([switch]$Contender)
$ErrorActionPreference = 'Stop'
$leaseTestRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$leaseTestDirectory = Join-Path $leaseTestRoot 'TestResults/ProviderBuildCurrent'
$leaseTestSentinel = Join-Path $leaseTestDirectory 'lease-test-operation-ran.txt'
$leaseTestWrapper = Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1'
if ($Contender) {
    & $leaseTestWrapper -Owner 'lease-contract-child' -Operation {
        [IO.File]::WriteAllText($leaseTestSentinel, 'entered')
    }
    exit 0
}
New-Item -ItemType Directory -Path $leaseTestDirectory -Force | Out-Null
if (Test-Path -LiteralPath $leaseTestSentinel) { Remove-Item -LiteralPath $leaseTestSentinel }
function Invoke-LeaseTestChild([string]$label) {
    $leaseTestChild = Start-Process -FilePath (Join-Path $PSHOME 'pwsh.exe') -ArgumentList @('-NoProfile','-NonInteractive','-File',$PSCommandPath,'-Contender') `
        -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $leaseTestDirectory ($label+'.out.log')) `
        -RedirectStandardError (Join-Path $leaseTestDirectory ($label+'.err.log'))
    try {
        if (!$leaseTestChild.WaitForExit(10000)) { throw 'Owned lease test child did not exit within ten seconds.' }
        return $leaseTestChild.ExitCode
    }
    finally {
        if (!$leaseTestChild.HasExited) { Stop-Process -Id $leaseTestChild.Id -ErrorAction Stop }
        $leaseTestChild.Dispose()
    }
}
$leaseTestBusyExit = & $leaseTestWrapper -Owner 'lease-contract-parent' -Operation {
    $leaseTestObservedBusyExit = Invoke-LeaseTestChild 'lease-busy'
    if ($null -eq $leaseTestObservedBusyExit -or $leaseTestObservedBusyExit -eq 0 -or (Test-Path -LiteralPath $leaseTestSentinel)) { throw 'A competing process entered a locked build operation or has no verified exit code.' }
    return $leaseTestObservedBusyExit
}
$leaseTestFreeExit = Invoke-LeaseTestChild 'lease-free'
if ($leaseTestFreeExit -ne 0 -or !(Test-Path -LiteralPath $leaseTestSentinel)) { throw 'A released lease could not be acquired.' }
$leaseTestFailureObserved = $false
try { & $leaseTestWrapper -Owner 'lease-contract-throw' -Operation { throw 'controlled lease body failure' } }
catch { if ($_.Exception.Message -ne 'controlled lease body failure') { throw }; $leaseTestFailureObserved = $true }
if (!$leaseTestFailureObserved) { throw 'The build operation failure was swallowed.' }
& $leaseTestWrapper -Owner 'lease-contract-after-throw' -Operation { [IO.File]::WriteAllText($leaseTestSentinel, 'released after error') }
if ([IO.File]::ReadAllText($leaseTestSentinel) -ne 'released after error') { throw 'A failed operation retained the lease.' }
Remove-Item -LiteralPath $leaseTestSentinel
@{ BusyChildExit=$leaseTestBusyExit; FreeChildExit=$leaseTestFreeExit; ExceptionReleased=$true } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $leaseTestDirectory 'lease-contract.json') -Encoding utf8
Write-Output 'Cross-process build exclusion, acquisition after release, and exception cleanup: PASS.'
