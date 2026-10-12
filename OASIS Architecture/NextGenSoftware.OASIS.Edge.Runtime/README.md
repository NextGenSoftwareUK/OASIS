# OASIS Edge Runtime

Offline-first application runtime for durable device-local data and synchronization with a hosted OASIS node. This is a reusable library, not a standalone server or a bundled Holochain conductor.

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.Edge.Runtime
```

[NuGet package and versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.Edge.Runtime) · [Source and examples](https://github.com/NextGenSoftwareUK/OASIS/tree/Development/OASIS%20Architecture/NextGenSoftware.OASIS.Edge.Runtime)

Targets `netstandard2.1` and `net8.0`. NuGet resolves Contracts, HyperDrive Synchronization and EdgeSQLite provider dependencies. For Unity, use the repository's reviewed Edge packaging workflow and platform integrations rather than copying arbitrary server assemblies into Assets.

## Features

- SQLite-backed local state and a durable mutation outbox: local writes do not require connectivity.
- Typed entity save, delete, load and enumeration through `Entities`; queued commands include NFT mint and GeoHotSpot operations. Queuing is not proof of hosted execution.
- Explicit synchronization and connectivity-triggered recovery, bounded exchanges, command-outcome polling and snapshot/rebase support through HyperDrive.
- Durable conflict inspection/resolution, pending-operation counts and status notifications.
- Optional secure offline-session grants with avatar/device binding, scope checks and a host signing-key validator.
- Optional durable local replication targets, configured before the store opens so offline mutations retain projection work.
- Suspend/resume lifecycle and asynchronous disposal.

## Host integration

Supply a stable device ID, authenticated avatar ID, writable persistent database path and an authenticated `IHyperDriveSyncTransport`. Do not generate a new device ID on each launch or use a shared database for unrelated avatar/device identities.

The following composition helper uses real API signatures. The host supplies its transport and connectivity monitor; it is not a complete authentication implementation.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Edge.Runtime;

static async Task<OASISEdgeRuntime> OpenEdgeAsync(
    Guid deviceId, Guid avatarId, string databasePath,
    IHyperDriveSyncTransport transport, IEdgeConnectivityMonitor monitor,
    CancellationToken cancellationToken)
{
    var runtime = new OASISEdgeRuntime(new EdgeRuntimeOptions
    {
        DeviceId = deviceId,
        AvatarId = avatarId,
        DatabasePath = databasePath
    }, transport);

    var result = await runtime.StartConnectivityMonitoringAsync(monitor, cancellationToken);
    if (result.IsError)
        Console.Error.WriteLine($"{result.ErrorCode}: {result.Message}");
    // Monitoring may remain attached after an initial hosted-service outage.
    // Keep the instance alive for recovery; dispose it when the host shuts down.
    return runtime;
}
```

Observe `StatusChanged` and inspect `OASISResult<T>.IsError`, `ErrorCode` and `Message` for each operation. `SaveLocalAsync` or `Entities.SaveAsync` acknowledges local persistence, not server convergence. Use `SynchronizeAsync`, conflict APIs and status/outcome information to track remote acceptance.

## Configuration

| Option | Default / requirement |
|---|---|
| `DeviceId`, `AvatarId` | Required, nonempty GUIDs |
| `DatabasePath` | Required persistent writable path |
| `MaximumOperationsPerExchange` | 100 |
| `MaximumRemoteChangesPerExchange` | 100 |
| `MaximumExchangesPerRun` | 20 |
| `CommandOutcomePollInterval` | 500 ms |
| `HostedServiceRecoveryInterval` | 30 seconds |
| `MaximumHostedServiceRecoveryInterval` | 5 minutes |
| `LocalReplicationTargetIds` | Empty; declare stable target IDs before opening the runtime |
| `MaximumLocalReplicationsPerRun` | 100 |
| `PayloadSerializer` | Optional application serializer; additional typed entities may need generated metadata |

## Security and platform requirements

Configure secure session storage and offline-grant validation together, or leave both absent. Request grants with `AcquireOfflineSessionAsync` and restore them with `ResumeOfflineSessionAsync`; use only required scopes and a pinned trusted host signing key. Never embed credentials/private keys in templates or persist tokens in plaintext.

Desktop implementations include Windows Credential Manager, macOS Keychain and Linux Secret Service. Linux requires an available Secret Service/session environment; secure-storage errors are not permission to fall back to plaintext. Mobile secure storage and lifecycle integration are supplied by the host/platform layer.

Managed target-framework compatibility does not establish native SQLite, Unity IL2CPP or physical-device compatibility. Physical Android/iOS runs, two-device convergence and Linux/macOS Unity IL2CPP verification remain separate acceptance tasks. Holochain and ONET integrations are separate components, not automatically enabled by installing this package.

## Troubleshooting

- Invalid construction: check IDs, database path and positive exchange/recovery limits.
- Offline session unavailable: configure both secure storage and the grant validator, plus a grant-capable authenticated transport.
- Pending changes: inspect connectivity, transport authentication, `PendingOperationCount`, conflicts and `LastErrorCode` before retrying.
- Local replication unavailable: configure the target ID before construction, then attach the matching target.
- Shutdown: await `DisposeAsync`; forward host lifecycle transitions to `SuspendAsync` and `ResumeAsync`.

## Related packages and release status

[Edge ONET Runtime](https://www.nuget.org/packages/NextGenSoftware.OASIS.Edge.ONET.Runtime) adds authenticated ONET composition. [Release inventory](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/Devs/OASIS_RELEASE_STATUS.md) separates published versions from pending builds and records verification limits. This README describes current source; consult the selected package version's release notes before assuming every feature is in an older package.
