# NextGenSoftware.OASIS.API.Providers.CloudflareOASIS

Connects the OASIS to Cloudflare Workers KV via the Cloudflare REST API. Stores OASIS records through shared encrypted record/history/query policy in a KV namespace. No Cloudflare SDK required — uses System.Net.Http. Part of the OASIS HOT-Swappable Provider Architecture. Ecosystem: https://oasisomniverse.one

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.API.Providers.CloudflareOASIS
```

[NuGet versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.CloudflareOASIS) · [Source project](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Providers/Cloud/NextGenSoftware.OASIS.API.Providers.CloudflareOASIS/NextGenSoftware.OASIS.API.Providers.CloudflareOASIS.csproj)

Choose a published version compatible with the other OASIS packages in your application. This guide describes current source; older packages may not contain the same functionality.

## Frameworks and dependencies

Declared target frameworks: `net10.0`.

Direct external package references:

- `NextGenSoftware.OASIS.API.Core`

Project references and conditional dependency versions are recorded in the source project. NuGet restore resolves packaged dependencies; do not mix arbitrary DLLs from different release graphs.

## Integration and configuration

Use this package for the responsibility described above, alongside its owning OASIS runtime/provider integration. Inspect the source project's public APIs and owning component documentation for construction and configuration; this package installation command does not start an API server, authenticate an avatar or provision an external service.

Provider adapters require their respective service configuration and credentials. Runtime libraries require the host application's lifecycle and persistent paths where applicable. Supply only the settings actually required by the owning component; do not copy production DNA into a client or public release.

## Storage configuration and guarantees

The existing constructor takes `accountId`, `namespaceId`, and a scoped KV read/write `apiToken`. Supply `OASIS_CLOUDFLARE_STORAGE_KEY` as canonical Base64 for a separately persisted32-byte key, or use the explicit key-resolver/HttpClient constructor. Missing keys fail before transport; keys are never generated or migrated by the provider. Injected clients remain caller-owned by default and must disable redirects and automatic mutation retries.

Legacy plaintext avatar/detail/holon/index prefixes are refused before private-layout access. Stop legacy writers and review migration separately. Default holon deletion is soft; history, metadata queries and record codecs use the shared storage implementation.

Workers KV is eventually consistent, has a same-key write rate limit, and supplies no atomic compare-and-swap or multi-key transaction. Reads can be stale, including negative lookups. Do not use this adapter as proof of globally current identity/credential authority, concurrent uniqueness or snapshot queries. The adapter preserves these native limitations; no local cache or retry loop pretends to provide immediate visibility.

The REST transport validates explicit acknowledgements, follows continuation cursors through empty pages, and throws on failed pages instead of returning partial success. Unknown mutation outcomes require reconciliation before replay. Controlled conformance is not native hosted acceptance; the opt-in native binding deliberately preserves actual visibility and rate-limit failures.

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

