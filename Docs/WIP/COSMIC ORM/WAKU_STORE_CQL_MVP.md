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
Hosted HTTP acceptance and public card63 are complete as recorded below.
Browser interaction and wider messaging/ORM contracts remain open.

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

The catalogue-cap correction is scoped to the enrollment guard: unique
canonical supported provider names,32KiB total configuration and128 IDs per
provider remain enforced. Exact candidate7672cd5ea0313dc3494761226766ad84d31fdaa2
changes only that guard, its five focused regression cases and a CI gate.
CI42384061405 tests that candidate; teaching image42384082328 builds it.
No new deployment is authorized by a queued/success-labelled image alone.
Both checks must pass before replacing the recovery runtime.

Explicit verifier option --reuse-acknowledged-readiness selects a retained
acknowledged visitor for discovery only after the Waku deployment/source
fence. It requires existing protected session credentials and forbids
session creation. The original502 operation stays untouched. All28 response
and journal controls passed, including reuse refusal on release drift.
This option does not replace the separate owner/foreign acceptance cohort.

Catalogue correction image42384082328/job128437674194 succeeded for exact
7672cd5ea, teaching runtimes enabled. Immutable digest
sha256:f06d27fb67f13a4911ac955553e9eddfc52e6ff149eeb85fc57f142e9a584707.
All five catalogue controls passed in42384061405, zero skips. Fresh live
configuration and recovery deployment checks matched; remote Development
and candidate ancestry were unchanged. Normal fast-forward advanced
Development to7672cd5ea. Rollout3cdeaf40-4fdb-489c-8696-7319e78d24db is queued.
No settings or archive writes were repeated. Mounted-source inspection and
live acceptance must verify this exact corrected deployment before counting
Waku hosted. Protected preparation and the original502 journal remain.

## Live Development HTTP acceptance — 7 November 2026

Deployment3cdeaf40-4fdb-489c-8696-7319e78d24db isSUCCESS at exact
7672cd5ea0313dc3494761226766ad84d31fdaa2/Core2fdd3e40183e8a5ed81bccee1f30660c91135eb1.
Active instance6fbafe8e-c676-40c5-932f-3c64659b8d51, deployed image
sha256:43659f79c8695c00f456fd7537ca4bb9a6a8bfa9e38ea4e80d9655b04ab9bf05.
Full protected settings, all six original write ACKs and the mounted original
source receipt matched before and after acceptance. No archive provisioning
or settings mutation was repeated. The catalogue-limit fix passed5cases,
zero skips, and its exact image qualification42384082328 passed.

The first hosted cohort passed native3/2 and the real published frontend
preparation flow against HTTP: first2 private Mongo saves, paired5/4,
repeat0new/2existing and exact SQLite/Neo4j replicas2/2. An independent
visitor saw public3 and private0 on Mongo and both replicas. All native
nanosecond metadata remained decimal text. Exactly two save ACKs and an empty
pending-intent object remain. The durable cohort is sealed by
LocalState/COSMIC/HostedQueryAcceptance-WakuOASIS/cql-http-acceptance.acknowledged.json.
Do not replay it. Protected owner/foreign sessions and original journals remain.

| HTTP report | Bytes | SHA256 |
|---|---:|---|
| WakuOASIS-native-http.json |12673|3bd44cd881179f47c75725f00c75cb73c52d797c9280464b7b13a09830fa9c4c|
| WakuOASIS-client-http-flow.json |35794|76af3c39c9e7c8d1e28a865a84271c5ff3b13603eaa9f016495a2ac094fad44a|
| WakuOASIS-isolation-http.json |5988|25ec2041f7e707b38b8e38d9ff2f2095cf544d905652ad16467f8aea09468904|

Reports are under TestResults/HostedCqlIntegrationCurrent. Discovery reused
the acknowledged PouchDB readiness visitor with allow_create=false; its
credential renewal was acknowledged in that original journal. The earlier
Waku502 session intent/response was preserved, never resubmitted. No browser
proof is claimed: updated app control stopped before navigation because it
could not reliably establish the browser URL. Public card publication follows.
Coverage is now119 scoped local/77 live HTTP/28 functional browser.

Public card63 is published on Web4Site main/work-cosmic sourcef854fc2.
All114 client controls pass. Public HTML245065B, SHA256
 a3b8d0d134a6225ca40333d3d50fb8ec60c790748c6ea107a1a885c1df2ed2a0,
is byte-identical to the committed blob. It links this accepted scope and
the real WakuStoreCqlTests.cs example at backendbc6ba3e91. Browser interaction
remains unverified. [Live demo](https://web4.oasisomniverse.one/web4-sandbox.html#cql-waku-store-catalogue).

## WEB4 Development startup incident — 9 November 2026 UTC

MapLibre deployment 435de4e4 at c6f25f76 and the exact Civic rollback
7bb067fa at 88c378c1 both logged that Waku readiness did not return the
three qualified messages exactly once. The rollback reports SUCCESS in
Railway, but a public /api/health request completed TLS and timed out
without HTTP headers. The API is gated on native readiness. The complete
protected service settings still matched the Civic baseline after rollback.

The protected Waku bootstrap archive was inspected in memory only. It
contains all three original messages, timestamped 7 November at 00:54:27
UTC. All were more than 48 hours old at the MapLibre failure at 02:53:34
UTC on 9 November. The pinned native Store v0.39.2 defaults to two-day
timestamp-based retention and the WEB4 launcher supplies no explicit
retention policy. This is consistent with pruning the persisted working
archive. The mounted archive has not been examined, so its exact contents
remain unverified.

Qualify a bounded native retention policy for the isolated demo archive
with the original aged fixture, across startup and restart. Inspect and
preserve the affected working archive before any reconciliation. Restore
only the original qualified messages if loss is verified. Preserve their
hashes and the existing CQL cohort. No MapLibre enrollment settings were
applied. No hosted archive or cohort was recreated during diagnosis.

The task branch correction 254f46216 sets native capacity-based retention
to 100 messages for the isolated Waku catalogue. All 14 local controls
passed. GitHub Actions 42554263377 passed with the exact original retained
archive on initial native startup and restart. The source archive and
provisioning acknowledgement remained intact in qualification. This
qualifies the retention change and demonstrates the original archive is
usable. It does not inspect or repair the live working archive.

Railway now reports both Civic rollback 7bb067fa and its instance
7cd8c611 CRASHED. There is no running instance of that deployment for
the existing read-only SSH inspection. The next rollout must account for
the persisted archive before WEB4 readiness. The scoped recovery should
inspect the stopped working database and its sidecars, preserve it, and
reconcile only the exact missing qualified identities. A full volume
restore would also affect other demo data on the shared mount.

The protected original passed SQLite integrity and contains exactly the
three receipt hashes. Inspect-WakuWorkingArchive.cjs checks the mounted
working file in a read-only SQLite transaction and reports only the total
row count and missing reviewed identities. The original and a disposable
one-missing-row copy both passed with unchanged database bytes. The
temporary copy was removed. The inspector needs a running maintenance
instance with the original WEB4 volume and the reviewed Waku settings.

The offline restore operator now verifies the original against the
protected bundle, refuses schema drift and changed existing rows,
creates an exclusive SQLite backup, and inserts only the reviewed
missing identities in one transaction. A disposable rehearsal verified
exact row recovery, unrelated row preservation, pre-repair backup,
wrong-set rejection and rejection of backup-path reuse. The live
operator requires a mounted maintenance instance with Waku stopped.

### Live archive confirmed empty

Read-only SQLite inspection on redeployed Civic instance 6180b972
(deployment 73c4a855) found an intact working archive with zero rows
and all three qualified identities missing. That redeploy reused the
historical start command, so no repair was attempted there.

The existing Development WEB4 service has a temporary /bin/sleep 3600
start override. Its prior start command was unset. Service-instance
redeploy dd45f721 is building with the current settings. Verify the
actual running process and volume before invoking the offline operator.
Reconcile the override back to unset before the verified recovery rollout.
No provider variables changed and no hosted archive rows have been written.

The scoped candidate 58816e262 applies only the qualified retention
change and its control to the current Development source. Exact image
build 42554927956 is in progress. Development remains at c6f25f76.

Railway maintenance deployment dd45f721 failed before creating a
container. Its builder could not reach the Ubuntu Noble package mirror
over port 80 during apt-get update. The previous Civic instance is also
crashed. No offline repair ran. The temporary sleep command was removed
by setting the service start command to the empty string. Railway's
public API accepted null but left the prior override in place. Readback
confirmed the empty value and no new deployment. Use the qualified
prebuilt image for the next maintenance attempt if the original service
source and volume can be preserved and restored exactly.
