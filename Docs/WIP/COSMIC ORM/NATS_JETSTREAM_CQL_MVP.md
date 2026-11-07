# NATS JetStream hosted CQL work

Updated 7 November 2026. Hosted acceptance remains open.

The existing NATS.Net3.2.0 provider, bounded CQL reader and public-record
profile are retained. Historical recovery-laptop evidence establishes native
JetStream2.15.2 storage/CQL3/2 and local WEB4/Mongo5/4 with privacy checks.
Do not replay those accepted fixtures or overwrite their records/journals.
Existing runtime and probes remain under
LocalState/COSMIC/RecoveryLaptopNats2152; local HTTP state is under
LocalState/COSMIC/NatsMongoHttpCurrent.

The existing public-profile configuration and publisher accept an explicit
remote TLS endpoint with an existing bucket. Its source-bound TLS/read-only
qualification receipt is still required. No external endpoint or new account
has been assumed, and that enrollment has not been applied.

## Owned sandbox source

An explicit owned factory now binds the source to
tls://127.0.0.1:24222, bucketcosmic_cql_public and readercql_reader. It requires
the enabled sandbox and a43-character base64url reader password. The SDK
uses TlsMode.Require and the dedicated CA file
/data/COSMIC/SandboxNATS/ca.pem. Certificate verification stays enabled.
The existing remote parser still rejects loopback endpoints. No failure-time
alternative protocol, endpoint or credentials are selected.

TLS options flow directly into the existing NATS.Net connection; key-value
reads continue using GetStoreAsync for an existing bucket. A source stays
inactive until activation verifies every enrolled public record and owner.
The pinned package XML confirms NatsTlsOpts.CaFile accepts a PEM string/path.
[SDK security](https://nats-io.github.io/nats.net/documentation/advanced/security.html) ·
[SDK TLS API](https://nats-io.github.io/nats.net/api/NATS.Client.Core.NatsTlsOpts.html) ·
[NATS subject authorization](https://docs.nats.io/learn/security/authorization).

Nine boundary/admission cases cover sandbox opt-in, inactive construction,
plaintext/foreign endpoints, reader credentials, another bucket and the four
native/Mongo presets. CI run 42394807859/job128469782521 passed all nine cases with zero skips
against 5ee6d08a4123f48eb75d5885306c5e75e10ddfbb; no native TLS acceptance
is claimed from these controlled cases. No NATS settings, hosted source,
credential/certificate or new local runtime/environment has been created.

## Remaining gates

- Boundary gate passed: artifact13583217752, 2480 bytes, SHA256
  88fddffe56601994af4abc4dd94271be773b1667d689d9885e8177d6dab9ab4a.
  The native TLS source gate below remains open.
- Qualify a real Linux TLS native source using restricted subject permissions,
  trusted CA verification and the existing real SDK. Verify native3/2, denied
  writes/foreign access and unchanged source before any hosted enrollment.
- Provide the owned runtime/volume lifecycle with exclusive source generation,
  intent/ACK and restart checks; retain its compact qualified artifact.
- Connect the explicit owned mode to sandbox registration and a protected
  publisher, preserving all accepted providers. Qualify the exact image.
- Batch the verified Development rollout, then verify mounted source/credentials,
  live native/Mongo/privacy/replica outcomes and published demo instructions.
- Complete browser verification separately. Full provider/ORM contracts remain
  part of the programme after the breadth-first MVP.

The native TLS qualifier now builds the real SDK consumer under the shared
build lease in an isolated Linux CI runner. It downloads the exact native
2.15.2 archive with its release SHA256, seeds one new owned synthetic source,
and requires real CQL3/2, explicit permission-denial events, typed TLS
authentication failure for an untrusted CA, and unchanged values/revisions.
The server stops before native-store archival. Partial source journals remain
on failure; private keys, server credentials and raw logs are excluded from
artifacts. This gate passed as recorded below; hosted enrollment remains open.

## Real Linux TLS qualification accepted

Run [42396383665](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42396383665),
job128474549330, exact source a95f0a07368591a86afff5a4136025b813880c4a:
real native2.15.2 and SDK3.2.0 passed CQL3/2, explicit server permission
violations for writes and foreign-bucket access, typed TLS authentication
failure with an untrusted CA, unchanged values/revisions, and stopped native
server before archival. Consumer build had zero errors and1150 warnings
from its dependency graph; this is scoped proof, not a warning-free build.

Retained artifact13583577000 is4296B, SHA256
5a96acfd7a032b3078ff45d3d3d6bcd214394144e1ba05e8c6d1e9f63567750b,
expires5February2027. Native store archive2011B, SHA256
a9daaeef39473742cf869b0a9673686523f2771766815f07dbd3c5ba7f29ad37.
Source JSON SHA256
5a8db3e17c3240289cbbb4e521cb63f8f1fb051722b8063e70664794f957709b.
The original source/save ACKs, native archive, qualification and exact CI
receipt are retained under LocalState/COSMIC/NatsOwnedTlsQualifiedCurrent.
Ten retained files total4110B; the downloaded zip was verified in memory.
Do not rerun or reseed this accepted source. No credentials/private keys
were retained. The next gate is owned hosted startup and restart verification,
then exact image qualification and protected enrollment/live HTTP acceptance.

Owned native-file placement is implemented in Docker/host-sandbox-nats.cjs.
Thirteen controlled runtime checks pass using cached Node24: source/descriptor
binding, byte hashes, safe paths, duplicate keys/entries, exact native streams,
exclusive first placement, partial-state refusal, no replacement, missing-file
refusal, and preservation of native-owned working changes on restart.
This module is not yet wired into hosted process supervision or deployment.
Remaining runtime work: TLS credential/certificate persistence, restricted
native configuration, bounded readiness and exact-source SDK activation,
real native restart qualification, image packaging and protected publisher.

NATS service configuration now provisions a persistent dedicated TLS identity
once with intent/ACK, verifies its certificate/key/config hashes and validity
on restart, and refuses partial provisioning or credential changes. The native
configuration exposes only loopback24222 and one restricted cql_reader user;
no writer account is shipped. Its bounded readiness uses actual NATS INFO,
verified TLS, reader authentication and PING/PONG. Only connection-refused
startup polling is allowed; TLS/auth/protocol errors fail immediately.
Nineteen focused controls pass, including a real certificate/key persistence
test using the existing Git OpenSSL executable. Native process/restart CI,
shipping image integration and live enrollment remain unverified and disabled.

Prepared a separate restored-source restart CI gate. It downloads retained
artifact13583577000 from run42396383665, verifies the exact descriptor/archive
hashes and accepted qualification, transports only bounded native files,
and invokes the shipping runtime module twice against one restored source.
A separate read-only SDK consumer checks native3/2 and both explicit permission
denials on both starts, then compares every record value hash and revision.
No writer credential, native source generation or SaveHolon call exists in
this gate. It requires unchanged TLS identity and clean shutdown for each
start. The original seed qualification workflow is not replayed. This new
restart gate is prepared but not yet accepted; deployment remains disabled.

## Real restored-native restart accepted

Run [42397239148](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42397239148),
job128477134308, exact source3ba58eca22c4bcb8e05f406d8f7076c9623e9dc8,
passed. Original accepted native archive restored once, no source seed/save.
Shipping runtime readiness authenticated the restricted reader over verified
TLS on both starts. Real SDK CQL3/2 and explicit write/foreign permission
violations passed before and after restart. Exact record hashes/revisions
and TLS identity stayed unchanged; both native processes stopped cleanly.
Nineteen runtime controls also passed on Linux. Consumer build zero errors,
1150 dependency warnings. This proves restored runtime/restart, not hosted HTTP.

Retained artifact13583717882,1721B, SHA256
8fac576e54df6803ff7577ef1fd34fefcbb6cc6953125a11290623280aa3a5b2,
expires5February2027. Compact CI receipt retained under
LocalState/COSMIC/RecoveryLaptopResume20261107/nats-owned-restart-ci-qualified.json.
Remaining gates: exact shipping image packaging, explicit sandbox API
registration, protected publisher, candidate admission/image qualification,
then scoped Development rollout and native/Mongo/privacy/replica HTTP checks.

WEB4 image/startup integration is prepared: native2.15.2 Linux archive is
SHA-pinned in its own build stage; only the verified binary enters the final
image, which also checks its exact version and existing OpenSSL. The shared
supervisor starts NATS only with explicit OASIS_SANDBOX_NATS=true and gates
API startup on authenticated TLS readiness. API registration selects the
explicit owned factory only for that exact flag plus enabled sandbox; remote
mode retains its TLS/nonloopback contract. Invalid mode flags fail explicitly.

All101 shared runtime controls passed using cached Node24, shell syntax and
HEAD manifest/gitlink checks passed. Expanded API boundary suite now14 cases;
its CI and the exact candidate image are not yet qualified. No deployment or
live configuration change has occurred. Unrelated working changes excluded.

Owned NATS protected publisher is implemented separately from the remote
publisher. It reuses the same four-preset/catalogue/admission builder with
an explicit owned-mode validation; remote mode still rejects loopback.
The accepted descriptor/native archive and exact two-start qualification
are required before building eight bounded settings. The shipping Node
parser validates transport before preparation. Reader credentials are
retained only in protected snapshots and reused on repeated preparation.
Mutation uses the existing per-key intent/ACK publisher, exact image/mount
guard and deployment recheck before each write. Six controlled guards pass;
no prepare/apply or live settings mutation has been executed. Post-activation
mounted-source verification and HTTP verifier integration remain to implement
before enrollment can be treated as accepted.

Owned publisher post-activation verification now checks all eight per-key
ACKs and the complete live candidate before read-only mounted inspection.
The exact deployment/instance and image/mount are required. Inspection checks
active environment, original source receipt/paths, restricted server config,
TLS file hashes/private permissions, certificate/key and pinned native version.
Credentials travel through private stdin, never command arguments or output.
The compact mounted-identity receipt pins the first observed certificate and
refuses later drift. Deployment is rechecked before recording acceptance.
Ten controlled publisher guards pass, including stale ACK/config drift,
certificate identity change and deployment change. No live inspection or
settings mutation has yet occurred. HTTP verifier integration and exact
candidate image qualification remain open.

HTTP acceptance integration now treats owned NATS as a reviewed retained source.
It requires the exact deployment and mounted-source verification before cohort
work, binds the original Actor/three LiveIds to the fixed TLS endpoint/bucket
and reader, checks exact public identities and permits explicit reuse of an
acknowledged readiness visitor. The descriptor contains only real source IDs;
no deleted/tombstone records or evidence are invented. Publisher preparation
writes that exact reviewed descriptor, preserving it across repeated prepare.
Eleven publisher checks,39 preset/configuration controls and28 response-evidence
controls pass. Initial response controls exposed the missing NATS retained-source
test fixture/guard expectations; updated them and verified the complete suite.
No live cohort, native query/save, settings change or deployment was performed.
Exact candidate image and live acceptance remain open.

## Scoped deployment candidate prepared

Task API boundary run42397587804/job128478192083 passed all14 cases against
bb9a73dd88b7e91eb8d1554e059899e6947a5073. Artifact13584237599,3111B,
SHA256faaa719810eb3c7b4ffc23bd8470cdae7798b84ec40f4395901d5d6f537b60f1,
retained30days. No local TRX/archive download.

Fresh parent Development and existing candidate both remained3cc1ab108.
Core Development contains concurrent separate work; neither its branch nor
working files were switched or overwritten. The reused candidate Core branch
advanced normally from9fac20cd to2a09c9be620b3f3c2b1bae05bf62758bc5b13983,
with only the NATS read tariff. Parent candidate
82428c7a6441e6088cfdd6960f899641dae54dd1 is based on the accepted DuckDB
release3cc1ab108ef082b9aa7e00224eb895ad01060434. Fifteen changed paths cover
NATS runtime/provider/API/compiler/reference/test wiring plus matching Core
gitlink/manifest. Source9ca0c20aa; unrelated working changes excluded. Exact
manifest/gitlink validation and scoped diff review pass. Both existing candidate
refs were pushed normally, without switching the dirty working checkout.

Exact candidate boundary run42398316205 and image run42398318179 are running.
Image dispatch pins82428c7a6441e6088cfdd6960f899641dae54dd1 explicitly,
tagcosmic-nats-82428c7a6 and teaching_sidecars=true to preserve accepted GUN/
OrbitDB sources. Dispatch intents and204 ACKs retained; do not resubmit them.
No Development push, configuration staging or service deployment has occurred.

NATS eight-value preparation now completed against the actual Development
baseline, without mutation/deployment. Protected before/candidate snapshots
and the177B original-ID reviewed descriptor are retained under
LocalState/COSMIC/HostedSourceRolloutCurrent with prefixnats-owned-public.
A single43-character random reader password exists only in the protected
candidate and is reused on repeated preparation. Complete four-preset/source
binding comparison against the HTTP verifier passes. Parent/Core candidate
remote refs were read back exactly and a publication ACK retained. Both
candidate jobs remain in progress; do not apply settings or resubmit jobs
based solely on prepared configuration.
