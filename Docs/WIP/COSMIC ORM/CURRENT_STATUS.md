# COSMIC ORM / CQL current status

Updated 2 November 2026. Goal active and incomplete.
Priority: breadth-first live demos. Deep selected-stage LINQ parity is paused.
Work only red scopes or reproduced bugs; preserve accepted cohorts.

## Verified coverage

- **63 integrated query types:** 3 canonical, 56 bounded public/source profiles,
  4 local PostgreSQL/Cosmos/Oracle/AWS implementations.
- **58 hosted functional HTTP proofs**, 5 browser verified, 53 browser gates open.
  These figures do not certify full ORM/native writes/reliability/uptime.
- MongoDB, SQLite and Neo4j: **50/82 strict parity features each**.
  The original investor-demo subset is green.
- Bittensor accepted and frozen. Native3/2, Mongo5/4, repeat0new/2existing,
  foreign0private/3public, exactSQLite/Neo4j2each,2version1saves/pending0.
  Original cohort frozen; no independent consensus or AI/subnet claim.
- Zcash local acceptance:27adapter/config tests; actual manager/LINQ/CLI3/2;
  local HTTP3/2+Mongo5/4, foreign0private/3public,2SDKsaves/9receipts.
  Exact fixture cleaned and owned Mongo stopped. Original source is frozen.
- Zcash HTTP admission3API+1Core, prepared4presets and10publisher controls pass.
  Combined Worldscape/Zcash merge11API+1Core passes. Independent hosted audit
  controls pass2tests/16faults. Shared C# federation whitelist bug reproduced
  and fixed with a controlled red/green test; no live replay.

## Active release and exact next step

1. Development parent **9e98a4278c91a9d01caaabdfbed447d38120a6f2**;
   Core **5b47c0b60b027092e51e486050b898ba147a4194**. Worldscape preserved.
   Manifest/gitlinks and7Dockerfiles pass; remote component pushes verified.
2. Zcash code job **624285a4-15a7-419d-8dad-e8d8daf90bc8 SUCCESS**.
   Three settings applied once, exact full candidate/per-key ACKs verified,
   node DNA unchanged. No additional settings submission permitted.
3. Single activation **66d2ccea-7d77-4f76-816a-12ed810fe6ed SUCCESS**
   at the same code. Observe this job, never start another on a polling timeout:

   `python Scripts/Sandbox/Observe-HostedDeployment.py 66d2ccea-7d77-4f76-816a-12ed810fe6ed --expected-commit 9e98a4278c91a9d01caaabdfbed447d38120a6f2 --output TestResults/ZcashPublicSourceCurrent/activation-observation.json`

4. Original Zcash hosted acceptance and independent audit PASS: native3/2,
   Mongo5/4, repeat0new/2existing, foreign0private/3public, exactSQLite/Neo4j2each,
   2normal APIversion1saves/pending0. Cohort frozen; no query/save replay.
   Evidence/ACK: `LocalState/COSMIC/HostedQueryAcceptance-ZcashOASIS`.
   Zcash card published at website3141cca9491d585ed0b1c8aab7a52c7dd5bbc948;
   HTML and both JS assets match exact Git blobs. Browser gate remains closed.
   ClickHouse adapter builds,25controlled tests pass; native countries3/2
   qualified,4probecontrols pass. Actual manager/LINQ/CLI3/2passed; HTTPadmission3API+1Core and
   publisher10controls pass. Next original localHTTP/Mongo/privacy gate.
   Original absentStackOverflowtable failure retained; no hosted ClickHouse acceptance yet. Do not replay accepted Zcash.
5. Durable Zcash state: `LocalState/COSMIC/HostedZcashPublicSourceCurrent`,
   `ZcashPublicQualificationCurrent`, `ZcashMongoHttpCurrent` (cleaned),
   `HostedZcashReleaseCurrent`. Preserve owners/config/intents/ACKs; no reseeding.

## Remaining gates

- [ClickHouse qualification](CLICKHOUSE_PUBLIC_SOURCE_STATUS.md): public read-only
  identity/schema and countries3/2 verified; adapter25controls pass,
  manager/nativeCQL3/2+HTTPadmissioncontrols pass; localHTTP/hosted remain red. Profile keys printableASCII.

- [Zcash details](ZCASH_PUBLIC_SOURCE_STATUS.md): hosted proof/card/static bytes pass; browser open.
- [Ceramic qualification](CERAMIC_PUBLIC_SOURCE_STATUS.md): pinned official
  client reviewed; original Clay healthGET failed before HTTP (URLError, cause
  unproven). No retry; current operator access/public StreamIDs unresolved.
- Preserve denied Lens/Aztec403/1010 outcomes; no bypass/retry. Farcaster free
  access unresolved. Nostr503/Blockscout timeout causes remain unresolved.
- Hosted Cosmos/Oracle/PostgreSQL deferred by David; no paid hosting/re-asking.
- Browser/OS control remains closed. HTTP/static assets are not browser proof.
- Generic private browser copies omit full native payload; backlog remains red.
- Full ownership/concurrency/children/history/deltas/migration/placement and
  wider HyperDrive acceptance remain subject to exact roadmap evidence.
  Legacy gaps docs-only; implementation work V2 only.

## Workspace and resources

Work/build/test only `C:/Source/OASIS-cosmic`, branch `work/cosmic-cql-ton-public`;
Core `work/cosmic-cql-google-public`; frontend `C:/Source/Web4Site/work/cosmic-cql`.
Website main/task3141cca9491d585ed0b1c8aab7a52c7dd5bbc948 includes Zcash card.
No new worktree/environment. Dev push timing authorized; no production/master.

Use build lease with `/m:2 /nr:false /p:UseSharedCompilation=false`.
Raw output only `TestResults/`; stable identity/proof `LocalState/COSMIC/`.
Named-file commits only; fetch/preserve upstream before integration.

Disk target120GB unmet; latest measured free58.27GB. Direct Zcash build output
57,798,520bytes measured. Previously policy-denied188folders/2.155GB and shared
external outputs preserved; no cleanup retry/bypass. Personal Miniforge/models
KEEP. No owned local DB/build process remains. Shared provider data preserved.
See [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) and
[disk recovery](../Cleanup/DISK_RECOVERY_CURRENT.md). Unknown root logs untouched.

[Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) |
[Manual](CQL_MANUAL.md) | [Architecture](CQL_ARCHITECTURE.md) |
[Parity](CQL_PARITY_CHECKLIST.md) | [Backlog](AFTER_DEMO_BACKLOG.md) |
[David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md)
