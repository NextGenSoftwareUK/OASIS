# CouchDB CQL demo — retained records and revisions

11 November queue update: Railway deployment 4879c6fe for 59eebd2c11 includes the nine verified CouchDB and native asset paths unchanged, plus HyperDrive's Firebase storage release. If it supersedes 1b1ad2f0, apply the cutover checks to 4879c6fe and its exact serving revision.

## Development rollout checkpoint

Exact image CI 43101002944 passed on a209b82e9b. The verified CouchDB reader, Core tariff pin and NATS/Waku download fix are on Development at a614a4377f. Railway deployment 1b1ad2f0 is waiting for a build slot. Live 214462362b has no CouchDB enrolment. The Development variable-name check found no dedicated CouchDB CQL settings. The isolated HTTPS teaching database and restricted reader remain the enrollment gate.

### Cutover checks for Development a614a4377f

1. Confirm Railway deployment 1b1ad2f0 is successful and serving a614a4377f. Verify its build log passed the NATS and Waku pinned-asset stages. The exact GitHub image digest qualifies the source candidate. Railway builds the Git revision separately.
2. After warmup, use the retained visitor for authenticated discovery. Compare the native and federated source counts with the 105/106 baseline on 214462362b. Inspect activation diagnostics only for regressions or newly unavailable providers. Leave accepted cohorts intact.
3. Expect CouchDB to remain absent before the dedicated HTTPS fixture is configured. Once available, qualify the restricted reader against the reviewed catalogue, demonstrate that out-of-catalogue keys cannot reach the native client, and verify the configured public presets, private Mongo pairing and replicas before recording live acceptance.

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

For a dedicated role named `oasis_cql_reader`, the teaching database design
document can enforce the restriction with this validator. Assign the role
only to the WEB4 query account and use a separate account to seed fixtures.

```javascript
function (newDoc, oldDoc, userCtx, security) {
    if (userCtx.roles.indexOf("oasis_cql_reader") !== -1) {
        throw { forbidden: "The CQL demo role is read-only." };
    }
}
```

Review the target's existing design documents and privileges before applying
the rule. Verify creation, update and deletion rejection through CouchDB
itself before live enrollment.

Recovery-laptop inventory found no CouchDB or Docker executable, WSL
distribution, retained CouchDB fixture directory or listener on port 5984.
Apache's Windows binary distribution uses an installer. The controlled
validator check used Node only. Native policy qualification still requires
a dedicated CouchDB instance with an isolated teaching fixture.
[Apache Windows installation guide](https://docs.couchdb.org/en/stable/install/windows.html)

The existing `ci-cd.yml` provider integration matrix already runs the
CouchDB integration project against a job-owned CouchDB 3 container on port
5984. It provides a native qualification environment without a laptop
install. The current integration tests exercise ordinary avatar and holon
storage using the administrator account. They leave the query-role
restriction unverified. The scoped branch `codex/couchdb-cql-reader-ci`
adds the role test using a separate database and user scoped to the CI
job. The remote branch is at `a2666e4c1`. GitHub reported no
pull-request-triggered workflow runs for that commit. The existing owner
action tracks opening the draft PR to start the matrix. CI evidence and
policy verification on the Development source are separate gates.

After verifying the reviewed Development deployment, check discovery before
creating a cohort:

```powershell
python -B Scripts/Sandbox/Verify-HostedQuerySource.py CouchDBOASIS --readiness
```

11 November UTC: recovered the existing CouchDB native reader-policy test from the COSMIC branch. Diagnostic 33e42af5d9 is published for disposable CI against CouchDB 3. It checks a restricted reader can load the original document and receives the exact role-policy denial on create, update and delete. Hosted reader policy and live acceptance remain open.

11 November UTC: CouchDB native reader-policy CI 43044567583 passed against the disposable CouchDB 3 service. The reader loaded the original document and received the exact validation denial for create, update and delete. The original document was unchanged. Hosted CouchDB authorization and sandbox acceptance remain open.

11 November UTC: CouchDB candidate 7d1418d55b is in remote CI on Development a39f5fe06d. The fixture uses the existing pinned CouchDB version and disposable MongoDB helper to run native CQL before and after CouchDB restart, SDK/HTTP federation, and a final source recheck. Results are pending. Development a39f5fe06d also carries HyperDrive's verified Mastodon search and deletion.

11 November UTC: Development WEB4 health returned HTTP 200 on the active deployment window for 165e8abaee. Successors c837fe5ad8 and a39f5fe06d are queued. CouchDB restart and federation CI 43045093852 is running. Development WEB4 has no CouchDB-named protected settings, and the recovery workspace has no CouchDB teaching descriptor. Hosted acceptance needs an existing HTTPS teaching database and its restricted reader.

11 November UTC: CouchDB restart/federation CI 43045093852 stopped before tests because the first startup probe received a connection reset. Diagnostic 2c2a2266a6 now waits up to 45 seconds for the owned running container to return its expected native version and identity on startup and restart. It fails on terminal HTTP errors or container exit. Native restart and federation remain pending.

11 November UTC: CouchDB CI 43045487847 passed five focused phases with zero skips: native CQL before and after restart, Mongo replica admission, SDK/HTTP federation and final native recheck. Candidate 17df68f439 routes generic storage and restricted reader policy to the standard matrix and native CQL to the dedicated workflow. Combined diagnostic 8a5f86cbfe is submitted. Hosted HTTPS acceptance remains open.

11 November UTC: combined CouchDB standard pipeline diagnostic 43046007325 is queued. It covers generic native storage, restricted reader policy and the separate restart/federation job. Candidate 17df68f439 remains pending those results.

11 November UTC: both CouchDB jobs passed in combined CI 43046007325. The generic storage and restricted-reader checks used the standard matrix fixture. The native CQL restart and MongoDB federation checks used the dedicated fixture. Verified routing is on the COSMIC branch, pending the next Development batch. Hosted HTTPS acceptance remains open.

11 November UTC: Development 3def10424e integrates HyperDrive Polkadot native storage. CI 43041109458 passed all 52 checks across sync and async on the fresh contracts node, with independent native empty-contract checks. Hosted and retained ledger validation remain open. The verified CouchDB CI batch was prepared on this Development base at ea614f3fc6.

11 November UTC: Development ea614f3fc6 carries the verified CouchDB CI wiring on top of HyperDrive Polkadot changes. The four-file batch matches the focused CI candidate. The combined run executed four generic tests and verified the restricted role plus native restart and MongoDB federation. Hosted HTTPS acceptance remains open.

11 November UTC: the standard Development pipeline 43048587905 passed both CouchDB jobs at ea614f3fc6. The matrix ran four generic storage tests and the restricted-role native policy, and the dedicated job verified restart and MongoDB federation. Hosted CouchDB still requires the restricted HTTPS teaching fixture.

11 November UTC: compared the retained CouchDB WEB4 startup draft with current Development. Development has the native reader and compiler but lacks hosted registration and the Core CouchDB query tariff. The feature draft registers the ordinary CouchDB storage adapter. Its activation calls EnsureDatabasesAsync, which can provision databases and conflicts with the SELECT-style hosted reader contract. Before enrollment, use a dedicated cancellation-aware read-only activation that validates one existing public holon with the canonical bounded document parser. Keep ordinary storage initialization on the existing storage path. No hosted settings were changed.

11 November UTC: CouchDB WEB4 candidate 917347d7f7 adds exact public document activation, guarded HTTPS registration and the matching Core tariff pin. The existing query and activation share bounded identity, revision and payload validation. Remote CI 43085660573 checks seven controlled cases for read-only dispatch, failure shapes and query-only tariff admission. Hosted HTTPS teaching fixture remains open.

11 November UTC: prepared guarded CouchDB release 9f02bc689f on Development eeefb7a0dc, preserving the separate WEB4 avatar verification change. All eight CouchDB reader, test and pin files match the candidate in CI 43085660573. Since the intervening change touches shipping WEB4, qualify the combined image before merging. Supabase and PlanetScale rollouts remain queued.

11 November UTC: CouchDB WEB4 CI 43085660573 passed all seven read-only admission cases. Core Development advanced to exact tested tariff pin 9c0aa3851a. Exact WEB4 image 43086919320 is queued on the current Development-based merge candidate 15e142ee8d with its SHA as the sole requested tag. Parent integration awaits the image result. Hosted teaching data and restricted reader remain open.

11 November UTC: CouchDB release review found the older feature reader filters the server-authorized catalogue before dispatch, while the prepared Development candidate only checked authority on returned rows. The shared identity compiler does not intersect requested keys with the catalogue. Restored the pre-dispatch filter in f343bb8446. Requested cancellation of queued image 43086919320 before it could publish the superseded candidate. Add an out-of-catalogue native-dispatch regression and qualify the corrected release.

11 November UTC: Corrected catalogue-authority candidate is under CI in run 43088383856. The additional regression checks that an out-of-catalogue GUID never reaches the native CouchDB transport. Superseded exact-image run 43086919320 was cancelled.

11 November UTC: exact CouchDB WEB4 image build 43090552807 was dispatched for merge candidate ab4482e254 on Development 214462362b. GitHub qualification 43088383856 and the image build must both pass before Development publication. The native reader, regression and Core pin are unchanged from the qualification candidate.

11 November UTC: CouchDB WEB4 qualification 43088383856 passed all eight controlled checks, including the regression that prevents dispatch of IDs outside the authoritative catalogue. The exact WEB4 image 43090552807 is queued. Development advanced to 335be6013a with BNB wallet tests and documentation. Prepared merge cd75512596 preserves the eight qualified paths and passes Railway manifest and gitlink validation.

11 November UTC: WEB4 candidate 3845f4723c combines the qualified CouchDB reader with direct upstream release downloads for pinned NATS 2.15.2 and Waku 0.39.2. Both archive bytes matched the existing SHA-256 pins. Railway manifest, seven Dockerfiles and parent gitlinks validate. Exact image 43097362380 is queued. Current live AvalancheGo revision a05741af68 warmed to 105/106 ready, with Cosmos and Hedera restored and Nostr unavailable.

11 November UTC: combined WEB4 image run 43097362380 reported startup_failure with zero jobs on attempt one. GitHub accepted a rerun of the same workflow and inputs and returned it to queued. The original source remains 3845f4723c; prepared Development merge 6e1bd3587e preserves all nine release paths on 6507bc8563. No Development push or local build.

11 November UTC: combined WEB4 dispatch 43097362380 reached startup_failure again with zero jobs. A tag-triggered qualification 43098289449 uses the existing image workflow steps, a separate concurrency group, and the exact original checkout and sole image tag 3845f4723c. Diagnostic commit cbcdb3d040 adds the workflow outside the built source. Development publication awaits verification.

11 November UTC: verified both native asset stages using the Dockerfile curl flags. NATS and Waku downloads matched the pinned hashes, and the expected archive members were Linux x64 ELF executables. The exact combined image remains queued in tag-triggered run 43098289449.

11 November UTC: Development 268f29c214 changed the Base provider compiled by WEB4 BootLoader. Combined candidate a209b82e9b preserves those verified Base changes and all nine CouchDB and native-download paths. Dependency and whitespace validation passed. Exact image qualification 43101002944 is queued in the same concurrency group as 43098289449. GitHub returned HTTP 409 to cancellation of the older queued run; retain its evidence and track both handles.

11 November UTC: recovered terminal Buildx logs for superseded CouchDB run 43090552807. Its image publication completed before workflow cancellation. The published SHA tag ab4482e2540697370dd7b526f84802d399c7db6b resolves in the log to sha256:661ab24276983bc5767986003a91af308d86ad4883cc9b598382b6190599810b. The release gate is the newer Base-inclusive image 43101002944.

11 November UTC: GitHub Actions 43101002944 completed successfully. Buildx checked out a209b82e9b920d193f5dc88c687d1fd4e7eafc29 and published its exact SHA tag with manifest digest sha256:da90ab7c74c0f9d0095fe45ed4c90b9503ab7822ca2f072ae96358cd9abb9dea. This image used OASIS_INCLUDE_TEACHING_SIDECARS=false. The later Development integration a614a4377f is a separate Railway job and is still queued. The older WEB4 job 268f29c214 started building. Live CouchDB acceptance still requires deployment and the restricted HTTPS teaching source.

11 November UTC: Railway deployment 1b1ad2f0 succeeded at a614a4377f with the CouchDB reader and pinned NATS/Waku downloads. Both existing NATS and Waku CQL sources became ready for native and federation after warmup. CouchDB remains absent from discovery pending restricted HTTPS teaching fixture enrollment.
