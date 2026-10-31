> Superseded by ../DRAGONFLY_CQL_MVP.md for current-branch demo acceptance; preserved historical evidence and failure review.

# Dragonfly CQL MVP — native snapshot/restart scope

Updated29October2026. Scoped demo proof; full ORM/provider parity remains open.

## Launch

```powershell
# Inspect without starting services:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Dragonfly -Federated -ShowPlan

# Run the C#/HTTP/browser preset, then use its Stop button:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Dragonfly -Federated

# Automated native/checkpoint/restart/SDK/HTTP verification and teardown:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Dragonfly -Federated -VerifyOnly
```

Open http://127.0.0.1:5120/ when ready. All records returns5physical rows
(Dragonfly3+Mongo2); two-ID subset returns4(2+2). Provider-labelled copies of the
same IDs stay visible; the unrelated third Mongo record is excluded.

## C# example and execution

The preset compiles the shared, commented
[IdentityFederationWalkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs).
With activated providers and trusted per-source catalogues:

```csharp
// Freeze the canonical holon selection; do not trust client-supplied access lists.
var selectedIds = readableDragonflyIds.Take(2).ToArray();

// HolonManager dispatches explicit Mongo + Dragonfly scope.
var result = await IdentityFederationWalkthrough.RunAsync(
    manager,
    mongo,
    dragonfly,
    selectedIds,
    readableMongoIds,
    readableDragonflyIds,
    actor,
    Console.Out);

// Both native sources evaluate Where(row => selectedIds.Contains(row.Id)).
// result.Rows retains provider provenance for physical copies.
```

Dragonfly explicitly binds its own fixed authority through the same encrypted
Redis-protocol executor and canonical save keys. Mutable identity alone cannot
admit another facade. There is no second executor or plaintext source.

Supports authorized ID equality/membership/Boolean algebra (catalogue maximum100),
missing/deleted exclusion and paging after exclusion. Unsupported fields/order/
children reject before reads. Decoded total is capped at4MiB after individual
reads; budget errors publish no partial rows. Arbitrary predicates/full LINQ,
central registration, HyperDrive/cluster/active-replication acceptance and distributed
snapshots remain separate. This profile proves completed native snapshot/restart,
not AOF, per-save crash durability, PITR or uncheckpointed-write recovery.

## Evidence

| Proof | Result | Evidence | SHA256 |
|---|---|---|---|
| Authority/shape/cancellation controls | 8 passed, 0 failed/skipped | `TestResults/DragonflyCqlCurrent/dragonfly-controls.trx` | `86230a5250cc7d07829250a4d28ec3cfc1e18ed70e009f0c779533a0d53a9555` |
| Native source | 1 passed, 0 failed/skipped | `TestResults/DragonflyCqlCurrent/dragonfly-cql-native.trx` | `f486fc352cde13105d1b8329e244ac05eb32d205ba191e545d6dc2455169a08e` |
| Loaded native checkpoint | 1 passed, 0 failed/skipped | `TestResults/DragonflyCqlCurrent/dragonfly-checkpoint-native.trx` | `3a2be1eb2cc440a4048e752084c0d84330d80013079d1c6a9523200f0656da5e` |
| Clean restart | 1 passed, 0 failed/skipped | `TestResults/DragonflyCqlCurrent/dragonfly-cql-restart.trx` | `df8943bcb944c9b4b600d47d25b1299993882848fb52fd9dfa3c3d5a2c9edf52` |
| Restarted checkpoint | 1 passed, 0 failed/skipped | `TestResults/DragonflyCqlCurrent/dragonfly-checkpoint-restart.trx` | `d732d79714006b0a9c73ec652cbe0dc3ca0a4f2e30ec5729948cb5863ed470e7` |
| Mongo bootstrap | 1 passed, 0 failed/skipped | `TestResults/DragonflyCqlCurrent/mongo-init.trx` | `0e6409d3a7ce02498ef031a7ddbab83e1861564ea83c3d2151f9f331ecf1f7b1` |
| Paired SDK/HTTP | 1 passed, 0 failed/skipped | `TestResults/DragonflyCqlCurrent/dragonfly-mongo-federation.trx` | `216c130888ef8e2a55eed131993a5fc50e7ff781f958438d44c52bbe9d67b0c0` |

Native source has3live records/1soft-deleted record. All encrypted native key/DUMP
hashes survive provider and actual process restart. Missing/deleted/paging/budget
checks pass. Pinned executable/socket ownership, native dragonfly_version2.0.2 and
reported PID independently bind the source to Dragonfly, not another compatible
server. Pair verifies SDK/HTTP5/4, forged actor/unknown preset400, inactive source503
without rows, unchanged encrypted source and exact temporary Mongo cleanup.

Browser visibly returned5then4 with MongoDBOASIS/DragonflyOASIS labels; regenerable
screenshot `TestResults/DragonflyCqlCurrent/browser.jpg`. Hub routing controls pass:
8single/16plans,29paired/58plans,44paired rejections,3archive rejections; listeners
unchanged. Actual interactive launch/query/stop and automated -VerifyOnly repeat passed.

## Snapshot ownership and retained source

Initial source writes and soft delete are acknowledged once; an explicit native
`SAVE RDB source.rdb` records its own durable intent/acknowledgement. Later tests
load the same source and do not replay saves or create new identities. Dragonfly
also saves on clean SIGTERM, legitimately changing RDB metadata. Every stop binds
its resulting exact file hash; every startup requires that admitted file, and
native reads independently require every original encrypted record/index DUMP
hash to match. Snapshot-file hash equality is enforced within a running generation;
logical encrypted-data equality is enforced across generations. Byte-hash comparisons
are independent of hex letter casing. The initial snapshot ack remains unchanged.

The first harness incorrectly expected immutable whole RDB files, then exposed
hex casing in its corrected check. Both failure reports and original state were
preserved and explicitly audited. Fresh complete native/checkpoint/restart tests
then passed. No original source/key replacement, reset or write replay occurred.

## Resources and cleanup

- Runtime `/var/tmp/oasis-runtimes/dragonfly-2.0.2`,36,177,326B including admission.
  [Official v2.0.2 release](https://github.com/dragonflydb/dragonfly/releases/tag/v2.0.2),
  archive17,766,071B SHA256
  `eb9dfe654c69ab190cb3aa329ed3599ebe19c5e9a3a437bb7bc38ef52f665c88`;
  daemon SHA256 `2cf7bea7e142b0360421ed5083e54ee49e0effc41f7ced5ebdcaf88f8ff2e116`.
  Owned archive removed after admission; no OS dependencies installed.
- Original `/var/tmp/oasis-dragonfly-cql`: keys/password0600, loopback16391/db0,
  RDB snapshot/stop receipts, one proactor,256MiB configured ceiling/no cache eviction,
  admin port0 and version-check telemetry disabled. Retain keys and data together.
- Reuses Linux Mongo8.3.13 `/var/tmp/oasis-cosmic-mongo/data` on27039, .NET10.0.12
  and cached portable VSTest18.11.0. Temporary cosmic_cql_mongo_dragonfly_demo was
  absent before journal, verified exactly and dropped. All owned services stopped.
- Pending outcomes, changed key/config/source hashes or missing original data refuse
  continuation. No automatic source recreation, replay, flush or snapshot replacement.
  Expected native shutdown snapshots are explicitly tracked, not hidden as query writes.

Scripts under Scripts/TestHosts: prepare-cosmic-dragonfly-linux.py,
run-cosmic-dragonfly-linux.py and Run-CosmicDragonflyLinuxDemo.ps1.
[Full resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
