# WEB6 MCP Tool Reference

The OASIS MCP Server exposes **111 typed named tools** covering WEB4 through WEB10. This reference lists every tool, its parameters, and its return value.

All tools return a JSON-serialised `OASISResult<T>` envelope. On success, `isError` is `false` and the data is in `result`. On failure, `isError` is `true` and `message` describes the problem.

Install: `npm install -g @oasisomniverse/mcp-server` or `dotnet tool install -g NextGenSoftware.OASIS.MCP.Server`

---

## WEB4 — Identity, Karma, NFTs, Wallets

### Avatar tools

#### `web4_avatar_register`
Register a new OASIS avatar.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `username` | string | yes | Unique username |
| `email` | string | yes | Email address |
| `password` | string | yes | Password (hashed server-side) |
| `firstName` | string | no | First name |
| `lastName` | string | no | Last name |

**Returns:** The created avatar object including its assigned GUID.

---

#### `web4_avatar_authenticate`
Authenticate an avatar. Returns the avatar including its JWT token.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `username` | string | yes | Username or email |
| `password` | string | yes | Password |

**Returns:** Avatar object with `jwtToken` field.

---

#### `web4_avatar_load_by_id`
Load an avatar by GUID.

**Parameters:** `id` (string, GUID)

---

#### `web4_avatar_load_by_email`
Load an avatar by email address.

**Parameters:** `email` (string)

---

#### `web4_avatar_load_by_username`
Load an avatar by username.

**Parameters:** `username` (string)

---

#### `web4_avatar_load_all`
Load all registered avatars. Admin/server-side use.

**Parameters:** none

---

#### `web4_avatar_save`
Create or update an avatar. Pass the full avatar JSON.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `avatarJson` | string | yes | JSON with avatar fields. Omit `Id` for create; include for update. |
| `avatarId` | string | no | Caller's avatar ID for auth context |

---

#### `web4_avatar_delete`
Soft-delete an avatar by ID.

**Parameters:** `id` (string, GUID), `softDelete` (bool, default true)

---

### Karma tools

#### `web4_karma_get`
Get an avatar's current karma total.

**Parameters:** `avatarId` (string, GUID)

**Returns:** `{ "karmaTotal": 1240 }`

---

#### `web4_karma_add`
Add karma for a positive action.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `avatarId` | string | yes | Target avatar |
| `karmaType` | string | yes | Type of positive action |
| `karmaSourceTitle` | string | no | What generated the karma |
| `karmaSourceDesc` | string | no | Description |

---

#### `web4_karma_deduct`
Deduct karma for a negative action.

**Parameters:** same shape as `web4_karma_add` but deducts.

---

#### `web4_karma_get_history`
Get the full karma transaction history for an avatar.

**Parameters:** `avatarId` (string, GUID)

---

#### `web4_karma_transfer`
Transfer karma from one avatar to another.

**Parameters:** `fromAvatarId`, `toAvatarId`, `amount` (int), `reason` (string)

---

#### `web4_karma_get_stats`
Get karma statistics and leaderboard rankings.

**Parameters:** `avatarId` (string, GUID, optional — omit for global stats)

---

### Holon tools

#### `web4_holon_load`
Load a holon by ID.

**Parameters:** `holonId` (string, GUID)

---

#### `web4_holon_save`
Create or update a holon. Pass holonJson with `HolonType`, `Name`, `Description`, and `MetaData`.

**Parameters:** `holonJson` (string), `avatarId` (string, optional)

---

#### `web4_holon_delete`
Delete a holon.

**Parameters:** `holonId` (string, GUID)

---

#### `web4_holon_search`
Search holons by name or type.

**Parameters:** `searchTerm` (string), `holonType` (string, optional)

---

### NFT tools

#### `web4_nft_load`
Load an NFT by ID.

**Parameters:** `nftId` (string, GUID)

---

#### `web4_nft_load_all_for_avatar`
List all NFTs owned by an avatar.

**Parameters:** `avatarId` (string, GUID)

---

#### `web4_nft_mint`
Mint a new NFT.

**Parameters:** `mintNftJson` (string) ��� JSON with `Name`, `Description`, `ImageUrl`, `MetaData`, `ProviderType`

---

#### `web4_nft_send`
Transfer an NFT to another avatar.

**Parameters:** `nftId` (string, GUID), `toAvatarId` (string, GUID)

---

#### `web4_nft_delete`
Delete an NFT.

**Parameters:** `nftId` (string, GUID)

---

#### `web4_nft_collection_load`
Load an NFT collection by ID.

**Parameters:** `collectionId` (string, GUID)

---

#### `web4_nft_collection_load_all_for_avatar`
List all NFT collections for an avatar.

**Parameters:** `avatarId` (string, GUID)

---

### Geo NFT tools

#### `web4_geo_nft_mint_and_place`
Mint an NFT and place it at a geographic coordinate (for AR/geo-caching).

**Parameters:** `mintAndPlaceNftJson` (string) — JSON with `Name`, `Description`, `Lat`, `Long`, `ProviderType`

---

#### `web4_geo_nft_load`
Load a geo-placed NFT by ID.

**Parameters:** `nftId` (string, GUID)

---

#### `web4_geo_nft_load_all_for_avatar`
List all geo NFTs owned by an avatar.

**Parameters:** `avatarId` (string, GUID)

---

#### `web4_geo_nft_load_near_location`
Find geo NFTs within a radius of a coordinate.

**Parameters:** `lat` (float), `long` (float), `radiusKm` (float, default 1.0)

---

#### `web4_geo_nft_delete`
Delete a geo NFT.

**Parameters:** `nftId` (string, GUID)

---

### Wallet tools

#### `web4_wallet_get_total_balance`
Get total wallet balance across all chains for an avatar.

**Parameters:** `avatarId` (string, GUID)

---

#### `web4_wallet_load_provider_wallets`
List wallets per provider/chain.

**Parameters:** `avatarId` (string, GUID), `providerType` (string, optional)

---

#### `web4_wallet_create`
Create a new wallet for an avatar.

**Parameters:** `avatarId` (string, GUID), `providerType` (string)

---

## WEB5 — Quests, Missions, OAPPs, Holons

### Quest tools

#### `web5_quest_load`
Load a quest by ID.

**Parameters:** `questId` (string, GUID)

---

#### `web5_quest_load_all_for_avatar`
List all quests for an avatar.

**Parameters:** `avatarId` (string, GUID)

---

#### `web5_quest_search`
Search quests by keyword.

**Parameters:** `searchTerm` (string)

---

#### `web5_quest_start`
Start a quest for an avatar.

**Parameters:** `questId` (string, GUID), `avatarId` (string, GUID)

---

#### `web5_quest_complete_objective`
Mark a quest objective complete.

**Parameters:** `questId` (string, GUID), `objectiveId` (string, GUID), `avatarId` (string, GUID)

---

#### `web5_quest_complete`
Complete a quest.

**Parameters:** `questId` (string, GUID), `avatarId` (string, GUID)

---

### Mission tools

#### `web5_mission_load` · `web5_mission_load_all_for_avatar` · `web5_mission_search`
Same pattern as quests.

#### `web5_mission_complete`
Complete a mission.

**Parameters:** `missionId` (string, GUID), `avatarId` (string, GUID)

---

#### `web5_mission_get_leaderboard`
Get the leaderboard for a mission.

**Parameters:** `missionId` (string, GUID)

---

#### `web5_mission_get_rewards`
Get rewards for completing a mission.

**Parameters:** `missionId` (string, GUID)

---

#### `web5_mission_get_stats`
Get mission statistics.

**Parameters:** `missionId` (string, GUID)

---

### OAPP tools

#### `web5_oapp_load` · `web5_oapp_load_all_for_avatar` · `web5_oapp_search`
Load, list, or search OAPPs.

#### `web5_oapp_download`
Download an OAPP.

**Parameters:** `oappId` (string, GUID), `avatarId` (string, GUID)

---

#### `web5_oapp_download_and_install`
Download and install an OAPP in one step.

**Parameters:** `oappId` (string, GUID), `avatarId` (string, GUID)

---

#### `web5_oapp_activate` · `web5_oapp_deactivate`
Activate or deactivate an installed OAPP.

**Parameters:** `oappId` (string, GUID), `avatarId` (string, GUID)

---

#### `web5_oapp_is_installed`
Check if an OAPP is installed for an avatar.

**Parameters:** `oappId` (string, GUID), `avatarId` (string, GUID)

**Returns:** `{ "installed": true }`

---

#### `web5_oapp_list_installed`
List all installed OAPPs for an avatar.

**Parameters:** `avatarId` (string, GUID)

---

### Holon & Celestial Body tools (WEB5)

`web5_holon_load`, `web5_holon_load_all_for_avatar`, `web5_holon_search`, `web5_holon_download`, `web5_holon_download_and_install` — same pattern as WEB4 holon tools but operating on STAR holons.

`web5_celestial_body_load`, `web5_celestial_body_load_all_for_avatar`, `web5_celestial_body_search`, `web5_celestial_body_download` — celestial bodies in the STAR ODK universe.

---

## WEB6 — AI, FAHRN, BRAID, Memory, Orchestration

### AI Completion & Embeddings

#### `web6_complete`
Unified AI completion — routes to 15+ providers with auto failover.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `request` | CompletionRequest | yes | Full completion request object |
| `avatarId` | string | no | Avatar ID for key vault lookup |

**CompletionRequest fields:**
- `provider` (string, default `"auto"`)
- `model` (string, default `"auto"`)
- `messages` (array of `{role, content}`)
- `maxTokens` (int)
- `temperature` (float)
- `tools` (array, optional)
- `routing.priority` (`quality`/`latency`/`cost`/`balanced`)
- `externalMemoryProviders` (array of provider names, optional)

---

#### `web6_embed`
Generate text embeddings.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `request` | EmbeddingRequest | yes | `{ texts, provider, model }` |
| `avatarId` | string | no | |

---

#### `web6_generate_image`
Generate an image.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `request` | ImageGenerationRequest | yes | `{ prompt, provider, model, size, quality }` |
| `avatarId` | string | no | |

---

### FAHRN Tools

#### `web6_fahrn_solve`
Full FAHRN pipeline in one call. Recommended entry point.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `request` | FahrnSolveRequest | yes | Problem + budget + mode |
| `avatarId` | string | no | |

**FahrnSolveRequest fields:**
- `problem` (string) — the natural language problem
- `mode` (string, default `"auto"`) — `serial`/`parallel`/`debate`/`voting`/`decomposed`/`auto`
- `maxTotalTokens` (int, optional)
- `maxCostUsd` (decimal, optional)
- `maxTokensPerAgent` (int, optional)
- `budgetExceededBehaviour` (string, default `"stop"`) — `stop` or `best_so_far`
- `externalMemoryProviders` (array, optional)

---

#### `web6_fahrn_register_agent`
Register a new agent in the FAHRN network.

**Parameters:** `config` (AgentHolonConfig), `avatarId` (string, optional)

---

#### `web6_fahrn_get_agents`
List all registered FAHRN agents.

**Parameters:** `avatarId` (string, optional)

---

#### `web6_fahrn_seed_openserv_agents`
Auto-seed the agent registry from the OpenSERV SERV catalogue.

**Parameters:** `avatarId` (string, optional)

---

#### `web6_fahrn_dispatch`
Dispatch a problem to the agent network with full control.

**Parameters:** `request` (DispatchRequest), `avatarId` (string, optional)

**DispatchRequest fields:**
- `problem`, `taskType`, `mode`
- `eligibleAgentIds` (array, optional — omit for all agents)
- `maxTotalTokens`, `maxCostUsd`, `maxTokensPerAgent`
- `budgetExceededBehaviour`
- `votingStrategy` (`majority`/`unanimous`/`weighted`)
- `minVotingAgents` (int)
- `externalMemoryProviders` (array)

---

#### `web6_fahrn_get_agent_skill`
Get the current SkillOpt skill document for an agent and task category.

**Parameters:** `agentId` (string, GUID), `taskCategory` (string), `avatarId` (string, optional)

---

#### `web6_fahrn_evolve_agent_skill`
Run a SkillOpt epoch to improve an agent's skill document.

**Parameters:** `agentId` (string, GUID), `taskCategory` (string), `avatarId` (string, optional)

---

### Holonic BRAID Tools

#### `web6_braid_find_graph`
Look up the shared reasoning graph for a task type.

**Parameters:** `taskType` (string), `avatarId` (string, optional)

---

#### `web6_braid_save_graph`
Store a new Mermaid reasoning graph.

**Parameters:** `taskType` (string), `mermaidDiagram` (string), `generatedByModel` (string), `version` (int, default 1), `avatarId` (string, optional)

---

#### `web6_braid_record_outcome`
Record a solver outcome to update the graph's EMA accuracy score.

**Parameters:** `graphId` (string, GUID), `solverModel` (string), `succeeded` (bool), `qualityScore` (float), `tokensUsed` (int), `avatarId` (string, optional)

---

### Holonic Memory Tools

#### `web6_memory_get_earth_holon`
Get or create the planetary Earth holon.

**Parameters:** `avatarId` (string, optional)

---

#### `web6_memory_get_or_create_holon`
Get or create a holon at a given level.

**Parameters:** `level` (string), `name` (string), `parentHolonId` (string, GUID), `avatarId` (string, optional)

**`level` values:** `Session`, `Agent`, `User`, `Group`, `Neighbourhood`, `District`, `City`, `County`, `Country`, `Continent`

---

#### `web6_memory_set_membrane_rule`
Set the membrane propagation rule for a holon.

**Parameters:** `holonId` (string, GUID), `rule` (MembraneRule object), `avatarId` (string, optional)

**MembraneRule fields:** `fieldsAllowedToPropagate` (array), `anonymisedAggregateOnly` (bool), `triggerCondition` (string)

---

#### `web6_memory_record`
Record a memory item at a holon.

**Parameters:** `holonId` (string, GUID), `item` (HolonicMemoryItem), `avatarId` (string, optional)

**HolonicMemoryItem fields:** `fieldName`, `value`, `tags` (array), `retentionPolicy` (`Ephemeral`/`Session`/`Persistent`/`TimeLimited`), `expiresUtc` (datetime, for TimeLimited)

---

#### `web6_memory_propagate`
Propagate permitted items from a child holon to its parent (single hop).

**Parameters:** `childHolonId` (string, GUID), `avatarId` (string, optional)

---

#### `web6_memory_propagate_up`
Multi-hop upward propagation up the fractal hierarchy.

**Parameters:** `childHolonId` (string, GUID), `levels` (int, default 1), `avatarId` (string, optional)

Pass `levels=2147483647` to propagate all the way to the Earth holon.

---

#### `web6_memory_search`
Semantic search over all memory items in a holon.

**Parameters:**
| Name | Type | Default | Description |
|---|---|---|---|
| `holonId` | string | required | Holon to search |
| `query` | string | required | Natural language query |
| `topK` | int | 5 | Maximum results |
| `embeddingProvider` | string | `"auto"` | `auto`/`openai`/`cohere`/`huggingface` |
| `avatarId` | string | — | Optional |

**Returns:** Array of `{ item, score }` ranked by cosine similarity (keyword overlap fallback when no embeddings stored).

---

### External Memory Provider Tools

#### `web6_memory_external_search`
Semantic search across all configured external memory providers.

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `avatarId` | string | yes | Avatar whose memories to search |
| `query` | string | yes | Search query |
| `providers` | array | no | Subset of providers; omit for all |
| `topK` | int | no | Max results (default 10) |

**Returns:** Merged, score-ranked results from all queried providers.

---

#### `web6_memory_external_add`
Add a memory to a specific external provider.

**Parameters:** `avatarId` (string), `provider` (string), `content` (string), `metadata` (object, optional)

---

#### `web6_memory_external_list_providers`
List all configured and active external memory providers.

**Parameters:** none

**Returns:** Array of provider name strings.

---

### Orchestrator Tools

#### `web6_orchestrator_register`
Register an external agent/orchestrator endpoint.

**Parameters:** `config` (OrchestratorAdapterConfig), `avatarId` (string, optional)

**OrchestratorAdapterConfig fields:** `name`, `protocol` (MCP/A2A/ACP/ANP/GRPC/GraphQL/Kafka/AMQP/MQTT/HTTP), `endpointUrl`, `agentId`

---

#### `web6_orchestrator_list`
List all registered orchestrator adapters.

**Parameters:** `avatarId` (string, optional)

---

#### `web6_orchestrator_invoke`
Invoke a registered adapter with a problem.

**Parameters:** `request` (OrchestratorInvokeRequest — `adapterId`, `problem`, `avatarId`), `avatarId` (string, optional)

---

### Utility Tools

#### `web6_ml_classify_task`
Classify a problem string to a FAHRN task category using the in-process ML.NET model. Zero latency.

**Parameters:** `text` (string), `avatarId` (string, optional)

**Returns:** `{ "taskType": "code" }`

---

#### `web6_ml_sentiment`
Sentiment analysis.

**Parameters:** `text` (string), `avatarId` (string, optional)

**Returns:** `{ "sentiment": "Positive" }`

---

#### `web6_list_openserv_models`
List the live OpenSERV SERV model catalogue.

**Parameters:** `avatarId` (string, optional)

---

#### `web6_get_avatar_context`
Get a rich context block for an avatar — karma, quests, OAPPs, Web7 spaces, Web8 nodes.

**Parameters:** `avatarId` (string, GUID)

---

## WEB7 — Symbiosis & Collective Consciousness

#### `web7_start_session`
Start a Web7 symbiosis session.

**Parameters:** `avatarId` (string, GUID), `sessionType` (string)

---

#### `web7_submit_signals`
Submit biometric or consciousness signals to an active session.

**Parameters:** `sessionId` (string), `signals` (object — provider-specific signal map)

---

#### `web7_end_session`
End a Web7 session.

**Parameters:** `sessionId` (string)

---

#### `web7_get_session`
Get session state and aggregated signals.

**Parameters:** `sessionId` (string)

---

#### `web7_create_space`
Create a collective consciousness space.

**Parameters:** `name` (string), `description` (string), `avatarId` (string)

---

#### `web7_join_space`
Join an existing consciousness space.

**Parameters:** `spaceId` (string, GUID), `avatarId` (string, GUID)

---

#### `web7_get_aggregate_field`
Get the aggregate field value for a consciousness space.

**Parameters:** `spaceId` (string, GUID), `fieldName` (string)

---

## WEB8 — Galactic Mesh & Protocol Bridge

#### `web8_register_node`
Register a node in the galactic mesh.

**Parameters:** `node` (MeshNodeConfig — `id`, `name`, `endpointUrl`, `protocols`)

---

#### `web8_get_nodes`
List all nodes in the mesh.

**Parameters:** none

---

#### `web8_add_link`
Add a directed link between two mesh nodes.

**Parameters:** `fromNodeId`, `toNodeId`, `weight` (float, optional)

---

#### `web8_heartbeat`
Send a heartbeat for a node to mark it as active.

**Parameters:** `nodeId` (string, GUID)

---

#### `web8_compute_route`
Compute the optimal route between two nodes.

**Parameters:** `fromNodeId`, `toNodeId`

**Returns:** Array of node IDs representing the shortest path.

---

#### `web8_send_message`
Send a message across the mesh to a target node.

**Parameters:** `toNodeId` (string, GUID), `message` (string), `protocol` (string, optional)

---

#### `web8_translate_inbound`
Translate an inbound message from a foreign protocol to OASIS normalised format.

**Parameters:** `rawMessage` (string), `sourceProtocol` (string)

---

#### `web8_translate_outbound`
Translate an OASIS normalised message to a target protocol's wire format.

**Parameters:** `message` (string), `targetProtocol` (string)

---

## WEB9 — Singularity

#### `web9_get_unified_status`
Get the unified singularity status — aggregate health, coherence, and consciousness metrics across all active OASIS layers.

**Parameters:** none

---

## WEB10 — The Source

#### `web10_get_source`
Access the omniversal source layer — the unified field of all OASIS data, consciousness, and computation.

**Parameters:** none

---

## See also

- [WEB6 REST API Reference](WEB6_REST_API_Reference.md)
- [WEB6 User Guide](WEB6_User_Guide.md)
- [MCP Server README](../../../../WEB6/NextGenSoftware.OASIS.MCP.Server/README.md)
