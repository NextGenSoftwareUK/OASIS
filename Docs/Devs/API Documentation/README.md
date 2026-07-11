# OASIS API Documentation

## Overview

Welcome to the OASIS API documentation. The OASIS platform provides three fully live API layers (as of July 2026), with WEB7–WEB10 on the roadmap:

- **WEB4 OASIS API** - The foundational layer bridging Web2 and Web3
- **WEB5 STAR API** - Advanced metaverse and application development layer
- **WEB6 OASIS AI API** - The Intelligence Layer — unified AI orchestration, holonic memory, and self-evolving agents

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
- **111 MCP Tools** — the largest production MCP surface area of any AI platform
- **REST API v2**: 56 endpoints · Swagger at `https://api.web6.oasisomniverse.one/swagger`
- **npm**: `@oasisomniverse/web6-api` (v2.0.0 — 14 modules, 40 operations)

[View WEB6 REST API Reference →](../../WEB6/Docs/WEB6_REST_API_REFERENCE.md)
[View WEB6 MCP Tool Reference →](../../WEB6/Docs/WEB6_MCP_TOOL_REFERENCE.md)
[View WEB6 User Guide →](../../WEB6/Docs/WEB6_USER_GUIDE.md)

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
