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
  Spa,
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
  Public,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { celestialSpaceService } from '../services';
import { CelestialSpace } from '../types/star';
import { toast } from 'react-hot-toast';

const CelestialSpaceDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [celestialSpace, setCelestialSpace] = useState<CelestialSpace | null>(null);

  const queryClient = useQueryClient();

  // Fetch celestial space details
  const { data: celestialSpaceData, isLoading, error, refetch } = useQuery(
    ['celestialSpace', id],
    async () => {
      if (!id) throw new Error('Celestial Space ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await celestialSpaceService.getAll();
      const foundCelestialSpace = response.result?.find((cs: CelestialSpace) => cs.id === id);
      if (!foundCelestialSpace) throw new Error('Celestial Space not found');
      return { result: foundCelestialSpace };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setCelestialSpace(data.result);
        }
      },
    }
  );

  // Update celestial space mutation
  const updateCelestialSpaceMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('Celestial Space ID is required');
      return await celestialSpaceService.update(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['celestialSpace', id]);
        queryClient.invalidateQueries('allCelestialSpaces');
        toast.success('Celestial Space updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update celestial space');
        console.error('Update celestial space error:', error);
      },
    }
  );

  // Delete celestial space mutation
  const deleteCelestialSpaceMutation = useMutation(
    async () => {
      if (!id) throw new Error('Celestial Space ID is required');
      return await celestialSpaceService.delete(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allCelestialSpaces');
        toast.success('Celestial Space deleted successfully!');
        navigate('/celestial-spaces');
      },
      onError: (error: any) => {
        toast.error('Failed to delete celestial space');
        console.error('Delete celestial space error:', error);
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
    updateCelestialSpaceMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteCelestialSpaceMutation.mutate();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  const getTypeColor = (type: string) => {
    switch (type.toLowerCase()) {
      case 'galaxy': return 'primary';
      case 'solar system': return 'secondary';
      case 'nebula': return 'info';
      case 'cluster': return 'success';
      case 'void': return 'error';
      default: return 'default';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active': return 'success';
      case 'inactive': return 'error';
      case 'forming': return 'warning';
      case 'collapsing': return 'error';
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
          Failed to load celestial space details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!celestialSpace) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Celestial Space not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/celestial-spaces')} sx={{ mt: 2 }}>
          Back to Celestial Spaces
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/celestial-spaces')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {celestialSpace.name}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {celestialSpace.type} • {celestialSpace.status}
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
        {/* Celestial Space Info */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Celestial Space Description
              </Typography>
              <Typography variant="body1" paragraph>
                {celestialSpace.description}
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
                    label={celestialSpace.type}
                    color={getTypeColor(celestialSpace.type || 'Unknown') as any}
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
                    label={celestialSpace.status}
                    color={getStatusColor(celestialSpace.status || 'Unknown') as any}
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
                    {celestialSpace.galaxy || 'Unknown'}
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
                    {celestialSpace.discoveredDate ? formatDate(celestialSpace.discoveredDate.toISOString()) : 'Unknown'}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Celestial Space Stats and Info */}
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
                        primary="Diameter"
                        secondary={`${celestialSpace.diameter || 'N/A'} light years`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Storage />
                      </ListItemIcon>
                      <ListItemText
                        primary="Volume"
                        secondary={`${celestialSpace.volume || 'N/A'} cubic light years`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Speed />
                      </ListItemIcon>
                      <ListItemText
                        primary="Temperature"
                        secondary={`${celestialSpace.temperature || 'N/A'} K`}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Composition Properties */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Satellite sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Composition
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Language />
                      </ListItemIcon>
                      <ListItemText
                        primary="Matter Density"
                        secondary={`${celestialSpace.matterDensity || 'N/A'} atoms/cm³`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Speed />
                      </ListItemIcon>
                      <ListItemText
                        primary="Dark Matter"
                        secondary={`${celestialSpace.darkMatterPercentage || 'N/A'}%`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Public />
                      </ListItemIcon>
                      <ListItemText
                        primary="Energy Level"
                        secondary={`${celestialSpace.energyLevel || 'N/A'} eV`}
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
                        secondary={celestialSpace.discoverer || 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <CalendarToday />
                      </ListItemIcon>
                      <ListItemText
                        primary="Discovery Date"
                        secondary={celestialSpace.discoveredDate ? formatDate(celestialSpace.discoveredDate.toISOString()) : 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <School />
                      </ListItemIcon>
                      <ListItemText
                        primary="Discovery Method"
                        secondary={celestialSpace.discoveryMethod || 'Unknown'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>

        {/* Celestial Space Features */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <School sx={{ mr: 1, verticalAlign: 'middle' }} />
                Celestial Space Features
              </Typography>
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Star Count
                    </Typography>
                    <Typography variant="h6">
                      {celestialSpace.starCount || 'N/A'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Age
                    </Typography>
                    <Typography variant="h6">
                      {celestialSpace.age || 'N/A'} years
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Expansion Rate
                    </Typography>
                    <Typography variant="h6">
                      {celestialSpace.expansionRate || 'N/A'} km/s
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Gravitational Field
                    </Typography>
                    <Typography variant="h6">
                      {celestialSpace.gravitationalField || 'N/A'}
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
        <DialogTitle>Edit Celestial Space</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Name"
                value={celestialSpace.name}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={celestialSpace.description}
                multiline
                rows={3}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Type"
                value={celestialSpace.type}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Status"
                value={celestialSpace.status}
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
            disabled={updateCelestialSpaceMutation.isLoading}
          >
            {updateCelestialSpaceMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Celestial Space</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{celestialSpace.name}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteCelestialSpaceMutation.isLoading}
          >
            {deleteCelestialSpaceMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share Celestial Space</DialogTitle>
        <DialogContent>
          <Typography>
            Share this celestial space with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/celestial-spaces/${celestialSpace.id}`}
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
              navigator.clipboard.writeText(`${window.location.origin}/celestial-spaces/${celestialSpace.id}`);
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

export default CelestialSpaceDetailPage;
