# NextGenSoftware.OASIS.API.Providers.UpstashOASIS

Upstash OASIS Provider â€” serverless Redis-compatible key-value store for OASIS holons and avatars via Upstash REST API. No SDK required â€” uses System.Net.Http. https://oasisomniverse.one

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.API.Providers.UpstashOASIS
```

[NuGet versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.UpstashOASIS) Â· [Source project](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Providers/Cloud/NextGenSoftware.OASIS.API.Providers.UpstashOASIS/NextGenSoftware.OASIS.API.Providers.UpstashOASIS.csproj)

Choose a published version compatible with the other OASIS packages in your application. This guide describes current source; older packages may not contain the same functionality.

## Frameworks and dependencies

Declared target frameworks: `net10.0`.

Direct external package references:

- `NextGenSoftware.OASIS.API.Core`

Project references and conditional dependency versions are recorded in the source project. NuGet restore resolves packaged dependencies; do not mix arbitrary DLLs from different release graphs.

## Integration and configuration

Use this package for the responsibility described above, alongside its owning OASIS runtime/provider integration. Inspect the source project's public APIs and owning component documentation for construction and configuration; this package installation command does not start an API server, authenticate an avatar or provision an external service.

Provider adapters require their respective service configuration and credentials. Runtime libraries require the host application's lifecycle and persistent paths where applicable. Supply only the settings actually required by the owning component; do not copy production DNA into a client or public release.

## Storage configuration and consistency

The source adapter uses the real Upstash REST API and shared encrypted OASIS record/history/query policy. Construct `UpstashOASIS(restUrl, token)` with a root HTTPS origin. Configure `OASIS_UPSTASH_STORAGE_KEY` as canonical Base64 for a separately persisted32-byte storage key. Missing keys fail before data access; the adapter never generates or migrates keys. The injected `HttpClient`/key-resolver constructor preserves caller ownership by default. Caller-supplied HTTP handlers must disable redirects and automatic mutation retries.

The existing plaintext `avatar:`, `detail:` and `holon:` layouts are refused before new-layout access; stop old-layout writers and review migration separately. Default holon deletion is soft; history and queries use the shared storage policy. Native per-key comparisons are content-state comparisons, not monotonic versions or multi-record transactions.

Requests within one provider instance are serialized and forward the native `upstash-sync-token` for read-your-writes. Independent clients do not automatically share causal state; do not infer cross-client linearizability or snapshots. Unknown write acknowledgements remain reconciliation errors and are never automatically replayed. Credentials and remote error bodies are not included in diagnostics.

The opt-in canonical verification runner is `Scripts/Providers/Run-UpstashStorageConformance.ps1` in the source repository. It reuses its protected dedicated test resource and refuses expired resources or existing run reports. Native core-storage checks are a baseline, not certification of every optional interface or HyperDrive route.

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

[Verified release matrix](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/WIP/Release and Status/OASIS_RELEASE_STATUS.md) Â· [Documentation audit](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Docs/WIP/Release and Status/RELEASE_DOCUMENTATION_AUDIT.md)

Published NuGet versions are immutable. Updated documentation ships with a new unpublished package version after checked promotion and publication. This metadata-backed guide does not replace product-specific API examples or claim unverified platform tests.

