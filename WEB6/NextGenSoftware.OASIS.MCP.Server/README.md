# OASIS MCP Server

The OASIS [Model Context Protocol](https://modelcontextprotocol.io) Server — **101 typed tools** covering the full OASIS stack (WEB4 through WEB10) delivered directly into Cursor, VS Code, Claude Desktop, and any other MCP-compatible IDE.

Built on the official [`ModelContextProtocol`](https://github.com/modelcontextprotocol/csharp-sdk) C# SDK, communicating over stdio. Part of the [OASIS Omniverse](https://oasisomniverse.one) ecosystem.

> Every tool is **specific, named, and typed** — the agent sees exactly what's available and can reason about what to call. Generic HTTP passthrough is deliberately omitted because it gives agents no discoverability.

---

## Install

### Option A — npm (recommended, no .NET SDK required)

```bash
npm install -g @oasisomniverse/mcp-server
```

Downloads the correct pre-built native binary for your platform (Windows / macOS / Linux, x64 & arm64) automatically.

### Option B — NuGet dotnet tool (requires .NET 8+ SDK)

```bash
dotnet tool install -g NextGenSoftware.OASIS.MCP.Server
```

### Option C — Build from source (requires .NET 10 SDK)

```bash
git clone https://github.com/NextGenSoftwareUK/OASIS.git
cd OASIS/WEB6/NextGenSoftware.OASIS.MCP.Server
dotnet build -c Release
dotnet run
```

---

## Configure your IDE

After installing, the `oasis-mcp` command is on your PATH. Add it to your IDE's MCP config:

**Cursor** (`~/.cursor/mcp.json`) / **VS Code** (`.vscode/mcp.json`) / **Claude Desktop** (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "oasis": {
      "command": "oasis-mcp",
      "env": {
        "OASIS_API_URL": "https://api.oasisplatform.world",
        "OASIS_MCP_LICENSE_KEY": "your-license-key",
        "OPENAI_API_KEY": "sk-...",
        "ANTHROPIC_API_KEY": "sk-ant-..."
      }
    }
  }
}
```

For a local build, point at the project instead:

```json
{
  "mcpServers": {
    "oasis": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/OASIS/WEB6/NextGenSoftware.OASIS.MCP.Server"]
    }
  }
}
```

---

## Tool Reference

### WEB4 — Identity, Karma, NFTs, Wallets (25 tools)

#### Avatar

| Tool | Description |
|---|---|
| `web4_avatar_register` | Register a new OASIS avatar |
| `web4_avatar_authenticate` | Authenticate by username/password, returns JWT |
| `web4_avatar_load_by_id` | Load avatar by GUID |
| `web4_avatar_load_by_email` | Load avatar by email |
| `web4_avatar_load_by_username` | Load avatar by username |
| `web4_avatar_load_all` | Load all avatars (admin) |
| `web4_avatar_save` | Create or update an avatar |
| `web4_avatar_delete` | Soft-delete an avatar |

#### Karma

| Tool | Description |
|---|---|
| `web4_karma_get` | Get an avatar's current karma total |
| `web4_karma_add` | Add karma for a positive action |
| `web4_karma_deduct` | Deduct karma for a negative action |
| `web4_karma_get_history` | Get full karma transaction history |
| `web4_karma_transfer` | Transfer karma between avatars |
| `web4_karma_get_stats` | Get karma statistics and rankings |

#### Holons, NFTs & Wallets

| Tool | Description |
|---|---|
| `web4_holon_load` | Load a holon by ID |
| `web4_holon_save` | Save (create/update) a holon |
| `web4_holon_delete` | Delete a holon |
| `web4_holon_search` | Search holons by name/type |
| `web4_nft_load` | Load an NFT by ID |
| `web4_nft_load_all_for_avatar` | List all NFTs for an avatar |
| `web4_nft_mint` | Mint a new NFT |
| `web4_nft_send` | Transfer an NFT to another avatar |
| `web4_nft_delete` | Delete an NFT |
| `web4_nft_collection_load` | Load an NFT collection |
| `web4_nft_collection_load_all_for_avatar` | List all NFT collections for an avatar |
| `web4_geo_nft_mint_and_place` | Mint an NFT and place it at a geo coordinate |
| `web4_geo_nft_load` | Load a geo-placed NFT |
| `web4_geo_nft_load_all_for_avatar` | List all geo NFTs for an avatar |
| `web4_geo_nft_load_near_location` | Find geo NFTs near a lat/lng |
| `web4_geo_nft_delete` | Delete a geo NFT |
| `web4_wallet_get_total_balance` | Get total wallet balance across all chains |
| `web4_wallet_load_provider_wallets` | List wallets per provider/chain |
| `web4_wallet_create` | Create a new wallet |

---

### WEB5 — Holon Graph, OAPPs, Quests, Missions (26 tools)

#### Quests & Missions

| Tool | Description |
|---|---|
| `web5_quest_load` | Load a quest by ID |
| `web5_quest_load_all_for_avatar` | List all quests for an avatar |
| `web5_quest_search` | Search quests by keyword |
| `web5_quest_start` | Start a quest |
| `web5_quest_complete_objective` | Mark a quest objective complete |
| `web5_quest_complete` | Complete a quest |
| `web5_mission_load` | Load a mission by ID |
| `web5_mission_load_all_for_avatar` | List all missions for an avatar |
| `web5_mission_search` | Search missions |
| `web5_mission_complete` | Complete a mission |
| `web5_mission_get_leaderboard` | Get mission leaderboard |
| `web5_mission_get_rewards` | Get mission rewards |
| `web5_mission_get_stats` | Get mission statistics |

#### OAPPs (OASIS Applications)

| Tool | Description |
|---|---|
| `web5_oapp_load` | Load an OAPP by ID |
| `web5_oapp_load_all_for_avatar` | List all OAPPs for an avatar |
| `web5_oapp_search` | Search OAPPs by keyword |
| `web5_oapp_download` | Download an OAPP |
| `web5_oapp_download_and_install` | Download and install an OAPP |
| `web5_oapp_activate` | Activate an installed OAPP |
| `web5_oapp_deactivate` | Deactivate an OAPP |
| `web5_oapp_is_installed` | Check if an OAPP is installed |
| `web5_oapp_list_installed` | List all installed OAPPs |

#### Holons & Celestial Bodies

| Tool | Description |
|---|---|
| `web5_holon_load` | Load a STAR holon |
| `web5_holon_load_all_for_avatar` | List holons for an avatar |
| `web5_holon_search` | Search holons |
| `web5_holon_download` | Download a holon |
| `web5_holon_download_and_install` | Download and install a holon |
| `web5_celestial_body_load` | Load a celestial body |
| `web5_celestial_body_load_all_for_avatar` | List celestial bodies for an avatar |
| `web5_celestial_body_search` | Search celestial bodies |
| `web5_celestial_body_download` | Download a celestial body |

---

### WEB6 — AI, FAHRN, BRAID, Memory, Orchestration (28 tools)

#### AI Completion & Embeddings

| Tool | Description |
|---|---|
| `web6_complete` | Unified AI completion — routes to 15+ providers, auto failover |
| `web6_embed` | Generate text embeddings (OpenAI / Cohere / HuggingFace) |
| `web6_generate_image` | Generate an image (StabilityAI, OpenAI) |

#### FAHRN Multi-Agent

| Tool | Description |
|---|---|
| `web6_fahrn_solve` | Hero endpoint — full pipeline: classify → BRAID → dispatch → score |
| `web6_fahrn_register_agent` | Register a new FAHRN agent |
| `web6_fahrn_get_agents` | List all registered FAHRN agents |
| `web6_fahrn_seed_openserv_agents` | Auto-seed agents from OpenSERV catalogue |
| `web6_fahrn_dispatch` | Dispatch a problem (Serial/Parallel/Debate/Voting/Decomposed modes) |
| `web6_fahrn_get_agent_skill` | Get the current SkillOpt skill document for an agent |
| `web6_fahrn_evolve_agent_skill` | Run a SkillOpt epoch to improve an agent's skill |

#### Holonic BRAID

| Tool | Description |
|---|---|
| `web6_braid_find_graph` | Look up the shared reasoning graph for a task type |
| `web6_braid_save_graph` | Store a new Mermaid reasoning graph |
| `web6_braid_record_outcome` | Record a solver outcome to update EMA accuracy |

#### Holonic Memory

| Tool | Description |
|---|---|
| `web6_memory_get_earth_holon` | Get or create the planetary Earth holon |
| `web6_memory_get_or_create_holon` | Get or create a holon at any level |
| `web6_memory_set_membrane_rule` | Set the membrane propagation rule for a holon |
| `web6_memory_record` | Record a memory item at a holon |
| `web6_memory_propagate` | Propagate permitted items to parent (single hop) |
| `web6_memory_propagate_up` | Multi-hop propagation (pass `levels=2147483647` for Earth) |
| `web6_memory_search` | Semantic search within a holon (cosine similarity, keyword fallback) |

#### External Memory Providers

| Tool | Description |
|---|---|
| `web6_memory_external_search` | Search across all configured providers (Mem0/Zep/Letta/LangMem/Graphiti) |
| `web6_memory_external_add` | Add a memory to a specific external provider |
| `web6_memory_external_list_providers` | List all configured external memory providers |

#### Orchestrators

| Tool | Description |
|---|---|
| `web6_orchestrator_register` | Register an external agent/orchestrator endpoint |
| `web6_orchestrator_list` | List all registered orchestrator adapters |
| `web6_orchestrator_invoke` | Invoke an adapter (MCP/A2A/ACP/ANP/GraphQL/Kafka/AMQP/MQTT) |

#### ML.NET & Context

| Tool | Description |
|---|---|
| `web6_ml_classify_task` | Classify problem text to a FAHRN task category (zero-latency, in-process) |
| `web6_ml_sentiment` | Sentiment analysis — Positive / Neutral / Negative |
| `web6_list_openserv_models` | List available OpenSERV SERV model catalogue |
| `web6_get_avatar_context` | Rich avatar context block (karma, quests, OAPPs, Web7/8 membership) |

---

### WEB7 — Symbiosis & Collective Consciousness (7 tools)

| Tool | Description |
|---|---|
| `web7_start_session` | Start a Web7 symbiosis session |
| `web7_submit_signals` | Submit biometric/consciousness signals to a session |
| `web7_end_session` | End a Web7 session |
| `web7_get_session` | Get session state and aggregated signals |
| `web7_create_space` | Create a collective consciousness space |
| `web7_join_space` | Join an existing consciousness space |
| `web7_get_aggregate_field` | Get the aggregate field value for a space |

---

### WEB8 — Galactic Mesh & Protocol Bridge (8 tools)

| Tool | Description |
|---|---|
| `web8_register_node` | Register a node in the galactic mesh |
| `web8_get_nodes` | List all nodes in the mesh |
| `web8_add_link` | Add a link between two mesh nodes |
| `web8_heartbeat` | Send a node heartbeat |
| `web8_compute_route` | Compute the optimal route between two nodes |
| `web8_send_message` | Send a message across the mesh |
| `web8_translate_inbound` | Translate an inbound protocol message to OASIS format |
| `web8_translate_outbound` | Translate an OASIS message to a target protocol format |

---

### WEB9 — Singularity (1 tool)

| Tool | Description |
|---|---|
| `web9_get_unified_status` | Get the unified singularity status across all OASIS layers |

---

### WEB10 — The Source (1 tool)

| Tool | Description |
|---|---|
| `web10_get_source` | Access the omniversal source layer status and field values |

---

## Environment variables

### Required

| Variable | Purpose |
|---|---|
| `OASIS_API_URL` | WEB4 API base URL (default: `https://api.oasisplatform.world`) |
| `OASIS_MCP_LICENSE_KEY` | License key (required for write operations) |

### AI Provider Keys (at least one required for WEB6 tools)

| Variable | Provider |
|---|---|
| `OPENAI_API_KEY` | OpenAI (completions, embeddings, images) |
| `ANTHROPIC_API_KEY` | Anthropic / Claude |
| `GEMINI_API_KEY` | Google Gemini |
| `GROQ_API_KEY` | Groq |
| `MISTRAL_API_KEY` | Mistral |
| `COHERE_API_KEY` | Cohere (completions + embeddings) |
| `XAI_API_KEY` | xAI / Grok |
| `DEEPSEEK_API_KEY` | DeepSeek |
| `HUGGINGFACE_API_KEY` | HuggingFace (completions + embeddings) |
| `SERV_API_KEY` | OpenSERV (one key, all models) |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint URL |
| `AWS_ACCESS_KEY_ID` | AWS Bedrock |
| `AWS_SECRET_ACCESS_KEY` | AWS Bedrock |
| `AWS_REGION` | AWS Bedrock region |

### External Memory Providers (optional)

| Variable | Provider |
|---|---|
| `MEM0_API_KEY` | Mem0 |
| `ZEP_API_KEY` | Zep |
| `LETTA_BASE_URL` | Letta (default: `http://localhost:8283`) |
| `LANGMEM_API_KEY` | LangMem |
| `GRAPHITI_BASE_URL` | Graphiti |

### API Base URL Overrides (for local dev)

| Variable | Default |
|---|---|
| `WEB4_API_BASE_URL` | `https://api.web4.oasisomniverse.one` |
| `WEB5_API_BASE_URL` | `https://api.starnet.oasisomniverse.one` |
| `WEB6_API_BASE_URL` | `https://api.web6.oasisomniverse.one` |

---

## Links

- [oasisomniverse.one](https://oasisomniverse.one)
- [oasisweb4.com/products/mcp](https://oasisweb4.com/products/mcp) — product page & pricing
- [WEB6 README](../README.md) — full REST API reference
- [WEB6 API Reference](../../Docs/Devs/API%20Documentation/WEB6/WEB6_REST_API_Reference.md)
- [WEB6 MCP Tool Reference](../../Docs/Devs/API%20Documentation/WEB6/WEB6_MCP_Tool_Reference.md)
- [WEB6 User Guide](../../Docs/Devs/API%20Documentation/WEB6/WEB6_User_Guide.md)
- [OASIS GitHub](https://github.com/NextGenSoftwareUK/OASIS)
