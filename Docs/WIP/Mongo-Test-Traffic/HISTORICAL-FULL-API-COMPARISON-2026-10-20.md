# Historical full quest API comparison

## Method and provenance

Clean detached parent worktree: `C:/Source/OASIS-quest-comparison/OASIS`, commit `dc653aa29c503c45ce002b661521ed12501f10d1` (October 6). Nested repositories were checked out at the historical manifest pins, using existing Git object stores. No branch, clone, source patch, or new SDK was needed. Historical STAR API restored and built successfully (zero errors).

Executed the actual historical ASP.NET API on port 18888. Used the current WEB5 dev DNA configuration, including the same MongoDB Atlas URI/database, and the same authenticated avatar. Overrode the historical default WEB4 URL to the dev WEB4 API. Subscription usage disabled for this diagnostic. Alternated two historical and two current requests to `GET /api/quests/all-for-avatar/game`. Authentication was shared; no reward or quest-selection writes were issued by the comparison harness.

| Version | First request | Second request | Response |
| --- | ---: | ---: | --- |
| October 6 historical API, local host | 6,904 ms | 6,484 ms | HTTP 200, IsError false, 10 quests, 37,297 bytes |
| Current dev API, Railway | 5,812 ms | 5,533 ms | HTTP 200, IsError false, 10 quests, 37,297 bytes |

All four parsed quest payloads produced the same SHA-256 hash. Quest ID lists also match. Exact measurements/hashes: [historical-full-api-comparison.json](historical-full-api-comparison.json). Reusable harness: [Compare-HistoricalQuestApi.ps1](Compare-HistoricalQuestApi.ps1).

## Interpretation and limits

This is a full historical API comparison, unlike the earlier historical-predicate-only test. Recent request-path code changes are not necessary to reproduce multi-second quest loads against the current Atlas database. Historical code is also slow with the same current data.

It does **not** rule out all code changes as an indirect cause: newly deployed background workers could load the shared database for both versions. Nor does it prove Atlas quota throttling. Old and new applications ran on different hosts/network paths, and two requests per version are bounded diagnostic evidence, not a throughput benchmark. The historical runtime logged a provider-list warning (`AutoFailOverLocalProviders list is null`) with current DNA, but served both requests successfully with identical complete payloads. This configuration compatibility caveat is retained in the diagnostic log.

Historical process was stopped and its generated credential-bearing runtime DNA removed. Source worktree remains clean. Reusable build retained; about 63.55 GB free on C: after the run.

## Background traffic finding

Current WEB4 logs show domain change capture repeatedly failing with `MONGO_DOMAIN_CAPTURE_FAILED`, approximately every five seconds. Current source confirms hosted capture retries after five seconds, independent of HTTP activity.

The command worker also polls when idle: each successful lease-owning idle cycle renews its global lease with `FindOneAndUpdateAsync`, then reads the oldest incomplete command; it delays one second between empty polls and retains the lease until shutdown. The fan-out worker acquires its global lease, reads the oldest incomplete item, and releases the lease even for an empty batch; that is two lease writes and a read per successful empty dispatch, plus a one-second delay. Database round trips add to these intervals. These are concrete autonomous database operations; their exact share of the Atlas traffic chart remains unmeasured. Do not infer four lease writes per second: the command worker does not release on every empty poll.

Next causal step: measure the active hosted workers' queue/lease activity and capture failure cause, correlate their deployment times with traffic, then compare bounded reads during a controlled worker pause if operationally appropriate. HTTP access logs alone cannot count these direct database operations.

## Follow-up: capture failure established

At approximately 08:18 UTC, a single bounded, read-only native MongoDB change-stream probe used the stored `holon-v4` checkpoint with the provider's UpdateLookup/Required-before-image options. Atlas returned MongoCommandException **286 / ChangeStreamHistoryLost**: the resume point may no longer be in the oplog. The probe did not write a checkpoint, acquire a lease, or project changes. This explains the observed generic `MONGO_DOMAIN_CAPTURE_FAILED` loop. The stored capture state's updatedUtc remains September 26, while its capture lease continues being renewed now.

Metadata snapshots also show the command executor lease expiry advancing from 08:23:13.201 to 08:23:15.264 within about 2.7 seconds, confirming an active worker. The fan-out lease was null in both snapshots; these samples cannot establish whether it was repeatedly acquired/released between observations.

Additional source defect: a capture call that cannot acquire its lease returns an empty successful result immediately, and the hosted service has no delay on that path. Multiple workers can therefore repeatedly query the database without the intended change-stream wait. This is a confirmed code path, not proof that multiple contenders are currently active.

Git dates the hosted worker to September 25 and its failure-isolation change to September 29. The domain capture provider file's latest tracked change is September 26. Therefore these loops are not inherently new within the last two weeks; deployment/configuration/replica changes and accumulated quota usage still need correlation.

Evidence: `C:/Source/Our-World/Docs/WIP/quest-mongo-performance-traffic.json`. Diagnostic mode added to the existing reusable tool, build verified with zero warnings/errors. No hosted configuration was changed, and no checkpoint was reset. A recovery must reconcile missed state before installing a fresh checkpoint; silently replacing the token would risk missing changes. The eventual latency control should pause the hosted synchronization workers while preserving the ordinary data-write concurrency protection.
