# OASIS Omniverse — IP & Repository Structure Report

**NextGen Software Ltd** | Prepared for: Team & Investors | August 2026

---

## Overview

The OASIS Omniverse platform is structured across two tiers of GitHub repositories under the **NextGenSoftwareUK** organisation. The public tier demonstrates capability and drives community adoption; the private tier protects the core IP that constitutes our competitive moat.

| | Count |
|---|---|
| Public repositories | 6 |
| Private repositories | 8 |
| Private as % of total | 57% |

---

## Public Repositories — Open Ecosystem

These repositories are intentionally public. They drive developer adoption, NuGet downloads, community contributions, and ecosystem trust. Consumers of the platform build on these without needing access to the proprietary core.

| Repository | What it contains | Role |
|---|---|---|
| **NextGenSoftwareUK/OASIS** | Main monorepo — WEB4 OASIS API, 33+ storage/blockchain/social providers, WebAPI, Swagger, Docker, Railway deploy configs | Public face of the platform; community entry point |
| **NextGenSoftwareUK/holochain-client-csharp** | HoloNET Client, ORM, HyperNET — the only production-grade .NET Holochain client | Community flagship; drives NuGet adoption |
| **NextGenSoftwareUK/HoloNET-Manager** | Desktop WPF reference implementation showcasing every HoloNET Client & ORM capability | Developer showcase; reusable UI components |
| **NextGenSoftwareUK/HoloNET-ORM** | Object Relational Mapping layer for Holochain | Published NuGet package; referenced as submodule |
| **NextGenSoftwareUK/NextGenSoftware-Libraries** | Shared utility libraries (WebSockets, logging, core helpers) | Foundational dependency for all OASIS projects |
| **NextGenSoftwareUK/UltimateDoomBuilder** | OGEditor SDK — level/world editor toolchain for OGEngine games | Open toolchain; drives game developer adoption |

---

## Private Repositories — Competitive Moat

These repositories contain proprietary IP and are never exposed publicly. They are consumed by the public tier either as private NuGet packages, private git submodules, or compiled binaries distributed via OPORTAL.

| Repository | What it contains | Why private |
|---|---|---|
| **NextGenSoftwareUK/OASIS-API-Core** | Core OASIS API interfaces, base classes, and provider contracts — the foundation every provider implements | Proprietary interface design; controls the entire provider ecosystem |
| **NextGenSoftwareUK/OASIS-ONODE-Core** | ONODE (OASIS Network Operating Node) core — supervisor, orchestration, and node management contracts | Core infrastructure IP; powers all deployed OASIS nodes |
| **NextGenSoftwareUK/STAR-ODK** | WEB5 STAR API, ODK runtime, STAR CLI, COSMIC engine, STARDNA — the full WEB5 layer | Flagship proprietary platform; STAR is a core commercial differentiator |
| **NextGenSoftwareUK/OASIS-WEB6** | WEB6 AI layer — MCP server, holonic AI orchestration, FAHRN, multi-provider AI routing, gRPC/GraphQL APIs | Next-generation AI platform IP; cutting-edge multi-agent architecture |
| **NextGenSoftwareUK/ONODEManager** | ONODE Manager (Avalonia desktop app), ONODE Service (background REST supervisor), ONODE Client (typed .NET client) | Operational tooling IP; governs node deployment and management |
| **NextGenSoftwareUK/OGEngineClient** | Native AOT .NET HTTP client for WEB4 & WEB5 APIs + P/Invoke interop bridge into OGEngine native games (OQuake, ODOOM) | Game engine integration layer; bridges OASIS platform into proprietary game runtime |
| **NextGenSoftwareUK/HyperDrive** | Peer-to-peer lag-free networking for Unity/Unreal games built on HoloNET — near-unlimited concurrent players, no cloud server | Disruptive multiplayer tech IP; eliminates traditional game server costs |
| **NextGenSoftwareUK/OIDE** | OASIS Integrated Development Environment — full IDE for building OAPPs and WEB5/WEB6 experiences | Proprietary developer toolchain; creates lock-in and ecosystem stickiness |

---

## Architecture: How the Tiers Interact

```
PUBLIC CONSUMERS
     │
     ▼
┌─────────────────────────────────────────────────────┐
│  NextGenSoftwareUK/OASIS  (public monorepo)          │
│  33+ Providers · WebAPI · NuGet packages             │
│  holochain-client-csharp · HoloNET-ORM               │
└────────────────────┬────────────────────────────────┘
                     │  private submodules / NuGet
                     ▼
┌─────────────────────────────────────────────────────┐
│  PRIVATE CORE                                        │
│  OASIS-API-Core · OASIS-ONODE-Core                  │
│  STAR-ODK · OASIS-WEB6 · ONODEManager               │
│  OGEngineClient · HyperDrive · OIDE                  │
└─────────────────────────────────────────────────────┘
```

The public tier has zero access to private source — it consumes only compiled outputs (NuGet packages, versioned binaries via OPORTAL). Private repos are injected into the Docker build pipeline at Railway using a scoped PAT and are never present in any public build artifact.

---

## Distribution Strategy

| Asset type | Channel |
|---|---|
| .NET libraries (API.Core, ONODE.Core, HoloNET etc.) | NuGet — `NextGenSoftware.*` packages |
| STAR CLI | `dotnet tool install -g NextGenSoftware.OASIS.STAR.CLI` |
| Desktop apps (ONODE Manager, HoloNET Manager, OIDE, HyperDrive) | OPORTAL downloads — `oportal.oasisomniverse.one/downloads/` |
| Games (OQuake, ODOOM) | OPORTAL downloads + storefronts |
| WEB4–WEB10 APIs | Railway-hosted, auto-deployed from master |

---

## Summary

The two-tier structure means the platform is **open enough to attract a developer ecosystem** while keeping every piece of strategic IP — the STAR WEB5 engine, the WEB6 AI layer, the node infrastructure, the game engine bridge, and the IDE — firmly under proprietary control. Consumers depend on the public surface; the private core is what they cannot replicate without years of engineering effort.

---

*NextGen Software Ltd · davidellams@hotmail.com · oasisomniverse.one*
