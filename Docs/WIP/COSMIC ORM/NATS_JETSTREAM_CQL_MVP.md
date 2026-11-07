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
native/Mongo presets. CI qualification is pending; no native TLS acceptance
is claimed from these controlled cases. No NATS settings, hosted source,
credential/certificate or new local runtime/environment has been created.

## Remaining gates

- Run the exact nine-case boundary job, retaining its TRX.
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
