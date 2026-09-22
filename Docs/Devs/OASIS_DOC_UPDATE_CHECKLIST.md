# OASIS Doc Update Checklist

Run through this list whenever WEB6 capabilities change: new AI providers, orchestrator protocols, memory adapters, MCP tool count, REST endpoint count, or tool descriptions.

---

## Current counts (last verified 2026-09-21)

| Metric | Value |
|--------|-------|
| AI providers | **100** (99 real + `Auto`) |
| Orchestrator protocols | **22** (MCP, A2A, ACP, ANP, LangGraph, OpenAI Agents SDK, Nostr NIP-90, LangChain, AutoGen, CrewAI, SemanticKernel, BeeAgent, Temporal, Dapr, NATSJetStream, gRPC, GraphQL, Kafka, AMQP, MQTT, Webhook) |
| External memory adapters | 7 (Mem0, Zep, Letta, LangMem, Graphiti, Qdrant, Weaviate) |
| MCP tools | **507** (371 WEB4 + 96 WEB5 + 40 WEB6) |
| REST endpoints | 56 |
| OASIS storage/network/identity providers | 227 |

---

## Source of truth (enums/code to check first)

| What | File |
|------|------|
| AI provider enum | `WEB6/NextGenSoftware.OASIS.Web6.Core/Enums/AIProviderType.cs` |
| Orchestrator protocol enum | `WEB6/NextGenSoftware.OASIS.Web6.Core/Enums/OrchestratorProtocolType.cs` |
| MCP tool definitions | `WEB6/NextGenSoftware.OASIS.MCP.Server/Tools/Web4Tools.cs`, `Web5Tools.cs`, `Web6Tools.cs` |
| REST controllers | `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/` |

---

## Files to update

### OASIS repo — `C:\Source\OASIS` (branch: Development)

| File | What to update |
|------|---------------|
| `Docs/Devs/API Documentation/WEB6/WEB6_REST_API_Reference.md` | Orchestrator `protocol` values, memory provider list |
| `Docs/Devs/API Documentation/WEB6/WEB6-Getting-Started-Guide.md` | Provider count (×many), orchestrator count + list, full provider table |
| `Docs/Devs/API Documentation/WEB6/WEB6_MCP_Tool_Reference.md` | Provider count in tool descriptions, orchestrator list |
| `Docs/Devs/API Documentation/WEB6/WEB6_Partner_Overview.md` | Provider count (headline + table) |
| `Docs/Devs/API Documentation/WEB6/WEB6_User_Guide.md` | Memory provider list |
| `Docs/Devs/API Documentation/WEB6/WEB6_Quotas_and_Tiers.md` | Provider count |
| `Docs/Devs/WEB4_SUBSCRIPTION_AUTHORITY.md` | Any provider/tool counts referenced |
| `Docs/Devs/WEB6_METERING_WEB4_INTEGRATION_AUDIT.md` | Any provider/tool counts referenced |
| `Docs/Devs/WEB6_QUICKSTART.md` | Provider count, orchestrator count |
| `Docs/INVESTOR_EVALUATION_GUIDE.md` | Provider count, protocol count, full provider/protocol lists |
| `Docs/OASIS_TECHNOLOGY_SUMMARY_AND_USE_CASES.md` | Provider count, provider category breakdown, memory list |
| `Docs/OASIS_UNIQUE_SELLING_PROPOSITIONS.md` | Provider count, memory provider list |
| `Docs/OASIS-IP-Repository-Report.md` | WEB6 row: provider count, protocol count, sample list |
| `Docs/OASIS-IP-Repository-Report.html` | Stat tiles + repository table row |

### WEB6 submodule — `C:\Source\OASIS\WEB6` (branch: main)

| File | What to update |
|------|---------------|
| `README.md` | Header stats line, AI Provider Reference section (all 100 providers), Orchestrator Protocol Reference section (all 22 protocols) |
| `NextGenSoftware.OASIS.MCP.Server/README.md` | `web6_complete` row (provider count), `web6_orchestrator_invoke` row (protocol list) |
| `NextGenSoftware.OASIS.MCP.Server/Tools/Web6Tools.cs` | `web6_orchestrator_invoke` Description attribute (protocol list) |
| `NextGenSoftware.OASIS.MCP.Server/Tools/Web4Tools.cs` | Add any MCP tools for new REST endpoints (see gap audit) |
| `NextGenSoftware.OASIS.MCP.Server/Tools/Web5Tools.cs` | Add any MCP tools for new REST endpoints (see gap audit) |

### Web6Site repo — `C:\Source\Web6Site` (branch: main)

| File | What to update |
|------|---------------|
| `index.html` | Stat tiles: AI providers, MCP tools count, orchestrator count + label |
| `index.html` | Hero description: orchestrator count + list, provider count |
| `api.html` | `/v1/providers` desc, `web6_complete` and `web6_list_providers` rows in MCP table |
| `api.html` | Orchestrator endpoint descriptions (`web6_orchestrator_invoke`) |
| `providers.html` | Full 100-provider reference table (10 categories, enum keys, env vars) — **new page** |
| `holonic-braid-whitepaper.html` | Memory provider paragraph, OrchestratorManager protocol list |
| `whitepaper-pdf.html` | Check for mirrored content from holonic-braid-whitepaper.html |

### oasisweb4.com — `C:\Source\OASIS\oasisweb4.com` (branch: Development)

| File | What to update |
|------|---------------|
| `src/pages/APIs.tsx` | `web6Apis[0].desc` — provider count in AI Completion card |

### OASISLearnWebsite — `C:\Source\OASISLearnWebsite` (branch: main)

| File | What to update |
|------|---------------|
| `tutorials/15-web6-ai-intro.html` | Provider count and example provider list |

---

## Quick grep to find stale numbers

```bash
grep -rn "99 provider\|17 orchestrat\|20+ AI\|250 MCP\|6 orchestrat" \
  C:/Source/OASIS/Docs \
  C:/Source/OASIS/WEB6 \
  C:/Source/Web6Site \
  C:/Source/OASIS/oasisweb4.com
```

---

## MCP tool gap audit

Whenever new REST endpoints are added to controllers in  
`ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/`,  
check `WEB6/NextGenSoftware.OASIS.MCP.Server/Tools/Web4Tools.cs` (or Web5Tools.cs)  
for a matching `McpServerTool`. Run:

```bash
grep -rn "McpServerTool" C:/Source/OASIS/WEB6/NextGenSoftware.OASIS.MCP.Server/Tools/ | grep -oP 'Name = "\K[^"]+' | sort
```

Full gap audit run 2026-09-21. Gap implementation completed 2026-09-21: **507 tools** total (371 Web4 across Web4Tools.cs + 5 batch files + 96 Web5 + 40 Web6). All previously uncovered controllers now have MCP tools (some stubbed where HTTP session context is required).

### Controllers with ZERO MCP coverage (entire controllers uncovered)

| Controller | Routes | Description |
|------------|--------|-------------|
| MapController | `/api/map/...` | 20+ endpoints: nearby search, visit/draw, pan/zoom, routes |
| ONETController | `/api/onet/...` | OASISDNA config, network status/nodes/topology, connect/disconnect/broadcast, node register |
| ONODEController | `/api/onode/...` | ONODE lifecycle, metrics, logs, config, peers, provider enable/disable, audit log |
| ProviderController | `/api/provider/...` | Register/unregister, set active, auto-replication/failover/load-balance, activate/deactivate |
| OLandController | `/api/oland/...` | Load/buy/transfer OLand parcels |
| OlandUnitController | `/api/oland/...` | CRUD for OLand units |
| VideoController | `/api/video/...` | Start/join/end video calls |
| BridgeController | `/api/bridge/...` | Cross-chain bridge orders, exchange rates, networks, proofs |
| EggsController | `/api/eggs/...` | List eggs, discover, hatch, quest leaderboard |
| GiftsController | `/api/gifts/...` | Send/receive/open gifts, gift history |
| SeedsController | `/api/seeds/...` | SEEDS crypto transactions |
| OidcController | `/oauth/...` | OIDC discovery, JWKS, userinfo, authorize, token |

### Controllers with PARTIAL coverage — known gaps

| Controller | Missing MCP Tools |
|------------|-------------------|
| AvatarSessionController | get-sessions, get-session-stats, create-session, update-session, logout, logout-all, validate-account-token |
| AvatarProfileController | UMA JSON endpoints, add-xp, set-active-quest, search, all update variants, delete by username/email, full inventory CRUD |
| AvatarAuthController | authenticate-token, DID challenge/auth, refresh-token, revoke-token, register-with-provider |
| KarmaController | get/vote/set karma weightings, akashic records, activity feed |
| WalletController | load by username/email, default wallet get/set, import key, portfolio value, token transfer, wallet tokens, analytics |
| WalletTokenController | burn-token, lock-token, unlock-token, import-by-secret-phrase |
| KeysController | private keys by email, WIF decode, base58 decode, signature encode, key CRUD, link wallet address, list all |
| LevelController | level-lookup table, calculate-level-from-karma |
| NftController | ~25+ missing: collection CRUD, transfer history, load-all variants, geo-NFT update |
| FilesController | upload, download, delete, metadata CRUD |
| ClanController | All 10 endpoints: create, update, load, members, inventory |
| CompetitionController | leaderboard, rank, leagues, tournaments, stats |
| HerzIdController | register, vouch, verify, profile, set-clearance, vouch-chain, ghost-check |
| HolochainController | agent IDs, private keys, HoloFuel balance |
| EOSIOController | account name/keys/balance lookups (8 endpoints) |
| SettingsController | all settings CRUD, notification prefs, privacy, system config |
| ShareController | share-holon with one/many avatars |
| BiometricController | status, voice enroll/verify/delete |
| StatsController | achievement-stats |
| DataController | save-file/load-file, save-data/load-data, load-by-metadata, save-holons bulk, provider-key ops |

---

## When to run this checklist

- New AI provider added → update all count references + provider tables
- New orchestrator protocol added → update enum, tool Description, docs + web pages
- New REST endpoint added → add MCP tool + update MCP README
- New memory adapter added → update memory section in docs + web pages
- MCP tool added/removed → update MCP README tool counts (WEB4/WEB5/WEB6 section headers + total)
