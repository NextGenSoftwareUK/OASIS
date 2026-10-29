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
| Request/avatar/node precedence | Concurrent controlled sync/async router execution2/2 proves request-over-avatar, avatar failover list overnode and peer opt-out without node mutation. |
| Subscription/capability candidate filtering | Controlled selection/execution suites exist; audit native provider scope and denial behavior. |
| Temporary read failure and failover | Native Mongo process-down -> SQLite reads pass sync/async/explicit with ordered diagnostics; other providers/consistency boundaries remain open. |
| Unknown mutation outcome | Existing recovery/cancellation suites; never infer permission for a second mutation from a timeout. |
| Empty replica response | Core8917a52 propagates failed primary confirmation and includes explicit loadbalance entry;12controls plus native primary-only Mongo witness/emptySQLite proof pass. Preferred-provider/explicit-failover scope remains next. |
| Round-robin/performance/geography/cost | Native round-robin4reads alternate Mongo/SQLite after recovery; performance/geography/cost remain controlled-test evidence only. |
| Avatar fault isolation | Controlled routing2/2 proves one-avatar refusal before storage IO, peer access unaffected and fault restoration. Native ACL/privacy certification is separate. |
| Native provider breadth | Mongo/SQLite/Neo first; extend priority IPFS/Azure/AWS/Oracle/SOLID/ActivityPub only with proving evidence. |

Replication restart/outage/order/budget proofs are separately scoped in
[DURABLE_ROUTE_DESCRIPTORS](DURABLE_ROUTE_DESCRIPTORS.md). They do not certify
failover/load balancing. Default retry scheduling, other-pair outages and audited
operator/V2 lineage migration remain open; no global snapshot or100%uptime claim.

## Evidence

- `TestResults/OwnedRoutingCurrent/routing-isolation-red.trx`: 2failed/2passed before fix;SHA256 `040E4F1F7AA81354921BA3946F6647AD0E3FC50AE41FA8D7A066CEBBADD5F7BB`.
- `TestResults/OwnedRoutingCurrent/routing-isolation-final.trx`: 4426passed/0failed/0skipped;SHA256 `34B7489F13B9A07E561370F57A5E5671EF072AACC146F371C00479ECC787A0CE`.

Raw evidence is regenerable in the worktree TestResults folder; no raw output is committed here.

## Native Mongo/SQLite read routing

Run `Scripts/TestHosts/Run-CosmicBoundSourceWorkerTests.ps1 -NativeRoutingProof`.
The owned Mongo process is stopped and TCP refusal is verified while SQLite holds
the journaled records. Sync, async and explicit failover reads return the expected
ID/name/owner from SQLite. Diagnostics show the failed Mongo attempt then successful
SQLite attempt, selected provider and public result metadata. Explicit failover
starts a new sequence and uses OriginalProvider=Default; automatic modes retain
OriginalProvider=Mongo. IsExplicitRequest distinguishes them.

After same-data Mongo recovery and shipping replication, four sync/async reads
alternate Mongo/SQLite under explicit round robin. Record identity/owner/provider
keys and the stable configured list are verified. Native scenario1/1 passes in22s.
This is not write-failover, geo/performance ranking, a global snapshot or all-provider
certification. Per-avatar eligibility, protected errors and uncertain mutation
boundaries still need the remaining scoped acceptance.

The first setup failed because the runner read a not-yet-published journal; that
runner bug is fixed by flushed atomic publication and a snapshot reader sharing
delete permission. The observer stops reading after process handoffs, avoiding a
cleanup race. Explicit unwritten-fixture reconciliation verified0business rows,
receipts/outbox/approvals in both stores before cleanup. A later assertion used the
automatic OriginalProvider expectation for explicit mode; the mode-specific test
was corrected, and original pending0/0 operations were reconciled without source
resaves. Both reconciliation checks pass1/1; the final fresh native proof passes.
No production failover behavior was changed to satisfy these fixture corrections.

- `TestResults/OwnedRoutingCurrent/worker-offline-routing.trx`: 1pass;SHA256 `3817CFC1FF728DBFF6D4CD46C6CA7008CBC37503779A6AB79352F90889112D12`.
- `TestResults/OwnedRoutingCurrent/worker-offline-cleanup-unwritten.trx`: 1pass;SHA256 `905192BF17986C02D273C00A8D2E98B03FB31D7F988BB22F709D38A0B356E0FC`.
- `TestResults/OwnedRoutingCurrent/worker-offline-reconcile-routing.trx`: 1pass;SHA256 `E34759CA7183F7C195F3BE72DA5522257E9FD1193CF086D68CA827B501135731`.
- `TestResults/OwnedRoutingCurrent/worker-offline-routing-initial-failure.trx`: 1setup failure;SHA256 `D0935BE65CF362957B713217717DE4A9691487AB991C64ECD837F6D39AA2D810`.
- `TestResults/OwnedRoutingCurrent/worker-offline-routing-diagnostic-assertion-failure.trx`: 1assertion failure;SHA256 `806F8BBD509CEE405A4039E4822CEA142720ABDECFEE0A2267FC099F050E227B`.

## Concurrent scoped execution and primary-absence correctness

Two sync/async controlled cases use one registry and concurrent AsyncLocal avatar
contexts. Request load-balancing preference overrides the avatar preference; that
avatar's fault forces its scoped SQLite failover. A peer with failover disabled
still reads Mongo. The faulted avatar makes no primary storage call; node flags,
node lists and parent identity stay unchanged. This tests routing, not record ACLs.

A separate reproduced defect ignored primary errors after an empty replica read,
returning false successful absence. Explicit LoadBalanceRequestAsync skipped that
confirmation entirely. Core8917a52 centralizes sync/async primary confirmation and
preserves its actual failure. Default-provider, unpinned reads now return the
primary's connection/denial/no-wrapper error; genuine primary absence still succeeds.
Ten of12new cases failed before the fix; all12now pass. Existing primary-hit controls
remain intact. Both Core targets build and4440Core tests pass overall.

Native `-NativePrimaryConsistencyProof` creates a journaled Mongo-only witness with
replication off. While Mongo's owned process is stopped, an empty SQLite replica
cannot prove that record absent: all3public forms return an error. After same-data
recovery they return the real witness from Mongo, while SQLite stays empty for that
ID. Existing replication/order/restart checks also run. Native scenario1/1 passes
in22seconds; exact target cleanup includes the witness's own ID/receipt operation.

Scope still open: preferred-provider and explicit-failover absence semantics,
other provider pairs, strategy eligibility and full consistency/placement policy.
Do not infer all-provider or all-entry-point completion from this milestone.

- `TestResults/OwnedRoutingCurrent/avatar-routing-execution.trx`: 2pass;SHA256 `164B3837723BEA573BF664B0A8598983358F7091750B04C4B1BE4D14CFCC407A`.
- `TestResults/OwnedRoutingCurrent/primary-absence-red.trx`: 10failed/2passed beforefix;SHA256 `16DAFFBCE68986BAA693F05AA7FC941D6567A8D0AFB93A87F00B1620792913D6`.
- `TestResults/OwnedRoutingCurrent/primary-absence-final.trx`: 4440pass;SHA256 `64E52D1446DA7A775A885E372D0EF6093CCF82AF4380F6ACFE784FF6C88D8205`.
- `TestResults/OwnedRoutingCurrent/worker-offline-consistency.trx`: 1nativepass;SHA256 `E23DF6B123094B0014CE6398528F129BB113F76A0FB5A92467D18226E3993BD5`.

## Preferred authority and explicit failover outcome ownership

Core `ed83223` chooses request preference before node default for read authority.
An empty alternate cannot erase an authority failure already observed in the same
request. Sync, async and explicit failover retain the original code/cause and mark
the public diagnostic unsuccessful with the authority selected. Known outcomes are
reused without a duplicate authority read; an additional confirmation, when needed,
is recorded as an actual diagnostic attempt. Healthy authority absence stays terminal.

Nine new controlled cases: six failed before the fix, all nine pass afterward.
Both Core targets build; full Core suite passes 4449/4449. Native preferred-provider
coverage is still next; this does not certify all provider pairs or policy strategies.

- `TestResults/OwnedRoutingCurrent/preferred-absence-red.trx`: 6 failed / 3 passed; SHA256 `9ACE5271A8C40B8B51688117507DA05E1E26B2F3032AF3F021DD190BD3B6E030`.

- `TestResults/OwnedRoutingCurrent/preferred-absence-green.trx`: 9 passed; SHA256 `BE15BFC9BE595F5D96EAE6F43F511DB9370D6AFB1DF0AAC426191AA082502821`.

- `TestResults/OwnedRoutingCurrent/preferred-absence-full.trx`: 4449 passed; SHA256 `1A4A6641483613F74F6BB5FF4071FDC950C26781273AE5C9F5C97DF8DA0C8ADA`.
