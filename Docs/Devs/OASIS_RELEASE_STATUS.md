# OASIS release inventory

Status snapshot: 11 October 2026, America/Cancun. This inventory distinguishes releases completed in the current publication run from older downloadable releases. Older assets do not establish that the requested updates are complete.

Six GitHub releases are verified published in this run: MCP Server 3.1.0, ODOOM 1.1.0, OQUAKE 1.1.0, OASIS Runtime 5.1.0, STAR Runtime 4.1.0 and Integrated Native Endpoint 2.1.0. MCP Server 3.1.0 is also published on NuGet and npm. Remaining publication is outstanding.

| Product or release | Current publication status | Published download |
|---|---|---|
| MCP Server | Done: 3.1.0; five platform binaries and NuGet | [GitHub 3.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/mcp-v3.1.0) · [NuGet 3.1.0](https://www.nuget.org/packages/NextGenSoftware.OASIS.MCP.Server/3.1.0) |
| ODOOM | Done: 1.1.0; Windows x64, Linux x64 and macOS ARM64 assets | [Release 1.1.0](https://github.com/NextGenSoftwareUK/ODOOM/releases/tag/v1.1.0) |
| OQUAKE | Done: 1.1.0; Windows x64, Linux x64 and macOS ARM64 assets | [Release 1.1.0](https://github.com/NextGenSoftwareUK/OQUAKE/releases/tag/v1.1.0) |
| OASIS Runtime | Done: 5.1.0; Windows x64, Linux x64/ARM64, macOS x64/ARM64 | [Release 5.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OASIS-Runtime-v5.1.0) |
| STAR Runtime including CLI | Done: 4.1.0; Windows x64, Linux x64/ARM64, macOS x64/ARM64 | [Release 4.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/STAR-ODK-Runtime-v4.1.0) |
| Edge Runtime | 1.1.0 planned; validation and archive packaging passed in run 38204827438; publication blocked by NuGet failure | — |
| OGEngineClient Edge and remote-only profiles | Updated release outstanding | [Existing 2.0.2](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OGEngineClient-v2.0.2) |
| Integrated Native Endpoint | Done: 2.1.0; Windows x64, Linux x64/ARM64, macOS x64/ARM64 | [Release 2.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/Native-Endpoint-v2.1.0) |
| OIDE | Requested update or unchanged-source verification outstanding | [Existing 1.0.0](https://github.com/NextGenSoftwareUK/OIDE/releases/tag/v1.0.0) |
| ONODE Manager | Requested update or unchanged-source verification outstanding | [Existing 1.0.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/onode-manager-v1.0.0) |
| HyperDrive Client | Requested update or unchanged-source verification outstanding | [Existing 1.0.0](https://github.com/NextGenSoftwareUK/OASIS-HyperDrive-Client/releases/tag/v1.0.0) |
| OurWorld | Outstanding; Unity Cloud configuration required; no published release found | — |
| All first-party NuGet packages | Partially published: upload stopped on AbstractOASIS 2.0.2 HTTP 409 (version already exists); full registry reconciliation required before resuming | MCP 3.1.0 confirmed above |
| MCP npm package | Done: 3.1.0; registry version, CLI entry point and published checksum verified | [npm 3.1.0](https://www.npmjs.com/package/@oasisomniverse/mcp-server/v/3.1.0) |

## Publication requirements

- Promoted master `7f622f9c2c28d5b4fb18b01ceef6fa57794f2cab`: all 14 staging/production WEB4–WEB10 deployments verified successful/sleeping; all 14 Swagger endpoints plus WEB6 health/discovery in both environments returned HTTP 200 before dispatch.
- Every binary product release must have verified Windows, Linux and macOS assets for its supported architectures. OASIS Runtime, STAR Runtime and Native Endpoint each have five successful hosted platform builds and five nonempty public release archives. Other products' platform audits remain outstanding.
- Runtime archives must contain the updated public `Default/OASIS_DNA.json` template, never configured DNA or credentials.
- ODOOM and OQUAKE live notes now use consistent reviewed feature, fix, packaging, download and changelog sections.
- MCP npm first publication is complete. npm-side trusted-publisher configuration remains required for future automated releases. Do not duplicate existing GitHub, NuGet or npm versions.
- Draft releases, including OASIS Runtime 4.5.0 and OIDE 0.1.0, are not counted as completed.
- Published assets and passing builds are not proof of physical Android/iOS testing, two-device convergence or Linux/macOS Unity IL2CPP execution.

## Verification basis

### Partial NuGet publication: verified 12 October 2026

Run 38204827438 successfully uploaded these ten packages before the AbstractOASIS conflict. Every exact version below is present in NuGet's registry. AbstractOASIS 2.0.2 was pre-existing and is not counted as a successful upload from this run. Remaining packages are not yet complete.

| Package | Verified version |
|---|---|
| HoloNET ORM | [4.1.4](https://www.nuget.org/packages/NextGenSoftware.Holochain.HoloNET.ORM/4.1.4) |
| Apollo Server | [2.0.4](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Apollo.Server/2.0.4) |
| API Contracts | [2.0.4](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Contracts/2.0.4) |
| API Core | [2.0.4](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Core/2.0.4) |
| Core ARC Membrane | [2.0.4](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Core.ARC.Membrane/2.0.4) |
| DNA | [2.0.3](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.DNA/2.0.3) |
| Managers | [2.0.4](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Managers/2.0.4) |
| Native Integrated Endpoint | [2.0.4](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint/2.0.4) |
| Native Integrated Endpoint Edge | [1.0.2](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge/1.0.2) |
| ONODE Core | [2.0.4](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.ONODE.Core/2.0.4) |

Links and asset presence were checked using GitHub release APIs; MCP 3.1.0 was checked against NuGet and npm registries. npm's CLI entry point and package checksum match the successful publication.

[Global release run 38189900873](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/38189900873) built and published the three runtime releases from the verified master revision. Last check: 11 October 2026, 20:39 America/Cancun (12 October 01:39 UTC): NuGet packaging passed; Edge hApp/Android builds were still running without reported failures. Public release assets establish publication, not physical-device execution. Update this inventory after each verified publication milestone; do not mark pending builds or registry uploads as done.
