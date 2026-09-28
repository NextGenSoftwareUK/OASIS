# OASIS Runtime v5.0.1

This release advances the native OASIS Runtime used by OAPPs and by the live WEB4 API to the production source graph released on 28 September 2026.

## What changed

- Unified HyperDrive v2, ONET and offline synchronization around the hosted provider contract, with durable commands, outcomes, checkpoints, snapshots and provider fan-out.
- Added end-to-end offline session grants and synchronization for shared inventory, GeoNFT collection and quest progress.
- Preserved configured HyperDrive provider selection, automatic failover and background replication while adding capability-based routing and authoritative-provider handling.
- Hardened MongoDB synchronization, primary handoff, transaction ordering, canonical Holon identity and inventory/quest projections.
- Added and completed first-party Web2, Web3, storage, identity, maps, AI and network providers, with CI integration coverage across the provider matrix.
- Upgraded the runtime and dependency graph for .NET 10 and removed known package restore and security conflicts.
- Added production release automation, exact submodule/dependency pins and reproducible package metadata checks.

## Runtime use

The OASIS Runtime supplies the in-process WEB4 engine used by the Native Integrated Endpoint and generated OAPPs. Applications can use the same identity, provider, data, NFT, GeoNFT, inventory, ONET and HyperDrive services online or offline without changing application contracts.

Live WEB4 API: <https://api.oasisweb4.one>

NuGet entry point: [NextGenSoftware.OASIS.API.Native.Integrated.EndPoint](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint)

Full changelog: <https://github.com/NextGenSoftwareUK/OASIS/compare/OASIS-Runtime-v4.5.1...OASIS-Runtime-v5.0.1>
