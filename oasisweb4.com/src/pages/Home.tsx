import { Box, Button, Grid, Typography, Card, CardContent, CardActions } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { AccountBalanceWallet, Code, Cloud, Security } from '@mui/icons-material';

export default function Home() {
  return (
    <Box>
      <Typography variant="h2" gutterBottom sx={{ fontWeight: 'bold', background: 'linear-gradient(45deg, #2196F3 30%, #21CBF3 90%)', backgroundClip: 'text', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent' }}>
        Build on WEB4 OASIS
      </Typography>
      <Typography variant="h5" color="text.secondary" gutterBottom sx={{ mb: 4 }}>
        Unified data, identity, wallets, and gamification APIs to power apps, games, and platforms.
      </Typography>
      <Box sx={{ mt: 3, mb: 6 }}>
        <Button variant="contained" size="large" component={RouterLink} to="/plans" sx={{ mr: 2 }}>
          View Plans
        </Button>
        <Button variant="outlined" size="large" component={RouterLink} to="/providers">
          See Providers
        </Button>
      </Box>
      
      <Grid container spacing={4} sx={{ mb: 6 }}>
        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <AccountBalanceWallet sx={{ fontSize: 40, color: 'primary.main', mb: 2 }} />
              <Typography variant="h5" gutterBottom>Universal Wallet</Typography>
              <Typography color="text.secondary">
                Manage all your Web2 and Web3 assets across 50+ blockchain networks with one unified interface.
              </Typography>
            </CardContent>
            <CardActions>
              <Button component={RouterLink} to="/apis">Learn More</Button>
            </CardActions>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Code sx={{ fontSize: 40, color: 'primary.main', mb: 2 }} />
              <Typography variant="h5" gutterBottom>Developer APIs</Typography>
              <Typography color="text.secondary">
                AVATAR, KARMA, DATA, WALLET, NFT, and KEYS APIs with 100% uptime and auto-failover.
              </Typography>
            </CardContent>
            <CardActions>
              <Button component={RouterLink} to="/apis">Explore APIs</Button>
            </CardActions>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Cloud sx={{ fontSize: 40, color: 'primary.main', mb: 2 }} />
              <Typography variant="h5" gutterBottom>Multi-Provider</Typography>
              <Typography color="text.secondary">
                Connect to any Web2 or Web3 provider with intelligent routing and auto-failover.
              </Typography>
            </CardContent>
            <CardActions>
              <Button component={RouterLink} to="/providers">View Providers</Button>
            </CardActions>
          </Card>
        </Grid>
      </Grid>

      <Box sx={{ textAlign: 'center', py: 4, bgcolor: 'grey.50', borderRadius: 2 }}>
        <Typography variant="h4" gutterBottom>Ready to get started?</Typography>
        <Typography variant="h6" color="text.secondary" sx={{ mb: 3 }}>
          Choose your plan and start building with OASIS Web4 today.
        </Typography>
        <Button variant="contained" size="large" component={RouterLink} to="/plans">
          Get Started Now
        </Button>
      </Box>
    </Box>
  );
}


