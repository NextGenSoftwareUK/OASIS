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
