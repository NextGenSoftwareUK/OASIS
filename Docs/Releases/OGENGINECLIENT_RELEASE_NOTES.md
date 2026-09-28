# OGEngineClient v2.0.2

STAR API Client is now **OGEngineClient**, the native game integration layer for the WEB4 OASIS API and WEB5 STAR API.

## What changed

- Added production shared inventory, GeoNFT and quest synchronization across Our World, ODOOM and OQUAKE.
- Added offline session grants and durable reconnect synchronization through the unified Edge/ONET/HyperDrive v2 pipeline.
- Added typed quest-progress and inventory projections so native games consume the same authoritative state as WEB4 and WEB5.
- Added C and C++ exports for login/session handling, inventory, quests, GeoNFTs, portals, teleport/arrival events and synchronized gameplay transitions.
- Expanded OGEngine integrations for ten game engines and editors, including entity spawning, map portals and incoming teleport polling.
- Split the native client and export implementation into focused modules, with clean-build, ABI/export and cross-game transition checks.
- Added production dependency pins and release artifacts that match the headers and native libraries used by ODOOM and OQUAKE.

## Integration

The package contains the managed client and native bindings used by C/C++ games. It is the supported successor to STAR API Client and keeps the same goal: one authenticated avatar, inventory and quest state across every connected game and real-world experience.

Build and integration documentation: [OGEngine overview](https://github.com/NextGenSoftwareUK/OASIS/blob/master/OASIS%20Omniverse/Docs/OGEngine_Overview.md) and [STAR games user guide](https://github.com/NextGenSoftwareUK/OASIS/blob/master/OASIS%20Omniverse/Docs/STAR_Games_User_Guide.md).

Full changelog: <https://github.com/NextGenSoftwareUK/OASIS/compare/STAR-API-CLIENT-v1.0.0...OGEngineClient-v2.0.2>
