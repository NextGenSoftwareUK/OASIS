import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  Alert,
  Divider,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Paper,
  Avatar,
  Badge,
  LinearProgress,
  Stepper,
  Step,
  StepLabel,
  StepContent,
} from '@mui/material';
import {
  ArrowBack,
  Edit,
  Delete,
  Share,
  PlayArrow,
  Pause,
  CheckCircle,
  Schedule,
  Person,
  CalendarToday,
  Category,
  Code,
  Security,
  Speed,
  Memory,
  Storage,
  AttachMoney,
  TrendingUp,
  Star,
  Flag,
  Assignment,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { missionService } from '../services';
import { Mission } from '../types/star';
import { toast } from 'react-hot-toast';

const MissionDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [mission, setMission] = useState<Mission | null>(null);

  const queryClient = useQueryClient();

  // Fetch mission details
  const { data: missionData, isLoading, error, refetch } = useQuery(
    ['mission', id],
    async () => {
      if (!id) throw new Error('Mission ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await missionService.getAll();
      const foundMission = response.result?.find((m: Mission) => m.id === id);
      if (!foundMission) throw new Error('Mission not found');
      return { result: foundMission };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setMission(data.result);
        }
      },
    }
  );

  // Update mission mutation
  const updateMissionMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('Mission ID is required');
      return await missionService.update(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['mission', id]);
        queryClient.invalidateQueries('allMissions');
        toast.success('Mission updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update mission');
        console.error('Update mission error:', error);
      },
    }
  );

  // Delete mission mutation
  const deleteMissionMutation = useMutation(
    async () => {
      if (!id) throw new Error('Mission ID is required');
      return await missionService.delete(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allMissions');
        toast.success('Mission deleted successfully!');
        navigate('/missions');
      },
      onError: (error: any) => {
        toast.error('Failed to delete mission');
        console.error('Delete mission error:', error);
      },
    }
  );

  const handleEdit = () => {
    setEditDialogOpen(true);
  };

  const handleDelete = () => {
    setDeleteDialogOpen(true);
  };

  const handleShare = () => {
    setShareDialogOpen(true);
  };

  const handleEditSubmit = () => {
    updateMissionMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteMissionMutation.mutate();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active': return 'success';
      case 'completed': return 'primary';
      case 'paused': return 'warning';
      case 'failed': return 'error';
      case 'pending': return 'info';
      default: return 'default';
    }
  };

  const getPriorityColor = (priority: string) => {
    switch (priority.toLowerCase()) {
      case 'high': return 'error';
      case 'medium': return 'warning';
      case 'low': return 'success';
      default: return 'default';
    }
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <CircularProgress size={60} />
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load mission details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!mission) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Mission not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/missions')} sx={{ mt: 2 }}>
          Back to Missions
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/missions')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {mission.title}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {mission.category} • {mission.priority} Priority
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<Share />}
            onClick={handleShare}
          >
            Share
          </Button>
          <Button
            variant="outlined"
            startIcon={<Edit />}
            onClick={handleEdit}
          >
            Edit
          </Button>
          <Button
            variant="outlined"
            color="error"
            startIcon={<Delete />}
            onClick={handleDelete}
          >
            Delete
          </Button>
        </Box>
      </Box>

      <Grid container spacing={3}>
        {/* Mission Info */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Mission Description
              </Typography>
              <Typography variant="body1" paragraph>
                {mission.description}
              </Typography>
              
              <Divider sx={{ my: 2 }} />
              
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Category sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Category
                    </Typography>
                  </Box>
                  <Chip
                    label={mission.category}
                    color="primary"
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Flag sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Priority
                    </Typography>
                  </Box>
                  <Chip
                    label={mission.priority}
                    color={getPriorityColor(mission.priority || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Schedule sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Due Date
                    </Typography>
                  </Box>
                  <Typography variant="body1">
                    {formatDate(mission.dueDate ? mission.dueDate.toISOString() : new Date().toISOString())}
                  </Typography>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <CheckCircle sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Status
                    </Typography>
                  </Box>
                  <Chip
                    label={mission.status}
                    color={getStatusColor(mission.status || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Mission Stats and Actions */}
        <Grid item xs={12} md={4}>
          <Grid container spacing={2}>
            {/* Progress Card */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <TrendingUp sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Progress
                  </Typography>
                  <Box sx={{ mb: 2 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                      <Typography variant="body2">
                        Completion
                      </Typography>
                      <Typography variant="body2">
                        {mission.progress || 0}%
                      </Typography>
                    </Box>
                    <LinearProgress
                      variant="determinate"
                      value={mission.progress || 0}
                      sx={{ height: 8, borderRadius: 4 }}
                    />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    {mission.completedTasks || 0} of {mission.totalTasks || 0} tasks completed
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            {/* Rewards Card */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <AttachMoney sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Rewards
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <AttachMoney />
                      </ListItemIcon>
                      <ListItemText
                        primary="Karma Points"
                        secondary={mission.karmaReward?.toLocaleString() || '0'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Star />
                      </ListItemIcon>
                      <ListItemText
                        primary="Experience"
                        secondary={mission.xpReward?.toLocaleString() || '0'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Assignment />
                      </ListItemIcon>
                      <ListItemText
                        primary="Items"
                        secondary={mission.itemRewards?.length || 0}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Assignee Info */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Person sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Assignee
                  </Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <Avatar sx={{ mr: 2 }}>
                      {mission.assignee?.charAt(0) || 'U'}
                    </Avatar>
                    <Box>
                      <Typography variant="body1">
                        {mission.assignee || 'Unassigned'}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {mission.assignee ? 'Assigned' : 'Available'}
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>

        {/* Mission Steps */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <Assignment sx={{ mr: 1, verticalAlign: 'middle' }} />
                Mission Steps
              </Typography>
              <Stepper orientation="vertical">
                {mission.steps?.map((step, index) => (
                  <Step key={index} active={index <= (mission.completedSteps || 0)}>
                    <StepLabel>
                      {step}
                    </StepLabel>
                    <StepContent>
                      <Typography variant="body2" color="text.secondary">
                        Step {index + 1} of {mission.steps?.length || 0}
                      </Typography>
                    </StepContent>
                  </Step>
                )) || (
                  <Step>
                    <StepLabel>No steps defined</StepLabel>
                  </Step>
                )}
              </Stepper>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Edit Dialog */}
      <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Mission</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Title"
                value={mission.title}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={mission.description}
                multiline
                rows={3}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Category"
                value={mission.category}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Priority"
                value={mission.priority}
                disabled
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleEditSubmit}
            variant="contained"
            disabled={updateMissionMutation.isLoading}
          >
            {updateMissionMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Mission</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{mission.title}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteMissionMutation.isLoading}
          >
            {deleteMissionMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share Mission</DialogTitle>
        <DialogContent>
          <Typography>
            Share this mission with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/missions/${mission.id}`}
            sx={{ mt: 2 }}
            InputProps={{
              readOnly: true,
            }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShareDialogOpen(false)}>Close</Button>
          <Button
            onClick={() => {
              navigator.clipboard.writeText(`${window.location.origin}/missions/${mission.id}`);
              toast.success('Link copied to clipboard!');
              setShareDialogOpen(false);
            }}
            variant="contained"
          >
            Copy Link
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default MissionDetailPage;
