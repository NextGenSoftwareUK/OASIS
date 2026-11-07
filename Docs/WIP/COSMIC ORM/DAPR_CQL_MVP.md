# Dapr CQL MVP

Current7November: Linux restricted-read/write-denial/WEB4 native proof passed.
Rebuilt upstream native IO now loads and passes the synchronous AOF write probe
in42416939381; its full durable restart also passed. Stronger shared
configuration/default-writer-disabled recovery also passed in42417629733; no hosted Dapr source,
startup flag, visitor enrollment or public card is enabled. Coverage119/91/28.


The read-only profile uses the existing Dapr .NET SDK to request strongly
consistent reads of `oasis/holon/{id:N}` from one named state component. It
uses an explicit IPv4 loopback sidecar endpoint, authenticates with a sidecar
token and caps gRPC replies at 1 MiB. The shared query profile checks public
visibility and canonical identity before paging. The query backend rejects
listing, writes, deletes and non-Holon reads.
[Dapr .NET client](https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-client/dotnet-daprclient-usage/).

Six focused tests passed with zero skips at Core
`79405fdb497834f275ef10edeb7d4b3b2c47ee38`. The run includes four CQL and
endpoint controls and two storage index regressions. TRX SHA-256 is
`2adfe17177d0204e3bf1e512993854692f0cf03351158570a8eaf98a3360edd4`.

Native verification passed on a pinned portable Dapr 1.18.9 sidecar and a
dedicated in-memory SQLite state component. The synthetic fixture returned
3/2/0 and its values and ETags were identical before and after the reads.
The native log SHA-256 is
`eeb88d1f7b32f6b76564d8ffafc8bde3993a10b0d2a8332ddfcb1230c904269f`.
The sidecar stopped, ports 54861â€“54863 were clear and no database file remained.
Hosted WEB4 enrollment
requires a co-located sidecar and a reviewed state component and remains open.
LocalFile keeps the first live Development rollout slot.
[Dapr SQLite component](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-sqlite/).

## Development WEB4 candidate

Opt-in settings are `CQL_DAPR_QUERY_STORE`, `CQL_DAPR_QUERY_GRPC_ENDPOINT`,
`CQL_DAPR_QUERY_PROBE_ID` and the protected `CQL_DAPR_QUERY_SIDECAR_TOKEN`.
The catalogue binds the named component, canonical IPv4 loopback endpoint
and first public ID. All 22 protected-settings tests passed, including
component and port substitution checks. Seven Dapr WEB4 admission checks and
seven Fastly regressions passed without skips at Core
`7ee05c39f2415d3da7ed41acb6514ff6dc2db87d`. Admission TRX SHA-256 is
`f1d76919f67201c6d056a3775231274fe5ead475b5e20c9d109fefee53de2c19`.
Candidate preparation also checks that the protected baseline has a valid
sidecar token. The token stays outside the reviewed catalogue and settings diff.

The sidecar must run in WEB4's network namespace and bind its application
API to loopback. Dapr's state API allowlist covers the state API group,
which includes mutation methods. Verify a dedicated backing store and its
write-denial policy before exposing this profile. Verify the actual sidecar
and native reads on the reviewed WEB4 instance before enabling visitor
settings. [Dapr API allowlists](https://docs.dapr.io/operations/configuration/api-allowlist/).

The Windows Dapr 1.18.9 SQLite component failed to start on a synthetic
database reopened with `mode=ro`. The second run reproduced SQLite disk I/O
error 3850 during the component ping after the writer had shut down cleanly.
It never reached the read or write-denial assertions. The failure log SHA-256
is `76daa1c4052e44efc38b91b1937bf97bf9ce985227d3f64703e5dd4ebed7dcdf`.
Keep hosted write denial open until a reviewed backing component passes it.

The local `state.redis` alternative passed with the existing Garnet 2.2.5
runtime. A dedicated writer sidecar seeded a synthetic record and shut down.
A second sidecar used a restricted Redis ACL principal and read the record
with its ETag. The attempted update returned Redis `NOPERM` through the
sidecar, and the value and ETag remained identical. The result SHA-256 is
`78db3be203f637125ae4cf0c37ad3e19ccad926c03c1d135870f58932a811e04`.
The follow-up run passed native gRPC CQL 3/2/0 through that same restricted
sidecar at Core `7ee05c39f2415d3da7ed41acb6514ff6dc2db87d`. Values and ETags
were unchanged. The native CQL log SHA-256 is
`b0e5e321d3df08f010ffa7fd2a874b13aa1d24fb25deed43c906fd09c0ba1489`.
The actual Development backing store, sidecar and visitor route still require
verification.
[Dapr Redis component](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-redis/).

The loopback HTTP acceptance passed. It runs the shipping CQL
endpoint with the real Dapr registration, profile registry, protected public
catalogue and prepared read execution. The fixture supplies controlled bearer
authentication and usage authorization. The test checks exact public IDs and
provider receipts for both the three-record and two-record requests. The
initial run and the repository fixture runner each passed one test without
skips. The repository runner TRX SHA-256 is
`e93ac939be51b939ba9768498799fe229a51f5f6805eeeecfae10019eb7c8471`.
Hosted authentication, browser verification and federation remain open.

The repository runner is
`Scripts/TestHosts/Run-CosmicDaprWeb4Native.ps1 -Daprd <exe> -Garnet <exe>`.
It validates the pinned Dapr 1.18.9 and Garnet 2.2.5 binaries, builds under
the shared lease, and verifies that the native HTTP test ran and passed
without skipping. It uses synthetic loopback data and leaves compact logs
in this checkout's qualification directory.

## Linux hosting qualification â€” 7 November 2026

The six-reader batch is accepted/published; Dapr is the next independent native
hosting gate. Existing Windows runner pins Windows executable hashes and uses
Windows process creation, so its native result cannot certify Linux deployment.
Keep that original result intact. No native Dapr fixture has been replayed here.

[Linux runtime pins](../../../Scripts/TestHosts/dapr-linux-runtime-pins.json)
record the official Dapr1.18.9 Linux amd64 archive (64024904B) and Garnet2.2.5
Linux x64 framework-dependent archive (5694440B), including release digests.
These were read from official GitHub release metadata, without downloading
archives or creating a local runtime environment. Native Linux execution remains
unverified. Next: one pinned cloud-runner fixture proving restricted reads,
write denial, unchanged values/ETags, WEB4 native3/2 and clean owned shutdown;
then durable hosted source/restart and Mongo federation before enrollment.

[Dapr release](https://github.com/dapr/dapr/releases/tag/v1.18.9),
[Garnet release](https://github.com/microsoft/garnet/releases/tag/v2.2.5).

The native fixture is now shared as `run-cosmic-dapr-web4-native.py`; the
existing PowerShell entry point selects it and retains both exact Windows
binary hashes. Linux requires the pinned archives and exact extracted executable
bytes/locations. Installer extraction rejects links, traversal, duplicate
executables and expanded-size overflow. Five offline controls pass; no native
Linux result is claimed by those tests.

`qualify-dapr-linux.yml` runs the pinned installer only on an ephemeral Linux
runner, builds the actual HTTP host under one provider build lease, then runs
the same native writer-shutdown/restricted-reader/write-denial/unchanged-ETag
and WEB4 assertions. It retains only compact TRX/result evidence for30days,
not archives, ACL credentials or runtime logs. No Development rollout is wired
to this qualification job. Terminal native proof remains required.

## Linux native qualification passed

Exact task source02b15cc56 passed the [Linux qualification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42412751967),
job128524212961. Actual pinned Dapr1.18.9 and Garnet2.2.5 archives installed
and verified. Build succeeded with0errors (3201existing warnings). Native reader
ACL allowed reads, denied a write with NOPERM, and retained identical value/ETag.
The fixture required its single actual WEB4 HTTP test to pass1/1without skips.
It exercised registered Dapr native3/2 through the shipping endpoint with
controlled test authentication/metering. Owned processes stopped and ports cleared.

Remote compact artifact13590146953:1698B, SHA256
`f006ad2632e1b1605c6780638bd044e582a2d4a5118686e86209d1c62ceb1f41`,
expires7December2026. No artifact/runtime archive downloaded to this laptop.
Durable restart, live visitor authentication, Mongo federation and actual hosted
sidecar enrollment remain open. Counts119local/91live HTTP/28browser unchanged.
Next: extend the dedicated native fixture to persist acknowledged records and
verify a backing-store/sidecar restart without another seed operation.

The next qualification extends the same owned fixture with synchronous AOF
commit acknowledgement and bounded log pages/segments. After the one writer
stage shuts down, the restricted reader checks every original value/ETag and
WEB4 queries. Both backing store and sidecar then restart in recovery mode;
no writer or seed operation exists in that phase. All four original records
(including the tombstone), write denial, and a second1/1WEB4 test must pass.
Both TRX reports remain separate; the earlier passing artifact is preserved.
Recovery flags were checked against [pinned Garnet2.2.5 options](https://github.com/microsoft/garnet/blob/v2.2.5/libs/host/Configuration/Options.cs).
This stronger restart proof is pending execution, not inferred from syntax.

Hosted image packaging is prepared in the task Dockerfile using the same
`dapr-linux-runtime-pins.json` and integrity installer as qualification. Only
the Dapr binary and Garnet net10 runtime directory enter the final image;
Python and downloaded archives stay in the separate build stage. Version
commands are required at image build. This stage has not been image-qualified
or deployed. No startup flag, source seed or visitor enrollment is enabled.
Hosted source creation must be an explicit journalled provisioning operation;
normal startup must only recover existing acknowledged records. Preserve the
live six-reader release and source generations while implementing that contract.

Restart qualification42413413865/j128526185779 FAILED at Garnet initial startup,
after a successful host build. Trace reached `backing_store('initial')` before
the writer sidecar or seed call; no native record mutation or HTTP proof occurred.
The prior passing nonpersistent Linux proof remains valid in its narrower scope.
No TRX/result artifact existed for the failed restart attempt. Do not call this
restart certified or silently remove its failure.

The fixture now retains a bounded native diagnostic with all generated credentials
redacted before tail truncation. Six offline integrity/diagnostic controls pass.
A startup-only preflight runs before the expensive host build and performs no
seed, sidecar call or WEB4 query. This next run diagnoses the actual server error;
no persistence flags are guessed or removed to bypass the failed invariant.

Startup diagnostic42414093417/job128528313952 identified the root cause:
`LogDir specified without enabling tiered storage (UseStorage)`. Garnet exited0
without opening its listener. The preflight stopped before host build or seed.
The fixture incorrectly supplied the main-store `LogDir` while using an in-memory
main store with durable AOF. The correction removes only that invalid setting;
explicit `CheckpointDir` still owns AOF persistence. Synchronous commit waiting,
strict recovery and original-value/ETag comparisons remain required. No source
is recreated in recovery. The redacted diagnostic artifact is retained remotely.

Corrected run42414245421/job128528770620 passed startup preflight and host
build, then FAILED with TimeoutError during the initial Dapr state POST.
No HTTP acceptance or restart stage ran. Original seed outcome remains unknown;
there is no automatic resend and the failed attempt is not promoted to a pass.
Its startup-only artifact13590938195 is254B, SHA256
`1ddb496022a851fb2bd0bbced1ea6fc0465c7c46debe48da64c625b879851273`.
The timed-out fixture was confined to the ephemeral CI runner; live sources and
accepted cohorts were not involved.

Failure capture now covers every owned native process, with bounded redacted
logs and seed intent/ACK state. Every subsequent explicitly isolated fixture has
a unique canonical GUID prefix; the actual C# native test requires that exact
prefix from its runner. Recovery within one fixture retains the same identities.
This prevents another diagnostic fixture from reusing the failed attempt's
record identities. Timeout and persistence flags remain unchanged pending
native server evidence; no success is inferred or retry hidden.

Diagnostic42415058067/job128531230929 FAILED after startup/build passed.
Redacted native logs show the object-store HSET path entering AOF commit and
failing in `AllocatorBase.device.WriteAsync`; the Dapr caller timed out.
The leading device exception was omitted by tail-only truncation. Its original
seed intent is unacknowledged; preserve the uncertain outcome, never resend.
The connector's job snapshot was stale; direct GitHub job timestamps established
terminal failure. Use fresh REST state for this job's subsequent observations.

Diagnostics now retain leading error lines as well as the bounded tail, after
credential redaction. Seven offline integrity/diagnostic controls pass. A direct,
journalled single-key HSET probe on its own unique diagnostic source now runs
before the host build. It exercises the same native AOF write path without Dapr,
so a storage failure can be diagnosed without rebuilding WEB4 or resending the
uncertain Dapr request. Only that probe's own intent/ACK and redacted logs are
retained. Original live sources and accepted cohorts remain untouched.

Native AOF probe42415866369/job128533697756 identified the concrete root cause:
DllNotFoundException for mandatory `runtimes/linux-x64/native/libnative_device.so`.
The probe failed before the host build or Dapr seed; its own native control write
was unacknowledged and is not resent. The standalone archive did not supply the
library at the loader's expected location. This was a packaging defect, not proof
that synchronous commits should be disabled or the timeout enlarged.

The manifest now pins the required native library from the same official Garnet
v2.2.5 commit `8d007138208ce030eb10a50c80e636b1678b83e4`:2210897B, Git blob
`4eaa89279f0a83be63a5829e26faba693420d2c1`. The installer always places it in
its mandatory runtime location, verifies size and complete Git-object identity,
and requires a successful native load before any source write. Required OS
libraries `libaio1t64`/`liburing2` are declared in CI and the runtime image.
No alternate storage device, optional symbol or silent recovery is added.
Eight offline integrity/diagnostic controls pass. Corrected native proof and
image qualification remain required. No native library was downloaded locally.
[Exact upstream native source tree](https://github.com/microsoft/garnet/tree/8d007138208ce030eb10a50c80e636b1678b83e4/libs/storage/Tsavorite/cs/src/core/Device/runtimes/linux-x64/native).

Mandatory-load qualification42416231682/job128534813817 FAILED before any write:
upstream prebuilt native library requires `libaio.so.1`, while the target OS
provides the t64 ABI. The fail-fast load check exposed this instead of allowing
another uncertain source mutation. No compatibility symlink is introduced.

Packaging now builds the mandatory native library from the exact clean upstream
Garnet commit8d007138208ce030eb10a50c80e636b1678b83e4 using its own CMake
`native_device` target, Release mode and USE_URING=ON. A sparse public-source
checkout and two compiler workers stay on the ephemeral runner/build stage.
The resulting library is linked against the target OS's actual development
libraries, placed at the SDK's mandatory path and SHA256-bound to a source/build
receipt. Native loading remains mandatory before source writes. CI and Docker
use this same installer/recipe; compiler/source/archive directories do not enter
the final runtime image. Eight offline integrity/diagnostic controls pass.
The rebuilt native AOF probe and full restart proof remain pending execution.
[Upstream CMake contract](https://github.com/microsoft/garnet/blob/8d007138208ce030eb10a50c80e636b1678b83e4/libs/storage/Tsavorite/cc/CMakeLists.txt).

Source-build qualification42416692352/job128536212384 reached the upstream
native_device compilation, then FAILED on missing `uuid/uuid.h`; no probe or
Dapr save ran. Declared `uuid-dev` in both CI and the isolated Docker build stage.
The native build's source/SHA256 receipt is now included in compact qualification
artifacts, alongside the separate native results. This does not include source
checkouts, compiler outputs, runtime archives or credentials. Native and image
qualification remain open until the corrected build and tests pass.

## Shared hosted lifecycle configuration

`Docker/DaprRuntime/contract.json` owns native ports and AOF parameters.
`configuration.cjs` generates the actual Garnet arguments, Dapr arguments,
component YAML and separate provisioning/reader ACLs. Six configuration controls
pass: strict durable recovery, explicit lifecycle selection, reader-only permissions,
credential-injection rejection, loopback endpoints and distinct writer credentials.
The native fixture now consumes these same generators through Node stdin/stdout;
credentials stay out of command arguments and console output. During recovery
it disables the default writer and authenticates readiness as the reader.
This stronger reader-only recovery is pending cloud execution. The final image
copies the shared configuration, but no startup path invokes Dapr yet.

## Durable Linux restart proof passed

[Run42416939381](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42416939381),
job128536974684, sourcef90c23b74 SUCCESS. Compiled native IO loads and its AOF
probe is acknowledged. The actual Dapr source is seeded once; all four original
records and ETags survive backing-store/sidecar restart. Restricted reads,
write denial and WEB4 native queries pass before and after restart.

Artifact13591404960 inspected in memory:4920B, SHA256
`afded8becb041ab9d3fe5810aaa2557baed056703a42f3bf9a3c7f2ffb3536b1`,
expires7December2026. Initial/restart TRX each contain1passed/1total. The single
four-record seed intent/ACK match. Build receipt matches pinned source/recipe,
compiled native library SHA256
`4ec8298481a7ce929f2245e6a67d3b733a2a4f550c59c96ccecc9a7cd046422d`.
No ZIP or extracted copy saved locally; compact qualified receipt retained.

This baseline used the provisioning backing-store ACL with Dapr's restricted
reader. Stronger recovery with default writer disabled and shared configuration
is running separately in42417629733/source0c5800169. Hosted first-generation
provisioning, image qualification/enrollment, Mongo federation and visitor/browser
acceptance remain open. Coverage119local/91live HTTP/28browser unchanged.

## Reader-only recovery and hosted helper

Shared-configuration qualification42417629733/job128539040599 SUCCESS,
source0c5800169. Native AOF writes, actual Dapr restricted reads/write denial,
original values/ETags and both WEB4 query stages passed with the recovery ACL's
default writer disabled. Seed ran once. Remote artifact13592083986:4915B,
SHA256`5f836937bc7ed8b0fad1bcf5e3599cb04052ac1bafde2fff02998e1bfbc851e6`,
expires7December2026. Earlier baseline and failed attempts remain retained.

`host-sandbox-dapr.cjs` now implements recovery-only service configuration.
It requires original acknowledged source/catalogue/owner/token binding, exact
private ACL/component files and an existing bounded native store. It never
creates a missing source. Reader-authenticated Garnet readiness precedes Dapr
health and exact original values/ETags. Source queries have bounded responses,
explicit cancellation and no retries. Thirteen controlled configuration/runtime
checks pass; those checks do not establish hosted or native SDK acceptance.

The next real Linux fixture tests this shipping helper against the running
recovered source before its second WEB4 query test. It also explicitly rejects
the original provisioning writer's authentication. Source credential files stay
in the owned fixture with restrictive modes and never enter artifacts; only
runtime-source-proof.json is retained. The image copies the helper, but the
supervisor/entrypoint does not enable Dapr. Journalled hosted provisioning,
image/runtime qualification, settings enrollment, federation and visitors remain
open. No live Development settings or existing source/cohort was changed.


### 7 November: hosted recovery fixture filename correction

Run42418683499/job128542212746 failed before the hosted-helper/restart gate: the fixture wrote users.acl while the shipping contract hashes and opens reader.acl. Initial native source save was acknowledged and initial WEB4 query passed; this is not a completed recovery proof. Corrected the fixture to use reader.acl throughout. A new run uses a unique source prefix and does not replay the prior source. The helper now shares bounded observation/startup functions for subsequent explicit provisioning;13 controlled configuration/runtime checks pass. Native qualification remains required.

Failure artifact13592882298:4903 bytes, SHA256 8d2c88f4a309703eedd2623b6b85744ae1864df7f0448afcfd53432caf915fe1.


### Explicit creation journal shared with native qualification

Docker/DaprRuntime/seed.cjs now owns the single Dapr bulk-save path used by the native fixture: validated four-record authority, exclusive intent, payload hash, file sync (plus directory sync on Linux) before one bounded POST, exclusive acknowledgement only on204. An existing intent prevents resend after both success and unknown outcome. Three focused controls pass; these are lifecycle controls, not real SDK proof. Sourceea3d78835 is currently qualifying the preceding ACL correction in run42419782648. The shared seed path requires its own exact-source native qualification. Full hosted provisioning/process lifecycle/image/enrollment remain open.
