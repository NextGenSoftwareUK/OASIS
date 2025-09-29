import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Button,
  Grid,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  Alert,
  LinearProgress,
  Badge,
} from '@mui/material';
import {
  Assignment,
  Add,
  PlayArrow,
  Pause,
  CheckCircle,
  Star,
  Refresh,
  FilterList,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starService } from '../services/starService';
import { toast } from 'react-hot-toast';
import { useNavigate } from 'react-router-dom';

interface Quest {
  id: string;
  title: string;
  description: string;
  status: 'active' | 'completed' | 'paused' | 'draft';
  difficulty: 'easy' | 'medium' | 'hard' | 'legendary';
  karmaReward: number;
  progress: number;
  maxProgress: number;
  createdAt: string;
  updatedAt: string;
  category: string;
  tags: string[];
}

const QuestsPage: React.FC = () => {
  const navigate = useNavigate();
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [newQuest, setNewQuest] = useState({
    title: '',
    description: '',
    difficulty: 'medium',
    karmaReward: 100,
    category: 'general',
    tags: '',
  });

  const queryClient = useQueryClient();

  // Mock quest data - replace with real API calls when available
  const { data: questsData, isLoading, error, refetch } = useQuery(
    'quests',
    () => Promise.resolve({
      result: [
        {
          id: '1',
          title: 'Environmental Cleanup',
          description: 'Help clean up the virtual environment and earn karma points',
          status: 'active',
          difficulty: 'medium',
          karmaReward: 150,
          progress: 75,
          maxProgress: 100,
          createdAt: '2024-12-19T10:00:00Z',
          updatedAt: '2024-12-19T15:30:00Z',
          category: 'environment',
          tags: ['environment', 'cleanup', 'karma'],
        },
        {
          id: '2',
          title: 'Avatar Training',
          description: 'Complete basic avatar training to unlock new abilities',
          status: 'completed',
          difficulty: 'easy',
          karmaReward: 50,
          progress: 100,
          maxProgress: 100,
          createdAt: '2024-12-18T09:00:00Z',
          updatedAt: '2024-12-18T12:00:00Z',
          category: 'training',
          tags: ['training', 'avatar', 'beginner'],
        },
        {
          id: '3',
          title: 'Legendary Challenge',
          description: 'Face the ultimate challenge in the OASIS',
          status: 'paused',
          difficulty: 'legendary',
          karmaReward: 1000,
          progress: 25,
          maxProgress: 100,
          createdAt: '2024-12-17T08:00:00Z',
          updatedAt: '2024-12-19T14:00:00Z',
          category: 'challenge',
          tags: ['legendary', 'challenge', 'ultimate'],
        },
      ]
    }),
    {
      refetchInterval: 30000,
    }
  );

  // Create quest mutation
  const createQuestMutation = useMutation(
    (questData: any) => {
      // Mock API call - replace with real implementation
      return Promise.resolve({ success: true, result: { id: Date.now().toString(), ...questData } });
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('quests');
        setCreateDialogOpen(false);
        setNewQuest({
          title: '',
          description: '',
          difficulty: 'medium',
          karmaReward: 100,
          category: 'general',
          tags: '',
        });
        toast.success('Quest created successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to create quest');
      },
    }
  );

  const handleCreateQuest = () => {
    createQuestMutation.mutate({
      ...newQuest,
      tags: newQuest.tags.split(',').map(tag => tag.trim()).filter(tag => tag),
      status: 'draft',
      progress: 0,
      maxProgress: 100,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    });
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active': return 'success';
      case 'completed': return 'primary';
      case 'paused': return 'warning';
      case 'draft': return 'default';
      default: return 'default';
    }
  };

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty) {
      case 'easy': return '#4caf50';
      case 'medium': return '#ff9800';
      case 'hard': return '#f44336';
      case 'legendary': return '#9c27b0';
      default: return '#757575';
    }
  };

  const filteredQuests = questsData?.result?.filter((quest: any) => 
    filterStatus === 'all' || quest.status === filterStatus
  ) || [];

  return (
    <>
        <Box sx={{ mb: 4, mt: 4, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Box>
          <Typography variant="h4" gutterBottom className="page-heading">
            Quests
          </Typography>
          <Typography variant="subtitle1" color="text.secondary">
            Interactive quests and adventures in the OASIS
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button
            variant="outlined"
            startIcon={<FilterList />}
            onClick={() => setFilterStatus(filterStatus === 'all' ? 'active' : 'all')}
          >
            {filterStatus === 'all' ? 'All Quests' : 'Active Only'}
          </Button>
          <Button
            variant="outlined"
            startIcon={<Refresh />}
            onClick={() => refetch()}
            disabled={isLoading}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<Add />}
            onClick={() => setCreateDialogOpen(true)}
          >
            Create Quest
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load quests: {error instanceof Error ? error.message : 'Unknown error'}
        </Alert>
      )}

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {filteredQuests.map((quest: any) => (
            <Grid item xs={12} md={6} lg={4} key={quest.id}>
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3 }}
              >
                <Card 
                  sx={{ height: '100%', position: 'relative', cursor: 'pointer' }}
                  onClick={() => navigate(`/quests/${quest.id}`)}
                >
                  <CardContent>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                      <Box sx={{ flexGrow: 1 }}>
                        <Typography variant="h6" gutterBottom>
                          {quest.title}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                          {quest.description}
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        <Chip
                          label={quest.status.toUpperCase()}
                          size="small"
                          color={getStatusColor(quest.status) as any}
                          variant="outlined"
                        />
                      </Box>
                    </Box>

                    <Box sx={{ mb: 2 }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                        <Typography variant="body2" color="text.secondary">
                          Progress
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {quest.progress}/{quest.maxProgress}
                        </Typography>
                      </Box>
                      <LinearProgress
                        variant="determinate"
                        value={(quest.progress / quest.maxProgress) * 100}
                        sx={{ mb: 2 }}
                      />
                    </Box>

                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        <Chip
                          label={quest.difficulty.toUpperCase()}
                          size="small"
                          sx={{
                            bgcolor: getDifficultyColor(quest.difficulty),
                            color: 'white',
                            fontWeight: 'bold',
                          }}
                        />
                        <Chip
                          label={quest.category.toUpperCase()}
                          size="small"
                          color="primary"
                          variant="outlined"
                        />
                        <Chip
                          label={`${quest.karmaReward} Karma`}
                          size="small"
                          color="secondary"
                          variant="outlined"
                        />
                      </Box>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        {quest.status === 'active' && (
                          <IconButton size="small" color="primary">
                            <PlayArrow />
                          </IconButton>
                        )}
                        {quest.status === 'paused' && (
                          <IconButton size="small" color="warning">
                            <Pause />
                          </IconButton>
                        )}
                        {quest.status === 'completed' && (
                          <IconButton size="small" color="success">
                            <CheckCircle />
                          </IconButton>
                        )}
                      </Box>
                    </Box>

                    <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                      {quest.tags.map((tag: string, index: number) => (
                        <Chip
                          key={index}
                          label={tag}
                          size="small"
                          variant="outlined"
                          sx={{ fontSize: '0.7rem' }}
                        />
                      ))}
                    </Box>

                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                      Created: {new Date(quest.createdAt).toLocaleDateString()}
                    </Typography>
                  </CardContent>
                </Card>
              </motion.div>
            </Grid>
          ))}
        </Grid>
      )}

      {/* Create Quest Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New Quest</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Quest Title"
              value={newQuest.title}
              onChange={(e) => setNewQuest({ ...newQuest, title: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={newQuest.description}
              onChange={(e) => setNewQuest({ ...newQuest, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Difficulty"
              value={newQuest.difficulty}
              onChange={(e) => setNewQuest({ ...newQuest, difficulty: e.target.value })}
              fullWidth
              select
              SelectProps={{ native: true }}
            >
              <option value="easy">Easy</option>
              <option value="medium">Medium</option>
              <option value="hard">Hard</option>
              <option value="legendary">Legendary</option>
            </TextField>
            <TextField
              label="Karma Reward"
              type="number"
              value={newQuest.karmaReward}
              onChange={(e) => setNewQuest({ ...newQuest, karmaReward: parseInt(e.target.value) })}
              fullWidth
              required
            />
            <TextField
              label="Category"
              value={newQuest.category}
              onChange={(e) => setNewQuest({ ...newQuest, category: e.target.value })}
              fullWidth
              select
              SelectProps={{ native: true }}
              required
            >
              <option value="general">General</option>
              <option value="exploration">Exploration</option>
              <option value="combat">Combat</option>
              <option value="crafting">Crafting</option>
              <option value="social">Social</option>
              <option value="puzzle">Puzzle</option>
              <option value="collection">Collection</option>
              <option value="achievement">Achievement</option>
              <option value="story">Story</option>
              <option value="daily">Daily</option>
              <option value="weekly">Weekly</option>
              <option value="event">Event</option>
              <option value="pvp">PvP</option>
              <option value="pve">PvE</option>
              <option value="raid">Raid</option>
              <option value="dungeon">Dungeon</option>
              <option value="quest">Quest</option>
              <option value="mission">Mission</option>
              <option value="challenge">Challenge</option>
              <option value="tutorial">Tutorial</option>
            </TextField>
            <TextField
              label="Tags (comma-separated)"
              value={newQuest.tags}
              onChange={(e) => setNewQuest({ ...newQuest, tags: e.target.value })}
              fullWidth
              placeholder="environment, cleanup, karma"
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateQuest}
            variant="contained"
            disabled={createQuestMutation.isLoading}
          >
            {createQuestMutation.isLoading ? 'Creating...' : 'Create Quest'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default QuestsPage;
