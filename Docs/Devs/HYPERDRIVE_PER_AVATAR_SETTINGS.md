# HyperDrive per-avatar settings and the WEB4 sandbox controls

The OASIS DNA holds each node's **default** HyperDrive routing. Every avatar can override it for its own requests, and a
single request can override both. Precedence, per value:

```
request (autoFailOverMode, autoReplicationProviders, ... on that call)
  > avatar (saved settings)
    > node defaults (OASIS DNA / published runtime overrides)
```

The HyperDrive **mode** (Legacy V1 or V2) stays node-wide. New work targets V2; Legacy switches the active provider for
the whole process on every request, so it cannot be shared safely between concurrent users.

## What an avatar can override

Saved under the avatar's settings category `hyperDrive` (avatar MetaData `preferences` → `hyperDrive`):

| Key | Value |
|---|---|
| `autoFailoverEnabled`, `autoReplicationEnabled`, `autoLoadBalancingEnabled` | `true` / `false`; `null` or `"DEFAULT"` removes the override |
| `autoFailoverProviders`, `autoReplicationProviders`, `loadBalancingProviders` | ordered provider names (array or comma-separated); each must be registered on the node; `null`/`"DEFAULT"` removes it, an empty list is an explicit "none" |

Set them with the existing settings API (`SettingsController` HyperDrive endpoint, GraphQL or gRPC →
`SettingsManager.UpdateHyperDriveSettingsAsync`). Invalid values are refused with `AVATAR_HYPERDRIVE_SETTINGS_INVALID`,
never ignored. `GetHyperDriveSettingsAsync` returns the node defaults as before, plus `avatarOverrides` (only the keys this
avatar set) and `effective` (what its requests actually use).

## How it is applied

- `JwtMiddleware` loads the avatar for every authenticated request and sets `OASISRequestContext.AvatarRoutingOverrides`
  (`AvatarHyperDriveSettings.FromAvatar`). Saved settings that no longer parse fail the request with HTTP 409.
- `OASISControllerBase` puts a call's own routing parameters in `OASISRequestContext.RequestRoutingOverrides` and clears
  them afterwards. It no longer flips node-wide `ProviderManager` flags per request, which raced between concurrent
  requests. `SetGlobally=true` still publishes node defaults.
- `ProviderManager`'s `IsAutoFailOverEnabled` / `IsAutoReplicationEnabled` / `IsAutoLoadBalanceEnabled` and the three list
  getters answer with the request's effective value (AsyncLocal, so concurrent requests never see each other's).
  `PublishRoutingOverrides` captures node defaults only; `ResolveRoutingOverrides` and `CreateRoutingScope` start from the
  request's effective view; routing scopes then ignore the ambient context.
- Setters change only the node default.

Code: `Core/Configuration/AvatarHyperDriveSettings.cs`, `ProviderRoutingOverrides.Layer`, `OASISRequestContext`,
`ProviderManager.cs` / `.RequestPolicy.cs`. Tests: `AvatarHyperDriveRoutingTests`.

## Sandbox controls (per visitor)

Nodes with `OASIS_SANDBOX=true` expose `api/sandbox` (dev WEB4 does; production must not):

- `POST session` creates a verified demo avatar whose HyperDrive settings route only across MongoDB, SQLite and Neo4j.
- `POST providers/{p}/offline?seconds=` / `online`, `GET faults`, `GET refusals` act **only for the calling avatar**:
  `ProviderFaultGate` keys faults by avatar and checks `OASISRequestContext.CurrentAvatarId`. Other users, other demo
  visitors and background work never see them. V2 refuses a faulted provider with `HYPERDRIVE_PROVIDER_OFFLINE`
  (failover-eligible) before provider I/O and without recording it in shared provider metrics.
- `POST native-demo` seeds the demo shop for [native data import](NATIVE_DATA_IMPORT.md).

The public page is `web4.oasisomniverse.one/web4-sandbox.html`, pointed at `dev.api.web4.oasisomniverse.one`.

## Known V2 gap: API saves do not replicate (verified on dev WEB4, 2026-10-28)

V2 saves through the data API are owned conditional writes. They replicate only when the write carries a
`ReplicationEnrollment` (`CosmicWriteOptions.ReplicationEnrollment` → `ConditionalHolonWrite`), which makes the authority
provider record the replication atomically (`IAtomicHolonReplicationStorageProvider.ExecuteReplicatingHolonWrite`), and
delivery to the targets then needs the hosted outbox pipeline (`OASISHyperDriveConfig.EnableHostedSync`). Today the API
never sets the enrollment and dev runs with hosted sync off, so each save lands only on the provider load balancing picks.
Failover itself works (a faulted provider is refused and the next one is tried), but the next provider has no copy.
The sandbox page shows this as MISSING copies rather than hiding it. Fix belongs to the HyperDrive V2 provider work:
enrol replication from the effective routing policy (authority + replication list) and run outbox delivery.
