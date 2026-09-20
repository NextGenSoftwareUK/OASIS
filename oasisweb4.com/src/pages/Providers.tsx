import { Box, Card, CardContent, CardHeader, Chip, Grid, Typography } from '@mui/material';

const providers: Record<string, string[]> = {
  'Blockchain / L1 / L2': [
    'Ethereum', 'Bitcoin', 'Solana', 'BNB Chain', 'Polygon', 'Arbitrum', 'Optimism', 'Avalanche',
    'Base', 'Cardano', 'Polkadot', 'NEAR', 'Cosmos', 'TRON', 'XRP Ledger', 'EOSIO', 'Sui',
    'Aptos', 'Hedera Hashgraph', 'MultiversX (Elrond)', 'Fantom', 'zkSync', 'Scroll', 'Linea',
    'Rootstock (RSK)', 'Telos', 'Stacks (BlockStack)', 'Zcash', 'Miden', 'Aztec', 'Starknet',
    'Radix', 'TON', 'Stellar', 'Monad', 'ChainLink', 'Loom', 'Algorand', 'Filecoin',
    'Ceramic / ComposeDB', 'Basechain', 'Abstract', 'Berachain',
    'Axelar (cross-chain)', 'Wormhole (bridge)', 'Fhenix (FHE L2)', 'Web3Core (EVM universal)',
    'Chainflip (native cross-chain swap)', 'deBridge (cross-chain liquidity)',
    'Stargate (LayerZero bridge)', 'Synapse (cross-chain bridge)',
    'LayerZero V2 (omnichain messaging)', 'Hyperlane (permissionless interoperability)', 'Connext (modular cross-chain)',
    'EigenLayer (restaking protocol)', 'Espresso Systems (shared sequencer)',
  ],
  'Storage / Database / Cloud': [
    'MongoDB', 'Neo4j', 'SQL Server', 'Oracle DB', 'SQLite', 'Local File',
    'IPFS', 'Pinata (IPFS pinning)', 'Arweave (permanent)', 'SOLID (Tim Berners-Lee)',
    'ThreeFold', 'Azure Blob Storage', 'Azure Cosmos DB', 'AWS', 'Google Cloud',
    'Moralis (Web3 data API)', 'Tableland (on-chain SQL)',
    'PostgreSQL', 'Firebase', 'Supabase', 'Cloudflare Workers / KV', 'Cloudflare D1',
    'PocketBase', 'Turso', 'Appwrite', 'PlanetScale', 'OrbitDB', 'GUN', 'CockroachDB',
    'Neon', 'SurrealDB', 'RavenDB', 'Cassandra', 'Convex', 'Fauna',
    'Qdrant', 'Weaviate', 'InfluxDB', 'TimescaleDB', 'Elasticsearch', 'DynamoDB',
    'Couchbase', 'Upstash', 'ScyllaDB', 'Litestream', 'MinIO', 'Cloudinary',
    'PouchDB', 'Meilisearch', 'Typesense', 'ClickHouse', 'Redis', 'Memcached',
    'KeyDB', 'OpenSearch', 'Algolia', 'Solr', 'ArangoDB', 'QuestDB', 'CouchDB',
    'Xata', 'Pinecone', 'Milvus', 'Chroma', 'LanceDB', 'pgvector', 'Marqo', 'Zilliz',
    'ArcadeDB', 'DuckDB', 'MotherDuck', 'Snowflake', 'BigQuery', 'Redshift', 'Databricks',
    'Apache Druid', 'Apache Pinot', 'Dragonfly', 'ValKey', 'Garnet',
    'Fastly (edge CDN / KV)', 'Deno Deploy (edge functions)',
    'Vercel KV (edge key-value)', 'Netlify Blobs (edge object store)', 'Fly.io (distributed platform)',
    'Tigris (distributed S3-compatible)', 'Nile (serverless Postgres multi-tenant)',
  ],
  'Decentralised Social / Network': [
    'ActivityPub', 'Holochain', 'HoloWeb', 'Scuttlebutt', 'Urbit', 'SEEDS', 'Telegram',
    'Farcaster', 'Nostr', 'Lens Protocol', 'BlueSky (AT Protocol)', 'Matrix', 'Discord',
    'Waku (P2P messaging)', 'Livepeer (video)', 'Akash (compute)',
    'Tor / Onion', 'Orion Protocol (DEX aggregator)', 'PLAN',
    'Push Protocol', 'Celestia (DA layer)', 'Eclipse (SVM L2)',
    'Lens v2', 'Privy (embedded wallets)', 'LayerZero (cross-chain)',
    'Gitcoin Passport (identity scoring)', 'Polybase (decentralised DB)',
    'Sui zkLogin (social login)', 'ZKsync SSO (smart account SSO)',
    'Ceramic / ComposeDB (data streams)',
    'QuickNode (RPC and API infrastructure)', 'Tenderly (Web3 dev and simulation)',
    'Moralis Streams (real-time blockchain events)', 'SubQuery (blockchain data indexing)',
    'Ankr (multi-chain RPC)', 'Nansen (on-chain analytics)', 'Goldsky (real-time subgraph indexing)',
    'NATS JetStream (high-performance messaging)', 'Temporal (workflow orchestration)',
    'Dapr (distributed app runtime)', 'Intel OpenVINO (AI/ML inference)',
  ],
  'Web3 API / Indexing / RPC': [
    'The Graph (GraphQL subgraph indexing)', 'ENS (Ethereum Name Service)',
    'Alchemy', 'Infura',
  ],
  'Spatial / Gaming / AR': [
    'GO Map (Unity AR)', 'Mapbox (geospatial)', 'WRLD 3D (metaverse)', 'Cargo (NFT marketplace)',
    'Google Maps', 'HERE Maps', 'MapLibre (open-source maps)', 'Niantic Lightship (AR)',
  ],
  'Identity / Security / IP': [
    'World ID (Worldcoin ZK proof-of-humanity)',
    'Lit Protocol (threshold access control)',
    'Story Protocol (programmable IP)',
    'Civic (decentralised identity)',
    'Reclaim Protocol (zk identity proofs)',
    'Polygon ID (ZK decentralised identity)', 'zkPass (ZK data verification)', 'Holonym (ZK proof of humanity)',
    'Self Protocol (ZK identity verification)', 'Proof of Humanity (sybil-resistant registry)',
    'ENS Off-Chain Resolver (off-chain ENS names)', 'Privy Server Wallets (programmatic embedded wallets)',
  ],
  'Infrastructure / Multisig': [
    'Safe (Gnosis multisig)', 'Sei Network',
  ],
};

const total = Object.values(providers).reduce((n, arr) => n + arr.length, 0);

const categoryLabels: Record<string, string> = {
  'Blockchain / L1 / L2': '⛓️',
  'Storage / Database / Cloud': '🗄️',
  'Decentralised Social / Network': '🌐',
  'Web3 API / Indexing / RPC': '🔍',
  'Spatial / Gaming / AR': '🗺️',
  'Identity / Security / IP': '🔐',
  'Infrastructure / Multisig': '🏗️',
};

export default function Providers() {
  return (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Box display="flex" alignItems="center" gap={2} flexWrap="wrap">
          <Typography variant="h4">Supported Providers</Typography>
          <Chip label={`${total} providers`} color="primary" size="medium" />
        </Box>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          All providers are fully implemented and hot-swappable via OASIS HyperDrive.
        </Typography>
      </Grid>
      {Object.entries(providers).map(([group, items]) => (
        <Grid item xs={12} md={6} key={group}>
          <Card>
            <CardHeader
              title={`${categoryLabels[group] ?? ''} ${group}`}
              subheader={`${items.length} providers`}
            />
            <CardContent>
              <Typography color="text.secondary" variant="body2">
                {items.join(' • ')}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  );
}
