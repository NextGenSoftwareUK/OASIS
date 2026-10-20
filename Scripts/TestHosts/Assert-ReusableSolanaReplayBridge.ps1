$ErrorActionPreference='Stop'

function Assert-SolanaReplayChain {
    param([long]$SnapshotSlot,[long]$RootSlot,[hashtable]$Records)
    if($SnapshotSlot -lt 0 -or $RootSlot -lt $SnapshotSlot){throw 'Invalid retained snapshot/root bounds.'}
    $visited=[Collections.Generic.HashSet[long]]::new();$slot=$RootSlot
    while($slot -ne $SnapshotSlot){
        if(!$visited.Add($slot)){throw "Retained replay ancestry cycles at slot $slot."}
        if(!$Records.ContainsKey($slot)){throw "Retained replay ancestry has no payload metadata at slot $slot."}
        $row=$Records[$slot]
        if(!$row.Full -or $null -eq $row.Parent){throw "Retained replay ancestry is incomplete at slot $slot; restore original evidence, do not reset the tower."}
        $parent=[long]$row.Parent
        if($parent -lt $SnapshotSlot -or $parent -ge $slot){throw "Retained replay ancestry has an invalid parent at slot $slot."}
        $slot=$parent
    }
}

$tool='/var/tmp/oasis-runtimes/agave-4.3.0/agave-ledger-tool'
$ledger='/var/tmp/oasis-solana/ledger'
$version=& wsl -d Ubuntu -- $tool --version
if($LASTEXITCODE -ne 0 -or "$version".Trim() -ne 'agave-ledger-tool 4.3.0 (src:825efd18; feat:c9ad34d2, client:Agave)'){throw 'Existing pinned offline ledger tool required; no installation or fallback.'}
$bounds=& wsl -d Ubuntu -- $tool --ledger $ledger blockstore bounds 2>&1
if($LASTEXITCODE -ne 0){throw 'Read-only retained ledger bounds inspection failed.'}
$rootLines=@($bounds | Where-Object {"$_" -match '^\s*with \d+ rooted slots from \d+ to (\d+)\s*$'})
if($rootLines.Count -ne 1 -or "$($rootLines[0])" -notmatch 'to (\d+)\s*$'){throw 'Pinned native tool omitted unambiguous rooted bounds.'}
$rootSlot=[long]$Matches[1]
$snapshots=@(Get-ChildItem -LiteralPath '\\wsl.localhost\Ubuntu\var\tmp\oasis-solana\ledger' -Filter 'snapshot-*.tar.zst' -File | ForEach-Object {
    if($_.Name -match '^snapshot-(\d+)-[^.]+\.tar\.zst$' -and [long]$Matches[1] -le $rootSlot){[long]$Matches[1]}
})
if(!$snapshots.Count){throw 'Retained full snapshot archive required; no new ledger or reset.'}
$snapshotSlot=[long]($snapshots | Sort-Object -Descending | Select-Object -First 1)
if($rootSlot-$snapshotSlot -gt 100000){throw 'Replay inspection exceeds its 100000-slot safety bound; inspect offline before startup.'}
$lines=& wsl -d Ubuntu -- $tool --ledger $ledger blockstore print --starting-slot $snapshotSlot --ending-slot $rootSlot --num-slots ($rootSlot-$snapshotSlot+1) 2>&1
if($LASTEXITCODE -ne 0){throw 'Read-only retained replay metadata inspection failed.'}
$records=@{};$current=$null
foreach($line in $lines){
    $value="$line"
    if($value -match '^Slot (\d+)(?: \(root\))?$'){$current=[long]$Matches[1]}
    if($null -ne $current -and $value -match '^\s+num_shreds: \d+, parent_slot: (None|Some\((\d+)\)), next_slots: \[.*\], num_entries: \d+, is_full: (true|false)$'){
        if($records.ContainsKey($current)){throw 'Duplicate retained slot metadata.'}
        $records[$current]=[pscustomobject]@{Parent=if($Matches[1] -eq 'None'){$null}else{[long]$Matches[2]};Full=$Matches[3] -eq 'true'}
    }
}
Assert-SolanaReplayChain -SnapshotSlot $snapshotSlot -RootSlot $rootSlot -Records $records
[pscustomobject]@{SnapshotSlot=$snapshotSlot;RootSlot=$rootSlot;ReplayMetadataAdmitted=$true}
