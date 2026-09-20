<#
.SYNOPSIS
Reset all seeded Our World test inventory, GeoNFT collection history and quest progress.
.DESCRIPTION
Defaults to a read-only plan. Use -Apply to execute. Targets only GeoNFT placements
marked OurWorld.DemoSeed=true or OurWorld.TestSuite=quest-mode-spawn-matrix.
#>
[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$Web4BaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [string]$Web5BaseUrl = 'https://dev.api.starnet.oasisomniverse.one',
    [ValidateNotNullOrEmpty()][string]$Provider = 'MongoDBOASIS',
    [string]$CredentialPath = (Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geonft-seed.credential.clixml'),
    [string]$BackupDirectory = (Join-Path $env:LOCALAPPDATA 'OASIS/OurWorldTestResetBackups')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Unwrap($response) {
    if ($null -eq $response.PSObject.Properties['isError']) { $response = $response.result }
    if ($null -eq $response -or $null -eq $response.PSObject.Properties['isError'] -or $response.isError) {
        throw "API operation failed: $($response.message)"
    }
    return $response.result
}

$credential = Import-Clixml -LiteralPath $CredentialPath
$loginBody = @{ username=$credential.UserName; password=$credential.GetNetworkCredential().Password } | ConvertTo-Json
try { $avatar = Unwrap (Invoke-RestMethod "$Web4BaseUrl/api/avatar/authenticate" -Method Post -ContentType application/json -Body $loginBody) }
finally { $loginBody=$null; $credential=$null }
$headers = @{ Authorization="Bearer $($avatar.jwtToken)" }

function Api($base, $path, $method='Get', $data=$null) {
    $request = @{ Uri="$($base.TrimEnd('/'))/api/$path"; Headers=$headers; Method=$method; TimeoutSec=120 }
    if ($null -ne $data) { $request.ContentType='application/json'; $request.Body=ConvertTo-Json -InputObject $data -Depth 80 }
    Unwrap (Invoke-RestMethod @request)
}

function PropertyValue($object, [string]$name) {
    if ($null -eq $object) { return $null }
    $property = $object.PSObject.Properties[$name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

try {
    $placements = @(Api $Web4BaseUrl "nft/load-all-geo-nfts/$Provider/false")
    $testPlacements = @($placements | Where-Object {
        $metadata = PropertyValue $_ 'metaData'
        (PropertyValue $metadata 'OurWorld.DemoSeed') -eq 'true' -or
        (PropertyValue $metadata 'OurWorld.TestSuite') -eq 'quest-mode-spawn-matrix'
    })
    $testIds = @($testPlacements | ForEach-Object { [string]$_.id } | Where-Object { $_ } | Select-Object -Unique)
    if ($testIds.Count -eq 0) { throw 'No tagged Our World test GeoNFT placements were found.' }

    $questSummaries = @(Api $Web5BaseUrl 'quests/all-for-avatar/game')
    $testQuests = @()
    foreach ($summary in $questSummaries) {
        $quest = Api $Web5BaseUrl "quests/$($summary.id)"
        $requiredIds = @($quest.objectives | ForEach-Object {
            $direct = PropertyValue $_ 'needToCollectItems'
            $dictionaries = PropertyValue $_ 'dictionaries'
            $nested = PropertyValue $dictionaries 'needToCollectItems'
            @((PropertyValue $direct 'Our World')) + @((PropertyValue $nested 'Our World'))
        } | ForEach-Object {
            if ([string]$_ -match '^geonft:([0-9a-f-]{36})$') { $Matches[1] }
        })
        if (@($requiredIds | Where-Object { $_ -in $testIds }).Count -gt 0) { $testQuests += $quest }
    }
    if ($testQuests.Count -eq 0) { throw 'No avatar quests reference the tagged Our World test GeoNFTs.' }

    $inventory = @(Api $Web4BaseUrl 'avatar/inventory')
    $testItems = @($inventory | Where-Object { [string]$_.geoNFTId -in $testIds })
    $untouched = @($inventory | Where-Object { [string]$_.geoNFTId -notin $testIds })
    $detail = Api $Web4BaseUrl "avatar/get-avatar-detail-by-id/$($avatar.id)"
    $historyKey = 'GeoNFT.CollectionHistory.v1'
    $historyValue = $detail.metaData.$historyKey
    $history = if ($historyValue) { $historyValue | ConvertFrom-Json } else { [pscustomobject]@{} }
    $testHistory = @($history.PSObject.Properties | Where-Object { $_.Name -in $testIds })

    Write-Host "Avatar $($avatar.id): $($testItems.Count) seeded test inventory rows, $($testHistory.Count) history entries, $($testQuests.Count) test quests. Other inventory rows: $($untouched.Count)."
    if (!$Apply) { Write-Host 'Read-only plan. Use -Apply to reset.'; return }

    New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
    $backup = Join-Path $BackupDirectory ("our-world-test-{0:yyyyMMdd-HHmmss}-{1}.json" -f [DateTime]::UtcNow,[guid]::NewGuid().ToString('N'))
    @{ avatarId=$avatar.id; quests=$testQuests; inventory=$testItems; history=$historyValue; placementIds=$testIds } |
        ConvertTo-Json -Depth 80 | Set-Content -LiteralPath $backup -Encoding UTF8

    foreach ($item in $testItems) {
        $removed = Api $Web4BaseUrl "avatar/inventory/$($item.id)?quantity=$([Math]::Max(1,[int]$item.quantity))" 'Delete'
        if ($removed -ne $true) { throw "Inventory removal was not confirmed: $($item.id)" }
    }
    if ($testHistory.Count) {
        foreach ($entry in $testHistory) { $history.PSObject.Properties.Remove($entry.Name) }
        $metadata = @{}; $metadata[$historyKey] = ConvertTo-Json -InputObject $history -Depth 30 -Compress
        $avatarUpdate = @{ metaData=$metadata }
        foreach ($field in 'activeQuestId','activeObjectiveId','dimensionLevel') {
            $value = PropertyValue $detail $field
            if ($null -ne $value) { $avatarUpdate[$field] = $value }
        }
        $null = Api $Web4BaseUrl "avatar/update-avatar-detail-by-id/$($avatar.id)" 'Post' $avatarUpdate
    }
    foreach ($quest in $testQuests) { $null = Api $Web5BaseUrl "quests/$($quest.id)/progress/reset" 'Post' @{} }

    $after = @(Api $Web4BaseUrl 'avatar/inventory')
    if (@($after | Where-Object { [string]$_.geoNFTId -in $testIds }).Count) { throw 'Seeded test inventory remains after reset.' }
    if ((ConvertTo-Json -InputObject @($untouched | Sort-Object id) -Depth 50 -Compress) -ne
        (ConvertTo-Json -InputObject @($after | Sort-Object id) -Depth 50 -Compress)) { throw 'Unrelated inventory changed during reset; inspect the backup.' }
    $detailAfter = Api $Web4BaseUrl "avatar/get-avatar-detail-by-id/$($avatar.id)"
    if ($detailAfter.metaData.$historyKey) {
        $historyAfter = $detailAfter.metaData.$historyKey | ConvertFrom-Json
        if (@($historyAfter.PSObject.Properties | Where-Object { $_.Name -in $testIds }).Count) { throw 'Seeded test collection history remains.' }
    }
    foreach ($quest in $testQuests) {
        $reconciled = Api $Web5BaseUrl "quests/$($quest.id)/inventory-progress" 'Post' @{}
        if (@($reconciled.quest.objectives | Where-Object isCompleted).Count -ne 0) {
            throw "Quest '$($quest.name)' did not remain at zero after inventory reconciliation."
        }
    }
    Write-Host "Verified all $($testQuests.Count) seeded quests at zero; $($untouched.Count) unrelated inventory rows unchanged. Backup: $backup"
} finally {
    $headers.Clear(); $avatar=$null
}
