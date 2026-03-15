# OASIS / NextGen — IP Overview for Licensing Strategy

**Purpose:** Detailed document on **what IP exists** and **in which form**, to support IP licensing strategy that works with LFG and OASIS (per David Bovill’s suggestion).  
**Basis:** Technology pieces explicitly referenced in the **LFG Preliminary Proposal** (Feb 2026) as the core of OASIS/NextGen’s value.  
**Note:** LFG’s view is that “the only thing that matters is where the IP is housed.” This doc clarifies what that IP is so licensing and company structure can be designed around it.

---

## 1. Core IP (LFG: “the real intellectual property here”)

### 1.1 Holonic Architecture

| Aspect | Description |
|--------|-------------|
| **What it is** | Universal data model: the **Holon** as a data object that is both a whole and a part; converts any code/language into a standardized, interoperable format. Creates a **global graph library** — persistent, deduplicated, accessible across all connected systems. Identity-independent (GUID not tied to any single provider). |
| **Why LFG care** | Core innovation; “the Holonic architecture is the real intellectual property here”; underpins B2G data interop, AI efficiency (Holonic BRAID), and all other capabilities. |
| **IP form** | Software (interfaces, data structures, runtime); architecture & design; specifications; whitepapers. |
| **Where it lives** | OASIS codebase / NextGen Software (repos, runtime, documentation). |

### 1.2 Hyperdrive (Resilience Layer)

| Aspect | Description |
|--------|-------------|
| **What it is** | Auto-replication across connected providers; auto-routing to fastest/cheapest/available option. Zero downtime, cost optimization, **provider-agnostic** (AWS, Azure, GCP, Holochain, IPFS, multiple DBs, EVM and non-EVM chains). |
| **Why LFG care** | “Provider-agnostic resilience”; critical for government and enterprise (no single point of failure, no vendor lock-in). |
| **IP form** | Software (replication, failover, load-balancing logic); configuration and provider contracts. |
| **Where it lives** | OASIS codebase (e.g. HolonManager, ProviderManager, provider implementations). |

### 1.3 ORM (Data Migration / COSMIC ORM)

| Aspect | Description |
|--------|-------------|
| **What it is** | Object-Relational Mapper / data migration layer: automates **Web2 → Web3** (and cross-provider) migration programmatically, without AI dependency. Reduces risk of data corruption/loss. |
| **Why LFG care** | “Data migration automation”; directly relevant to B2G data sovereignty; potential ZK-compatible sovereign data migration product with ZK Pass / ZKYC. |
| **IP form** | Software (migration pipelines, mappings, COSMIC ORM layer); data models and schemas. |
| **Where it lives** | OASIS codebase (COSMIC ORM, ORM-related providers and tooling). |

---

## 2. Additional Capabilities (LFG-listed technology)

### 2.1 NFT API

| Aspect | Description |
|--------|-------------|
| **What it is** | Mint, transfer, manage NFTs across any chain via natural language; integrated into MCP; **Web4 NFTs** span all chains simultaneously. |
| **IP form** | Software (API, MCP integration); API specs. |
| **Where it lives** | OASIS codebase / ONODE API. |

### 2.2 GeoNFTs

| Aspect | Description |
|--------|-------------|
| **What it is** | Geocached, geo-fenced NFTs with real-world coordinates; appear in AR; credential-gated metadata; demonstrated in Unity-based (e.g. Pokemon Go–style) app. |
| **Why LFG care** | B2G geospatial credentialing and workforce verification; integrable with Pose/Pebble hardware. |
| **IP form** | Software (geocache logic, metadata, AR integration); product/spec. |
| **Where it lives** | OASIS codebase; AR/game assets if separate. |

### 2.3 Identity & Karma

| Aspect | Description |
|--------|-------------|
| **What it is** | Single cross-chain identity system; Karma scoring (rewards positive actions); scores queryable; gates access to missions, rewards, credentials. |
| **Why LFG care** | Programmable government benefits and incentives; credential-gated programmes. |
| **IP form** | Software (identity layer, Karma logic, credentials); data models. |
| **Where it lives** | OASIS codebase. |

### 2.4 Star Engine

| Aspect | Description |
|--------|-------------|
| **What it is** | Data visualization layer; applications as celestial bodies; virtual cyberspace view; app discovery and gamification. |
| **IP form** | Software (visualization, ontology); possible UI/front-end assets. |
| **Where it lives** | OASIS codebase / STAR layer. |

### 2.5 MCP Integration

| Aspect | Description |
|--------|-------------|
| **What it is** | Full OASIS API exposed via Model Context Protocol; 200+ operations via natural language; onboard new chain in under 10 minutes. |
| **Why LFG care** | “MCP integration enabling natural-language Web3 operations” — differentiator for AI agents and IDEs. |
| **IP form** | Software (MCP server, API surface); API definitions and docs. |
| **Where it lives** | OASIS codebase / ONODE; MCP tooling. |

### 2.6 Smart Contract Ops

| Aspect | Description |
|--------|-------------|
| **What it is** | Deploy and manage smart contracts across any chain via natural language; atomic swaps across connected providers. |
| **IP form** | Software (contract deployment, swap logic, multi-chain abstraction). |
| **Where it lives** | OASIS codebase. |

---

## 3. Holonic BRAID (B2G / AI use case)

| Aspect | Description |
|--------|-------------|
| **What it is** | Combination of OpenSERV’s BRAID (bounded reasoning, ~74× efficiency) with OASIS holonic architecture: **shared reasoning graphs stored as holons**; many agents reuse same graph; persistent, provider-agnostic. |
| **Why LFG care** | “AI Data Infrastructure Efficiency” — concrete B2G value proposition; government digital infrastructure and national AI strategy. |
| **IP form** | Software (graph library, BRAID integration, holon-backed graphs); methodology; papers/litepapers. |
| **Where it lives** | OASIS codebase; OpenSERV integration; Holonic BRAID docs and demos. |

---

## 4. Summary: What IP? In which form?

| IP / technology | Form(s) | Relevant to LFG / licensing |
|------------------|--------|----------------------------|
| **Holonic architecture** | Code, interfaces, specs, whitepapers | Core; “where the IP is housed” = where this lives |
| **Hyperdrive** | Code, provider contracts, config | Resilience; multi-provider value |
| **ORM / data migration** | Code, COSMIC ORM, schemas | B2G data sovereignty; ZK potential |
| **NFT API / Web4 NFTs** | Code, API, MCP | Cross-chain, AI/agent use |
| **GeoNFTs** | Code, geocache/AR logic | B2G geospatial, Pose/Pebble |
| **Identity & Karma** | Code, identity/karma logic | B2G benefits, credentials |
| **Star Engine** | Code, ontology, UI | Ecosystem visualization |
| **MCP Integration** | Code, MCP server, API | AI/IDE differentiation |
| **Smart contract ops** | Code, multi-chain layer | Enterprise/Web3 |
| **Holonic BRAID** | Code, BRAID integration, methodology, papers | B2G AI efficiency, pilots |

**Where it lives today:** OASIS repositories, NextGen Software codebase, ONODE/API, documentation and whitepapers. Ownership and licensing are determined by current agreements (OASIS project, NextGen, David as founder, etc.).

---

## 5. Implications for licensing and company structure

- **LFG’s position:** “The only thing that matters is where the IP is housed.” So licensing and structure should make clear: (a) **where** the above IP is owned/housed (e.g. OASIS project, NextGen, or a dedicated IP entity), and (b) **what** is licensed to whom (e.g. OASIS Commercial, LFG-backed vehicles, JVs).
- **David Bovill’s suggestion:** A detailed document on **what IP** and **in which form** to support an **IP licensing strategy** that works with both LFG and OASIS. This doc is that outline; it can be expanded with exact repo names, ownership, and existing licences.
- **Next steps:** (1) Confirm ownership of each bucket (OASIS vs NextGen vs other). (2) Decide what is licensed to OASIS Commercial (and on what terms). (3) Design licence(s) so LFG (and partners) engage with a single, clear IP-holding structure and a clear licensee (e.g. OASIS Commercial).

---

*Derived from LFG Preliminary Proposal (Feb 2026), Sections 3 (Technology Understanding) and 5 (B2G Value Propositions). Expand with legal and repo-level detail as needed.*
