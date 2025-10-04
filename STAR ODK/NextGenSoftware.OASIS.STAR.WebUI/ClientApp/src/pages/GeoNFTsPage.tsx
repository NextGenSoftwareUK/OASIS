/**
 * GeoNFTs Page
 * Complete GeoNFT management interface
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
  LocationOn,
  FilterList,
  Search,
  Help,
  Info,
  Build,
  MonetizationOn,
  Share,
  Favorite,
  Star,
  Map,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { geoNftService } from '../services';
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
      id={`geonft-tabpanel-${index}`}
      aria-labelledby={`geonft-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const GeoNFTsPage: React.FC = () => {
  const navigate = useNavigate();
  const { isDemoMode } = useDemoMode();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState('all');
  const [sortBy, setSortBy] = useState('newest');
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [selectedGeoNFT, setSelectedGeoNFT] = useState<any>(null);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  // Fetch GeoNFTs
  const { data: geoNFTs, isLoading, error } = useQuery('geoNFTs', geoNftService.getAll);

  // Create GeoNFT mutation
  const createGeoNFTMutation = useMutation(
    async (geoNFTData: any) => {
      const response = await geoNftService.create(geoNFTData);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('geoNFTs');
        toast.success('GeoNFT created successfully!');
        setCreateDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to create GeoNFT: ' + error.message);
      },
    }
  );

  // Publish GeoNFT mutation
  const publishGeoNFTMutation = useMutation(
    async (geoNFTId: string) => {
      const response = await geoNftService.publish(geoNFTId, {});
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('geoNFTs');
        toast.success('GeoNFT published successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to publish GeoNFT: ' + error.message);
      },
    }
  );

  // Download GeoNFT mutation
  const downloadGeoNFTMutation = useMutation(
    async (geoNFTId: string) => {
      const response = await geoNftService.download(geoNFTId, './downloads', true);
      return response.result;
    },
    {
      onSuccess: () => {
        toast.success('GeoNFT downloaded successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to download GeoNFT: ' + error.message);
      },
    }
  );

  const handleCreateGeoNFT = (geoNFTData: any) => {
    createGeoNFTMutation.mutate(geoNFTData);
  };

  const handlePublishGeoNFT = (geoNFTId: string) => {
    publishGeoNFTMutation.mutate(geoNFTId);
  };

  const handleDownloadGeoNFT = (geoNFTId: string) => {
    downloadGeoNFTMutation.mutate(geoNFTId);
  };

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, geoNFT: any) => {
    setAnchorEl(event.currentTarget);
    setSelectedGeoNFT(geoNFT);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedGeoNFT(null);
  };

  const filteredGeoNFTs = geoNFTs?.result?.filter((geoNFT: any) => {
    const matchesSearch = geoNFT.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         geoNFT.description?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesFilter = filterType === 'all' || geoNFT.type === filterType;
    return matchesSearch && matchesFilter;
  }) || [];

  const sortedGeoNFTs = [...filteredGeoNFTs].sort((a: any, b: any) => {
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

  const geoNFTStats = {
    total: geoNFTs?.result?.length || 0,
    published: geoNFTs?.result?.filter((geoNFT: any) => geoNFT.isPublished).length || 0,
    totalValue: geoNFTs?.result?.reduce((sum: number, geoNFT: any) => sum + (geoNFT.value || 0), 0) || 0,
    averageRating: geoNFTs?.result?.reduce((sum: number, geoNFT: any) => sum + (geoNFT.rating || 0), 0) / (geoNFTs?.result?.length || 1) || 0,
  };

  return (
    <Box sx={{ flexGrow: 1, p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" component="h1" gutterBottom>
            GeoNFTs
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage your Location-based Non-Fungible Tokens
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => setCreateDialogOpen(true)}
          sx={{ borderRadius: 2 }}
        >
          Create GeoNFT
        </Button>
      </Box>

      {/* Stats Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <LocationOn color="primary" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{geoNFTStats.total}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total GeoNFTs
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
                  <Typography variant="h6">{geoNFTStats.published}</Typography>
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
                  <Typography variant="h6">{geoNFTStats.totalValue.toFixed(2)} ETH</Typography>
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
                  <Typography variant="h6">{geoNFTStats.averageRating.toFixed(1)}</Typography>
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
                placeholder="Search GeoNFTs..."
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
                  <MenuItem value="Landmark">Landmark</MenuItem>
                  <MenuItem value="Event">Event</MenuItem>
                  <MenuItem value="Art">Art</MenuItem>
                  <MenuItem value="Experience">Experience</MenuItem>
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
                startIcon={<Map />}
              >
                View Map
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* GeoNFTs Grid */}
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
                        Loading GeoNFT details...
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
                  Failed to load GeoNFTs
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {error instanceof Error ? error.message : 'An error occurred'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ) : sortedGeoNFTs.length === 0 ? (
          <Grid item xs={12}>
            <Card>
              <CardContent sx={{ textAlign: 'center', py: 4 }}>
                <LocationOn sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                <Typography variant="h6" gutterBottom>
                  No GeoNFTs found
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                  {searchTerm ? 'Try adjusting your search criteria' : 'Create your first GeoNFT to get started'}
                </Typography>
                <Button
                  variant="contained"
                  startIcon={<Add />}
                  onClick={() => setCreateDialogOpen(true)}
                >
                  Create GeoNFT
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ) : (
          sortedGeoNFTs.map((geoNFT: any) => (
            <Grid item xs={12} sm={6} md={4} key={geoNFT.id}>
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3 }}
              >
                <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                  <CardMedia
                    component="img"
                    height="200"
                    image={geoNFT.imageUrl || '/api/placeholder/400/200'}
                    alt={geoNFT.name}
                    sx={{ objectFit: 'cover' }}
                  />
                  <CardContent sx={{ flexGrow: 1 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                      <Typography variant="h6" component="h3" noWrap>
                        {geoNFT.name}
                      </Typography>
                      <IconButton
                        size="small"
                        onClick={(e) => handleMenuClick(e, geoNFT)}
                      >
                        <MoreVert />
                      </IconButton>
                    </Box>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      {geoNFT.description}
                    </Typography>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Chip
                        label={geoNFT.type || 'GeoNFT'}
                        size="small"
                        color="primary"
                        variant="outlined"
                      />
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Star sx={{ fontSize: 16, mr: 0.5, color: 'warning.main' }} />
                        <Typography variant="body2">
                          {geoNFT.rating || 0}
                        </Typography>
                      </Box>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                      <LocationOn sx={{ fontSize: 16, mr: 0.5, color: 'text.secondary' }} />
                      <Typography variant="body2" color="text.secondary">
                        {geoNFT.latitude?.toFixed(4)}, {geoNFT.longitude?.toFixed(4)}
                      </Typography>
                    </Box>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography variant="h6" color="primary">
                        {geoNFT.value || '0'} ETH
                      </Typography>
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mr: 1 }}>
                          {geoNFT.downloads || 0} downloads
                        </Typography>
                      </Box>
                    </Box>
                  </CardContent>
                  <Divider />
                  <CardActions sx={{ justifyContent: 'space-between', p: 2 }}>
                    <Button
                      size="small"
                      startIcon={<Visibility />}
                      onClick={() => navigate(`/geonfts/${geoNFT.id}`)}
                    >
                      View
                    </Button>
                    <Box>
                      <IconButton
                        size="small"
                        onClick={() => handleDownloadGeoNFT(geoNFT.id)}
                        disabled={downloadGeoNFTMutation.isLoading}
                      >
                        <Download />
                      </IconButton>
                      <IconButton
                        size="small"
                        onClick={() => handlePublishGeoNFT(geoNFT.id)}
                        disabled={publishGeoNFTMutation.isLoading}
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

      {/* Create GeoNFT Dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Create New GeoNFT</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="GeoNFT Name"
                placeholder="Enter GeoNFT name"
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                placeholder="Enter GeoNFT description"
                multiline
                rows={3}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select defaultValue="Landmark">
                  <MenuItem value="Landmark">Landmark</MenuItem>
                  <MenuItem value="Event">Event</MenuItem>
                  <MenuItem value="Art">Art</MenuItem>
                  <MenuItem value="Experience">Experience</MenuItem>
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
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Latitude"
                type="number"
                placeholder="40.7128"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Longitude"
                type="number"
                placeholder="-74.0060"
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
            onClick={() => handleCreateGeoNFT({})}
            disabled={createGeoNFTMutation.isLoading}
          >
            {createGeoNFTMutation.isLoading ? 'Creating...' : 'Create GeoNFT'}
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
          if (selectedGeoNFT) navigate(`/geonfts/${selectedGeoNFT.id}`);
          handleMenuClose();
        }}>
          <Visibility sx={{ mr: 1 }} />
          View Details
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedGeoNFT) handleDownloadGeoNFT(selectedGeoNFT.id);
          handleMenuClose();
        }}>
          <Download sx={{ mr: 1 }} />
          Download
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedGeoNFT) handlePublishGeoNFT(selectedGeoNFT.id);
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
        aria-label="create geonft"
        sx={{ position: 'fixed', bottom: 16, right: 16 }}
        onClick={() => setCreateDialogOpen(true)}
      >
        <Add />
      </Fab>
    </Box>
  );
};

export default GeoNFTsPage;