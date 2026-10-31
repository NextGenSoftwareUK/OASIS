# Read-only availability evidence, not canonical record validation or permission to replay.
param(
    [Parameter(Mandatory)][ValidateCount(1,8)]
    [ValidatePattern('^[1-9A-HJ-NP-Za-km-z]{64,88}$')][string[]]$Signatures,
    [string]$Endpoint='http://127.0.0.1:18899'
)
$ErrorActionPreference='Stop'
function Read-HistoryRpc([string]$Method,[object[]]$Arguments) {
    $body=@{jsonrpc='2.0';id=1;method=$Method;params=@($Arguments)} | ConvertTo-Json -Depth 8 -Compress
    $reply=Invoke-RestMethod -Uri $Endpoint -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 5
    if($reply.error -or !$reply.PSObject.Properties['result']){throw "Read-only $Method failed; absence cannot be inferred."}
    return $reply.result
}
$first=Read-HistoryRpc 'getFirstAvailableBlock' @()
$minimum=Read-HistoryRpc 'minimumLedgerSlot' @()
$status=Read-HistoryRpc 'getSignatureStatuses' @(@($Signatures),@{searchTransactionHistory=$true})
if($null -eq $status -or !$status.PSObject.Properties['value'] -or @($status.value).Count -ne $Signatures.Count){throw 'Signature status cardinality differs.'}
$rows=for($index=0;$index -lt $Signatures.Count;$index++){
    $signature=$Signatures[$index]
    $transaction=Read-HistoryRpc 'getTransaction' @($signature,@{encoding='json';commitment='finalized';maxSupportedTransactionVersion=1})
    if($null -ne $transaction -and (@($transaction.transaction.signatures).Count -eq 0 -or $transaction.transaction.signatures[0] -cne $signature)){
        throw 'Native transaction identity differs; no availability claim returned.'
    }
    $observation=$status.value[$index]
    [pscustomobject]@{
        Signature=$signature;TransactionPresent=$null -ne $transaction
        TransactionSlot=if($null -ne $transaction){$transaction.slot}else{$null}
        StatusPresent=$null -ne $observation
        StatusSlot=if($null -ne $observation){$observation.slot}else{$null}
        ConfirmationStatus=if($null -ne $observation){$observation.confirmationStatus}else{$null}
    }
}
[pscustomobject]@{FirstAvailableBlock=$first;MinimumLedgerSlot=$minimum;Transactions=@($rows)}
