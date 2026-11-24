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
# Elasticsearch CQL MVP continuation

5 November 2026. Accepted scoped local profile101; hosted acceptance remains open.

Real bounded realtime REST document reads use the authorized identity compiler,
validate index/document/canonical IDs and indexed owner/type/deletion metadata,
and reject malformed, over-budget or partial responses. Embedded children are
projected out before canonical materialization; a reproduced interface-graph
failure is preserved in child-diagnostic.trx.34controlled tests pass, including
API-key activation/read. Six runner controls cover log lineage, admission and
native index readiness. Consumer build passes0errors/7warnings.

One create-only source was admitted after native capacity passed unchanged90%
high watermark: initial-2c8768c273c54e3589e954f9bf8be637. Four canonical wrappers
remain version1, including a full embedded-child record and one deleted control;
a fifth ID stays absent. Native bootstrap and CQL3/2/0/paging each pass1/1.
Retained-68f218d994cc4ad6a894f09444fad322 passes without reseeding.
Source descriptor SHA256:
`bc23e3576e1839d6e5b14b21cb021f9d34adf24937871466cf24cc6a148aa6ba`.

The first paired attempt ca18995df69a4363ae7c0c6288773748 failed before Mongo
saves: root node readiness preceded index shard readiness. Its original503 and
logs remain. The runner now uses one bounded native cluster-health wait for the
owned index, green/one active primary/no initializing or unassigned shards,
before any source reads. No failed query retry or threshold bypass.

Interactive-12879b8cb8e645ffabe23201275bf84d completes manager/Mongo/HTTP1/1,
zero skips; TRX SHA256
`04416ed1fe41e7f3cc590d807e548c6e365ebfa248a8a04862a4fe3390c0c846`.
Actual browser5139 returns5full/4subset physical rows with both providers and
MovingProviderReads. Unknown/forged inputs and inactive-provider errors are
checked by the consumer. Exact temporary Mongo fixture removed; all original
Elasticsearch payloads unchanged. Native Ctrl+C exit130/nodeClosed and Mongo
exit12/storageClosed/shutdownComplete verified. No source writes in this run.

Official Windows8.19.24 archive is520,583,121bytes; vendor SHA512 verified:
`a88a6aec0f02c18c24ead95461b564b8318dc4ea1cf354ce05c9e4a822430e84f8f92f7017259b4c6110d35b902925f402a6b264dbbdd9dbc86f8d34917290b9`.
One cache under `LocalState/COSMIC/RecoveryLaptopElasticsearch81924`.
Shared JRE21 passes version/help but actual server fails before readiness:
`jdk.jlink` is required by `org.elasticsearch.entitlement`. The original failure
is retained. Its archive was removed prematurely after version qualification;
the identical official archive was restored and its complete bundled JDK27
extracted. No entitlement bypass or mixed runtime modules. The archive has now
been removed after actual native startup/shutdown qualification.

Non-JDK runtime887files/514571804bytes, manifest SHA256
`a62eac761035e14e3327308430105c9aa7a803a2ecb6cf9862cae972aaa3d568`.
Bundled JDK479files/340254021bytes, manifest SHA256
`89bbce38a7fecebd6294288887796e0816a3b990f3c029c3412f48ea4ebd6519`.
Two real daemon preflights pass, version8.19.24/native product header/owned
cluster verified; exact native PID receives Ctrl+C, node stopped/closed logs,
exit130, both19400/19500listeners closed. No business source/index initialized.
Second preflight uses dedicated `ES_TMPDIR` under the cache; initial default
temp path `C:/Users/David/AppData/Local/Temp/elasticsearch` remains untouched.
Single loopback teaching node has explicit local-only settings; hosted security
and credentials are not certified. Heap128/512MiB, two processors.


Evidence lives under the one runtime cache, with admission, source, intent,
ACK, health, result and shutdown receipts. Shared build used Core checkout
54159614ebdb879c8636eed5fae60be97dbbfff3; exact-parent deployed API qualification
is separate. Local WEB4 card45 is published at website27d58418598ebcb3eed0419b919024c5e5930fd2,
Vercel success. Live218492-byte HTML SHA256
`6864f112ee92082ef3da2cccd81ebc1f4e2106746144ad6556a646c703114f06`
matches the Git blob; both source links match parent7e3b9b973422e26ebf806d592ed5295d9af54763.
Actual live card and expanded launcher browser-verified; link target strings
are ellipsized by AX, covered by exact HTML and native5139 browser proof.
113client tests pass. Compact publication.json retained.
Hosted enrollment/TLS/authentication, general search/vector/CRUD/history,
replication and full EF/NHibernate parity remain open. Hosted60/browser28/open32
unchanged. See [local guide](../../Devs/ELASTICSEARCH_CQL_LOCAL_DEMO.md) and
[cleanup ledger](RECOVERY_LAPTOP_CLEANUP_LEDGER.md).

## WEB4 hosted enrollment preparation

The opt-in read profile requires CQL_ELASTICSEARCH_QUERY_ORIGIN,
CQL_ELASTICSEARCH_QUERY_PROBE_ID and CQL_ELASTICSEARCH_QUERY_API_KEY.
Use an existing owned HTTPS origin, a live teaching probe and an encoded
API key restricted to reads on oasis-holons. The shared transport admits
only an exact canonical GUID document GET at that origin.

ElasticsearchHostedCqlNative accepts COSMIC_ELASTICSEARCH_HOSTED_CQL_SOURCE
with Endpoint, Actor, three LiveIds, one DeletedId and one MissingId.
It uses the same guarded HTTP transport as WEB4 and checks public root
ownership, native absence, full and subset reads, paging and unchanged
document hashes. Keep the API key in the process environment. The hosted
tests and deployment remain pending. The retained Windows proof is preserved.
The shared WEB4 admission run passed ten focused checks with zero skips at
Core `0205f68`, covering both providers and all native and Mongo presets.
TRX SHA-256 `e61277ee1449a4cb2156be6a9954608f2f7543be7d697b0235315e12b0cd9edb`.
The native HTTPS fixture compiled with 148 warnings and zero errors.
Execution needs an owned source and restricted key.

Offline enrollment binds the reviewed Endpoint and the first LiveIds
entry to the protected HTTPS origin and probe settings. The descriptor
does not contain credentials. The protected reader must pass format
checks, and its actual read scope must be qualified against the owned
teaching index before live enrollment.

The native fixture requires CQL_ELASTICSEARCH_QUERY_ORIGIN and
CQL_ELASTICSEARCH_QUERY_PROBE_ID to match the reviewed Endpoint and first
LiveIds entry before it sends any request. The revised fixture compiled
with zero errors. A hosted run remains pending.
