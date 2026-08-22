import { Card, CardContent, CardHeader, Grid, Typography } from '@mui/material';

const providers = {
  blockchains: [
    'Ethereum', 'Solana', 'Polygon', 'Arbitrum', 'Optimism', 'Base', 'Avalanche',
    'BNB Chain', 'Fantom', 'Cardano', 'Polkadot', 'Bitcoin', 'NEAR', 'Sui', 'Aptos',
    'Cosmos', 'EOSIO', 'Telos', 'SEEDS', 'TON', 'Rootstock (RSK)', 'Telos',
    'Hedera Hashgraph', 'MultiversX (Elrond)', 'TRON', 'XRP Ledger', 'Stacks (BlockStack)',
    'Zcash', 'Miden', 'Aztec', 'Starknet', 'Radix', 'Monad', 'ChainLink', 'Loom',
    'Stellar', 'zkSync', 'Scroll', 'Linea', 'Abstract', 'Berachain', 'Telegram',
  ],
  clouds: ['AWS', 'Azure Cosmos DB', 'Azure Storage', 'Google Cloud', 'Oracle DB', 'SQL Server'],
  databases: ['MongoDB', 'Neo4j', 'SQLite', 'Local File'],
  storage: ['IPFS', 'Pinata (IPFS pinning)', 'Arweave (permanent)', 'ThreeFold', 'Moralis'],
  decentralised: ['Holochain', 'HoloWeb', 'Urbit', 'SOLID', 'ActivityPub', 'Scuttlebutt', 'Farcaster', 'Nostr', 'PLAN'],
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


