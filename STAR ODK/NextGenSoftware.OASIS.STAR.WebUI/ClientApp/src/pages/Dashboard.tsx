import React from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  Chip,
  Avatar,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Divider,
  IconButton,
} from '@mui/material';
import {
  Star,
  Apps,
  Assignment,
  Image,
  People,
  Refresh,
  PlayArrow,
  Pause,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery } from 'react-query';
import { starService } from '../services/starService';
import { useSTARConnection } from '../hooks/useSTARConnection';
import { toast } from 'react-hot-toast';
import KarmaVisualization from '../components/KarmaVisualization';
import KarmaSearch from '../components/KarmaSearch';
import STARStatusCard from '../components/STARStatusCard';
import StatsOverview from '../components/StatsOverview';
import RecentActivity from '../components/RecentActivity';

interface DashboardProps {
  isConnected: boolean;
}

const Dashboard: React.FC<DashboardProps> = ({ isConnected }) => {
  // Use the STAR connection hook for consistent state management
  const { igniteSTAR, extinguishStar, starStatus } = useSTARConnection();

  // Fetch OAPPs data
  const { data: oappsData } = useQuery(
    'oapps',
    () => Promise.resolve({ result: [] }), // Placeholder for now
    {
      enabled: isConnected,
    }
  );

  // Fetch Quests data
  const { data: questsData } = useQuery(
    'quests',
    () => Promise.resolve({ result: [] }), // Placeholder for now
    {
      enabled: isConnected,
    }
  );

  // Fetch NFTs data
  const { data: nftsData } = useQuery(
    'nfts',
    () => Promise.resolve({ result: [] }), // Placeholder for now
    {
      enabled: isConnected,
    }
  );

  // Fetch dashboard data
  const { data: avatarData } = useQuery(
    'beamedInAvatar',
    starService.getBeamedInAvatar,
    {
      enabled: starStatus?.isIgnited,
      refetchInterval: 30000,
    }
  );

  const handleIgniteSTAR = async () => {
    try {
      await igniteSTAR();
      toast.success('STAR ignited successfully!');
    } catch (error) {
      toast.error('Failed to ignite STAR');
    }
  };

  const handleExtinguishStar = async () => {
    try {
      await extinguishStar();
      toast.success('STAR extinguished successfully!');
    } catch (error) {
      toast.error('Failed to extinguish STAR');
    }
  };

  const stats = [
    {
      title: 'OAPPs',
      value: oappsData?.result?.length || 0,
      icon: <Apps />,
      color: '#00bcd4',
      description: 'Omniverse Applications',
    },
    {
      title: 'Quests',
      value: questsData?.result?.length || 0,
      icon: <Assignment />,
      color: '#ff4081',
      description: 'Interactive Quests',
    },
    {
      title: 'NFTs',
      value: nftsData?.result?.length || 0,
      icon: <Image />,
      color: '#4caf50',
      description: 'Digital Assets',
    },
    {
      title: 'Avatars',
      value: 1, // Current user
      icon: <People />,
      color: '#ff9800',
      description: 'Active Users',
    },
  ];

  const recentActivities = [
    {
      id: 1,
      type: 'oapp',
      title: 'New OAPP Created',
      description: 'My Awesome App v1.0',
      timestamp: '2 hours ago',
      icon: <Apps />,
    },
    {
      id: 2,
      type: 'quest',
      title: 'Quest Completed',
      description: 'Tutorial Quest',
      timestamp: '4 hours ago',
      icon: <Assignment />,
    },
    {
      id: 3,
      type: 'nft',
      title: 'NFT Minted',
      description: 'Digital Art #123',
      timestamp: '1 day ago',
      icon: <Image />,
    },
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
    visible: {
      opacity: 1,
      y: 0,
      transition: {
        duration: 0.5,
      },
    },
  };

  if (!isConnected) {
    return (
      <motion.div
        variants={containerVariants}
        initial="hidden"
        animate="visible"
      >
        <Box sx={{ textAlign: 'center', py: 8 }}>
          <motion.div variants={itemVariants}>
            <Star sx={{ fontSize: 80, color: 'primary.main', mb: 2 }} />
            <Typography variant="h4" gutterBottom>
              Welcome to STAR Web UI
            </Typography>
            <Typography variant="h6" color="text.secondary" paragraph>
              The OASIS Omniverse/MagicVerse Light Interface
            </Typography>
            <Typography variant="body1" color="text.secondary" paragraph>
              Connect to STAR to begin your journey through the omniverse
            </Typography>
            <Button
              variant="contained"
              size="large"
              startIcon={<Star />}
              onClick={handleIgniteSTAR}
              sx={{ mt: 2 }}
            >
              Ignite STAR
            </Button>
          </motion.div>
        </Box>
      </motion.div>
    );
  }

  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
    >
      <Box sx={{ mb: 4 }}>
        <motion.div variants={itemVariants}>
          <Typography variant="h4" gutterBottom>
            Dashboard
          </Typography>
          <Typography variant="subtitle1" color="text.secondary">
            Welcome back to the OASIS Omniverse
          </Typography>
        </motion.div>
      </Box>

      {/* Enhanced STAR Status Card */}
      <motion.div variants={itemVariants}>
        <STARStatusCard />
      </motion.div>

      {/* Statistics Overview */}
      <motion.div variants={itemVariants}>
        <StatsOverview />
      </motion.div>

      {/* Stats Grid */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        {stats.map((stat, index) => (
          <Grid item xs={12} sm={6} md={3} key={stat.title}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: '100%' }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                    <Avatar sx={{ bgcolor: stat.color }}>
                      {stat.icon}
                    </Avatar>
                    <Box sx={{ flexGrow: 1 }}>
                      <Typography variant="h4" color="primary">
                        {stat.value}
                      </Typography>
                      <Typography variant="h6">
                        {stat.title}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {stat.description}
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>
        ))}
      </Grid>

      <Grid container spacing={3}>
        {/* Avatar Info */}
        <Grid item xs={12} md={6}>
          <motion.div variants={itemVariants}>
            <Card sx={{ height: '100%' }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Current Avatar
                </Typography>
                {avatarData?.result ? (
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                    <Avatar sx={{ width: 60, height: 60, bgcolor: 'primary.main' }}>
                      {avatarData.result.username?.charAt(0).toUpperCase()}
                    </Avatar>
                    <Box>
                      <Typography variant="h6">
                        {avatarData.result.username}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {avatarData.result.email}
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 1, mt: 1 }}>
                        <Chip
                          label={`Level ${avatarData.result.level || 1}`}
                          size="small"
                          color="primary"
                          variant="outlined"
                        />
                        <Chip
                          label={`${avatarData.result.karma || 0} Karma`}
                          size="small"
                          color="secondary"
                          variant="outlined"
                        />
                      </Box>
                    </Box>
                  </Box>
                ) : (
                  <Typography color="text.secondary">
                    No avatar beamed in
                  </Typography>
                )}
              </CardContent>
            </Card>
          </motion.div>
        </Grid>

        {/* Enhanced Recent Activity */}
        <Grid item xs={12} md={6}>
          <motion.div variants={itemVariants}>
            <RecentActivity />
          </motion.div>
        </Grid>
      </Grid>

      {/* Karma Section */}
      <motion.div variants={itemVariants}>
        <Box sx={{ mt: 4 }}>
          <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
            🌟 OAPP Karma System
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
            Explore OAPPs and their karma data from the WEB4 OASIS API. 
            Karma represents the total positive energy generated by all users in each OAPP.
          </Typography>
          
          {/* Karma Search */}
          <Box sx={{ mb: 4 }}>
            <KarmaSearch />
          </Box>
          
          {/* Sample Karma Visualizations */}
          <Box sx={{ mb: 4 }}>
            <Typography variant="h6" gutterBottom>
              Featured OAPPs
            </Typography>
            <Grid container spacing={3}>
              <Grid item xs={12} md={4}>
                <KarmaVisualization 
                  oappId="oapp_quest" 
                  oappName="Quest OAPP" 
                />
              </Grid>
              <Grid item xs={12} md={4}>
                <KarmaVisualization 
                  oappId="oapp_social" 
                  oappName="Social OAPP" 
                />
              </Grid>
              <Grid item xs={12} md={4}>
                <KarmaVisualization 
                  oappId="oapp_legendary" 
                  oappName="Legendary OAPP" 
                />
              </Grid>
            </Grid>
          </Box>
        </Box>
      </motion.div>
    </motion.div>
  );
};

export default Dashboard;
