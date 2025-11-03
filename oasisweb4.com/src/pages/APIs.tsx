import { Card, CardContent, CardHeader, Grid, Typography } from '@mui/material';

const apis = [
  { name: 'AVATAR', desc: 'Centralized user data and identity across the internet.' },
  { name: 'KARMA', desc: 'Track positive actions and build digital reputation.' },
  { name: 'DATA', desc: 'Move/share data seamlessly between Web2 and Web3.' },
  { name: 'WALLET', desc: 'High-security cross-chain wallet (future fiat integration).' },
  { name: 'NFT', desc: 'Cross-chain NFTs with geo-caching for AR/gaming.' },
  { name: 'KEYS', desc: 'Secure key storage and backup.' }
];

export default function APIs() {
  return (
    <Grid container spacing={3}>
      <Grid item xs={12}><Typography variant="h4">Core WEB4 APIs</Typography></Grid>
      {apis.map(api => (
        <Grid item xs={12} md={6} key={api.name}>
          <Card>
            <CardHeader title={api.name} />
            <CardContent>
              <Typography color="text.secondary">{api.desc}</Typography>
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  );
}


