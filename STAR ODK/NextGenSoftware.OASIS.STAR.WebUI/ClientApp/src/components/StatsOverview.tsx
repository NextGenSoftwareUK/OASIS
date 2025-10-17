import React from 'react';
import { Box, Grid, Card, CardContent, Typography, LinearProgress } from '@mui/material';
import { motion } from 'framer-motion';
import { useQuery } from 'react-query';
import { statsService } from '../services';

interface StatCardProps {
  title: string;
  value: number;
  icon: string;
  color: string;
  isLoading?: boolean;
}

const StatCard: React.FC<StatCardProps> = ({ title, value, icon, color, isLoading }) => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
    >
      <Card
        sx={{
          background: 'linear-gradient(145deg, #1a1a1a 0%, #2a2a2a 100%)',
          border: `2px solid ${color}`,
          boxShadow: `0 0 15px ${color}30`,
          transition: 'all 0.3s ease-in-out',
          '&:hover': {
            transform: 'translateY(-5px)',
            boxShadow: `0 0 25px ${color}50`,
          },
        }}
      >
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="h6" component="div" sx={{ color: 'white' }}>
              {icon} {title}
            </Typography>
            <Typography variant="h4" sx={{ color, fontWeight: 'bold' }}>
              {isLoading ? '...' : value.toLocaleString()}
            </Typography>
          </Box>

          {isLoading && (
            <LinearProgress
              sx={{
                bgcolor: 'rgba(255,255,255,0.1)',
                '& .MuiLinearProgress-bar': {
                  bgcolor: color,
                },
              }}
            />
          )}

          <Typography variant="body2" color="text.secondary">
            {isLoading ? 'Loading...' : 'Active items in the OASIS'}
          </Typography>
        </CardContent>
      </Card>
    </motion.div>
  );
};

const StatsOverview: React.FC = () => {
  const avatarId = typeof window !== 'undefined' ? (localStorage.getItem('avatarId') || 'demo-avatar') : 'demo-avatar';

  // Real stats via STAR API with demo fallback inside service
  const { data: karmaStats, isLoading: karmaLoading } = useQuery(
    ['stats-karma', avatarId],
    async () => {
      const res = await statsService.getKarmaStats(avatarId);
      return res.result || { totalKarma: 0 };
    },
    { refetchInterval: 30000 }
  );

  const { data: chatStats, isLoading: chatLoading } = useQuery(
    ['stats-chat', avatarId],
    async () => {
      const res = await statsService.getChatStats(avatarId);
      return res.result || { totalMessages: 0 };
    },
    { refetchInterval: 30000 }
  );

  const { data: questStats, isLoading: questLoading } = useQuery(
    ['stats-quests', avatarId],
    async () => {
      const res = await statsService.getQuestStats(avatarId);
      return res.result || { totalQuests: 0 };
    },
    { refetchInterval: 30000 }
  );

  const stats = [
    {
      title: 'Total Karma',
      value: karmaStats?.totalKarma || 0,
      icon: '⭐',
      color: '#9c27b0',
      isLoading: karmaLoading,
    },
    {
      title: 'Messages Sent',
      value: chatStats?.totalMessages || 0,
      icon: '💬',
      color: '#2196f3',
      isLoading: chatLoading,
    },
    {
      title: 'Total Quests',
      value: questStats?.totalQuests || 0,
      icon: '⚔️',
      color: '#ff9800',
      isLoading: questLoading,
    },
    {
      title: 'Completed Quests',
      value: questStats?.completedQuests || 0,
      icon: '🏁',
      color: '#4caf50',
      isLoading: questLoading,
    },
  ];

  return (
    <Box sx={{ mb: 4 }}>
      <Typography variant="h5" gutterBottom sx={{ mb: 3, color: 'white' }}>
        📊 OASIS Statistics
      </Typography>
      <Grid container spacing={3}>
        {stats.map((stat, index) => (
          <Grid item xs={12} sm={6} md={3} key={stat.title}>
            <StatCard
              title={stat.title}
              value={stat.value}
              icon={stat.icon}
              color={stat.color}
              isLoading={stat.isLoading}
            />
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default StatsOverview;
