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
  Public,
  Star,
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
  Bookmark,
  School,
  Satellite,
  Language,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { celestialBodyService } from '../services';
import { CelestialBody } from '../types/star';
import { toast } from 'react-hot-toast';

const CelestialBodyDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [celestialBody, setCelestialBody] = useState<CelestialBody | null>(null);

  const queryClient = useQueryClient();

  // Fetch celestial body details
  const { data: celestialBodyData, isLoading, error, refetch } = useQuery(
    ['celestialBody', id],
    async () => {
      if (!id) throw new Error('Celestial Body ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await celestialBodyService.getAll();
      const foundCelestialBody = response.result?.find((cb: CelestialBody) => cb.id === id);
      if (!foundCelestialBody) throw new Error('Celestial Body not found');
      return { result: foundCelestialBody };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setCelestialBody(data.result);
        }
      },
    }
  );

  // Update celestial body mutation
  const updateCelestialBodyMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('Celestial Body ID is required');
      return await celestialBodyService.update(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['celestialBody', id]);
        queryClient.invalidateQueries('allCelestialBodies');
        toast.success('Celestial Body updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update celestial body');
        console.error('Update celestial body error:', error);
      },
    }
  );

  // Delete celestial body mutation
  const deleteCelestialBodyMutation = useMutation(
    async () => {
      if (!id) throw new Error('Celestial Body ID is required');
      return await celestialBodyService.delete(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allCelestialBodies');
        toast.success('Celestial Body deleted successfully!');
        navigate('/celestial-bodies');
      },
      onError: (error: any) => {
        toast.error('Failed to delete celestial body');
        console.error('Delete celestial body error:', error);
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
    updateCelestialBodyMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteCelestialBodyMutation.mutate();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  const getTypeColor = (type: string) => {
    switch (type.toLowerCase()) {
      case 'star': return 'warning';
      case 'planet': return 'primary';
      case 'moon': return 'secondary';
      case 'asteroid': return 'error';
      case 'comet': return 'info';
      default: return 'default';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active': return 'success';
      case 'inactive': return 'error';
      case 'dormant': return 'warning';
      case 'exploded': return 'error';
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
          Failed to load celestial body details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!celestialBody) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Celestial Body not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/celestial-bodies')} sx={{ mt: 2 }}>
          Back to Celestial Bodies
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/celestial-bodies')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {celestialBody.name}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {celestialBody.type} • {celestialBody.status}
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
        {/* Celestial Body Info */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Celestial Body Description
              </Typography>
              <Typography variant="body1" paragraph>
                {celestialBody.description}
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
                    label={celestialBody.type}
                    color={getTypeColor(celestialBody.type || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Flag sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Status
                    </Typography>
                  </Box>
                  <Chip
                    label={celestialBody.status}
                    color={getStatusColor(celestialBody.status || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Public sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Galaxy
                    </Typography>
                  </Box>
                  <Typography variant="body1">
                    {celestialBody.galaxy || 'Unknown'}
                  </Typography>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <CalendarToday sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Discovered
                    </Typography>
                  </Box>
                  <Typography variant="body1">
                    {celestialBody.discoveredDate ? formatDate(celestialBody.discoveredDate.toISOString()) : 'Unknown'}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Celestial Body Stats and Info */}
        <Grid item xs={12} md={4}>
          <Grid container spacing={2}>
            {/* Physical Properties Card */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Speed sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Physical Properties
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Public />
                      </ListItemIcon>
                      <ListItemText
                        primary="Radius"
                        secondary={`${celestialBody.radius || 'N/A'} km`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Storage />
                      </ListItemIcon>
                      <ListItemText
                        primary="Mass"
                        secondary={`${celestialBody.mass || 'N/A'} kg`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Speed />
                      </ListItemIcon>
                      <ListItemText
                        primary="Temperature"
                        secondary={`${celestialBody.temperature || 'N/A'} K`}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Orbital Properties */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Satellite sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Orbital Properties
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Language />
                      </ListItemIcon>
                      <ListItemText
                        primary="Orbital Period"
                        secondary={`${celestialBody.orbitalPeriod || 'N/A'} days`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Speed />
                      </ListItemIcon>
                      <ListItemText
                        primary="Orbital Speed"
                        secondary={`${celestialBody.orbitSpeed || 'N/A'} km/s`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Public />
                      </ListItemIcon>
                      <ListItemText
                        primary="Distance from Star"
                        secondary={`${celestialBody.distanceFromStar || 'N/A'} AU`}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Discovery Info */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Assignment sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Discovery Info
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Person />
                      </ListItemIcon>
                      <ListItemText
                        primary="Discoverer"
                        secondary={celestialBody.discoverer || 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <CalendarToday />
                      </ListItemIcon>
                      <ListItemText
                        primary="Discovery Date"
                        secondary={celestialBody.discoveredDate ? formatDate(celestialBody.discoveredDate.toISOString()) : 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <School />
                      </ListItemIcon>
                      <ListItemText
                        primary="Discovery Method"
                        secondary={celestialBody.discoveryMethod || 'Unknown'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>

        {/* Celestial Body Features */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <School sx={{ mr: 1, verticalAlign: 'middle' }} />
                Celestial Body Features
              </Typography>
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Luminosity
                    </Typography>
                    <Typography variant="h6">
                      {celestialBody.luminosity || 'N/A'} L☉
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Age
                    </Typography>
                    <Typography variant="h6">
                      {celestialBody.age || 'N/A'} years
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Composition
                    </Typography>
                    <Typography variant="h6">
                      {celestialBody.composition || 'N/A'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Atmosphere
                    </Typography>
                    <Typography variant="h6">
                      {celestialBody.atmosphere || 'N/A'}
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
        <DialogTitle>Edit Celestial Body</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Name"
                value={celestialBody.name}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={celestialBody.description}
                multiline
                rows={3}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Type"
                value={celestialBody.type}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Status"
                value={celestialBody.status}
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
            disabled={updateCelestialBodyMutation.isLoading}
          >
            {updateCelestialBodyMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Celestial Body</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{celestialBody.name}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteCelestialBodyMutation.isLoading}
          >
            {deleteCelestialBodyMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share Celestial Body</DialogTitle>
        <DialogContent>
          <Typography>
            Share this celestial body with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/celestial-bodies/${celestialBody.id}`}
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
              navigator.clipboard.writeText(`${window.location.origin}/celestial-bodies/${celestialBody.id}`);
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

export default CelestialBodyDetailPage;
