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

To clear every seeded Our World test item and reset the Anorak, GeoNFT matrix and
GeoHotSpot matrix fixtures, use the broader command:

```powershell
./Scripts/reset_our_world_test_progress.ps1          # inspect
./Scripts/reset_our_world_test_progress.ps1 -Apply   # back up, reset and verify
```

The script authenticates using the existing encrypted development seed credential.
It discovers Anorak's five required GeoNFTs, includes only tagged demo duplicates
at those coordinates, removes their inventory rows and collection-history entries,
and resets that quest. It does not reset the database, avatar, karma, other quests
or other inventory. It verifies unrelated inventory is unchanged and calls inventory
reconciliation to prove progress remains zero.

For the GeoHotSpot matrix, the same command also removes granted inventory rewards,
resets linked quest progress, and removes `GeoHotSpotTriggerStateV1` from the 12
hotspots listed in `%LOCALAPPDATA%\OASIS\our-world-geohotspot-quest-matrix.json`.
It preserves unrelated hotspots and inventory.

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

## Automated Anorak API acceptance

With Unity stopped, run:

```powershell
./Scripts/test_our_world_tree_collection.ps1 -Apply
```

This explicitly collects all required demo trees, tests one-time eligibility,
inventory category/quantity, one-objective-per-pickup progress, objective/quest
events and no event replay on an unchanged sync. It updates tagged demo duplicates
at the quest coordinates to the once-per-player defaults. Its final cleanup invokes
the targeted reset so a successful test leaves a fresh manual playthrough.

### Verified 2026-09-17 (development)

- Parent deployment: `80f2d2b769ac37eb2f7b9b5b012c1554887ead64`.
- All seven WEB4-WEB10 projects published locally successfully.
- Manifest/gitlink equality and all eight Development branch pointers passed.
- Authenticated eligibility route works; the previous 404 is resolved.
- All 12 tagged demo placements returned once-per-player settings after updating.
- The original four sequential pickups passed; all four duplicate pickups were rejected.
- Nature inventory quantity remained one per pickup despite duplicate requests.
- Quest progress advanced 1/4, 2/4, 3/4, 4/4, one objective event each, with one
  quest-complete event on the last pickup. Repeated sync returned no events.
- Cleanup removed four test inventory rows and four durable history entries.
  Reconciliation then remained 0/4; all 20 unrelated inventory rows were unchanged.
- On 2026-09-18 the quest was migrated in place to five canonical named trees
  (Rainbow, Lightning, Mycelium, Fruit and Skeleton) and reset to 0/5. The old
  four-placement set was retained but made ineligible (`PlayerSpawnQuantity=0`).

These are API results, not manual verification of chest rendering, audio, portal
placement or UI. Full rule combinations, multiplayer and cross-replica persistence
remain separate tests in the matrix. GitHub Actions also has private-submodule
checkout/restore failures; successful local publish is not a claim that CI is green.
