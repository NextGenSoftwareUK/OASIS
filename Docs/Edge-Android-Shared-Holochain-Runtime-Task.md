# Task: use the shared Holochain Android runtime when it's installed

**Goal:** on Android, Our World should use the phone's **shared** Holochain conductor when one is
installed, and fall back to the **embedded** conductor (today's behaviour) when not. Two conductors
on one phone roughly doubles memory, battery and data use (separate networking, gossip, databases
and WASM engines).

Related: [Edge-iOS-Holochain-Conductor-Task.md](Edge-iOS-Holochain-Conductor-Task.md) (iOS has no conductor yet).

## Status (2026-10-11)

**Done**
- Bridge (`HolochainUnityBridge.kt`): prefers the shared runtime, waits for player approval, falls back to
  embedded; conductor-side `signZomeCall`; creates the service's notification channel; network permissions;
  waits for conductor readiness; arm64-only packaging.
- C# Edge: `PreferSharedRuntime` / `SharedRuntimeApprovalTimeoutMs`, session `Mode`,
  `IHolochainZomeCallSigningBridge` + `BridgeZomeCallSigner` wired into HoloNET's `ZomeCallSigner`.
  The Unity JNI bridge type-checks against Unity 2022.3's UnityEngine/AndroidJNI DLLs.
- Device test passes on an x86_64 emulator (ARM translation), with no runtime and with the old v0.1.5 runtime
  (reports `shared_unavailable`, falls back, signed `call_zome` succeeds).
- 0.7 shared runtime built from our fork and published as a pre-release:
  [oasis-runtime-v0.2.0-holochain-0.7](https://github.com/NextGenSoftwareUK/android-service-runtime/releases/tag/oasis-runtime-v0.2.0-holochain-0.7)
  (debug-signed). Upstream fixes offered: holochain/android-service-runtime#149 (client parcelers) and #150
  (Tauri service plugin network config).

**Remaining**
- Physical phone: install the arm64 runtime, run `HolochainUnityBridgeDeviceTest`, approve the notification,
  confirm `mode == shared` and a signed zome call through the shared conductor.
- A full Unity Android player build with the HoloEnabled profile.

---

## 1. Background: the upstream runtime

Upstream is [`holochain/android-service-runtime`](https://github.com/holochain/android-service-runtime),
from the Holochain organisation. It provides:

- **A runtime app** that hosts one shared conductor in a foreground service. Package id
  `org.holochain.androidserviceruntime.app` (product name "Holochain Service Runtime"). It ships as
  "user" (normal app) and "system" (preinstalled, e.g. by a phone maker) APK variants.
- **Kotlin libraries:** `client` (`HolochainServiceAppClient`, `HolochainServiceAdminClient`) and
  `service` (`HolochainService`, wrapping the Rust conductor).

Version history (upstream CHANGELOG): 0.2.x → Holochain 0.5; **0.3.0 → Holochain 0.7.0**.

**Important:** the latest *published* runtime APK is `android-service-runtime-v0.1.5` (Oct 2025),
which predates Holochain 0.5. **No published shared runtime runs Holochain 0.7 yet.** OASIS builds
from unreleased 0.3.0 source (commit `a7b5bb12`, see `Plugins/Android/Holochain/build-manifest.json`).
So until upstream publishes a 0.7 runtime APK, the hybrid below always falls back to embedded. It
should still be built now, so Our World switches over automatically when one ships.

## 2. How a third-party app talks to the shared runtime (from upstream source)

The service exposes two binders, selected by the `api` extra on the bind intent:

| Binder | Who may use it | Operations |
|---|---|---|
| `api=admin` (`IHolochainServiceAdmin`) | **Only the package hosting the service** (`clientPackageName == packageName`) | Full admin: install, uninstall, list apps, etc. |
| `api=app` + `installedAppId` (`IHolochainServiceApp`) | Any package, **after user approval** | `isReady`, `setupApp(payload, enableAfterInstall)`, `enableApp`, `ensureAppWebsocket`, `signZomeCall`, `importKeySeed` |

**Authorization flow** (`HolochainService.AppBinder`):
1. The client binds with `api=app` and its `installedAppId`, then calls e.g. `setupApp`.
2. The service identifies the caller by `Binder.getCallingUid()` → package name and checks
   `runtime.isAppClientAuthorized(package, installedAppId)`.
3. If not authorized, it posts a notification ("allow *package* to access *app*?") with Approve/Deny
   actions, and the call fails with `AppBinderUnauthorizedException`.
4. When the user taps Approve, `runtime.authorizeAppClient(package, installedAppId)` records it, and the
   client retries.

`HolochainServiceAppClient(activity, ComponentName)`: `connectSetupApp(payload, enableAfterInstall): AppAuthFfi`,
`setupApp`, `enableApp`, `signZomeCall(ZomeCallParamsFfi): ZomeCallParamsSignedFfi`, `connect(installedAppId)`.

Our embedded mode works today because the Our World app **is** the host package, so it may use the
admin binder (`HolochainUnityBridge.kt` uses `HolochainServiceAdminClient` pointed at its own
`HolochainService`).

## 3. Consequences for OASIS

1. **No admin access in shared mode.** Everything the Edge stack does through the admin interface has
   to go through the app binder instead: install via `setupApp`, enable via `enableApp`, and the app
   websocket via `ensureAppWebsocket`.
2. **Zome-call signing must move to the runtime.** Today HoloNET generates its own signing key and
   grants itself a zome-call capability through the **admin** API. In shared mode it can't, so signing
   has to go through `signZomeCall` over IPC. HoloNET needs a pluggable signer (e.g. an
   `IZomeCallSigner` with the current local-key implementation as the default, and an Android-runtime
   implementation that calls the bridge). HoloNET already builds `ZomeCallParamsSigned`
   (`bytes` + `signature`), so the hook sits where the signature is produced.
3. **User approval UX.** The first launch shows a system notification the player must approve. The
   game needs a "waiting for permission" state, a retry, and a way to fall back to embedded if the
   player denies or ignores it.
4. **Version gate.** Use the shared runtime only if it runs Holochain 0.7.x, matching the bundled
   `oasis.happ` (`manifest_version: "0"`). Check the installed runtime's version (package version name,
   or try `setupApp` and treat a bundle-decode error as incompatible). If it's incompatible, fall back.
5. **Shared network config.** One conductor serves every hApp, so bootstrap/relay URLs are the runtime's,
   not ours. `HolochainAndroidRuntimeOptions.BootstrapUrl` / `RelayUrl` only apply in embedded mode;
   document that.
6. **Data location.** Shared mode stores the source chain in the runtime app, so uninstalling Our World
   doesn't remove it, and switching between shared and embedded means two separate chains for the
   same player. Decide the policy: e.g. once shared is chosen, stick with it, and import the key seed
   (`importKeySeed`) so the agent identity is the same.

## 4. Plan

1. **Detect:** in `HolochainUnityBridge.kt`, query `PackageManager` for
   `org.holochain.androidserviceruntime.app` (add a `<queries><package …/></queries>` entry to the
   manifest for Android 11+ package visibility).
2. **Shared path:** `HolochainServiceAppClient(activity, ComponentName("org.holochain.androidserviceruntime.app",
   "org.holochain.androidserviceruntime.service.HolochainService"))` → `connectSetupApp(payload, true)`
   → `AppAuthFfi` (port and token) → return the same `HolochainAndroidAppSession { Port, AuthenticationToken }`.
3. **Unauthorized:** catch `AppBinderUnauthorizedException`, report a new session state
   (`AwaitingUserApproval`) up through `IHolochainAndroidServiceBridge`, and poll or retry.
4. **Fallback:** if the runtime is missing, incompatible, or denied, start the embedded service exactly as today.
5. **Signing:** add the pluggable signer to HoloNET (`holochain-client-csharp`), plus a bridge call
   `signZomeCall(bytes) -> signature` for shared mode.
6. **Settings:** a player-facing toggle ("Use shared Holochain runtime when available"), on by default.
7. **Tests:** run HoloNET's live tests (`LiveConductorTests`) against both modes; add a test that
   denial falls back to embedded.
8. **Upstream:** contribute `holochain-android-runtime-0.3.0-client-parcelers.patch` (the 0.7 app
   statuses and the removed WebRTC fields) to `holochain/android-service-runtime`. Ask upstream when a
   0.7 runtime APK will be published, since that's what makes the shared path usable for players.
