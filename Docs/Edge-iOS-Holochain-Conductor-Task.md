# Task: Holochain conductor on iOS for the OASIS Edge (HoloEnabled) profile

**Goal:** run a Holochain **0.7** conductor inside the Our World iOS app, so the HoloOASIS Edge
provider works on iPhone and iPad the same way it already works on Android.

**Status (2026-10):** Android is done; iOS has **no conductor at all** (no `.xcframework`, no
static library, no Swift/Objective-C bridge). The Edge package's iOS support today is the runtime,
SQLite and Keychain session storage only. The HoloEnabled profile is Android-only.

**Update (2026-10-11): the conductor builds for iOS.**
- JIT blocker solved with Holochain 0.7's own `wasmer-wasmi` interpreter. Our fork's runtime crates now have
  `wasm-jit` (default; Cranelift, Android/desktop) and `wasm-interpreter` (wasmi, iOS) features.
- macOS CI workflow `ios-conductor.yml` on `NextGenSoftwareUK/android-service-runtime` (`oasis/holochain-0.7`)
  builds `aarch64-apple-ios` + `aarch64-apple-ios-sim`, generates UniFFI Swift bindings and packages
  `HolochainConductor.xcframework`. Push a tag `oasis-ios-conductor-vX.Y.Z` to build and publish.
- First release: [oasis-ios-conductor-v0.1.1](https://github.com/NextGenSoftwareUK/android-service-runtime/releases/tag/oasis-ios-conductor-v0.1.1)
  (xcframework ~121 MB zipped, Swift bindings, `build-manifest.json`). Minimum iOS 15.
- Not yet proven: starting a conductor and running an OASIS zome call on the simulator (plan step 1's exit
  criterion), then steps 3 to 7 below (Swift bridge, Unity C# bridge, lifecycle, build guard, tests).

**Update (2026-10-12): the conductor runs on the iOS simulator, and the Unity bridge is in place.**
- CI runs two simulator tests on every build: start the conductor, install the OASIS hApp, sign `oasis.get_all_avatars` with the
  conductor-held key and get `zome_called` over the app websocket; and the same through the Unity C ABI (start, duplicate start
  rejected, resume, sign, stop). Both pass.
- iOS fixes found on the way: link `Network.framework`; keep lair's domain socket under the 104-byte limit (iOS container paths are
  longer), see `crates/runtime/src/ios_lair.rs` in the fork.
- Unity: `UnityHolochainIosBridge.cs` (HoloEnabled~), platform selection in `HoloEdgeUnityLocalProvider`, an iOS build guard (iOS 15+,
  xcframework present) and packaging in `build_edge_unity_package.ps1`.
- Release: [oasis-holochain-0.7-v1.0.0](https://github.com/NextGenSoftwareUK/android-service-runtime/releases/tag/oasis-holochain-0.7-v1.0.0)
  (Android APKs + iOS xcframework and Swift bindings), pinned in `Scripts/holochain_ios_runtime.version.json`; fetch and verify with
  `Scripts/fetch_holochain_ios_runtime.ps1`.
- Still to do: a physical iPhone run, a full Unity iOS player build (add the system frameworks to UnityFramework), and the lifecycle
  hooks (plan step 5).

---

## 1. What exists on Android (use it as the reference)

| Piece | Location | Notes |
|---|---|---|
| Native conductor | `OASIS Omniverse/OASIS Hub/Packages/com.nextgensoftware.oasis.edge/Runtime/Plugins/Android/Holochain/holochain-service.aar` | `jni/arm64-v8a/libholochain_conductor_runtime_ffi.so` (~99 MB), plus `libiroh*.so`, `libholochain_wasmer_*.so`, `libc++_shared.so` |
| Client FFI | same folder, `holochain-client.aar` | `libholochain_conductor_runtime_types_ffi.so` |
| Build provenance | same folder, `build-manifest.json` | Source `https://github.com/holochain/android-service-runtime.git`, commit `a7b5bb12a64d837694a6939701823d61803654bf`, upstream release `0.3.0`, `holochainCompatibility: "0.7"`, ABI `arm64-v8a`, two patches (`…-client-parcelers.patch`, `…-unity-kotlin.patch`) with SHA-256s |
| Native bridge | `Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity/AndroidBridge/bridge/src/main/java/one/oasisomniverse/holooasis/unity/HolochainUnityBridge.kt` | Starts the conductor as a foreground service, installs the hApp (`InstallAppPayloadFfi`), returns app-websocket port and auth token; `suspend` / `resume` / `stop` |
| Unity C# bridge | `UnityPackages/com.nextgensoftware.oasis.edge/HoloEnabled~/UnityHolochainAndroidServiceBridge.cs` | Calls the Kotlin class over JNI |
| Platform-neutral contract | `OASIS Architecture/NextGenSoftware.OASIS.Edge.Runtime/Holochain/HolochainAndroidServiceContracts.cs` | `IHolochainAndroidServiceBridge`: `StartAndSetupAsync(options)`, `SuspendAsync`, `ResumeAsync(installedAppId)`, `StopAsync`; returns `HolochainAndroidAppSession { Port, AuthenticationToken }` |
| Consumer | `Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge` | Connects with HoloNET (`IHoloNETClientAppAgent`) to `127.0.0.1:{Port}` using the token |

The contract is the key point: everything above the bridge (Edge Runtime, HoloOASIS Edge, OGEngineClient, HoloNET)
only needs **a local app-interface port and an auth token**. iOS has to produce the same thing.

## 2. Constraints specific to iOS

1. **No child processes.** iOS forbids launching executables, so the conductor must be **compiled in
   as a static library** (`.a` inside an `.xcframework`) and started in-process via FFI. (Same as
   Android, which also loads it as a library rather than running `holochain`.)
2. **No long-lived background service.** iOS suspends apps shortly after they leave the foreground.
   The conductor can only run while the game is active, plus a few seconds of background task time.
   Plan for: stop or suspend on `applicationDidEnterBackground`, restart and resume on foreground.
   The Edge host already has `SuspendAsync` / `ResumeAsync` hooks, and the SQLite→Holo outbox is durable,
   so unsynced work survives this.
3. **No JIT.** Holochain runs zome WASM with Wasmer. iOS disallows writable+executable memory for
   App Store apps, so the conductor must use an **interpreter** or **ahead-of-time** WASM mode, not
   Wasmer's JIT (Cranelift/LLVM). This is the biggest technical risk; confirm it first (see step 1 below).
4. **Build host:** needs a Mac with Xcode and the Rust targets `aarch64-apple-ios` (devices) and
   `aarch64-apple-ios-sim` (Apple-silicon simulator).
5. **Networking:** iroh (QUIC) needs outbound UDP; check that it works on cellular and Wi-Fi with
   no extra entitlements. The local app interface binds `127.0.0.1` only.
6. **App size:** the Android conductor library is ~99 MB unstripped. Strip symbols, enable LTO and
   `opt-level = "z"`/`"s"`, and check against App Store download limits.

## 3. Suggested plan

1. **Spike: does the 0.7 conductor build and run zomes on iOS without JIT?**
   Build the conductor runtime FFI crate from `android-service-runtime` (same commit as Android) for
   `aarch64-apple-ios-sim`. Check which Wasmer engine it uses, and whether Holochain 0.7 offers an
   interpreter or AOT option (the `wasmer` features in `holochain_wasmer_host` are the place to look).
   Exit criterion: in a bare Xcode test app, start a conductor, install the OASIS hApp and run one zome call.
2. **Package:** produce `HolochainConductor.xcframework` (device and simulator slices) with a minimal C
   ABI: `start(config) -> handle`, `install_app(handle, bytes, app_id, network_seed) -> session`,
   `ensure_app_websocket(handle, app_id) -> {port, token}`, `suspend`, `resume`, `stop`. Mirror the
   functions the Kotlin bridge calls on `HolochainServiceAdminClient`, so the two platforms stay aligned.
   Write a `build-manifest.json` like Android's (source repo, commit, patches, `holochainCompatibility`, SHA-256s).
3. **Native bridge:** a small Objective-C or Swift layer (`HolochainUnityBridge` for iOS) exposed to Unity
   via `[DllImport("__Internal")]`.
4. **Unity C# bridge:** `UnityHolochainIosBridge : IHolochainAndroidServiceBridge`. Consider renaming
   the contract to `IHolochainServiceBridge` / `HolochainRuntimeOptions` now that it's cross-platform.
   Select the bridge per platform in `HoloEdgeUnityLocalProvider`.
5. **Lifecycle:** hook Unity `OnApplicationPause` into `SuspendAsync`/`ResumeAsync`, and make sure the
   Edge host drains the Holo outbox after resume.
6. **Build guard:** extend `UnityHolochainAndroidBuildGuard` (or add an iOS equivalent) so iOS builds
   fail clearly if the xcframework is missing, instead of failing at runtime.
7. **Tests:** reuse HoloNET's live tests (`holochain-client-csharp`, `LiveConductorTests`). They only need
   an admin URI and hApp path, so they can run against the iOS conductor via the simulator.

## 4. Things to keep consistent with the HoloNET fixes (October 2026)

The HoloOASIS Edge provider uses `holochain-client-csharp` directly. Build against current `main`. Before
the fixes there, HoloNET couldn't talk to a 0.6/0.7 conductor at all (the request envelope, response
decoding, auth token encoding and several hangs were all broken). See that repo's `AUDIT.md` and
`CHANGELOG.md`. In particular:

- App auth tokens are `Vec<u8>` and travel as **integer arrays** on the wire, not msgpack bin.
- App interfaces require `allowed_origins` when attached.
- The bundled `oasis.happ` must be built for 0.7 (`manifest_version: "0"`); older bundles are rejected.

## 5. Fallback if on-device iOS isn't viable

If WASM execution without JIT isn't workable for 0.7, iOS clients can connect HoloNET to a conductor
running elsewhere (the player's PC, or a hosted OASIS node). HoloNET supports this already; the Edge
host would need an "external conductor" option instead of the in-process bridge, plus a way to issue
app tokens remotely. The offline-first SQLite path keeps working either way.
