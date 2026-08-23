import { Card, CardContent, CardHeader, Grid, Typography } from '@mui/material';

const providers = {
  blockchains: [
    'Ethereum', 'Solana', 'Polygon', 'Arbitrum', 'Optimism', 'Base', 'Avalanche',
    'BNB Chain', 'Fantom', 'Cardano', 'Polkadot', 'Bitcoin', 'NEAR', 'Sui', 'Aptos',
    'Cosmos', 'EOSIO', 'Telos', 'SEEDS', 'TON', 'Rootstock (RSK)', 'Hedera Hashgraph',
    'MultiversX (Elrond)', 'TRON', 'XRP Ledger', 'Stacks (BlockStack)', 'Algorand',
    'Zcash', 'Miden', 'Aztec', 'Starknet', 'Radix', 'Monad', 'ChainLink', 'Loom (Basechain)',
    'Stellar', 'zkSync', 'Scroll', 'Linea', 'Abstract', 'Berachain', 'Telegram',
    'Story Protocol (Story Chain EVM)', 'Sei Network', 'Celestia (DA blobs)',
    'Eclipse (SVM L2)', 'Alchemy', 'Infura', 'Safe (Gnosis Multisig)',
    'Abstract (consumer gaming L2)', 'Berachain (Proof-of-Liquidity)', 'Stellar',
    'zkSync Era (ZK rollup)', 'Scroll (zkEVM)', 'Linea (ConsenSys zkEVM)', 'Monad (parallel EVM)',
  ],
  clouds: ['AWS', 'Azure Cosmos DB', 'Azure Blob Storage', 'Google Cloud', 'Oracle DB', 'SQL Server'],
  databases: ['MongoDB', 'Neo4j', 'SQLite', 'Local File'],
  storage: ['IPFS', 'Pinata (IPFS pinning)', 'Arweave (permanent)', 'ThreeFold', 'Moralis', 'Filecoin (Lotus RPC)', 'Ceramic / ComposeDB', 'Tableland (decentralised SQL)'],
  social: ['Farcaster', 'Nostr', 'BlueSky (AT Protocol)', 'Matrix', 'Discord', 'ActivityPub', 'Scuttlebutt', 'Lens Protocol', 'Push Protocol (notifications)', 'Waku (messaging)'],
  decentralised: ['Holochain', 'HoloWeb', 'Urbit', 'SOLID', 'PLAN', 'Livepeer (video)', 'Akash (cloud)', 'Tor / Onion (OnionOASIS)', 'Orion Protocol (DEX aggregator)'],
  spatial: ['GO Map (Unity AR)', 'Mapbox (geospatial)', 'WRLD 3D (metaverse)', 'Cargo (NFT marketplace)'],
  indexing: ['The Graph (GraphQL subgraph)', 'ENS (Ethereum Name Service)'],
  identity: ['World ID (Worldcoin ZK proof-of-humanity)'],
  encryption: ['Lit Protocol (threshold access control)'],
};

export default function Providers() {
  return (
    <Grid container spacing={3}>
      <Grid item xs={12}><Typography variant="h4">Supported Providers</Typography></Grid>
      {Object.entries(providers).map(([group, items]) => (
        <Grid item xs={12} md={6} key={group}>
          <Card>
            <CardHeader title={group.charAt(0).toUpperCase() + group.slice(1)} />
            <CardContent>
              <Typography color="text.secondary">{(items as string[]).join(' • ')}</Typography>
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  );
}


