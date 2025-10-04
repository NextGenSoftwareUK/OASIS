/**
 * Quests Page
 * Complete Quest management interface
 */

import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useDemoMode } from '../contexts/DemoModeContext';
import {
  Box,
  Typography,
  Button,
  Card,
  CardContent,
  Grid,
  Chip,
  IconButton,
  Menu,
  MenuItem,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  Fab,
  Tooltip,
  Tabs,
  Tab,
  Badge,
  Stack,
  Avatar,
  CardMedia,
  CardActions,
  Divider,
  LinearProgress,
} from '@mui/material';
import {
  Add,
  MoreVert,
  PlayArrow,
  Pause,
  Download,
  Upload,
  Delete,
  Edit,
  Visibility,
  Assignment,
  FilterList,
  Search,
  Help,
  Info,
  Build,
  Star,
  CheckCircle,
  Schedule,
  TrendingUp,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { questService } from '../services';
import { toast } from 'react-hot-toast';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`quest-tabpanel-${index}`}
      aria-labelledby={`quest-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const QuestsPage: React.FC = () => {
  const navigate = useNavigate();
  const { isDemoMode } = useDemoMode();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState('all');
  const [sortBy, setSortBy] = useState('newest');
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [selectedQuest, setSelectedQuest] = useState<any>(null);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  // Fetch Quests
  const { data: quests, isLoading, error } = useQuery('quests', questService.getAll);

  // Create Quest mutation
  const createQuestMutation = useMutation(
    async (questData: any) => {
      const response = await questService.create(questData);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('quests');
        toast.success('Quest created successfully!');
        setCreateDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to create Quest: ' + error.message);
      },
    }
  );

  // Start Quest mutation
  const startQuestMutation = useMutation(
    async (questId: string) => {
      const response = await questService.start(questId);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('quests');
        toast.success('Quest started successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to start Quest: ' + error.message);
      },
    }
  );

  // Complete Quest mutation
  const completeQuestMutation = useMutation(
    async (questId: string) => {
      const response = await questService.complete(questId);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('quests');
        toast.success('Quest completed successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to complete Quest: ' + error.message);
      },
    }
  );

  const handleCreateQuest = (questData: any) => {
    createQuestMutation.mutate(questData);
  };

  const handleStartQuest = (questId: string) => {
    startQuestMutation.mutate(questId);
  };

  const handleCompleteQuest = (questId: string) => {
    completeQuestMutation.mutate(questId);
  };

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, quest: any) => {
    setAnchorEl(event.currentTarget);
    setSelectedQuest(quest);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedQuest(null);
  };

  const filteredQuests = quests?.result?.filter((quest: any) => {
    const matchesSearch = quest.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         quest.description?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesFilter = filterType === 'all' || quest.status === filterType;
    return matchesSearch && matchesFilter;
  }) || [];

  const sortedQuests = [...filteredQuests].sort((a: any, b: any) => {
    switch (sortBy) {
      case 'newest':
        return new Date(b.createdOn || 0).getTime() - new Date(a.createdOn || 0).getTime();
      case 'oldest':
        return new Date(a.createdOn || 0).getTime() - new Date(b.createdOn || 0).getTime();
      case 'name':
        return (a.name || '').localeCompare(b.name || '');
      case 'difficulty':
        return (b.difficulty || 0) - (a.difficulty || 0);
      default:
        return 0;
    }
  });

  const questStats = {
    total: quests?.result?.length || 0,
    active: quests?.result?.filter((quest: any) => quest.status === 'active').length || 0,
    completed: quests?.result?.filter((quest: any) => quest.status === 'completed').length || 0,
    averageDifficulty: quests?.result?.reduce((sum: number, quest: any) => sum + (quest.difficulty || 0), 0) / (quests?.result?.length || 1) || 0,
  };

  return (
    <Box sx={{ flexGrow: 1, p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" component="h1" gutterBottom>
            Quests
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage your Quests and Adventures
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => setCreateDialogOpen(true)}
          sx={{ borderRadius: 2 }}
        >
          Create Quest
        </Button>
      </Box>

      {/* Stats Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Assignment color="primary" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{questStats.total}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total Quests
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <PlayArrow color="success" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{questStats.active}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Active
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <CheckCircle color="info" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{questStats.completed}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Completed
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <TrendingUp color="warning" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{questStats.averageDifficulty.toFixed(1)}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Avg Difficulty
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Filters and Search */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                placeholder="Search Quests..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                InputProps={{
                  startAdornment: <Search sx={{ mr: 1, color: 'text.secondary' }} />,
                }}
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <FormControl fullWidth>
                <InputLabel>Filter by Status</InputLabel>
                <Select
                  value={filterType}
                  onChange={(e) => setFilterType(e.target.value)}
                >
                  <MenuItem value="all">All Status</MenuItem>
                  <MenuItem value="draft">Draft</MenuItem>
                  <MenuItem value="active">Active</MenuItem>
                  <MenuItem value="completed">Completed</MenuItem>
                  <MenuItem value="paused">Paused</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={3}>
              <FormControl fullWidth>
                <InputLabel>Sort by</InputLabel>
                <Select
                  value={sortBy}
                  onChange={(e) => setSortBy(e.target.value)}
                >
                  <MenuItem value="newest">Newest</MenuItem>
                  <MenuItem value="oldest">Oldest</MenuItem>
                  <MenuItem value="name">Name</MenuItem>
                  <MenuItem value="difficulty">Difficulty</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={2}>
              <Button
                fullWidth
                variant="outlined"
                startIcon={<FilterList />}
              >
                More Filters
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Quests Grid */}
      <Grid container spacing={3}>
        {isLoading ? (
          Array.from({ length: 6 }).map((_, index) => (
            <Grid item xs={12} sm={6} md={4} key={index}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Avatar sx={{ mr: 2 }} />
                    <Box sx={{ flexGrow: 1 }}>
                      <Typography variant="h6" sx={{ mb: 1 }}>
                        Loading...
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Loading Quest details...
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          ))
        ) : error ? (
          <Grid item xs={12}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 4 }}>
                <Typography variant="h6" color="error" gutterBottom>
                  Failed to load Quests
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {error instanceof Error ? error.message : 'An error occurred'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ) : sortedQuests.length === 0 ? (
          <Grid item xs={12}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 4 }}>
                <Assignment sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                <Typography variant="h6" gutterBottom>
                  No Quests found
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                  {searchTerm ? 'Try adjusting your search criteria' : 'Create your first Quest to get started'}
                </Typography>
                <Button
                  variant="contained"
                  startIcon={<Add />}
                  onClick={() => setCreateDialogOpen(true)}
                >
                  Create Quest
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ) : (
          sortedQuests.map((quest: any) => (
            <Grid item xs={12} sm={6} md={4} key={quest.id}>
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3 }}
              >
                <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                  <CardMedia
                    component="img"
                    height="200"
                    image={quest.imageUrl || '/api/placeholder/400/200'}
                    alt={quest.name}
                    sx={{ objectFit: 'cover' }}
                  />
                  <CardContent sx={{ flexGrow: 1 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                      <Typography variant="h6" component="h3" noWrap>
                        {quest.name}
                      </Typography>
                      <IconButton
                        size="small"
                        onClick={(e) => handleMenuClick(e, quest)}
                      >
                        <MoreVert />
                      </IconButton>
                    </Box>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      {quest.description}
                    </Typography>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Chip
                        label={quest.status || 'Draft'}
                        size="small"
                        color={quest.status === 'completed' ? 'success' : quest.status === 'active' ? 'primary' : 'default'}
                        variant="outlined"
                      />
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Star sx={{ fontSize: 16, mr: 0.5, color: 'warning.main' }} />
                        <Typography variant="body2">
                          {quest.difficulty || 1}/5
                        </Typography>
                      </Box>
                    </Box>
                    {quest.progress !== undefined && (
                      <Box sx={{ mb: 2 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                          <Typography variant="body2" color="text.secondary">
                            Progress
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            {quest.progress}%
                          </Typography>
                        </Box>
                        <LinearProgress variant="determinate" value={quest.progress} />
                      </Box>
                    )}
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography variant="body2" color="text.secondary">
                        {quest.rewards || 'No rewards'}
                      </Typography>
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mr: 1 }}>
                          {quest.duration || 'Unknown'} duration
                        </Typography>
                      </Box>
                    </Box>
                  </CardContent>
                  <Divider />
                  <CardActions sx={{ justifyContent: 'space-between', p: 2 }}>
                    <Button
                      size="small"
                      startIcon={<Visibility />}
                      onClick={() => navigate(`/quests/${quest.id}`)}
                    >
                      View
                    </Button>
                    <Box>
                      {quest.status === 'draft' && (
                        <Button
                          size="small"
                          startIcon={<PlayArrow />}
                          onClick={() => handleStartQuest(quest.id)}
                          disabled={startQuestMutation.isLoading}
                        >
                          Start
                        </Button>
                      )}
                      {quest.status === 'active' && (
                        <Button
                          size="small"
                          startIcon={<CheckCircle />}
                          onClick={() => handleCompleteQuest(quest.id)}
                          disabled={completeQuestMutation.isLoading}
                        >
                          Complete
                        </Button>
                      )}
                    </Box>
                  </CardActions>
                </Card>
              </motion.div>
            </Grid>
          ))
        )}
      </Grid>

      {/* Create Quest Dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Create New Quest</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Quest Name"
                placeholder="Enter Quest name"
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                placeholder="Enter Quest description"
                multiline
                rows={3}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Difficulty</InputLabel>
                <Select defaultValue="1">
                  <MenuItem value="1">Easy</MenuItem>
                  <MenuItem value="2">Medium</MenuItem>
                  <MenuItem value="3">Hard</MenuItem>
                  <MenuItem value="4">Expert</MenuItem>
                  <MenuItem value="5">Master</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Duration"
                placeholder="e.g., 30 minutes"
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Rewards"
                placeholder="Enter Quest rewards"
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Image URL"
                placeholder="https://example.com/image.png"
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={() => handleCreateQuest({})}
            disabled={createQuestMutation.isLoading}
          >
            {createQuestMutation.isLoading ? 'Creating...' : 'Create Quest'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Context Menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItem onClick={() => {
          if (selectedQuest) navigate(`/quests/${selectedQuest.id}`);
          handleMenuClose();
        }}>
          <Visibility sx={{ mr: 1 }} />
          View Details
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedQuest) handleStartQuest(selectedQuest.id);
          handleMenuClose();
        }}>
          <PlayArrow sx={{ mr: 1 }} />
          Start Quest
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedQuest) handleCompleteQuest(selectedQuest.id);
          handleMenuClose();
        }}>
          <CheckCircle sx={{ mr: 1 }} />
          Complete Quest
        </MenuItem>
        <MenuItem onClick={handleMenuClose}>
          <Edit sx={{ mr: 1 }} />
          Edit
        </MenuItem>
        <MenuItem onClick={handleMenuClose} sx={{ color: 'error.main' }}>
          <Delete sx={{ mr: 1 }} />
          Delete
        </MenuItem>
      </Menu>

      {/* Floating Action Button */}
      <Fab
        color="primary"
        aria-label="create quest"
        sx={{ position: 'fixed', bottom: 16, right: 16 }}
        onClick={() => setCreateDialogOpen(true)}
      >
        <Add />
      </Fab>
    </Box>
  );
};

export default QuestsPage;