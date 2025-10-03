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
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';

const HomePage: React.FC = () => {
  const navigate = useNavigate();

  const features = [
    {
      icon: <Star sx={{ fontSize: 40, color: '#ffd700' }} />,
      title: 'OASIS Integration',
      description: 'Seamlessly connect with the OASIS universe and access all its features',
    },
    {
      icon: <Rocket sx={{ fontSize: 40, color: '#ff6b35' }} />,
      title: 'Lightning Fast',
      description: 'Built for speed with cutting-edge technology and optimized performance',
    },
    {
      icon: <Security sx={{ fontSize: 40, color: '#4caf50' }} />,
      title: 'Secure & Private',
      description: 'Enterprise-grade security with end-to-end encryption and privacy protection',
    },
    {
      icon: <Public sx={{ fontSize: 40, color: '#2196f3' }} />,
      title: 'Global Network',
      description: 'Connect with users worldwide in the largest virtual reality network',
    },
    {
      icon: <Cloud sx={{ fontSize: 40, color: '#9c27b0' }} />,
      title: 'Cloud Powered',
      description: 'Leverage the power of cloud computing for unlimited scalability',
    },
    {
      icon: <Science sx={{ fontSize: 40, color: '#ff9800' }} />,
      title: 'AI Enhanced',
      description: 'Advanced AI integration for intelligent automation and assistance',
    },
  ];

  const stats = [
    { label: 'Active Users', value: '2.5M+', color: '#00bcd4' },
    { label: 'Virtual Worlds', value: '10K+', color: '#00acc1' },
    { label: 'Transactions', value: '$50M+', color: '#0097a7' },
    { label: 'Countries', value: '180+', color: '#00838f' },
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
                Welcome to STARNET
              </Typography>
              
              <Typography variant="h5" sx={{ mb: 4, opacity: 0.9, maxWidth: 600, mx: 'auto' }}>
                The ultimate platform for the OASIS Omniverse. Connect, create, and explore 
                in the most advanced interoperable AR/VR/IR Metaverse ecosystem ever built.
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
              Trusted by Millions Worldwide
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
              Why Choose STARNET?
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
              Complete OASIS Platform
            </Typography>
            
            <Grid container spacing={4}>
              <Grid item xs={12} md={6}>
                <Paper sx={{ p: 4, height: '100%' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                    <Avatar sx={{ bgcolor: 'primary.main', mr: 2 }}>
                      <Explore />
                    </Avatar>
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                      Virtual Worlds
                    </Typography>
                  </Box>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Explore thousands of virtual worlds, from fantasy realms to futuristic cities. 
                    Each world offers unique experiences and endless possibilities.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label="3D Environments" size="small" />
                    <Chip label="Social Interaction" size="small" />
                    <Chip label="Real-time Events" size="small" />
                  </Stack>
                </Paper>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Paper sx={{ p: 4, height: '100%' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                    <Avatar sx={{ bgcolor: 'secondary.main', mr: 2 }}>
                      <Code />
                    </Avatar>
                    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                      Development Tools
                    </Typography>
                  </Box>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Build and deploy applications with our comprehensive development platform. 
                    From simple scripts to complex AI systems.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label="SDK & APIs" size="small" />
                    <Chip label="AI Integration" size="small" />
                    <Chip label="Cloud Deployment" size="small" />
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
                      Marketplace
                    </Typography>
                  </Box>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Buy, sell, and trade digital assets in our secure marketplace. 
                    From virtual real estate to unique digital collectibles.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label="NFT Trading" size="small" />
                    <Chip label="Virtual Real Estate" size="small" />
                    <Chip label="Digital Art" size="small" />
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
                      Community
                    </Typography>
                  </Box>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Join millions of users in the largest virtual community. 
                    Collaborate, learn, and grow together in the OASIS.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label="Global Network" size="small" />
                    <Chip label="Collaboration Tools" size="small" />
                    <Chip label="Knowledge Sharing" size="small" />
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
                Ready to Enter the OASIS?
              </Typography>
              <Typography variant="h6" sx={{ mb: 6, opacity: 0.9 }}>
                Join the future of virtual reality and be part of the most advanced 
                digital ecosystem ever created.
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
