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

## V2 replication of owned writes

V2 data-API saves are owned conditional writes. `HolonManager` enrols each one for replication from the request's
effective routing (`HolonReplicationRoutes.Plan`): source = the authority provider, targets = every other provider in the
effective replication list that implements `ICommittedHolonReplicaStorageProvider`, provided the authority implements
`IAtomicHolonReplicationStorageProvider`. The authority records the obligations in its native outbox in the same
transaction as the write. An explicit `CosmicWriteOptions.ReplicationEnrollment` still wins.

`HolonReplicationDeliveryHostedService` (WEB4) delivers them: after every enrolled commit (`EnrolledWriteCommitted`) and
every 15 seconds for retries, it claims pending obligations per source/target pair and applies them with
`HolonOutboxDispatcher` + `CommittedHolonReplicaTarget` (leases, retries and reconciliation are the outbox's). Source and
target ids are `hyperdrive-source:<Provider>` / `hyperdrive-target:<Provider>`.

Verified on dev WEB4 (2026-10-28): one save appeared on MongoDB, SQLite and Neo4j within about two seconds; with MongoDB
taken offline for the visitor, a load starting at MongoDB was refused and served from a replica.
Tests: `HolonReplicationRoutesTests`; provider pipeline coverage in `MongoSqliteRealProviderRoutingTests.CommittedReplicas`.
