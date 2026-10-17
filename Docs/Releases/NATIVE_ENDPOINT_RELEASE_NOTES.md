# OASIS API Integrated Native Endpoint v2.0.2

This is the current in-process entry point for applications that embed the WEB4 OASIS API instead of calling the HTTP service.

## What's new in v2.0.2

- Updated the embedded OASIS Runtime to v5.0.1 and .NET 10.
- Added the unified HyperDrive v2, ONET and offline synchronization services used for inventory, GeoNFT collection and quest progress.
- Added offline session-grant verification and reconnect synchronization for native applications and games.
- Included the expanded first-party provider graph with capability selection, automatic failover and replication.
- Hardened startup, OASIS DNA loading, dependency resolution and native publish output.
- Added reproducible GitHub/NuGet release checks and exact private dependency pins.

## Usage

Use this endpoint when an OAPP, desktop application or game needs the OASIS API in the same process. It exposes the runtime without HTTP overhead while retaining the same provider abstraction and online/offline behavior as WEB4.

NuGet package: <https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint>

Live WEB4 API for HTTP clients: <https://api.oasisweb4.one>

Full changelog: <https://github.com/NextGenSoftwareUK/OASIS/compare/v1.0.1...Native-Endpoint-v2.0.2>
