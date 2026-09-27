# Our World offline/synchronization MVP verification

Verified locally on 2026-09-24 from the current working tree. This report covers the narrowed SQLite MVP. It does
not claim physical-device performance certification, hosted Mongo failover evidence, server deployment of the
generated grant key, or store deployment.

## Verified implementation invariants

- OGEngineClient owns the reusable Edge Runtime integration. Managed/Unity offline synchronization is enabled by
  default and can be explicitly disabled; the native ABI adds versioned Edge configuration/status structures without
  changing the original configuration layout.
- Authentication requests no longer serialize anonymous objects through reflection. HyperDrive grants, exchanges,
  results, built-in projections, and command payloads use generated `System.Text.Json` metadata. Opaque world content
  remains JSON (`JObject`/`JArray`), and applications must supply generated metadata for additional typed entities.
- Windows protected offline grants use the same generated HyperDrive wire contract as macOS/Linux adapters. The
  NativeAOT build check now fails if IL2026 or IL3050 returns in authentication, synchronization, secure-grant,
  Edge-payload, or callback-marshalling files.
- Runtime tests cover validated device-bound grants, offline startup, network loss, durable local commit, process
  restart, reconnect, bounded drain, idempotent replay, stale in-flight responses, suspend/resume, and the exact
  offline/reconnecting/synchronized notification sequence.

## Local release evidence

| Gate | Result |
|---|---:|
| HyperDrive Core | 167/167 passed |
| Edge SQLite | 26/26 passed |
| Edge Runtime and UI state machine | 50/50 passed |
| OASIS DNA/default configuration | 11/11 passed |
| ONET synchronization contracts | 9/9 passed |
| Hosted offline-grant and JWT security | 12/12 passed |
| ONODE configured-key issuer verification | 11/11 passed |
| OGEngineClient | 12/12 passed |
| OGEngineClient Edge and Remote-Only profiles | passed |
| NativeAOT publish and C++ ABI smoke executable | passed |
| Critical NativeAOT IL2026/IL3050 count | 0 |
| Unity Edge package compilation and Android package smoke build | passed |
| Our World compilation against the generated package | passed |
| Our World Android ARM64 IL2CPP build and APK content inspection | passed |
| Android 15 x86_64 emulator install, launch, native-library load, and fatal-log inspection | passed |
| Local three-process Mongo replica-set transaction/election suite | 24/24 passed |
| Local externally coordinated abrupt-primary process termination | 1/1 passed |
| Hosted Atlas transaction and election suite | 24/24 passed |
| Production-configured ARM64 APK signing and `apksigner` verification | passed |
| Production-configured ARM64 Android App Bundle build and signature verification | passed |

TRX results are under `artifacts/our-world-mvp-current`. NativeAOT output is in
`artifacts/ogengine-nativeaot-validation-current.log`. Unity logs and the package are under
`artifacts/unity-current`.

The newly built APK is `artifacts/our-world-android-current/OurWorld-Edge-Android.apk`, 50,906,015 bytes, SHA-256
`CA2B9CC2E708EDC711620D8E058D2679920FB0D6DBFF2028A978C8767A99D300`. The build gate verified ARM64
`libil2cpp.so`, ARM64 `libe_sqlite3.so`, Java bytecode, packaged host configuration, and absence of SQLite binaries
for unintended ABIs.

The emulator validation APK and runtime evidence are under `artifacts/our-world-android-emulator`. The Android 15
x86_64 emulator launched `com.NextGenWorldLtd.OASISOmniverse`, reached the Beam In screen, and logged no fatal,
missing-library, SQLite, or `UnsatisfiedLinkError` failure. The emulator APK SHA-256 is
`0194236B06AC30E9DAA0EE3E4BBD1BF08615DF2FE532F8E750B393426290101A`.

The local replica-set evidence is under `artifacts/mongo-replica-validation`. The standard suite passed 24/24. The
separate process-loss run terminated the actual primary on port 27018, observed election of the node on port 27019,
and passed idempotent replay 1/1. These reports intentionally remain labeled local evidence.

The hosted Atlas transaction evidence is `artifacts/hosted-mongo-atlas/hosted-mongo-sync.trx`; all 24 tests passed
against the production-configured Atlas cluster using isolated `hd_*` databases that were removed by each test.

## Hosted Atlas tier, capacity, and failover constraint

The production-configured `OASIS` cluster was inspected in Atlas on 2026-09-25. It is an AWS N. Virginia
(`us-east-1`) Free cluster running MongoDB 8.0 with a fixed three-node replica set. Atlas showed 37.2 MB of logical
data, approximately 7.3% of the 512 MB allowance, leaving approximately 475 MB available.

MongoDB's documented Free-cluster limits at the time of verification are:

| Resource or capability | Free-cluster limit |
|---|---:|
| Data plus indexes | 512 MB total |
| Read/write throughput | 100 operations per second |
| Concurrent connections | 500 |
| Data transfer | 10 GB in and 10 GB out per rolling seven days |
| Databases | 100 |
| Collections | 500 total |
| Free clusters | One per Atlas project |
| Replica set | Three nodes, managed by Atlas |
| Database-name length | 38 bytes |
| Namespace length | 95 bytes |
| In-memory sort | 32 MB |
| Aggregation pipeline | 50 stages |

Free clusters do not provide automated backups, configurable storage or memory, sharding, private endpoints,
network peering, downloadable database logs, manual primary-failover testing, or regional-outage testing. They do
not honor `allowDiskUse` for temporary aggregation files and can be automatically paused after 30 days with no
connections. The authoritative list is MongoDB's
[Atlas Free Cluster Limits](https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/).

Atlas-managed primary-failover testing requires a dedicated M10-or-higher cluster. On 2026-09-25 MongoDB listed an
AWS `us-east-1` M10 with its default 10 GB storage at USD $0.08 per hour, approximately $56.94 for continuous monthly
use. Dedicated clusters are billed hourly while active; storage, backup, data transfer, and optional features can
add charges. A temporary M10 used for one to three hours would therefore have approximately $0.08-$0.24 in base
compute cost, and it must be deleted after testing to stop further compute charges. Current prices must be confirmed
in Atlas before creation; see [Atlas AWS pricing](https://www.mongodb.com/products/platform/atlas-cloud-providers/aws/pricing)
and [Atlas billing](https://www.mongodb.com/docs/atlas/billing/).

The signed release candidate is
`artifacts/our-world-release/OurWorld-Edge-Android-production-signed.apk`, 50,907,022 bytes, SHA-256
`2D222C971272D6B512CD3A1D8E506A0D6A102E37D7AD72AEB34EA50FDFF070B2`. It uses application id
`com.NextGenWorldLtd.OASISOmniverse`, version 1.0.0/code 1, ARM64 IL2CPP and SQLite, Android signature schemes v1
and v2, and the production WEB4/WEB5 URLs. The generated upload key and offline-grant private key are retained only
under the ignored `artifacts/our-world-release-secrets` directory and require an independent secure backup.

The corresponding Google Play Android App Bundle is
`artifacts/our-world-release/OurWorld-Edge-Android-production-signed.aab`, 50,912,339 bytes, SHA-256
`3484DD7821E1B83D92E6986B77463304238C37EA1D76A20BE746FBB272B8157D`. Its JAR signature verifies with the same
self-signed upload certificate, and its base module contains the production host configuration, ARM64 IL2CPP,
ARM64 SQLite and DEX payloads.

## External release gates

The aggregate `SqliteMvp` release validator now has fresh hosted transaction evidence. A separately coordinated
hosted abrupt-primary-termination TRX remains required; the local process-loss report does not substitute for an
Atlas-managed failover event. The current Free cluster cannot expose Atlas's primary-failover test, so completing
this gate requires a temporary or permanent M10-or-higher cluster.

A physical Android device remains required to verify first online sign-in, flight-mode startup, process kill/restart,
radio loss/recovery, frame-time percentiles, memory, database growth, network use, battery/thermal behavior, and
hardware-backed credential behavior. The generated offline-grant public configuration is wired into ONODE, and the
operator confirmed that `OASIS_OFFLINE_GRANT_SIGNING_PRIVATE_KEY` is set on the production deployment; live grant
renewal/revocation still requires endpoint verification. A Google Play Console application and its publisher access
remain required for store submission.
