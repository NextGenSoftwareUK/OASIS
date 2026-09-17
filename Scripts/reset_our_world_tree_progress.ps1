<#
.SYNOPSIS
Reset only the signed-in avatar's Anorak demo tree inventory, collection history and quest progress.
.DESCRIPTION
Defaults to a read-only plan. Use -Apply to execute. Stop Our World Play mode first.
Saves a scoped backup before mutation and verifies inventory reconciliation remains at zero.
#>
[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$Web4BaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [string]$Web5BaseUrl = 'https://dev.api.starnet.oasisomniverse.one',
    [string]$CredentialPath = (Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geonft-seed.credential.clixml'),
    [string]$BackupDirectory = (Join-Path $env:LOCALAPPDATA 'OASIS/AnorakResetBackups')
)
$ErrorActionPreference = 'Stop'
function Unwrap($response) {
    if ($null -eq $response.PSObject.Properties['isError']) { $response = $response.result }
    if ($null -eq $response -or $null -eq $response.PSObject.Properties['isError'] -or $response.isError) {
        throw "API operation failed: $($response.message)"
    }
    return $response.result
}
$credential = Import-Clixml -LiteralPath $CredentialPath
$body = @{ username=$credential.UserName; password=$credential.GetNetworkCredential().Password } | ConvertTo-Json
try { $avatar = Unwrap (Invoke-RestMethod "$Web4BaseUrl/api/avatar/authenticate" -Method Post -ContentType application/json -Body $body) }
finally { $body=$null; $credential=$null }
$headers = @{ Authorization="Bearer $($avatar.jwtToken)" }
function Api($base, $path, $method='Get', $data=$null) {
    $request = @{ Uri="$($base.TrimEnd('/'))/api/$path"; Headers=$headers; Method=$method; TimeoutSec=90 }
    if ($null -ne $data) { $request.ContentType='application/json'; $request.Body=ConvertTo-Json -InputObject $data -Depth 60 }
    Unwrap (Invoke-RestMethod @request)
}
try {
    $quests = @(Api $Web5BaseUrl 'quests/all-for-avatar/game')
    $target = @($quests | Where-Object startupSequence -eq 'anorak-trees')
    if ($target.Count -ne 1) { throw 'Expected exactly one Anorak quest for the signed-in avatar.' }
    $quest = Api $Web5BaseUrl "quests/$($target[0].id)"
    $required = @($quest.objectives | ForEach-Object { $_.needToCollectItems.'Our World' } | ForEach-Object {
        if ($_ -notmatch '^geonft:([0-9a-f-]{36})$') { throw "Unexpected Anorak requirement: $_" }
        $Matches[1]
    })
    if ($required.Count -notin @(4,5)) { throw 'Expected four or five tree objectives.' }
    $placements = @(Api $Web4BaseUrl 'nft/load-all-geo-nfts/MongoDBOASIS/false')
    $canonical = @($placements | Where-Object { $_.id -in $required })
    if ($canonical.Count -ne $required.Count) { throw 'Anorak placement is missing.' }
    # Include prior seed duplicates only when tagged as demo data and at the same quest coordinates.
    $demo = @($placements | Where-Object {
        $candidate = $_
        $candidate.metaData.'OurWorld.DemoSeed' -eq 'true' -and
        @($canonical | Where-Object { [Math]::Abs($_.lat-$candidate.lat) -lt 0.000000001 -and [Math]::Abs($_.long-$candidate.long) -lt 0.000000001 }).Count -gt 0
    })
    if (@($required | Where-Object { $_ -notin $demo.id }).Count) { throw 'Refusing to reset non-demo quest placements.' }
    $inventory = @(Api $Web4BaseUrl 'avatar/inventory')
    $items = @($inventory | Where-Object { $_.geoNFTId -in $demo.id })
    $untouched = @($inventory | Where-Object { $_.geoNFTId -notin $demo.id })
    $detail = Api $Web4BaseUrl "avatar/get-avatar-detail-by-id/$($avatar.id)"
    $historyKey = 'GeoNFT.CollectionHistory.v1'
    $historyValue = $detail.metaData.$historyKey
    $history = if ($historyValue) { $historyValue | ConvertFrom-Json } else { [pscustomobject]@{} }
    $removedHistory = @($history.PSObject.Properties | Where-Object { $_.Name -in $demo.id })
    Write-Host "Avatar $($avatar.id): $($items.Count) demo inventory rows, $($removedHistory.Count) history entries, quest $($quest.id). Other inventory rows: $($untouched.Count)."
    if (!$Apply) { Write-Host 'Read-only plan. Use -Apply to reset.'; return }
    New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
    $backup = Join-Path $BackupDirectory ("anorak-{0:yyyyMMdd-HHmmss}-{1}.json" -f [DateTime]::UtcNow,[guid]::NewGuid().ToString('N'))
    $preservedState = @{activeQuestId=$detail.activeQuestId;activeObjectiveId=$detail.activeObjectiveId;dimensionLevel=$detail.dimensionLevel}
    @{ avatarId=$avatar.id; quest=$quest; inventory=$items; history=$historyValue; placementIds=@($demo.id); preservedAvatarState=$preservedState } |
        ConvertTo-Json -Depth 70 | Set-Content -LiteralPath $backup -Encoding UTF8
    foreach ($item in $items) {
        $removed = Api $Web4BaseUrl "avatar/inventory/$($item.id)?quantity=$([Math]::Max(1,[int]$item.quantity))" 'Delete'
        if ($removed -ne $true) { throw "Inventory removal was not confirmed: $($item.id)" }
    }
    if ($removedHistory.Count) {
        foreach ($entry in $removedHistory) { $history.PSObject.Properties.Remove($entry.Name) }
        $metadata = @{}; $metadata[$historyKey] = ConvertTo-Json -InputObject $history -Depth 20 -Compress
        # The general avatar-detail update applies these three fields even when omitted.
        # Preserve them explicitly while merging only the collection-history metadata.
        $null = Api $Web4BaseUrl "avatar/update-avatar-detail-by-id/$($avatar.id)" 'Post' @{
            metaData=$metadata;activeQuestId=$detail.activeQuestId
            activeObjectiveId=$detail.activeObjectiveId;dimensionLevel=$detail.dimensionLevel
        }
    }
    $null = Api $Web5BaseUrl "quests/$($quest.id)/progress/reset" 'Post' @{}
    $after = @(Api $Web4BaseUrl 'avatar/inventory')
    if (@($after | Where-Object {$_.geoNFTId -in $demo.id}).Count) { throw 'Demo tree inventory remains after reset.' }
    if ((ConvertTo-Json -InputObject @($untouched | Sort-Object id) -Depth 40 -Compress) -ne
        (ConvertTo-Json -InputObject @($after | Sort-Object id) -Depth 40 -Compress)) { throw 'Unrelated inventory changed during reset; inspect backup and concurrent clients.' }
    $detailAfter = Api $Web4BaseUrl "avatar/get-avatar-detail-by-id/$($avatar.id)"
    if ($detailAfter.metaData.$historyKey) {
        $historyAfter = $detailAfter.metaData.$historyKey | ConvertFrom-Json
        if (@($historyAfter.PSObject.Properties | Where-Object {$_.Name -in $demo.id}).Count) { throw 'Demo collection history remains.' }
    }
    $reconciled = Api $Web5BaseUrl "quests/$($quest.id)/inventory-progress" 'Post' @{}
    $completed = @($reconciled.quest.objectives | Where-Object isCompleted).Count
    if ($completed -ne 0) { throw "Reconciliation unexpectedly completed $completed objectives." }
    Write-Host "Verified 0/$($required.Count) after inventory reconciliation; $($untouched.Count) unrelated inventory rows unchanged. Backup: $backup"
} finally { $headers.Clear(); $avatar=$null }
