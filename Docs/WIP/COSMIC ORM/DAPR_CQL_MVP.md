# Dapr CQL MVP

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
The sidecar stopped, ports 54861–54863 were clear and no database file remained.
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

## Linux hosting qualification — 7 November 2026

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
