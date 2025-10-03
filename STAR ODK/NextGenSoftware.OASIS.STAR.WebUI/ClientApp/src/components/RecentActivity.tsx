import React from 'react';
import { Box, Card, CardContent, Typography, List, ListItem, ListItemText, ListItemIcon, Chip } from '@mui/material';
import { motion } from 'framer-motion';
import { useQuery } from 'react-query';

interface ActivityItem {
  id: string;
  type: 'quest' | 'nft' | 'oapp' | 'avatar' | 'karma';
  title: string;
  description: string;
  timestamp: string;
  icon: string;
  color: string;
}

const RecentActivity: React.FC = () => {
  // Mock activity data - replace with real API calls when available
  const { data: activities, isLoading } = useQuery<ActivityItem[]>(
    'recentActivity',
    () => Promise.resolve([
      {
        id: '1',
        type: 'karma',
        title: 'Karma Earned',
        description: 'User earned 150 karma points in Quest OAPP',
        timestamp: '2 minutes ago',
        icon: '🌟',
        color: '#ffeb3b',
      },
      {
        id: '2',
        type: 'quest',
        title: 'Quest Completed',
        description: 'Environmental Cleanup Quest completed successfully',
        timestamp: '5 minutes ago',
        icon: '⚔️',
        color: '#4caf50',
      },
      {
        id: '3',
        type: 'nft',
        title: 'NFT Minted',
        description: 'New GeoNFT created at Central Park location',
        timestamp: '12 minutes ago',
        icon: '🎨',
        color: '#2196f3',
      },
      {
        id: '4',
        type: 'oapp',
        title: 'OAPP Created',
        description: 'New Social OAPP "Nature Lovers" launched',
        timestamp: '1 hour ago',
        icon: '🌌',
        color: '#9c27b0',
      },
      {
        id: '5',
        type: 'avatar',
        title: 'Avatar Joined',
        description: 'New avatar "EcoExplorer" joined the OASIS',
        timestamp: '2 hours ago',
        icon: '👤',
        color: '#ff9800',
      },
    ]),
    { refetchInterval: 30000 }
  );

  const getActivityChip = (type: string) => {
    const chipColors: { [key: string]: string } = {
      quest: '#4caf50',
      nft: '#2196f3',
      oapp: '#9c27b0',
      avatar: '#ff9800',
      karma: '#ffeb3b',
    };

    const chipLabels: { [key: string]: string } = {
      quest: 'Quest',
      nft: 'NFT',
      oapp: 'OAPP',
      avatar: 'Avatar',
      karma: 'Karma',
    };

    return (
      <Chip
        label={chipLabels[type] || type}
        size="small"
        sx={{
          bgcolor: chipColors[type] || '#616161',
          color: 'white',
          fontWeight: 'bold',
        }}
      />
    );
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
    >
      <Card
        sx={{
          background: 'linear-gradient(145deg, #1a1a1a 0%, #2a2a2a 100%)',
          border: '2px solid #333',
          boxShadow: '0 0 20px rgba(0,0,0,0.3)',
        }}
      >
        <CardContent>
          <Typography variant="h6" gutterBottom sx={{ color: 'white', mb: 2 }}>
            📈 Recent Activity
          </Typography>

          {isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <Typography color="text.secondary">Loading recent activity...</Typography>
            </Box>
          ) : (
            <List>
              {activities?.map((activity, index) => (
                <motion.div
                  key={activity.id}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ duration: 0.3, delay: index * 0.1 }}
                >
                  <ListItem
                    sx={{
                      borderBottom: '1px solid rgba(255,255,255,0.1)',
                      '&:last-child': { borderBottom: 'none' },
                    }}
                  >
                    <ListItemIcon sx={{ minWidth: 40 }}>
                      <Typography variant="h6" sx={{ color: activity.color }}>
                        {activity.icon}
                      </Typography>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="subtitle1" sx={{ color: 'white' }}>
                            {activity.title}
                          </Typography>
                          {getActivityChip(activity.type)}
                        </Box>
                      }
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
                </motion.div>
              ))}
            </List>
          )}

          <Box sx={{ mt: 2, p: 2, bgcolor: 'rgba(255,255,255,0.05)', borderRadius: 1 }}>
            <Typography variant="caption" color="text.secondary">
              Real-time activity feed from the OASIS ecosystem
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </motion.div>
  );
};

export default RecentActivity;
