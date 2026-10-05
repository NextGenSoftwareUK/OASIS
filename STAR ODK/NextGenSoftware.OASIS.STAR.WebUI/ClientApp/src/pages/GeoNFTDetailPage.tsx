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
  LocationOn,
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
  Map,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { geoNftService } from '../services';
import { GeoNFT } from '../types/star';
import { toast } from 'react-hot-toast';

const GeoNFTDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [geoNFT, setGeoNFT] = useState<GeoNFT | null>(null);

  const queryClient = useQueryClient();

  // Fetch GeoNFT details
  const { data: geoNFTData, isLoading, error, refetch } = useQuery(
    ['geoNFT', id],
    async () => {
      if (!id) throw new Error('GeoNFT ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await geoNftService.getAll();
      const foundGeoNFT = response.result?.find((gn: GeoNFT) => gn.id === id);
      if (!foundGeoNFT) throw new Error('GeoNFT not found');
      return { result: foundGeoNFT };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setGeoNFT(data.result);
        }
      },
    }
  );

  // Update GeoNFT mutation
  const updateGeoNFTMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('GeoNFT ID is required');
      return await geoNftService.update(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['geoNFT', id]);
        queryClient.invalidateQueries('allGeoNFTs');
        toast.success('GeoNFT updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update GeoNFT');
        console.error('Update GeoNFT error:', error);
      },
    }
  );

  // Delete GeoNFT mutation
  const deleteGeoNFTMutation = useMutation(
    async () => {
      if (!id) throw new Error('GeoNFT ID is required');
      return await geoNftService.delete(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allGeoNFTs');
        toast.success('GeoNFT deleted successfully!');
        navigate('/geonfts');
      },
      onError: (error: any) => {
        toast.error('Failed to delete GeoNFT');
        console.error('Delete GeoNFT error:', error);
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
    updateGeoNFTMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteGeoNFTMutation.mutate();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  const getRarityColor = (rarity: string) => {
    switch (rarity.toLowerCase()) {
      case 'legendary': return 'error';
      case 'epic': return 'secondary';
      case 'rare': return 'primary';
      case 'uncommon': return 'success';
      case 'common': return 'default';
      default: return 'default';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active': return 'success';
      case 'inactive': return 'error';
      case 'pending': return 'warning';
      case 'verified': return 'primary';
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
          Failed to load GeoNFT details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!geoNFT) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          GeoNFT not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/geonfts')} sx={{ mt: 2 }}>
          Back to GeoNFTs
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/geonfts')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {geoNFT.name}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {geoNFT.category} • {geoNFT.rarity}
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
        {/* GeoNFT Image and Basic Info */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box sx={{ textAlign: 'center', mb: 3 }}>
                <Box
                  sx={{
                    width: '100%',
                    height: 400,
                    backgroundImage: `url(${geoNFT.imageUrl})`,
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                    borderRadius: 2,
                    mb: 2,
                    position: 'relative',
                    overflow: 'hidden',
                  }}
                >
                  <Badge
                    badgeContent={geoNFT.rarity}
                    color={getRarityColor(geoNFT.rarity || 'Unknown') as any}
                    sx={{
                      position: 'absolute',
                      top: 16,
                      right: 16,
                    }}
                  >
                    <Box />
                  </Badge>
                  <Badge
                    badgeContent={geoNFT.status}
                    color={getStatusColor(geoNFT.status || 'Unknown') as any}
                    sx={{
                      position: 'absolute',
                      top: 16,
                      left: 16,
                    }}
                  >
                    <Box />
                  </Badge>
                </Box>
                <Typography variant="h5" gutterBottom>
                  {geoNFT.name}
                </Typography>
                <Typography variant="body1" color="text.secondary" paragraph>
                  {geoNFT.description}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* GeoNFT Details and Stats */}
        <Grid item xs={12} md={6}>
          <Grid container spacing={2}>
            {/* Location Info */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <LocationOn sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Location Information
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Map />
                      </ListItemIcon>
                      <ListItemText
                        primary="Coordinates"
                        secondary={`${geoNFT.latitude}, ${geoNFT.longitude}`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Public />
                      </ListItemIcon>
                      <ListItemText
                        primary="Country"
                        secondary={geoNFT.country || 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Language />
                      </ListItemIcon>
                      <ListItemText
                        primary="Region"
                        secondary={geoNFT.region || 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <LocationOn />
                      </ListItemIcon>
                      <ListItemText
                        primary="Address"
                        secondary={geoNFT.address || 'Unknown'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Properties */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Category sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Properties
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Star />
                      </ListItemIcon>
                      <ListItemText
                        primary="Rarity"
                        secondary={geoNFT.rarity}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Category />
                      </ListItemIcon>
                      <ListItemText
                        primary="Category"
                        secondary={geoNFT.category}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Code />
                      </ListItemIcon>
                      <ListItemText
                        primary="Token ID"
                        secondary={geoNFT.tokenId}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Security />
                      </ListItemIcon>
                      <ListItemText
                        primary="Contract"
                        secondary={geoNFT.contractAddress?.slice(0, 10) + '...' || 'N/A'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Creator Info */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Person sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Creator
                  </Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <Avatar sx={{ mr: 2 }}>
                      {geoNFT.creator?.charAt(0)}
                    </Avatar>
                    <Box>
                      <Typography variant="body1">
                        {geoNFT.creator}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Artist
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>

        {/* Additional Details */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <Storage sx={{ mr: 1, verticalAlign: 'middle' }} />
                Additional Details
              </Typography>
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Created
                    </Typography>
                    <Typography variant="h6">
                      {formatDate(geoNFT.createdDate ? geoNFT.createdDate.toISOString() : new Date().toISOString())}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Views
                    </Typography>
                    <Typography variant="h6">
                      {geoNFT.views?.toLocaleString() || '0'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Likes
                    </Typography>
                    <Typography variant="h6">
                      {geoNFT.likes?.toLocaleString() || '0'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Collection
                    </Typography>
                    <Typography variant="h6">
                      {geoNFT.collection || 'Standalone'}
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
        <DialogTitle>Edit GeoNFT</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Name"
                value={geoNFT.name}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={geoNFT.description}
                multiline
                rows={3}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Latitude"
                type="number"
                value={geoNFT.latitude || ''}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Longitude"
                type="number"
                value={geoNFT.longitude || ''}
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
            disabled={updateGeoNFTMutation.isLoading}
          >
            {updateGeoNFTMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete GeoNFT</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{geoNFT.name}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteGeoNFTMutation.isLoading}
          >
            {deleteGeoNFTMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share GeoNFT</DialogTitle>
        <DialogContent>
          <Typography>
            Share this GeoNFT with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/geonfts/${geoNFT.id}`}
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
              navigator.clipboard.writeText(`${window.location.origin}/geonfts/${geoNFT.id}`);
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

export default GeoNFTDetailPage;
