# PocketBase CQL MVP — controls only

Updated 29 October 2026. Native, restart and Mongo-federated demo remain **unverified**.
This provider is not included in the green demo count.

## Implemented and tested

- Reuses the shared bounded identity compiler: GUID equality/membership, boolean composition and paging; at most 100 admitted identities.
- One native REST filter over the canonical `oasis_id` projection, with live-record filtering and stable identity ordering. Unsupported predicates fail explicitly.
- Required page metadata, complete response, identity agreement, duplicate/order rejection, cancellation, result budgets and a 4 MiB body limit.
- Generated caller ID/provider key become visible only after matching native acknowledgement. Ambiguous dispatched writes require reconciliation before replay.
- Common canonical reader validates projection identity and tombstones for point, parent, all-record and CQL reads.
- Activation state and authentication cleanup follow provider lifecycle.

## Evidence

Provider and unit project builds pass. 41 targeted tests passed, zero failed/skipped:
[query/save controls](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS.UnitTests/PocketBaseCqlTests.cs)
and [read contract](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS.UnitTests/PocketBaseReadContractTests.cs).

Regenerable raw evidence: `C:/Source/OASIS-cosmic/TestResults/PocketBaseCqlCurrent/pocketbase-controls.trx`.
SHA256: `1C572091282F872F5B14767AA6B16EA8529AB2C231912CFB5868F61E70B27FAF`.
Synthetic HTTP controls do not establish native PocketBase compatibility or durability.

## Next admission and remaining gaps

Inspect existing runtime/cache first, then provision one reusable free local service and record its exact version, schema, identity and resources in the ledger before writing fixtures. Verify several records, missing/deleted identities, native restart, then one Mongo-paired C#/HTTP/browser preset.

Storage currently uses plaintext canonical JSON. Upsert is read-then-write, not atomic conditional concurrency. Child-depth/history and atomic concurrency remain unverified; avatar/detail save ownership is not certified by the holon tests. No automatic retries, hosted availability, full parity or central-factory claim.
