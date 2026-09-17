# Fresh Anorak test run

Resetting quest progress alone is insufficient: inventory reconciliation completes
the objectives again while the avatar still owns the matching trees.

Stop Unity Play mode, then from the OASIS repository root:

```powershell
# Inspect without changing anything.
./Scripts/reset_our_world_tree_progress.ps1
# Apply, back up and verify the targeted reset.
./Scripts/reset_our_world_tree_progress.ps1 -Apply
```

The script authenticates using the existing encrypted development seed credential.
It discovers Anorak's four/five required GeoNFTs, includes only tagged demo duplicates
at those coordinates, removes their inventory rows and collection-history entries,
and resets that quest. It does not reset the database, avatar, karma, other quests
or other inventory. It verifies unrelated inventory is unchanged and calls inventory
reconciliation to prove progress remains zero.

Backups are under `%LOCALAPPDATA%/OASIS/AnorakResetBackups`. They contain scoped test
state, not tokens. Partial failure stops with an error; inspect it before rerunning.
This is a multi-request developer maintenance command, not an atomic game operation.
Other clients must not collect during the reset.

Use `-Web4BaseUrl` and `-Web5BaseUrl` for a local node; defaults are hosted dev APIs.
The reset does not rewrite placement rules. The GeoNFT seed sets and updates reused
placements to permanent=false, sharing=true, global=0, player=1, cooldown=60 seconds.

## WEB4 routes required by the collection-rules client

Both require the signed-in avatar's bearer token:

- `POST /api/nft/geo-nft-collection-status`: JSON array of up to 500 placement GUIDs.
  Returns `OASISResult<List<GeoNFTCollectionStatus>>`: canCollect, reason,
  nextCollectAtUtc, playerCollectionCount and globalCollectionCount.
- `PUT /api/nft/geo-nft/{id}`: creator-only metadata/rule update. Send permSpawn,
  allowOtherPlayersToAlsoCollect, globalSpawnQuantity, playerSpawnQuantity and
  respawnDurationInSeconds. Omit coordinates when changing rules. Check isError
  and the returned result, not HTTP status alone.

Deploy these before running the matching Unity client. A 404 is a backend/client
version mismatch, not a reason to add a local eligibility fallback.

See [the test matrix](GEONFT_COLLECTION_TEST_MATRIX.md) and
[dependency deployment policy](RAILWAY_DEPENDENCY_PINS.md). Startup reconciliation
updates quest state silently; pickup celebrations belong to confirmed gameplay.
