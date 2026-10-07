# HoloOASIS Unity Provider

This package exposes the portable HoloOASIS provider and the Unity lifecycle host used to bind a device-owned Holochain conductor to OASIS provider activation.

Platform packages implement `IHolochainConductorLifecycle`. The host serializes start, suspend, resume, and stop transitions so OASIS never advertises HoloOASIS as active while its conductor is unavailable.

Android uses `HolochainAndroidConductorLifecycle` plus an `IHolochainAndroidServiceBridge`. Build the pinned
Holochain 0.7 ARM64 service, client, and Kotlin Unity bridge with
`Scripts/build_holochain_android_runtime.ps1`. Do not reference Maven Central's `0.0.19` artifact: its bytecode has
the obsolete pre-0.7 network contract. The generated manifest records the upstream commit, reviewed compatibility
patch hashes, patched-source hashes, AAR/dependency hashes, and sizes. The reviewed patches repair the stale
Holochain 0.7 parcel boundary and compile the upstream runtime with Kotlin 1.6.21/coroutines 1.6.4, the supported
toolchain boundary for Unity 2022.3's Android Gradle Plugin 7.1.2. `-PackageExistingBuild` only re-emits provenance
for already-built local outputs; release automation must perform the full source build.

The package targets `netstandard2.1` for Unity compatibility. Android conductor binaries and Unity player integration are distributed by the Holo-enabled OGEngine/Edge Unity package rather than embedded in this platform-neutral NuGet package.

Build and validate that distribution with:

```powershell
Scripts/build_edge_unity_package.ps1 -Configuration Release -Profile HoloEnabled `
  -OutputDirectory artifacts/unity-holo
Scripts/validate_edge_unity_package.ps1 `
  -PackageDirectory artifacts/unity-holo/com.nextgensoftware.oasis.edge `
  -LogDirectory artifacts/unity-holo-validation
```

The `HoloEnabled` package contains the provenance-verified service/client/Unity bridge AARs, their exact runtime
dependency closure, the packed `oasis.happ`, the lightweight JNI service bridge, and the Holo Edge repository/runtime
host. Its build guard requires Android
API 27 or newer and ARM64-only IL2CPP. The Android service contracts live in OASIS Edge Runtime so the Unity package
does not inherit the full OASIS Core/server assembly graph. HoloNET uses a pure-managed Ed25519 signer and the exact
Holochain 0.7 canonical MessagePack + SHA-512 signing contract; the old Windows serialization DLL and Sodium native
dependency are forbidden by the package gate.

This completes the package and player-build contract, not mobile qualification. Hardware installation/lifecycle
instrumentation, two-device offline gossip and reconnect convergence, resource profiling, and the durable SQLite-to-
Holo projection queue remain required before HoloOASIS becomes the mobile default.
