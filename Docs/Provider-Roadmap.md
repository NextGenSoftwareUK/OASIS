# OASIS Provider Inventory & Roadmap

_Last updated: 2026-08-16_

---

## Current Provider Inventory

### Blockchain / L1 / L2

| Provider | Project |
|---|---|
| Ethereum | EthereumOASIS |
| Bitcoin | BitcoinOASIS |
| Solana | SOLANAOASIS |
| BNB Chain | BNBChainOASIS |
| Polygon | PolygonOASIS |
| Arbitrum | ArbitrumOASIS |
| Optimism | OptimismOASIS |
| Avalanche | AvalancheOASIS |
| Base | BaseOASIS |
| Cardano | CardanoOASIS |
| Polkadot | PolkadotOASIS |
| NEAR | NEAROASIS |
| Cosmos | CosmosBlockChainOASIS |
| TRON | TRONOASIS |
| XRP Ledger | XRPLOASIS |
| EOS | EOSIOOASIS |
| Sui | SuiOASIS |
| Aptos | AptosOASIS |
| Hedera Hashgraph | HashgraphOASIS |
| MultiversX (Elrond) | ElrondOASIS |
| Fantom | FantomOASIS |
| zkSync | ZkSyncOASIS |
| Scroll | ScrollOASIS |
| Linea | LineaOASIS |
| Rootstock (RSK) | RootstockOASIS |
| Telos | TelosOASIS |
| Stacks (BlockStack) | BlockStackOASIS |
| Zcash | ZcashOASIS |
| Miden | MidenOASIS |
| Aztec | AztecOASIS |
| Starknet | StarknetOASIS |
| Radix | RadixOASIS |
| TON | TONOASIS |
| Monad | MonadOASIS |
| ChainLink | ChainLinkOASIS |

### Storage / Database

| Provider | Project |
|---|---|
| MongoDB | MongoOASIS |
| Neo4j | Neo4jOASIS / Neo4jOASIS2 / Neo4jOASIS.Aura |
| Azure Cosmos DB | AzureCosmosDBOASIS |
| AWS | AWSOASIS |
| Google Cloud | GoogleCloudOASIS |
| SQLite | SQLLiteDBOASIS |
| Local File | LocalFileOASIS |
| IPFS | IPFSOASIS |
| Pinata (IPFS pinning) | PinataOASIS |
| SOLID (Tim Berners-Lee) | SOLIDOASIS |
| ThreeFold | ThreeFoldOASIS |

### Decentralised Social / Network

| Provider | Project |
|---|---|
| ActivityPub | ActivityPubOASIS |
| Holochain | HoloOASIS / HoloOASIS.Desktop / HoloOASIS.Unity |
| Scuttlebutt | ScuttlebuttOASIS |
| SEEDS | SEEDSOASIS |
| Telegram | TelegramOASIS |
| Orion Protocol (DEX) | OrionProtocolOASIS |
| Tor / Onion | ONION-Protocol |
| Moralis | MoralisOASIS |

### Spatial / Gaming / Other

| Provider | Project |
|---|---|
| GO Map | GOMapOASIS |
| Mapbox | MapboxOASIS |
| WRLD 3D | WRLD3DOASIS |
| Cargo (NFT) | CargoOASIS |
| PLAN | PLANOASIS |

---

## New Providers — Recommended Priority Order

Ranked by: user reach, ecosystem activity (as of mid-2026), and strategic fit with OASIS's avatar/identity/NFT/Web5 mission.

---

### Tier 1 — Implement First (highest reach, immediate value)

#### 1. Arweave
- **What:** Permanent, pay-once decentralised storage — data stored on Arweave is guaranteed to exist forever
- **Why:** Core to OASIS's promise that avatars and holons cannot be lost or deleted. The only network that genuinely delivers permanent storage. Used by Solana NFT metadata, Mirror.xyz, and thousands of dApps. SDK: `ArweaveNet` (.NET NuGet), REST API at `https://arweave.net`
- **OASIS fit:** Avatar/holon permanent backup; NFT metadata permanence; "your data lives forever" marketing hook

#### 2. Abstract
- **What:** EVM L2 purpose-built for consumer apps and gaming (launched 2024, Ethereum-settled)
- **Why:** The fastest-growing consumer crypto chain; built explicitly for the use case OASIS targets. Has a large NFT and gaming community. Fully EVM-compatible so it extends `Web3CoreOASISBaseProvider` with minimal new code
- **OASIS fit:** Consumer gaming avatars, NFT minting, in-game item provenance

#### 3. Berachain
- **What:** EVM-compatible L1 with Proof-of-Liquidity consensus (mainnet launched early 2025)
- **Why:** One of the most anticipated chain launches of 2025, with a massive engaged community. EVM-compatible — easy to implement via Nethereum
- **OASIS fit:** DeFi-adjacent avatars and holons; would attract the Berachain community to OASIS

#### 4. Farcaster
- **What:** Decentralised social protocol built on Ethereum (~500k+ active users, growing fast)
- **Why:** Unlike ActivityPub, Farcaster is crypto-native and its users are exactly the Web3 audience OASIS targets. Farcaster IDs (FIDs) map naturally to OASIS avatars. SDK: `farcaster-dotnet` or direct Hubble gRPC API
- **OASIS fit:** Social identity layer; avatar FID = OASIS avatar; cast/channel data as holons

#### 5. Nostr
- **What:** Open decentralised social protocol (not blockchain-based — uses key pairs and relays)
- **Why:** Rapidly growing, especially among Bitcoin and privacy communities. npub keys map cleanly to OASIS avatar keys. No gas, no chain — extremely low barrier to integrate
- **OASIS fit:** Avatar public key = Nostr npub; notes/events as holons on Nostr relays

---

### Tier 2 — High Value, Implement Next

#### 6. Lens Protocol
- **What:** Decentralised social graph on Polygon (v2 now on Polygon zkEVM)
- **Why:** Largest on-chain social graph with 100k+ profiles. Profile NFTs map perfectly to OASIS avatars. API via GraphQL
- **OASIS fit:** Avatar = Lens profile; posts, follows, mirrors as holons

#### 7. World ID / Worldcoin
- **What:** Proof-of-humanity identity system (iris scan → zero-knowledge proof of unique personhood)
- **Why:** Solves the avatar uniqueness problem — verifying one person = one avatar — which is central to OASIS's vision of a single universal identity. Widely integrated in 2025
- **OASIS fit:** Avatar verification; one-person-one-avatar enforcement; Sybil resistance for karma/achievement systems

#### 8. Story Protocol
- **What:** L1 blockchain for programmable IP — register, license, and monetise intellectual property on-chain
- **Why:** OASIS's creator economy, OAPPs, and NFT holons all involve IP. Story Protocol provides the infrastructure to make OASIS-native content legally and programmatically licensed
- **OASIS fit:** OAPP IP registration; creative holon licensing; royalty distribution to avatar creators

#### 9. Sei
- **What:** High-performance L1 optimised for trading and real-time apps (400ms finality)
- **Why:** Fastest-finalising chain for latency-sensitive OASIS interactions; large gaming and DeFi community. EVM-compatible (v2)
- **OASIS fit:** Real-time avatar state updates; high-frequency holon writes in games

#### 10. Celestia
- **What:** Modular data availability (DA) layer — not an execution chain
- **Why:** Many of the newer L2s in this list (Abstract, Eclipse, etc.) use Celestia for DA. A CelestiaOASIS DA provider lets OASIS data be published to the DA layer directly, giving any rollup that uses Celestia access to OASIS holons
- **OASIS fit:** DA provider for OASIS holon data blobs; underpins multi-rollup storage strategy

---

### Tier 3 — Strategic / Niche but High OASIS Alignment

#### 11. Filecoin
- **What:** Incentivised decentralised storage (separate from IPFS — adds economic guarantees)
- **Why:** IPFSOASIS already exists but has no storage guarantees. FilecoinOASIS adds miner incentives and storage deals so data is provably kept long-term. SDK: `Glif.io` API or direct Lotus JSON-RPC
- **OASIS fit:** Long-term avatar archive deals; complements IPFSOASIS and Arweave

#### 12. Lit Protocol
- **What:** Decentralised access control and threshold encryption
- **Why:** Lets OASIS control who can decrypt which avatar/holon data without a centralised server. "Only avatars with karma ≥ 1000 can read this holon" becomes a Lit condition. REST API + JS SDK (call via HttpClient)
- **OASIS fit:** Privacy-preserving holons; avatar-gated content; OAPP access control

#### 13. Ceramic / ComposeDB
- **What:** Decentralised data streaming and composable data network (IPFS-based)
- **Why:** ComposeDB is purpose-built for portable user data — almost identical to OASIS's holon concept. Ceramic DIDs map to OASIS avatar identities. REST + GraphQL API
- **OASIS fit:** Avatar DID document storage; composable holon schemas shared across apps

#### 14. The Graph
- **What:** Blockchain indexing and querying protocol (GraphQL subgraphs)
- **Why:** Not a storage provider but an indexing provider. A GraphOASIS or TheGraphOASIS provider would let OASIS query data from all existing on-chain providers through a unified GraphQL interface, massively speeding up reads
- **OASIS fit:** Fast cross-chain avatar/holon queries; replaces bespoke RPC scanning

#### 15. Eclipse
- **What:** Solana VM (SVM) running on Ethereum settlement with Celestia DA
- **Why:** Bridges Solana's performance with Ethereum's security. Would let OASIS Solana-compatible OAPPs run with Ethereum-level settlement guarantees
- **OASIS fit:** High-performance OAPP execution with Ethereum finality

#### 16. Discord
- **What:** Social platform (800M+ users) — REST API for bots, slash commands, webhooks
- **Why:** The largest community platform for gaming and crypto. A DiscordOASIS network provider (like TelegramOASIS) lets avatars interact through Discord: send messages, award karma, trigger OAPP events. REST API is mature and well-documented
- **OASIS fit:** Network provider (not storage); community avatar interactions; karma events via bot

---

## Summary Table

| # | Provider | Category | Effort | Reach | Priority |
|---|---|---|---|---|---|
| 1 | Arweave | Storage | Low (REST API) | High | **NOW** |
| 2 | Abstract | EVM L2 | Very Low (extends Web3Core) | Very High | **NOW** |
| 3 | Berachain | EVM L1 | Very Low (extends Web3Core) | Very High | **NOW** |
| 4 | Farcaster | Social | Medium (gRPC/HTTP) | High | Soon |
| 5 | Nostr | Social/Identity | Low (WebSocket relays) | High | Soon |
| 6 | Lens Protocol | Social Graph | Low (GraphQL) | Medium | Soon |
| 7 | World ID | Identity | Medium (ZK proofs) | High | Soon |
| 8 | Story Protocol | IP / NFT | Medium | Medium | Next quarter |
| 9 | Sei | EVM L1 | Very Low (extends Web3Core) | Medium | Next quarter |
| 10 | Celestia | DA Layer | Medium | Medium | Next quarter |
| 11 | Filecoin | Storage | Medium (Lotus RPC) | Medium | Backlog |
| 12 | Lit Protocol | Encryption | Medium (REST) | Medium | Backlog |
| 13 | Ceramic/ComposeDB | Data | Medium (GraphQL) | Medium | Backlog |
| 14 | The Graph | Indexing | Low (GraphQL) | High | Backlog |
| 15 | Eclipse | SVM L2 | Low (Solana-compatible) | Low-Medium | Backlog |
| 16 | Discord | Social/Network | Low (REST bot) | Very High | Backlog |

---

## Implementation Notes

**Very Low effort (EVM extensions):** Abstract, Berachain, Sei all extend `Web3CoreOASISBaseProvider` — they need a new `.csproj`, a constructor with the correct RPC URL and chain ID, and any chain-specific differences in gas/token naming. Roughly 200 lines each.

**Low effort (REST/GraphQL):** Arweave (REST to `arweave.net`), Nostr (WebSocket to public relays), Lens (GraphQL to `api.lens.dev`), The Graph (GraphQL). These are self-contained HTTP/WebSocket clients with no binary serialisation complexity.

**Medium effort:** Farcaster (Hubble gRPC API + Farcaster-specific data model), World ID (ZK proof verification flow), Celestia (DA blob submission), Filecoin (storage deal lifecycle), Lit Protocol (threshold encryption key shares).
