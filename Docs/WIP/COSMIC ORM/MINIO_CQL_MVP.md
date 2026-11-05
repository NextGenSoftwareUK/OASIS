# MinIO CQL MVP continuation

5 November2026. Active, no new accepted local/hosted profile.

The real MinIOOASIS provider already uses AWSSDK.S3 and Pascal-case canonical
Holon JSON at oasis-holons/{guid}.json. Bounded CQL now uses the shared trusted
catalogue compiler and native SDK GetObject reads. Missing object is admitted
only for NoSuchKey with404; NoSuchBucket,403and server errors fail without
partial rows. Native/canonical identity, duplicate properties, full response
length, deletion, page and byte budgets are checked. Embedded child graphs are
projected out before materialization while budgeting the complete original data.
1MiB per object/4MiB aggregate/15second deadline, no bucket/object scans.
Historical mode rejects. Query activation reads an explicitly configured live
record with SDK retries disabled; it never provisions or lists buckets. Explicit
endpoint/scheme/credentials/bucket required. Deactivation clears/disposes client
so later activation cannot reuse a disposed SDK instance.

Controlled cases pass16/16, zero skips in controls-v2. This includes strict
native failures, payload truncation, embedded children, authorization selection
and cancelled probe. Original13case result retained. MinIO-derived QSS authority
uses its existing StorageProviderType identity; no ThreeFold/native-hosted claim
is inferred from common S3 transport or inherited capabilities.

## Actual Windows runtime
Old dl.min.io archive endpoint returned410. Official release assets on GitHub
still provide the pinned community Windows executable:
https://github.com/minio/minio/releases/tag/RELEASE.2025-09-07T16-13-09Z

One direct113115136B binary at
LocalState/COSMIC/RecoveryLaptopMinIO20250907/minio.exe, SHA256
`af709e6ba68488404e85acdd22a3030d0f5e56a108d4b27d744f18ceb50861b4`.
Version RELEASE.2025-09-07T16-13-09Z, vendor commit
07c3a429bfed433e49018cb0f78a52145d4bedeb, Go1.24.6 Windows/amd64, AGPLv3.
No Go/JRE/Linux/system install or archive. Runtime/hash/download receipts kept.

Native preflight passed authenticated-loopback service19700 and anonymous root
listing denied403; health200. Only engine metadata initialized, no bucket/Holon
business writes or source IDs. Root teaching credential generated once in local
credentials.json, never printed or committed. Browser disabled. GOMAXPROCS2.
Exact owned PID11824CtrlC0, current log Exiting on signal: INTERRUPT,
19700/19701closed. Reuse this native store and credentials; do not reinitialize
the preflight or generate another environment. Original log/intent/ready/stopped
receipts retained. The source is not a generic S3 or emulator substitute.

## Next step
Read-only native fixture prepared, requires explicit COSMIC_MINIO_CQL_SOURCE
metadata (Endpoint,Bucket,Actor,LiveIds[3],DeletedId,MissingId), fixed owned
loopback19700/bucketcosmic-cql-minio-recovery and separate existing credentials.
It independently observes native raw hashes/ETags/owner/deletion/absence, then
checks CQL3/2/0/page and unchanged source. Native reader build passes0errors/4warnings; no native fixture
execution yet. Next explicit create-only bucket/source admission with per-record
write intents/ACKs, then query/restart, manager/Mongo/HTTP/browser5142 and WEB4
catalogue. Preserve unknown writes; no automatic save replay.
Full storage acknowledgements/CRUD/history/replication/EF parity and hosted
least-privilege/TLS/source admission remain separate gates. Counts102/60/28 unchanged.

## Native source and retained restart accepted, 5 November2026
Initial-5273dda8a1f5450f9d7bbaae6d732dea bootstrap1/1 and nativeCQL1/1 pass,
zero skips. Fixed owned bucketcosmic-cql-minio-recovery was absent before its
journalled SDK create. Four unique canonical object writes use If-None-Match:*
with per-object intent and native ETag/MD5 acknowledgement. Three live, one
soft-deleted, one embedded-child control; fifth key remains absent. No legacy
SaveHolon acknowledgement or general storage certification is inferred.
Source SHA256
`b55f8216bf71530ab3f26269fa6a156d339a28e987cc570b336f4670563ce02f`.
Native CQL TRX SHA256
`c20d5cf018e928c0971509724919572c505136f90f13eb6bba34dc08fc143171`.

Retained-83a7ed8d59424ca19e4fed6e6d228612 passes1/1, zero skips, without source
writes; TRX SHA256
`fa822bb1ea814fe70928fc474dbf78007531be327293a40081ee043d81d5fc62`.
Each run independently checks original SDK raw SHA/ETag/owner/deletion/absence
against immutable per-write ACKs, then CQL3/2/0/page and unchanged observations.
Original credentials are hashed in credential.admission.json and rechecked.
Both exact owned native PIDs1612/10684CtrlC0, current phase INTERRUPT logs and
19700/19701closed. Four runner admission/shutdown guards pass. Source fixture
build0errors/4warnings; first bootstrap build0errors/1120existing shared warnings.
Shared Core checkout54159614ebdb879c8636eed5fae60be97dbbfff3; exact-parent/API
qualification remains separate. All source/ETag/intent/ACK/TRX/logs retained in
one cache; no new runtime/download. Reusable Run-CosmicMinioWindowsNative.ps1
supports Initialize once or Retained with one build/consumer lease.
Next manager/Mongo/HTTP/browser5142 and WEB4 local catalogue, then separate
hosted scoped-reader credential/TLS/enrollment. Counts102/60/28 unchanged.

## Local federation/browser accepted, 5 November2026
First paired-fa22cb63012b497b96771831a41a1618 failed at a copied fixture assertion
expecting anonymous access, after source checks/direct federation and page GET.
The actual authenticated MinIO implementation was correct; no security changes
made. Original failure/log/TRX and temporary Mongo journal retained. Explicit
reconcile-b6367b4a72564b9bb21ab7016bf542ea1/1 verifies original database/actor/
IDs/names/schema against the journal and removes that exact fixture with durable
cleanup intent/ack, no new Holon saves. TRX SHA256
`3e4d7b99936b30892bdbf53a661349018dedc1e31da68bdf1a2b9574dc03a6a2`.
Native source not reseeded. Positive authenticated-page assertion corrected.

Interactive-5921f60f71e6413993499e603ebbca4b completes1/1, zero skips, TRX SHA256
`188d71c2b0bcb8720d1aa8200a3037e9d5a47d21bae1c64b3f57ff96c12159b9`.
Actual browser5142 returns5full/4subset physical rows, both providers and
MovingProviderReads. SDK/manager federation, forged actors/unknown presets400,
inactive provider503/reactivation without partial rows pass. The shared native
observation helper verifies original write ACK hashes/ETags/owner/deletion/
absence before saves and after the last query. Exact temporary Mongo database
verified/dropped/absence checked with cleanup receipts; pending/ready removed.
MinIOPID13200CtrlC0/current native INTERRUPT receipt, MongoPID18960CtrlC12/
storageClosed/shutdownComplete;19700/19701/27029/5142closed. No MinIO source
writes in paired runs. Five runner admission/reconciliation/shutdown controls
pass; latest consumer build0errors/8warnings, initial broad build1140warnings.
Local profile103 accepted. Hosted60/browser28/open32 unchanged; local catalogue
publication follows scoped source commit. Full storage/replication/parity and
hosted restricted credentials/TLS/enrollment remain open.

## Published local catalogue
Websitefe496ad71c02fb226d2ff3df638e15d57f93f4f1/Vercel success publishes card48.
Live225801-byte HTML SHA256
`67ce344ed818e8743a5a8b43372ec53a4def48e9aa35741b94c7ab0b0588432f`
is Git-blob exact. Two guide/fixture links match scoped parent
2d55029988e65c61ab7f318c1a36aaabfb6025b4. Actual public card/expanded launcher
browser verified; ellipsized AX targets covered by exact HTML plus actual5142
native browser proof.113client tests pass. publication.json retained. Local
profile103, hosted60/browser28/open32; no hosted MinIO enrollment claim.
