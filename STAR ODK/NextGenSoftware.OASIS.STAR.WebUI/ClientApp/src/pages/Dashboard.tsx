import React from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  Chip,
  LinearProgress,
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
  LocationOn,
  TrendingUp,
  People,
  Public,
  Refresh,
  Add,
  PlayArrow,
  Pause,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery } from 'react-query';
import { starService } from '../services/starService';
import { starNetService } from '../services/starNetService';
import { useSTARConnection } from '../hooks/useSTARConnection';
import { toast } from 'react-hot-toast';

interface DashboardProps {
  isConnected: boolean;
}

const Dashboard: React.FC<DashboardProps> = ({ isConnected }) => {
  // Fetch STAR status
  const { data: starStatus, refetch: refetchStarStatus } = useQuery(
    'starStatus',
    starService.getSTARStatus,
    {
      refetchInterval: 5000,
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
      await starService.igniteSTAR();
      refetchStarStatus();
      toast.success('STAR ignited successfully!');
    } catch (error) {
      toast.error('Failed to ignite STAR');
    }
  };

  const handleExtinguishStar = async () => {
    try {
      await starService.extinguishStar();
      refetchStarStatus();
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

      {/* STAR Status Card */}
      <motion.div variants={itemVariants}>
        <Card sx={{ mb: 4, background: 'linear-gradient(135deg, #1a1a1a 0%, #2a2a2a 100%)' }}>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Avatar sx={{ bgcolor: 'primary.main' }}>
                  <Star />
                </Avatar>
                <Box>
                  <Typography variant="h6">
                    STAR Status
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {isConnected ? 'Connected and Ready' : 'Disconnected'}
                  </Typography>
                </Box>
              </Box>
              <Box sx={{ display: 'flex', gap: 1 }}>
                <Chip
                  label={isConnected ? 'Online' : 'Offline'}
                  color={isConnected ? 'success' : 'error'}
                  variant="outlined"
                />
                <IconButton
                  color="primary"
                  onClick={isConnected ? handleExtinguishStar : handleIgniteSTAR}
                >
                  {isConnected ? <Pause /> : <PlayArrow />}
                </IconButton>
              </Box>
            </Box>
          </CardContent>
        </Card>
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

        {/* Recent Activity */}
        <Grid item xs={12} md={6}>
          <motion.div variants={itemVariants}>
            <Card sx={{ height: '100%' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
                  <Typography variant="h6">
                    Recent Activity
                  </Typography>
                  <IconButton size="small">
                    <Refresh />
                  </IconButton>
                </Box>
                <List>
                  {recentActivities.map((activity, index) => (
                    <React.Fragment key={activity.id}>
                      <ListItem>
                        <ListItemAvatar>
                          <Avatar sx={{ bgcolor: 'primary.main' }}>
                            {activity.icon}
                          </Avatar>
                        </ListItemAvatar>
                        <ListItemText
                          primary={activity.title}
                          secondary={
                            <Box>
                              <Typography variant="body2" color="text.secondary">
                                {activity.description}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {activity.timestamp}
                              </Typography>
                            </Box>
                          }
                        />
                      </ListItem>
                      {index < recentActivities.length - 1 && <Divider />}
                    </React.Fragment>
                  ))}
                </List>
              </CardContent>
            </Card>
          </motion.div>
        </Grid>
      </Grid>
    </motion.div>
  );
};

export default Dashboard;
