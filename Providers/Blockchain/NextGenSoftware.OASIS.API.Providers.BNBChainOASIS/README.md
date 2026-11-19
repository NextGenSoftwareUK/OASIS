# NextGenSoftware.OASIS.API.Providers.BNBChainOASIS

Connects OASIS to BNB Smart Chain through Nethereum. The Web3Core storage contract stores avatars, avatar details and holons. The adapter also exposes blockchain wallet and NFT operations.

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.API.Providers.BNBChainOASIS
```

[NuGet versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.BNBChainOASIS) · [Source project](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BNBChainOASIS/NextGenSoftware.OASIS.API.Providers.BNBChainOASIS.csproj)

Choose a published version compatible with the other OASIS packages in your application. This guide describes current source; older packages may not contain the same functionality.

## Frameworks and dependencies

Declared target frameworks: `net10.0`.

Direct external package references:

- `NextGenSoftware.OASIS.API.Core`
- `Nethereum.RPC`
- `Nethereum.Signer`
- `Nethereum.Web3`
- `Nethereum.HdWallet`

Project references and conditional dependency versions are recorded in the source project. NuGet restore resolves packaged dependencies; do not mix arbitrary DLLs from different release graphs.

## Integration and configuration

Construct `BNBChainOASIS` with the BSC RPC endpoint, the signing account's private key, the deployed Web3CoreOASIS contract address and the expected chain ID. Use chain ID 56 for mainnet or 97 for BSC testnet. The signing account needs gas funds and permission to write to the contract.

Activation checks the RPC chain ID and the presence of deployed contract bytecode. Storage mutations use the node's `eth_gasPrice` quote when signing.

The shared storage suite passed 52 lifecycle checks against official BSC v1.7.8 on an isolated Parlia chain, with separate contracts for sync and async tests. Hosted storage, wallet and NFT acceptance have separate verification scopes. [Native storage evidence](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43084066216)

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

