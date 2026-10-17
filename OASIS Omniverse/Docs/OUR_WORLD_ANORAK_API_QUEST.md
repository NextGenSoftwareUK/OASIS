# Anorak tree quest: inventory and WEB5 progress

## Contract

The temporary demo uses the four GeoNFT placement IDs in `our-world-geonft-demo.json`. The seed also supports a five-placement manifest. Each objective has one `NeedToCollectItems["Our World"]` token, `geonft:{placement-guid}`. Names are display text, not collection identity.

The seeded Anorak quest sets `objectiveCompletionOrder` to `AnyOrder`. All four eligible portals may coexist and collecting a portal completes the objective whose placement GUID it owns, regardless of the currently displayed tracker objective. The tracker can cycle among incomplete objectives and persists that navigation choice through WEB4 `POST /api/Avatar/set-active-quest`; this selection does not constrain WEB5 progress. An any-order pickup plays only its completion effects and must not replay Anorak's introduction.

For a future `InOrder` quest, Our World hides tracker cycling, renders only the first incomplete objective's portal, rejects progress for later objectives at WEB5, and refreshes portals after completion so the next objective appears.

WEB4 `POST /api/nft/collect-geo-nft` must call `NFTManager.CollectGeoNFTAsync` and persist the placement as `GeoNFTId` for the authenticated avatar. Previously it called the regular NFT collector and saved that ID as `NftId`, losing its display metadata. The fixed collector loads title, description and image from the placement. Our World's tree collection submits category `Nature`. Inventory serializers emit category strings. Unity normalizes empty GUIDs and reads the canonical `Image2DURI` thumbnail field.

WEB5 `POST /api/quests/{id}/inventory-progress` reads the avatar's saved inventory and passes matching placement tokens to the existing `ApplyQuestProgressAsync` engine. Its result contains the refreshed authoritative `Quest`, `CompletedObjectives`, `QuestCompleted`, rewards, and `CrossGameEventsToDispatch`. ODOOM/OQuake use this same engine through OGEngineClient's `/progress` requests. Only incomplete objectives participate. One placement per objective permits replay/recovery without recounting a placement. This endpoint operates on avatar-owned quest instances and serializes reconciliation per avatar/quest within the API process; multi-instance transactional concurrency remains a backend concern.

Unity reconciles at startup and after successful collection, refreshes the quest list, and displays completed/total objectives plus the active or next incomplete objective. Completion comes from WEB5, not a local counter. Unity plays the canonical `CrossGameEventsToDispatch` returned by the same progress transaction. A `PlayAnimation` event with `objective-complete` starts the retained KashifFinal chest, blue pickup particles, congratulations audio, `OBJECTIVE COMPLETE`, and the objective title. A final `quest-complete` event follows it with the blue celebration, `QUEST COMPLETE`, `CONGRATULATIONS`, and the quest title. Neither animation opens the obsolete inventory popup.

Startup presentation comes from the first incomplete API quest's first-objective `CrossGameEventsOnActivate`; it no longer depends on an Anorak-specific startup metadata key. `ShowNarration`, `ShowImage`, `PlayAudio`, `PlayVideo`, `PlayAnimation`, and `OpenWebsite` are interpreted by the shared Our World event player. The seed authors Anorak's welcome narration, image/audio keys, and objective/quest completion animation events through this standard contract. The four generic demo placements represent the trees for now, as agreed; their current source images remain demo artwork.

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
- Any-order collection succeeds in reverse order and the tracker can cycle among remaining objectives.
- In-order collection cannot advance a later objective and only the current objective portal is visible.
- An objective pickup never replays the quest introduction; only an ordered transition may play the next objective's activation events.
- Both serializers emit `Nature` and preserve `GeoNFTId` separately from empty `NftId`.
- With deployed APIs, collect each placement, check saved inventory and quest progress, then restart to verify persistence.
- Check the Nature tab, tracker and quest popup after each collection; final completion should trigger congratulations.
- Check chest effects, text clarity and popup stacking in Unity Play Mode. Compilation is not visual verification.
