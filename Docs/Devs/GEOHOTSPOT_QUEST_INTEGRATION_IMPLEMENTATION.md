# GeoHotSpot Quest Integration — Specification and Implementation Checklist

**Status:** Design agreed; implementation not started  
**Owners:** WEB5 STAR API and Our World  
**Last updated:** 2026-09-19

This document is the source of truth for integrating GeoHotSpots with STAR quests and Our World. Check an item only after its implementation and relevant automated or manual verification have completed. Add evidence beside completed items where practical (test name, commit, endpoint response, or screenshot).

## Core invariant

A GeoHotSpot is the authoritative geographical trigger and spawn-policy container. Its payload describes the result of successfully triggering it. GeoNFTs are one supported GeoHotSpot payload/reward type; they must use the same trigger, eligibility, repeatability, cooldown, quantity, and respawn rules as other GeoHotSpot payloads rather than maintaining a parallel rule engine.

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

- `PermSpawn` means unlimited collection subject to its respawn/cooldown policy.
- When `PermSpawn` is false and `GlobalSpawnQuantity` is non-zero, the global limit takes precedence.
- When `PermSpawn` is false and `GlobalSpawnQuantity` is zero, `PlayerSpawnQuantity` is the per-player limit.
- A zero respawn delay means immediate eligibility when the selected policy permits another trigger.
- A positive respawn delay hides or disables the hotspot until the authoritative next-eligible time.
- Global and per-player counts are enforced atomically by WEB5.
- Repeated or concurrent trigger submissions are idempotent and cannot grant duplicate rewards.

The final model must also preserve all additional existing GeoNFT spawn properties discovered during implementation. They must be listed in the use-case matrix and tested before this section is marked complete.

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

- [ ] Define the canonical GeoHotSpot trigger request DTO, including trigger evidence and an idempotency key.
- [ ] Define the canonical trigger result DTO.
- [ ] Add an explicit typed GeoNFT reward/action representation.
- [ ] Move or share all applicable GeoNFT spawn-policy fields with GeoHotSpots without duplicating rule evaluation.
- [ ] Implement the WEB5 trigger endpoint at one documented route.
- [ ] Implement server-side trigger, eligibility, radius, dwell/gaze/touch evidence, spawn-policy, and idempotency validation.
- [ ] Implement persistent trigger/activity history.
- [ ] Atomically update linked quest/objective progress.
- [ ] Atomically grant inventory rewards.
- [ ] Atomically grant/collect GeoNFT rewards.
- [ ] Return cross-game events and refreshed state.
- [ ] Expose GeoHotSpot rewards and trigger events in quest create/update DTOs.
- [ ] Remove or replace stale documented routes and client calls rather than retaining fallback routes.
- [ ] Add API unit/integration tests for the full combination matrix.

## Our World work

- [ ] Replace the disconnected GeoHotSpot scaffold with the canonical WEB5 contract.
- [ ] Load only active, relevant, eligible quest-linked GeoHotSpots plus intentionally discoverable standalone hotspots.
- [ ] Parse the complete GeoHotSpot model, including content, assets, rewards, spawn policy, and eligibility.
- [ ] Place GeoHotSpots on the map using the proven GeoNFT geographical anchoring pipeline.
- [ ] Give GeoHotSpots distinct visuals while allowing authored 2D/3D representations.
- [ ] Implement immediate-arrival triggering.
- [ ] Implement continuous dwell progress and reset-on-exit.
- [ ] Implement AR gaze-duration triggering.
- [ ] Implement AR touch and map-tap triggering.
- [ ] Submit canonical trigger evidence and wait for server acceptance.
- [ ] Drive visibility and respawn countdown from server eligibility.
- [ ] Apply returned inventory and GeoNFT state to the shared inventory UI.
- [ ] Apply returned quest/objective state to the quest list and HUD tracker.
- [ ] Dispatch returned content and cross-game presentation actions in order.
- [ ] Prevent duplicate popups, audio overlap, intro replay, and premature objective/quest completion effects.
- [ ] Add creator UI fields for the complete trigger, payload, and spawn-policy model.
- [ ] Add clear validation and summaries to the creator UI.

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

- [ ] Update the WEB5 GeoHotSpots API reference with exact requests, responses, validation, and errors.
- [ ] Update the WEB5 Quests API reference with GeoHotSpot linkage, progress, events, and rewards.
- [ ] Update the GeoNFT API reference to describe the unified policy ownership and GeoNFT reward behavior.
- [ ] Update Swagger/OpenAPI annotations and generated examples.
- [ ] Update all Postman collections and environments under `C:\Source\OPORTAL-JS\postman`.
- [ ] Update the STAR quest developer guide.
- [ ] Add an Our World creator and player-facing GeoHotSpot guide.
- [ ] Publish the final use-case/combination matrix with automated and manual results.

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
| Specification | Complete | This document | Agreed unified GeoHotSpot trigger and GeoNFT reward/spawn-policy direction. |
| API contract | Not started |  |  |
| WEB5 implementation | Not started |  |  |
| Our World integration | Not started |  |  |
| Automated matrix | Not started |  |  |
| Manual matrix | Not started |  |  |
| Docs/Postman | Not started |  |  |

