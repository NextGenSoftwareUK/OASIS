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
} from '@mui/material';
import {
  ArrowBack,
  Edit,
  Delete,
  Share,
  Download,
  Visibility,
  Image,
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
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { nftService } from '../services';
import { NFT } from '../types/star';
import { toast } from 'react-hot-toast';

const NFTDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [nft, setNft] = useState<NFT | null>(null);

  const queryClient = useQueryClient();

  // Fetch NFT details
  const { data: nftData, isLoading, error, refetch } = useQuery(
    ['nft', id],
    async () => {
      if (!id) throw new Error('NFT ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await nftService.getAll();
      const foundNft = response.result?.find((n: NFT) => n.id === id);
      if (!foundNft) throw new Error('NFT not found');
      return { result: foundNft };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setNft(data.result);
        }
      },
    }
  );

  // Update NFT mutation
  const updateNftMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('NFT ID is required');
      return await nftService.update(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['nft', id]);
        queryClient.invalidateQueries('allNFTs');
        toast.success('NFT updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update NFT');
        console.error('Update NFT error:', error);
      },
    }
  );

  // Delete NFT mutation
  const deleteNftMutation = useMutation(
    async () => {
      if (!id) throw new Error('NFT ID is required');
      return await nftService.delete(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allNFTs');
        toast.success('NFT deleted successfully!');
        navigate('/nfts');
      },
      onError: (error: any) => {
        toast.error('Failed to delete NFT');
        console.error('Delete NFT error:', error);
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
    updateNftMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteNftMutation.mutate();
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
          Failed to load NFT details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!nft) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          NFT not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/nfts')} sx={{ mt: 2 }}>
          Back to NFTs
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/nfts')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {nft.name}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            by {nft.creator}
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
        {/* NFT Image and Basic Info */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box sx={{ textAlign: 'center', mb: 3 }}>
                <Box
                  sx={{
                    width: '100%',
                    height: 400,
                    backgroundImage: `url(${nft.imageUrl})`,
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                    borderRadius: 2,
                    mb: 2,
                    position: 'relative',
                    overflow: 'hidden',
                  }}
                >
                  <Badge
                    badgeContent={nft.rarity}
                    color={getRarityColor(nft.rarity || 'Unknown') as any}
                    sx={{
                      position: 'absolute',
                      top: 16,
                      right: 16,
                    }}
                  >
                    <Box />
                  </Badge>
                </Box>
                <Typography variant="h5" gutterBottom>
                  {nft.name}
                </Typography>
                <Typography variant="body1" color="text.secondary" paragraph>
                  {nft.description}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* NFT Details and Stats */}
        <Grid item xs={12} md={6}>
          <Grid container spacing={2}>
            {/* Price and Value */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <AttachMoney sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Value & Pricing
                  </Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="text.secondary">
                        Current Price
                      </Typography>
                      <Typography variant="h4" color="primary">
                        ${nft.price?.toLocaleString() || 'N/A'}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="text.secondary">
                        Last Sale
                      </Typography>
                      <Typography variant="h4" color="secondary">
                        ${nft.lastSalePrice?.toLocaleString() || 'N/A'}
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
                        <Star />
                      </ListItemIcon>
                      <ListItemText
                        primary="Rarity"
                        secondary={nft.rarity}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Category />
                      </ListItemIcon>
                      <ListItemText
                        primary="Category"
                        secondary={nft.category}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Code />
                      </ListItemIcon>
                      <ListItemText
                        primary="Token ID"
                        secondary={nft.tokenId}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Security />
                      </ListItemIcon>
                      <ListItemText
                        primary="Contract"
                        secondary={nft.contractAddress?.slice(0, 10) + '...' || 'N/A'}
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
                      {nft.creator?.charAt(0)}
                    </Avatar>
                    <Box>
                      <Typography variant="body1">
                        {nft.creator}
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
                      {formatDate(nft.createdDate ? nft.createdDate.toISOString() : new Date().toISOString())}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Views
                    </Typography>
                    <Typography variant="h6">
                      {nft.views?.toLocaleString() || '0'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Likes
                    </Typography>
                    <Typography variant="h6">
                      {nft.likes?.toLocaleString() || '0'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Collection
                    </Typography>
                    <Typography variant="h6">
                      {nft.collection || 'Standalone'}
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
        <DialogTitle>Edit NFT</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Name"
                value={nft.name}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={nft.description}
                multiline
                rows={3}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Price"
                type="number"
                value={nft.price || ''}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Category"
                value={nft.category}
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
            disabled={updateNftMutation.isLoading}
          >
            {updateNftMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete NFT</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{nft.name}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteNftMutation.isLoading}
          >
            {deleteNftMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share NFT</DialogTitle>
        <DialogContent>
          <Typography>
            Share this NFT with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/nfts/${nft.id}`}
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
              navigator.clipboard.writeText(`${window.location.origin}/nfts/${nft.id}`);
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

export default NFTDetailPage;
