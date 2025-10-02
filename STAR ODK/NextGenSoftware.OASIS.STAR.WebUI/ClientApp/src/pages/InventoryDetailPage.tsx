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
  Inventory,
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
  LocalShipping,
  CheckCircle,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starNetService } from '../services/starNetService';
import { InventoryItem } from '../types/star';
import { toast } from 'react-hot-toast';

const InventoryDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [item, setItem] = useState<InventoryItem | null>(null);

  const queryClient = useQueryClient();

  // Fetch inventory item details
  const { data: itemData, isLoading, error, refetch } = useQuery(
    ['inventory', id],
    async () => {
      if (!id) throw new Error('Item ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await starNetService.getAllInventoryItems();
      const foundItem = response.result?.find((i: InventoryItem) => i.id === id);
      if (!foundItem) throw new Error('Item not found');
      return { result: foundItem };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setItem(data.result);
        }
      },
    }
  );

  // Update item mutation
  const updateItemMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('Item ID is required');
      return await starNetService.updateInventoryItem(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['inventory', id]);
        queryClient.invalidateQueries('allInventoryItems');
        toast.success('Item updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update item');
        console.error('Update item error:', error);
      },
    }
  );

  // Delete item mutation
  const deleteItemMutation = useMutation(
    async () => {
      if (!id) throw new Error('Item ID is required');
      return await starNetService.deleteInventoryItem(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allInventoryItems');
        toast.success('Item deleted successfully!');
        navigate('/inventory');
      },
      onError: (error: any) => {
        toast.error('Failed to delete item');
        console.error('Delete item error:', error);
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
    updateItemMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteItemMutation.mutate();
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
      case 'maintenance': return 'warning';
      case 'upgrading': return 'info';
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
          Failed to load item details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!item) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Item not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/inventory')} sx={{ mt: 2 }}>
          Back to Inventory
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/inventory')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {item.name}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {item.type} • {item.category}
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
        {/* Item Image and Basic Info */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box sx={{ textAlign: 'center', mb: 3 }}>
                <Box
                  sx={{
                    width: '100%',
                    height: 400,
                    backgroundImage: `url(${item.imageUrl})`,
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                    borderRadius: 2,
                    mb: 2,
                    position: 'relative',
                    overflow: 'hidden',
                  }}
                >
                  <Badge
                    badgeContent={item.rarity}
                    color={getRarityColor(item.rarity || 'Unknown') as any}
                    sx={{
                      position: 'absolute',
                      top: 16,
                      right: 16,
                    }}
                  >
                    <Box />
                  </Badge>
                  <Badge
                    badgeContent={item.status}
                    color={getStatusColor(item.status || 'Unknown') as any}
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
                  {item.name}
                </Typography>
                <Typography variant="body1" color="text.secondary" paragraph>
                  {item.description}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Item Details and Stats */}
        <Grid item xs={12} md={6}>
          <Grid container spacing={2}>
            {/* Value and Stats */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <AttachMoney sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Value & Stats
                  </Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="text.secondary">
                        Value
                      </Typography>
                      <Typography variant="h4" color="primary">
                        ${item.value?.toLocaleString() || 'N/A'}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="text.secondary">
                        Quantity
                      </Typography>
                      <Typography variant="h4" color="secondary">
                        {item.quantity || 1}
                      </Typography>
                    </Grid>
                  </Grid>
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
                        <Inventory />
                      </ListItemIcon>
                      <ListItemText
                        primary="Type"
                        secondary={item.type}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Category />
                      </ListItemIcon>
                      <ListItemText
                        primary="Category"
                        secondary={item.category}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Star />
                      </ListItemIcon>
                      <ListItemText
                        primary="Rarity"
                        secondary={item.rarity}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <CheckCircle />
                      </ListItemIcon>
                      <ListItemText
                        primary="Status"
                        secondary={item.status}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Durability */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Memory sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Durability
                  </Typography>
                  <Box sx={{ mb: 2 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                      <Typography variant="body2">
                        Current Durability
                      </Typography>
                      <Typography variant="body2">
                        {item.durability || 100}%
                      </Typography>
                    </Box>
                    <LinearProgress
                      variant="determinate"
                      value={item.durability || 100}
                      sx={{ height: 8, borderRadius: 4 }}
                    />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Max Durability: {item.maxDurability || 100}%
                  </Typography>
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
                      Acquired
                    </Typography>
                    <Typography variant="h6">
                      {formatDate(item.acquiredDate ? item.acquiredDate.toISOString() : new Date().toISOString())}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Last Used
                    </Typography>
                    <Typography variant="h6">
                      {item.lastUsed ? formatDate(item.lastUsed.toISOString()) : 'Never'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Weight
                    </Typography>
                    <Typography variant="h6">
                      {item.weight || 'N/A'} kg
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Location
                    </Typography>
                    <Typography variant="h6">
                      {item.location || 'Inventory'}
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
        <DialogTitle>Edit Item</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Name"
                value={item.name}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={item.description}
                multiline
                rows={3}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Value"
                type="number"
                value={item.value || ''}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Quantity"
                type="number"
                value={item.quantity || 1}
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
            disabled={updateItemMutation.isLoading}
          >
            {updateItemMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Item</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{item.name}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteItemMutation.isLoading}
          >
            {deleteItemMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share Item</DialogTitle>
        <DialogContent>
          <Typography>
            Share this item with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/inventory/${item.id}`}
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
              navigator.clipboard.writeText(`${window.location.origin}/inventory/${item.id}`);
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

export default InventoryDetailPage;
