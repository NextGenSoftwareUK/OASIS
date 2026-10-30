# OpenSearch CQL MVP — controls verified; native demo pending

Updated 30 October 2026. Inventory remains red until independent native/paired proof.

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
expanded1,701,454,398B. No native container/source/credential yet. Reuse this image.
[Official Docker/configuration guide](https://docs.opensearch.org/latest/install-and-configure/install-opensearch/docker/).

Next inspect pinned image UID/entrypoint/security initialization, then create one
retained authenticated TLS source with privateCA/customcredentials and security
plugin enabled. No demo-cert/trust-bypass/disable-security shortcut. Record original
cluster/node/index/actor/IDs before acknowledged writes, prove native3/filter2/
missing0/page1/budget/JSON+revision preservation across actual restart. Then one
Mongo-paired C#/HTTP/browser5/4 preset and hub routing. Elasticsearch proof cannot
certify this separate backend. No hosted/signup/trial/purchase.

Avatar caller-state ownership, older unrestricted point/search/delete helpers,
childgraphs/nativeCAS/history/replication/migrations/unknownwrite recovery/fulltext
translation/fullEFparity remain open. Those older helpers still need full error/
missing semantics audit; this scope certifies only the new guarded query path and
acknowledged Holon writes. Legacy HyperDrive issues documentation only.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
