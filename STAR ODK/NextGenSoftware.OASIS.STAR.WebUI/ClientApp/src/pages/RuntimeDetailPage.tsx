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
} from '@mui/material';
import {
  ArrowBack,
  Edit,
  Delete,
  Share,
  Download,
  Visibility,
  Code,
  Star,
  Person,
  CalendarToday,
  Category,
  Security,
  Speed,
  Memory,
  Storage,
  AttachMoney,
  TrendingUp,
  Flag,
  Assignment,
  PlayArrow,
  Pause,
  Stop,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starService } from '../services/starService';
import { starNetService } from '../services/starNetService';
import { Runtime } from '../types/star';
import { toast } from 'react-hot-toast';

const RuntimeDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [runtime, setRuntime] = useState<Runtime | null>(null);

  const queryClient = useQueryClient();

  // Fetch runtime details
  const { data: runtimeData, isLoading, error, refetch } = useQuery(
    ['runtime', id],
    async () => {
      if (!id) throw new Error('Runtime ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await starService.getAllRuntimes();
      const foundRuntime = response.result?.find((r: Runtime) => String((r as any).id) === String(id));
      if (!foundRuntime) throw new Error('Runtime not found');
      return { result: foundRuntime };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setRuntime(data.result);
        }
      },
    }
  );

  // Update runtime mutation
  const updateRuntimeMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('Runtime ID is required');
      return await starNetService.updateRuntime(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['runtime', id]);
        queryClient.invalidateQueries('allRuntimes');
        toast.success('Runtime updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update runtime');
        console.error('Update runtime error:', error);
      },
    }
  );

  // Delete runtime mutation
  const deleteRuntimeMutation = useMutation(
    async () => {
      if (!id) throw new Error('Runtime ID is required');
      return await starNetService.deleteRuntime(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allRuntimes');
        toast.success('Runtime deleted successfully!');
        navigate('/runtimes');
      },
      onError: (error: any) => {
        toast.error('Failed to delete runtime');
        console.error('Delete runtime error:', error);
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
    updateRuntimeMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteRuntimeMutation.mutate();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'running': return 'success';
      case 'stopped': return 'error';
      case 'paused': return 'warning';
      case 'starting': return 'info';
      default: return 'default';
    }
  };

  const getTypeColor = (type: string) => {
    switch (type.toLowerCase()) {
      case 'nodejs': return 'success';
      case 'python': return 'primary';
      case 'java': return 'warning';
      case 'dotnet': return 'secondary';
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
          Failed to load runtime details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!runtime) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Runtime not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/runtimes')} sx={{ mt: 2 }}>
          Back to Runtimes
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/runtimes')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {runtime.name}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {runtime.type} • {runtime.version}
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
        {/* Runtime Info */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Runtime Description
              </Typography>
              <Typography variant="body1" paragraph>
                {runtime.description}
              </Typography>
              
              <Divider sx={{ my: 2 }} />
              
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Category sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Type
                    </Typography>
                  </Box>
                  <Chip
                    label={runtime.type}
                    color={getTypeColor(runtime.type || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Flag sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Version
                    </Typography>
                  </Box>
                  <Chip
                    label={runtime.version}
                    color="primary"
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <PlayArrow sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Status
                    </Typography>
                  </Box>
                  <Chip
                    label={runtime.status}
                    color={getStatusColor(runtime.status || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <CalendarToday sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Created
                    </Typography>
                  </Box>
                  <Typography variant="body1">
                    {formatDate(runtime.createdDate ? runtime.createdDate.toISOString() : new Date().toISOString())}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Runtime Stats and Actions */}
        <Grid item xs={12} md={4}>
          <Grid container spacing={2}>
            {/* Performance Card */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Speed sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Performance
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Memory />
                      </ListItemIcon>
                      <ListItemText
                        primary="Memory Usage"
                        secondary={`${runtime.memoryUsage || 0} MB`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Speed />
                      </ListItemIcon>
                      <ListItemText
                        primary="CPU Usage"
                        secondary={`${runtime.cpuUsage || 0}%`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Storage />
                      </ListItemIcon>
                      <ListItemText
                        primary="Disk Usage"
                        secondary={`${runtime.diskUsage || 0} GB`}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Runtime Info */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Code sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Runtime Info
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Person />
                      </ListItemIcon>
                      <ListItemText
                        primary="Owner"
                        secondary={runtime.owner || 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <CalendarToday />
                      </ListItemIcon>
                      <ListItemText
                        primary="Last Started"
                        secondary={runtime.lastStarted ? formatDate(runtime.lastStarted) : 'Never'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Security />
                      </ListItemIcon>
                      <ListItemText
                        primary="Security Level"
                        secondary={runtime.securityLevel || 'Standard'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Actions */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Assignment sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Actions
                  </Typography>
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                    <Button
                      variant="contained"
                      startIcon={<PlayArrow />}
                      disabled={runtime.status === 'Running'}
                    >
                      Start
                    </Button>
                    <Button
                      variant="outlined"
                      startIcon={<Pause />}
                      disabled={runtime.status !== 'Running'}
                    >
                      Pause
                    </Button>
                    <Button
                      variant="outlined"
                      color="error"
                      startIcon={<Stop />}
                      disabled={runtime.status === 'Stopped'}
                    >
                      Stop
                    </Button>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>

        {/* Runtime Configuration */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <Security sx={{ mr: 1, verticalAlign: 'middle' }} />
                Runtime Configuration
              </Typography>
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Max Memory
                    </Typography>
                    <Typography variant="h6">
                      {runtime.maxMemory || 'N/A'} MB
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Max CPU
                    </Typography>
                    <Typography variant="h6">
                      {runtime.maxCpu || 'N/A'}%
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Port
                    </Typography>
                    <Typography variant="h6">
                      {runtime.port || 'N/A'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Environment
                    </Typography>
                    <Typography variant="h6">
                      {runtime.environment || 'Development'}
                    </Typography>
                  </Box>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Edit Dialog */}
      <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Runtime</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Name"
                value={runtime.name}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={runtime.description}
                multiline
                rows={3}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Type"
                value={runtime.type}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Version"
                value={runtime.version}
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
            disabled={updateRuntimeMutation.isLoading}
          >
            {updateRuntimeMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Runtime</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{runtime.name}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteRuntimeMutation.isLoading}
          >
            {deleteRuntimeMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share Runtime</DialogTitle>
        <DialogContent>
          <Typography>
            Share this runtime with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/runtimes/${runtime.id}`}
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
              navigator.clipboard.writeText(`${window.location.origin}/runtimes/${runtime.id}`);
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

export default RuntimeDetailPage;
