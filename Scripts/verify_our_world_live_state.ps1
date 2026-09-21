<#
.SYNOPSIS
Verifies the deployed Our World GeoNFT inventory and Anorak quest contract.
.DESCRIPTION
Authenticates with the existing encrypted development credential, checks that every
manifest placement exists exactly once, that every collected placement has exactly
one canonical Nature inventory row, and that WEB5 has one matching Anorak quest.
The inventory-progress call is idempotent and verifies the deployed reconciliation
path as well as the saved quest state.
#>
[CmdletBinding()]
param(
    [string]$ManifestPath = (Join-Path ([IO.Path]::GetTempPath()) 'our-world-geonft-demo.json'),
    [string]$Web4BaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [string]$Web5BaseUrl = 'https://dev.api.starnet.oasisomniverse.one',
    [string]$CredentialPath = (Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geonft-seed.credential.clixml'),
    [PSCredential]$Credential
)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$ids = @($manifest.placements | ForEach-Object { ([Guid]$_.id).ToString() } | Select-Object -Unique)
if ($ids.Count -ne 5) { throw 'Expected five unique Anorak manifest placements.' }
if ($null -eq $Credential) { $Credential = Import-Clixml -LiteralPath $CredentialPath }

function Unwrap($response) {
    if ($null -eq $response.PSObject.Properties['isError']) { $response = $response.result }
    if ($null -eq $response -or $null -eq $response.PSObject.Properties['isError'] -or $response.isError) {
        throw "API operation failed: $($response.message)"
    }
    return $response.result
}

$loginBody = @{ username=$Credential.UserName; password=$Credential.GetNetworkCredential().Password } | ConvertTo-Json
try { $avatar = Unwrap (Invoke-RestMethod -Uri "$($Web4BaseUrl.TrimEnd('/'))/api/avatar/authenticate" -Method Post -ContentType 'application/json' -Body $loginBody) }
finally { $loginBody = $null }
$headers = @{ Authorization = "Bearer $($avatar.jwtToken)" }
function Invoke-OasisApi($base, $path, $method='Get', $body=$null) {
    $args = @{ Uri="$($base.TrimEnd('/'))/api/$path"; Method=$method; Headers=$headers; TimeoutSec=90 }
    if ($null -ne $body) { $args.ContentType='application/json'; $args.Body=$body | ConvertTo-Json -Depth 30 }
    Unwrap (Invoke-RestMethod @args)
}

try {
    $geoNFTs = @(Invoke-OasisApi $Web4BaseUrl 'nft/load-all-geo-nfts/MongoDBOASIS/false')
    $inventory = @(Invoke-OasisApi $Web4BaseUrl 'avatar/inventory')
    foreach ($id in $ids) {
        if (@($geoNFTs | Where-Object { [string]$_.id -eq $id }).Count -ne 1) {
            throw "GeoNFT placement $id does not exist exactly once."
        }
        $items = @($inventory | Where-Object { [string]$_.geoNFTId -eq $id })
        if ($items.Count -ne 1) { throw "Expected one inventory row for GeoNFT $id; found $($items.Count)." }
        $item = $items[0]
        if ([string]$item.itemType -ne 'Nature') { throw "GeoNFT $id has category '$($item.itemType)' instead of Nature." }
        if (-not [string]::IsNullOrWhiteSpace([string]$item.nftId) -and [string]$item.nftId -ne [Guid]::Empty.ToString()) {
            throw "GeoNFT $id incorrectly also contains NFTId '$($item.nftId)'."
        }
    }

    $quests = @(Invoke-OasisApi $Web5BaseUrl 'quests/all-for-avatar/game')
    $anorak = @($quests | Where-Object startupSequence -eq 'anorak-trees')
    if ($anorak.Count -ne 1) { throw "Expected one Anorak startup quest; found $($anorak.Count)." }
    $quest = Invoke-OasisApi $Web5BaseUrl "quests/$($anorak[0].id)"
    $actual = @($quest.objectives | ForEach-Object { $_.needToCollectItems.'Our World' } | Sort-Object)
    $expected = @($ids | ForEach-Object { "geonft:$_" } | Sort-Object)
    if (@(Compare-Object $actual $expected).Count -gt 0) { throw 'Anorak objectives do not match the placement manifest.' }
    if (@($quest.objectives[0].crossGameEventsOnActivate).Count -lt 1) { throw 'Anorak intro events are not authored on the first objective.' }
    foreach ($objective in @($quest.objectives)) {
        if (-not (@($objective.crossGameEventsOnComplete) | Where-Object eventType -eq 'PlayAnimation')) {
            throw "Objective '$($objective.title)' has no completion presentation event."
        }
    }
    $reconciled = Invoke-OasisApi $Web5BaseUrl "quests/$($quest.id)/inventory-progress" 'Post' @{}
    $completed = @($reconciled.quest.objectives | Where-Object isCompleted).Count
    if ($completed -ne $ids.Count) { throw "Expected $($ids.Count) completed objectives; found $completed." }
    if ([string]$reconciled.quest.status -notin @('2', 'Completed')) { throw "Quest status is '$($reconciled.quest.status)' instead of Completed." }
    Write-Host "Verified $($ids.Count) canonical Nature GeoNFT items and completed Anorak quest $($quest.id)." -ForegroundColor Green
} finally {
    $headers.Clear(); $avatar=$null; $Credential=$null
}
