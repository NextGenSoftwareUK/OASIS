Superseded by ../KEYDB_CQL_MVP.md for current integration; preserved f89cf0085 history.

# KeyDB CQL MVP — independently verified native scope

Updated29October2026. Scoped teaching proof; full provider/ORM parity remains open.

## Launch

```powershell
# Preview without starting services:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider KeyDB -Federated -ShowPlan

# Run the C#/HTTP/browser demo; use the page's Stop button afterward:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider KeyDB -Federated

# Automated native/restart/SDK/HTTP checks and teardown:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider KeyDB -Federated -VerifyOnly
```

Open http://127.0.0.1:5119/ when ready. All-record selection returns5physical rows
(KeyDB3 + Mongo2). Two-ID selection returns4(2+2), retaining both providers' copies
of matching holon IDs. The unrelated third Mongo record is excluded.

## C# entry point

The preset compiles the same spacious, commented
[IdentityFederationWalkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
used by the other provider pairs. With activated providers and trusted catalogues:

```csharp
// Choose canonical holon IDs; the walkthrough freezes this selection.
var selectedIds = readableKeyDbIds.Take(2).ToArray();

// HolonManager dispatches one explicit Mongo + KeyDB federation.
// Each source receives its own trusted authorization catalogue.
var result = await IdentityFederationWalkthrough.RunAsync(
    manager,
    mongo,
    keyDb,
    selectedIds,
    readableMongoIds,
    readableKeyDbIds,
    actor,
    Console.Out);

// Both native queries use Where(row => selectedIds.Contains(row.Id)).
// Provider-labelled copies remain visible in result.Rows.
```

KeyDB explicitly specializes the existing encrypted Redis transport executor with
its own fixed authority. Mutable identity cannot grant capabilities to another
facade. Canonical save keys and authenticated private-record encryption are reused;
no plaintext source or alternate query executor is substituted.

Bounded ID equality/membership/Boolean algebra supports at most100authorized IDs.
Missing/deleted rows are excluded before paging; unsupported business fields,
ordering and children reject before native reads. Decoded response total is capped
at4MiB after individual reads, and budget failure publishes no partial rows.
Arbitrary predicates/full LINQ, central factory enrollment, active replication,
cluster/failover/HyperDrive behavior and distributed snapshots remain separate gates.

## Evidence

| Proof | Result | Evidence | SHA256 |
|---|---|---|---|
| Authority/shape/cancellation controls | 8 passed, 0 failed/skipped | `TestResults/KeyDbCqlCurrent/keydb-controls.trx` | `413b78a282c371793df528a868f21a8fe6879278c38e13436f918561ce39eff2` |
| Native KeyDB | 1 passed, 0 failed/skipped | `TestResults/KeyDbCqlCurrent/keydb-cql-native.trx` | `e417eab28d7248182e6b39c9c81acfd66515fedf87cb421c7aa1e2a472f206e9` |
| Clean native restart | 1 passed, 0 failed/skipped | `TestResults/KeyDbCqlCurrent/keydb-cql-restart.trx` | `b7af068db13f8f2447e8339f2944fdc1e5dde8bf971bd80b8f8b3bd9db5a9d94` |
| Mongo bootstrap | 1 passed, 0 failed/skipped | `TestResults/KeyDbCqlCurrent/mongo-init.trx` | `cb1f03d4adedcd7805757321fdd78003de1a2d4b444c9d3037dd669f80990929` |
| Paired SDK/HTTP | 1 passed, 0 failed/skipped | `TestResults/KeyDbCqlCurrent/keydb-mongo-federation.trx` | `2c36e3c7e41929b4ffda1d81460b968f221860170a140271bb04abe056860a21` |

Native source has3live records and1soft-deleted record. Exact encrypted key/DUMP
hashes survive provider and actual process restart. Native version, executable
path, admitted binary hash, socket ownership and reported process ID independently
bind the source to KeyDB6.3.4; generic Redis INFO fields alone are not certification.
Missing/deleted/paging/budget controls pass. Pair verifies SDK/HTTP5/4, forged
actor/unknown preset400, inactive source503 without rows, unchanged KeyDB ciphertext,
and exact temporary Mongo contents before verified drop.

Browser visibly returned5then4 with MongoDBOASIS/KeyDBOASIS labels; screenshot
`TestResults/KeyDbCqlCurrent/browser.jpg` is regenerable. Hub routing controls pass:
8single/16plans,28paired/56plans,42paired rejections,3archive rejections, no listener
changes. Actual interactive hub launch/query/stop and automated -VerifyOnly repeat passed.

## Native resources and recovery

- Runtime `/var/tmp/oasis-runtimes/keydb-6.3.4`:24,406,090B including admission.
  Official release [source commit](https://github.com/Snapchat/KeyDB/commit/7e7e5e57d25fe246a8201f0acf5e7363c0bf1e14), archive
  SHA256 `08e6dc019bbf7b5b46f2b6879ad87f34db801302c3f37a51f53436abee840ac6`.
  Built unchanged with existing GCC15.2.0, make-j2, TLSyes, libc, FLASHno. Daemon SHA256
  `309fe740e931ab61f4cdcc2ccb22359fca8241959ce1600224cb170dde821e29`.
- Official package host timed out; the release provides no prebuilt GitHub assets.
  Pinned official source was used. Missing UUID/curl development dependencies were
  installed once and inventoried (27packages,23,772KiB registered installed size).
  No vendor patches, new compiler, global KeyDB service or autostart.
- Completed build/source121,750,538B and archive5,946,301B removed after admission;
  owned package downloads6,859,064B removed after version/hash/ownership review.
  Reuse runtime and installed dependencies; no repeated builds/downloads.
- Original data/keys/receipts `/var/tmp/oasis-keydb-cql`: loopback16390, db0,
  AOF appendfsync always, protected key/password files0600. `enable-motd no`,
  server-threads1 and memory64MiB/noeviction. Retain keys and source together.
- Reuses Linux Mongo8.3.13 `/var/tmp/oasis-cosmic-mongo/data` on27039, .NET10.0.12
  and cached portable VSTest18.11.0. The owned temporary database
  `cosmic_cql_mongo_keydb_demo` was absent before journal and verified dropped.
- The first paired preflight correctly refused startup but used an incorrect
  Valkey-style directory assumption. Fixed KeyDB admission requires its actual
  single `data/appendonly.aof`; original source was not reset, replayed or recreated.
- Pending outcomes or source/key/config changes refuse continuation. Runner owns
  exact native PIDs/sockets and requires clean SIGTERM. All owned services stopped.

Scripts under Scripts/TestHosts: prepare-cosmic-keydb-linux.py,
run-cosmic-keydb-linux.py and Run-CosmicKeyDbLinuxDemo.ps1. Original unfinished build
can only be explicitly resumed after pinned source verification. If a runtime is
lost, any rebuilt executable requires a fresh hash admission review; never replace
original data or keys. [Resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
