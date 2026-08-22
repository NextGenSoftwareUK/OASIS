# OASIS Omniverse — IP & Repository Structure Report

**NextGen Software Ltd** | Prepared for: Team & Investors | August 2026

---

## Overview

The OASIS Omniverse platform is structured across two tiers of GitHub repositories under the **NextGenSoftwareUK** organisation. The public tier demonstrates capability and drives community adoption; the private tier protects the core IP that constitutes our competitive moat.

| | Count |
|---|---|
| Public GitHub repositories | 15 |
| Private GitHub repositories | 11 |
| Public NPM packages | 14 |
| Private as % of GitHub total | 42% |

---

## Public Repositories — Open Ecosystem

These repositories are intentionally public. They drive developer adoption, NuGet and NPM downloads, community contributions, and ecosystem trust. Consumers of the platform build on these without needing access to the proprietary core.

### Foundation & .NET Libraries

| Repository | What it contains | Role |
|---|---|---|
| **[NextGenSoftwareUK/OASIS](https://github.com/NextGenSoftwareUK/OASIS)** | Main monorepo — WEB4 OASIS API, 40+ storage/blockchain/social/AI providers, WebAPI, Swagger, Docker, Railway deploy configs | Public face of the platform; community entry point |
| **[NextGenSoftwareUK/holochain-client-csharp](https://github.com/NextGenSoftwareUK/holochain-client-csharp)** | HoloNET Client — the only production-grade .NET/Unity Holochain client. Full WebSocket zome call layer with async/event hybrid model | Community flagship; drives NuGet adoption |
| **[NextGenSoftwareUK/HoloNET-Manager](https://github.com/NextGenSoftwareUK/HoloNET-Manager)** | Desktop WPF reference implementation showcasing every HoloNET Client and HoloNET ORM capability with reusable UI components developers can reference and adapt | Developer showcase; reusable component library |
| **[NextGenSoftwareUK/NextGenSoftware-Libraries](https://github.com/NextGenSoftwareUK/NextGenSoftware-Libraries)** | Shared utility libraries — WebSocket extensions (including the ConnectAsync overload required by HoloNET), logging, core helpers | Foundational dependency for all OASIS projects |

### JavaScript / TypeScript API Packages

One public GitHub repo and NPM package per API tier — giving JS/TS developers a typed client for every layer of the OASIS without exposing the server-side implementation.

| Repository | NPM Package | What it contains |
|---|---|---|
| **[OASIS-API-Javascipt-Package-WEB4](https://github.com/NextGenSoftwareUK/OASIS-API-Javascipt-Package-WEB4)** | `@oasisomniverse/web4-api` | Typed JS client for the WEB4 OASIS API — avatars, holons, providers, inventory |
| **[OASIS-API-Javascript-Package-WEB5](https://github.com/NextGenSoftwareUK/OASIS-API-Javascript-Package-WEB5)** | `@oasisomniverse/web5-api` | Typed JS client for the WEB5 STAR API — OAPPs, metaverse, STARDNA |
| **[OASIS-API-Javascript-Package-WEB6](https://github.com/NextGenSoftwareUK/OASIS-API-Javascript-Package-WEB6)** | `@oasisomniverse/web6-api` | Typed JS client for the WEB6 AI API — FAHRN agents, Holonic BRAID, MCP tools |
| **[OASIS-API-Javascript-Package-WEB7](https://github.com/NextGenSoftwareUK/OASIS-API-Javascript-Package-WEB7)** | `@oasisomniverse/web7-api` | Typed JS client for the WEB7 Symbiotic Layer |
| **[OASIS-API-Javascript-Package-WEB8](https://github.com/NextGenSoftwareUK/OASIS-API-Javascript-Package-WEB8)** | `@oasisomniverse/web8-api` | Typed JS client for the WEB8 Inter-Galactic Layer |
| **[OASIS-API-Javascript-Package-WEB9](https://github.com/NextGenSoftwareUK/OASIS-API-Javascript-Package-WEB9)** | `@oasisomniverse/web9-api` | Typed JS client for the WEB9 Singularity Layer |
| **[OASIS-API-Javascript-Package-WEB10](https://github.com/NextGenSoftwareUK/OASIS-API-Javascript-Package-WEB10)** | `@oasisomniverse/web10-api` | Typed JS client for the WEB10 The Source |

### WebUI Framework SDKs (NPM only)

Drop-in SDKs for the most popular JS frameworks — connect to the full OASIS platform in minutes from any frontend stack.

| NPM Package | Framework |
|---|---|
| `@oasisomniverse/js` | Vanilla JS / TypeScript |
| `@oasisomniverse/react` | React |
| `@oasisomniverse/angular` | Angular |
| `@oasisomniverse/vue` | Vue |
| `@oasisomniverse/svelte` | Svelte |
| `@oasisomniverse/nextjs` | Next.js |
| `@oasisomniverse/mcp-server` | MCP server — expose the full OASIS as an AI tool |

### Games & Tooling

| Repository | What it contains | Role |
|---|---|---|
| **[NextGenSoftwareUK/ODOOM](https://github.com/NextGenSoftwareUK/ODOOM)** | OGEngine-powered DOOM reimagining — full OASIS SSO, shared inventory and cross-game quests via STAR | Flagship open game; demonstrates OASIS cross-game capabilities |
| **[NextGenSoftwareUK/OQUAKE](https://github.com/NextGenSoftwareUK/OQUAKE)** | OGEngine-powered Quake reimagining — shares avatars, weapons and inventory with ODOOM through STAR | Second flagship open game; proves cross-game SSO at scale |
| **[NextGenSoftwareUK/ODOOM-Editor](https://github.com/NextGenSoftwareUK/ODOOM-Editor)** | Level editor for ODOOM built on UltimateDoomBuilder / OGEditor SDK | Open editing toolchain for the ODOOM community |
| **[NextGenSoftwareUK/UltimateDoomBuilder](https://github.com/NextGenSoftwareUK/UltimateDoomBuilder)** | OGEditor SDK — a full-featured level and world editor toolchain for OGEngine-based games | Open toolchain to drive game developer adoption |

---

## Private Repositories — Competitive Moat

These repositories contain proprietary IP and are never exposed publicly. They are consumed by the public tier either as private NuGet packages, private git submodules, or compiled binaries distributed via OPORTAL.

| Repository | What it contains | Why private |
|---|---|---|
| **[NextGenSoftwareUK/OASIS-API-Core](https://github.com/NextGenSoftwareUK/OASIS-API-Core)** | Core OASIS API interfaces, base classes and provider contracts. Contains the **COSMIC ORM** — a universal data abstraction layer across 40+ Web2 and Web3 providers that lets developers migrate from traditional databases to decentralised storage with minimal code changes. Existing models can be reused as-is; swapping from Entity Framework or NHibernate is a matter of a few lines. The same codebase then spans SQL, NoSQL, IPFS, Holochain, Solana, Ethereum and 36+ more — without rewriting business logic. Also contains the full HyperDrive backend (auto-failover, auto-replication, auto-load-balancing across every provider) and the STARNET holon graph — all deeply integrated as the unified WEB4 engine | The single most critical piece of IP; the entire provider ecosystem, zero-downtime system, and the most frictionless Web2→Web3 migration path in existence |
| **[NextGenSoftwareUK/OASIS-ONODE-Core](https://github.com/NextGenSoftwareUK/OASIS-ONODE-Core)** | Core backend for the WEB5 STAR API and STARNET application layer, plus complementary WEB4 components that sit above the API.Core foundation | WEB5 platform IP; powers the metaverse and OAPP ecosystem |
| **[NextGenSoftwareUK/STAR-ODK](https://github.com/NextGenSoftwareUK/STAR-ODK)** | WEB5 STAR API, ODK runtime, STAR CLI, COSMIC simulation engine, STARDNA — the complete low-code/no-code metaverse and OAPP development platform | Flagship proprietary platform; core commercial differentiator |
| **[NextGenSoftwareUK/OASIS-WEB6](https://github.com/NextGenSoftwareUK/OASIS-WEB6)** | WEB6 AI layer — FAHRN multi-agent orchestration (5 dispatch modes, ML.NET classifier), Holonic BRAID shared reasoning graph, SkillOpt self-evolving skills, 250 MCP tools, DID/VC identity, 20+ AI providers, gRPC/GraphQL APIs | Next-generation AI platform IP; primary revenue growth engine |
| **[NextGenSoftwareUK/ONODEManager](https://github.com/NextGenSoftwareUK/ONODEManager)** | ONODE Manager (Avalonia cross-platform desktop app), ONODE Service (background REST supervisor), ONODE Client (typed .NET client library) | Operational tooling IP; governs node deployment and management |
| **[NextGenSoftwareUK/OGEngineClient](https://github.com/NextGenSoftwareUK/OGEngineClient)** | Native AOT .NET HTTP client for both the WEB4 OASIS API and WEB5 STAR API, plus P/Invoke interop bridge that exposes OASIS/STAR functionality directly into OGEngine native games (OQuake, ODOOM and others) | Game engine integration layer; bridges the entire OASIS platform into proprietary native game runtimes |
| **[NextGenSoftwareUK/HyperDrive](https://github.com/NextGenSoftwareUK/HyperDrive)** | HyperDrive desktop client — like Google Drive, Dropbox or OneDrive but spanning the entire Web2 + Web3 ecosystem. Manages not just files and data but also NFTs, GeoNFTs and digital assets across every provider the OASIS supports | Disruptive consumer product IP; the first unified Web2+Web3 personal storage client |
| **[NextGenSoftwareUK/HoloNET-ORM](https://github.com/NextGenSoftwareUK/HoloNET-ORM)** | **The easiest on-ramp from Web2 to Holochain that exists.** Extend `HoloNETEntryBaseClass` and get Load, Save, and Delete mapped directly onto Rust zome structs — no manual zome call wiring, no serialisation boilerplate. Developers already using an ORM like Entity Framework or NHibernate can reuse their existing model classes almost unchanged and swap in HoloNET ORM in a handful of lines. The migration path to fully decentralised, agent-centric storage becomes a refactor, not a rewrite. Kept private because this is exactly the kind of leverage that drives mass adoption — and that competitors cannot easily replicate | The lowest-friction Web2→Holochain migration path in existence; core developer acquisition moat |
| **[NextGenSoftwareUK/HoloNET-HyperNET](https://github.com/NextGenSoftwareUK/HoloNET-HyperNET)** | Peer-to-peer lag-free multiplayer networking for Unity and Unreal games, built on HoloNET. Near-unlimited concurrent players with no cloud game server required | Disruptive multiplayer tech IP; eliminates traditional game server costs entirely |
| **[NextGenSoftwareUK/OIDE](https://github.com/NextGenSoftwareUK/OIDE)** | OASIS Integrated Development Environment — full IDE for building OAPPs and WEB5/WEB6 experiences | Proprietary developer toolchain; creates deep ecosystem stickiness |
| **[NextGenSoftwareUK/Our-World](https://github.com/NextGenSoftwareUK/Our-World)** | Our World — the flagship OASIS location-based AR/XR game and metaverse that demonstrates the full OASIS platform stack and serves as its primary showcase | Flagship product IP; the original proof-of-concept that became the OASIS platform |

---

## Architecture: How the Tiers Interact

```
PUBLIC CONSUMERS
     │
     ▼
┌─────────────────────────────────────────────────────────────┐
│  PUBLIC TIER                                                 │
│  OASIS monorepo · holochain-client-csharp (HoloNET Client)  │
│  HoloNET-Manager · NextGenSoftware-Libraries · UltimateDoomBuilder │
└─────────────────────┬───────────────────────────────────────┘
                      │  private submodules · NuGet packages · compiled binaries
                      ▼
┌─────────────────────────────────────────────────────────────┐
│  PRIVATE CORE — source never exposed                         │
│  OASIS-API-Core · OASIS-ONODE-Core · STAR-ODK · OASIS-WEB6  │
│  ONODEManager · OGEngineClient · HyperDrive · HoloNET-ORM   │
│  HoloNET-HyperNET · OIDE · Our-World                        │
└─────────────────────────────────────────────────────────────┘
```

Private repos are injected into the Railway Docker build pipeline via a scoped GitHub PAT at build time only. No private source is ever present in any public build artifact or NuGet package.

---

## Distribution Strategy

| Asset type | Channel |
|---|---|
| .NET libraries (OASIS-API-Core, ONODE-Core, HoloNET etc.) | NuGet — `NextGenSoftware.*` packages |
| STAR CLI | `dotnet tool install -g NextGenSoftware.OASIS.STAR.CLI` |
| Desktop apps (ONODE Manager, HoloNET Manager, OIDE, HyperDrive Client) | OPORTAL — `oportal.oasisomniverse.one/downloads/` |
| Games (OQuake, ODOOM, Our World) | OPORTAL downloads + game storefronts |
| WEB4–WEB10 APIs | Railway-hosted, auto-deployed from master |

---

## Summary

The two-tier structure means the platform is **open enough to attract a developer ecosystem** — 15 public GitHub repos, 14 NPM packages spanning every major JS framework, and open-source games with a thriving modding community — while keeping every piece of strategic IP firmly under proprietary control. That private core includes the COSMIC ORM, the HyperDrive auto-failover engine, the STAR WEB5 platform, the WEB6 AI orchestration layer, the game engine bridge, the peer-to-peer multiplayer stack, the personal cloud client, and the flagship game. Community developers depend on and build trust in the public surface; the private core represents years of engineering that cannot be replicated from the outside.

As the platform matures — with an established commercial user base and paying subscribers — we plan to selectively open-source additional components where there is genuine demand from open-source developers willing to contribute. The boundary between tiers will evolve as our moat is proven and our community grows.

---

*NextGen Software Ltd · david.ellams@oasisomniverse.one · oasisomniverse.one*
