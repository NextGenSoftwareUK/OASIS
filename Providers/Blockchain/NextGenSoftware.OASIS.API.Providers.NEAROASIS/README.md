# NextGenSoftware.OASIS.API.Providers.NEAROASIS

Connects the OASIS to NEAR Protocol -- a sharded, developer-friendly blockchain with human-readable account names, near-zero fees, and Nightshade sharding for unlimited throughput. Stores OASIS holons, avatars, and tokens. Part of the OASIS HOT-Swappable Provider Architecture. Ecosystem: https://oasisomniverse.one

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.API.Providers.NEAROASIS
```

[NuGet versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.NEAROASIS) · [Source project](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/NextGenSoftware.OASIS.API.Providers.NEAROASIS.csproj)

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

Avatar, avatar-detail and holon contract storage now requires `OASIS_NEAR_STORAGE_ENCRYPTION_KEY`: a separately generated, persistently retained Base64-encoded 32-byte key. Configure it through protected host secrets; do not derive it from the account signing key, commit it, print it, or generate a replacement on every startup. Losing it prevents reading existing encrypted records. Back it up securely alongside the deployment's network and contract identity.

The existing shared authenticated-encryption codec protects storage values before SDK dispatch and binds them to network, contract and record key. Views decrypt locally, including collection queries. Plaintext records, incorrect keys and swapped ciphertext fail closed; there is no plaintext fallback or automatic migration. Generic storage get/list/put/delete requires key configuration before starting the SDK process. Token/NFT contract calls and their intentionally public metadata are separate from this record-storage boundary.

The encryption boundary has focused codec/preflight tests, but a fresh official-SDK sandbox roundtrip with the retained environment is still required. The evidence runner now connects only to an explicitly retained environment; it does not initialize/reset a sandbox, deploy contracts, download packages or restore .NET dependencies. Configure `OASIS_NEAR_REUSABLE_RPC_URL` (loopback HTTP), `OASIS_NEAR_REUSABLE_CHAIN_ID`, `OASIS_NEAR_REUSABLE_CONTRACT_ID` and `OASIS_NEAR_REUSABLE_SIGNER_PATH` (inside `/var/tmp/oasis-near-evidence/`). Deployed finalized code must match the locally compiled contract byte-for-byte. Provision missing native tools once explicitly and retain them; never use the sandbox SDK's destructive default initialization against an existing ledger.

Owned test credentials pass from the Linux SDK host to Windows through stdin, not a plaintext settings file or command line. The Windows test host creates `LocalAppData/OASIS/provider-evidence/near/storage-key.dpapi` only if absent, refuses to overwrite/replace it and restores pre-existing process environment after execution. The retained sandbox2.6.2 runtime and SDK2.0.0 compiler tools are now installed once; contract compilation is verified using `Scripts/run_near_provider_evidence.sh --compile-only`. That explicit mode never starts a node. DPAPI success-path execution, retained-ledger provisioning/deployment and the complete configured live workflow remain unverified. This is not full provider, historical-version or hosted release certification.

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

