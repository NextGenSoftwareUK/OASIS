import React from 'react';
import {
  Box,
  Typography,
  Container,
  Grid,
  Card,
  CardContent,
  Button,
  Chip,
  Avatar,
  Stack,
  Paper,
  Divider,
} from '@mui/material';
import {
  Star,
  Rocket,
  Security,
  Speed,
  Public,
  Cloud,
  Timeline,
  TrendingUp,
  Group,
  Code,
  Science,
  Explore,
  Store,
  AccountCircle,
  Login,
  PersonAdd,
  AutoAwesome,
  Memory,
  Storage,
  Psychology,
  Build,
  Apps,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';

const HomePage: React.FC = () => {
  const navigate = useNavigate();

  const features = [
    {
      icon: <AutoAwesome sx={{ fontSize: 40, color: '#ffd700' }} />,
      title: 'OASIS HyperDrive',
      description: 'Revolutionary auto-failover system that intelligently switches between Web2 and Web3 providers for optimal performance',
    },
    {
      icon: <Rocket sx={{ fontSize: 40, color: '#ff6b35' }} />,
      title: 'Universal Data Aggregation',
      description: 'Single API that connects everything to everything - the first universal Web2/Web3 aggregation system',
    },
    {
      icon: <Security sx={{ fontSize: 40, color: '#4caf50' }} />,
      title: 'Zero-Downtime Architecture',
      description: 'Impossible to shutdown with distributed, redundant architecture and hot-swappable providers',
    },
    {
      icon: <Build sx={{ fontSize: 40, color: '#2196f3' }} />,
      title: 'STAR ODK Platform',
      description: 'Low-code metaverse development with OAPPs, missions, NFTs, and comprehensive gamification',
    },
    {
      icon: <Memory sx={{ fontSize: 40, color: '#9c27b0' }} />,
      title: 'Future-Proof Technology',
      description: 'Never learn new tech stacks again - universal API abstraction with HOT swappable plugins',
    },
    {
      icon: <Psychology sx={{ fontSize: 40, color: '#ff9800' }} />,
      title: 'Karma & Reputation System',
      description: 'Universal reputation tracking across all platforms with cross-platform contribution rewards',
    },
  ];

  const stats = [
    { label: 'OASIS Users', value: '2.5M+', color: '#00bcd4' },
    { label: 'Supported Providers', value: '50+', color: '#00acc1' },
    { label: 'OAPPs Deployed', value: '15K+', color: '#0097a7' },
    { label: 'Karma Points', value: '12.5M+', color: '#00838f' },
  ];

  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.1,
      },
    },
  };

  const itemVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
    >
      {/* Hero Section */}
      <Box
        sx={{
          background: `
            linear-gradient(180deg, 
              rgba(1, 1, 37, 0.95) 0%, 
              rgba(1, 1, 37, 0.98) 50%,
              rgba(1, 1, 37, 1) 100%
            )
          `,
          color: 'white',
          py: 12,
          position: 'relative',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: `
              radial-gradient(ellipse at center top, 
                rgba(0, 188, 212, 0.1) 0%, 
                transparent 50%
              )
            `,
            pointerEvents: 'none',
          }
        }}
      >
        <Container maxWidth="lg" sx={{ position: 'relative', zIndex: 1 }}>
          <motion.div variants={itemVariants}>
            <Box sx={{ textAlign: 'center', mb: 8 }}>
              {/* STAR Logo */}
              <Box sx={{ mb: 6, display: 'flex', justifyContent: 'center' }}>
                <motion.div
                  initial={{ scale: 0.8, opacity: 0 }}
                  animate={{ scale: 1, opacity: 1 }}
                  transition={{ duration: 0.8, ease: 'easeOut' }}
                  whileHover={{ scale: 1.05 }}
                >
                  <img 
                    src="/star-logo.png" 
                    alt="STAR Logo" 
                    style={{ 
                      maxWidth: '300px', 
                      height: 'auto',
                      filter: 'drop-shadow(0 0 20px rgba(0, 188, 212, 0.3))'
                    }}
                  />
                </motion.div>
              </Box>
              
              <Typography variant="h2" component="h1" gutterBottom sx={{ fontWeight: 'bold', mb: 3 }}>
                Welcome to OASIS
              </Typography>
              
              <Typography variant="h5" sx={{ mb: 4, opacity: 0.9, maxWidth: 800, mx: 'auto' }}>
                The Universal Web4/Web5 Infrastructure that unifies all Web2 and Web3 technologies 
                into a single, intelligent, auto-failover system. The first universal API that 
                connects everything to everything.
              </Typography>
              
              <Stack direction="row" spacing={2} justifyContent="center" sx={{ mb: 6 }}>
                <Button
                  variant="contained"
                  size="large"
                  startIcon={<Login />}
                  onClick={() => navigate('/avatar/signin')}
                  sx={{
                    bgcolor: '#00bcd4',
                    color: 'white',
                    boxShadow: '0 0 20px rgba(0, 188, 212, 0.5)',
                    '&:hover': {
                      bgcolor: '#00acc1',
                      boxShadow: '0 0 30px rgba(0, 188, 212, 0.7)',
                    },
                  }}
                >
                  Sign In
                </Button>
                <Button
                  variant="outlined"
                  size="large"
                  startIcon={<PersonAdd />}
                  onClick={() => navigate('/avatar/signup')}
                  sx={{
                    borderColor: '#00bcd4',
                    color: '#00bcd4',
                    '&:hover': {
                      borderColor: '#00acc1',
                      bgcolor: 'rgba(0, 188, 212, 0.1)',
                    },
                  }}
                >
                  Create Account
                </Button>
              </Stack>
            </Box>
          </motion.div>
        </Container>
      </Box>

      {/* Stats Section */}
      <Box sx={{ py: 8, bgcolor: 'background.default' }}>
        <Container maxWidth="lg">
          <motion.div variants={itemVariants}>
            <Typography variant="h4" align="center" gutterBottom sx={{ mb: 6, fontWeight: 'bold' }}>
              OASIS Ecosystem Metrics
            </Typography>
            
            <Grid container spacing={4}>
              {stats.map((stat, index) => (
                <Grid item xs={6} md={3} key={index}>
                  <motion.div
                    variants={itemVariants}
                    whileHover={{ scale: 1.05 }}
                  >
                    <Card sx={{ textAlign: 'center', p: 3, height: '100%' }}>
                      <CardContent>
                        <Typography variant="h3" sx={{ color: stat.color, fontWeight: 'bold', mb: 1 }}>
                          {stat.value}
                        </Typography>
                        <Typography variant="h6" color="text.secondary">
                          {stat.label}
                        </Typography>
                      </CardContent>
                    </Card>
                  </motion.div>
                </Grid>
              ))}
            </Grid>
          </motion.div>
        </Container>
      </Box>

      {/* Features Section */}
      <Box sx={{ py: 12, bgcolor: 'background.paper' }}>
        <Container maxWidth="lg">
          <motion.div variants={itemVariants}>
            <Typography variant="h4" align="center" gutterBottom sx={{ mb: 8, fontWeight: 'bold' }}>
              Revolutionary OASIS Technology
            </Typography>
            
            <Grid container spacing={4}>
              {features.map((feature, index) => (
                <Grid item xs={12} md={6} lg={4} key={index}>
                  <motion.div
                    variants={itemVariants}
                    whileHover={{ y: -5 }}
                    transition={{ duration: 0.2 }}
                  >
                    <Card sx={{ height: '100%', p: 3, textAlign: 'center' }}>
                      <CardContent>
                        <Box sx={{ mb: 3 }}>
                          {feature.icon}
                        </Box>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>
                          {feature.title}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {feature.description}
                        </Typography>
                      </CardContent>
                    </Card>
                  </motion.div>
                </Grid>
              ))}
            </Grid>
          </motion.div>
        </Container>
      </Box>

      {/* Platform Overview */}
      <Box sx={{ py: 12, bgcolor: 'background.default' }}>
        <Container maxWidth="lg">
          <motion.div variants={itemVariants}>
            <Typography variant="h4" align="center" gutterBottom sx={{ mb: 8, fontWeight: 'bold' }}>
              OASIS Architecture Overview
            </Typography>
            
            <Grid container spacing={4}>
              <Grid item xs={12} md={6}>
                <Paper sx={{ p: 4, height: '100%' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                    <Avatar sx={{ bgcolor: 'primary.main', mr: 2 }}>
                      <Storage />
                    </Avatar>
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                      WEB4 OASIS API
                    </Typography>
                  </Box>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    The foundational data aggregation and identity layer with OASIS HyperDrive 
                    auto-failover system. Universal data aggregation across all Web2 and Web3 providers.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label="Auto-Failover" size="small" />
                    <Chip label="Universal Data" size="small" />
                    <Chip label="Identity Management" size="small" />
                    <Chip label="Karma System" size="small" />
                  </Stack>
                </Paper>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Paper sx={{ p: 4, height: '100%' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                    <Avatar sx={{ bgcolor: 'secondary.main', mr: 2 }}>
                      <Apps />
                    </Avatar>
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                      WEB5 STAR API
                    </Typography>
                  </Box>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    The gamification and business layer with STAR ODK (Omniverse Interoperable 
                    Metaverse Low Code Generator). Build OAPPs, manage missions, and create NFTs.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label="STAR ODK" size="small" />
                    <Chip label="OAPPs Framework" size="small" />
                    <Chip label="Missions & NFTs" size="small" />
                    <Chip label="Low-Code Development" size="small" />
                  </Stack>
                </Paper>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Paper sx={{ p: 4, height: '100%' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                    <Avatar sx={{ bgcolor: 'success.main', mr: 2 }}>
                      <Store />
                    </Avatar>
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                      Provider Ecosystem
                    </Typography>
                  </Box>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Support for 50+ Web2 and Web3 providers including Ethereum, Solana, Holochain, 
                    IPFS, AWS, Azure, and more. Hot-swappable provider architecture.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label="50+ Providers" size="small" />
                    <Chip label="Hot-Swappable" size="small" />
                    <Chip label="Auto-Optimization" size="small" />
                    <Chip label="Zero Vendor Lock-in" size="small" />
                  </Stack>
                </Paper>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Paper sx={{ p: 4, height: '100%' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                    <Avatar sx={{ bgcolor: 'warning.main', mr: 2 }}>
                      <Group />
                    </Avatar>
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                      Universal Identity
                    </Typography>
                  </Box>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Single identity across all platforms with DID support, cross-platform authentication, 
                    and privacy-preserving identity management with karma integration.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label="DID Support" size="small" />
                    <Chip label="Cross-Platform Auth" size="small" />
                    <Chip label="Privacy-Preserving" size="small" />
                    <Chip label="Karma Integration" size="small" />
                  </Stack>
                </Paper>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Paper sx={{ p: 4, height: '100%' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                    <Avatar sx={{ bgcolor: 'info.main', mr: 2 }}>
                      <Store />
                    </Avatar>
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                      STARNET Web UI
                    </Typography>
                  </Box>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Comprehensive web interface and app store for STAR, STARNET & The OASIS. 
                    Drag-and-drop OAPP builder, asset management, and publishing platform.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label="App Store" size="small" />
                    <Chip label="OAPP Builder" size="small" />
                    <Chip label="Asset Management" size="small" />
                    <Chip label="Publishing Platform" size="small" />
                  </Stack>
                </Paper>
              </Grid>
            </Grid>
          </motion.div>
        </Container>
      </Box>

      {/* Call to Action */}
      <Box sx={{ py: 12, bgcolor: 'primary.main', color: 'white' }}>
        <Container maxWidth="md">
          <motion.div variants={itemVariants}>
            <Box sx={{ textAlign: 'center' }}>
              <Typography variant="h4" gutterBottom sx={{ fontWeight: 'bold', mb: 3 }}>
                Ready to Build the Future?
              </Typography>
              <Typography variant="h6" sx={{ mb: 6, opacity: 0.9 }}>
                Join the OASIS ecosystem and be part of the first universal Web4/Web5 infrastructure 
                that connects everything to everything. Use STARNET Web UI to build OAPPs, manage assets, 
                earn karma, and shape the future of the metaverse.
              </Typography>
              <Stack direction="row" spacing={2} justifyContent="center">
                <Button
                  variant="contained"
                  size="large"
                  startIcon={<PersonAdd />}
                  onClick={() => navigate('/avatar/signup')}
                  sx={{
                    bgcolor: 'white',
                    color: 'primary.main',
                    '&:hover': {
                      bgcolor: 'grey.100',
                    },
                  }}
                >
                  Get Started Free
                </Button>
                <Button
                  variant="outlined"
                  size="large"
                  startIcon={<Login />}
                  onClick={() => navigate('/avatar/signin')}
                  sx={{
                    borderColor: 'white',
                    color: 'white',
                    '&:hover': {
                      borderColor: 'white',
                      bgcolor: 'rgba(255,255,255,0.1)',
                    },
                  }}
                >
                  Sign In
                </Button>
              </Stack>
            </Box>
          </motion.div>
        </Container>
      </Box>
    </motion.div>
  );
};

export default HomePage;
