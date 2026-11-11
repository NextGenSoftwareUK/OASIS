# Supabase CQL demo — local PostgREST data plane

## Source recovery — 2 November 2026

Named source/test/launcher closure restored. 36 controlled tests pass (zero
failures/skips); provider/native-consumer builds and PS/Python syntax pass.
Original service, PostgreSQL data, credentials and accepted fixture untouched.
The native acceptance below is historical; current hosted/runtime availability
is unverified. Bounded GUID PostgREST CQL is not full Supabase/ORM parity.
Original TRX SHA256 `197451a1cc98a742cf0cb60157a9142bc3f74a76174af3c9b28a85f0edc9c632`; durable manifest
`LocalState/COSMIC/LauncherRecoveryCurrent/supabase-recovery-evidence.json`.

Updated 30 October 2026. Current scoped local acceptance; hosted Supabase, its
gateway/Auth/storage/realtime and full ORM/provider parity remain unverified.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicSupabaseLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5125/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both retain Supabase/Mongo provider labels.
Finish with the local **Stop demo** button and wait for verified fixture cleanup
and runner exit. Start one native demo at a time; this uses the shared retained
PostgreSQL container and Linux Mongo service.

The shared [commented C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects an explicit provider scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Authoritative
access catalogues belong to each source; requested IDs do not create access.

## Current verified scope

- Actual PostgREST16.5 and PostgreSQL, explicit `restApiPath: ""`; production
  constructor still defaults to `/rest/v1`. No URL probing or alternate-route retry.
- Existing JWT/roles/private credentials reused; missing and altered-signature
  JWTs reject401. Original4canonical records/3live/1deleted survive actual REST
  and PostgreSQL restart without reseeding or business-write replay.
- Native `id=in.(...)`, deletion filter and ascending UUID order, maximum100
  authorized IDs. Complete exact Content-Range, canonical identity/payload and
  duplicate/order checks;4MiB streamed-body bound, cancellation, paging after
  missing/deleted exclusion and result-budget errors without partial rows.
- One canonical reader serves point/parent/all-record/CQL reads. Detached save
  projection publishes caller ID/provider key/saved count only after exact native
  returned representation; uncertain dispatch returns reconciliation contract.
  Soft delete uses the same canonical acknowledged save. Current save/delete
  fault controls are synthetic HTTP tests, not native cross-client CAS proof.
- SDK/HTTP5/4; forged actor/unknown preset400, inactive source503 without rows;
  browser independently returned5/4. Unrelated Mongo record excluded.
- Exact temporary `cosmic_cql_mongo_supabase_demo` verified and dropped;
  REST/Mongo/PostgreSQL stopped, pending/active/ready absent,15432/18091/27039/5125
  listeners absent. Original Supabase-provider database and shared volume retained.

## Current evidence

All40 cases passed, no failures/skips. Paths below are relative to
`C:/Source/OASIS-cosmic/TestResults/SupabaseCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| supabase-current-controls.trx |36|8B9ACA7A1C617F87356A6AD53ACA6397651A095B4EE7E3B7E484400341901628|
| native.trx |1|135290482A974E686E78CECC43D2BF5C0766A78CC2BBB98E9449E5CFE187B634|
| restart.trx |1|4FDAEA137B2972607046892D18179703AF8631E2071585FD6E01C728ED578BF1|
| mongo-init.trx |1|406313AF6F447B689641F14336D449491DD6E74E6919F387E1217EA9E34C6900|
| mongo-federation.trx |1|93B96276BAD8F7495DA84348E9AC56BD80BD879BEEC7863F12A73D2AA83160F4|

Browser evidence: `supabase-mongo-subset-current.png`. Public CQL section31 main
2c63d5e / Production7453264651 publishes a local launch catalogue, not hosted
Supabase connectivity. Backend remains task branch; no Railway deployment.

## Retention and remaining work

`/var/tmp/oasis-supabase-cql`14files/995B (including protected credentials) unchanged.
Exact SQL row/projection/payload baseline remains
762E72718AFF2D8793189610C759D6FCB68447D43CE44382874841ABB9E2F7E2.
Retain the7,845,555B `cosmic_supabase_demo` database/roles with its credential files;
never remove the shared `oasis-postgres-data` volume. Prior completed TRXs archived.

Canonical JSON is plaintext. General name/description/metadata predicates,
children/depth/history, cross-client conditional writes, avatar/detail ownership,
bulk atomicity, central registration, hosted row policies/auth/gateway acceptance,
HyperDrive/failover/load-balancing and distributed snapshots remain separate work.
[Historical proof](Archive/SUPABASE_CQL_MVP_PRESERVED_2026-10-30.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).

## Development WEB4 candidate

The hosted profile uses `CQL_SUPABASE_QUERY_ORIGIN` for the dedicated HTTPS
project, `CQL_SUPABASE_QUERY_GATEWAY_KEY` for the publishable or legacy
anonymous gateway key, `CQL_SUPABASE_QUERY_READER_TOKEN` for a separate
restricted authenticated JWT, and `CQL_SUPABASE_QUERY_PROBE_ID` for an
existing public teaching record. Review the table's row-level policies for
the teaching principal and scope access to the reviewed rows. Keep the
reader token in protected settings and review its expiry before use.
[Supabase API keys](https://supabase.com/docs/guides/getting-started/api-keys) ·
[Row-level security](https://supabase.com/docs/guides/database/postgres/row-level-security).

The native HTTPS fixture uses `TestCategory=SupabaseHostedCqlNative` with
`COSMIC_SUPABASE_HOSTED_CQL_SOURCE` pointing to the reviewed catalogue
with `Actor`, three `LiveIds`, `DeletedId` and `MissingId`. Use one live ID
as the probe. All 36 controlled provider and eight WEB4 admission tests
passed. The native HTTPS fixture compiled and awaits the reviewed source.
The retained local gateway evidence is separate.


11 November UTC: Supabase remains absent from the live Development catalogue after c39eba5cd1 cutover. The reviewed HTTPS fixture and restricted reader in ACTIONS-FOR-DAVID remain prerequisites for native and WEB4 acceptance. HyperDrive owns the separate BootLoader and storage conformance work. Preserve the retained local PostgREST proof.

11 November UTC: HyperDrive's [Supabase native credential isolation CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43014993069) passed at f6906649aa. The shipping provider exercised two independently authenticated local PostgREST 16.5 endpoints against PostgreSQL 16.6, including crossed-credential rejection and isolated holon save/load. This qualifies that native path. Hosted Supabase gateway, writable project, and the restricted HTTPS teaching reader in the Development CQL Playground remain open.

11 November UTC: HyperDrive's [Supabase native storage conformance](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43015778894) passed all 52 sync/async checks against its isolated loopback fixture and verified cleanup. Development 998a9b5986 carries the adapter fix for uniform parent keys in PostgREST graph inserts. Hosted project and restricted CQL teaching-reader verification remain open.

11 November UTC: HyperDrive's [Supabase native pagination CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43016561338) passed at a994ffa8b2. It used PostgreSQL and PostgREST with 121 rows per listing surface and a 37-row response cap, requiring four native pages. This is loopback evidence for the three listing surfaces. Hosted Supabase CQL still awaits the dedicated HTTPS teaching project and restricted reader.

11 November UTC: Supabase WEB4 registration is on COSMIC but absent from Development 31db62c741. Candidate 7533021f03 reuses the reviewed registration and preserves the current HyperDrive storage implementation. The restricted reader passes separate gateway and bearer credentials and its transport guard admits the canonical bounded offsets produced by the shipping PostgREST paginator. Diagnostic 58466f5f48 is submitted for nine boundary tests. No hosted settings were applied. The dedicated HTTPS source and restricted reader remain required.

11 November UTC: Supabase WEB4 boundary CI 43061249200 passed all nine controlled cases on candidate 7533021f03. The exact candidate WEB4 image build was dispatched with the commit SHA only. Development advanced to 2dd52cf6c0 with CockroachDB storage verification and no overlapping WEB4 or Supabase changes. Hosted source enrollment and live acceptance remain open.

11 November UTC: prepared Supabase integration commit 24d780526c on Development 2ec6cd9cce. All six changed file blobs match the boundary-qualified candidate 7533021f03. The exact candidate image run 43062441681 is active. Hosted teaching source remains required, and Development integration awaits image verification.

11 November UTC: [Supabase activation and pagination CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43064279367) is running on the prepared Development candidate plus five activation controls. The restricted public probe must return one exact record; a second native page containing another record must reject activation. Hosted fixture and image verification remain open.

11 November UTC: Development 3b8445942b integrated PlanetScale's 52/52 native storage result on official MySQL 8.4.13 (HyperDrive CI 43064133424). Managed PlanetScale CQL acceptance stays separate. Supabase integration 0dfbf05ce9 is prepared on that head. The six reader files, WEB4 source and Docker configuration match image candidate 7533021f03. PlanetScale is not referenced by the current WEB4 project graph. Supabase image and activation CI must pass before integration.

11 November UTC: release inspection found that Development's pinned Core e5802022e9 omits Supabase from StorageQueryTariff.SupportedProviders. Existing Core commit 39168ef records the Supabase-only admission and is outside the pinned ancestry. The active WEB4 image and activation CI qualify the reader but do not qualify paid query admission. Hold Development integration until the Supabase-only Core admission is carried onto the current pin, the parent manifest and gitlink agree, and the complete candidate is verified. PlanetScale has the same admission dependency and remains a separate batch.

11 November UTC: Supabase activation/pagination CI 43064279367 passed. The revised release candidate f5ee717872 pins the scoped Core tariff admission 8a5de72b6e in both the dependency manifest and gitlink. Exact admission CI 43066096421 includes a query-only tariff test and the five activation cases. Exact image run 43066150933 targets that same parent candidate. Development integration awaits both results.

11 November UTC: superseded Supabase image run 43062441681 and activation run 43064279367 completed successfully on the earlier Core pin. The exact tariff-enabled candidate remains under qualification in 43066096421 and 43066150933.

11 November UTC: exact tariff-enabled Supabase WEB4 CI 43066096421 passed. The job required all ten WEB4 boundary and query-only admission cases and all five provider activation cases to pass. The feature branch now carries the matching scoped reader and tests. Image 43066150933 is still building on release candidate f5ee717872. Development integration and hosted enrollment remain open.

11 November UTC: Development 1ce576c988 integrated verified Apache Cassandra storage (52/52 plus five ownership cases in HyperDrive CI 43066716817). Cassandra is in the WEB4 project graph. Supabase candidate fcf3a50b57 preserves that verified change and every Supabase reader, test and Core pin blob from tariff-qualified f5ee717872. Exact combined WEB4 image CI 43067946525 was dispatched with a SHA-only image tag. Managed Supabase enrollment remains open.

11 November UTC: cancelled superseded image run 43066150933 to release the shared build slot. The combined Supabase and verified Cassandra image run 43067946525 moved from pending to queued. The tariff admission and activation results from 43066096421 still apply to identical Supabase and Core source.
