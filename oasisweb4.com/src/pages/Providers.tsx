import { Box, Card, CardContent, CardHeader, Chip, Grid, Typography } from '@mui/material';

const providers: Record<string, string[]> = {
  'Blockchain / L1 / L2': [
    'Ethereum', 'Bitcoin', 'Solana', 'BNB Chain', 'Polygon', 'Arbitrum', 'Optimism', 'Avalanche',
    'Base', 'Cardano', 'Polkadot', 'NEAR', 'Cosmos', 'TRON', 'XRP Ledger', 'EOSIO', 'Sui',
    'Aptos', 'Hedera Hashgraph', 'MultiversX (Elrond)', 'Fantom', 'zkSync', 'Scroll', 'Linea',
    'Rootstock (RSK)', 'Telos', 'Stacks (BlockStack)', 'Zcash', 'Miden', 'Aztec', 'Starknet',
    'Radix', 'TON', 'Stellar', 'Monad', 'ChainLink', 'Loom', 'Algorand', 'Filecoin',
    'Ceramic / ComposeDB', 'Basechain', 'Abstract', 'Berachain',
  ],
  'Storage / Database / Cloud': [
    'MongoDB', 'Neo4j', 'SQL Server', 'Oracle DB', 'SQLite', 'Local File',
    'IPFS', 'Pinata (IPFS pinning)', 'Arweave (permanent)', 'SOLID (Tim Berners-Lee)',
    'ThreeFold', 'Azure Blob Storage', 'Azure Cosmos DB', 'AWS', 'Google Cloud',
    'Moralis (Web3 data API)', 'Tableland (on-chain SQL)',
  ],
  'Decentralised Social / Network': [
    'ActivityPub', 'Holochain', 'HoloWeb', 'Scuttlebutt', 'Urbit', 'SEEDS', 'Telegram',
    'Farcaster', 'Nostr', 'Lens Protocol', 'BlueSky (AT Protocol)', 'Matrix', 'Discord',
    'Waku (P2P messaging)', 'Livepeer (video)', 'Akash (compute)',
    'Tor / Onion', 'Orion Protocol (DEX aggregator)', 'PLAN',
    'Push Protocol', 'Celestia (DA layer)', 'Eclipse (SVM L2)',
  ],
  'Web3 API / Indexing / RPC': [
    'The Graph (GraphQL subgraph indexing)', 'ENS (Ethereum Name Service)',
    'Alchemy', 'Infura',
  ],
  'Spatial / Gaming / AR': [
    'GO Map (Unity AR)', 'Mapbox (geospatial)', 'WRLD 3D (metaverse)', 'Cargo (NFT marketplace)',
  ],
  'Identity / Security / IP': [
    'World ID (Worldcoin ZK proof-of-humanity)',
    'Lit Protocol (threshold access control)',
    'Story Protocol (programmable IP)',
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
