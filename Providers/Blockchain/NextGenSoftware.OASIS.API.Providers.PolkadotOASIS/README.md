# NextGenSoftware.OASIS.API.Providers.PolkadotOASIS

Polkadot adapter under implementation. RPC connectivity is verified; storage, signing, token/NFT and bridge capabilities are not production-certified.

## Current implementation and verification limits

Activation uses HTTP JSON-RPC `system_chain` and `chain_getBlockHash(0)`, validates response version/correlation/result and configured network before publishing activation. Constructor `chainId` accepts the exact reported chain name (case-insensitive) or a `0x` genesis hash. Use a genesis hash for stronger network identity; a human-readable name is not globally unique. Deactivation preserves the reusable HTTP client; `Dispose` releases it. Each activation RPC has a ten-second deadline. Connection activation is not proof that every advertised capability works.

Explicit opt-in read-only tests use `https://rpc.polkadot.io`, checking name/genesis configuration and sync/async balance reads (including a public non-zero-balance account) without keys, signing, transfers or transaction fees. Controlled tests cover malformed/failed/miscorrelated replies, wrong networks, reuse and exact unsigned native units. These are not full-provider tests.

`GetBalance`/`GetBalanceAsync` now use pinned `@polkadot/api`17.0.2 to decode runtime metadata and finalized `System.Account` storage. They verify the SDK genesis against the activated network, read authoritative token precision, preserve u128 base units through decoding and convert only at the existing `double` interface boundary (display precision is approximate). HTTP/RPC/SDK failures are errors, not invented zero balances. Native free balance is not a promise that all free funds are transferable; freezes/reserves/fees require separate checks before signing. Node must be installed once, and locked SDK dependencies provisioned once with `npm ci --ignore-scripts --no-audit --no-fund` in `SdkBridge`. Reuse that installation. Configure `OASIS_POLKADOT_NODE` and `OASIS_POLKADOT_SDK_BRIDGE` when not using the published bridge directory. Reads have a45second process deadline; the owned process tree is stopped on cancellation. Docker bridge packaging now includes `PolkadotSdkBridge`; an actual hosted image/deployment has not yet been verified.

OPEN: existing storage methods assume REST routes that are not standard node RPC; transaction methods construct unsigned JSON-shaped extrinsics rather than supported signed SCALE submissions; bridge payloads and NFT/storage runtime assumptions require replacement and native proof. Private on-chain record encryption/queryability, real SDK signing, exact amounts/fees/finality/reconciliation and all relevant interfaces remain unfinished. Do not use this adapter for production storage/minting or claim full XCM/relay/parachain support from a successful activation.

Official references: [Polkadot transaction SDKs](https://docs.polkadot.com/chain-interactions/send-transactions/with-sdks/), [SDK lifecycle guidance](https://docs.polkadot.com/reference/tools/polkadot-js-api/), [runtime queries](https://polkadot.js.org/docs/api/). Polkadot.js is in maintenance mode, not active feature development; PAPI/Dedot are recommended for new applications. This narrowly scoped dynamic-metadata reader uses its existing supported account-storage interface and does not imply new chain-feature support. One pinned SDK installation was added and retained; no local chain was installed. Native signing/storage replacement remains OPEN.

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
```

[NuGet versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS) · [Source project](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.csproj)

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

[Verified release matrix](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/Devs/OASIS_RELEASE_STATUS.md) · [Documentation audit](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/Devs/RELEASE_DOCUMENTATION_AUDIT.md)

Published NuGet versions are immutable. Updated documentation ships with a new unpublished package version after checked promotion and publication. This metadata-backed guide does not replace product-specific API examples or claim unverified platform tests.

