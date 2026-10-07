# OGEngineClient for Unity

## What this package provides

The Edge-enabled OGEngineClient keeps supported gameplay reads and commands available when connectivity disappears.
It commits local work to a durable SQLite journal, reports its state through `EdgeRuntimeStatus`, and reconciles
idempotently with the hosted ONODE when connectivity returns.

The runtime does not turn server-authoritative actions into unsafe local writes. Inventory rewards, Karma changes,
quest completion and GeoNFT ownership are queued as commands and become authoritative only after the ONODE accepts
them.

GeoNFT collection availability is synchronized as a private, server-authoritative projection containing the
authenticated avatar's player/global counts, denial reason and next collection time. While offline, an elapsed
cooldown may become eligible locally because its end time is known; ownership and quantity limits remain blocked
until the hosted ONODE publishes a newer projection. Collection itself is always a durable command and is
revalidated against current authoritative rules when it reaches the ONODE.

GeoHotSpot triggers follow the same rule. Call `QueueGeoHotSpotTriggerAsync` with a stable operation id and observed
evidence. The local commit is durable and replay-safe, but the client must not display rewards as granted until the
typed hosted command outcome synchronizes back. WEB5 REST and queued Edge commands use the same ONODE authority for
authored trigger rules, spawn limits, cooldowns and reward effects.

## Requirements

- Unity 2022.3.62f3 or newer compatible 2022.3 release.
- API Compatibility Level `.NET Standard 2.1`.
- A hosted ONODE using the matching HyperDrive synchronization contract.
- An authenticated avatar, stable per-installation device ID and writable persistent-data directory.
- A signed offline-session grant for authenticated offline use.

## Initialization

Create one `OASISEdgeUnityHost` component in the bootstrap scene and preserve it across scene changes. Authenticate
with the shared OGEngineClient flow, then initialize Edge with the returned bearer token:

```csharp
Guid stableDeviceId = LoadOrCreateStableDeviceId();
string databasePath = Path.Combine(Application.persistentDataPath, "oasis-edge.db");
var secureStore = new UnityPlatformSecureSessionStore(avatarId, stableDeviceId);

await edgeHost.InitializeAsync(
    new Uri(onodeUrl), bearerToken, avatarId, stableDeviceId, databasePath,
    secureStore, offlineGrantValidator);

var grant = await edgeHost.AuthenticateHostedSessionAsync(
    bearerToken, new[] { "edge:play" }, 1440);
if (grant.IsError)
    throw new InvalidOperationException($"{grant.ErrorCode}: {grant.Message}");
```

The `HoloEnabled` package can additionally project every accepted local entity mutation to the bundled Holochain
hApp. Supply its lifecycle to the same host; OGEngineClient remains the application-facing API:

```csharp
var holoProvider = new HoloEdgeUnityLocalProvider(new HolochainAndroidRuntimeOptions
{
    AppBundle = holoAppTextAsset.bytes,
    InstalledAppId = "oasis"
});

await edgeHost.InitializeAsync(
    new Uri(onodeUrl), bearerToken, avatarId, stableDeviceId, databasePath,
    secureStore, offlineGrantValidator, holoProvider);
```

The host starts Holochain before opening OGEngineClient, drains the separately durable SQLite-to-Holo outbox in
device-sequence order, suspends Edge workers before the conductor, and restores the conductor before resuming sync.
The hosted outbox and Holo outbox have independent acknowledgements, so either destination can be unavailable
without erasing work for the other.

The application must provide an `IEdgeOfflineGrantValidator` configured with the public verification key published
by its ONODE. Never place the private signing key in Unity assets, `Resources`, `StreamingAssets`, scenes or
`PlayerPrefs`.

## Status and user experience

Subscribe to `OASISEdgeUnityHost.StatusChanged`. Drive UI from the reported Edge state and durable pending count:

- Offline: “Working offline — changes will sync when you reconnect.”
- Recovering/synchronizing: “Back online — syncing changes.”
- Online with zero pending operations: “Sync complete.”
- Rejected command: show the server-provided error; do not report successful synchronization.

Unity reachability is only a hint. HyperDrive request results are authoritative, so a connected Wi-Fi network with
no usable internet still transitions correctly.

## Lifecycle

`OASISEdgeUnityHost` suspends and resumes the client and optional local provider on Unity application pause events
and disposes both when destroyed. A failed two-part lifecycle transition is rolled back to the prior coherent state.
Use one host per application database. Do not create parallel clients against the same journal.

## Platform security

- Android grants are encrypted with an Android Keystore key.
- iOS grants are stored in Keychain.
- iOS/tvOS Keychain replacement is atomic: an existing signed grant is updated in place, and native read failures
  are returned as errors rather than being mistaken for a missing offline session.
- Windows grants are stored in Credential Manager.
- Linux and macOS protected grant stores are not included in 1.0.0; use remote-only operation there.

Bearer tokens and signed offline grants must never be stored in `PlayerPrefs` or ordinary JSON configuration.

## Remote-only profile

The Remote-Only build comes from the same OGEngineClient codebase but excludes Edge Runtime and SQLite assemblies.
Choose it only when offline operation is intentionally disabled. It does not silently activate when Edge fails.

## Troubleshooting

- `OGENGINE_EDGE_NOT_INITIALIZED`: initialize the host before using Edge commands.
- `OGENGINE_BEARER_TOKEN_REQUIRED`: authentication did not provide a bearer token.
- `EDGE_SECURE_STORE_UNSUPPORTED`: the current platform has no qualified protected grant store.
- `HYPERDRIVE_REMOTE_UNAVAILABLE`: the client is operating offline; this is expected only when a valid offline
  session has already been established.
- Rejected commands remain visible until handled; inspect the returned error code rather than retrying through a
  different API path.

