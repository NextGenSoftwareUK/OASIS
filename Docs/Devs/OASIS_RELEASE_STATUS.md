# OASIS release inventory

Status snapshot: 12 October 2026, America/Cancun. This inventory distinguishes releases completed in the current publication run from older downloadable releases. Older assets do not establish that the requested updates are complete.

Optional OIDE/HyperDrive publication run 38340851481 stopped at OIDE's existing v1.0.0 tag; HyperDrive was not dispatched. The optional prerelease API-array enumeration bug is corrected in Development; preview 38342066211 passed. Planner logs now print exact versions, classification decisions and blockers for review before another publication attempt. Neither optional update is verified published.

Full original-artifact reconciliation run 38326306085 found 10 content-identical published packages, 79 unpublished candidates and 140 existing-version content collisions (including later updated Edge packages). No pushes occurred. Original bulk artifacts cannot be blindly resumed; collision identities need reviewed current-version/source decisions, while successful uploads must remain untouched. A structured recovery report is now retained by the hosted audit workflow.

Original-artifact recovery preflight run 38325000220 verified the previously uploaded packages against registry content, then rejected the pre-existing AbstractOASIS 2.0.2 content collision. No packages were pushed. Preflight now collects all content collisions before rejecting the whole set, so recovery can reconcile the complete inventory rather than discovering conflicts one at a time. A corrected unpublished-package plan is still required.

Promotion PR465 merged as master `99d82d0c4494a41af97b1b0f56f6367d5f864960`. All fourteen staging/production WEB4–WEB10 deployments were verified SUCCESS/SLEEPING on that exact SHA. HTTPS Swagger returned 200 on all fourteen public domains; WEB6 health and MCP/A2A well-known discovery documents returned 200 in both environments. This verifies deployment availability, not every functional contract or physical-device acceptance.

Unity 1.1.0 live GitHub title/notes explicitly identify **OGEngineClient + Edge + Unity**. Source README updates are pushed; the existing ZIP's embedded README remains pending refresh. Shared generator and four-family global/component workflow integration are on master through PR465, but a coordinated global end-to-end run is not yet verified. Both OGEngineClient profile releases are published by successful run [38286314026](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/38286314026).

Verified GitHub products include MCP Server 3.1.0, ODOOM/OQUAKE 1.1.0, OASIS Runtime 5.1.0, STAR Runtime 4.1.0, Integrated Native Endpoint 2.1.0 and all four split Edge/client families listed below. MCP is also published on NuGet/npm; nine Edge NuGet components and the distinct Unity wrapper are public. Remaining bulk NuGet and optional-product publication is outstanding.

Latest OG profile run 38284824702 passed nine of ten platform/profile builds. RemoteOnly macOS ARM64 failed because concurrent dependency graph instances wrote the same Utilities deps.json; neither profile published. The shared workflow now serializes MSBuild graph execution for both managed and native publication to prevent that output-file race. Hosted verification remains required.

| Product or release | Current publication status | Published download |
|---|---|---|
| MCP Server | Done: 3.1.0; five platform binaries and NuGet | [GitHub 3.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/mcp-v3.1.0) · [NuGet 3.1.0](https://www.nuget.org/packages/NextGenSoftware.OASIS.MCP.Server/3.1.0) |
| ODOOM | Done: 1.1.0; Windows x64, Linux x64 and macOS ARM64 assets | [Release 1.1.0](https://github.com/NextGenSoftwareUK/ODOOM/releases/tag/v1.1.0) |
| OQUAKE | Done: 1.1.0; Windows x64, Linux x64 and macOS ARM64 assets | [Release 1.1.0](https://github.com/NextGenSoftwareUK/OQUAKE/releases/tag/v1.1.0) |
| OASIS Runtime | Done: 5.1.0; Windows x64, Linux x64/ARM64, macOS x64/ARM64 | [Release 5.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OASIS-Runtime-v5.1.0) |
| STAR Runtime including CLI | Done: 4.1.0; Windows x64, Linux x64/ARM64, macOS x64/ARM64 | [Release 4.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/STAR-ODK-Runtime-v4.1.0) |
| Edge Runtime | Done: runtime-only GitHub ZIP 1.1.0; installable Runtime/Editor payload, no test reports/logs/NuGet archives | [Release 1.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OASIS-Edge-Runtime-v1.1.0) |
| Edge standalone compiled distribution | GitHub 1.2.0 verified published: five compiled Windows/Linux/macOS RID archives | [Release 1.2.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OASIS-Edge-Runtime-v1.2.0) |
| OGEngineClient + Edge + Unity | GitHub 1.1.0 verified published; distinct Unity NuGet wrapper 1.0.0 now verified visible in the public flat-container registry. Source READMEs explicitly identify the bundled OGEngineClient; refreshed ZIP README still pending | [Unity release 1.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OASIS-Edge-Unity-v1.1.0) · [Unity NuGet 1.0.0](https://www.nuget.org/packages/NextGenSoftware.OASIS.Edge.Unity/1.0.0) |
| Historical combined OGEngineClient | Superseded by the separate 2.1.0 profile releases below; retain 2.0.2 as historical Edge-enabled managed distribution | [Historical 2.0.2](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OGEngineClient-v2.0.2) |
| OGEngineClient — RemoteOnly | Done: GitHub 2.1.0 public, five nonempty Windows x64/Linux x64/ARM64/macOS x64/ARM64 managed/native archives; run 38286314026 succeeded | [RemoteOnly 2.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OGEngineClient-RemoteOnly-v2.1.0) |
| OGEngineClient + Edge | Done: GitHub 2.1.0 public, five nonempty Windows x64/Linux x64/ARM64/macOS x64/ARM64 managed/native archives; all ten profile builds and publication succeeded | [Client + Edge 2.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OGEngineClient-Edge-v2.1.0) |
| Integrated Native Endpoint | Done: 2.1.0; Windows x64, Linux x64/ARM64, macOS x64/ARM64 | [Release 2.1.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/Native-Endpoint-v2.1.0) |
| OIDE | Requested update or unchanged-source verification outstanding | [Existing 1.0.0](https://github.com/NextGenSoftwareUK/OIDE/releases/tag/v1.0.0) |
| ONODE Manager | Requested update or unchanged-source verification outstanding | [Existing 1.0.0](https://github.com/NextGenSoftwareUK/OASIS/releases/tag/onode-manager-v1.0.0) |
| HyperDrive Client | Requested update or unchanged-source verification outstanding | [Existing 1.0.0](https://github.com/NextGenSoftwareUK/OASIS-HyperDrive-Client/releases/tag/v1.0.0) |
| OurWorld | Outstanding; Unity Cloud configuration required; no published release found | — |
| All first-party NuGet packages | Partially published: upload stopped on AbstractOASIS 2.0.2 HTTP 409 (version already exists); full registry reconciliation required before resuming | MCP 3.1.0 confirmed above |
| MCP npm package | Done: 3.1.0; registry version, CLI entry point and published checksum verified | [npm 3.1.0](https://www.npmjs.com/package/@oasisomniverse/mcp-server/v/3.1.0) |

## Publication requirements

See [the four cross-referenced Edge/game-client distributions](EDGE_DISTRIBUTIONS.md). Unity integration includes OGEngineClient + Edge; it is separate from standalone DLL archives and both explicit OGEngineClient profiles. Historical OGEngineClient 2.0.2 was Edge-enabled, not RemoteOnly.

- Promoted master `7f622f9c2c28d5b4fb18b01ceef6fa57794f2cab`: all 14 staging/production WEB4–WEB10 deployments verified successful/sleeping; all 14 Swagger endpoints plus WEB6 health/discovery in both environments returned HTTP 200 before dispatch.
- Every binary product release must have verified Windows, Linux and macOS assets for its supported architectures. OASIS Runtime, STAR Runtime and Native Endpoint each have five successful hosted platform builds and five nonempty public release archives. Other products' platform audits remain outstanding.
- Runtime archives must contain the updated public `Default/OASIS_DNA.json` template, never configured DNA or credentials.
- ODOOM and OQUAKE live notes now use consistent reviewed feature, fix, packaging, download and changelog sections.
- MCP npm first publication is complete. npm-side trusted-publisher configuration remains required for future automated releases. Do not duplicate existing GitHub, NuGet or npm versions.
- Draft releases, including OASIS Runtime 4.5.0 and OIDE 0.1.0, are not counted as completed.
- Published assets and passing builds are not proof of physical Android/iOS testing, two-device convergence or Linux/macOS Unity IL2CPP execution.

## Verification basis

### Edge NuGet uploads: 12 October 2026

[Run 38270870776](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/38270870776) successfully packed and uploaded all nine packages below. Edge Runtime and Edge ONET Runtime 1.0.2 were subsequently verified indexed with their expanded README headings rendered on NuGet. Other exact-version pages require individual rendering verification.

[Runtime-only replacement run 38280716485](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/38280716485) succeeded. The sole uploaded asset `OASIS.Edge.Runtime.v1.1.0.zip` is now 67,360,584 bytes, replacing the 256,248,093-byte evidence bundle. Its allowlisted installation payload is Runtime, Editor, package.json, README and license files where present; hosted checks require the Edge Runtime assembly and reject test reports, logs, NuGet archives and evidence filenames. Packaging reported 50 runtime assemblies. Full evidence remains in the original validation/workflow artifacts, not this public ZIP. The tag still targets validated source `54bb599ed3b9b00d3be6711e1ff150b7edb66bb9`. This packaging check is not proof of physical-device or all-platform execution.

| Package | Uploaded version |
|---|---|
| Native Integrated Endpoint Edge | [1.0.3](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge/1.0.3) |
| EdgeSQLite provider | [1.0.2](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS/1.0.2) |
| HoloOASIS Edge provider | [1.0.0](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge/1.0.0) |
| HoloOASIS Unity provider | [2.0.4](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity/2.0.4) |
| Contracts | [1.0.2](https://www.nuget.org/packages/NextGenSoftware.OASIS.Contracts/1.0.2) |
| Edge ONET Runtime | [1.0.2](https://www.nuget.org/packages/NextGenSoftware.OASIS.Edge.ONET.Runtime/1.0.2) |
| Edge Runtime | [1.0.2](https://www.nuget.org/packages/NextGenSoftware.OASIS.Edge.Runtime/1.0.2) |
| HyperDrive Synchronization | [1.0.2](https://www.nuget.org/packages/NextGenSoftware.OASIS.HyperDrive.Synchronization/1.0.2) |
| ONET | [1.0.2](https://www.nuget.org/packages/NextGenSoftware.OASIS.ONET/1.0.2) |

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
