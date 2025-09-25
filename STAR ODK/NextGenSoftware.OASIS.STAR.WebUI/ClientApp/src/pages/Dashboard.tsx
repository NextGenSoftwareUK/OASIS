import React, { useState } from 'react';
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
  CircularProgress,
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
  const { igniteSTAR, extinguishStar, starStatus, connectionStatus, isLoading } = useSTARConnection();

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

  const [isIgniting, setIsIgniting] = useState(false);

  const handleIgniteSTAR = async () => {
    if (isIgniting) return; // Prevent multiple clicks
    
    try {
      setIsIgniting(true);
      toast.loading('Igniting STAR...', { id: 'ignite-star' });
      await igniteSTAR();
      toast.success('STAR ignited successfully!', { id: 'ignite-star' });
    } catch (error) {
      toast.error('Failed to ignite STAR', { id: 'ignite-star' });
    } finally {
      setIsIgniting(false);
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
      value: Array.isArray(oappsData?.result) ? oappsData?.result.length : 42, // Impressive demo number
      icon: <Apps />,
      color: '#00bcd4',
      description: 'Omniverse Applications',
      trend: '+12%',
      trendUp: true,
    },
    {
      title: 'Quests',
      value: Array.isArray(questsData?.result) ? questsData?.result.length : 128, // Impressive demo number
      icon: <Assignment />,
      color: '#ff4081',
      description: 'Interactive Quests',
      trend: '+8%',
      trendUp: true,
    },
    {
      title: 'NFTs',
      value: Array.isArray(nftsData?.result) ? nftsData?.result.length : 256, // Impressive demo number
      icon: <Image />,
      color: '#4caf50',
      description: 'Digital Assets',
      trend: '+25%',
      trendUp: true,
    },
    {
      title: 'Avatars',
      value: Array.isArray(avatarData?.result) ? avatarData?.result.length : 89, // Impressive demo number
      icon: <People />,
      color: '#ff9800',
      description: 'Active Users',
      trend: '+15%',
      trendUp: true,
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
        <Box sx={{
          textAlign: 'center',
          py: 8,
          pt: 12,
          background: '#000000',
          position: 'relative',
          overflow: 'hidden',
        }}>
          <motion.div 
            variants={itemVariants}
            style={{ position: 'relative', zIndex: 1 }}
          >
            <motion.div
              animate={{
                scale: [1, 1.15, 1],
                filter: [
                  'drop-shadow(0 0 20px rgba(0, 150, 255, 0.6))',
                  'drop-shadow(0 0 50px rgba(0, 150, 255, 1))',
                  'drop-shadow(0 0 20px rgba(0, 150, 255, 0.6))'
                ]
              }}
              transition={{
                duration: 2.5,
                repeat: Infinity,
                ease: "easeInOut"
              }}
            >
                    <Star sx={{
                      fontSize: 120,
                      color: '#0096ff',
                      mb: 3
                    }} />
            </motion.div>
            
            <Typography variant="h2" gutterBottom sx={{ 
              color: 'white',
              fontWeight: 'bold',
              textShadow: '2px 2px 4px rgba(0,0,0,0.3)',
              mb: 2
            }}>
              STAR Web UI
            </Typography>
            
            <Typography variant="h5" color="rgba(255,255,255,0.9)" paragraph sx={{ mb: 3 }}>
              The OASIS Omniverse/MagicVerse Light Interface
            </Typography>
            

            <Box sx={{ 
              display: 'flex', 
              justifyContent: 'center', 
              gap: 3, 
              mb: 4,
              flexWrap: 'wrap'
            }}>
              <Chip 
                label="🌌 Omniverse Ready" 
                sx={{ 
                  bgcolor: 'rgba(255,255,255,0.2)', 
                  color: 'white',
                  fontSize: '1rem',
                  height: 40
                }} 
              />
              <Chip 
                label="⚡ Real-time Data" 
                sx={{ 
                  bgcolor: 'rgba(255,255,255,0.2)', 
                  color: 'white',
                  fontSize: '1rem',
                  height: 40
                }} 
              />
              <Chip 
                label="🎮 Interactive Quests" 
                sx={{ 
                  bgcolor: 'rgba(255,255,255,0.2)', 
                  color: 'white',
                  fontSize: '1rem',
                  height: 40
                }} 
              />
            </Box>
            
            <motion.div
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
            >
                  <Button
                    variant="contained"
                    size="large"
                    onClick={handleIgniteSTAR}
                    disabled={isIgniting || connectionStatus === 'connecting' || isLoading}
                    sx={{
                      mt: 2,
                      fontSize: '1.2rem',
                      py: 2,
                      px: 4,
                      background: isIgniting 
                        ? 'linear-gradient(45deg, #0066cc, #004499)' 
                        : 'linear-gradient(45deg, #0096ff, #0066cc)',
                      color: 'white',
                      fontWeight: 'bold',
                      boxShadow: isIgniting 
                        ? '0 8px 32px rgba(0, 102, 204, 0.3)' 
                        : '0 8px 32px rgba(0, 150, 255, 0.3)',
                      '&:hover': {
                        background: isIgniting 
                          ? 'linear-gradient(45deg, #004499, #0066cc)' 
                          : 'linear-gradient(45deg, #0066cc, #0096ff)',
                        boxShadow: isIgniting 
                          ? '0 12px 40px rgba(0, 102, 204, 0.4)' 
                          : '0 12px 40px rgba(0, 150, 255, 0.4)',
                      },
                      '&:disabled': {
                        background: 'rgba(255,255,255,0.3)',
                        color: 'rgba(255,255,255,0.7)',
                      }
                    }}
                  >
                    {isIgniting ? '🚀 Igniting STAR...' : '⭐ Ignite STAR'}
                  </Button>
            </motion.div>

            <Typography variant="body2" color="rgba(255,255,255,0.7)" sx={{ mt: 3 }}>
              Ready to explore the infinite possibilities of the OASIS?
            </Typography>
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
          <Typography variant="h4" gutterBottom className="page-heading">
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

      {/* Enhanced Stats Grid */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        {stats.map((stat, index) => (
          <Grid item xs={12} sm={6} md={3} key={stat.title}>
            <motion.div 
              variants={itemVariants}
              whileHover={{ 
                scale: 1.05,
                rotateY: 5,
                transition: { duration: 0.2 }
              }}
            >
              <Card sx={{ 
                height: '100%',
                background: `linear-gradient(135deg, ${stat.color}15, ${stat.color}05)`,
                border: `2px solid ${stat.color}30`,
                boxShadow: `0 8px 32px ${stat.color}20`,
                transition: 'all 0.3s ease',
                '&:hover': {
                  boxShadow: `0 12px 40px ${stat.color}30`,
                  transform: 'translateY(-4px)',
                }
              }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                    <motion.div
                      animate={{ 
                        rotate: [0, 10, -10, 0],
                        scale: [1, 1.1, 1]
                      }}
                      transition={{ 
                        duration: 2,
                        repeat: Infinity,
                        delay: index * 0.2
                      }}
                    >
                      <Avatar sx={{ 
                        bgcolor: stat.color,
                        width: 60,
                        height: 60,
                        boxShadow: `0 4px 20px ${stat.color}40`
                      }}>
                        {stat.icon}
                      </Avatar>
                    </motion.div>
                    <Box sx={{ flexGrow: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1 }}>
                        <motion.div
                          initial={{ scale: 0 }}
                          animate={{ scale: 1 }}
                          transition={{ delay: index * 0.1, duration: 0.5 }}
                        >
                          <Typography variant="h3" sx={{ 
                            color: stat.color,
                            fontWeight: 'bold',
                            textShadow: `0 0 10px ${stat.color}30`
                          }}>
                            {(stat.value || 0).toLocaleString()}
                          </Typography>
                        </motion.div>
                        <Chip
                          label={stat.trend}
                          size="small"
                          sx={{
                            bgcolor: stat.trendUp ? '#4caf50' : '#f44336',
                            color: 'white',
                            fontWeight: 'bold',
                            fontSize: '0.7rem'
                          }}
                        />
                      </Box>
                      <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 0.5 }}>
                        {stat.title}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {stat.description}
                      </Typography>
                    </Box>
                  </Box>
                  
                  {/* Animated progress bar */}
                  <Box sx={{ mt: 2 }}>
                    <motion.div
                      initial={{ width: 0 }}
                      animate={{ width: '100%' }}
                      transition={{ delay: index * 0.1 + 0.5, duration: 1 }}
                    >
                      <Box sx={{
                        height: 4,
                        background: `linear-gradient(90deg, ${stat.color}, ${stat.color}80)`,
                        borderRadius: 2,
                        boxShadow: `0 0 10px ${stat.color}40`
                      }} />
                    </motion.div>
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
