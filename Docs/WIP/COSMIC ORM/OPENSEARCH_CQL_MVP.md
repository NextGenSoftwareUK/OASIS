# OpenSearch CQL MVP — native/restart verified; paired demo pending

Updated 30 October 2026. Inventory remains red until Mongo-paired/browser acceptance.

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
Next: Mongo-paired C#/HTTP/browser5/4 and hub routing. Elasticsearch proof cannot
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
