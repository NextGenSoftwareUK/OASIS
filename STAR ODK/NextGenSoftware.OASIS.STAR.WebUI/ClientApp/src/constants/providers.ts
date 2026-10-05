// OASIS Provider Types - Full ProviderType enum from backend
export const OASIS_PROVIDERS = [
  { value: 'Auto', label: 'Auto (Let OASIS Choose)', description: 'OASIS will automatically select the best provider for your needs' },
  { value: 'Default', label: 'Default', description: 'Use the default OASIS provider' },
  
  // Blockchain Providers
  { value: 'EthereumOASIS', label: 'Ethereum', description: 'Ethereum blockchain - Decentralized and secure' },
  { value: 'ArbitrumOASIS', label: 'Arbitrum', description: 'Arbitrum Layer 2 - Fast and cheap transactions' },
  { value: 'AvalancheOASIS', label: 'Avalanche', description: 'Avalanche blockchain - High performance and low latency' },
  { value: 'BaseOASIS', label: 'Base', description: 'Base blockchain - Coinbase Layer 2 solution' },
  { value: 'PolygonOASIS', label: 'Polygon', description: 'Polygon network - Low-cost Ethereum scaling' },
  { value: 'SolanaOASIS', label: 'Solana', description: 'Solana blockchain - High-speed transactions' },
  { value: 'EOSIOOASIS', label: 'EOSIO', description: 'EOSIO blockchain - Enterprise-grade performance' },
  { value: 'TelosOASIS', label: 'Telos', description: 'Telos blockchain - Fast and free transactions' },
  { value: 'SEEDSOASIS', label: 'SEEDS', description: 'SEEDS blockchain - Regenerative economy platform' },
  { value: 'LoomOASIS', label: 'Loom', description: 'Loom Network - Ethereum sidechain' },
  { value: 'TONOASIS', label: 'TON', description: 'TON blockchain - Telegram Open Network' },
  { value: 'StellarOASIS', label: 'Stellar', description: 'Stellar blockchain - Fast and low-cost payments' },
  { value: 'HashgraphOASIS', label: 'Hashgraph', description: 'Hedera Hashgraph - Enterprise DLT platform' },
  { value: 'ElrondOASIS', label: 'Elrond', description: 'Elrond blockchain - High throughput and low latency' },
  { value: 'TRONOASIS', label: 'TRON', description: 'TRON blockchain - High throughput network' },
  { value: 'CosmosBlockChainOASIS', label: 'Cosmos', description: 'Cosmos blockchain - Internet of blockchains' },
  { value: 'RootstockOASIS', label: 'Rootstock', description: 'Rootstock - Bitcoin smart contracts' },
  { value: 'ChainLinkOASIS', label: 'Chainlink', description: 'Chainlink - Decentralized oracle network' },
  { value: 'CardanoOASIS', label: 'Cardano', description: 'Cardano blockchain - Research-driven development' },
  { value: 'PolkadotOASIS', label: 'Polkadot', description: 'Polkadot - Multi-chain interoperability' },
  { value: 'BitcoinOASIS', label: 'Bitcoin', description: 'Bitcoin blockchain - Digital gold standard' },
  { value: 'NEAROASIS', label: 'NEAR', description: 'NEAR Protocol - Developer-friendly blockchain' },
  { value: 'SuiOASIS', label: 'Sui', description: 'Sui blockchain - High-performance Layer 1' },
  { value: 'AptosOASIS', label: 'Aptos', description: 'Aptos blockchain - Move-based smart contracts' },
  { value: 'OptimismOASIS', label: 'Optimism', description: 'Optimism - Ethereum Layer 2 scaling' },
  { value: 'BNBChainOASIS', label: 'BNB Chain', description: 'BNB Chain - Binance Smart Chain' },
  { value: 'FantomOASIS', label: 'Fantom', description: 'Fantom blockchain - High-speed and low-cost' },
  
  // IPFS Providers
  { value: 'IPFSOASIS', label: 'IPFS', description: 'InterPlanetary File System - Distributed storage' },
  { value: 'PinataOASIS', label: 'Pinata', description: 'Pinata IPFS service - Reliable IPFS hosting' },
  
  // Holochain Providers
  { value: 'HoloOASIS', label: 'Holochain', description: 'Holochain - Agent-centric distributed computing' },
  
  // Database Providers
  { value: 'MongoDBOASIS', label: 'MongoDB', description: 'MongoDB document database - Fast and flexible' },
  { value: 'Neo4jOASIS', label: 'Neo4j', description: 'Neo4j graph database - Perfect for relationships' },
  { value: 'SQLLiteDBOASIS', label: 'SQLite', description: 'SQLite relational database - Local and lightweight' },
  { value: 'SQLServerDBOASIS', label: 'SQL Server', description: 'Microsoft SQL Server - Enterprise database' },
  { value: 'OracleDBOASIS', label: 'Oracle DB', description: 'Oracle Database - Enterprise-grade RDBMS' },
  
  // Cloud Providers
  { value: 'GoogleCloudOASIS', label: 'Google Cloud', description: 'Google Cloud Platform services' },
  { value: 'AzureStorageOASIS', label: 'Azure Storage', description: 'Microsoft Azure cloud storage' },
  { value: 'AzureCosmosDBOASIS', label: 'Azure Cosmos DB', description: 'Azure Cosmos DB - Global distributed database' },
  { value: 'AWSOASIS', label: 'AWS', description: 'Amazon Web Services cloud platform' },
  
  // Other Providers
  { value: 'UrbitOASIS', label: 'Urbit', description: 'Urbit - Personal server platform' },
  { value: 'ThreeFoldOASIS', label: 'ThreeFold', description: 'ThreeFold - Decentralized internet infrastructure' },
  { value: 'PLANOASIS', label: 'PLAN', description: 'PLAN protocol - Decentralized planning' },
  { value: 'HoloWebOASIS', label: 'Holo Web', description: 'Holo Web - Distributed web hosting' },
  { value: 'SOLIDOASIS', label: 'SOLID', description: 'SOLID - Decentralized web standards' },
  { value: 'ActivityPubOASIS', label: 'ActivityPub', description: 'ActivityPub protocol - Federated social web' },
  { value: 'ScuttlebuttOASIS', label: 'Scuttlebutt', description: 'Scuttlebutt - Offline-first social network' },
  { value: 'LocalFileOASIS', label: 'Local File', description: 'Local file system storage' },
];
