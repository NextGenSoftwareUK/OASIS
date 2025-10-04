/**
 * Missions Page
 * Complete Mission management interface
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
  Flag,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { missionService } from '../services';
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
      id={`mission-tabpanel-${index}`}
      aria-labelledby={`mission-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const MissionsPage: React.FC = () => {
  const navigate = useNavigate();
  const { isDemoMode } = useDemoMode();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState('all');
  const [sortBy, setSortBy] = useState('newest');
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [selectedMission, setSelectedMission] = useState<any>(null);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  // Fetch Missions
  const { data: missions, isLoading, error } = useQuery('missions', missionService.getAll);

  // Create Mission mutation
  const createMissionMutation = useMutation(
    async (missionData: any) => {
      const response = await missionService.create(missionData);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('missions');
        toast.success('Mission created successfully!');
        setCreateDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to create Mission: ' + error.message);
      },
    }
  );

  // Start Mission mutation
  const startMissionMutation = useMutation(
    async (missionId: string) => {
      const response = await missionService.start(missionId);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('missions');
        toast.success('Mission started successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to start Mission: ' + error.message);
      },
    }
  );

  // Complete Mission mutation
  const completeMissionMutation = useMutation(
    async (missionId: string) => {
      const response = await missionService.complete(missionId);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('missions');
        toast.success('Mission completed successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to complete Mission: ' + error.message);
      },
    }
  );

  const handleCreateMission = (missionData: any) => {
    createMissionMutation.mutate(missionData);
  };

  const handleStartMission = (missionId: string) => {
    startMissionMutation.mutate(missionId);
  };

  const handleCompleteMission = (missionId: string) => {
    completeMissionMutation.mutate(missionId);
  };

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, mission: any) => {
    setAnchorEl(event.currentTarget);
    setSelectedMission(mission);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedMission(null);
  };

  const filteredMissions = missions?.result?.filter((mission: any) => {
    const matchesSearch = mission.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         mission.description?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesFilter = filterType === 'all' || mission.status === filterType;
    return matchesSearch && matchesFilter;
  }) || [];

  const sortedMissions = [...filteredMissions].sort((a: any, b: any) => {
    switch (sortBy) {
      case 'newest':
        return new Date(b.createdOn || 0).getTime() - new Date(a.createdOn || 0).getTime();
      case 'oldest':
        return new Date(a.createdOn || 0).getTime() - new Date(b.createdOn || 0).getTime();
      case 'name':
        return (a.name || '').localeCompare(b.name || '');
      case 'priority':
        return (b.priority || 0) - (a.priority || 0);
      default:
        return 0;
    }
  });

  const missionStats = {
    total: missions?.result?.length || 0,
    active: missions?.result?.filter((mission: any) => mission.status === 'active').length || 0,
    completed: missions?.result?.filter((mission: any) => mission.status === 'completed').length || 0,
    averagePriority: missions?.result?.reduce((sum: number, mission: any) => sum + (mission.priority || 0), 0) / (missions?.result?.length || 1) || 0,
  };

  return (
    <Box sx={{ flexGrow: 1, p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" component="h1" gutterBottom>
            Missions
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage your Missions and Tasks
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => setCreateDialogOpen(true)}
          sx={{ borderRadius: 2 }}
        >
          Create Mission
        </Button>
      </Box>

      {/* Stats Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Flag color="primary" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{missionStats.total}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total Missions
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
                  <Typography variant="h6">{missionStats.active}</Typography>
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
                  <Typography variant="h6">{missionStats.completed}</Typography>
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
                  <Typography variant="h6">{missionStats.averagePriority.toFixed(1)}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Avg Priority
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
                placeholder="Search Missions..."
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
                  <MenuItem value="priority">Priority</MenuItem>
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

      {/* Missions Grid */}
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
                        Loading Mission details...
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
                  Failed to load Missions
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {error instanceof Error ? error.message : 'An error occurred'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ) : sortedMissions.length === 0 ? (
          <Grid item xs={12}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 4 }}>
                <Flag sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                <Typography variant="h6" gutterBottom>
                  No Missions found
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                  {searchTerm ? 'Try adjusting your search criteria' : 'Create your first Mission to get started'}
                </Typography>
                <Button
                  variant="contained"
                  startIcon={<Add />}
                  onClick={() => setCreateDialogOpen(true)}
                >
                  Create Mission
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ) : (
          sortedMissions.map((mission: any) => (
            <Grid item xs={12} sm={6} md={4} key={mission.id}>
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3 }}
              >
                <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                  <CardMedia
                    component="img"
                    height="200"
                    image={mission.imageUrl || '/api/placeholder/400/200'}
                    alt={mission.name}
                    sx={{ objectFit: 'cover' }}
                  />
                  <CardContent sx={{ flexGrow: 1 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                      <Typography variant="h6" component="h3" noWrap>
                        {mission.name}
                      </Typography>
                      <IconButton
                        size="small"
                        onClick={(e) => handleMenuClick(e, mission)}
                      >
                        <MoreVert />
                      </IconButton>
                    </Box>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      {mission.description}
                    </Typography>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Chip
                        label={mission.status || 'Draft'}
                        size="small"
                        color={mission.status === 'completed' ? 'success' : mission.status === 'active' ? 'primary' : 'default'}
                        variant="outlined"
                      />
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Star sx={{ fontSize: 16, mr: 0.5, color: 'warning.main' }} />
                        <Typography variant="body2">
                          Priority: {mission.priority || 1}/5
                        </Typography>
                      </Box>
                    </Box>
                    {mission.progress !== undefined && (
                      <Box sx={{ mb: 2 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                          <Typography variant="body2" color="text.secondary">
                            Progress
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            {mission.progress}%
                          </Typography>
                        </Box>
                        <LinearProgress variant="determinate" value={mission.progress} />
                      </Box>
                    )}
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography variant="body2" color="text.secondary">
                        {mission.rewards || 'No rewards'}
                      </Typography>
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mr: 1 }}>
                          {mission.duration || 'Unknown'} duration
                        </Typography>
                      </Box>
                    </Box>
                  </CardContent>
                  <Divider />
                  <CardActions sx={{ justifyContent: 'space-between', p: 2 }}>
                    <Button
                      size="small"
                      startIcon={<Visibility />}
                      onClick={() => navigate(`/missions/${mission.id}`)}
                    >
                      View
                    </Button>
                    <Box>
                      {mission.status === 'draft' && (
                        <Button
                          size="small"
                          startIcon={<PlayArrow />}
                          onClick={() => handleStartMission(mission.id)}
                          disabled={startMissionMutation.isLoading}
                        >
                          Start
                        </Button>
                      )}
                      {mission.status === 'active' && (
                        <Button
                          size="small"
                          startIcon={<CheckCircle />}
                          onClick={() => handleCompleteMission(mission.id)}
                          disabled={completeMissionMutation.isLoading}
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

      {/* Create Mission Dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Create New Mission</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Mission Name"
                placeholder="Enter Mission name"
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                placeholder="Enter Mission description"
                multiline
                rows={3}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Priority</InputLabel>
                <Select defaultValue="1">
                  <MenuItem value="1">Low</MenuItem>
                  <MenuItem value="2">Medium</MenuItem>
                  <MenuItem value="3">High</MenuItem>
                  <MenuItem value="4">Critical</MenuItem>
                  <MenuItem value="5">Emergency</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Duration"
                placeholder="e.g., 2 hours"
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Rewards"
                placeholder="Enter Mission rewards"
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
            onClick={() => handleCreateMission({})}
            disabled={createMissionMutation.isLoading}
          >
            {createMissionMutation.isLoading ? 'Creating...' : 'Create Mission'}
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
          if (selectedMission) navigate(`/missions/${selectedMission.id}`);
          handleMenuClose();
        }}>
          <Visibility sx={{ mr: 1 }} />
          View Details
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedMission) handleStartMission(selectedMission.id);
          handleMenuClose();
        }}>
          <PlayArrow sx={{ mr: 1 }} />
          Start Mission
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedMission) handleCompleteMission(selectedMission.id);
          handleMenuClose();
        }}>
          <CheckCircle sx={{ mr: 1 }} />
          Complete Mission
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
        aria-label="create mission"
        sx={{ position: 'fixed', bottom: 16, right: 16 }}
        onClick={() => setCreateDialogOpen(true)}
      >
        <Add />
      </Fab>
    </Box>
  );
};

export default MissionsPage;