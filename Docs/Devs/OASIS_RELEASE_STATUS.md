# OASIS release inventory

Status snapshot: 11 October 2026, America/Cancun. This inventory distinguishes releases completed in the current publication run from older downloadable releases. Older assets do not establish that the requested updates are complete.

Three GitHub releases are completed in this run: MCP Server 3.1.0, ODOOM 1.1.0 and OQUAKE 1.1.0. MCP Server 3.1.0 is also published on NuGet. Remaining publication is outstanding.

| Product or release | Current publication status | Published download |
|---|---|---|
| MCP Server | Done: 3.1.0; five platform binaries and NuGet | [GitHub 3.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/mcp-v3.1.0) · [NuGet 3.1.0](https://www.nuget.org/packages/NextGenSoftware.OASIS.MCP.Server/3.1.0) |
| ODOOM | Done: 1.1.0; Windows x64, Linux x64 and macOS ARM64 assets | [Release 1.1.0](https://github.com/NextGenSoftwareUK/ODOOM/releases/tag/v1.1.0) |
| OQUAKE | Done: 1.1.0; Windows x64, Linux x64 and macOS ARM64 assets | [Release 1.1.0](https://github.com/NextGenSoftwareUK/OQUAKE/releases/tag/v1.1.0) |
| OASIS Runtime | Updated release outstanding | [Existing 5.0.1](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OASIS-Runtime-v5.0.1) |
| STAR Runtime including CLI | Updated release outstanding | [Existing runtime 4.0.1](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/STAR-ODK-Runtime-v4.0.1) |
| Edge Runtime | Outstanding; no published release verified | — |
| OGEngineClient Edge and remote-only profiles | Updated release outstanding | [Existing 2.0.2](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OGEngineClient-v2.0.2) |
| Integrated Native Endpoint | Updated release outstanding | [Existing 2.0.2](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/Native-Endpoint-v2.0.2) |
| OIDE | Requested update or unchanged-source verification outstanding | [Existing 1.0.0](https://github.com/NextGenSoftwareUK/OIDE/releases/tag/v1.0.0) |
| ONODE Manager | Requested update or unchanged-source verification outstanding | [Existing 1.0.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/onode-manager-v1.0.0) |
| HyperDrive Client | Requested update or unchanged-source verification outstanding | [Existing 1.0.0](https://github.com/NextGenSoftwareUK/OASIS-HyperDrive-Client/releases/tag/v1.0.0) |
| OurWorld | Outstanding; Unity Cloud configuration required; no published release found | — |
| All first-party NuGet packages | Bulk updated publication outstanding | MCP 3.1.0 confirmed above |
| MCP npm package | Done: 3.1.0; registry version, CLI entry point and published checksum verified | [npm 3.1.0](https://www.npmjs.com/package/@oasisomniverse/mcp-server/v/3.1.0) |

## Publication requirements

- Complete checked Development-to-master promotion, then verify all 14 staging and production WEB4–WEB10 deployments and affected endpoints before remaining publication.
- Every binary product release must have verified Windows, Linux and macOS assets for its supported architectures. OASIS Runtime and Native Endpoint five-RID workflow changes are on Development, but hosted builds and published archives are not yet verified.
- Runtime archives must contain the updated public `Default/OASIS_DNA.json` template, never configured DNA or credentials.
- ODOOM and OQUAKE live notes now use consistent reviewed feature, fix, packaging, download and changelog sections.
- MCP npm first publication is complete. npm-side trusted-publisher configuration remains required for future automated releases. Do not duplicate existing GitHub, NuGet or npm versions.
- Draft releases, including OASIS Runtime 4.5.0 and OIDE 0.1.0, are not counted as completed.
- Published assets and passing builds are not proof of physical Android/iOS testing, two-device convergence or Linux/macOS Unity IL2CPP execution.

## Verification basis

Links and asset presence were checked using GitHub release APIs; MCP 3.1.0 was checked against NuGet's package registry. This is a dated snapshot, not a claim that remaining releases have since completed.
