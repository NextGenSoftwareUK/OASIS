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
  Flag,
  Assignment,
  Star,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { questService } from '../services';
import { Quest } from '../types/star';
import { toast } from 'react-hot-toast';

const QuestDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [quest, setQuest] = useState<Quest | null>(null);

  const queryClient = useQueryClient();

  // Fetch quest details
  const { data: questData, isLoading, error, refetch } = useQuery(
    ['quest', id],
    async () => {
      if (!id) throw new Error('Quest ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await questService.getAll();
      const foundQuest = response.result?.find((q: Quest) => q.id === id);
      if (!foundQuest) throw new Error('Quest not found');
      return { result: foundQuest };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setQuest(data.result);
        }
      },
    }
  );

  // Update quest mutation
  const updateQuestMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('Quest ID is required');
      return await questService.update(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['quest', id]);
        queryClient.invalidateQueries('allQuests');
        toast.success('Quest updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update quest');
        console.error('Update quest error:', error);
      },
    }
  );

  // Delete quest mutation
  const deleteQuestMutation = useMutation(
    async () => {
      if (!id) throw new Error('Quest ID is required');
      return await questService.delete(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allQuests');
        toast.success('Quest deleted successfully!');
        navigate('/quests');
      },
      onError: (error: any) => {
        toast.error('Failed to delete quest');
        console.error('Delete quest error:', error);
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
    updateQuestMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteQuestMutation.mutate();
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

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty.toLowerCase()) {
      case 'hard': return 'error';
      case 'medium': return 'warning';
      case 'easy': return 'success';
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
          Failed to load quest details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!quest) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Quest not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/quests')} sx={{ mt: 2 }}>
          Back to Quests
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/quests')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {quest.title}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {quest.category} • {quest.difficulty} Difficulty
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
        {/* Quest Info */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Quest Description
              </Typography>
              <Typography variant="body1" paragraph>
                {quest.description}
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
                    label={quest.category}
                    color="primary"
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Flag sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Difficulty
                    </Typography>
                  </Box>
                  <Chip
                    label={quest.difficulty}
                    color={getDifficultyColor(quest.difficulty || 'Unknown') as any}
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
                    {formatDate(quest.dueDate ? quest.dueDate.toISOString() : new Date().toISOString())}
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
                    label={quest.status}
                    color={getStatusColor(quest.status || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Quest Stats and Actions */}
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
                        {quest.progress || 0}%
                      </Typography>
                    </Box>
                    <LinearProgress
                      variant="determinate"
                      value={quest.progress || 0}
                      sx={{ height: 8, borderRadius: 4 }}
                    />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    {quest.completedTasks || 0} of {quest.totalTasks || 0} tasks completed
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
                        <Star />
                      </ListItemIcon>
                      <ListItemText
                        primary="Karma Points"
                        secondary={quest.karmaReward?.toLocaleString() || '0'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <TrendingUp />
                      </ListItemIcon>
                      <ListItemText
                        primary="Experience"
                        secondary={quest.xpReward?.toLocaleString() || '0'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Assignment />
                      </ListItemIcon>
                      <ListItemText
                        primary="Items"
                        secondary={quest.itemRewards?.length || 0}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Quest Info */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Assignment sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Quest Info
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Person />
                      </ListItemIcon>
                      <ListItemText
                        primary="Creator"
                        secondary={quest.creator || 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <CalendarToday />
                      </ListItemIcon>
                      <ListItemText
                        primary="Created"
                        secondary={quest.createdDate ? formatDate(quest.createdDate.toISOString()) : 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Schedule />
                      </ListItemIcon>
                      <ListItemText
                        primary="Duration"
                        secondary={quest.estimatedDuration || 'Unknown'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>

        {/* Quest Steps */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <Assignment sx={{ mr: 1, verticalAlign: 'middle' }} />
                Quest Steps
              </Typography>
              <Stepper orientation="vertical">
                {quest.steps?.map((step, index) => (
                  <Step key={index} active={index <= (quest.completedSteps || 0)}>
                    <StepLabel>
                      {step}
                    </StepLabel>
                    <StepContent>
                      <Typography variant="body2" color="text.secondary">
                        Step {index + 1} of {quest.steps?.length || 0}
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
        <DialogTitle>Edit Quest</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Title"
                value={quest.title}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={quest.description}
                multiline
                rows={3}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Category"
                value={quest.category}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Difficulty"
                value={quest.difficulty}
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
            disabled={updateQuestMutation.isLoading}
          >
            {updateQuestMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Quest</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{quest.title}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteQuestMutation.isLoading}
          >
            {deleteQuestMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share Quest</DialogTitle>
        <DialogContent>
          <Typography>
            Share this quest with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/quests/${quest.id}`}
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
              navigator.clipboard.writeText(`${window.location.origin}/quests/${quest.id}`);
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

export default QuestDetailPage;
