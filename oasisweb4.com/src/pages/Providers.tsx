import { Card, CardContent, CardHeader, Grid, Typography } from '@mui/material';

const providers = {
  blockchains: ['Ethereum', 'Solana', 'Polygon', 'Arbitrum', 'Base', 'Avalanche', 'BNB Chain', 'Fantom', 'Cardano', 'Polkadot', 'Bitcoin', 'NEAR', 'Sui', 'Aptos', 'Cosmos', 'EOSIO', 'Telos', 'SEEDS'],
  clouds: ['AWS', 'Azure', 'Google Cloud', 'IBM Cloud', 'Oracle Cloud'],
  databases: ['MongoDB', 'PostgreSQL', 'MySQL', 'Redis', 'Neo4j'],
  storage: ['IPFS', 'Filecoin', 'Arweave', 'AWS S3'],
  other: ['Holochain', 'SOLID', 'ActivityPub', 'XMPP']
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


