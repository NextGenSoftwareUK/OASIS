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

Do not claim physical Android flight-mode, battery or performance certification until the device acceptance report
exists. Do not claim HoloOASIS as the default until the `HoloEnabled` release profile passes with a provenance-verified
hApp.

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
