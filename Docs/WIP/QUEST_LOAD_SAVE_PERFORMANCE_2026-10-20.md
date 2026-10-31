# Dev quest load/save investigation — 20 October 2026

## Findings

The main measured quest-read bottleneck is retrieving large MongoDB result documents, not executing the metadata filter. A standalone MongoDB.Driver client, without HyperDrive routing or COSMIC object mapping, reproduces approximately 4.58 seconds for the full quest result. Tracker saving compounds this cost by repeatedly reading the full avatar detail and replacing the entire document for a two-field change.

This investigation did not deploy a performance fix. It used backend requests and read-only database diagnostics, without Computer Use. The only mutation was saving the already-current quest/objective selection once, followed by a read confirming the selection was unchanged. No additional karma was awarded during this investigation.

## Deployed source/configuration inspected

- WEB4 successful deployment: `d5386d48-bd07-4ab3-83a7-0000132b07d4`.
- WEB5 successful deployment: `99e18827-11bf-4516-b029-d1e5349e91a5`.
- Both deployed parent revision: `c4646ccbfd141b79d41aa31fdf19f48f96f9fa30`.
- Core pin: `dfa7a8f0e02d227d9d67cdc36b57063292bac039`.
- ONODE Core pin: `fa911ee5036565cd4b7a7ca77c5f24c00bd2cfad`.
- STAR ODK pin: `e608dc135fae608be2f25a63e1d1a6bfad52b22a`.
- Railway dev `OASIS_DNA_JSON`: WEB4 `OASISHyperDrive2`; WEB5 `Legacy`. Both specify the same MongoDB connection/database. Auto-replication is false in both deployed environment configurations. Environment configuration was inspected without changing it. No in-process mode switch was performed; this is configuration evidence rather than a new runtime trace of each routing decision.
- Deployed source was inspected with `git show` rather than assuming the dirty shared checkout exactly matched deployment.

## Measurements

Authenticated dev API requests, 20 October 2026 UTC:

| Request | Client elapsed | Result |
|---|---:|---|
| WEB5 `/api/quests/all-for-avatar/game` | 5,475 / 6,127 ms | HTTP 200, 10 summaries, about 37 KB |
| WEB4 profile read before/after save | 1,121 / 1,128 ms | HTTP 200 |
| WEB4 `/api/avatar/set-active-quest`, same selection | 4,199 ms | HTTP 200; persisted selection verified unchanged |
| Railway upstream duration for the same save | 3,788 ms | Delay was inside the upstream API path |
| Railway upstream durations for the two quest reads | 4,953 / 5,604 ms | Delay was inside the upstream API path |

Raw MongoDB.Driver controls using the deployed dev connection, without loading OASIS libraries:

| Read | BSON payload | Elapsed |
|---|---:|---:|
| Matching quest IDs only | Small projection | 61–87 ms |
| Full matching quest documents, repeated | 419,565 bytes, 32 rows | 4,583 / 4,581 ms |
| Quest/objective field projection | 205,656 bytes, 32 rows | 2,297 ms |
| Full avatar detail | 56,639 bytes, 1 row | 673 / 668 ms |
| Avatar tracker fields only | 188 bytes, 1 row | 54 ms |
| Separate serial child queries, one per matching quest | 32 queries | 1,720 ms |

The serial-child-query control is illustrative, not an instrumented attribution of the production endpoint's child-query count or elapsed time. It must not be added mechanically to the raw full-read measurement to claim an exact production breakdown.

MongoDB `explain` reported 0–1 ms execution, examining 612 documents, for the metadata queries. Existing indexes were `_id` and `HolonId`. An index may help future growth, but query execution in this dataset does not explain a multi-second delay.

Most of the full quest payload was metadata (320,281 bytes), including objectives (200,650 bytes) and QuestDNAJSON (58,390 bytes). The game response is made small **after** loading full holons. The raw database roots and final game summaries are different stages; their 32-versus-10 counts do not by themselves establish duplicate versions or a data-integrity problem.

The transfer-time scaling is consistent with effective throughput around 90 KB/s. This is evidence of a payload delivery bottleneck, not proof of a particular Atlas quota. MongoDB identified itself as version 8.0.34 on an Atlas hostname. An explicitly requested zlib control did not improve timings; a hello negotiation returned no supported compression. Compression has not been presented as a verified fix.

## Code path and what changed

### Quest loads

`QuestsController.GetAllQuestsForAvatarGame` calls `LoadAllQuestsForAvatarAsync`, which calls the generic STARNET `LoadAllForAvatarAsync`. That method invokes `HolonManager.LoadHolonsByMetaDataAsync<T>` with default child/recursive loading enabled. Filtering/version handling, metadata promotion and game-summary mapping occur after the database read. This loads substantially more data than the final game DTO requires.

WEB5's deployed configuration uses legacy routing. The standalone driver reproduces most of the delay without either routing implementation or COSMIC mapping. Therefore a new HyperDrive 2 routing algorithm is not required to reproduce the quest-load problem. This does not rule out indirect shared-database load from other services or quantify all remaining mapping/child-load overhead.

The additional Unity startup regression had two sequential requests (quests, then saved selection); they have already been changed to concurrent requests in the local client. That removes an added profile-read wait but cannot remove the measured backend quest-read delay. Changing active selection now updates the local UI before the background save completes, with rollback on failure. These UI changes are not evidence that remote persistence became instant.

### Tracker saves

The deployed existing-record path performs four full avatar-detail reads:

1. `AvatarProfileController.SetActiveQuest` loads the avatar detail.
2. `AvatarManager.UpdateAvatarDetailAsync(Guid, ...)` loads it again before merging.
3. `MongoDBOASIS.SaveAvatarDetailAsync` loads the existing native entity.
4. `MongoNativeRecordWriter.SaveAsync` reads exact persisted BSON for its compare-and-swap observation.

The writer then sends a full replacement plus a full-document equality condition. Thus changing only ActiveQuestId/ActiveObjectiveId moves the approximately 56 KB profile repeatedly, including unrelated avatar state. Four raw reads at the observed rate cost roughly 2.7 seconds before the replacement request and other processing; this is an explanatory estimate, not per-stage instrumentation of the hosted request.

Recent storage safety work contributes to this amplification. Commit `18a602cc38fdfae2ef946221888fa841c7a1f9b2`, 15 October, **Share native Mongo save safety with avatar and detail writes**, moved avatar-detail updates through the shared native writer. The deployed writer includes an additional persisted-BSON read, whole-document compare-and-swap, unknown-field preservation and majority/journal acknowledgement. Earlier repository code replaced the entity directly after checking acknowledgement. This establishes a concrete added I/O mechanism; no before/after deployment benchmark exists here to assign an exact historical regression percentage.

These are storage-provider safety changes in the HyperDrive/storage work, rather than evidence that COSMIC property mapping itself consumes several seconds of CPU. Removing the concurrency protections would trade correctness for speed and is not an appropriate fix.

## Appropriate corrections

1. Give the game quest list a storage projection matching its summary/objective requirements, and apply eligible filtering before transferring full holons. Preserve legacy child-backed objective semantics explicitly; do not globally turn off graph loading for consumers that require it.
2. Give tracker selection a narrow persisted read and atomic field update, retaining authorization, lifecycle/identity checks, concurrency requirements and any required replication/domain-capture semantics. Avoid routing a two-field tracker change through repeated full-profile merge/replacement operations.
3. Confirm Atlas tier, rolling transfer usage and throttling metrics before making any infrastructure change. Both dev services share that database, so their aggregate traffic matters. Atlas account metrics/management credentials were not available through the existing CLI setup.

Atlas documents network throttling for free clusters that exceed transfer or operations limits: [Atlas Free Cluster Limits](https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/). This supports throttling as a hypothesis; this investigation does **not** establish the current cluster tier or quota state.

## Reproduction and evidence

Reusable diagnostics in `C:/Source/Our-World`:

- `Scripts/Test-QuestBackendPerformance.ps1 -VerifySelectionSave`
- `Scripts/Test-QuestMongoPerformance.ps1` (read-only)
- `Tools/QuestPerformance/QuestPerformance.csproj` (MongoDB.Driver 3.12.0 reused from the installed NuGet cache)
- `Docs/WIP/quest-backend-performance.json`
- `Docs/WIP/quest-mongo-performance.json`
- `Docs/WIP/quest-mongo-performance-compressed.json` (earlier compression control)

Additional sanitized evidence in this repository:

- `Docs/WIP/quest-performance-routing-summary.json`
- `Docs/WIP/quest-save-http-timing.json`
- `Docs/WIP/quest-save-server-log.json`

Credentials were used only in process memory/environment and were not written to these reports. No new runtime, repository, database, index or infrastructure plan was installed or created. Diagnostic build outputs were cleaned after the runs; the project can rebuild using the existing package cache.
