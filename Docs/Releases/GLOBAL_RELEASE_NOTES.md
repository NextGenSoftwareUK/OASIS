# OASIS platform release

This coordinated release publishes the current tested OASIS platform from one exact Git commit. It includes the OASIS and STAR runtimes, OGEngineClient, the native endpoint, the OASIS MCP Server, and the complete first-party NuGet package set.

## Platform coverage

- **WEB4 OASIS API** provides avatars and SSO, karma and reputation, holons and COSMIC ORM data, wallets and keys, NFTs and GeoNFTs, inventory, maps, search, provider management, HyperDrive, ONET/ONODE integration and offline session grants.
- **WEB5 STAR API/ODK** provides OAPP and STARNET creation, missions, quests and ordered/any-order objectives, celestial bodies, games, GeoHotSpots, libraries, templates, plugins and cross-game progress.
- **WEB6 AI API** provides multi-provider AI routing, FAHRN orchestration, Holonic BRAID memory, SkillOpt, embeddings, streaming, MCP, A2A, DID/VC integration and observability.
- **WEB7-WEB10 contracts and packages** cover the Symbiotic, Galactic Mesh, Singularity and Source layers. Their API versions are not advanced by the WEB4-WEB6 release option.
- **OASIS MCP Server** exposes the complete 516-command typed tool catalog across WEB4-WEB10 over MCP stdio/HTTP-compatible clients. Its release includes native binaries, NuGet and npm distributions, plus the generated command catalog.
- **HyperDrive v2 + ONET + offline sync** share one synchronization pipeline for online/offline sessions, provider failover, replication, inventory, GeoNFT collection and quest progress.
- **OGEngineClient** is the renamed and expanded STAR API Client. It supplies managed and native game bindings used by Our World, ODOOM and OQUAKE for SSO, shared inventory, GeoNFTs, quests and real-time synchronization.

## Release artifacts

| Artifact | Purpose |
|---|---|
| `OASIS.Runtime.vX.Y.Z.zip` | Integrated OASIS runtime assemblies and dependencies for native OAPP embedding |
| `STAR.Runtime.vX.Y.Z.zip` | Self-contained STAR CLI/ODK runtime and DNA templates |
| `OGEngineClient.vX.Y.Z.zip` | OGEngineClient managed/native integration output |
| `OASIS.API.Integrated.Native.EndPoint.vX.Y.Z.zip` | In-process WEB4/WEB5 endpoint for applications that do not use HTTP |
| `oasis-mcp-*` | Self-contained MCP executables for Windows, Linux and macOS on x64/ARM64 |
| NuGet packages | All first-party OASIS contracts, runtimes, APIs, providers and developer libraries |
| `@oasisomniverse/mcp-server` | npm launcher/package for the OASIS MCP Server |

Optional application releases are versioned in the same plan: Our World, ODOOM, OQUAKE, OIDE, ONODE Manager and OASIS HyperDrive Client. These products default to excluded so the regular platform release does not unexpectedly launch large game, Unity, IDE or desktop-client builds.

## Versioning and provenance

The global release planner compares source versions with NuGet.org and existing GitHub release tags. Existing packages advance by one patch version; new packages start at the source-declared version. The generated `release-plan.json` records every package/version pair and the exact source commit.

WEB4, WEB5 and WEB6 API versions and their Swagger-linked release histories are advanced together when the explicit API option is selected. WEB7-WEB10 are excluded from that option.

## Validation

The release workflow performs these checks before publishing:

- complete NuGet metadata for every discovered first-party package;
- exact private dependency and submodule pins;
- package restore and Release builds;
- OASIS Runtime and Native Endpoint publish;
- self-contained STAR Runtime/CLI publish;
- OGEngineClient publish with Edge/ONET/HyperDrive dependencies;
- MCP native builds and protocol smoke tests on all supported operating systems;
- npm contents and generated 516-command catalog consistency;
- immutable GitHub tags and release assets created from the tested commit.

See [global release automation](https://github.com/NextGenSoftwareUK/OASIS/blob/master/Docs/Devs/GLOBAL_RELEASE_AUTOMATION.md), [CI/CD workflows and releases](https://github.com/NextGenSoftwareUK/OASIS/blob/master/Docs/Devs/CI_CD_WORKFLOWS_AND_RELEASES.md), [MCP server documentation](https://github.com/NextGenSoftwareUK/OASIS/blob/master/WEB6/NextGenSoftware.OASIS.MCP.Server/README.md), and the [complete MCP tool catalog](https://github.com/NextGenSoftwareUK/OASIS/blob/master/WEB6/NextGenSoftware.OASIS.MCP.Server/MCP_TOOL_CATALOG.md) for detailed commands and operational guidance.
