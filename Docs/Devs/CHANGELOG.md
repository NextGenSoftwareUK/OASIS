# OASIS Changelog

---

## [Unreleased] — 2026-08-08

### Bug Fixes

- **ONETProtocol deadlock** — removed `Task.Run(InitializeAsync).GetAwaiter().GetResult()` from the `ONETProtocol` constructor; moved `await InitializeAsync()` to the top of `StartNetworkAsync()`. Prevented a sync-over-async deadlock that caused silent network initialisation failure under ASP.NET Core's synchronisation context.
  - `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/ONET/ONETProtocol.cs`

- **`karmaSourceTitle` typo** — renamed parameter `karamSourceTitle` → `karmaSourceTitle` across 13 files in Core, Providers, and Controllers.
  - `OASIS Architecture/.../OASISStorageProviderBase.cs`
  - `OASIS Architecture/.../Managers/KarmaManager.cs`
  - `OASIS Architecture/.../Managers/AvatarManager-Karma.cs`
  - `OASIS Architecture/.../Managers/AvatarManager-OLD.cs`
  - `OASIS Architecture/.../Interfaces/Providers/IOASISStorageProvider.cs`
  - `OASIS Architecture/.../Interfaces/Avatar/IAvatarDetail.cs`
  - `OASIS Architecture/.../Holons/AvatarDetail.cs`
  - `OASIS Architecture/.../Holons/Avatar.cs`
  - `Providers/Cloud/.../AzureCosmosDBOASIS/Entities/AvatarDetail.cs`
  - `Providers/Cloud/.../AzureCosmosDBOASIS/Entities/Avatar.cs`
  - `Providers/Storage/.../LocalFileOASIS/LocalFileOASIS.cs`
  - `ONODE/.../Controllers/KarmaController.cs`
  - `ONODE/.../Controllers/AvatarController.cs`

### Refactors

- **`IOASIStorageProvider.cs` → `IOASISStorageProvider.cs`** — filename now matches the interface name declared inside it (`IOASISStorageProvider`). All 40+ provider implementations reference the interface by name, not file path, so there are no breaking changes.
  - `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Providers/`

- **`AcceptnviteToJoinSeedsUsingAvatarRequest.cs` → `AcceptInviteToJoinSeedsUsingAvatarRequest.cs`** — typo in filename fixed; class name inside was already correct.
  - `ONODE/.../Models/SEEDS/AcceptInviteToJoinSeedsRequest/`

- **AvatarController split** — the 2,907-line monolithic `AvatarController.cs` has been split into three focused controllers all mounted on `[Route("api/avatar")]` so the public API contract is unchanged:

  | New controller | Lines | Responsibility |
  |---|---|---|
  | `AvatarAuthController.cs` | 512 | register, verify-email, authenticate, DID auth, refresh/revoke token, forgot/reset password |
  | `AvatarProfileController.cs` | 1,457 | profile CRUD, portraits, karma, XP, quests, UMA, search, get-all/get-by-* |
  | `AvatarAdminController.cs` | 599 | session management, inventory management |

  The original `AvatarController.cs` (378 lines) is retained as an archive with legacy key-management code commented out for reference.

### New Features

- **WEB6 Quick-Start Guide** — `Docs/Devs/WEB6_QUICKSTART.md`
  Five-step walkthrough covering: boot OASIS, register avatar, seed FAHRN agent pool, `DispatchAsync`, save result as Holon. Includes `OASIS_DNA.json` config block, full C# example, REST curl examples, and dispatch-mode reference table.

- **OASIS Sandbox playground** — `Oportal-DevPortal/sandbox/index.html`
  Live developer playground deployed at `https://sandbox.oasisomniverse.one`. Terminal-style UI with 10 pre-built examples spanning all three API layers:
  - WEB6: FAHRN Dispatch (serial), FAHRN Dispatch (parallel), SkillOpt Evolve
  - WEB5: Create Mission, Create Celestial Body, Mint NFT
  - WEB4: Save Holon, Load Holon, Register Avatar, HyperDrive Status

### Documentation

- `Docs/Devs/API Documentation/WEB4 OASIS API/Avatar-API.md` — added controller-structure table reflecting the split above
- `Docs/Devs/API Documentation/WEB4 OASIS API/ONET-API.md` — added Known Issues section documenting the ONETProtocol deadlock fix
- `Docs/Devs/WIKI_DOCUMENTATION_INDEX.md` — added WEB6 section with links to quick-start, REST reference, MCP reference, user guide, and sandbox
- `Docs/Devs/WEB6_QUICKSTART.md` — FAHRN endpoint corrected from `/api/fahrn/dispatch` to `/v1/fahrn/solve`

---

## Format

Entries follow [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) conventions.
Versions follow [Semantic Versioning](https://semver.org/).
