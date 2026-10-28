# Automatic native-bound replication — verified local milestone

Updated 28 October 2026. X10 remains red; this report does not certify full replication or CQL parity.

## Invariant

A queued automatic obligation names the original native source, native destination and host authorization policy meaning. Restart, configuration changes or provider replacement must not silently retarget that obligation. The primary records the graph, receipt and all obligations atomically. Delivery is asynchronous and at least once; native receipt replay makes target application idempotent.

## Implemented local milestone

- `HolonReplicationRouteBinding` computes deterministic length-prefixed SHA-256 route identifiers from both native binding fingerprints and the policy binding. Source IDs also include the native source fingerprint. Credentials are excluded.
- Automatic planning uses effective request > avatar > node routing. Every selected different-authority target must support native-bound receiving; unsupported or unavailable first captures reject enrollment rather than dropping selected targets. Explicit enrollment remains authoritative.
- Native identities are pinned to each provider registration object. A warm target outage does not require recapturing identity to enroll another write. Native source/receiver guards still validate actual storage in their transactions. `ForgetNativeBinding` is an explicit host reconfiguration action and never rewrites queued IDs.
- The worker reads durable unfinished route inventory, including routes absent from current routing. Its scan is bounded and retains a continuation between passes. Explicit catalog/custom namespaces are left to their owning workers.
- Legacy type-only automatic routes require reconciliation. Unmatched bound routes require reconciliation only when discovery completed successfully. Failed discovery is insufficient evidence that a route changed.
- Storage-binding mismatch, invalid binding and unbound/changed routes enter reconciliation immediately. They do not consume ordinary transient retry attempts or write into a replacement store.
- Hosts with a custom policy must register its stable meaning with `UseAuthorizationBinding` before accepting writes. The worker registers the same policy; grants/policy changes must change the binding meaning. Default policy means committed-owner authorization, not tenant/residency/encryption placement approval.
- Unchanged owned saves validate write conditions without capturing replication targets. Invalid selected-graph intent cannot be acknowledged as a successful no-op.

## Evidence so far

- Full Core suite: 4,400 passed. Native identity pin/outage, route meaning, explicit enrollment and request/avatar/node routing included.
- Shipping Neo4j → SQLite worker: one real native delivery/restart case passed with bound automatic routes. Fresh worker leaves completed work completed and does not redeliver.
- Native Mongo primary: eight cases passed, including four routed single/batch success/rollback combinations and four existing atomic primary cases. Targets are declaration-only in primary enrollment tests; these are not cross-provider delivery proofs.
- Full Shared suite:1,289passed, including immediate reconciliation for all four route/storage-binding failure codes.
- Native SQLite routed primary:4passed. Neo routed primary plus atomic matrix:8passed, including the final runner gate. The stale6-case assertion was corrected against the actual case identities.
- Native shipping admission:5passed, including old unbound route and changed policy meaning. Each is quarantined after one attempt with unchanged source payload/version and no target write;another worker pass leaves it blocked.
- Native abrupt-process receiver commit/restart:1passed in2m8s. Original lease expires, the same obligation retries, and native target receipt replay prevents a second revision;later restart does not redeliver.
- Raw evidence stays under `TestResults/OwnedRoutingCurrent/` and `TestResults/Neo4jCurrent/`. Pending native fixture journals stay under `LocalState/COSMIC/OwnedRoutingCurrent/` and must be reconciled before reuse.

## Remaining acceptance

Native physical replacement and claim-ownership proofs remain; old-route/policy-change and abrupt-process recovery are now verified in the local Neo→SQLite topology. Other directions/topologies retain separate acceptance. Pinning is process-local: a cold restart needs fresh successful native capture before new enrollment; durable offline snapshots are not implemented. Source-outbox identity validation during delivery must also cover physical source replacement and claim races, beyond the existing guarded primary/recovery path. No automatic route migration, operator repair workflow, arbitrary topology certification, 100% uptime, hosted Azure proof, tenant/encryption grant or full X10 completion is claimed.

## Raw evidence references

Results are regenerable and are not committed into Docs. SHA-256 hashes identify this verification run.

| Path relative to worktree | Passed | SHA-256 |
|---|---:|---|
| `TestResults/OwnedRoutingCurrent/bound-route-core.trx` | 4400 | `E1AD7D79EF0EC01B18DE80A61D624A162BF802DA387138F6C5671C64D5F4E23A` |
| `TestResults/OwnedRoutingCurrent/bound-route-shared.trx` | 1289 | `20D604E0CF04E0F5D90CC456C1FE7CBE32B0B8D3950FB902FBD8042B6A0F93D6` |
| `TestResults/OwnedRoutingCurrent/bound-route-sqlite.trx` | 4 | `6528CA084EABA989025F1ED0EBC18B175D1A6671CB25D906F617D3E184E387E6` |
| `TestResults/OwnedRoutingCurrent/mongo-owned-atomic-regression.trx` | 8 | `275F4E5B464E974E1F03485D80BDF75ED28E230DBBD2ABC0583573F9FD46A097` |
| `TestResults/Neo4jCurrent/neo4j-routed-owned-outbox-current.trx` | 8 | `7B9ECD3D4FC0D52250EA6D002241ECB10B50D295F717EF4625C64FAF256133B8` |
| `TestResults/Neo4jCurrent/neo4j-hosted-admission-current.trx` | 5 | `9F4067A86B7B9CE8EE1C291AAE52A00150F5B4E6322AF0D4958A793FE009B2BE` |
| `TestResults/Neo4jCurrent/neo4j-hosted-crash-current.trx` | 1 | `58BDD1AAD8686D6D6897495CE2315A47C34F944392B4A2BFCFD9A7AFA66EB42D` |

Core commit:c128178c9507aa9912645fc0fd168a4bb7bc9d91. Native producer hashes and build logs are retained under TestResults/OwnedRoutingCurrent/bound-routing-*-runner.log. No deployment claim.
