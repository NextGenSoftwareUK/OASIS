# Dragonfly CQL demo — current integration

Updated 30 October 2026. Scoped local acceptance; full provider/ORM parity remains incomplete.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicDragonflyLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5122/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4 rows). Both preserve Dragonfly/Mongo provider labels.
Finish with **Stop demo (keep original Dragonfly source)** and wait for the runner
to exit. The original encrypted source is retained; the exact owned Mongo fixture
is verified and removed. Start only one Linux Mongo demo at a time.

The commented C# example is [IdentityFederationWalkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs).
It builds an explicit provider scope and applies `Where(row => selectedIds.Contains(row.Id))`
through HolonManager. Authoritative access catalogues belong to each source;
caller-supplied IDs do not create authorization.

## What is verified

- Independent Dragonfly 2.0.2 executable hash, native version/PID/socket ownership;
  existing Redis-protocol encrypted executor with a distinct Dragonfly authority.
- Three original live records and one tombstone; ID membership/equality/Boolean
  selection, missing/deleted exclusion, paging after exclusion, bounded results,
  cancellation and unsupported-shape rejection. Catalogue maximum100; decoded4MiB
  limit is checked after native values are read; errors publish no partial rows.
- Original encrypted record/index hashes unchanged before/after queries and actual
  clean restart. Loaded checkpoint verified twice. No business write or SAVE replay.
- C# and HTTP paired5/4; malformed/forged requests400, inactive provider503 without
  rows. Browser independently returned5/4. Unrelated Mongo record excluded.
- Exact temporary `cosmic_cql_mongo_dragonfly_demo` removed after identity checks;
  owned daemons exited0, pending/active/ready files absent,16391/27039/5122 stopped.

## Current evidence

All14 checks passed, no failures/skips. Files below are relative to
`C:/Source/OASIS-cosmic/TestResults/DragonflyCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| dragonfly-current-controls.trx |8|EF78F2E7C3040C10DAE3F9379B0C5CDDEF27F31E9E63DE8CCC76AF5A75A480ED|
| dragonfly-cql-native.trx |1|28D4EE640AAAB85364B2D05FD096BC5923EAB4402AA0CF9B17D5223BCDF7BA0C|
| dragonfly-checkpoint-native.trx |1|7B96D172A45A33A2A24C5BB762659A89FE928A59ED79F7DFA06A8A148DE8FE0F|
| dragonfly-cql-restart.trx |1|A55C059353A48A5E2E81E869872D8A729987E618EE9DC03E612F9C8E746C9E41|
| dragonfly-checkpoint-restart.trx |1|6E02CAA6E7BDFC4DB49495AB5AA27088F1BA2C431F5EA191DD7CD71A8321E753|
| mongo-init.trx |1|EDCA50AB7961913A10B3E1B847420931F0FAB4E7FE7DDF555308EB293A460932|
| dragonfly-mongo-federation.trx |1|475353B037C79CFBE50EA6161B20A462BA9CF3FC92C197499BAD013A573B2DC3|

Browser evidence: `dragonfly-mongo-subset-current.png`. Public CQL catalogue
section28 main496f879 / Production7453130290 succeeded; this is a local launch
catalogue, not a hosted Dragonfly connection or Railway backend release.

## Retained source and remaining limits

`/var/tmp/oasis-dragonfly-cql` retains the original seed, private key/password,
snapshot and acknowledgements together. Seed SHA256
B078D49F455CD3F647B0B2F43D991CE40E6D82C9C31F9F9A706E196357C33084 unchanged.
Clean SIGTERM legitimately changes RDB metadata: logical encrypted-data equality
is enforced across generations and exact file hashes bind each stopped snapshot.
Earlier verification receipt is hash-archived; unresolved outcomes refuse replay.

General name/metadata predicates, children, conditional writes/history, central
registration, active replication/cluster/failover, public hosting, AOF/per-save crash
durability/PITR and distributed snapshots remain unverified for this scope.
[Historical proof/failure review](Archive/DRAGONFLY_CQL_MVP_PRESERVED_2026-10-30.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).
