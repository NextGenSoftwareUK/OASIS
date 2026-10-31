$ErrorActionPreference='Stop'
$probe=Join-Path $PSScriptRoot 'Get-SolanaHistoryAvailability.ps1'
$requests=[Collections.Generic.List[object]]::new()
$mode='available'
$signature='1'*64
function Invoke-RestMethod {
    param($Uri,$Method,$ContentType,$Body,$TimeoutSec)
    $request=$Body | ConvertFrom-Json
    $requests.Add($request)
    if($mode -eq 'rpc-error' -and $request.method -eq 'getTransaction'){
        return [pscustomobject]@{error=[pscustomobject]@{code=-32000}}
    }
    $value=switch($request.method){
        'getFirstAvailableBlock' {100}
        'minimumLedgerSlot' {90}
        'getSignatureStatuses' {
            if($mode -eq 'missing-status-result'){$null}
            else {[pscustomobject]@{value=@($null)}}
        }
        'getTransaction' {
            if($mode -eq 'available'){
                [pscustomobject]@{slot=101;transaction=[pscustomobject]@{signatures=@($signature)}}
            } elseif($mode -eq 'wrong-identity'){
                [pscustomobject]@{slot=101;transaction=[pscustomobject]@{signatures=@('2'*64)}}
            }
        }
        default {throw 'Probe attempted an unapproved RPC method.'}
    }
    [pscustomobject]@{result=$value}
}
foreach($mode in @('available','absent','rpc-error','wrong-identity','missing-status-result')){
    $mode=$mode
    $requests.Clear()
    $failed=$false
    try {$result=& $probe -Signatures @($signature)} catch {$failed=$true; $failure=$_}
    if($failed -ne ($mode -in @('rpc-error','wrong-identity','missing-status-result'))){throw "Unexpected outcome: $mode; $failure"}
    if(!$failed -and ($result.Transactions.Count -ne 1 -or $result.Transactions[0].TransactionPresent -ne ($mode -eq 'available'))){throw 'Availability evidence differs.'}
    $lookup=$requests | Where-Object method -eq 'getSignatureStatuses'
    if(@($lookup.params[0]).Count -ne 1 -or $lookup.params[0][0] -ne $signature -or !$lookup.params[1].searchTransactionHistory){throw 'History request changed shape.'}
    if($mode -eq 'missing-status-result'){
        if($requests.Count -ne 3){throw 'Malformed status did not stop the probe.'}
        continue
    }
    $transaction=$requests | Where-Object method -eq 'getTransaction'
    if($transaction.params[1].commitment -ne 'finalized' -or $transaction.params[1].maxSupportedTransactionVersion -ne 1){throw 'Native read options differ.'}
    if($transaction.params[0] -ne $signature){throw 'Transaction signature differs.'}
    if($requests.Count -ne 4){throw 'Probe retried or issued an unexpected RPC.'}
}
'PASS: five availability/error/identity schedules; finalized reads, bounded calls, no submissions.'
