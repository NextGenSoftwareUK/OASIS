# GeoHotSpot Quest Integration — Specification and Implementation Checklist

**Status:** Runtime integration implemented; live/manual and distributed-transaction verification remains  
**Owners:** WEB5 STAR API and Our World  
**Last updated:** 2026-09-19

This document is the source of truth for integrating GeoHotSpots with STAR quests and Our World. Check an item only after its implementation and relevant automated or manual verification have completed. Add evidence beside completed items where practical (test name, commit, endpoint response, or screenshot).

For the complete Web3 → WEB4 → WEB5 NFT/GeoNFT wrapper model, typed collections, Smartbrick role, and the decision that Our World supports both WEB4 and WEB5 GeoNFT sources, see [NFT and GeoNFT architecture across Web3, WEB4, and WEB5](NFT_GEONFT_WEB4_WEB5_ARCHITECTURE.md).

## Core invariant

GeoNFTs and GeoHotSpots remain distinct public domain types and distinct APIs. A GeoNFT is the streamlined, NFT-focused experience for placing and collecting a token geographically. A GeoHotSpot is the general-purpose/power-user experience with arrival, dwell, AR gaze/touch, media, links, inventory rewards, GeoNFT rewards, quest actions, and cross-game events.

Both types use one shared geospatial placement and spawn-policy engine underneath their public APIs. Sharing the engine must not collapse `/api/geonfts` into `/api/geohotspots`, remove either authored model, or force ordinary GeoNFT creators through the advanced GeoHotSpot workflow. A GeoNFT can also be a reward emitted by a GeoHotSpot, but that relationship does not make every independently authored GeoNFT a GeoHotSpot record.

WEB5 is authoritative for trigger acceptance, progress, reward grants, repeatability, and idempotency. Our World detects and presents an attempted interaction, but must not mark a hotspot triggered, grant a reward, or complete a quest objective until WEB5 accepts the trigger.

## Supported trigger rules

- `WhenArrivedAtGeoLocation`: trigger on entering the authored radius.
- `WhenAtGeoLocationForXSeconds`: remain continuously within the authored radius for the authored duration; leaving resets dwell progress.
- `WhenLookingAtObjectOrImageForXSecondsInARMode`: continuously look at the hotspot's 2D/3D representation for the authored duration.
- `WhenObjectOrImageIsTouchedInARMode`: touch the AR representation or tap its map representation.

## Supported content and result types

Existing GeoHotSpot content:

- `Map`
- `AR`
- `VR`
- `IR`
- `Audio` using embedded data or a URL
- `Video` using embedded data or a URL
- `Text`
- `WebsiteLink`

Trigger results to support through one typed result/action contract:

- Grant one or more inventory items.
- Grant or collect one or more GeoNFTs.
- Update linked quest/objective GeoHotSpot progress.
- Complete an objective when all of its authored requirements are satisfied.
- Dispatch authored cross-game events: `SpawnEntity`, `UnlockPortal`, `ShowNarration`, `TeleportTo`, `PlayAudio`, `PlayVideo`, `OpenWebsite`, `ShowImage`, and `PlayAnimation`.
- Return refreshed quest, objective, inventory, and GeoNFT state needed by the client.

## Unified spawn and collection policy

GeoHotSpots must support the same policy dimensions already authored for GeoNFTs. The implementation must preserve the documented GeoNFT precedence:

- `AllowOtherPlayersToAlsoCollect` controls whether collection is exclusive to the first/owning player or remains available to other avatars.
- `PermSpawn` means unlimited collection subject to its respawn/cooldown policy.
- When `PermSpawn` is false and `GlobalSpawnQuantity` is non-zero, the global limit takes precedence.
- When `PermSpawn` is false and `GlobalSpawnQuantity` is zero, `PlayerSpawnQuantity` is the per-player limit.
- A quantity of `-1` means unlimited wherever the selected global or per-player quantity applies.
- A zero respawn delay means immediate eligibility when the selected policy permits another trigger.
- A positive respawn delay hides or disables the hotspot until the authoritative next-eligible time.
- Global and per-player counts are enforced atomically by WEB5.
- Repeated or concurrent trigger submissions are idempotent and cannot grant duplicate rewards.
- `SpawnInSafeZone` controls whether the hotspot/payload may be placed in a safe zone.
- `SpawnNearPlayer` selects authored coordinates or player-relative placement.
- `SpawnWithinXMetersFromPlayer` bounds player-relative placement.
- `SpawnXMetersAwayFromPlayer` supplies the authored player-relative distance when applicable.
- `IsVisibleOnMap` controls map-marker visibility without bypassing server eligibility.

These fields are the complete additional GeoNFT spawn properties found in the current canonical `IWeb4GeoSpatialNFT`/`Web4OASISGeoSpatialNFT` model. They must be represented in the use-case matrix and tested before this section is marked complete.

## Quest behavior

- A quest and an individual objective may reference a GeoHotSpot through `LinkedGeoHotSpotId`.
- `NeedToGoToGeoHotSpots` defines required hotspot progress; `GeoHotSpotsArrived` records accepted progress.
- For `AnyOrder` quests, Our World displays every currently eligible hotspot belonging to incomplete objectives, and the HUD can cycle the active objective.
- For `InOrder` quests, Our World displays only the current incomplete objective's hotspot.
- A hotspot trigger may satisfy only one requirement or may complete an objective, depending on the remaining authored requirements.
- Objective-complete presentation runs only when an objective actually transitions to complete.
- Quest-complete presentation runs only when the quest actually transitions to complete.
- Triggering a hotspot must not replay the quest introduction.

## Authoritative trigger transaction

The WEB5 trigger operation must perform the following as one logical transaction:

1. Authenticate the avatar.
2. Load the canonical GeoHotSpot and linked quest/objective state.
3. Validate that the hotspot is active and currently eligible for the avatar.
4. Validate the requested trigger type and required evidence, including location/radius where applicable.
5. Enforce the unified spawn, quantity, cooldown, and respawn policy.
6. Record an idempotent trigger/visit history entry.
7. Update `GeoHotSpotsArrived` and recompute objective and quest completion.
8. Grant inventory and GeoNFT rewards exactly once.
9. Collect the applicable cross-game events.
10. Persist all resulting state before returning success.
11. Return a typed result containing the accepted trigger, next eligibility, grants, completion transitions, events, and refreshed state.

Failures must return an `OASISResult<T>` error and must not leave partial grants or progress.

## API contract work

- [x] Define the canonical GeoHotSpot trigger request DTO, including trigger evidence and an idempotency key. (`TriggerGeoHotSpotRequest`)
- [x] Define the canonical trigger result DTO. (`TriggerGeoHotSpotResult`)
- [x] Add an explicit typed GeoNFT reward/action representation. (`GeoHotSpot.GeoNFTRewardIds`)
- [x] Move or share all applicable GeoNFT spawn-policy fields with GeoHotSpots without duplicating rule evaluation. (`GeoSpatialSpawnPolicy`; 1,536-combination regression passes)
- [x] Implement the WEB5 trigger endpoint at one documented route. (`POST /api/geohotspots/{id}/trigger`)
- [x] Implement server-side trigger, eligibility, radius, dwell/gaze/touch evidence, spawn-policy, and idempotency validation.
- [x] Implement persistent trigger/activity history. (`GeoHotSpotTriggerStateV1` metadata, persisted with the hotspot)
- [x] Update linked quest/objective progress through the shared `QuestManager` progress engine.
- [x] Grant inventory rewards through the canonical game inventory manager.
- [x] Grant/collect GeoNFT rewards through `NFTManager.CollectGeoNFTAsync`.
- [x] Return cross-game events and refreshed quest state.
- [ ] Expose GeoHotSpot rewards and trigger events in quest create/update DTOs.
- [x] Remove or replace stale documented routes and client calls rather than retaining fallback routes.
- [ ] Add API unit/integration tests for the full combination matrix.

## Our World work

- [x] Replace the disconnected GeoHotSpot scaffold with the canonical WEB5 contract.
- [ ] Load only active, relevant, eligible quest-linked GeoHotSpots plus intentionally discoverable standalone hotspots.
- [ ] Parse the complete GeoHotSpot model, including content, assets, rewards, spawn policy, and eligibility.
- [x] Place GeoHotSpots through `UnifiedGeoDisplaySystem` and register them with the trigger manager.
- [ ] Give GeoHotSpots distinct visuals while allowing authored 2D/3D representations.
- [x] Implement immediate-arrival triggering.
- [x] Implement continuous dwell progress and reset-on-exit.
- [x] Implement AR gaze-duration triggering.
- [x] Implement AR touch triggering.
- [x] Submit canonical trigger evidence and wait for server acceptance.
- [x] Drive visibility and respawn countdown from server eligibility.
- [x] Refresh the shared inventory UI after accepted inventory/GeoNFT grants.
- [x] Refresh the quest list and HUD tracker after authoritative quest changes.
- [x] Dispatch returned supported presentation actions after completion effects, suppressing duplicate completion animations.
- [x] Prevent premature local trigger effects: `HasBeenTriggered`, object events, and presentation now change only after WEB5 accepts the evidence.
- [ ] Add creator UI fields for the complete trigger, payload, and spawn-policy model.
- [ ] Add clear validation and summaries to the creator UI.

## Shared-engine cutover

This phase happens only after the shared geospatial engine passes the automated and manual matrix. Both public APIs and both Our World content paths remain supported throughout and after the cutover.

- [ ] Inventory every legacy GeoNFT runtime path and classify it as shared/reused, migrated, or redundant.
- [ ] Reuse neutral GeoNFT components (assets, presentation, inventory mapping, and proven map anchoring) from the unified GeoHotSpot path where they still have one clear responsibility.
- [ ] Route the common parts of GeoNFT and GeoHotSpot eligibility, spawn policy, cooldown, respawn, and placement through the shared geospatial engine.
- [ ] Preserve the GeoNFT API, GeoHotSpot API, their specialised fields, and their separate user/creator workflows.
- [ ] Disable only genuinely duplicated internal rule evaluation or polling after both public paths are verified.
- [ ] Add a clear comment above every intentionally retained disabled internal block: `Superseded by the shared geospatial engine`, including the replacement class/method and migration date.
- [ ] Remove duplicate event subscriptions, polling loops, local rule evaluation, and API calls so only one runtime path owns each invariant.
- [ ] Confirm no scene or prefab still references disabled entry points.
- [ ] Run the existing GeoNFT regression suite again after cutover.

Disabled duplicate internal code is temporary migration evidence, not a permanent fallback. Once the shared implementation has remained verified and the duplicate block is no longer needed for review, remove it in a dedicated cleanup change. Public GeoNFT behavior is not legacy and must not be removed.

## Verification matrix

Automated and manual coverage must include at least:

- [ ] Each of the four trigger rules.
- [ ] Enter, exit, and re-enter during dwell timing.
- [ ] Boundary positions just inside, exactly on, and just outside the radius.
- [ ] Any-order and in-order quest objectives.
- [ ] Hotspot progress that does not complete an objective.
- [ ] Objective completion that does not complete a quest.
- [ ] Final objective and quest completion.
- [ ] Inventory-item reward.
- [ ] GeoNFT reward/collection.
- [ ] Multiple rewards in one accepted trigger.
- [ ] Every supported content type.
- [ ] Every supported cross-game event type.
- [ ] `PermSpawn` unlimited with zero and positive cooldowns.
- [ ] Global quantity precedence when non-zero.
- [ ] Per-player quantity when global quantity is zero.
- [ ] Immediate and delayed respawn.
- [ ] Two avatars competing for the final global allocation.
- [ ] Duplicate and concurrent submissions using the same idempotency key.
- [ ] Reconnect/reload while a cooldown is active.
- [ ] Inactive, exhausted, locked, unauthenticated, and malformed requests.
- [ ] No partial reward or quest update after a rejected/failed transaction.
- [ ] Map marker, HUD tracker, popup, media, inventory, and quest-list refresh behavior in Our World.

## Documentation and client artifacts

- [x] Update the WEB5 GeoHotSpots API reference with exact requests, responses, validation, and errors.
- [x] Update the WEB5 Quests API reference with GeoHotSpot linkage, progress, events, and rewards.
- [ ] Update the GeoNFT API reference to describe the unified policy ownership and GeoNFT reward behavior.
- [ ] Update Swagger/OpenAPI annotations and generated examples.
- [x] Update the applicable Postman collection under `C:\Source\OPORTAL-JS\postman`.
- [x] Update the STAR quest developer guide.
- [ ] Add an Our World creator and player-facing GeoHotSpot guide.
- [x] Publish the use-case/combination matrix with automated results and explicit manual/pending rows. (`GEOHOTSPOT_USE_CASE_MATRIX.md`)

## Current audited gaps

- The API documentation mentions `POST /api/geohotspots/{geoHotSpotId}/visit`, but the current WEB5 controller implements no visit, trigger, or activity route.
- Our World currently calls `/geohotspots/trigger/{id}`, which is not implemented by WEB5.
- `UnifiedGeoDisplaySystem.LoadGeoHotSpots()` is explicitly unimplemented.
- Our World's GeoHotSpot parser omits content, assets, rewards, spawn policy, and quest-trigger events.
- Our World marks a hotspot locally triggered before server acceptance.
- Reward and quest-progress handling in `GeoHotSpotTriggerManager` remains unfinished.
- The current model exposes inventory rewards, but no explicit GeoNFT reward/action contract.
- GeoHotSpot repeatability and respawn behavior are not yet governed by the complete GeoNFT spawn-policy contract.

## Completion record

Record each completed phase here with its commit, verification evidence, and any intentionally deferred work.

| Phase | Status | Commit/evidence | Notes |
|---|---|---|---|
| Specification | Complete | This document | Separate GeoNFT/GeoHotSpot APIs and workflows over a shared geospatial policy engine; GeoNFT is also a supported hotspot reward. |
| API contract | In progress | Unified policy test: PASS 1,536 combinations and 14 named boundaries | Shared policy and GeoNFT reward identity added; request/result DTOs remain. |
| WEB5 implementation | In progress | `66958bbbb`; focused suite 22/22 | Core progress/reward/result path implemented; durable cross-provider rollback still requires a transaction boundary. |
| Our World integration | In progress | `d0833993`, `4eefb457`; Unity build 0 errors | Accepted results, refresh, timed re-arm, and presentation dispatch wired; live manual matrix remains. |
| Shared-engine cutover | Not started |  | Preserve both public APIs; disable duplicate internals only after verification. |
| Automated matrix | In progress | 1,536 policy combinations; 22 focused WEB5 tests; builds 0 errors | Media/action integration coverage remains. |
| Manual matrix | Not started |  |  |
| Docs/Postman | In progress | API refs, quest guide, Postman `9145d5d`, matrix | Creator/player guide and GeoNFT cross-reference remain. |
