# OpenSearch CQL MVP — native/restart/Mongo-paired demo verified

Updated 30 October 2026. Scoped native/C#/HTTP/browser acceptance passes; full parity remains open.

## Implemented and verified without backend

OpenSearchOASIS explicitly implements ICosmicQueryStorageProvider, with its own
bounded identity compiler binding. Complete trusted catalogue at most100GUIDs,
identity equality/membership/Boolean composition and paging. One real-time native
POST /oasis_holons/_mget; no search/index scan or arbitrary full-text/metadata shape.
Keep OpenSearch's existing rawHolon source and underscore index names (Elasticsearch
uses a different wrapper/index spelling). Native order restored before paging.

Native transport cancellation and4MiB streamed response bound enforced. Every
selected ID accounted for once; missing record is empty, omissions/foreignIDs/
duplicates/wrongindex/itemerror/missingrevision/payload-ID mismatch reject without
partial rows. Authoritative record catalogue governs access; creator mismatch
alone does not revoke an explicitly authorized ID. No distributed snapshot claim.

Common write helper previously ignored HTTP failures. It now validates status,
stagedindex/ID/version/sequence/primaryterm/result/successful+nonfailedshards before
acknowledging any upsert. SaveHolon stages copied payload/ID/providerkeys, publishes
caller identity/IsSaved/count only after that boundary. Failed writes preserve caller
state. Batch stop/continue/error/save counts fixed; manager owns tracking acceptance.

Index initialization now checks HEAD; only404 permits creation. Other errors fail,
creation must acknowledge exactindex/shards, and activation state clears before
setup/failure. No blanket400-as-alreadyexists path. Original constructor preserved;
additional dedicated caller-owned HttpClient overload supports TLS/transport policy.

Existing history flag had no save/history implementation. Holon writes and CQL now
explicitly reject that mode before I/O rather than silently implying versioning.

## Controls and evidence

40passed/0failed/0skipped: metadata5, compiler/admission9, nativeHTTPboundary26.
Provider/unit builds and cache-only restores pass; no new packages/backend/port.
Tests cover malformed/per-item responses, identity/version/index/shard ack errors,
missingdocs, stablepaging/budgets/activecancel, incompletewrite acknowledgement,
batchstop/continue, indexfailure/existing/create paths and unsupportedhistory.
[HTTP tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.UnitTests/OpenSearchNativeBoundaryTests.cs)
/ [query tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.UnitTests/OpenSearchCqlTests.cs).
Raw TestResults/OpenSearchCqlCurrent/controls.trx SHA256
`f53d3b62c01a442b2727a1136aa8b53a1b9d0424f01a0f7d0ce2851ed41f0e1d`.

## Native runtime and next step

One official3.9.0 Linuxamd64 image cached/pinned
`sha256:5c20af6b5cb472a74ba99f663bee3dd03afbeced14b3897b220a87fc75278445`,
image IDf546dde21473d6d15f4fe4e76e10a5da3fc09f6a0799721642344f9abab0737d,
expanded1,701,454,398B. One retained native source now verified. Reuse this image and original source.
[Official Docker/configuration guide](https://docs.opensearch.org/latest/install-and-configure/install-opensearch/docker/).

Original secured3.9.0 distribution, native3/filter2/missing0/page1/budget and
rawHolon JSON/nativeversion/indexUUID preservation across actual restart now pass.
Mongo-paired C#/HTTP/browser5/4 and hub routing also pass. Elasticsearch proof cannot
certify this backend. No hosted/signup/trial/purchase.

Avatar caller-state ownership, older unrestricted point/search/delete helpers,
childgraphs/nativeCAS/history/replication/migrations/unknownwrite recovery/fulltext
translation/fullEFparity remain open. Those older helpers still need full error/
missing semantics audit; this scope certifies only the new guarded query path and
acknowledged Holon writes. Legacy HyperDrive issues documentation only.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).


## Retained secured source and native acceptance

Source `/var/tmp/oasis-opensearch-cql`2,814,787B logical, original cluster
tMsndYSSR9mlwNDoL0A2sA/nodeZ7mJ5J-NQgCH-U-jZxz-eA, distributionopensearch3.9.0.
Original container8efd35dc5b7853e690e100cbfc2da0d9d13db286de592b52121646ccf79e6032,
localhost19201HTTPS,2CPU/2GiB/512MiBheap. PrivateCA/nodeTLSkey/randompassword/
bcrypthash/native securityindex/config/data retained outside TestResults.

Securityplugin enabled throughout. Demo installer disabled in favour of custom
securityconfig/certs, only custom oasis_test account mapped to all_access; automatic
initial securityindex import uses these private files. No well-known demo passwords
or unsafe demo certificates. HTTP chain/hostname and transport hostname validation
remain enabled, transportbound127.0.0.1; anonymousHTTP401 and authenticatedauthinfo
prove expected user/role. Inherited system bcrypt5.0.0/PyYAML6.0.3 reused, no install.
Password hashing stays in memory/privatefiles, never argv/env/logs. Process-only
SSL_CERT_FILE privateCA for .NET, no global trust change or certificate bypass.

Initial setup erroneously called Elasticsearch's /_license endpoint (OpenSearch400)
after security readiness; corrected to OpenSearch authinfo. Original node/cluster
startup records and publicCAhash used for explicit readback, same source/keys kept.
No business writes occurred on that failed setup; firststop143/noOOM was clean.
Subsequent setup1/restart1 match original IDs and retain original intent/acks.

Native fixture journalled original actor/3IDs, three acknowledged rawHolon writes,
receipts/seed/rawJSONhash/nativeversion/indexUUID snapshot. Native initial1/restart1
pass, source-loss/orphan snapshot/pending refuses recreation/replay. Source count
verified after refresh; CQL itself is real-time native multi-get. Own node stopped143/
noOOM/noError, runner37306terminal0, pending absent. Proving integration fixture:
OpenSearchCqlNativeTests.cs. Raw TestResults/OpenSearchCqlCurrent/native-run.log/TRX:
- native-first: `1d6f8edf3b922f0cf23e5f80b5b2e00ba68fc22a1b3cd7db9325470e39cfc4a2`
- native-restart: `dd9993e6354d0dcb92b5ccc16aee8c3df65bd951a553bc99d578dca8ece51884`

```powershell
Scripts/TestHosts/Run-CosmicOpenSearchLinuxDemo.ps1
```

[Security YAML documentation](https://docs.opensearch.org/latest/security/configuration/yaml/)
/ [TLS configuration](https://docs.opensearch.org/latest/security/configuration/tls/).


## Mongo-paired teaching preset

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider OpenSearch -Federated
# Add -VerifyOnly for bounded native/restart/C#/HTTP checks.
```

Browser http://127.0.0.1:5141 shows5physical rows (native3+Mongo2), then4for the
2-ID subset, keeping provider copies/provenance and excluding Tea. C#/HTTP also
reject forged/unknown presets and return503/no partial rows for inactive native
provider. Native backend remains authenticatedHTTPS; teachingHTTP is loopbackonly.
Bounded identity selection, no full-text translation/cross-provider snapshot/
HyperDrive replication/full-parity claim. Shared examples live in Examples/
CosmicCqlDemo/IdentityFederationWalkthrough.cs and IdentityFederationApiHost.cs;
OpenSearchMongoCqlNativeTests.cs proves C#/HTTP usage.

Bounded/interactive runners each5phases/one test passed. Browser5/4 verified,
screenshot TestResults/OpenSearchCqlCurrent/browser.jpg50,394B. Stopbutton/UIshutdown
checked, originalrunner27946terminal0, native rawJSON/version/indexUUID preserved.
Originalsource2,825,369B logical retained; exact cosmic_cql_mongo_opensearch_demo
checked/dropped, pending/ready cleared, own19201/Mongo27039/demo5141stopped
(JVM143/noOOM/noError). Browser55closed. Hub8single/16plans,50paired/100plans,
86paired and3archive rejections pass; listeners unchanged. No newdownloads/environments.
Current interactiveTRX hashes under TestResults/OpenSearchCqlCurrent:
- native-first: `1ec76c02bacea562b1167a1741159b2b0132261821ab077c2adbafb5ac5d82c7`
- native-restart: `b127a8f5c53e7e9dd5d8d94d952b357ad6b3c3f037220c65d7754ce22c1c91d0`
- mongo-init: `0822d3912db88b2c806e915ac21084c48821eaf8d67407fde219b2b30efae50c`
- mongo-federation: `1de641914c720b7cb38d2a27652dc1882cc7acd3f0de706fffba56804d652ffb`
- native-postpair: `bc5c483514c81019ceae59e367c23495b3e7a993c34af6bfed72632f2e2cd021`

Next red provider: Couchbase; continue breadth before deeper parity.
OpenSearchHostedCqlNative accepts COSMIC_OPENSEARCH_HOSTED_CQL_SOURCE with
Endpoint, Actor, three LiveIds, one DeletedId and one MissingId. It uses
the same guarded HTTP transport as WEB4 and checks public root ownership,
native absence, full and subset reads, paging and unchanged document hashes.
Keep credentials in the process environment. The hosted tests and deployment
remain pending. The retained Windows proof is preserved.
The shared WEB4 admission run passed ten focused checks with zero skips at
Core `0205f68`, covering both providers and all native and Mongo presets.
TRX SHA-256 `e61277ee1449a4cb2156be6a9954608f2f7543be7d697b0235315e12b0cd9edb`.
The native HTTPS fixture compiled with 11 warnings and zero errors.
Execution needs an owned source and restricted read credentials.

Offline enrollment binds the reviewed Endpoint and the first LiveIds
entry to the protected HTTPS origin and probe settings. The descriptor
does not contain credentials. The protected reader must pass format
checks, and its actual read scope must be qualified against the owned
teaching index before live enrollment.

The OpenSearch hosted fixture now requires CQL_OPENSEARCH_QUERY_ORIGIN
and CQL_OPENSEARCH_QUERY_PROBE_ID to match the reviewed Endpoint and first
LiveIds entry. It compiled with zero errors, and both mismatch checks
failed before sending HTTP. Live qualification remains open.

11 November UTC: Development OpenSearch CI passed all four generic storage cases. The remaining three tests stopped at missing fixture prerequisites: owned Mongo replica admission, persistent OpenSearch restart state, and the Mongo federation connection. The next CI change should reuse the existing restart and federation tests on isolated state, preserving the generic storage coverage. Hosted CQL remains open. [OpenSearch job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43028699870/job/130549848053)

11 November UTC: published OpenSearch diagnostic `77285ecd91` against Development `73e0d4bb08`. CI reuses the existing secured OpenSearch 3.9.0 provisioner with the runner account as the receipt owner, then runs the existing native test before and after restarting the same container. The diagnostic uses an isolated runner and a private CA. Federation and hosted CQL remain open.

11 November UTC: secured OpenSearch 3.9.0 CI passed one native CQL test before restart and one after restart, zero skips. The tests verified the same cluster identity and original document snapshot over authenticated HTTPS with the fixture CA. CI removed its container. Mongo federation and hosted WEB4 remain open. [OpenSearch restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43035774220)

11 November UTC: OpenSearch federation diagnostic `b4ff3d1e2c` extends the passing secured restart workflow with a disposable Mongo replica set. It runs existing replica admission, SDK/HTTP federation, and the post-federation native source recheck. Hosted HTTPS teaching-source acceptance remains open.

11 November UTC: OpenSearch passed all five isolated CI phases with zero skips, including Mongo replica admission, native SDK/HTTP federation, and a final source recheck. Hosted HTTPS CQL remains open. [OpenSearch federation CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43037028447)

11 November UTC: Development `2df4de082f` contains the verified OpenSearch restart/federation and SurrealDB restart workflows and fixture scripts. Both are manually dispatched. SurrealDB generic storage and federation remain open, as does hosted HTTPS CQL for both providers. [OpenSearch CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43037028447) · [SurrealDB restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43038591878)

11 November UTC: standard OpenSearch CI fixture orchestration is under verification at ba77981977. Both generic and native coverage are retained. Hosted HTTPS CQL remains open.

11 November UTC: the generic and secured native OpenSearch jobs are queued in [OpenSearch fixture orchestration CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43040976654).

11 November UTC: both OpenSearch CI jobs passed together, including the generic storage matrix and secured native restart/federation workflow. Candidate `18d99faa23` is ready for the next Development batch. The equivalent SurrealDB jobs are running. Hosted HTTPS CQL remains open for both. [OpenSearch CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43040976654) · [SurrealDB CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43042182895)

11 November UTC: Development c837fe5ad8 carries the OpenSearch and SurrealDB CI routing verified in runs 43040976654 and 43042182895. Both passed generic storage and native federation. Development pipeline 43043263300 and Railway WEB4 deployment f573b5ac-55d9-4394-a84f-20feb7eaaa87 were queued at inspection. Live acceptance remains gated on restricted HTTPS teaching sources already recorded in ACTIONS-FOR-DAVID.

11 November UTC: OpenSearch generic storage and native restart/federation both passed in standard Development CI 43043263300 at c837fe5ad8. Railway deployed earlier revision 165e8abaee successfully and still has c837fe5ad8 queued. Hosted HTTPS CQL acceptance remains open.
