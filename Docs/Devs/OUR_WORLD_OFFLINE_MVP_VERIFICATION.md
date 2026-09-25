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
| Production-configured ARM64 APK signing and `apksigner` verification | passed |

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

The signed release candidate is
`artifacts/our-world-release/OurWorld-Edge-Android-production-signed.apk`, 50,907,022 bytes, SHA-256
`2D222C971272D6B512CD3A1D8E506A0D6A102E37D7AD72AEB34EA50FDFF070B2`. It uses application id
`com.NextGenWorldLtd.OASISOmniverse`, version 1.0.0/code 1, ARM64 IL2CPP and SQLite, Android signature schemes v1
and v2, and the production WEB4/WEB5 URLs. The generated upload key and offline-grant private key are retained only
under the ignored `artifacts/our-world-release-secrets` directory and require an independent secure backup.

## External release gates

The aggregate `SqliteMvp` release validator correctly stops before packaging without fresh hosted evidence. It
requires a Mongo replica-set integration TRX and a separately coordinated abrupt-primary-termination TRX. Those
reports cannot be manufactured from unit tests and must come from the transaction-capable hosted test environment.

A physical Android device remains required to verify first online sign-in, flight-mode startup, process kill/restart,
radio loss/recovery, frame-time percentiles, memory, database growth, network use, battery/thermal behavior, and
hardware-backed credential behavior. The generated offline-grant public configuration is wired into ONODE; its
`OASIS_OFFLINE_GRANT_SIGNING_PRIVATE_KEY` secret must be set on the production deployment before production grant
renewal/revocation can pass. A Google Play Console application and its
publisher access remain required for store submission.
