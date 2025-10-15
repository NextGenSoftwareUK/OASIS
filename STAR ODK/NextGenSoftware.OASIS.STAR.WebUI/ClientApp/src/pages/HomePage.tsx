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
  Image,
  LocationOn,
  Assignment,
  FlightTakeoff,
  MenuBook,
  Inventory,
  SpaceDashboard,
  DataObject,
  LibraryBooks,
  Extension,
  AccountBalanceWallet,
  Terminal,
  Gamepad,
  ViewInAr,
  School,
  Business,
  ShoppingCart,
  LocalHospital,
  AttachMoney,
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
    {
      icon: <Apps sx={{ fontSize: 40, color: '#e91e63' }} />,
      title: 'STARNET Holons Linking',
      description: 'Revolutionary linking system - any STARNETHolon can be connected to any other STARNETHolon as dependencies',
    },
    {
      icon: <Code sx={{ fontSize: 40, color: '#795548' }} />,
      title: 'STAR CLI',
      description: 'Revolutionary Interoperable Low/No Code Generator for all metaverses, games, apps, sites, and platforms',
    },
    {
      icon: <AccountBalanceWallet sx={{ fontSize: 40, color: '#00bcd4' }} />,
      title: 'OASIS Universal Wallet',
      description: 'Unified digital asset management across 50+ blockchain networks with cross-chain support and DeFi integration',
    },
    {
      icon: <Gamepad sx={{ fontSize: 40, color: '#4caf50' }} />,
      title: 'Our World AR Game',
      description: 'Groundbreaking AR geo-location game that encourages environmental stewardship and community service',
    },
    {
      icon: <ViewInAr sx={{ fontSize: 40, color: '#9c27b0' }} />,
      title: 'One World MMORPG',
      description: 'Benevolent MMORPG with optional VR, similar to Minecraft and Pax Dei with infinite building possibilities',
    },
    {
      icon: <Public sx={{ fontSize: 40, color: '#ff5722' }} />,
      title: 'Cross-Platform Universal System',
      description: 'ALL STARNET Holons can be shared across ANY OAPP (apps, games, sites, services, platforms) with infinite use cases',
    },
    {
      icon: <Terminal sx={{ fontSize: 40, color: '#795548' }} />,
      title: 'HoloNET Integration',
      description: 'World-first .NET and Unity client for Holochain, bringing P2P architecture to mainstream development',
    },
    {
      icon: <Image sx={{ fontSize: 40, color: '#ff9800' }} />,
      title: 'Revolutionary NFT System',
      description: 'Cross-chain NFTs with shared metadata, Geo-NFTs, and universal NFT standard across all platforms',
    },
    {
      icon: <Storage sx={{ fontSize: 40, color: '#607d8b' }} />,
      title: 'OASIS COSMIC ORM',
      description: 'Universal data abstraction layer for all Web2/Web3 providers with simple .Load(), .Save(), .Delete() commands',
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
                    src="/star-logo-1.png" 
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

      {/* Infinite Use Cases Section */}
      <Box sx={{ py: 12, bgcolor: 'background.default' }}>
        <Container maxWidth="lg">
          <motion.div variants={itemVariants}>
            <Typography variant="h4" align="center" gutterBottom sx={{ mb: 8, fontWeight: 'bold' }}>
              Infinite Use Cases - The Future of Everything
            </Typography>
            
            <Typography variant="h6" align="center" color="text.secondary" sx={{ mb: 6 }}>
              ALL STARNET Holons can be shared across ANY OAPP (apps, games, sites, services, platforms, etc.) - 
              creating infinite possibilities for games, businesses, shops, e-commerce, finance, education, healthcare, and everything else!
            </Typography>
            
            <Grid container spacing={4}>
              {[
                { name: 'Games', description: 'AR/VR Games, MMORPGs, Mobile Games', icon: <Gamepad />, color: '#4caf50' },
                { name: 'Business', description: 'Enterprise Applications, CRM, ERP', icon: <Business />, color: '#2196f3' },
                { name: 'E-Commerce', description: 'Online Stores, Marketplaces, Shopping', icon: <ShoppingCart />, color: '#ff9800' },
                { name: 'Finance', description: 'Banking, Trading, DeFi, Payments', icon: <AttachMoney />, color: '#4caf50' },
                { name: 'Education', description: 'Learning Platforms, Training, Schools', icon: <School />, color: '#9c27b0' },
                { name: 'Healthcare', description: 'Medical Apps, Telemedicine, Health', icon: <LocalHospital />, color: '#f44336' },
                { name: 'Social', description: 'Social Networks, Communities, Chat', icon: <Group />, color: '#00bcd4' },
                { name: 'Entertainment', description: 'Streaming, Media, Content Creation', icon: <Public />, color: '#ff5722' },
              ].map((useCase, index) => (
                <Grid item xs={12} sm={6} md={4} lg={3} key={index}>
                  <motion.div
                    variants={itemVariants}
                    whileHover={{ y: -5, scale: 1.02 }}
                    transition={{ duration: 0.2 }}
                  >
                    <Card sx={{ 
                      height: '100%', 
                      p: 2, 
                      textAlign: 'center',
                      border: 1,
                      borderColor: 'divider',
                      '&:hover': {
                        borderColor: useCase.color,
                        boxShadow: `0 4px 20px rgba(0,0,0,0.1)`,
                      }
                    }}>
                      <CardContent>
                        <Box sx={{ mb: 2, color: useCase.color }}>
                          {React.cloneElement(useCase.icon, { sx: { fontSize: 40 } })}
                        </Box>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>
                          {useCase.name}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {useCase.description}
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

      {/* STARNET Holons Section */}
      <Box sx={{ py: 12, bgcolor: 'background.paper' }}>
        <Container maxWidth="lg">
          <motion.div variants={itemVariants}>
            <Typography variant="h4" align="center" gutterBottom sx={{ mb: 8, fontWeight: 'bold' }}>
              STARNET Holons - The Building Blocks of the Metaverse
            </Typography>
            
            <Typography variant="h6" align="center" color="text.secondary" sx={{ mb: 6 }}>
              Any STARNETHolon can be linked to any other STARNETHolon as dependencies, creating infinite possibilities for unique combinations
            </Typography>
            
            <Grid container spacing={4}>
              {[
                { name: 'OAPPs', description: 'OASIS Applications', icon: <Apps />, color: '#2196f3' },
                { name: 'Runtimes', description: 'Execution Environments', icon: <Build />, color: '#673ab7' },
                { name: 'Libraries', description: 'Code Libraries', icon: <LibraryBooks />, color: '#8bc34a' },
                { name: 'Templates', description: 'Reusable Templates', icon: <Code />, color: '#ff5722' },
                { name: 'NFTs', description: 'Non-Fungible Tokens', icon: <Image />, color: '#ff9800' },
                { name: 'GeoNFTs', description: 'Geospatial NFTs', icon: <LocationOn />, color: '#4caf50' },
                { name: 'GeoHotSpots', description: 'Location-based Hotspots', icon: <LocationOn />, color: '#4caf50' },
                { name: 'Quests', description: 'Interactive Quests', icon: <Assignment />, color: '#9c27b0' },
                { name: 'Missions', description: 'Mission Objectives', icon: <FlightTakeoff />, color: '#f44336' },
                { name: 'Chapters', description: 'Story Chapters', icon: <MenuBook />, color: '#795548' },
                { name: 'Inventory Items', description: 'Game Items & Rewards', icon: <Inventory />, color: '#607d8b' },
                { name: 'Celestial Spaces', description: 'MagicVerses, Superverses, Multiverses, Universes, Galaxies, Solar Systems & More!', icon: <Public />, color: '#00bcd4' },
                { name: 'Celestial Bodies', description: 'Stars, Planets, Moons & More!', icon: <SpaceDashboard />, color: '#3f51b5' },
                { name: 'Zomes', description: 'Holochain Zomes', icon: <Memory />, color: '#e91e63' },
                { name: 'Holons', description: 'Basic Data Structures', icon: <DataObject />, color: '#009688' },
                { name: 'MetaData DNA', description: 'DNA Metadata', icon: <Code />, color: '#ff5722' },
              ].map((holon, index) => (
                <Grid item xs={12} sm={6} md={4} lg={3} key={index}>
                  <motion.div
                    variants={itemVariants}
                    whileHover={{ y: -5, scale: 1.02 }}
                    transition={{ duration: 0.2 }}
                  >
                    <Card sx={{ 
                      height: '100%', 
                      p: 2, 
                      textAlign: 'center',
                      border: 1,
                      borderColor: 'divider',
                      '&:hover': {
                        borderColor: holon.color,
                        boxShadow: `0 4px 20px rgba(0,0,0,0.1)`,
                      }
                    }}>
                      <CardContent>
                        <Box sx={{ mb: 2, color: holon.color }}>
                          {React.cloneElement(holon.icon, { sx: { fontSize: 40 } })}
                        </Box>
                        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>
                          {holon.name}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {holon.description}
                        </Typography>
                      </CardContent>
                    </Card>
                  </motion.div>
                </Grid>
              ))}
            </Grid>
            
            {/*<Box sx={{ mt: 6, textAlign: 'center' }}>*/}
            {/*  <Typography variant="h6" gutterBottom>*/}
            {/*    Example Linkings - Infinite Possibilities:*/}
            {/*  </Typography>*/}
            {/*  <Stack direction="row" spacing={2} justifyContent="center" flexWrap="wrap">*/}
            {/*    <Chip label="Quest + GeoNFT" color="primary" />*/}
            {/*    <Chip label="OAPP + NFT" color="secondary" />*/}
            {/*    <Chip label="Mission + Inventory Items" color="success" />*/}
            {/*    <Chip label="GeoHotSpot + Quest" color="warning" />*/}
            {/*    <Chip label="Template + Library" color="info" />*/}
            {/*    <Chip label="Runtime + Zome" color="error" />*/}
            {/*    <Chip label="Celestial Space + Celestial Body" color="primary" />*/}
            {/*    <Chip label="Any STARNETHolon + Any STARNETHolon!" color="secondary" />*/}
            {/*  </Stack>*/}
            {/*</Box>*/}
          </motion.div>
        </Container>
      </Box>

      {/* Platform Overview */}
      <Box sx={{ py: 12, bgcolor: 'background.paper' }}>
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
                    <Chip label="App/Asset/STARNET Holon Store" size="small" />
                    <Chip label="OAPP Builder" size="small" />
                    <Chip label="Asset Management" size="small" />
                    <Chip label="Publishing Platform" size="small" />
                  </Stack>
                </Paper>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Paper sx={{ p: 4, height: '100%' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                    <Avatar sx={{ bgcolor: 'error.main', mr: 2 }}>
                      <Code />
                    </Avatar>
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                      STAR CLI & DNA System
                    </Typography>
                  </Box>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Powerful command-line interface for developers. Link any STARNETHolon to any other 
                    STARNETHolon as dependencies. Unlimited combinations and creativity.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label="STAR CLI" size="small" />
                    <Chip label="DNA/STARNET (Holon) System" size="small" />
                    <Chip label="Dependencies" size="small" />
                    <Chip label="Developer Tools" size="small" />
                    <Chip label="CLI To The OASIS" size="small" />
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
                that connects everything to everything. Use STAR CLI and STARNET Web UI to build OAPPs, 
                link ALL STARNET Holons as dependencies, manage assets, earn karma, and shape the future of 
                games, businesses, shops, e-commerce, finance, education, healthcare, and everything else!
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
