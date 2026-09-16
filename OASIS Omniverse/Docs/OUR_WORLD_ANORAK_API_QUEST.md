# Anorak tree quest: inventory and WEB5 progress

## Contract

The temporary demo uses the four GeoNFT placement IDs in `our-world-geonft-demo.json`. The seed also supports a five-placement manifest. Each objective has one `NeedToCollectItems["Our World"]` token, `geonft:{placement-guid}`. Names are display text, not collection identity.

WEB4 `POST /api/nft/collect-geo-nft` must call `NFTManager.CollectGeoNFTAsync` and persist the placement as `GeoNFTId` for the authenticated avatar. Previously it called the regular NFT collector and saved that ID as `NftId`, losing its display metadata. The fixed collector loads title, description and image from the placement. Our World's tree collection submits category `Nature`. Inventory serializers emit category strings. Unity normalizes empty GUIDs and reads the canonical `Image2DURI` thumbnail field.

WEB5 `POST /api/quests/{id}/inventory-progress` reads the avatar's saved inventory and passes matching placement tokens to the existing `ApplyQuestProgressAsync` engine. ODOOM/OQuake use this same engine through OGEngineClient's `/progress` requests. Only incomplete objectives participate. One placement per objective permits replay/recovery without recounting a placement. This endpoint operates on avatar-owned quest instances and serializes reconciliation per avatar/quest within the API process; multi-instance transactional concurrency remains a backend concern.

Unity reconciles at startup and after successful collection, refreshes the quest list, and displays completed/total objectives plus the active or next incomplete objective. Completion comes from WEB5, not a local counter. Every newly completed GeoNFT objective starts the retained KashifFinal chest, blue pickup particles and congratulations audio with an `OBJECTIVE COMPLETE` message and objective title. The last objective instead uses the final chest and blue celebration with `QUEST COMPLETE`, `CONGRATULATIONS`, and the quest title. This is based on API state transitions for any matching quest, not the Anorak name or a local four-tree count. Neither animation opens the obsolete inventory popup.

Startup eligibility and instructions come from the API quest with `MetaData["OurWorld.StartupSequence"] = "anorak-trees"`, exposed in the game quest DTO. The existing Anorak scene/audio sequence remains the presentation layer; a general-purpose API media/event player is still a separate step. The four generic demo placements represent the trees for now, as agreed; their current source images remain demo artwork.

## Deployment and seeding

1. Deploy WEB4 collector/category fixes and WEB5 reconciliation/DTO changes.
2. Run the existing GeoNFT seed if a placement manifest does not exist.
3. Run `Scripts/seed_our_world_tree_quest.bat`, or its PowerShell script with `-ManifestPath`, `-Web4BaseUrl`, `-Web5BaseUrl`, `-CredentialPath` or `-Credential`. Defaults are the hosted dev APIs. Credentials use the existing DPAPI file and are not printed.
4. `-PlanOnly` prints the four/five objective quest without authentication or writes. Live seeding checks Swagger for the new endpoint before changing data.
5. The script reuses the matching Anorak quest and repairs only inventory rows whose incorrect `NftId` matches the manifest. It then reconciles already-collected items. Conflicting objective sets produce an error rather than overwriting progress.

At implementation time hosted WEB5 Swagger did **not** expose `inventory-progress`. Local builds/tests do not imply deployment. Live seeding and in-game acceptance remain pending deployment.

## OGEngineClient consolidation

Inspected `OGames/ODOOM/ogengine_sync.c`, `OGames/ODOOM/uzdoom_ogengine_integration.cpp`, `OGames/OQuake/Code/oquake_ogengine_integration.c`, and `OGEngineClient.Inventory.Progress.cs`. These share inventory operations, progress submission, cache refresh and active-objective tracking through OGEngineClient.

Recommended architecture: extract a portable managed OGEngineClient core and add a Unity adapter. The core owns authentication, inventory identity/stacking, quest operations, cache invalidation and event delivery. Unity owns location sensors, main-thread dispatch, scenes, UI and effects. Native exports remain an adapter used by ODOOM/OQuake. Migrate feature by feature, disabling each old Unity transport as it moves; do not run two collection/progress implementations together.

Current OGEngineClient targets .NET 10/NativeAOT; Our World uses Unity 2022.3.62f3. Its managed assembly is not directly compatible. A portable core needs Unity-compatible dependencies and platform APIs. Native exports are an alternative to investigate for desktop, requiring platform binaries and mobile/IL2CPP validation. No OGEngineClient Unity migration is implemented by this change.

## Acceptance

- Four/five objectives match exact placement IDs in reverse collection order; completed objectives are skipped.
- Both serializers emit `Nature` and preserve `GeoNFTId` separately from empty `NftId`.
- With deployed APIs, collect each placement, check saved inventory and quest progress, then restart to verify persistence.
- Check the Nature tab, tracker and quest popup after each collection; final completion should trigger congratulations.
- Check chest effects, text clarity and popup stacking in Unity Play Mode. Compilation is not visual verification.
