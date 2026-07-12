# OASIS API Documentation

## Overview

Welcome to the OASIS API documentation. The OASIS platform has seven live API layers as of July 2026, spanning WEB4 through WEB10. The primary development focus is WEB4–WEB6; WEB7–WEB10 are live with MCP tools available now and full REST API surfaces in development.

- **WEB4 OASIS API** - The foundational layer bridging Web2 and Web3
- **WEB5 STAR API** - Advanced metaverse and application development layer
- **WEB6 OASIS AI API** - The Intelligence Layer — unified AI orchestration, holonic memory, and self-evolving agents
- **WEB7 Symbiosis Layer** - Bio-signal and neural interface integration (7 MCP tools live)
- **WEB8 Galactic Mesh** - Distributed mesh networking at planetary scale (8 MCP tools live)
- **WEB9 Singularity Layer** - Unified cross-layer status and observability (1 MCP tool live)
- **WEB10 Source Layer** - Root identity and ontological foundation of the OASIS stack (1 MCP tool live)

## API Documentation

### [WEB4 OASIS API](WEB4%20OASIS%20API/README.md)

The WEB4 OASIS API provides the core infrastructure for the OASIS ecosystem, including:

- **Identity & Authentication** - Avatar, Keys, Karma management
- **Data & Storage** - Universal data management across Web2/Web3
- **Blockchain Integration** - Multi-chain wallet, NFT, and transaction support
- **Network Operations** - ONET protocol and HyperDrive functionality
- **Core Services** - Search, Stats, Settings, Messaging, and more

[View WEB4 OASIS API Documentation →](WEB4%20OASIS%20API/README.md)

### [WEB5 STAR API](WEB5%20STAR%20API/README.md)

The WEB5 STAR API provides advanced functionality for building immersive metaverse experiences, including:

- **Game Mechanics** - Missions, Quests, Competition, Eggs
- **Celestial Systems** - Planets, Stars, Moons, Space Regions
- **Location Services** - GeoNFTs, GeoHotSpots, AR Integration
- **Development Tools** - OAPPs, Zomes, Templates, Runtimes
- **Data Structures** - Holons, Chapters, Inventory
- **Communication** - Chat, Messaging, Social Features

[View WEB5 STAR API Documentation →](WEB5%20STAR%20API/README.md)

### [WEB6 OASIS AI API](../../WEB6/README.md) *(Live — July 2026)*

The WEB6 OASIS AI API is the Intelligence Layer that supercharges every application built on WEB4 and WEB5 with production-grade AI capabilities:

- **FAHRN Orchestrator** - Multi-agent reasoning: Serial, Parallel, Debate, Voting, and Decomposed modes; auto-selected by ML.NET classifier. Benchmarked 74× PPD improvement on GSM-Hard
- **SkillOpt** - Self-evolving agent skills (Microsoft Research, arXiv:2605.23904) — +23.5% accuracy gain; builds a proprietary skill corpus asset over time
- **Holonic Memory** - Fractal persistent memory (Session→Agent→User→Group→…→Earth) with semantic search, TTL, and multi-hop propagation
- **Karma-Gated AI** - Bronze/Silver/Gold/Diamond access tiers; aligned incentives as recurring SaaS revenue
- **20+ AI Providers** - OpenAI, Anthropic, Google Gemini, Groq, Mistral, Cohere, xAI, Ollama, HuggingFace, DeepSeek, AWS Bedrock, Azure, Replicate, StabilityAI, and more — one unified API
- **DID/Verifiable Credentials** - W3C DID (did:key, did:web, did:ethr, did:ion), HMAC-SHA256 proof, Universal Resolver
- **BudgetGuard** - Per-dispatch cost and token caps enforced before any API call
- **250 MCP tools** — the largest production MCP surface area of any AI platform
- **REST API v2**: 56 endpoints · Swagger at `https://api.web6.oasisomniverse.one/swagger`
- **npm**: `@oasisomniverse/web6-api` (v2.0.0 — 14 modules, 40 operations)

[View WEB6 REST API Reference →](WEB6/WEB6_REST_API_Reference.md)
[View WEB6 MCP Tool Reference →](WEB6/WEB6_MCP_Tool_Reference.md)
[View WEB6 User Guide →](WEB6/WEB6_User_Guide.md)

### WEB7 — Symbiosis Layer *(Live — 7 MCP tools)*

The WEB7 Symbiosis Layer bridges biological and digital intelligence through bio-signal and neural interface integration. 7 MCP tools are live now; the full REST API surface is in active development.

- **Bio-Signal Integration** - Ingest heart rate, EEG, galvanic skin response and other physiological signals
- **Neural Interface Hooks** - Low-latency bridge to BCI devices (OpenBCI, Muse, NeuroSky)
- **Biometric-Verified Karma** - Karma actions verified and weighted by real physiological state
- **Adaptive AI Responses** - AI behaviour modulates based on user's measured cognitive/emotional state
- **Symbiosis Sessions** - Persistent session context combining digital identity with bio-signal stream

*WEB7 REST API reference coming as the layer matures. Use MCP tools for current access.*
[View MCP Tool Reference — WEB7 section →](WEB6/WEB6_MCP_Tool_Reference.md)

### WEB8 — Galactic Mesh *(Live — 8 MCP tools)*

The WEB8 Galactic Mesh layer extends the OASIS network beyond planetary infrastructure into distributed mesh networking at scale. 8 MCP tools are live.

- **Galactic Node Registry** - Register and discover mesh nodes across global and off-world infrastructure
- **Mesh Routing** - Intelligent routing across planetary and satellite relay networks
- **Distributed Consensus** - Byzantine-fault-tolerant consensus across geographically dispersed nodes
- **Bandwidth Optimisation** - Adaptive compression and protocol switching for high-latency links
- **Mesh Telemetry** - Real-time health and performance monitoring across the mesh

[View MCP Tool Reference — WEB8 section →](WEB6/WEB6_MCP_Tool_Reference.md)

### WEB9 — Singularity Layer *(Live — 1 MCP tool)*

The WEB9 Singularity Layer provides a unified status interface across the entire OASIS stack — a single endpoint that surfaces the operational state of every layer from WEB4 through WEB10.

- **Unified Stack Status** - Single MCP tool returns live health across all OASIS layers
- **Cross-Layer Observability** - Aggregated metrics, error rates, and latency from every service
- **Singularity Health Score** - Composite wellness score for the full OASIS ecosystem

[View MCP Tool Reference — WEB9 section →](WEB6/WEB6_MCP_Tool_Reference.md)

### WEB10 — Source / Root Identity *(Live — 1 MCP tool)*

The WEB10 Source Layer is the root identity and ontological foundation of the OASIS stack — the anchor point from which all holonic identity hierarchies originate.

- **Root Identity Anchor** - The ultimate source holon from which all avatar, group, and Earth holons descend
- **Ontological Integrity** - Ensures every identity chain in the system traces back to a verifiable root
- **Source Attestation** - Cryptographic proof of identity lineage from avatar all the way to Source

[View MCP Tool Reference — WEB10 section →](WEB6/WEB6_MCP_Tool_Reference.md)

## Getting Started

### Authentication

All APIs require authentication using Bearer tokens:

```http
Authorization: Bearer YOUR_TOKEN
```

### Response Format

All API responses follow the standard OASIS response format:

```json
{
  "result": {
    "success": true,
    "data": { ... },
    "message": "Operation successful"
  },
  "isError": false,
  "message": "Success"
}
```

### Error Handling

Error responses include detailed information:

```json
{
  "result": null,
  "isError": true,
  "message": "Error message",
  "exception": "Detailed error information"
}
```

## API Features

### Multi-Chain Support
- Ethereum, Solana, Cardano, NEAR, and 40+ blockchains
- Unified interface across all chains
- Automatic provider failover and load balancing

### Real-Time Operations
- WebSocket support for live updates
- Event-driven architecture
- Push notifications

### Security
- AES-256 encryption
- JWT authentication
- RBAC authorization
- GDPR/CCPA compliance

### Performance
- Auto-scaling infrastructure
- CDN integration
- Response caching
- Rate limiting

## Support & Resources

- **Developer Documentation**: [DEVELOPER_DOCUMENTATION_INDEX.md](../DEVELOPER_DOCUMENTATION_INDEX.md)
- **Implementation Status**: [CURRENT_IMPLEMENTATION_STATUS.md](../../CURRENT_IMPLEMENTATION_STATUS.md)
- **Investor Guide**: [INVESTOR_EVALUATION_GUIDE.md](../../INVESTOR_EVALUATION_GUIDE.md)
- **Main README**: [README.md](../../../README.md)

## Quick Links

- [WEB4 OASIS API →](WEB4%20OASIS%20API/README.md)
- [WEB5 STAR API →](WEB5%20STAR%20API/README.md)
- [Developer Documentation →](../DEVELOPER_DOCUMENTATION_INDEX.md)

---

**[Back to Developer Documentation](../DEVELOPER_DOCUMENTATION_INDEX.md)**
