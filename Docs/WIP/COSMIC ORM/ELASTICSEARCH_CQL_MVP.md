# Elasticsearch CQL MVP — native/restart/Mongo-paired demo verified

Updated 30 October 2026. Scoped native/C#/HTTP/browser acceptance passes; full parity remains open.

## Implemented

ElasticsearchOASIS explicitly implements ICosmicQueryStorageProvider, bound to
its own authority through the shared bounded identity compiler. Complete trusted
catalogue at most100GUIDs, identity equality/membership/Boolean composition and
paging. One real-time native POST /oasis-holons/_mget; no index/search scan, arbitrary
content predicate or OpenSearch certification. Restore catalogue ordering before
paging, retain read provenance and reject unsupported query shapes.

HTTP transport/cancellation and4MiB response budget are enforced while streaming.
Every admitted ID must be accounted for once; foreign/duplicate IDs, index changes,
per-item errors, missing revisions and payload-ID/read-authority mismatch reject.
Missing documents are empty reads; no partial result on errors/cancellation/budgets.
Read authority is the supplied authoritative record catalogue: a creator mismatch
alone does not revoke an explicitly authorized ID. No distributed snapshot claim.

SaveHolon copies/stages ID/provider keys/payload and validates native index/ID,
version/sequence/primary term/result and successful/nonfailed shard acknowledgement
before publishing caller ID/key/IsSaved/SavedCount. Errors preserve caller state.
Batch stop/continue counts acknowledged writes only; HolonManager owns tracking
acceptance. Activation/deactivation now maintain active state correctly.

Original constructor retained. Additional (HttpClient, host, credentials...) overload
accepts a dedicated caller-owned HTTP client/TLS policy; no certificate bypass or
client disposal by the provider. Avoids ambiguity with existing host/null calls.

## Proving controls

32passed/0failed/0skipped: metadata4, shared compiler/admission/lifecycle9 and
native HTTP boundary19. Provider/unit builds and cache-only restores pass.
Proving [HTTP tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.UnitTests/ElasticsearchNativeBoundaryTests.cs)
and [query tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.UnitTests/ElasticsearchCqlTests.cs).
Tests inject native responses, not a real Elasticsearch backend. Wrong IDs/index,
shard failure/malformed acknowledgement, incomplete acknowledgement, missing records,
ordering/page/budget/cancellation and batch stop/continue are exercised.
Stale metadata tests corrected to EnumValue.Value; nonexistent GetProviderVersion
assertion removed instead of adding a fake production method.

Evidence TestResults/ElasticsearchCqlCurrent/controls.trx SHA256 `1d8580ed64703d22a68e2714519ed3d5e2d56a541e86760c6c08eecc386d5537`.
Raw output stays in TestResults; no raw committed evidence or package downloads.

## Next

One official Elasticsearch9.5.5 Linuxamd64 runtime is cached by pinned digest:
`sha256:c40a1fb2bc6976821e5625d2329992a1e9c29df576e5955fdcba02421abf0d3c`.
Image ID952a43cd5ab721f7032b2dfd4dcc86a76ba9a791b4c34a223292cf26a832e040,
expanded1,631,515,940B; reuse it. No native container/source/credentials yet.
One retained native source now passes authentication/TLS/freeBasic and real restart
acceptance, preserving original cluster/node/index/actor/IDs. Native3/filter2/missing0/
page1/budget, JSON hashes/native versions/indexUUID unchanged. Mongo-paired
C#/HTTP/browser5/4 and hub routing also pass. No index alias substitution or reseeding.
[Official Docker installation](https://www.elastic.co/docs/deploy-manage/deploy/self-managed/install-elasticsearch-with-docker).

Arbitrary metadata/full-text query translation, child graphs, native CAS, history,
replication/migrations/unknown write recovery and full EF/NHibernate parity remain
open. Avatar save ownership and older unrestricted load/search APIs are outside
this scoped admission. Legacy HyperDrive issues documentation only.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).


## Secured original source and native acceptance

Source `/var/tmp/oasis-elasticsearch-cql`226,420B logical, original cluster
JPqKZ5T1SrCITpgSRGD3sg/nodezoVUraTmQXC4r1zmh5TNzg, localhost19200 HTTPS,
2CPU/2GiB/512MiBJVMheap. PrivateCA/TLSkeys/password/nativekeystore/config/data
retained; no hosted/signup/trial. HTTP and transportTLS enabled; normal HTTP chain/
hostname validation. Anonymous requests401, Basiclicenseactive. ML and geoip
background downloads disabled. Caller process SSL_CERT_FILE points at privateCA;
no global trust change or certificate bypass.

Original setup root endpoint responded before async license initialization;
license404 triggered premature shutdown and30sstop deadline forced137/noOOM.
Readiness now requires activeBasiclicense plus yellow/green clusterhealth, bounded
180s startup/60s graceful stop. Explicit original readback preserved original node/
cluster from its structured startup log and publicCAhash. Same source then passed
two actual starts/stops143; firstforcedstop remains historical, no replacement.

Native fixture created one journalled actor/3IDs with three acknowledged writes,
retained original receipts/seed/indexUUID/JSONhash/nativeversion snapshot. Missing
source/orphan baseline/uncertain pending refuses recreation/replay. Both initial
and restart tests passed1each. Node stopped143/noOOM/noError, runner66063terminal0,
pending absent. Refresh is used only to verify dedicated source count; CQL uses
real-time multi-get, independent of search refresh. Proving native fixture:
ElasticsearchCqlNativeTests.cs in the integration project.

```powershell
Scripts/TestHosts/Run-CosmicElasticsearchLinuxDemo.ps1
```

Raw TestResults/ElasticsearchCqlCurrent/native-run.log and native-*.trx:
- native-first: `2dbf488a5aa9ea08243c85335558c525d30ad17151db02b8d48dd4c2a2293869`
- native-restart: `22c2eaa24a797f402ddad85b897d8b093a074ce9178b56ed56c3dd0b1ee18741`


## Mongo-paired teaching preset

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Elasticsearch -Federated
# Add -VerifyOnly for bounded native/restart/C#/HTTP verification.
```

Browser http://127.0.0.1:5140 shows5physical rows (native3+Mongo2), then4for the
2-ID subset, retaining provider copies/provenance and excluding Tea. C#/HTTP also
reject forged/unknown presets and return503/no partial rows for inactive native
provider. Native Elasticsearch stays HTTPS/Basic-authenticated; teaching HTTP is
loopback only. This is bounded identity selection, no full-text translation,
cross-provider snapshot, HyperDrive replication or full-parity certification.
Shared C#/HTTP code is in Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs
and IdentityFederationApiHost.cs; ElasticsearchMongoCqlNativeTests.cs proves usage.

Bounded and interactive runners each passed5phases/one test each. Browser5/4
verified, screenshot TestResults/ElasticsearchCqlCurrent/browser.jpg50,360B.
Stop control exercised, UI shutdown message checked, original runner45780terminal0.
Original JSON/nativeversion/indexUUID preserved, source227,096B logical retained;
exact cosmic_cql_mongo_elasticsearch_demo verified/dropped, pending/ready cleared,
own19200/Mongo27039/demo5140stopped (JVM143/noOOM/noError). Browser54closed.
Hub8single/16plans,49paired/98plans,84paired/3archive rejections pass; listeners
unchanged. No new downloads/environments; original source and private keys retained.
Current interactive TRX hashes under TestResults/ElasticsearchCqlCurrent:
- native-first: `a9a7f2625fb95ad50eab2001fbd03b5c664453369c1bd4d0967b1d17a1e1104d`
- native-restart: `260d16edd384cf395f7c717115c5b7998a2a56980ed28f8b12355877debab9f2`
- mongo-init: `e2a1e6f60299788c58219d93ac820b1e310f45c3498a5dd1e0fd8701b1433761`
- mongo-federation: `65b78e2d77953f137ff9dfc20b53488dbd5d7189f3413cede619e6edad02622f`
- native-postpair: `eb9711d79895caf9c4f19ffe4145933ac826043ed97f963660f5c48512a8409b`

Next red provider: OpenSearch; use its own native proof, no compatibility inference.
