# Valkey CQL MVP — independently verified native scope

Updated29October2026. Bounded demo scope; full ORM/provider parity remains open.

## Run

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Valkey -Federated -ShowPlan
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Valkey -Federated
# Automated native/restart/SDK/HTTP checks and teardown:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Valkey -Federated -VerifyOnly
```

Open http://127.0.0.1:5118/ when ready. All records returns5physical rows
(Valkey3+Mongo2); two-ID subset returns4(2+2). Copies retain provider provenance.
The unrelated third Mongo record is excluded. Stop using the page button; original
Valkey source stays and verified temporary Mongo data is removed.

## Execution and limits

[Shared C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
freezes selected IDs, creates explicit scope and separate trusted catalogues, then
calls HolonManager's federated executor. Both native providers evaluate
`Where(row => selectedIds.Contains(row.Id))` with bounded input.

Valkey binds its own authority through the existing encrypted Redis transport
executor and canonical save keys. Mutable identity alone cannot grant capabilities
to another facade. Native INFO independently proves valkey_version9.0.4; Redis
compatibility is not substituted for product admission.

Supports authorized ID equality/membership/Boolean algebra (catalogue maximum100),
missing/deleted exclusion and paging after exclusion. Unsupported fields, ordering
and children reject before native reads. Decoded payload is capped at4MiB after
individual reads; result-budget failure publishes no partial rows. Arbitrary field
filters/full LINQ, cluster failover/replication, public cloud and central factory
enrollment remain separate gates. Reads do not share a distributed snapshot.

## Verification

| Proof | Result | Evidence | SHA256 |
|---|---|---|---|
| Valkey authority | 8 passed, 0 failed/skipped | `TestResults/ValKeyCqlCurrent/ValKey-controls.trx` | `afba3b626505935fc6bf5e3f56cab628fffd2b111147e12ea67458800242851a` |
| Redis regression | 8 passed, 0 failed/skipped | `TestResults/ValKeyCqlCurrent/Redis-controls.trx` | `45b19fa12595250cacf30b1945337ca7954bb4e38b50dd54ad5eb5f28873d427` |
| Native Valkey | 1 passed, 0 failed/skipped | `TestResults/ValKeyCqlCurrent/valkey-cql-native.trx` | `b85c44acac39c36c6b8ee0b52eeda845b75156be0cc5ef6a42f2dbda5d0620e0` |
| Clean restart | 1 passed, 0 failed/skipped | `TestResults/ValKeyCqlCurrent/valkey-cql-restart.trx` | `e2d550778c2a99698ea55e4266d8cce2206789acef6658cd2b4ba3ae949734a9` |
| Mongo bootstrap | 1 passed, 0 failed/skipped | `TestResults/ValKeyCqlCurrent/mongo-init.trx` | `bce551aadfac6a3170b9035953adedb23c67024ac020636a9a54a261888db700` |
| Paired SDK/HTTP repeat | 1 passed, 0 failed/skipped | `TestResults/ValKeyCqlCurrent/valkey-mongo-federation.trx` | `e300e9173c1716feb180927deb6b1ce6bf88ccf25f864593ac9acf60456b9bc0` |

Native proof retains3live records/1soft-deleted record and exact encrypted DUMP
hashes through provider and actual server restart. Missing/deleted/paging/budget
checks pass. Pair verifies SDK/HTTP5/4, forged actor/unknown preset400, inactive
source503 without rows, exact fixture cleanup and unchanged Valkey ciphertext.

Browser visibly returned5then4 with MongoDBOASIS/ValKeyOASIS labels; regenerable
screenshot `TestResults/ValKeyCqlCurrent/browser.jpg`. Hub routing controls pass:
8single/16plans,27paired/54plans,40paired rejections,3archive rejections, listeners
unchanged. Corrected actual hub -VerifyOnly and interactive restart/query/stop passed on the same retained source.

## Resources and lifecycle

- `/var/tmp/oasis-runtimes/valkey-9.0.4`: official Ubuntu9.0.4-0ubuntu0.2 daemon,
  CLI/license,3,966,309B before admission JSON. Daemon SHA256
  `2cad3c3f3d307ff1dd5d5762c7ecc5f719cf8c671a5f4cfd4b8f2878a7888582`.
- `/var/tmp/oasis-valkey-cql`: original encrypted data, keys and receipts; protected
  key/password files0600, loopback16389/database0, AOF appendfsync always. Retain
  data and keys together. No global service/autostart/new OS libraries installed.
- Reuses Linux Mongo8.3.13 `/var/tmp/oasis-cosmic-mongo/data`, loopback27039;
  .NET10.0.12 and cached portable VSTest18.11.0. Temporary database
  cosmic_cql_mongo_valkey_demo was absent before journalling and verified dropped.
- Runner refuses pending outcomes, changed key/config hashes or missing original
  source. PIDs/sockets are explicitly owned; clean SIGTERM required. No source
  replay/reseed/flush. All owned services now stopped.
- Initial key-format failure happened before writes (empty AOF increment): original
  key/IDs and failure proof retained and explicitly audited. The same32key bytes
  are now encoded as canonical base64 at the API boundary.
- Repeat browser-port preflight failure occurred after native/restart proof and
  clean stop, before Mongo start. Audited without source changes. Corrected preflight
  reserves5118 only for interactive mode, with normal TCP reuse while refusing live
  listeners. Repeated native/paired proof then passed.

Scripts under Scripts/TestHosts: prepare-cosmic-valkey-linux.py,
run-cosmic-valkey-linux.py and Run-CosmicValKeyLinuxDemo.ps1. Runtime preparation
reuses admitted hashes; do not recreate an existing source.
[Full resource/cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
