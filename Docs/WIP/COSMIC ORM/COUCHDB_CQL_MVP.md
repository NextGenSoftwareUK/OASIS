# CouchDB CQL demo — retained records and revisions

## Source recovery — 2 November 2026

Named provider/CQL/unit/consumer/PowerShell/Python source restored from retained
pin237403a75. Forty-one controlled cases, producer/consumer compilation and syntax
pass. In-memory HTTP handlers cover query execution, errors and save publication;
no original service/account/credential/fixture was opened or replayed. Native and
browser evidence below is historical; current runtime and hosted CouchDB remain
unverified. Consumer uses one shared Mongo fixture-support source, compile only.


Updated 30 October 2026. Current local demo acceptance; full ORM/provider parity,
cross-client conditional writes and HyperDrive acceptance remain incomplete.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicCouchDbLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5127/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both retain CouchDB/Mongo provider labels.
Finish with the local **Stop demo** button and wait for the post-demo native check,
verified fixture cleanup and runner exit. Start one Linux Mongo demo at a time.

The shared [commented C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects explicit provider scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Each source
owns its authoritative catalogue; selected IDs do not create access.

## Current verified scope

- Real authenticated retained CouchDB container/image/data and original server UUID;
  original3records/payload hashes/native revisions survive actual restart and pairing.
- Bounded GUID identity/Boolean selection, at most100 authorized keys, via native
  document GETs with `conflicts=true`. Native identity/revision/payload/authority
  checks, explicit conflict rejection, aggregate4MiB budget, cancellation and
  paging after exclusion. Independent document reads are not a shared snapshot.
- Controls cover absent/deleted/error responses, unsupported name/metadata/order/
  graph shapes and version-mode rejection; retained native fixture is3live records.
- Save stages detached identity/provider key and requires matching `ok/id/rev`
  receipt before publishing caller ID/key/IsSaved/SavedCount. Partial-batch errors
  propagate with acknowledged counts and continuation policy. Denied/unavailable
  revision reads fail before PUT; only404 means absent. Index and delete failures
  cannot silently succeed. These save/error controls use synthetic HTTP replies.
- SDK/HTTP5/4, forged actor/unknown preset400, inactive source503 without rows;
  browser independently returned5/4. Unrelated Mongo record excluded.
- Native source rechecked after pairing; CQL leaves update_seq unchanged in each
  verification phase. Exact owned `cosmic_cql_mongo_couchdb_demo` verified/dropped;
  container exited0, Mongo stopped, pending/ready absent,15984/27039/5127 absent.

## Current evidence

All46 cases passed, no failures/skips. Paths below are relative to
`C:/Source/OASIS-cosmic/TestResults/CouchDbCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| couchdb-current-controls.trx |41|AFDD30AACAC270F26CDA93B6E97577F5A354D352D1DC68CA0E6674DDA6F85FD2|
| native-first.trx |1|8ED549E23CA12950E0E010D6E9164D5CA5209ACC2758AB6AFC54DDA05E746DB7|
| native-restart.trx |1|FA21FD18E0AA32EDE0337185D0B5A6D9FEEE60AD261976028AA144B73E819746|
| mongo-init.trx |1|8136649304AAF6E61F89257BA7254FDA0D09E0A48A8A3E423083F8ED9D84942C|
| mongo-federation.trx |1|029D794E744533F1AA0734340B24539D853BD63F1B07644CEF4255C082FB605B|
| native-postpair.trx |1|1A0B621B08FD688F7A8ED4FC62DEEE8F138CD487BFB393DBB38C8A042381CDB0|

Browser evidence: `couchdb-mongo-subset-current.png`. Public CQL section33 main
70b9bef / Production7453354296 is a local launch catalogue, not hosted CouchDB
connectivity. Backend remains task branch; no Railway deployment.

## Retention and remaining work

All42files/231,082B under `/var/tmp/oasis-couchdb-cql` byte-identical before/after,
including original IDs,3writeACKs, credentials0600 and native data. Native-baseline
file SHA2560A4EF1004E16D491FF53A2CACF66A01CCAA08A5AEE8A4ABA3878EEF638F647EC.
Prior completed5TRXs hash-archived; no source writes, reseed or credential replacement.

Canonical JSON is plaintext. Save currently obtains latest native revision rather
than asserting the caller's observed token: cross-client CAS is not certified.
Durable uncertain-write reconciliation, general name/description/metadata queries,
children/depth/history/version mode, avatar/bulk atomicity, central registration,
replication/failover/load-balancing and distributed snapshots remain separate work.
[Historical audit/proof](Archive/COUCHDB_CQL_MVP_PRESERVED_2026-10-30.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).

## Development enrollment

The recovery branch builds the opt-in WEB4 adapter. Its 43 provider tests
and the focused Core tariff and WEB4 preset admission tests passed. Native
replay of the retained CouchDB source and live Development acceptance remain
open.

Use a qualified HTTPS source with three synthetic holons in oasis_holons.
Set CQL_COUCHDB_QUERY_ENDPOINT and supply CQL_COUCHDB_QUERY_USERNAME and
CQL_COUCHDB_QUERY_PASSWORD privately for an account restricted to reads of
the demo database. Preserve other provider entries when enrolling the three
IDs in CQL_PUBLIC_READ_CATALOGUES_JSON and adding an explicit query-only
zero-cost tariff in PREPARED_STORAGE_ADMISSION_JSON. Configure the four
canonical native and Mongo-paired presets in
CQL_HOSTED_QUERY_EXAMPLES__CouchDBOASIS.

Before enrollment, verify the read-only boundary on the server. CouchDB
database membership permits ordinary document writes, so a member account
alone is insufficient. Use a dedicated non-admin query role on the teaching
database and enforce its write restriction with a database
`validate_doc_update` rule. Verify the account can read the database identity
and all three exact teaching IDs. In a dedicated disposable qualification
database with the same policy, verify that creating, updating and deleting
ordinary documents as the query role are rejected. Preserve the retained demo
source and qualify the target policy before handing the credentials to WEB4.
[CouchDB database security](https://docs.couchdb.org/en/stable/api/database/security.html)

After verifying the reviewed Development deployment, check discovery before
creating a cohort:

```powershell
python -B Scripts/Sandbox/Verify-HostedQuerySource.py CouchDBOASIS --readiness
```
