# NextGenSoftware.OASIS.API.Providers.AWSOASIS

Connects OASIS to DynamoDB through the official AWSSDK.DynamoDBv2 client. AWSOASIS reuses DynamoDBOASIS storage operations with AWS-specific provider identity and configuration; this adapter does not implement S3 object storage. Part of the OASIS HOT-Swappable Provider Architecture. Ecosystem: https://oasisomniverse.one

## Current verification scope

The retained DynamoDB Local runner is `Scripts/run_aws_provider_evidence.ps1`; it reuses the installed runtime, shared dependency cache and fixed output directories. Local native tests do not certify hosted AWS credentials, costs, scale or all provider interfaces/options. The runner uses a test-owned in-memory database; it is not a durable production recovery journal.

Save operations require an authoritative SDK HTTP 200 acknowledgement before publishing caller IDs/provider keys. Missing/invalid responses retain mutation-reconciliation evidence; do not blindly retry them. Avatar-detail records persist the provider-specific storage key and deletion projection, like profile/holon records. Historical versions are still unsupported; additional interface, concurrency, recovery and hosted-release gates remain open. Current evidence and limitations are recorded in `Docs/Devs/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md`.

## Installation

```sh
dotnet add package NextGenSoftware.OASIS.API.Providers.AWSOASIS
```

[NuGet versions](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.AWSOASIS) · [Source project](https://github.com/NextGenSoftwareUK/OASIS/blob/Development/Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSOASIS/NextGenSoftware.OASIS.API.Providers.AWSOASIS.csproj)

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

