import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Chip,
  Button,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Avatar,
  LinearProgress,
  Divider,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Paper,
} from '@mui/material';
import {
  ArrowBack,
  Star,
  TrendingUp,
  TrendingDown,
  Timeline,
  EmojiEvents,
  History,
  Edit,
  Delete,
  Add,
  CheckCircle,
  Cancel,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starService } from '../services/starService';
import { toast } from 'react-hot-toast';

interface KarmaEntry {
  id: string;
  date: Date;
  action: string;
  points: number;
  type: 'earned' | 'spent' | 'bonus' | 'penalty';
  description: string;
  source: string;
}

interface KarmaDetail {
  id: string;
  totalKarma: number;
  currentLevel: number;
  levelName: string;
  progressToNextLevel: number;
  karmaHistory: KarmaEntry[];
  achievements: Array<{
    id: string;
    name: string;
    description: string;
    icon: string;
    earnedDate: Date;
    karmaReward: number;
  }>;
  recentActivity: KarmaEntry[];
  monthlyStats: {
    earned: number;
    spent: number;
    net: number;
  };
}

const KarmaDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedEntry, setSelectedEntry] = useState<KarmaEntry | null>(null);

  // Fetch karma detail data
  const { data, isLoading, error } = useQuery(
    ['karmaDetail', id],
    async () => {
      // Demo data for now
      const demoData: KarmaDetail = {
        id: id || '1',
        totalKarma: 15420,
        currentLevel: 8,
        levelName: 'Cosmic Guardian',
        progressToNextLevel: 75,
        karmaHistory: [
          {
            id: '1',
            date: new Date('2024-01-15'),
            action: 'Completed Mission: Space Exploration',
            points: 500,
            type: 'earned',
            description: 'Successfully completed a complex space exploration mission',
            source: 'Mission System'
          },
          {
            id: '2',
            date: new Date('2024-01-14'),
            action: 'Helped New User',
            points: 100,
            type: 'earned',
            description: 'Provided assistance to a new OASIS user',
            source: 'Community'
          },
          {
            id: '3',
            date: new Date('2024-01-13'),
            action: 'Purchased Premium Feature',
            points: -200,
            type: 'spent',
            description: 'Unlocked advanced analytics dashboard',
            source: 'Store'
          },
          {
            id: '4',
            date: new Date('2024-01-12'),
            action: 'Achievement Bonus',
            points: 1000,
            type: 'bonus',
            description: 'Reached 1000 hours of active participation',
            source: 'Achievement System'
          },
          {
            id: '5',
            date: new Date('2024-01-11'),
            action: 'Violation Warning',
            points: -50,
            type: 'penalty',
            description: 'Minor community guideline violation',
            source: 'Moderation'
          }
        ],
        achievements: [
          {
            id: '1',
            name: 'Space Explorer',
            description: 'Completed 50 space missions',
            icon: '🚀',
            earnedDate: new Date('2024-01-10'),
            karmaReward: 1000
          },
          {
            id: '2',
            name: 'Community Helper',
            description: 'Helped 100 users',
            icon: '🤝',
            earnedDate: new Date('2024-01-08'),
            karmaReward: 500
          },
          {
            id: '3',
            name: 'Knowledge Seeker',
            description: 'Completed all tutorial modules',
            icon: '📚',
            earnedDate: new Date('2024-01-05'),
            karmaReward: 300
          }
        ],
        recentActivity: [
          {
            id: '1',
            date: new Date('2024-01-15'),
            action: 'Completed Mission: Space Exploration',
            points: 500,
            type: 'earned',
            description: 'Successfully completed a complex space exploration mission',
            source: 'Mission System'
          },
          {
            id: '2',
            date: new Date('2024-01-14'),
            action: 'Helped New User',
            points: 100,
            type: 'earned',
            description: 'Provided assistance to a new OASIS user',
            source: 'Community'
          }
        ],
        monthlyStats: {
          earned: 2500,
          spent: 300,
          net: 2200
        }
      };
      return demoData;
    }
  );

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'earned': return 'success';
      case 'spent': return 'error';
      case 'bonus': return 'warning';
      case 'penalty': return 'error';
      default: return 'default';
    }
  };

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'earned': return <TrendingUp />;
      case 'spent': return <TrendingDown />;
      case 'bonus': return <Star />;
      case 'penalty': return <Cancel />;
      default: return <Timeline />;
    }
  };

  const formatDate = (date: Date) => {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  };

  if (isLoading) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Loading karma details...</Typography>
      </Box>
    );
  }

  if (error || !data) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography color="error">Error loading karma details</Typography>
        <Button onClick={() => navigate('/karma')} startIcon={<ArrowBack />}>
          Back to Karma
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        {/* Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
          <IconButton onClick={() => navigate('/karma')} sx={{ mr: 2 }}>
            <ArrowBack />
          </IconButton>
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h4" gutterBottom>
              Karma Profile
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Track your karma journey and achievements
            </Typography>
          </Box>
          <Button
            variant="outlined"
            startIcon={<Edit />}
            onClick={() => setEditDialogOpen(true)}
            sx={{ mr: 1 }}
          >
            Edit Profile
          </Button>
        </Box>

        {/* Karma Overview */}
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Star color="primary" sx={{ mr: 1 }} />
                  <Typography variant="h6">Total Karma</Typography>
                </Box>
                <Typography variant="h3" color="primary">
                  {data.totalKarma.toLocaleString()}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Lifetime accumulated karma
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <EmojiEvents color="secondary" sx={{ mr: 1 }} />
                  <Typography variant="h6">Current Level</Typography>
                </Box>
                <Typography variant="h3" color="secondary">
                  {data.currentLevel}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {data.levelName}
                </Typography>
                <Box sx={{ mt: 2 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Progress to Level {data.currentLevel + 1}</Typography>
                    <Typography variant="body2">{data.progressToNextLevel}%</Typography>
                  </Box>
                  <LinearProgress variant="determinate" value={data.progressToNextLevel} />
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <TrendingUp color="success" sx={{ mr: 1 }} />
                  <Typography variant="h6">This Month</Typography>
                </Box>
                <Typography variant="h4" color="success.main">
                  +{data.monthlyStats.net.toLocaleString()}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Net karma earned
                </Typography>
                <Box sx={{ mt: 1 }}>
                  <Typography variant="body2" color="success.main">
                    Earned: +{data.monthlyStats.earned.toLocaleString()}
                  </Typography>
                  <Typography variant="body2" color="error.main">
                    Spent: -{Math.abs(data.monthlyStats.spent).toLocaleString()}
                  </Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Achievements */}
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              <EmojiEvents sx={{ mr: 1, verticalAlign: 'middle' }} />
              Achievements
            </Typography>
            <Grid container spacing={2}>
              {data.achievements.map((achievement) => (
                <Grid item xs={12} sm={6} md={4} key={achievement.id}>
                  <Paper sx={{ p: 2, textAlign: 'center' }}>
                    <Typography variant="h4" sx={{ mb: 1 }}>
                      {achievement.icon}
                    </Typography>
                    <Typography variant="h6" gutterBottom>
                      {achievement.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                      {achievement.description}
                    </Typography>
                    <Chip
                      label={`+${achievement.karmaReward} Karma`}
                      color="primary"
                      size="small"
                    />
                    <Typography variant="caption" display="block" sx={{ mt: 1 }}>
                      Earned: {formatDate(achievement.earnedDate)}
                    </Typography>
                  </Paper>
                </Grid>
              ))}
            </Grid>
          </CardContent>
        </Card>

        {/* Recent Activity */}
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              <History sx={{ mr: 1, verticalAlign: 'middle' }} />
              Recent Activity
            </Typography>
            <List>
              {data.recentActivity.map((entry, index) => (
                <React.Fragment key={entry.id}>
                  <ListItem>
                    <ListItemIcon>
                      <Avatar sx={{ bgcolor: `${getTypeColor(entry.type)}.main` }}>
                        {getTypeIcon(entry.type)}
                      </Avatar>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="subtitle1">
                            {entry.action}
                          </Typography>
                          <Chip
                            label={`${entry.points > 0 ? '+' : ''}${entry.points}`}
                            color={getTypeColor(entry.type)}
                            size="small"
                          />
                        </Box>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="text.secondary">
                            {entry.description}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {formatDate(entry.date)} • {entry.source}
                          </Typography>
                        </Box>
                      }
                    />
                  </ListItem>
                  {index < data.recentActivity.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          </CardContent>
        </Card>

        {/* Full Karma History */}
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              <Timeline sx={{ mr: 1, verticalAlign: 'middle' }} />
              Complete Karma History
            </Typography>
            <List>
              {data.karmaHistory.map((entry, index) => (
                <React.Fragment key={entry.id}>
                  <ListItem>
                    <ListItemIcon>
                      <Avatar sx={{ bgcolor: `${getTypeColor(entry.type)}.main` }}>
                        {getTypeIcon(entry.type)}
                      </Avatar>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="subtitle1">
                            {entry.action}
                          </Typography>
                          <Chip
                            label={`${entry.points > 0 ? '+' : ''}${entry.points}`}
                            color={getTypeColor(entry.type)}
                            size="small"
                          />
                        </Box>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="text.secondary">
                            {entry.description}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {formatDate(entry.date)} • {entry.source}
                          </Typography>
                        </Box>
                      }
                    />
                  </ListItem>
                  {index < data.karmaHistory.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          </CardContent>
        </Card>
      </motion.div>
    </Box>
  );
};

export default KarmaDetailPage;
