# OASIS Edge ONET Runtime

Composes OASIS Edge Runtime with an authenticated ONET application-message channel: durable local storage, peer identity binding, signed capability advertisements and discovery-based HyperDrive synchronization. It does not load a Full ONODE, BootLoader or ASP.NET server on the device.

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.Edge.ONET.Runtime
```

[NuGet package and versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.Edge.ONET.Runtime) · [Source](https://github.com/NextGenSoftwareUK/OASIS/tree/Development/OASIS%20Architecture/NextGenSoftware.OASIS.Edge.ONET.Runtime)

Targets `netstandard2.1` and `net8.0`; depends on Edge Runtime and ONET. Install this composition when the application uses ONET, or use Edge Runtime directly with another supported authenticated synchronization transport.

## Features

- All underlying Edge Runtime local persistence, durable outbox, synchronization, conflict and lifecycle APIs through `EdgeRuntime`.
- Authenticated bootstrap peer binding; the platform identity must match the channel's local node ID.
- Signed, expiring Edge-profile capabilities advertising `hyperdrive-sync-v3` and `offline-first`.
- Default EdgeSQLite capabilities: durable outbox, local read/write and snapshot rebase; hosts may supply eligible additional provider capabilities.
- Capability discovery targeting a Full-profile HyperDrive synchronization service.
- Optional federated capability registries with explicit quorum, renewal diagnostics and warnings.
- Offline grants through a host-provided bootstrap transport and secure session storage/validation.

## Compose and start

The host must provide:

1. `EdgeRuntimeOptions` with stable device/authenticated avatar IDs and persistent database path.
2. An authenticated `IONETApplicationMessageChannel` and hosted ONODE ID.
3. An authenticated `IHyperDrivePeerBindingTransport` for bootstrap identity binding.
4. A platform-secure `IEdgeNodeIdentity` matching `channel.LocalNodeId`, and an `IEdgeConnectivityMonitor`.
5. If offline sessions are enabled: grant transport, secure session store and pinned signing-key validator.

Example inside your host's asynchronous startup method, using those already-configured objects:

```csharp
using NextGenSoftware.OASIS.Edge.ONET.Runtime;

await using var runtime = new OASISEdgeOnetRuntime(
    options, channel, hostedNodeId, peerBindingTransport,
    offlineGrantTransport: grantTransport,
    secureSessionStore: secureSessionStore,
    offlineGrantValidator: grantValidator);

var started = await runtime.StartAsync(identity, connectivityMonitor, cancellationToken);
if (started.IsError)
    logger.LogError("{Code}: {Message}", started.ErrorCode, started.Message);

// Keep this scope alive for the application session. Use runtime.EdgeRuntime
// for local entities, synchronization, offline grants and suspend/resume.
await RunApplicationSessionAsync(runtime.EdgeRuntime, cancellationToken);
```

`options`, transports, identity, monitor, logger and session lifetime are host dependencies, not objects supplied by this snippet. Keep the runtime alive until the session ends. This package does not log in an avatar or create an authenticated network channel for you.

## Startup and lifecycle contract

`StartAsync` checks identity, binds the peer, publishes capabilities, then attaches connectivity monitoring. Identity/binding/advertisement failure is an error, not successful startup. An initial synchronization error after monitoring attaches can leave the local lifecycle running for hosted recovery; inspect the returned result and underlying status instead of repeatedly calling `StartAsync` (a second start returns `EDGE_ONET_ALREADY_STARTED`).

Forward suspension/resumption to `EdgeRuntime.SuspendAsync` / `ResumeAsync`. Await `DisposeAsync` to stop capability renewal, dispose the underlying Edge runtime and release the request/response endpoint. Channel ownership outside that endpoint remains the host's responsibility.

## Configuration and diagnostics

| Setting | Default / requirement |
|---|---|
| `hostedNodeId` | Required nonempty ID |
| `peerBindingTransport` | Required authenticated bootstrap transport |
| `capabilityLifetime` | 10 minutes |
| `capabilityRenewalInterval` | 5 minutes; positive and shorter than lease lifetime |
| `capabilityRegistryNodeIds` | Hosted node by default; nonempty distinct registry set |
| `capabilityRegistryQuorum` | All configured registries; between 1 and registry count |
| `eligibleProviderCapabilities` | Optional additions to default EdgeSQLite capabilities |

Inspect `LastCapabilityAdvertisementError`, `LastCapabilityAdvertisementWarning`, `EdgeRuntime.Status` and `EdgeRuntime.StatusChanged`. Preserve error codes/messages in application diagnostics. Signed capability discovery does not remove hosted authorization requirements or grant arbitrary server-provider access.

## Security, platforms and limitations

Do not put node private keys, credentials or avatar tokens in a public DNA template. Use platform-secure identity/session storage and trusted host grant-validation keys. Secure-store and offline-validator arguments must be configured together.

Windows/Linux/macOS managed applications and Unity-compatible framework targets still require suitable platform/native dependencies. This NuGet package is not a desktop installer, conductor APK or iOS xcframework. Android shared-conductor integration reports/logs shared-runtime failure and recovers to the built-in conductor by design in its owning platform layer; it is not an ONET transport fallback provided here.

Physical Android/iOS runs, two-device convergence and Linux/macOS Unity IL2CPP execution must be verified separately; successful hosted compilation or packaging is not proof of those tests.

## Troubleshooting and further reading

- `EDGE_ONET_IDENTITY_MISMATCH`: the signing identity and authenticated channel must use the same node ID.
- Binding failure: verify hosted bootstrap authentication and persisted avatar/device/node scope.
- Advertisement failure: check registry availability, signature/lease validation and configured quorum.
- Synchronization failure: check advertised Full-node service availability and underlying Edge status/errors.
- Offline grants: see the [Edge Runtime usage and security guide](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/OASIS%20Architecture/NextGenSoftware.OASIS.Edge.Runtime/README.md).

[Release inventory](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/Devs/OASIS_RELEASE_STATUS.md) lists verified publications and outstanding gates. Current-source documentation may contain functionality not present in older published versions.
