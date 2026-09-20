# GeoHotSpot use-case and verification matrix

**Updated:** 2026-09-19

This matrix covers the shared policy engine, WEB5 trigger contract, quest progress, rewards, and Our World client behavior. “Automated pass” means a repeatable checked test/build. “Manual required” identifies behavior that must be observed in a running Unity scene with real media, location, and two-avatar sessions.

| Area | Use case | Expected result | Automated result | Manual result |
|---|---|---|---|---|
| Trigger | Arrival | Enter authored radius; one accepted trigger | Pass: evidence validator tests | Required |
| Trigger | Dwell | Continuous authored duration; leaving resets client timer | Pass: validator duration cases; Unity build | Required |
| Trigger | AR gaze | Continuous authored gaze duration | Pass: validator duration cases; Unity build | Required |
| Trigger | AR touch | Touch evidence accepted without dwell | Pass: controller tests; Unity build | Required |
| Radius | Inside / boundary / outside | Inside and exact boundary accepted; outside rejected | Pass: named boundary suite | Required |
| Quest | Linked hotspot | Exact linked ID records one visit and completes when it is the only requirement | Pass: `LinkedGeoHotSpotCompletesOnlyForItsExactId` | Required |
| Quest | Explicit list | Duplicate visit does not count twice; all IDs required | Pass: `ExplicitGeoHotSpotListRequiresEveryDistinctHotSpot` | Required |
| Quest | Distinct count | Numeric requirement counts distinct hotspot IDs | Pass: `CountRequirementCountsDistinctGeoHotSpots` | Required |
| Quest | Any order | Any matching incomplete objective may progress | Pass: shared target-selection tests | Required |
| Quest | In order | Only first incomplete objective progresses | Pass: shared target-selection tests | Required |
| Transition | Partial objective | No completion presentation | Pass: transition contract | Required |
| Transition | Objective only | Objective event/effect, no quest-complete effect | Pass: shared quest tests | Required |
| Transition | Final objective | One objective transition and one quest transition | Pass: shared quest tests | Required |
| Reward | Inventory object | Accepted trigger grants configured object | Build pass; endpoint path covered structurally | Required with live provider |
| Reward | Inventory ID | Template is loaded and granted once | Build pass; endpoint path covered structurally | Required with live provider |
| Reward | WEB4 GeoNFT | `NFTManager.CollectGeoNFTAsync` creates Nature inventory entry | Build pass; existing GeoNFT collection tests | Required with live provider |
| Reward | Multiple | All configured grants returned in typed result | Build pass | Required with live provider |
| Events | Hotspot-triggered | Returned only after a new matching visit | Pass: shared engine visit delta | Required for all event types |
| Policy | Permanent, zero cooldown | Unlimited and immediately eligible | Pass: 1,536-combination matrix | Required |
| Policy | Permanent, positive cooldown | Unlimited after `nextEligibleAtUtc` | Pass: policy matrix; Unity UTC re-arm build | Required |
| Policy | Global quantity non-zero | Global limit takes precedence | Pass: policy matrix | Required with two avatars |
| Policy | Global zero, player quantity | Per-avatar limit applies | Pass: policy matrix | Required with two avatars |
| Policy | `-1` quantity | Selected scope is unlimited | Pass: policy matrix | Required |
| Policy | First-player exclusivity | Other avatar rejected | Pass: policy matrix | Required with two avatars |
| Idempotency | Same key replay | Original result, no extra count/effects | Pass: controller tests | Required with live provider |
| Concurrency | Same hotspot | Per-hotspot server gate serializes requests | Pass: controller contract | Required under deployed multi-instance load |
| Client | Server rejection | No local trigger, grant, quest effect, or hide | Unity build; callback ownership verified | Required |
| Client | Delayed respawn | Re-arm at server UTC time | Unity build | Required |
| Client | Inventory/quest refresh | Shared UIs reload after acceptance | Unity build | Required |
| Content | Map/AR/VR/IR | Authored representation loads | Unity build | Required |
| Content | Audio/video/text/link | Authored action dispatches in response order | Pass: parser/dispatcher compile | Required |
| Events | Spawn/unlock/narration/teleport/audio/video/web/image/animation | Each action dispatches once in authored order | Pass: explicit dispatcher and scene-binding contract compile | Required |
| Failure | Provider/save/grant failure | Error surfaced; no silent success | Build pass; error paths explicit | Transaction rollback test still required |

Automated evidence:

- `GeoSpatialSpawnPolicy`: 1,536 combinations plus 14 named boundary cases passed.
- Focused WEB5 suite: 24/24 tests passed (`GeoHotSpotsControllerTests`, `GeoHotSpotQuestContractTests`, and `GeoNFTQuestContractTests`).
- WEB5 project build: 0 errors.
- Our World `Assembly-CSharp.csproj` build: 0 errors.
- Railway dependency manifest validation is run whenever the dependency SHAs below are advanced.

The uncompleted rows are deliberately marked rather than reported as verified. Media/action dispatch and durable cross-provider rollback need implementation before the complete feature can be signed off.
