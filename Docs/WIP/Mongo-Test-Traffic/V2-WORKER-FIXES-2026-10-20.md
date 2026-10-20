# HyperDrive v2 worker fixes

Legacy `OASIS.StorageProviders.AutoReplicationEnabled` and v2 `OASIS.OASISHyperDriveConfig.AutoReplicationEnabled` are intentionally separate. No change to either switch's meaning. Dev WEB4 remains paused through v2 `EnableHostedSync=false`.

## Changes

- Capture preserves MongoDB error 286 as `MONGO_DOMAIN_CAPTURE_HISTORY_LOST` for both holon and private avatar streams. The hosted worker logs the underlying exception at critical severity and stops on this permanent failure, rather than continuously retrying an expired checkpoint. It does not silently install a fresh token or discard missed mutations. Reconciliation of the existing stale hosted checkpoint is still required before capture can resume successfully.
- All capture calls without progress, including immediate lease-contention results, wait using an explicit idle/error policy: 1, 2, 4, 8, 16, then 30 seconds. Actual progress resets the delay. Command and fan-out workers use the same bounded policy. Cancellation interrupts the waits. Work arriving after prolonged idleness can wait up to 30 seconds for the next poll; this policy concerns background synchronization, not ordinary quest API requests.
- Mongo command/fan-out stores use a read-only existence check before acquiring the global write lease when their queues are empty. After finding potential work they still acquire the original conditional global lease, re-read the ordered head, and use the original conditional item claim. No transaction, CAS, lease-owner, ordering, completion or renewal protection was removed. A race inserting work immediately after an empty read simply waits for the next poll.

## Validation

- Mongo provider build: zero errors (18 warnings).
- WEB4/ONODE build: zero errors (253 warnings).
- `HostedMongoIdlePollingTests.EmptyQueuesDoNotAcquireOrRenewGlobalWriteLeases`: passed against existing loopback MongoDB `127.0.0.1:27017`. After initialization, three empty command/fan-out polls retained byte-identical global lease documents, no acquired leases or claimed items. Owned temporary test database removed in finally.
- `HyperDriveWorkerBackoffTests`: two passed; verifies every idle retry waits, the 30-second bound, and reset/immediate processing after progress.
- Focused diff whitespace check passed. Initial full test graph encountered concurrent missing/locked Core build outputs; validation then reused completed dependencies with `BuildProjectReferences=false`. No unrelated source was changed to accommodate that collision.
- No hosted Atlas test traffic or worker resume was used for these fixes. No hosted checkpoint was modified.

Compact build/test logs remain in this folder. The source changes are a scoped batch; unrelated shared worktree edits are excluded.

## Historical comparison environment cleanup

David requested removal of the temporary October 6 worktree after verification. Comparison evidence is retained in `HISTORICAL-FULL-API-COMPARISON-2026-10-20.md` and associated JSON/log files. Before removal, the parent and all five nested detached worktrees were clean, and no comparison dotnet process remained. Sibling historical Libraries/Holochain checkouts are part of this same disposable environment. Removal uses Git's worktree command for each registered checkout, preserving the shared Git repositories and object stores; the recorded commits allow reconstruction if needed.
