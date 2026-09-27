# OGEngineClient for Unity

## What this package provides

The Edge-enabled OGEngineClient keeps supported gameplay reads and commands available when connectivity disappears.
It commits local work to a durable SQLite journal, reports its state through `EdgeRuntimeStatus`, and reconciles
idempotently with the hosted ONODE when connectivity returns.

The runtime does not turn server-authoritative actions into unsafe local writes. Inventory rewards, Karma changes,
quest completion and GeoNFT ownership are queued as commands and become authoritative only after the ONODE accepts
them.

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

`OASISEdgeUnityHost` suspends and resumes the client on Unity application pause events and disposes it when destroyed.
Use one host per application database. Do not create parallel clients against the same journal.

## Platform security

- Android grants are encrypted with an Android Keystore key.
- iOS grants are stored in Keychain.
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

