# GeoHotSpot use-case and verification matrix

**Updated:** 2026-09-20

This matrix covers the shared policy engine, WEB5 trigger contract, quest progress, rewards, and Our World client behavior. “Automated pass” means a repeatable checked test/build. “Evidence required” identifies behavior that must be observed in a running Unity scene or deployed environment and then validated by the acceptance-evidence gate.

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
| Failure | Provider/save/grant failure | No untracked partial success; retry same key resumes without duplicates | Release build; journal round-trip and child-operation stability tests; focused suite 22/22 | Live provider interruption remains a manual deployment check |

Automated evidence:

- `GeoSpatialSpawnPolicy`: 1,536 combinations plus 14 named boundary cases passed.
- Focused WEB5 suite: 22/22 tests passed (`GeoHotSpotsControllerTests`, `GeoHotSpotQuestContractTests`, and `GeoNFTQuestContractTests`).
- WEB5 project build: 0 errors.
- Our World `Assembly-CSharp.csproj` build: 0 errors; isolated Unity runtime suite: 6/6 passed.
- Live development Anorak contract: all five distinct tree pickups succeeded; every duplicate was rejected, each pickup produced one objective transition with no replay, and cleanup restored progress to 0/5 while preserving all 26 unrelated inventory rows.
- Railway dependency manifest validation is run whenever the dependency SHAs below are advanced.

## One-command automated runner

Run `Scripts/run_geohotspot_full_matrix.ps1`. It executes the WEB5 Release build, focused API suite, all 1,536 spawn-policy combinations, and Our World EditMode/PlayMode runtime tests. It writes `geohotspot-matrix.json`, `.md`, and `.html`, plus TRX, Unity XML, and logs under `TestResults/GeoHotSpotMatrix`.

Setup, fixture contracts, CI gates, transaction assertions, evidence formats, and troubleshooting are documented in [GeoHotSpot automated verification](./GEOHOTSPOT_AUTOMATED_VERIFICATION.md).

Device and deployment execution steps are documented in [GeoHotSpot live acceptance runbook](./GEOHOTSPOT_LIVE_ACCEPTANCE_RUNBOOK.md).

Live providers use `-ProviderProfilesPath Scripts/geohotspot-provider-profiles.example.json` after copying that example outside the repository and supplying disposable credentials/endpoints. With no concurrency fixture, the runner automatically uses `Scripts/run_local_geohotspot_resilience.ps1` for synchronized two-process competition and forced restart/replay over temporary SQLite state. Full WEB5 deployments use `-ConcurrencyFixturePath` with the schema in `Scripts/geohotspot-resilience-fixture.example.json`. Credentials and deployed process commands remain external because secrets must never be committed. An omitted provider prerequisite is recorded as `SKIP`; it is never reported as a pass.

The device/deployment rows are deliberately marked rather than reported as verified. Live Unity presentation, media playback, physical location/AR behavior, two-avatar concurrency across deployed replicas, and deliberately interrupted deployed providers require observation in the environment that owns those signals. Use `Scripts/geohotspot-acceptance-evidence.template.json`; the full runner's `-AcceptanceEvidencePath` gate validates all nine cases and their artifacts. The cross-provider mutation path uses a durable reservation journal and idempotent owning-manager writes, so a retry resumes without compensating or duplicating state.

### Environment audit on 2026-09-20

Every locally executable stage passed. The added disposable resilience environment passed one-of-two acceptance across independent HTTP processes and passed an actual in-flight termination, restart, commit replay, and identical second replay. Portable, loopback-only provider processes also passed direct activation and persistence on 2026-09-20: SQLite 3/3, MongoDB 3/3, and Neo4j 5/5 (`PASS 3 / FAIL 0 / SKIP 0` providers). These direct-provider results do not substitute for the separate full WEB4/WEB5 HTTP deployment profiles.
