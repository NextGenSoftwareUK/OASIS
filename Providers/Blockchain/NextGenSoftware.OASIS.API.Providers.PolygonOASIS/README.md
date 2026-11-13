# NextGenSoftware.OASIS.API.Providers.PolygonOASIS

Connects the OASIS to Polygon -- Ethereum's leading L2 scaling solution offering EVM compatibility, dramatically lower fees, and near-instant finality. Stores OASIS holons, avatars, and NFTs on Polygon PoS or zkEVM with full Ethereum interoperability. Part of the OASIS HOT-Swappable Provider Architecture -- 55+ networks. Ecosystem: https://oasisomniverse.one

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.API.Providers.PolygonOASIS
```

[NuGet versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.PolygonOASIS) · [Source project](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolygonOASIS/NextGenSoftware.OASIS.API.Providers.PolygonOASIS.csproj)

Choose a published version compatible with the other OASIS packages in your application. This guide describes current source; older packages may not contain the same functionality.

## Frameworks and dependencies

Declared target frameworks: `net10.0`.

Direct external package references:

- `NextGenSoftware.OASIS.API.Core`

Project references and conditional dependency versions are recorded in the source project. NuGet restore resolves packaged dependencies; do not mix arbitrary DLLs from different release graphs.

## Integration and configuration

Use this package for the responsibility described above, alongside its owning OASIS runtime/provider integration. Inspect the source project's public APIs and owning component documentation for construction and configuration; this package installation command does not start an API server, authenticate an avatar or provision an external service.

Provider adapters require their respective service configuration and credentials. Runtime libraries require the host application's lifecycle and persistent paths where applicable. Supply only the settings actually required by the owning component; do not copy production DNA into a client or public release.

## Errors and security

For APIs returning `OASISResult<T>`, check `IsError`, `ErrorCode` and `Message`; do not treat a failed result as success. Preserve useful diagnostics without logging access tokens, passwords or private keys. Keep secrets in the host's appropriate protected configuration or platform storage.

## Platform requirements

Target-framework compatibility is not proof that every native dependency or external service works on every operating system. Check the selected version's platform assets and native/service requirements before deployment. Unity/mobile integrations require their supported platform layer and packaging workflow; a managed NuGet archive alone does not establish Android/iOS, IL2CPP or two-device convergence acceptance.

## Troubleshooting

- Restore/assembly mismatch: pin a coherent set of package versions and inspect the dependency graph.
- Configuration or authentication error: inspect the owning component's result diagnostics and service requirements without exposing secrets.
- Native loading failure: verify OS/architecture and required native artifacts rather than suppressing the error.
- Missing functionality in an older version: compare the immutable released source and release notes, not only the moving Development branch.

## Releases and further documentation

[Verified release matrix](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/WIP/Release and Status/OASIS_RELEASE_STATUS.md) · [Documentation audit](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/WIP/Release and Status/RELEASE_DOCUMENTATION_AUDIT.md)

Published NuGet versions are immutable. Updated documentation ships with a new unpublished package version after checked promotion and publication. This metadata-backed guide does not replace product-specific API examples or claim unverified platform tests.



## Explicit public Polygon PoS balance CQL profile

PolygonPublicAccountMetadataSource is a separate anonymous read representation.
It queries configured mainnet addresses; it does not use a wallet, submit a
transaction, scan accounts or write owned records. Canonical PolygonOASIS signed
storage remains separate. The common Web3Core EvmPublicAccountMetadataSource owns
RPC parsing, bounds, native selection and mapping; each provider supplies its
fixed chain/genesis/metadata descriptor.

This profile requires chain0x89and the pinned Polygon genesis. It resolves one
RPC-reported finalized block and reads every selected balance with its canonical
block hash. There is no latest-block/other-endpoint fallback. Metadata includes
PolygonAccountAddress, PolygonChainId, PolygonBlockHash, PolygonBlockNumber,
PolygonCommitment and PolygonBalanceWei. Balance values are exact decimal strings
in the chain's smallest native unit; they are not ETH-denominated values.

Configuration has exactly schema1, rpcUrl, chainId0x89, addresses and sourceOwner.
Use the retained nonsecret catalogue in LocalState/COSMIC/HostedPolygonPublicSourceCurrent.
One to eight lowercase20-byte addresses, anonymous root HTTPS,8KiB/depth4JSON and
256KiB/depth16RPC bounds apply. Signer fields/credential paths/auth headers and
unknown/duplicate properties are rejected. Keep the mapping owner stable.

```csharp
// Compose the exact bounded native address selection. No account scan occurs.
var query = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.PolygonOASIS).Result)
    .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(avatarId).Result)
    .Where(row => selectedAddresses.Contains(
        CosmicQueryMetadata.Value<string>(row, "PolygonAccountAddress")));

if (query.IsError)
    throw new InvalidOperationException(query.Message);

var plan = CosmicQueryPlanner.Plan(
    query.Result,
    ProviderType.PolygonOASIS,
    new[] { new CosmicQueryTarget(ProviderType.PolygonOASIS, source.QueryCapabilities) });

if (plan.IsError)
    throw new InvalidOperationException(plan.Message);

var rows = await source.ExecuteCosmicQueryAsync(
    plan.Result, cancellationToken: cancellationToken);

if (rows.IsError)
    throw new InvalidOperationException(rows.Message, rows.Exception);
```

Supply source from PolygonPublicAccountMetadataSource.FromJson and await its
ActivateQuerySourceAsync before executing, checking IsError. Content/name/description
filters, sorting, continuation and arbitrary chain queries are unsupported.
Public address-state rows do not prove that COSMIC holons were persisted on-chain
or cryptographically verify an RPC-reported finalized state.

Native3/2CQL passes;18controlled Polygon cases plus18affected Ethereum cases pass.
Ethereum native3/2and its published C# sample also pass after the shared-reader
extraction. Tests reuse the existing EthereumOASIS.CqlTests target with a Polygon
reference; no separate runtime/node/environment is required.

WEB4 query-only admission, HTTP discovery/presets, Mongo federation/isolation,
replication and hosted/browser acceptance remain next. No hosted Polygon enrollment
or complete ORM parity is claimed. The retained local signed-record demo is a
different profile. [Protocol readiness probe](../../../Scripts/Sandbox/Test-PolygonPublicMetadataCandidate.ps1).

WEB4 task integration now registers CQL_POLYGON_PUBLIC_SOURCE_JSON through the
isolated query-only profile registry. Explicit read permission/tariff remain
separate from signed Polygon storage. Native HTTP acceptance passes3/2and Mongo
pairing5/4with foreign-private0/public-pair3; controlled bearer/usage infrastructure
is not production login/billing/replication/browser proof.

Use the existing fixture-aware runner -PolygonMongoHttp after a leased bounded
build. It retains original intents/ACKs and removes only the owner-checked Mongo
fixture on success. Do not replay uncertain writes or replace a retained cohort.

Four Polygon presets can be staged individually with
Publish-HostedPolygonPublicConfiguration.py. For the joint Google/Ethereum/Polygon
release, prepare Publish-HostedGoogleEthereumConfiguration.py with-polygon. That
explicit option composes all3strict validators into5changed values/12presets,
preserving wallet DNA and each independent permission/free tariff. No live change
occurs during preparation. Use only the matching tested application/Core revision
for the scheduled rollout; apply-with-polygon is the explicit journalled mutation.
Encrypted snapshots require an explicit PolygonAccountAddress queryable-key grant;
the publisher refuses missing policy rather than changing global reserved keys.

Runnable console and HTTP walkthrough: [public Polygon CQL guide](../../../Docs/Devs/POLYGON_PUBLIC_METADATA_CQL_GUIDE.md).
The --polygon-public console mode passes native3/2through HolonManager/LINQ,
with a native test compiling the actual published sample files.
