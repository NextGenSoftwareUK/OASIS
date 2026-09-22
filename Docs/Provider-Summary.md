# OASIS Provider Summary

_227 unique providers across 9 categories — last updated: 2026-09-22 (category audit)_

> Provider count is derived directly from the `Providers/` directory tree.
> Duplicate folder copies (Ceramic in Blockchain, MoralisDBOASIS in Blockchain, Arweave in Network) have been removed — each provider now lives in exactly one canonical folder.

---

## Category Breakdown

`ProviderCategory` enum value shown for each folder. The primary category is the most specific semantic label; providers that also implement `IOASISStorageProvider` or `IOASISNETProvider` are automatically included in storage/network queries regardless of primary category.

| Folder | `ProviderCategory` | Providers |
|---|---|---|
| AI | `AI` (Bittensor, Ritual) · `EVMBlockchain` (Galadriel) | 3 |
| Blockchain | `EVMBlockchain` (34 EVM chains) · `Blockchain` (24 non-EVM) | 58 |
| Cloud | `Cloud` | 16 |
| Identity | `Identity` | 7 |
| Maps | `Map` (most) · `Spatial` (Decentraland, Ready Player Me, The Sandbox) | 10 |
| Network | `Network` | 48 |
| Other | `Storage` (GUN, OrbitDB) · `Network` (Dapr, PLAN, Temporal) · `Application` (SEEDS, Urbit) · `AI` (IntelOpenVINO) · stubs (Cargo, ONION, Orion) | 11 |
| Social | `Social` | 12 |
| Storage | `Storage` (most) · `StorageLocal` (LocalFile, SQLite, DuckDB) | 65 |

**Total unique providers: 227**

---

## Full Provider List

### AI / Decentralised AI (3) — `AI` / `EVMBlockchain`

These are AI-network blockchain adapters that expose network data as holons. They are distinct from WEB6 AI inference providers (OpenAI, Anthropic, etc.).

| Provider | Package | Category | Description |
|---|---|---|---|
| Bittensor | `NextGenSoftware.OASIS.API.Providers.BittensorOASIS` | `AI` | Bittensor decentralised AI network — reads account/subnet data as holons |
| Galadriel | `NextGenSoftware.OASIS.API.Providers.GaladrielOASIS` | `EVMBlockchain` | Galadriel EVM chain where smart contracts can call AI models on-chain |
| Ritual | `NextGenSoftware.OASIS.API.Providers.RitualOASIS` | `AI` | Ritual Infernet — submits decentralised AI compute jobs, reads results as holons |

---

### Blockchain / L1 / L2 / Cross-chain (58) — `EVMBlockchain` / `Blockchain`

Includes L1/L2 chains, cross-chain bridges, omnichain messaging, and on-chain tooling.

**EVM-compatible (`EVMBlockchain`, 34):** Abstract, Arbitrum, Avalanche, Base, Basechain, Berachain, BNB Chain, ChainLink, Chainlink Functions, Connext, deBridge, EigenLayer, Espresso Systems, Ethereum, Fantom, Fhenix, Gelato Network, Hyperlane, LayerZero V2, Linea, Monad, OpenZeppelin Defender, Optimism, Polygon, Rootstock, Scroll, Sei, Stargate, Synapse, Telos, TRON, Web3Core, Wormhole, zkSync

**Non-EVM (`Blockchain`, 24):** Algorand, Aptos, Axelar, Aztec, Bitcoin, Stacks, Cardano, Chainflip, Cosmos, EOS, MultiversX, Filecoin, Hashgraph, Miden, NEAR, Polkadot, Radix, Solana, Starknet, Stellar, Sui, TON, XRP, Zcash

| Provider | Package | Description |
|---|---|---|
| Abstract | `NextGenSoftware.OASIS.API.Providers.AbstractOASIS` | Abstract L2 (ZK, consumer crypto) |
| Algorand | `NextGenSoftware.OASIS.API.Providers.AlgorandOASIS` | Algorand L1 |
| Aptos | `NextGenSoftware.OASIS.API.Providers.AptosOASIS` | Aptos L1 |
| Arbitrum | `NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS` | Arbitrum One L2 |
| Avalanche | `NextGenSoftware.OASIS.API.Providers.AvalancheOASIS` | Avalanche C-Chain |
| Axelar | `NextGenSoftware.OASIS.API.Providers.AxelarOASIS` | Axelar cross-chain messaging |
| Aztec | `NextGenSoftware.OASIS.API.Providers.AztecOASIS` | Aztec Network private ZK rollup |
| BNB Chain | `NextGenSoftware.OASIS.API.Providers.BNBChainOASIS` | BNB Smart Chain (BSC) |
| Base | `NextGenSoftware.OASIS.API.Providers.BaseOASIS` | Base L2 (Coinbase / OP Stack) |
| Basechain | `NextGenSoftware.OASIS.API.Providers.BasechainOASIS` | Basechain (Loom Network EVM sidechain) |
| Berachain | `NextGenSoftware.OASIS.API.Providers.BerachainOASIS` | Berachain L1 (Proof-of-Liquidity) |
| Bitcoin | `NextGenSoftware.OASIS.API.Providers.BitcoinOASIS` | Bitcoin L1 |
| Stacks (BlockStack) | `NextGenSoftware.OASIS.API.Providers.BlockStackOASIS` | Stacks Bitcoin L2 |
| Cardano | `NextGenSoftware.OASIS.API.Providers.CardanoOASIS` | Cardano L1 |
| ChainLink | `NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS` | Chainlink decentralised oracle network |
| Chainflip | `NextGenSoftware.OASIS.API.Providers.ChainflipOASIS` | Chainflip native cross-chain DEX |
| Chainlink Functions | `NextGenSoftware.OASIS.API.Providers.ChainlinkFunctionsOASIS` | Chainlink Functions serverless on-chain compute |
| Connext | `NextGenSoftware.OASIS.API.Providers.ConnextOASIS` | Connext modular cross-chain execution |
| Cosmos | `NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS` | Cosmos Hub (IBC) |
| deBridge | `NextGenSoftware.OASIS.API.Providers.deBridgeOASIS` | deBridge cross-chain liquidity protocol |
| EigenLayer | `NextGenSoftware.OASIS.API.Providers.EigenLayerOASIS` | EigenLayer restaking protocol |
| EOS | `NextGenSoftware.OASIS.API.Providers.EOSIOOASIS` | EOSIO / EOS L1 |
| MultiversX (Elrond) | `NextGenSoftware.OASIS.API.Providers.ElrondOASIS` | MultiversX (formerly Elrond) L1 |
| Espresso Systems | `NextGenSoftware.OASIS.API.Providers.EspressoSystemsOASIS` | Espresso Systems decentralised sequencer |
| Ethereum | `NextGenSoftware.OASIS.API.Providers.EthereumOASIS` | Ethereum mainnet + EVM |
| Fantom | `NextGenSoftware.OASIS.API.Providers.FantomOASIS` | Fantom / Sonic L1 |
| Fhenix | `NextGenSoftware.OASIS.API.Providers.FhenixOASIS` | Fhenix FHE (Fully Homomorphic Encryption) L2 |
| Filecoin | `NextGenSoftware.OASIS.API.Providers.FilecoinOASIS` | Filecoin decentralised storage chain |
| Gelato Network | `NextGenSoftware.OASIS.API.Providers.GelatoNetworkOASIS` | Gelato Network smart contract automation |
| Hedera Hashgraph | `NextGenSoftware.OASIS.API.Providers.HashgraphOASIS` | Hedera Hashgraph DLT |
| Hyperlane | `NextGenSoftware.OASIS.API.Providers.HyperlaneOASIS` | Hyperlane permissionless interoperability |
| LayerZero V2 | `NextGenSoftware.OASIS.API.Providers.LayerZeroV2OASIS` | LayerZero V2 omnichain messaging |
| Linea | `NextGenSoftware.OASIS.API.Providers.LineaOASIS` | Linea zkEVM L2 (Consensys) |
| Miden | `NextGenSoftware.OASIS.API.Providers.MidenOASIS` | Polygon Miden ZK rollup |
| Monad | `NextGenSoftware.OASIS.API.Providers.MonadOASIS` | Monad high-performance EVM L1 |
| NEAR | `NextGenSoftware.OASIS.API.Providers.NEAROASIS` | NEAR Protocol L1 |
| OpenZeppelin Defender | `NextGenSoftware.OASIS.API.Providers.OpenZeppelinDefenderOASIS` | OpenZeppelin Defender smart contract security |
| Optimism | `NextGenSoftware.OASIS.API.Providers.OptimismOASIS` | Optimism L2 (OP Stack) |
| Polkadot | `NextGenSoftware.OASIS.API.Providers.PolkadotOASIS` | Polkadot relay chain + parachains |
| Polygon | `NextGenSoftware.OASIS.API.Providers.PolygonOASIS` | Polygon PoS + zkEVM |
| Radix | `NextGenSoftware.OASIS.API.Providers.RadixOASIS` | Radix DLT L1 |
| Rootstock (RSK) | `NextGenSoftware.OASIS.API.Providers.RootstockOASIS` | Rootstock (RSK) Bitcoin sidechain |
| Solana | `NextGenSoftware.OASIS.API.Providers.SOLANAOASIS` | Solana L1 |
| Scroll | `NextGenSoftware.OASIS.API.Providers.ScrollOASIS` | Scroll zkEVM L2 |
| Sei Network | `NextGenSoftware.OASIS.API.Providers.SeiOASIS` | Sei Network high-performance L1 |
| Stargate | `NextGenSoftware.OASIS.API.Providers.StargateOASIS` | Stargate (LayerZero) cross-chain bridge |
| Starknet | `NextGenSoftware.OASIS.API.Providers.StarknetOASIS` | Starknet ZK rollup (StarkWare) |
| Stellar | `NextGenSoftware.OASIS.API.Providers.StellarOASIS` | Stellar L1 |
| Sui | `NextGenSoftware.OASIS.API.Providers.SuiOASIS` | Sui L1 |
| Synapse | `NextGenSoftware.OASIS.API.Providers.SynapseOASIS` | Synapse cross-chain bridge |
| TON | `NextGenSoftware.OASIS.API.Providers.TONOASIS` | The Open Network (TON) L1 |
| TRON | `NextGenSoftware.OASIS.API.Providers.TRONOASIS` | TRON L1 |
| Telos | `NextGenSoftware.OASIS.API.Providers.TelosOASIS` | Telos EVM L1 |
| Web3Core | `NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS` | Web3Core universal EVM adapter |
| Wormhole | `NextGenSoftware.OASIS.API.Providers.WormholeOASIS` | Wormhole cross-chain bridge |
| XRP Ledger | `NextGenSoftware.OASIS.API.Providers.XRPLOASIS` | XRP Ledger L1 |
| Zcash | `NextGenSoftware.OASIS.API.Providers.ZcashOASIS` | Zcash privacy chain |
| zkSync | `NextGenSoftware.OASIS.API.Providers.ZkSyncOASIS` | zkSync Era L2 |

---

### Cloud / Edge / Serverless (16) — `Cloud`

| Provider | Package | Description |
|---|---|---|
| AWS | `NextGenSoftware.OASIS.API.Providers.AWSOASIS` | Amazon Web Services (S3, Lambda, etc.) |
| Appwrite | `NextGenSoftware.OASIS.API.Providers.AppwriteOASIS` | Appwrite open-source backend |
| Azure Cosmos DB | `NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS` | Azure Cosmos DB globally-distributed NoSQL |
| Azure Storage | `NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS` | Azure Blob / Table / Queue Storage |
| Cloudflare D1 | `NextGenSoftware.OASIS.API.Providers.CloudflareD1OASIS` | Cloudflare D1 serverless SQLite at the edge |
| Cloudflare Workers / KV | `NextGenSoftware.OASIS.API.Providers.CloudflareOASIS` | Cloudflare Workers + KV edge compute |
| Convex | `NextGenSoftware.OASIS.API.Providers.ConvexOASIS` | Convex reactive backend-as-a-service |
| DynamoDB | `NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS` | Amazon DynamoDB managed NoSQL |
| Fauna | `NextGenSoftware.OASIS.API.Providers.FaunaOASIS` | Fauna serverless document/relational DB |
| Firebase | `NextGenSoftware.OASIS.API.Providers.FirebaseOASIS` | Google Firebase real-time database + auth |
| Google Cloud | `NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS` | Google Cloud Platform |
| Neon | `NextGenSoftware.OASIS.API.Providers.NeonOASIS` | Neon serverless Postgres |
| PocketBase | `NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS` | PocketBase open-source BaaS |
| Supabase | `NextGenSoftware.OASIS.API.Providers.SupabaseOASIS` | Supabase open-source Firebase alternative |
| Turso | `NextGenSoftware.OASIS.API.Providers.TursoOASIS` | Turso edge SQLite (libSQL) |
| Upstash | `NextGenSoftware.OASIS.API.Providers.UpstashOASIS` | Upstash serverless Redis + Kafka |

---

### Identity / Privacy / IP (7) — `Identity`

> Additional identity-adjacent providers are in the Network and Social folders (World ID, Lit Protocol, Civic, Reclaim Protocol, etc.)

| Provider | Package | Description |
|---|---|---|
| ENS Offchain | `NextGenSoftware.OASIS.API.Providers.ENSOffchainOASIS` | ENS off-chain resolver |
| Holonym | `NextGenSoftware.OASIS.API.Providers.HolonymOASIS` | Holonym ZK proof of humanity |
| Polygon ID | `NextGenSoftware.OASIS.API.Providers.PolygonIDOASIS` | Polygon ID ZK decentralised identity |
| Privy Server Wallets | `NextGenSoftware.OASIS.API.Providers.PrivyServerWalletsOASIS` | Privy server-side programmatic wallets |
| Proof of Humanity | `NextGenSoftware.OASIS.API.Providers.ProofOfHumanityOASIS` | Proof of Humanity sybil-resistance registry |
| Self Protocol | `NextGenSoftware.OASIS.API.Providers.SelfProtocolOASIS` | Self Protocol ZK identity verification |
| zkPass | `NextGenSoftware.OASIS.API.Providers.zkPassOASIS` | zkPass ZK data verification |

---

### Maps / Spatial / AR / Gaming (10) — `Map` / `Spatial`

| Provider | Package | Description |
|---|---|---|
| Decentraland | `NextGenSoftware.OASIS.API.Providers.DecentralandOASIS` | Decentraland metaverse |
| GO Map | `NextGenSoftware.OASIS.API.Providers.GOMapOASIS` | GO Map Unity AR game framework |
| Google Maps | `NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS` | Google Maps Platform |
| HERE Maps | `NextGenSoftware.OASIS.API.Providers.HEREMapsOASIS` | HERE Maps navigation & location |
| MapLibre | `NextGenSoftware.OASIS.API.Providers.MapLibreOASIS` | MapLibre open-source maps |
| Mapbox | `NextGenSoftware.OASIS.API.Providers.MapboxOASIS` | Mapbox mapping & geospatial |
| Niantic Lightship | `NextGenSoftware.OASIS.API.Providers.NianticLightshipOASIS` | Niantic Lightship AR platform |
| Ready Player Me | `NextGenSoftware.OASIS.API.Providers.ReadyPlayerMeOASIS` | Ready Player Me cross-platform avatars |
| The Sandbox | `NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS` | The Sandbox voxel gaming metaverse |
| WRLD 3D | `NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS` | WRLD 3D geospatial metaverse platform |

---

### Network / Web3 API (48) — `Network`

Broadly scoped: P2P protocols, Web3 RPC/indexing infrastructure, identity scoring, messaging, and social networks not in the dedicated Social folder.

| Provider | Package | Description |
|---|---|---|
| ActivityPub | `NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS` | ActivityPub / Mastodon federation protocol |
| Akash | `NextGenSoftware.OASIS.API.Providers.AkashOASIS` | Akash decentralised cloud compute |
| Alchemy | `NextGenSoftware.OASIS.API.Providers.AlchemyOASIS` | Alchemy Web3 developer platform |
| Ankr | `NextGenSoftware.OASIS.API.Providers.AnkrOASIS` | Ankr multi-chain RPC & staking |
| Arweave | `NextGenSoftware.OASIS.API.Providers.ArweaveOASIS` | Arweave permanent storage |
| Blockscout | `NextGenSoftware.OASIS.API.Providers.BlockscoutOASIS` | Blockscout open-source block explorer API |
| Celestia | `NextGenSoftware.OASIS.API.Providers.CelestiaOASIS` | Celestia modular data availability layer |
| Ceramic / ComposeDB | `NextGenSoftware.OASIS.API.Providers.CeramicOASIS` | Ceramic / ComposeDB data streams |
| Civic | `NextGenSoftware.OASIS.API.Providers.CivicOASIS` | Civic identity & KYC |
| Covalent | `NextGenSoftware.OASIS.API.Providers.CovalentOASIS` | Covalent unified multi-chain data API |
| Dune Analytics | `NextGenSoftware.OASIS.API.Providers.DuneAnalyticsOASIS` | Dune Analytics on-chain SQL queries |
| ENS | `NextGenSoftware.OASIS.API.Providers.ENSOASIS` | Ethereum Name Service |
| Eclipse | `NextGenSoftware.OASIS.API.Providers.EclipseOASIS` | Eclipse SVM L2 (Solana VM on Ethereum) |
| Gitcoin Passport | `NextGenSoftware.OASIS.API.Providers.GitcoinPassportOASIS` | Gitcoin Passport decentralised identity scoring |
| Goldsky | `NextGenSoftware.OASIS.API.Providers.GoldskyOASIS` | Goldsky real-time subgraph indexing |
| Holochain | `NextGenSoftware.OASIS.API.Providers.HoloOASIS` | Holochain / HoloNET distributed P2P |
| Holochain Desktop | `NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop` | HoloNET desktop runtime |
| Holochain Unity | `NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity` | HoloNET Unity integration |
| HoloWeb | `NextGenSoftware.OASIS.API.Providers.HoloWeb` | HoloWeb browser extension |
| HoloWebOASIS | `NextGenSoftware.OASIS.API.Providers.HoloWebOASIS` | HoloWeb OASIS provider wrapper |
| IPFS | `NextGenSoftware.OASIS.API.Providers.IPFSOASIS` | IPFS distributed file system |
| Infura | `NextGenSoftware.OASIS.API.Providers.InfuraOASIS` | Infura Ethereum / IPFS infrastructure |
| LayerZero | `NextGenSoftware.OASIS.API.Providers.LayerZeroOASIS` | LayerZero V1 omnichain messaging |
| Livepeer | `NextGenSoftware.OASIS.API.Providers.LivepeerOASIS` | Livepeer decentralised video transcoding |
| Moralis | `NextGenSoftware.OASIS.API.Providers.MoralisOASIS` | Moralis Web3 data API |
| Moralis Streams | `NextGenSoftware.OASIS.API.Providers.MoralisStreamsOASIS` | Moralis Streams real-time blockchain events |
| NATS JetStream | `NextGenSoftware.OASIS.API.Providers.NATSJetStreamOASIS` | NATS JetStream high-performance messaging |
| Nansen | `NextGenSoftware.OASIS.API.Providers.NansenOASIS` | Nansen on-chain analytics & wallet labels |
| Pinata | `NextGenSoftware.OASIS.API.Providers.PinataOASIS` | Pinata IPFS pinning service |
| Polybase | `NextGenSoftware.OASIS.API.Providers.PolybaseOASIS` | Polybase decentralised database |
| Privy | `NextGenSoftware.OASIS.API.Providers.PrivyOASIS` | Privy embedded wallet & auth |
| Push Protocol | `NextGenSoftware.OASIS.API.Providers.PushProtocolOASIS` | Push Protocol (EPNS) Web3 notifications |
| QuickNode | `NextGenSoftware.OASIS.API.Providers.QuickNodeOASIS` | QuickNode Web3 RPC infrastructure |
| Reclaim Protocol | `NextGenSoftware.OASIS.API.Providers.ReclaimProtocolOASIS` | Reclaim Protocol ZK proofs of web data |
| Reservoir | `NextGenSoftware.OASIS.API.Providers.ReservoirOASIS` | Reservoir NFT data & trading API |
| SOLID | `NextGenSoftware.OASIS.API.Providers.SOLIDOASIS` | SOLID (Social Linked Data) personal data pods |
| Safe (Gnosis) | `NextGenSoftware.OASIS.API.Providers.SafeOASIS` | Safe (Gnosis) multi-sig smart accounts |
| Scuttlebutt | `NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS` | Secure Scuttlebutt (SSB) P2P protocol |
| SubQuery | `NextGenSoftware.OASIS.API.Providers.SubqueryOASIS` | SubQuery decentralised blockchain indexer |
| Sui zkLogin | `NextGenSoftware.OASIS.API.Providers.SuiZkLoginOASIS` | Sui zkLogin social-login to blockchain |
| Tableland | `NextGenSoftware.OASIS.API.Providers.TablelandOASIS` | Tableland permissionless on-chain SQL |
| Telegram | `NextGenSoftware.OASIS.API.Providers.TelegramOASIS` | Telegram messaging platform |
| Tenderly | `NextGenSoftware.OASIS.API.Providers.TenderlyOASIS` | Tenderly smart contract dev & simulation |
| ThreeFold | `NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS` | ThreeFold decentralised cloud grid |
| Waku | `NextGenSoftware.OASIS.API.Providers.WakuOASIS` | Waku decentralised P2P messaging |
| Zapper | `NextGenSoftware.OASIS.API.Providers.ZapperOASIS` | Zapper DeFi portfolio tracker |
| ZkSync SSO | `NextGenSoftware.OASIS.API.Providers.ZkSyncSSOOASIS` | ZkSync SSO smart account single sign-on |

---

### Other / Infrastructure (11) — mixed

| Provider | Package | Category | Description |
|---|---|---|---|
| Cargo | `NextGenSoftware.OASIS.API.Providers.CargoOASIS` | *(stub — no provider class yet)* | Cargo NFT minting & marketplace |
| Dapr | `NextGenSoftware.OASIS.API.Providers.DaprOASIS` | `Network` | Dapr distributed application runtime |
| GUN | `NextGenSoftware.OASIS.API.Providers.GUNOASIS` | `Storage` | GUN decentralised graph database |
| Intel OpenVINO | `NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS` | `AI` | Intel OpenVINO AI/ML inference toolkit |
| ONION Protocol | `NextGenSoftware.OASIS.API.Providers.ONION-Protocol` | *(stub — no provider class yet)* | Tor / Onion routing privacy network |
| OrbitDB | `NextGenSoftware.OASIS.API.Providers.OrbitDBOASIS` | `Storage` | OrbitDB peer-to-peer database (IPFS-based) |
| Orion Protocol | `NextGenSoftware.OASIS.API.Providers.OrionProtocolOASIS` | *(stub — no ProviderCategory set)* | Orion Protocol DEX aggregator |
| PLAN | `NextGenSoftware.OASIS.API.Providers.PLANOASIS` | `Network` | PLAN collaborative community platform |
| SEEDS | `NextGenSoftware.OASIS.API.Providers.SEEDSOASIS` | `Application` | SEEDS regenerative economy protocol |
| Temporal | `NextGenSoftware.OASIS.API.Providers.TemporalOASIS` | `Network` | Temporal workflow orchestration engine |
| Urbit | `NextGenSoftware.OASIS.API.Providers.UrbitOASIS` | `Application` | Urbit personal server OS & P2P network |

---

### Social / DAO / Identity (12) — `Social`

| Provider | Package | Description |
|---|---|---|
| BlueSky | `NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS` | BlueSky (AT Protocol) decentralised social |
| Discord | `NextGenSoftware.OASIS.API.Providers.DiscordOASIS` | Discord community & gaming platform |
| Farcaster | `NextGenSoftware.OASIS.API.Providers.FarcasterOASIS` | Farcaster decentralised social protocol |
| Lens Protocol | `NextGenSoftware.OASIS.API.Providers.LensOASIS` | Lens Protocol social graph (V1) |
| Lens Protocol V2 | `NextGenSoftware.OASIS.API.Providers.LensV2OASIS` | Lens Protocol V2 |
| Lit Protocol | `NextGenSoftware.OASIS.API.Providers.LitProtocolOASIS` | Lit Protocol threshold cryptography & access control |
| Loom | `NextGenSoftware.OASIS.API.Providers.LoomOASIS` | Loom video messaging |
| Matrix | `NextGenSoftware.OASIS.API.Providers.MatrixOASIS` | Matrix decentralised real-time messaging |
| Nostr | `NextGenSoftware.OASIS.API.Providers.NostrOASIS` | Nostr censorship-resistant social protocol |
| Story Protocol | `NextGenSoftware.OASIS.API.Providers.StoryProtocolOASIS` | Story Protocol programmable IP licensing |
| The Graph | `NextGenSoftware.OASIS.API.Providers.TheGraphOASIS` | The Graph decentralised GraphQL indexing |
| World ID | `NextGenSoftware.OASIS.API.Providers.WorldIDOASIS` | World ID (Worldcoin) ZK proof-of-personhood |

---

### Storage / Database (65) — `Storage` / `StorageLocal`

| Provider | Package | Description |
|---|---|---|
| Algolia | `NextGenSoftware.OASIS.API.Providers.AlgoliaOASIS` | Algolia hosted search-as-a-service |
| ArangoDB | `NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS` | ArangoDB multi-model (graph/doc/KV) |
| ArcadeDB | `NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS` | ArcadeDB multi-model (graph/doc/KV/time-series) |
| Arweave | `NextGenSoftware.OASIS.API.Providers.ArweaveOASIS` | Arweave permanent on-chain storage |
| BigQuery | `NextGenSoftware.OASIS.API.Providers.BigQueryOASIS` | Google BigQuery serverless analytics |
| Cassandra | `NextGenSoftware.OASIS.API.Providers.CassandraOASIS` | Apache Cassandra wide-column distributed DB |
| Chroma | `NextGenSoftware.OASIS.API.Providers.ChromaOASIS` | Chroma AI-native open-source vector DB |
| ClickHouse | `NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS` | ClickHouse columnar OLAP analytics |
| Cloudinary | `NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS` | Cloudinary cloud media management |
| CockroachDB | `NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS` | CockroachDB distributed SQL |
| CouchDB | `NextGenSoftware.OASIS.API.Providers.CouchDBOASIS` | Apache CouchDB document database |
| Couchbase | `NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS` | Couchbase NoSQL + full-text search |
| Databricks | `NextGenSoftware.OASIS.API.Providers.DatabricksOASIS` | Databricks unified data + AI platform |
| Deno Deploy | `NextGenSoftware.OASIS.API.Providers.DenoDeployOASIS` | Deno Deploy edge serverless functions |
| Dragonfly | `NextGenSoftware.OASIS.API.Providers.DragonflyOASIS` | Dragonfly high-performance Redis-compatible cache |
| Apache Druid | `NextGenSoftware.OASIS.API.Providers.DruidOASIS` | Apache Druid real-time analytics OLAP |
| DuckDB | `NextGenSoftware.OASIS.API.Providers.DuckDBOASIS` | DuckDB in-process analytical SQL |
| Elasticsearch | `NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS` | Elasticsearch distributed search & analytics |
| Fastly | `NextGenSoftware.OASIS.API.Providers.FastlyOASIS` | Fastly CDN edge compute & KV store |
| Fly.io | `NextGenSoftware.OASIS.API.Providers.FlyIOOASIS` | Fly.io globally distributed app platform |
| Garnet | `NextGenSoftware.OASIS.API.Providers.GarnetOASIS` | Garnet (Microsoft) Redis-compatible cache |
| InfluxDB | `NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS` | InfluxDB purpose-built time-series DB |
| KeyDB | `NextGenSoftware.OASIS.API.Providers.KeyDBOASIS` | KeyDB multi-threaded Redis fork |
| LanceDB | `NextGenSoftware.OASIS.API.Providers.LanceDBOASIS` | LanceDB multimodal AI vector database |
| Litestream | `NextGenSoftware.OASIS.API.Providers.LitestreamOASIS` | Litestream SQLite continuous replication |
| Local File | `NextGenSoftware.OASIS.API.Providers.LocalFileOASIS` | Local file system storage |
| Marqo | `NextGenSoftware.OASIS.API.Providers.MarqoOASIS` | Marqo tensor search & vector database |
| Meilisearch | `NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS` | Meilisearch fast open-source search |
| Memcached | `NextGenSoftware.OASIS.API.Providers.MemcachedOASIS` | Memcached distributed memory cache |
| Milvus | `NextGenSoftware.OASIS.API.Providers.MilvusOASIS` | Milvus open-source vector database |
| MinIO | `NextGenSoftware.OASIS.API.Providers.MinIOOASIS` | MinIO S3-compatible object storage |
| MongoDB | `NextGenSoftware.OASIS.API.Providers.MongoOASIS` | MongoDB document database |
| MotherDuck | `NextGenSoftware.OASIS.API.Providers.MotherDuckOASIS` | MotherDuck serverless DuckDB in the cloud |
| Neo4j | `NextGenSoftware.OASIS.API.Providers.Neo4jOASIS` | Neo4j graph database |
| Neo4j Aura | `NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura` | Neo4j Aura fully managed cloud graph DB |
| Neo4j v2 | `NextGenSoftware.OASIS.API.Providers.Neo4jOASIS2` | Neo4j v2 updated driver |
| Netlify Blobs | `NextGenSoftware.OASIS.API.Providers.NetlifyBlobsOASIS` | Netlify Blobs edge object storage |
| Nile | `NextGenSoftware.OASIS.API.Providers.NileOASIS` | Nile serverless multi-tenant Postgres |
| OpenSearch | `NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS` | OpenSearch (Elasticsearch fork) |
| Oracle DB | `NextGenSoftware.OASIS.API.Providers.OracleDBOASIS` | Oracle Database |
| pgvector | `NextGenSoftware.OASIS.API.Providers.PgVectorOASIS` | pgvector Postgres vector extension |
| Pinecone | `NextGenSoftware.OASIS.API.Providers.PineconeOASIS` | Pinecone managed vector database |
| Apache Pinot | `NextGenSoftware.OASIS.API.Providers.PinotOASIS` | Apache Pinot real-time OLAP |
| PlanetScale | `NextGenSoftware.OASIS.API.Providers.PlanetScaleOASIS` | PlanetScale serverless MySQL |
| PostgreSQL | `NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS` | PostgreSQL relational database |
| PouchDB | `NextGenSoftware.OASIS.API.Providers.PouchDBOASIS` | PouchDB browser/offline CouchDB sync |
| Qdrant | `NextGenSoftware.OASIS.API.Providers.QdrantOASIS` | Qdrant high-performance vector search |
| QuestDB | `NextGenSoftware.OASIS.API.Providers.QuestDBOASIS` | QuestDB high-performance time-series SQL |
| RavenDB | `NextGenSoftware.OASIS.API.Providers.RavenDBOASIS` | RavenDB .NET-native document database |
| Redis | `NextGenSoftware.OASIS.API.Providers.RedisOASIS` | Redis in-memory data structure store |
| Redshift | `NextGenSoftware.OASIS.API.Providers.RedshiftOASIS` | Amazon Redshift cloud data warehouse |
| SQLite | `NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS` | SQLite embedded relational database |
| SQL Server | `NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS` | Microsoft SQL Server |
| ScyllaDB | `NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS` | ScyllaDB C++ Cassandra-compatible DB |
| Snowflake | `NextGenSoftware.OASIS.API.Providers.SnowflakeOASIS` | Snowflake cloud data warehouse |
| Solr | `NextGenSoftware.OASIS.API.Providers.SolrOASIS` | Apache Solr enterprise search |
| SurrealDB | `NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS` | SurrealDB multi-model cloud-native DB |
| Tigris | `NextGenSoftware.OASIS.API.Providers.TigrisOASIS` | Tigris globally distributed S3-compatible store |
| TimescaleDB | `NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS` | TimescaleDB time-series Postgres extension |
| Typesense | `NextGenSoftware.OASIS.API.Providers.TypesenseOASIS` | Typesense open-source typo-tolerant search |
| ValKey | `NextGenSoftware.OASIS.API.Providers.ValKeyOASIS` | ValKey Linux Foundation Redis fork |
| Vercel KV | `NextGenSoftware.OASIS.API.Providers.VercelKVOASIS` | Vercel KV edge key-value (Upstash Redis) |
| Weaviate | `NextGenSoftware.OASIS.API.Providers.WeaviateOASIS` | Weaviate open-source vector database |
| Xata | `NextGenSoftware.OASIS.API.Providers.XataOASIS` | Xata serverless Postgres + search |
| Zilliz | `NextGenSoftware.OASIS.API.Providers.ZillizOASIS` | Zilliz managed Milvus vector DB |

---

## Change History

> **2026-09-22 (category audit):** ProviderCategory enum assignments corrected across all providers. `Social` added to enum. 12 Social providers → `Social`. 58 Blockchain folder providers split into `EVMBlockchain` (34 EVM-compatible) and `Blockchain` (24 non-EVM) — fixes missed `new(...)` syntax providers and promotes EVM chains from generic `Blockchain`. 16 Cloud → `Cloud`. 27 Network → `Network`. Storage folder → `Storage`/`StorageLocal`. Other folder fixed (GUN/OrbitDB → `Storage`, PLAN/Dapr/Temporal → `Network`, SEEDS/Urbit → `Application`). ProviderManager updated to use interface checks for activation; `GetCloudProviders`, `GetSocialProviders`, `GetIdentityProviders`, `GetAIProviders`, `GetMapProviders`, `GetSpatialProviders` and matching `IsProvider*` helpers added. GaladrielOASIS → `EVMBlockchain`. Duplicate folder copies removed (Ceramic/Blockchain, MoralisDBOASIS/Blockchain, Arweave/Network). Total: 227 providers.
> **2026-09-21:** Added COSMOS submodule to Blockchain — updated to 220.
> **2026-09-20d (+14):** Blockchain ×3 (OpenZeppelin Defender, Gelato, Chainlink Functions), Network ×5 (Covalent, Dune Analytics, Reservoir, Blockscout, Zapper), Spatial ×3 (Ready Player Me, Decentraland, The Sandbox), AI ×3 (Bittensor, Galadriel, Ritual). Previous: 206.
> **2026-09-20c (+4):** Network ×4 (NATS JetStream, Temporal, Dapr, Intel OpenVINO). Previous: 202.
> **2026-09-20b (+9):** Blockchain ×2 (EigenLayer, Espresso Systems), Network ×3 (Ankr, Nansen, Goldsky), Storage ×2 (Tigris, Nile), Identity ×2 (ENS Offchain, Privy Server Wallets). Previous: 193.
> **2026-09-20 (+9):** Blockchain ×3 (LayerZero V2, Hyperlane, Connext), Identity ×2 (Self Protocol, Proof of Humanity), Network ×4 (QuickNode, Tenderly, Moralis Streams, SubQuery). Previous: 184.
> **2026-09-19 (+10):** Blockchain ×4 (Chainflip, deBridge, Stargate, Synapse), Storage ×3 (Vercel KV, Netlify Blobs, Fly.io), Identity ×3 (Polygon ID, zkPass, Holonym). Previous: 174.
