# OGEngineClient for Unity — Asset Store release

## Product identity

The public product is **OGEngineClient for Unity**. It is the shared OGEngineClient with the OASIS Edge Runtime
integrated, not a separate Edge client. The UPM identifier remains `com.nextgensoftware.oasis.edge` for compatibility
with existing projects. Version 1.0.0 is the Edge-enabled distribution; Remote-Only is a deployment profile from the
same source tree and is not a second Asset Store product.

## Supported scope for 1.0.0

- Unity 2022.3.62f3 and compatible later 2022.3 releases.
- Android, iOS and Windows protected offline-session storage.
- Android ARM64 IL2CPP build validation.
- Windows Editor compilation and secure-store smoke testing.
- SQLite-backed durable Edge state, HyperDrive synchronization and ONET Edge support.
- Linux/macOS runtime and SQLite binaries are present, but protected offline-session storage is not yet qualified;
  the listing and local documentation must disclose remote-only operation on those platforms.

Desktop integration follow-up: Unity now selects the shared `DesktopPlatformSecureSessionStore` on Windows,
macOS and Linux editor/player builds, rather than excluding the existing macOS Keychain and Linux Secret Service
adapters. Linux requires `secret-tool` and an unlocked Secret Service collection; unavailable native storage returns
an explicit error and never writes an unprotected grant file. Qualification remains pending on actual macOS/Linux
hosts. Local Windows evidence: 69/69 Edge Runtime tests passed, including the linked Unity adapter's credential
round trip, device isolation, deletion, identity validation and cancellation. The OSX and Linux conditional branches
each compiled and passed the four adapter tests **on Windows**; this does not prove native Keychain/Secret Service
behavior or Unity/IL2CPP execution on those operating systems. The Unity editor validation entry point now requires
the protected-store round trip on every desktop editor platform.

The same local SQLite-profile package passed Unity 2022.3.62f3 editor compilation/secure-store validation and an
Android ARM64 IL2CPP build. Its APK was inspected for `lib/arm64-v8a/libil2cpp.so` and `libe_sqlite3.so`, with
foreign ABIs rejected. An earlier Mono/ARMv7 build is explicitly excluded from this evidence: all profiles now
set IL2CPP/ARM64 and share APK payload verification. Regression fixtures reject Mono, missing SQLite/IL2CPP,
foreign ABIs and missing Holo conductor libraries. The validation script reuses its fixed owned project/cache
and refuses to refresh it while a Windows Unity process owns that project. This is not physical-device or
HoloEnabled current-candidate acceptance.

The complete validator also passed a repeat run in the reused project after fixing sample copying to refresh
contents rather than nest `QuickStart`. Evidence: `artifacts/unity-edge-validation.log`,
`artifacts/unity-edge-android-validation.log` and `artifacts/unity-edge-validation-project/Build/OASISEdgeValidation.apk`.
The inspected APK SHA-256 is `2309FC59D3FDE16A781CF6D8700A873B92E1119C4C5BA23B380805020F0DF0F7`.

Do not claim physical Android flight-mode, battery or performance certification until the device acceptance report
exists. Historical local `HoloEnabled` validation is recorded in [Edge Runtime releases](./EDGE_RUNTIME_RELEASES.md);
it does not certify the current revision. Require the current release candidate's successful provenance-verified
Unity compile and ARM64 IL2CPP player-build gate before describing that candidate as validated.
The profile's lightweight Holo Edge repository, authenticated runtime host, managed Holochain 0.7 signing path and
separately durable SQLite-to-Holo projection outbox are included. HoloOASIS must not be advertised as the mobile
default until the physical-device lifecycle, resource and two-device convergence gates also pass.

## Package contents

The canonical source template is `UnityPackages/com.nextgensoftware.oasis.edge`. The build script compiles the shared
OGEngineClient and Edge assemblies, copies native SQLite libraries, creates deterministic Unity metafiles and emits:

- `artifacts/unity-store-candidate/com.nextgensoftware.oasis.edge/`
- `artifacts/unity-store-candidate/com.nextgensoftware.oasis.edge.tgz`

The public archive includes local documentation, changelog, licence, third-party notices and a Quick Start sample.
Internal batch validators, Our World configuration, endpoints, credentials, signing keys, APKs and debug artifacts
are prohibited.

## Automated release commands

```powershell
Scripts/build_edge_unity_package.ps1 -Configuration Release `
  -OutputDirectory artifacts/unity-store-candidate

Scripts/validate_ogengine_unity_asset_store_package.ps1

Scripts/validate_edge_unity_package.ps1 `
  -PackageDirectory artifacts/unity-store-candidate/com.nextgensoftware.oasis.edge `
  -LogDirectory artifacts/unity-store-candidate
```

The readiness validator enforces the current Unity UPM metadata fields, semantic versioning, the 700 MB archive
limit, one root folder, the 150-character path limit, complete/non-redundant metafiles, required public documentation,
sample presence, internal-file exclusion and credential/private-endpoint scanning. Unity then compiles the exact
package with the sample imported and builds the Android validation player.

The normal Edge release gate invokes the same readiness validator automatically:

```powershell
Scripts/validate_edge_runtime_release.ps1 -Configuration Release -Profile SqliteMvp `
  -HostedMongoSyncReport <replica-set-suite.trx> `
  -HostedMongoProcessKillReport <primary-termination.trx>
```

## Listing disclosures

The Asset Store description must state at its top that this SDK communicates with the external OASIS/ONODE service,
that an account and network connection are needed for initial authentication and hosted synchronization, and whether
any chosen service tier has additional cost. It must explain that supported gameplay continues using a previously
issued signed offline grant, and disclose the Linux/macOS secure-store limitation for 1.0.0.

The listing must include this third-party notice:

> Asset uses Microsoft.Data.Sqlite, SQLitePCLRaw, Newtonsoft.Json and Microsoft .NET compatibility libraries under
> the MIT licence, and SQLite public-domain code; see THIRD PARTY NOTICES.md in the package for details.

No analytics or telemetry is collected by the package itself. If a host application adds analytics, its own consent
and privacy implementation remains the host application's responsibility.

## Manual Publisher Portal work

Code cannot complete publisher-owned portal actions. Before submission, the publisher must:

1. Confirm the NextGen Software Ltd publisher profile, identity verification, business details and payout/tax data.
2. Confirm access to Unity's UPM early-access publishing flow; otherwise submit through the supported Asset Store
   Publishing Tools workflow.
3. Create the listing, price, support email, privacy/terms links, category and keywords.
4. Supply original icon, card images, screenshots and an optional externally hosted demonstration video.
5. Complete the physical Android acceptance matrix and attach the resulting supported-platform claims.
6. Upload the exact validated archive/hash, submit it for Unity review and address any reviewer findings through the
   same draft rather than creating duplicate submissions.

## Current official policy basis

This checklist tracks Unity's Asset Store Submission Guidelines updated 20 May 2026. The official rules require
professional/error-free content, accurate dependency/service disclosures, third-party notices, comprehensive local
documentation for code/configurable assets, Unity 2022.3 or newer, paths under 150 characters, no embedded executable
applications, and—when using UPM—the required manifest fields, valid assembly definitions, complete metafiles and a
maximum 700 MB package. Re-check the official guidelines immediately before upload because portal rules can change.
