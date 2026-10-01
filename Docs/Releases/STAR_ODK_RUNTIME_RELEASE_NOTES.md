# STAR ODK Runtime v4.0.1

This release advances the STAR ODK, STAR CLI and WEB5 gameplay runtime used to create and run OAPPs.

## What's new in v4.0.1

- Added the production cross-game quest pipeline, including ordered and any-order objectives, typed progress projections and durable completion transitions.
- Added GeoHotSpot quest triggers and Text, Image, Video and WebLink event payloads, with reliable creation, persistence and transaction handling.
- Integrated shared inventory, GeoNFT collection and quest progress with the unified HyperDrive v2/ONET offline synchronization pipeline.
- Expanded non-interactive STAR CLI operation and JSON output for scripts, AI agents and MCP clients while retaining interactive Light and STARNET workflows.
- Improved quest and inventory commands, objective authoring, status output and cross-platform DNA/configuration discovery.
- Centralized subscription and entitlement contracts through WEB4 and hardened WEB5 JWT/DNA startup behavior.
- Added production release, dependency-pin and cross-platform validation for Windows, Linux and macOS.

## Included runtime components

- `NextGenSoftware.OASIS.STAR.dll` — STAR engine and OAPP generation/runtime APIs.
- `NextGenSoftware.OASIS.STAR.CLI.dll` — STAR command-line application.
- `NextGenSoftware.OASIS.STAR.CLI.Lib.dll` — reusable CLI and STARNET UI library.
- `STAR` / `STAR.exe` — cross-platform CLI entry point with the packaged DNA templates.

This STAR ODK release uses OASIS Runtime v5.0.1.

Full changelog: <https://github.com/NextGenSoftwareUK/OASIS/compare/STAR-ODK-Runtime-v3.5.0...STAR-ODK-Runtime-v4.0.1>
