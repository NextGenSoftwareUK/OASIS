# Waku Store v3 CQL MVP

The read profile uses the official Store v3 exact hash endpoint on
an isolated node. An explicit reviewed catalogue admits up to four
keys. It verifies the returned hash, reviewed pubsub and content
topics, and the protocol SHA-256 over the topics, payload, metadata
and big-endian timestamp. The public holon includes hash, topics,
timestamp and payload size. Payload and metadata bytes stay out of
the projection.

The pinned v0.39.2 Linux fixture runs two real peer-to-peer nodes
with loopback listeners and one SQLite archive. One peer publishes
three synthetic messages and the other stores them. Native REST and
CQL both returned exact 3/2/0. The modified-payload control also
passed, for a total of two focused CQL checks with zero skips.

The initial REST qualification returned HTTP 400 because the fixture
combined exact hashes and topic filters. The pinned API permits hash
selection independently of content filters. The corrected native
request validates the returned topics against the reviewed pair.

The source uses the fixture's fixed loopback address and topic.
Sandbox admission and loopback WEB4 federation are verified below.
A managed hosted Store and live sandbox acceptance remain open.

[Original Linux run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42289148713) ·
[Passing native REST run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42289322076) ·
[Passing CQL run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42289754507) ·
[Pinned native hash implementation](https://github.com/logos-messaging/logos-delivery/blob/fd07b72c370a6b39c69870c087cc0d928d485327/logos_delivery/waku/waku_core/message/digest.nim).

### WEB4 configuration contract

The reviewed teaching descriptor accepts only a canonical sourceOwner and
three distinct lowercase Waku messageHashes. The source retains its fixed
loopback endpoint. Focused descriptor and forged-payload tests passed (2/2).
Evidence: TestResults/WakuStoreCqlCurrent/descriptor.trx, SHA-256 D926E7A290AA059A29F503E61E83CA7EE928473A9C90C11C2A1B3BF73009C51A.
WEB4 registration, tariff and MongoDB federation evidence follows.

### WEB4 admission verification

The CQL_WAKU_STORE_QUERY_SOURCE_JSON setting adds a read-only profile in
sandbox mode. WEB4 admits explicit Waku scope with a dedicated read tariff.
The focused registration and tariff suite passed 28/28.
Evidence: TestResults/WakuStoreCqlCurrent/web4-admission.trx, SHA-256 D3BA8A478BCA338B40661E1E92161849DD46B6326106ADA231AA87654891EE52.
The native fixture and MongoDB integration result follows.

### Native Store and WEB4 federation verification

[Linux CI 42317536191](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42317536191)
passed three native tests and one WEB4 HTTP integration test, with zero
failures or skips. Store exact-hash reads returned 3/2/0. WEB4 returned
3/2 native rows and 5/4 federated rows, preserving source attribution.
A second visitor saw zero private MongoDB copies and all three public
Waku records. The owned test database and MongoDB container were removed.
The test used controlled authentication and admission on loopback.

The runner wrote TestResults/WakuStoreNative/native.trx and web4.trx.
Its console abbreviated the SHA-256 values, so the linked run and exact
test commit e46efdcd0 are the durable evidence. Hosted deployment still
needs a managed loopback Store, persistent reviewed messages, and a
sandbox preset verified against the deployed service.

### Restart and snapshot

[CI 42318687767](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42318687767)
read all three reviewed hashes after restarting the Store. The subsequent
seal step rejected SQLite sidecars. The snapshot now uses SQLite backup
from the closed native archive, then verifies the standalone file and
records its digest. A local smoke test confirmed committed WAL data
survived, the digest matched, and a second seal refused to overwrite
the snapshot. Native snapshot qualification passed in
[CI 42319328016](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42319328016).

The passing run confirmed all three reviewed messages survived restart,
sealed the archive, reran three native checks and one WEB4 federation
check with zero failures or skips, and removed the MongoDB container.
The snapshot and digest receipt were runner-local and disposable. A
hosted rollout needs a separately provisioned and verified source.

### Provisioning handoff

[CI 42320285565](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42320285565)
is the first run configured to retain a qualified source. After the run
passes, verify the artifact is available and unexpired. It contains only
store-qualified.sqlite3 and archive-qualified.json and expires after
three days. Verify the receipt byte count and SHA-256 against the file,
then bind its three hashes to the sandbox teaching owner in
CQL_WAKU_STORE_QUERY_SOURCE_JSON. Keep the source outside Docs.
Hosted readiness still requires a managed loopback Store and an actual
read of all three records before publishing any sandbox preset.

Run the offline verifier on the exact passing artifact before provisioning.
Use the reviewed sandbox owner UUID in canonical lowercase form.

```powershell
python -B Scripts/Sandbox/Verify-WakuQualifiedSource.py <artifact-directory> <owner-uuid>
```

The verifier emits the descriptor only after checking the SHA-256, file
length, SQLite header and three distinct canonical hashes. Local smoke
checks passed for a valid receipt, archive tampering, a duplicate receipt
field and duplicate hashes. The native readiness gate is separate.

The export passed. Artifact 13552413276 contains the exact archive and
receipt from the passing native, restart, and WEB4/MongoDB run. The
downloaded SQLite file is 12,288 bytes and matches CI SHA-256
0bd3c388c5662d3f73a6239555013d4526dd801e8746b58e38c4921f55ec9880.
The offline verifier produced three canonical reviewed hashes. The
archive and receipt are retained outside Git in
LocalState/COSMIC/WakuQualifiedSourceCurrent. The online artifact
expires on 2026-11-10 at 00:54:44 UTC.

### Complete catalogue activation

The WEB4 descriptor now requires successful native verification of every
reviewed record before marking the source active. Previously the first
record could pass and leave a partial catalogue active. A regression test
returns a valid first record and omits the second. Activation rejects it.
Three focused tests passed. Evidence: TestResults/WakuStoreCqlCurrent/activation.trx,
SHA-256 66D83B0D8ABE8CC0B8E46A9D804A1389D754ED064E334C3FF0273BD12C54B16B.

[CI 42321399172](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42321399172)
confirmed complete-catalogue activation against the real fixture, with
four provider tests and one WEB4/MongoDB HTTP test passing. The original
locally retained archive remains the provisioning input.

### Hosted runtime implementation,7November2026

On the old laptop, the original artifact13552413276 was recovered with its
exact archive and receipt hashes. The current environment permits the scoped
source edits that the earlier recovered-laptop command could not complete.
The new native runtime module restores that standalone archive exactly once
into its own mounted directory. Durable intent/acknowledgement and exclusive
creation refuse interrupted, changed and concurrent provisioning. Later
starts preserve native SQLite files and check the full three-message catalogue.
The existing shared supervisor owns the native child and WEB4 shutdown.
Native credentials are limited to PATH/HOME/TMPDIR. The fixed loopback Store,
cluster/topic and C# complete-catalogue activation contract are retained.

The WEB4 Docker source downloads only the original pinned0.39.2 release,
checks its SHA256 and verifies that executable in both provider/final stages.
The runtime remains explicitly enabled by OASIS_SANDBOX_WAKU. Its bundle and
CQL_WAKU_STORE_QUERY_SOURCE_JSON must bind all three qualified hashes.

Fourteen focused controls pass. Combined controlled runtime regression has
66 passes. The accepted Windows native IPFS bootstrap is explicitly excluded
from that run. Its first attempted invocation stopped at the preserved probe
existence guard, before any source read/write or native execution. Existing
Node18 was initially used, then the already-installed Codex Node24.22.1 was
used for the final controlled run. No new runtime installation.

The new retained-source workflow restores original artifact42320285565,
checks the known archive SHA256, starts the native process on a separate owned
working copy, verifies all three hashes, closes it and repeats from the same
working archive. It preserves compact proof, native logs and the unchanged
original input for90days. It does not publish replacement messages or run the
accepted Mongo/CQL source-generation schedule. This native restore/restart
qualification and exact immutable image build remain pending.

Native retained-source run42366567554 passed. Three original messages survived
two starts on the same working archive, with the original input and bootstrap
acknowledgement unchanged. Artifact13571766234 is7029 bytes, SHA256
 af22f8de4dcef1e45e2f6f46033544856a06f195e6ae269ded70a5b808169ed0,
retained until5February2027. Its actual qualification.json was recovered
and hash-checked locally,233 bytes/SHA256
cd91118d3a752c6502cbff1d2bdd9f4fdfd1412c9c7dceb69351d671a49bb5a6.

Scoped candidatec5b5d16e/Core2fdd3e40 is published on the reused
work/cosmic-teaching-release-candidate refs. It preserves the accepted Graph,
PouchDB and earlier native sources. Core adds only the Waku read tariff.
Image42366908944 caught an incorrect exact tar member path before publishing
an image. Packaging now uses the native fixture's proven extraction hierarchy
and installs only its verified binary into the final runtime. No live rollout
or Waku settings were requested. The next exact image must pass first.

Corrected image42367507424 reached the native executable and exposed its
required libpq.so.5 dependency. The pinned upstream release Dockerfile also
declares OpenMP, C++ and GCC runtime libraries. These dependencies are now
installed in the common runtime base so both native qualification and the
final image inherit the same ABI. The original tar error and this dependency
failure remain recorded. No Waku source is enabled on Development yet.
[Upstream pinned runtime contract](https://github.com/logos-messaging/logos-delivery/blob/fd07b72c370a6b39c69870c087cc0d928d485327/Dockerfile).

Native ABI/image qualification42367817374 passed fore0ea3d57, digest
sha256:d12bba06fa03bc9daf2511830a85498114ec4d2f896c272afbc8c14e555ae5de.
Both provider and final stages verified native0.39.2. Activation waits for
the timestamp projection correction described below.

A controlled regression exposed JSON numeric rounding for nanosecond values
above JavaScript's exact integer range. The shipping native hash validation
continues using Int64, while projected WakuTimestampNanoseconds now uses
invariant decimal text. A normal CQL query and shipping stored JSON serializer
preserve9007199254740993 exactly. The regression failed before the fix with
Expected String/Actual Number. All four controlled Waku C# cases now pass,
zero skips. No native fixture or account was opened for these tests. The
updated native CI gate requires five provider cases and one WEB4/Mongo case.

Timestamp-corrected image42368952565 passed for257404f8, digest
sha256:9671e1f694c02f1e6f26b84e7d705ee0585e41c49393be46dd0de822bf585226.
The source-preserved native regression42368859021 attempt2 passed all five
provider cases and the WEB4/Mongo case, zero skips. Native Store3/2/0,
restart/seal and exact owned Mongo container cleanup passed. Its freshly
created runner fixture is a separate regression input. The hosted source
continues using the original archiveSHA256 0bd3c388c5662d3f73a6239555013d4526dd801e8746b58e38c4921f55ec9880.

After fresh live baseline, dependency manifest, source ref and ancestry
checks, Development advanced normally to257404f8. Code deployment
3fcf6d34-e591-4e24-a377-599a33022a4c is queued. The six protected Waku
settings are prepared and await exact deployment/mount verification.

2026-11-07 Waku activation recovery: code deployment
3fcf6d34-e591-4e24-a377-599a33022a4c succeeded. The six original Waku
configuration writes were acknowledged once. Activation28a381bd-187f-4214-9b4d-aa91d6a45860
restored the original three-record archive but the API crashed during policy
enrollment: CqlPublicReadCatalogue imposed a global16-provider limit and
Waku is the17th catalogue. No Waku hosted acceptance is claimed.

Railway rollback to the preceding successful deployment was acknowledged
once; recovery deploymenta6688a2e-84d1-4b83-a88b-cb30616395e3 reachedSUCCESS.
Public health returned200 with the expected browser origin/credentials
headers. The running rollback uses the previous configuration snapshot;
current service variables still match the protected Waku candidate with17
catalogues. Preserve the six write ACKs and bootstrap ACK; do not reapply
or recreate their source. A corrected code deployment must consume that
staged configuration after image and regression qualification.

The first Waku readiness session request received a502 gateway response.
Its original operation7eacfd75-2949-4e5e-b7e7-7d7df5865ca8 and encrypted
response are retained in HostedWakuReadiness-28a381bd-187f-4214-9b4d-aa91d6a45860.
No replacement visitor or private import cohort has been created. Resolve
this response or explicitly reuse an acknowledged existing visitor before
further readiness work. Browser proof remains open.
