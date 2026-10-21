# Dev HyperDrive workers paused

David explicitly requested the pause. Only WEB4 dev `OASIS_DNA_JSON.OASIS.OASISHyperDriveConfig.EnableHostedSync` changed from true to false; parsed configuration comparison verified every other setting was preserved. No source edits or concurrency-protection changes. Staging/production untouched.

Redeployed existing successful image from deployment `67a07f09-ac15-4a95-ad4f-d909ef1a5759`, commit `cdee56440506a096ec00bd9710f635a115b456ac`, with `usePreviousImageTag:true`. New deployment `f6ffef77-b05e-4389-aea8-491374c24f21` is SUCCESS. At 08:21:05 UTC startup logs explicitly confirmed hosted capture, command execution, replication fan-out and sync-history compaction disabled. See `dev-workers-paused-logs.json`.

Pausing also disables the hosted sync API gate, which reads this same flag. Ordinary quest APIs remained functional. This pause is persistent until the flag is restored and the service redeployed. Queued/checkpoint state was preserved; nothing was reset or deleted. Both ongoing backend chats were told to preserve the pause and avoid hosted Atlas testing during isolation.

## Immediate bounded verification

| Operation | Paused result | Earlier observed result |
| --- | ---: | ---: |
| Full game quest read, two requests | 5,495 / 5,437 ms | 5,812 / 5,533 ms |
| Same-selection active quest save | 4,285 ms | 4,199 ms |
| Avatar profile after save | 1,128 ms | approximately 1,120 ms |

All HTTP 200, no API error. Quest response remained 10 items / 37,297 bytes. Four full metadata requests remained 5,168–5,339 ms. Selected quest/objective verified unchanged after the write. Raw compact measurements: `dev-workers-paused-performance.json`.

The pause did **not** immediately remove the multi-second delay. This argues against active worker polling being the sole immediate bottleneck. It does not exclude a historical contribution to rolling database quotas, nor confirm quota throttling. Measurements are small samples across ongoing service deployments, not a fully isolated load benchmark: WEB4 and WEB5 had advanced to cdee5644 through other work before this pause. The worker pause reused that image, without rebuilding current source.

Leave workers paused as requested. Investigate result-transfer latency and available Atlas quota evidence next; avoid claiming that waiting will fix it or promising an exact recovery time.
