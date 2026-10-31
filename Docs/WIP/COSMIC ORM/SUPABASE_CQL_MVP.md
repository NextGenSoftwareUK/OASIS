# Supabase CQL demo — local PostgREST data plane

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
