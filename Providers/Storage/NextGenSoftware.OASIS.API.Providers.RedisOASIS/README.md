# NextGenSoftware.OASIS.API.Providers.RedisOASIS

Redis OASIS Provider — high-performance in-memory key-value store for OASIS holons and avatars via StackExchange.Redis. https://oasisomniverse.one

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.API.Providers.RedisOASIS
```

[NuGet versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.RedisOASIS) · [Source project](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Providers/Storage/NextGenSoftware.OASIS.API.Providers.RedisOASIS/NextGenSoftware.OASIS.API.Providers.RedisOASIS.csproj)

Choose a published version compatible with the other OASIS packages in your application. This guide describes current source; older packages may not contain the same functionality.

## Frameworks and dependencies

Declared target frameworks: `net10.0`.

Direct external package references:

- `NextGenSoftware.OASIS.API.Core`
- `StackExchange.Redis`

Project references and conditional dependency versions are recorded in the source project. NuGet restore resolves packaged dependencies; do not mix arbitrary DLLs from different release graphs.

## Integration and configuration

Use this package for the responsibility described above, alongside its owning OASIS runtime/provider integration. Inspect the source project's public APIs and owning component documentation for construction and configuration; this package installation command does not start an API server, authenticate an avatar or provision an external service.

Provider adapters require their respective service configuration and credentials. Runtime libraries require the host application's lifecycle and persistent paths where applicable. Supply only the settings actually required by the owning component; do not copy production DNA into a client or public release.

## Errors and security

Current source uses the shared canonical storage/history/query implementation with mandatory encrypted records and opaque keyed indexes. Configure `OASIS_REDIS_STORAGE_ENCRYPTION_KEY` as canonical Base64 for a separately persisted 32-byte key, or supply the constructor's key resolver. Keep that key with the Redis data and backups; losing it makes private records unreadable. Production constructors never generate or replace it. Connections use the supplied StackExchange.Redis configuration; use appropriate authentication/TLS for your service.

Existing plaintext `avatar:*`, `avatar-detail:*`, `holon:*` and legacy identity/set indexes cause explicit activation refusal; no automatic conversion or alternate reader is provided. Review any existing data before upgrading. Provider keys remain GUIDs, not usernames. History is opt-in through `IsVersionControlEnabled`; absent historical records are errors, never substitutes for current data. Redis's existing default Holon deletion remains soft; `DeleteHolonSoft(..., false)` explicitly hard-deletes through the shared lifecycle owner.

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

