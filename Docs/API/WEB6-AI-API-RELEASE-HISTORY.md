# WEB6 OASIS AI API RELEASE HISTORY

This document records every public WEB6 OASIS AI API version in chronological order, oldest to newest.
It is the source history synchronized to the public OASIS repository and linked by WEB6 Swagger.

----------------------------------------------------------------------------------------------------------------------------
## 1.0.0 (22/06/26)

Initial release of the WEB6 OASIS AI API — the unified AI abstraction and aggregation layer built on top of WEB4 (OASIS API/ONODE) and WEB5 (STAR ODK/STARNET).

- One API surface for every AI provider (OpenAI, Anthropic, Google Gemini, Mistral, Cohere, Ollama and more).
- Karma-gated AI access — avatar karma level controls which AI capabilities are available.
- JWT authentication via the existing OASIS avatar system.
- Basic provider routing and aggregation.
- Full Swagger/OpenAPI documentation.
- Initial MCP (Model Context Protocol) tool surface.

----------------------------------------------------------------------------------------------------------------------------
## 2.0.0 (11/07/26)

Major upgrade establishing WEB6 as the production-grade unified AI layer.

- **HTTP MCP transport**: the entire OASIS WEB4-WEB10 tool surface is now reachable by any MCP client (Claude.ai, OpenAI, Cursor, etc.) via a single HTTP MCP endpoint.
- **FAHRN** (Fractal Adaptive Holonic Reasoning Network): controller agent that coordinates multi-model reasoning chains across the full OASIS provider fleet, with adaptive routing and self-healing.
- **SSE streaming** (Server-Sent Events): real-time token-by-token streaming for all AI completions.
- **Holonic BRAID**: shared reasoning-graph memory — AI agents share context and reasoning history across sessions via OASIS holons.
- **SkillOpt**: self-evolving agent skills that improve over time based on karma-weighted feedback.
- **ML.NET in-process AutoML**: on-device machine learning without external dependencies.
- **Embeddings API**: vector embeddings for semantic search and RAG pipelines.
- **Avatar context**: all AI requests are scoped to the authenticated OASIS avatar — karma, history and preferences inform AI responses.
- **Debate/Voting dispatch**: route the same prompt to multiple models and aggregate responses via debate or voting strategies.
- **A2A** (Agent-to-Agent): OASIS agents can communicate and delegate tasks between each other via a standardised A2A protocol.
- **WebSocket telemetry**: real-time observability stream for AI request tracing and performance metrics.
- **DID/Verifiable Credentials**: AI access and data sharing gated by decentralised identity and W3C Verifiable Credentials.
- **ACP/ANP/gRPC/GraphQL multi-protocol orchestration**: AI requests can be dispatched via any protocol.
- **Self-registration and discovery**: provider self-registration and a /.well-known discovery endpoint so MCP clients can auto-configure.
- **Provider health monitor**: continuous background health-checks across all registered AI providers with automatic failover.
- **Full OpenTelemetry observability**: distributed tracing, metrics and logs for every AI call.
- **516 MCP tools** across WEB4-WEB10 exposed via the HTTP MCP server.
- Leela AI added as a new WEB6 AI provider.
- Swagger 500 fixes: custom schema IDs, open-form JsonObject/JsonNode/JsonArray mappings, ResolveConflictingActions, excluded SSE/WebSocket/discovery routes from API explorer.
- Various bug fixes and performance improvements.

----------------------------------------------------------------------------------------------------------------------------
## 3.0.0 (23/08/26)

Major WEB6 release expanding the unified AI gateway, orchestration surface and production usage controls.

### What's new in 3.0.0

- Expanded the AI gateway to 100+ providers and added image, video, voice, speech-to-text, local-inference and broader embedding/model coverage.
- Added 25 capability endpoints, model catalogue and provider status APIs, cost estimation, live usage data, rate-limit headers, health reporting and HMAC webhooks.
- Added CrewAI, AutoGen and LangGraph orchestration plus matching MCP tools and synchronous/asynchronous Python SDK support.
- Connected usage metering to the subscription Data API and enforced plan model tiers with karma-based quota multipliers and real remaining-call counters.
- Expanded the underlying OASIS provider fleet across blockchain, identity, messaging, storage, video, spatial and enterprise database integrations used by WEB6 orchestration.
- Completed publish-ready metadata, documentation, build fixes and real implementations for the associated provider graph.

### Full changelog

The first inventory covers every distinct non-merge OASIS parent/dependency change in the 3.0.0 version range. The second covers the dedicated WEB6 source repository.

<details>
<summary>Complete commit inventory (22 distinct changes)</summary>

- [263b46c](https://github.com/NextGenSoftwareUK/OASIS/commit/263b46c431c6194bd1c24861151ac2d2881f996f) feat: bump WEB6APIVersion to 3.0.0
- [6fb81b2](https://github.com/NextGenSoftwareUK/OASIS/commit/6fb81b2b06704542012c3b3caf0343a7c88fb57b) fix: replace all stubs in OracleDBOASIS and SQLServerDBOASIS with real ADO.NET implementations
- [1a6d2a5](https://github.com/NextGenSoftwareUK/OASIS/commit/1a6d2a5783fb91d3a8f751259794de71610991f0) feat: complete all 13 unpublished NuGet providers to publish-ready standard — READMEs, release notes, logo, metadata, copyright
- [6514694](https://github.com/NextGenSoftwareUK/OASIS/commit/65146947877294ca54a21a6ac73dddacee6cc8ab) fix: delete BerrachainOASIS (typo duplicate), fix BerachainOASIS ProviderType enum ref, clean up solution and Startup.cs
- [9b253a5](https://github.com/NextGenSoftwareUK/OASIS/commit/9b253a5bcf90d57f0a659cac2e158edd6a2e4dcd) feat: fix OrionProtocolOASIS and OnionOASIS builds; add spatial/gaming providers; update docs
- [d8dc029](https://github.com/NextGenSoftwareUK/OASIS/commit/d8dc029673b3024ba4503503f2c2c17d03b4e527) feat: fix SQLServerDBOASIS and OracleDBOASIS builds; add MoralisOASIS provider
- [7331508](https://github.com/NextGenSoftwareUK/OASIS/commit/7331508f029ba28fa805c2735e0c219ec554fd22) feat: add 5 new OASIS providers — zkSyncOASIS, ScrollOASIS, LineaOASIS, MonadOASIS, AzureStorageOASIS
- [eff8bfa](https://github.com/NextGenSoftwareUK/OASIS/commit/eff8bfa8f65aef233d74f32448dc389e9ee37639) docs: add AbstractOASIS, BerachainOASIS, StellarOASIS to Swagger, roadmap and Providers.tsx
- [783d161](https://github.com/NextGenSoftwareUK/OASIS/commit/783d1617d98ec117cd6d8fbd39f65ab68a71581d) feat: add 3 new OASIS providers — AbstractOASIS, BerachainOASIS, StellarOASIS
- [2e08a04](https://github.com/NextGenSoftwareUK/OASIS/commit/2e08a0437fca595150b35c20822d3f5d1216daa3) docs: add 12 new OASIS providers to Swagger, Provider-Roadmap and Providers.tsx
- [12021f6](https://github.com/NextGenSoftwareUK/OASIS/commit/12021f6eeb33f6cf12b15b0575f9a9b08ad97940) chore: update WEB6 submodule — quota bug fix, HMAC webhooks, health endpoint, .gitignore, pyproject.toml
- [c1fbb3b](https://github.com/NextGenSoftwareUK/OASIS/commit/c1fbb3b9f6c6a27c4545d07bd28d74c991ebb9a1) feat: add 12 new OASIS providers — SeiOASIS, CelestiaOASIS, EclipseOASIS, PushProtocolOASIS, ENSOASIS, AlchemyOASIS, InfuraOASIS, SafeOASIS, TablelandOASIS, WakuOASIS, LivepeerOASIS, AkashOASIS
- [fe51893](https://github.com/NextGenSoftwareUK/OASIS/commit/fe51893b23b27cdb61ca8357b89a11e73452ec67) chore: update WEB6 submodule — Subscription Data API usage metering, AsyncWeb6Client Python SDK
- [35d7129](https://github.com/NextGenSoftwareUK/OASIS/commit/35d7129efdf20c306f66bdb45269286dc133cbbe) chore: update WEB6 submodule — 6 new capabilities: model catalogue, provider status, cost estimate, rate-limit headers, webhooks, Python SDK
- [bed462e](https://github.com/NextGenSoftwareUK/OASIS/commit/bed462edb89a98e7d06d1ed442d0abdf12b73e34) feat: add 5 new OASIS providers — Discord, TheGraph, WorldID, LitProtocol, StoryProtocol
- [a556f05](https://github.com/NextGenSoftwareUK/OASIS/commit/a556f0549544282d00e382781377638fd7b95a60) chore: update WEB6 submodule — /v1/usage exposes plan+karma quota fields
- [ba809e8](https://github.com/NextGenSoftwareUK/OASIS/commit/ba809e8cc2e7ce01a43a11f6d6861908d7aac5ba) chore: update WEB6 submodule — conservative karma gating, quota multipliers
- [b2c2bf2](https://github.com/NextGenSoftwareUK/OASIS/commit/b2c2bf24e421fcbff3fef1a6b424582fb0f7c208) docs: add swagger descriptions and roadmap entries for 6 new providers
- [83588cc](https://github.com/NextGenSoftwareUK/OASIS/commit/83588cc5dfd249015b6f9a8a2c78c7f23372d343) feat: add 6 new OASIS providers — BlueSky, Matrix, Filecoin, Algorand, Ceramic, Basechain
- [a58434a](https://github.com/NextGenSoftwareUK/OASIS/commit/a58434a2580e7da5ac18f2e927c050caf357ea8e) chore: update WEB6 submodule — 25 new endpoints, 9 capability managers, full docs
- [df8ac72](https://github.com/NextGenSoftwareUK/OASIS/commit/df8ac723656ac3d1a030b81b20623bfbac0946e0) chore: update WEB6 submodule — 100+ AI providers, 5 new capability managers
- [a300b01](https://github.com/NextGenSoftwareUK/OASIS/commit/a300b01fb396d61ba5f2a3e8f4495e9efe3d9d65) feat: add LoomOASIS provider — Loom video messaging platform

</details>

<details>
<summary>Complete commit inventory (14 distinct changes)</summary>

- [9beb261](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/9beb2616cfab0a38e91be8a94b56006da9173d0f) feat: expand MCP tool surface — list_models, get_model, list_providers, estimate_cost, get_usage, orchestrate_crewai/autogen/langgraph, health; update provider count to 20+ in A2A card; add orchestrators + model-catalogue skills
- [f8297ba](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/f8297bacef56d700ad27e0e0170cb3379c1d41a1) feat: add orchestrate_crewai/autogen/langgraph to Python SDK; update provider count to 20+ in Swagger/agent.json
- [cd38a4b](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/cd38a4b8e47caf930c6bdd68207a995ad72cfa35) feat: add new models, SambaNova+OpenRouter providers, CrewAI/AutoGen/LangGraph orchestrators
- [f3c663b](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/f3c663b7d7c0601b0b3a46a8fa19270cff4b0089) fix: real X-RateLimit-Remaining counter + SDK per-call timeouts
- [2f76e37](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/2f76e375e25e0c485534c2b84c6bba9e4980b84d) fix+feat: quota plan/karma from JWT, HMAC webhooks, health endpoint, .gitignore, pyproject.toml
- [4edb7ec](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/4edb7eca41220ea37f3807ed0ed0c7b0c607738c) feat: wire usage metering to Subscription Data API; add AsyncWeb6Client Python SDK
- [d087b69](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/d087b699ae16a3c55901d6e08d7ed69e60b45f61) feat: add 6 new WEB6 capabilities — model catalogue, provider status, cost estimate, rate-limit headers, webhooks, Python SDK
- [5351c73](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/5351c73cec080b43eabe03ca3c29f434d5f31b77) feat: expose plan+karma in /v1/usage — DailyCallLimit, KarmaMultiplier, RemainingCallsToday
- [3326ec9](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/3326ec956f807b9071f4d6681e8e45cee966f480) feat: conservative karma gating — plan gates model tier, karma multiplies quota only
- [b070abb](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/b070abb3f1cd586cdc7e57c72b9596832af8b8c1) feat: full WEB6 capability expansion — 25 new endpoints across all AI modalities
- [871a7cd](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/871a7cdb24073a0bcb67a5a1dbd305d2c441c979) feat: expand WEB6 AI gateway to 100+ providers across all modalities
- [a4ec32f](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/a4ec32f15788f048e6ea1f14172149170472b170) feat(web6): add GPT4All local inference + 8 new embedding providers in EmbeddingManager
- [0ab05ef](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/0ab05ef37da30b2cd8b15582bf13a1eff48d76bf) feat(web6): add 28 more AI providers plus image/video/voice/STT dispatch + full docs
- [3162ec5](https://github.com/NextGenSoftwareUK/OASIS-WEB6/commit/3162ec53c0433ea0b45ab131c9b0265b0312c6a0) feat(web6): add 24 new AI providers covering OpenRouter, TensorArt, full inference suite

</details>

[Compare the OASIS parent range](https://github.com/NextGenSoftwareUK/OASIS/compare/a5657b3bd8eab2f307b69152c016c2b286ec4c08...263b46c431c6194bd1c24861151ac2d2881f996f)

[Compare the WEB6 source range](https://github.com/NextGenSoftwareUK/OASIS-WEB6/compare/601e2d55fc2e91b4c56bc1516186bcb41d30819c...9beb2616cfab0a38e91be8a94b56006da9173d0f)

----------------------------------------------------------------------------------------------------------------------------
## 3.2.0 (27/09/26)

- Unified HyperDrive v2, ONET and offline synchronization; cross-game inventory, GeoNFT and quest progress; API, provider, MCP, runtime and release-pipeline improvements.
- Published by the automated OASIS global release process after CI validation.
