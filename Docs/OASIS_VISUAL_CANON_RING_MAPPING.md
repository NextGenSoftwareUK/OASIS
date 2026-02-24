# OASIS Visual Canon v1 – Ring Mapping (Codebase)

This document maps **products, features, and code** in the OASIS codebase to the five rings of the [OASIS Visual Canon](assets/Screenshot_2026-01-26_at_15.32.58-943e6a8f-5237-4c97-956a-4f2f46fbacb3.png): **Holon → Kernel → Interfaces → Execution Contexts → Edge**. Constraint is highest at the centre; freedom increases outward.

---

## 1. HOLON (centre)

**Definition:** Identity-first unit designed to persist independently of any execution environment. Self-contained, persistent, inspectable.

| Item | Location | Notes |
|------|----------|--------|
| **IHolon** | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Holons/IHolon.cs` | Core contract: parent hierarchy (Omniverse→…→Zome), Load/Save/Delete/AddHolon, events. |
| **HolonBase** | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/HolonBase.cs` | Abstract base: `Id`, `Name`, `Description`, `HolonType`, `MetaData`, `ProviderUniqueStorageKey`, `ProviderMetaData`. |
| **Holon** | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/Holon.cs` | Concrete holon extending `SemanticHolon`; CRUD events, `GlobalHolonData`. |
| **HolonType** | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/HolonType.cs` | Enum of holon kinds (Avatar, Star, Planet, NFT, Mission, etc.) – identity taxonomy. |
| **ISemanticHolon / SemanticHolon** | `API.Core/Interfaces/Holons/`, `API.Core/Holons/SemanticHolon.cs` | Semantic layer for holon (still centre – “what” a holon is). |
| **GlobalHolonData** | `API.Core/Holons/GlobalHolonData.cs` | Cross-provider holon metadata. |
| **STAR holon types** (conceptual) | `API.Core` interfaces under `Interfaces/STAR/` | Celestial holons (IStar, IPlanet, etc.) – still identity units; STAR ODK implements them. |

**Provider-specific holon entities** (e.g. `MongoOASIS/Entities/Holon.cs`, `Neo4jOASIS2/Entities/Holon.cs`, `SQLLiteDBOASIS/Entities/HolonModel.cs`) are **persistence shapes** for the same centre concept; the canonical definition is in API.Core.

---

## 2. KERNEL

**Definition:** Neutral coordination constraints. No products, no economics, no governance outcomes. Minimal invariants.

| Item | Location | Notes |
|------|----------|--------|
| **OASISDNA / OASIS_DNA.json** | `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/` | Config: storage providers, AI, security, HyperDrive mode, replication/failover. Defines *how* OASIS coordinates, not what it sells. |
| **OASIS Boot Loader** | `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/` | Initializes OASIS and providers from DNA. |
| **IOASISStorageProvider** | `API.Core/Interfaces/Providers/` | Base contract for storage (Save, Load, Delete holons, etc.). |
| **IOASISDBStorageProvider** | `API.Core/Interfaces/Providers/IOASISDBStorageProvider.cs` | DB persistence contract (e.g. versioning). |
| **IOASISBlockchainStorageProvider** | `API.Core/Interfaces/Providers/IOASISBlockchainStorageProvider.cs` | Blockchain persistence contract. |
| **OASIS HyperDrive** | `API.Core/Managers/OASIS HyperDrive/` | ProviderManager, ProviderRegistry, ProviderSelector, ProviderSwitcher, ProviderConfigurator, ReplicatorManager. Auto-failover, load-balancing, replication – coordination only. |
| **HolonManager** | `API.Core/Managers/HolonManager/` | Coordinates holon CRUD across providers (uses storage providers; does not define products). |
| **OASISResult / error handling** | `NextGenSoftware.OASIS.Common/`, `API.Core/Helpers/` | Neutral result and error pattern. |
| **ProviderType** | `API.Core/Enums/ProviderType.cs` | Enum of *which* execution contexts exist (blockchains, DBs, clouds, network). Registry of hosts, not products. |

---

## 3. INTERFACES

**Definition:** Legibility without authority. Interpretation, not integration. Expose meaning so others can use the kernel/holon.

| Item | Location | Notes |
|------|----------|--------|
| **Star SDK / STAR ODK** | `STAR-Mac-Build/STAR ODK/` | NextGenSoftware.OASIS.STAR (CelestialBodies, CelestialSpace, Star.cs), STAR CLI Lib (Avatars, NFTs, Missions, Quests, etc.), STARDNA. Primary SDK for building holonic apps; “how you access OASIS.” |
| **ONODE WebAPI** | `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/` | REST API: AvatarController, HolochainController, NftController, SearchController, WalletController, BraidController, KarmaController, etc. HTTP surface for all OASIS operations. |
| **Native EndPoint** | `Native EndPoint/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint/` | OASIS API + STAR API for in-process / native access. |
| **MCP (Model Context Protocol)** | `MCP/` | `src/tools/oasisTools.ts`, `smartContractTools.ts`, etc. – tools for AI/agents to call OASIS (avatar, NFT, wallet, etc.). |
| **OASIS API Contracts** | `OASIS Architecture/NextGenSoftware.OASIS.API.Contracts/` | Data models and provider interfaces (e.g. map, geocoding) used by APIs. |
| **OPORTAL API usage** | `oportal-repo/api/oasisApi.js`, `starApiClient.js` | Client-side usage of OASIS/Star APIs from the portal (portal itself is Edge). |

---

## 4. EXECUTION CONTEXTS

**Definition:** Blockchains, databases, runtimes, clouds. They *host* but do not *define* identity. No canonical host; replaceable, non-authoritative.

| Category | Items | Location (examples) |
|----------|--------|----------------------|
| **Blockchain** | Solana, Ethereum, IPFS, Holo, Telos, EOSIO, BNB, Avalanche, Polygon, Arbitrum, NEAR, Sui, Aptos, Starknet, etc. | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.*OASIS/` |
| **Storage / DB** | MongoDB, Neo4j, Neo4j Aura, SQLLiteDB, LocalFile | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.*OASIS/` |
| **Cloud** | AWS, Azure Cosmos DB, Google Cloud | `Providers/Cloud/` |
| **Network** | IPFS, Pinata, HoloWeb, SOLID, ActivityPub, Scuttlebutt, ThreeFold, Telegram | `Providers/Network/` |
| **Other** | SEEDS, PLAN, Cargo, ONION-Protocol | `Providers/Other/` |

Each provider implements `IOASISStorageProvider` (or DB/Blockchain variant), stores holons by `ProviderUniqueStorageKey`, and is registered with the Kernel (ProviderManager / HyperDrive). They are swappable execution contexts.

---

## 5. EDGE

**Definition:** Applications, economies, experiments. Value creation and risk; freely emerge and fail.

| Item | Location | Notes |
|------|----------|--------|
| **Hitchhikers** | `Hitchhikers/` | Marvin agent, holonic guides; first partnership using Holonic Braid / OASIS tech. |
| **Holonic Braid (demo)** | `ONODE/.../Controllers/BraidController.cs` | OpenSERV + OASIS collaboration demo; standard vs holonic metrics, LLM integration. |
| **OPORTAL** | `ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL/`, `oportal-repo/` | Portal UI: avatars, NFTs, quests, wallet, missions, content verification, STAR dashboard. |
| **ARWorld** | `ARWorld/` | Unity game; OASIS integration, avatars, gameplay. |
| **Pangea** | `pangea-repo/` | Apps: cap-mgmt, launchboard, markets; use OASIS (e.g. cap-mgmt `oasis-client`, `oasis-sync`). |
| **A2A (Agent-to-Agent)** | `A2A/`, `API.Core/Managers/A2AManager/` | Protocol, OpenSERV/SERV, payments, agent cards – application/ecosystem layer. |
| **Holonic demo** | `holonic-demo/` | Braid agents, `oasis-api.js` – demos and experiments. |
| **Holonic visualizer** | `holonic-visualizer/` | Celestial/holon visualization – experiment. |
| **Karma** | `API.Core/Managers/KarmaManager.cs`, `ONODE/.../KarmaController.cs` | Reputation/scoring economy (avatar-bound). |
| **TimoRides** | `TimoRides/` | Ride scheduler – app using OASIS. |
| **Reform Labs / oracle** | `Reform Labs/`, `oasis-oracle-frontend/` | Government spend, NHS viz – experiments. |
| **Universal Asset Bridge** | `UniversalAssetBridge/` | Cross-chain assets – economy/experiment. |
| **OASIS-IDE** | `OASIS-IDE/` | Developer IDE – tool/experiment. |
| **Tokenomics / liquidity** | `tokenomics/`, `Web4 Liquidity Pool/`, `API.Core/Managers/Liquidity/` | Token and liquidity design – economies. |
| **Subscription / billing** | `ONODE/.../SubscriptionController.cs`, Stripe-related | Payments – economy. |
| **SCMS** | `ONODE/.../Controllers/SCMS*.cs` | Supply chain – application. |
| **Voice memo, AI, Video** | `ONODE/.../VoiceMemoController.cs`, `AIController.cs`, `VideoController.cs` | Feature endpoints used by apps. |

---

## Summary Table

| Ring | Constraint | Contains (summary) |
|------|------------|--------------------|
| **Holon** | Highest | IHolon, HolonBase, Holon, HolonType, semantic/celestial types – identity unit. |
| **Kernel** | High | OASIS DNA, Boot Loader, provider interfaces, HyperDrive, HolonManager – coordination only. |
| **Interfaces** | Medium | Star SDK/ODK, ONODE WebAPI, Native EndPoint, MCP, API contracts – legibility/access. |
| **Execution** | Lower | All providers (blockchain, DB, cloud, network) – hosts for holons. |
| **Edge** | Lowest | Hitchhikers, Braid demo, OPORTAL, ARWorld, Pangea, A2A, Karma, TimoRides, demos, tokenomics, SCMS, etc. |

---

## How to use this mapping

- **Narrative / docs:** Start with Holon (centre), then Kernel, then Interfaces (Star, ONODE, MCP), then “you can run on these execution contexts,” then “here are apps and economies at the edge.”
- **Branding:** Star SDK and ONODE are **Interfaces** to OASIS; they are not separate products. Hitchhikers and Holonic Braid are **Edge** use cases of the same stack.
- **Prioritisation:** Core = Holon + Kernel; de-prioritise Edge (Karma, avatars/game, many apps) until narrative and one strong Edge use case (e.g. Holonic Braid) are clear.

If you add new repos or features, assign them to a ring using the definitions above and update this doc.
