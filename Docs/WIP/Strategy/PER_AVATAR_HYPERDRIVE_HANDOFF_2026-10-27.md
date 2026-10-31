# Per-avatar HyperDrive V2 overrides + per-visitor offline — handoff (2026-10-27)

Goal (user-approved): per-avatar HyperDrive V2 overrides (failover/replication/load-balance on/off and provider lists;
DNA = defaults), per-visitor "take provider offline", V1 stays server-wide, then move the WEB4 sandbox into dev WEB4
and delete the sandbox service plus the Railway MongoDB and Neo4j (ask before deleting). Neo4j → AuraDB Free (user
creates the instance) with a free daily GitHub Actions keep-awake ping; Mongo → Atlas.

## Done, uncommitted (Core submodule, branch fix/hyperdrive-v2-providers)
- `ProviderFaultGate.cs`: faults keyed by (avatarId, provider); `IsFaulted` uses `OASISRequestContext.CurrentAvatarId`.
  New signatures: `Fault(avatarId, p, d)`, `Restore(avatarId, p)`, `GetFaulted(avatarId)`, `GetRefusals(avatarId)`.
- `Configuration/ProviderRoutingPolicy.cs`: `ProviderRoutingOverrides.Layer(lower, upper)`.
- `OASISRequestContext.cs`: `AvatarRoutingOverrides`, `RequestRoutingOverrides`, `EffectiveRoutingOverrides` (AsyncLocal).
- `ProviderManager.cs/.Failover.cs/.LoadBalance.cs`: flags and the three list getters return the ambient effective
  value (request > avatar > node default); routing scopes (`_isRoutingScope`) ignore ambient.
- `ProviderManager.RequestPolicy.cs`: `CaptureRoutingPolicyCore(includeRequestContext)`; Publish uses node defaults only.
- `OASISHyperDrive.cs`: both provider-acquisition sites return `HYPERDRIVE_PROVIDER_OFFLINE` when faulted (failover-eligible).
- Do NOT commit `Queries/CosmicJoinQuery.cs` (another agent's change).

## Remaining
1. `OASISControllerBase.cs`: non-`SetGlobally` path must set `OASISRequestContext.RequestRoutingOverrides` instead of
   mutating `ProviderManager.Instance` flags/lists (race across concurrent requests); `ResetOASISSettings` clears it.
   (Last attempt was rejected by the user mid-session for token reasons, not design.)
2. `JwtMiddleware`: after loading the avatar, set `AvatarRoutingOverrides` from avatar MetaData
   `preferences.hyperDrive` keys: autoFailoverEnabled, autoReplicationEnabled, autoLoadBalancingEnabled,
   autoFailoverProviders, autoReplicationProviders, loadBalancingProviders (store lists as comma strings, bools as bool).
3. `SettingsManager.UpdateHyperDriveSettingsAsync`: validate/normalize those keys (registered providers only);
   `GetHyperDriveSettingsAsync`: add `avatarOverrides` and `effective`.
4. `SandboxController`: use avatar-scoped fault calls (`AvatarId`); give new demo avatars overrides
   MongoDBOASIS, SQLLiteDBOASIS, Neo4jOASIS so demos avoid chains.
5. Tests: layering precedence, scope isolation, per-avatar fault isolation. Build in `C:\b2` export (long paths).
6. Dev WEB4 Railway: `OASIS_SANDBOX=true`, DNA `MongoDBOASIS.NativeImportDBName=SANDBOX_NATIVE_APPS`, Neo4j → AuraDB.
7. Web4Site `web4-sandbox.html`: `SANDBOX` → `https://dev.api.web4.oasisomniverse.one`; Import tab is written, unpushed.
8. Verify V2 failover/replication live (never verified yet), then one batched push to Development.

## Context
- Sandbox (Legacy V1) live and verified: failover, replication, native import (5 created, re-import 5 updated, holon
  read from Mongo/SQLite/Neo4j). Guide: `Docs/Devs/NATIVE_DATA_IMPORT.md`.
- Dockerfile.web4 back to `-m:2` on the fix branch only; Actions run 40564304242 tests it.
- Fix branch is ahead of Development; batch dev pushes (each push rebuilds every Railway service).
