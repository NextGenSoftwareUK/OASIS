# Dev HyperDrive v2 workers resumed

David explicitly requested resuming workers after the fixes. WEB4 dev `OASIS.OASISHyperDriveConfig.EnableHostedSync` restored to true; parsed comparison verified no other configuration change. Legacy/v1 settings, concurrency protection and staging/production untouched.

## Deployment prerequisite fixed

First worker-fix build `3f984bf0-e40d-4b2b-8917-4707688e53ea` failed because four already-committed cloud providers called a six-argument `HolonGraphWritePlan.Create`, while the committed shared helper provided only five arguments. This was a published-source contract gap, not a worker-code compiler error. Minimal parent fix `18aa2be48e324476126c916dd185af32a9d99d35` added the validator overload and invoked it before projecting each source. The old overload delegates with a null validator. Regression check on the exact minimal source passed legacy calls, validator invocation, propagated rejection and unchanged caller identity. Broader concurrent COSMIC work was preserved uncommitted; no dependency pins or unrelated feature batches were swept in.

The fixed build `e751f0b5-7b1d-475a-91db-374c50aa3f16` succeeded. After setting the flag, redeployed that exact existing image with `usePreviousImageTag:true` as `5143eb25-9f3d-464f-9742-1462bcc0d15b` (SUCCESS).

## Observed startup, 08:48 UTC

- Ordered command worker started.
- Sync-history compaction started, hourly interval and 30-day inactive-device retention.
- Fan-out worker started with zero explicitly configured target providers. Its worker is running; this does not establish delivery to external targets.
- Domain capture initialized from existing state, projected zero records, then logged `MONGO_DOMAIN_CAPTURE_HISTORY_LOST` and stopped once. No checkpoint was silently replaced. The expired checkpoint still requires reconciliation before successful capture can resume.

Command/fan-out workers are resumed with idle backoff and empty-queue lease-write avoidance. Capture is safely halted, not fully recovered. Do not describe all synchronization as healthy until reconciliation has been implemented/performed and verified.
