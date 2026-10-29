# HyperDrive V2 routing acceptance

This maintained report owns failover/load-balancing progress. Full nine-provider
acceptance remains incomplete. [Current status](CURRENT_STATUS.md) is the recovery entry.
Legacy HyperDrive pipelines are documentation-only; this phase targets the V2 router.

## Fixed: selection must not mutate routing configuration

Both sync and async `OASISHyperDrive` selection obtained the node's mutable
load-balancing list. When empty, adding the current provider as a candidate also
modified that shared runtime configuration. A read therefore changed what later
requests inherited, without an explicit settings update.

Core23fd899 copies the effective candidates before adding the request-local default.
Two controlled regression cases failed before this fix and now pass. Two further
sync/async controls verify subsequent requests still use their current provider.
Core builds both netstandard2.1/net10.0;4426Core tests pass. The controls do not prove
an actual wrong-provider response occurred before the fix; the reproduced defect
is mutation of the shared configured list.

## Acceptance scope still to verify

| Gate | Current evidence / remaining work |
|---|---|
| Request/avatar/node precedence | Existing AvatarHyperDriveRoutingTests; inspect end-to-end selection and cross-request isolation. |
| Subscription/capability candidate filtering | Controlled selection/execution suites exist; audit native provider scope and denial behavior. |
| Temporary read failure and failover | Existing cause/attempt-budget suites; native process-outage routing proof required. |
| Unknown mutation outcome | Existing recovery/cancellation suites; never infer permission for a second mutation from a timeout. |
| Empty replica response | Preserve upstream primary-confirmation fix and controlled tests; native read consistency proof required. |
| Round-robin/performance/geography/cost | Controlled determinism/metrics tests; actual native routing/diagnostic proof required. |
| Avatar fault isolation | Audit faulted-provider eligibility and effect on other avatars. |
| Native provider breadth | Mongo/SQLite/Neo first; extend priority IPFS/Azure/AWS/Oracle/SOLID/ActivityPub only with proving evidence. |

Replication restart/outage/order/budget proofs are separately scoped in
[DURABLE_ROUTE_DESCRIPTORS](DURABLE_ROUTE_DESCRIPTORS.md). They do not certify
failover/load balancing. Default retry scheduling, other-pair outages and audited
operator/V2 lineage migration remain open; no global snapshot or100%uptime claim.

## Evidence

- `TestResults/OwnedRoutingCurrent/routing-isolation-red.trx`: 2failed/2passed before fix;SHA256 `040E4F1F7AA81354921BA3946F6647AD0E3FC50AE41FA8D7A066CEBBADD5F7BB`.
- `TestResults/OwnedRoutingCurrent/routing-isolation-final.trx`: 4426passed/0failed/0skipped;SHA256 `34B7489F13B9A07E561370F57A5E5671EF072AACC146F371C00479ECC787A0CE`.

Raw evidence is regenerable in the worktree TestResults folder; no raw output is committed here.
