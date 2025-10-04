/**
 * NFTs Page
 * Complete NFT management interface
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
  Image,
  FilterList,
  Search,
  Help,
  Info,
  Build,
  MonetizationOn,
  Share,
  Favorite,
  Star,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { nftService } from '../services';
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
      id={`nft-tabpanel-${index}`}
      aria-labelledby={`nft-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const NFTsPage: React.FC = () => {
  const navigate = useNavigate();
  const { isDemoMode } = useDemoMode();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState('all');
  const [sortBy, setSortBy] = useState('newest');
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [selectedNFT, setSelectedNFT] = useState<any>(null);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  // Fetch NFTs
  const { data: nfts, isLoading, error } = useQuery('nfts', nftService.getAll);

  // Create NFT mutation
  const createNFTMutation = useMutation(
    async (nftData: any) => {
      const response = await nftService.create(nftData);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('nfts');
        toast.success('NFT created successfully!');
        setCreateDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to create NFT: ' + error.message);
      },
    }
  );

  // Publish NFT mutation
  const publishNFTMutation = useMutation(
    async (nftId: string) => {
      const response = await nftService.publish(nftId, {});
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('nfts');
        toast.success('NFT published successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to publish NFT: ' + error.message);
      },
    }
  );

  // Download NFT mutation
  const downloadNFTMutation = useMutation(
    async (nftId: string) => {
      const response = await nftService.download(nftId, './downloads', true);
      return response.result;
    },
    {
      onSuccess: () => {
        toast.success('NFT downloaded successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to download NFT: ' + error.message);
      },
    }
  );

  const handleCreateNFT = (nftData: any) => {
    createNFTMutation.mutate(nftData);
  };

  const handlePublishNFT = (nftId: string) => {
    publishNFTMutation.mutate(nftId);
  };

  const handleDownloadNFT = (nftId: string) => {
    downloadNFTMutation.mutate(nftId);
  };

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, nft: any) => {
    setAnchorEl(event.currentTarget);
    setSelectedNFT(nft);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedNFT(null);
  };

  const filteredNFTs = nfts?.result?.filter((nft: any) => {
    const matchesSearch = nft.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         nft.description?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesFilter = filterType === 'all' || nft.type === filterType;
    return matchesSearch && matchesFilter;
  }) || [];

  const sortedNFTs = [...filteredNFTs].sort((a: any, b: any) => {
    switch (sortBy) {
      case 'newest':
        return new Date(b.createdOn || 0).getTime() - new Date(a.createdOn || 0).getTime();
      case 'oldest':
        return new Date(a.createdOn || 0).getTime() - new Date(b.createdOn || 0).getTime();
      case 'name':
        return (a.name || '').localeCompare(b.name || '');
      case 'value':
        return (b.value || 0) - (a.value || 0);
      default:
        return 0;
    }
  });

  const nftStats = {
    total: nfts?.result?.length || 0,
    published: nfts?.result?.filter((nft: any) => nft.isPublished).length || 0,
    totalValue: nfts?.result?.reduce((sum: number, nft: any) => sum + (nft.value || 0), 0) || 0,
    averageRating: nfts?.result?.reduce((sum: number, nft: any) => sum + (nft.rating || 0), 0) / (nfts?.result?.length || 1) || 0,
  };

  return (
    <Box sx={{ flexGrow: 1, p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" component="h1" gutterBottom>
            NFTs
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage your Non-Fungible Tokens
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => setCreateDialogOpen(true)}
          sx={{ borderRadius: 2 }}
        >
          Create NFT
        </Button>
      </Box>

      {/* Stats Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Image color="primary" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{nftStats.total}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total NFTs
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
                <Upload color="success" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{nftStats.published}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Published
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
                <MonetizationOn color="warning" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{nftStats.totalValue.toFixed(2)} ETH</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total Value
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
                <Star color="info" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{nftStats.averageRating.toFixed(1)}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Avg Rating
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
                placeholder="Search NFTs..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                InputProps={{
                  startAdornment: <Search sx={{ mr: 1, color: 'text.secondary' }} />,
                }}
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <FormControl fullWidth>
                <InputLabel>Filter by Type</InputLabel>
                <Select
                  value={filterType}
                  onChange={(e) => setFilterType(e.target.value)}
                >
                  <MenuItem value="all">All Types</MenuItem>
                  <MenuItem value="Art">Art</MenuItem>
                  <MenuItem value="Music">Music</MenuItem>
                  <MenuItem value="Video">Video</MenuItem>
                  <MenuItem value="Game">Game</MenuItem>
                  <MenuItem value="Collectible">Collectible</MenuItem>
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
                  <MenuItem value="value">Value</MenuItem>
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

      {/* NFTs Grid */}
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
                        Loading NFT details...
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
                  Failed to load NFTs
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {error instanceof Error ? error.message : 'An error occurred'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ) : sortedNFTs.length === 0 ? (
          <Grid item xs={12}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 4 }}>
                <Image sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                <Typography variant="h6" gutterBottom>
                  No NFTs found
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                  {searchTerm ? 'Try adjusting your search criteria' : 'Create your first NFT to get started'}
                </Typography>
                <Button
                  variant="contained"
                  startIcon={<Add />}
                  onClick={() => setCreateDialogOpen(true)}
                >
                  Create NFT
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ) : (
          sortedNFTs.map((nft: any) => (
            <Grid item xs={12} sm={6} md={4} key={nft.id}>
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3 }}
              >
                <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                  <CardMedia
                    component="img"
                    height="200"
                    image={nft.imageUrl || '/api/placeholder/400/200'}
                    alt={nft.name}
                    sx={{ objectFit: 'cover' }}
                  />
                  <CardContent sx={{ flexGrow: 1 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                      <Typography variant="h6" component="h3" noWrap>
                        {nft.name}
                      </Typography>
                      <IconButton
                        size="small"
                        onClick={(e) => handleMenuClick(e, nft)}
                      >
                        <MoreVert />
                      </IconButton>
                    </Box>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      {nft.description}
                    </Typography>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Chip
                        label={nft.type || 'NFT'}
                        size="small"
                        color="primary"
                        variant="outlined"
                      />
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Star sx={{ fontSize: 16, mr: 0.5, color: 'warning.main' }} />
                        <Typography variant="body2">
                          {nft.rating || 0}
                        </Typography>
                      </Box>
                    </Box>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography variant="h6" color="primary">
                        {nft.value || '0'} ETH
                      </Typography>
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mr: 1 }}>
                          {nft.downloads || 0} downloads
                        </Typography>
                      </Box>
                    </Box>
                  </CardContent>
                  <Divider />
                  <CardActions sx={{ justifyContent: 'space-between', p: 2 }}>
                    <Button
                      size="small"
                      startIcon={<Visibility />}
                      onClick={() => navigate(`/nfts/${nft.id}`)}
                    >
                      View
                    </Button>
                    <Box>
                      <IconButton
                        size="small"
                        onClick={() => handleDownloadNFT(nft.id)}
                        disabled={downloadNFTMutation.isLoading}
                      >
                        <Download />
                      </IconButton>
                      <IconButton
                        size="small"
                        onClick={() => handlePublishNFT(nft.id)}
                        disabled={publishNFTMutation.isLoading}
                      >
                        <Upload />
                      </IconButton>
                    </Box>
                  </CardActions>
                </Card>
              </motion.div>
            </Grid>
          ))
        )}
      </Grid>

      {/* Create NFT Dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Create New NFT</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="NFT Name"
                placeholder="Enter NFT name"
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                placeholder="Enter NFT description"
                multiline
                rows={3}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select defaultValue="Art">
                  <MenuItem value="Art">Art</MenuItem>
                  <MenuItem value="Music">Music</MenuItem>
                  <MenuItem value="Video">Video</MenuItem>
                  <MenuItem value="Game">Game</MenuItem>
                  <MenuItem value="Collectible">Collectible</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Value (ETH)"
                type="number"
                placeholder="0.1"
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
            onClick={() => handleCreateNFT({})}
            disabled={createNFTMutation.isLoading}
          >
            {createNFTMutation.isLoading ? 'Creating...' : 'Create NFT'}
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
          if (selectedNFT) navigate(`/nfts/${selectedNFT.id}`);
          handleMenuClose();
        }}>
          <Visibility sx={{ mr: 1 }} />
          View Details
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedNFT) handleDownloadNFT(selectedNFT.id);
          handleMenuClose();
        }}>
          <Download sx={{ mr: 1 }} />
          Download
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedNFT) handlePublishNFT(selectedNFT.id);
          handleMenuClose();
        }}>
          <Upload sx={{ mr: 1 }} />
          Publish
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
        aria-label="create nft"
        sx={{ position: 'fixed', bottom: 16, right: 16 }}
        onClick={() => setCreateDialogOpen(true)}
      >
        <Add />
      </Fab>
    </Box>
  );
};

export default NFTsPage;