# Anorak tree quest: inventory and WEB5 progress

## Contract

The temporary demo uses the four GeoNFT placement IDs in `our-world-geonft-demo.json`. The seed also supports a five-placement manifest. Each objective has one `NeedToCollectItems["Our World"]` token, `geonft:{placement-guid}`. Names are display text, not collection identity.

WEB4 `POST /api/nft/collect-geo-nft` must call `NFTManager.CollectGeoNFTAsync` and persist the placement as `GeoNFTId` for the authenticated avatar. Previously it called the regular NFT collector and saved that ID as `NftId`, losing its display metadata. The fixed collector loads title, description and image from the placement. Our World's tree collection submits category `Nature`. Inventory serializers emit category strings. Unity normalizes empty GUIDs and reads the canonical `Image2DURI` thumbnail field.

WEB5 `POST /api/quests/{id}/inventory-progress` reads the avatar's saved inventory and passes matching placement tokens to the existing `ApplyQuestProgressAsync` engine. ODOOM/OQuake use this same engine through OGEngineClient's `/progress` requests. Only incomplete objectives participate. One placement per objective permits replay/recovery without recounting a placement. This endpoint operates on avatar-owned quest instances and serializes reconciliation per avatar/quest within the API process; multi-instance transactional concurrency remains a backend concern.

Unity reconciles at startup and after successful collection, refreshes the quest list, and displays completed/total objectives plus the active or next incomplete objective. Completion comes from WEB5, not a local counter. Every newly completed GeoNFT objective starts the retained KashifFinal chest, blue pickup particles and congratulations audio with an `OBJECTIVE COMPLETE` message and objective title. When the last objective also completes its quest, that objective presentation is followed by the final blue celebration with `QUEST COMPLETE`, `CONGRATULATIONS`, and the quest title. This is based on API state transitions for any matching quest, not the Anorak name or a local four-tree count. Neither animation opens the obsolete inventory popup.

Startup eligibility and instructions come from the API quest with `MetaData["OurWorld.StartupSequence"] = "anorak-trees"`, exposed in the game quest DTO. The existing Anorak scene/audio sequence remains the presentation layer; a general-purpose API media/event player is still a separate step. The four generic demo placements represent the trees for now, as agreed; their current source images remain demo artwork.

## Deployment and seeding

1. Deploy WEB4 collector/category fixes and WEB5 reconciliation/DTO changes.
2. Run the existing GeoNFT seed if a placement manifest does not exist.
3. Run `Scripts/seed_our_world_tree_quest.bat`, or its PowerShell script with `-ManifestPath`, `-Web4BaseUrl`, `-Web5BaseUrl`, `-CredentialPath` or `-Credential`. Defaults are the hosted dev APIs. Credentials use the existing DPAPI file and are not printed.
4. `-PlanOnly` prints the four/five objective quest without authentication or writes. Live seeding checks Swagger for the new endpoint before changing data.
5. The script reuses the matching Anorak quest and repairs only inventory rows whose incorrect `NftId` matches the manifest. It consolidates historical duplicate rows only for the same manifest `GeoNFTId`, retaining the earliest acquisition, and verifies that no duplicate remains. The deployed AvatarManager token-identity guard prevents repeated collection from adding another row. It then reconciles already-collected items. Conflicting objective sets produce an error rather than overwriting progress.

Hosted development WEB4 and WEB5 expose the collector and `inventory-progress` contracts. On 17 September 2026 the live repair reused quest `e7528f70-8452-42d6-be63-ddee006c746c`, verified four completed objectives, and proved one canonical `Nature` inventory item for each manifest GeoNFT. Quest detail and inventory reconciliation both returned HTTP 200. `QuestEffectsRuntimeTests.DynamicObjectiveAndQuestEffectsRunInOrder` also passed in an isolated Unity 2022.3.62f3 Play Mode run (1/1, 19.77 seconds), executing the real objective chest, both audio calls, objective completion, quest chest, final canvas, both final audio calls and quest completion in order. A human visual pass remains appropriate after changing art, timing or audio assets.

## OGEngineClient consolidation

Inspected `OGames/ODOOM/ogengine_sync.c`, `OGames/ODOOM/uzdoom_ogengine_integration.cpp`, `OGames/OQuake/Code/oquake_ogengine_integration.c`, and `OGEngineClient.Inventory.Progress.cs`. These share inventory operations, progress submission, cache refresh and active-objective tracking through OGEngineClient.

The consolidation is implemented as a portable .NET Standard 2.1 project in `OGEngineClient/Shared`. It owns endpoint defaults, optional NFT/GeoNFT identity normalization, and authoritative objective/quest transition ordering. Native OGEngineClient references it directly. Our World's Unity adapter copies the same reviewed source with `Scripts/sync_ogengine_shared.ps1` and verifies the SHA-256 hash; Unity continues to own `UnityWebRequest`, coroutines, main-thread UI, location sensors, scenes, audio and particles.

This keeps transport and engine concerns separate without duplicating quest rules. The native adapter remains the C ABI used by ODOOM/OQuake, while Unity maps the same transition results to its retained chest and celebration effects. The previous independent GeoNFT quest counter remains disabled.

## Acceptance

- Four/five objectives match exact placement IDs in reverse collection order; completed objectives are skipped.
- Both serializers emit `Nature` and preserve `GeoNFTId` separately from empty `NftId`.
- With deployed APIs, collect each placement, check saved inventory and quest progress, then restart to verify persistence.
- Check the Nature tab, tracker and quest popup after each collection; final completion should trigger congratulations.
- Check chest effects, text clarity and popup stacking in Unity Play Mode. Compilation is not visual verification.
