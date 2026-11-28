# HyperDrive v2 remaining gates — 2026-11-27

Source-only audit. Parent Development `6426cde360bd1ae9f936ad06d82f2e1d469c45a1`; Core `41e46d939a03032d01b974453024e5ea4a609ced`. No global backend suite was run ahead of provider acceptance. Programme estimate remains about115/234; requested current-source rechecks2/3 (Firebase and Turso accepted, PocketBase owned elsewhere). Telos/SEEDS canonical-key fixes are accepted and integrated; Eclipse validation remains pending.

## Public switching control plane

The public manager-method manifest includes eight ProviderSwitcher methods as v2 runtime controls. This surface is not yet accepted:

1. `ProviderSwitcher` reads and writes `ProviderRegistry.Instance`, whose dictionaries/current-provider fields are independent of the shipping `ProviderManager` registration and activation state. Repository call-graph inspection finds only the helper classes and their refactoring README using this singleton.
2. All six Switch* methods update only an enum field. `SwitchProviderAsync` checks dictionary membership but never activates the selected provider, verifies its interface/lifecycle, or updates `ProviderManager.CurrentStorageProvider`. Success therefore does not establish a usable shipping provider switch.
3. Selection is published before asynchronous audit/metrics calls; a later exception reports failure while leaving the selected enum changed. Logging still contains TODO/scaffolding and catches exceptions with console output.
4. `GetSwitchStatus` returns `DateTime.UtcNow` as LastSwitchTime rather than the last acknowledged transition.

These are source findings, not claims that a hosted request failed. The main HyperDrive router calls `ProviderManager.SelectOptimalProviderForLoadBalancing`; its existing native triad evidence must be preserved rather than attributed to this separate helper. The root fix must establish one authoritative registry/activation state and acknowledge an actual successful transition. Do not add a second switching path or repair only the status display. Verify failed activation leaves the former selection usable, successful switching changes the actual provider used, and status records the acknowledged transition time.

## Release sequence

- Finish the currently claimed provider identity acceptance and respect Miden/PocketBase ownership. Hosted-account needs stay in the existing ACTIONS-FOR-DAVID list.
- Fix the public switcher against the canonical provider state, then run only changed control contracts and real eligible-provider checks.
- Preserve accepted Mongo/SQLite/SQL Server triad evidence. Remaining global acceptance includes hosted WEB4 multi-provider behavior and the broader eligible-provider fan-out; source presence or a unit test does not close these gates.
- Use isolated CI backends. Shared Atlas is not a test fixture, and protected deliveries535/543 must not be replayed/cleared/reauthorized.

Cleanup: no local build, environment, download, database or generated test output; source and CI metadata inspected only. Private indexes removed in finally. No owner action added for engineering work.

Checkpoint: Eclipse CI46101901966 and PocketBase CI46103379382 are now accepted; Miden current-graph/shipping-SDK integration remains in progress. Core6613656a preserves the audited switcher/registry files; no global backend test is being inferred from this source audit.
