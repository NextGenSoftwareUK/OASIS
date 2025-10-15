/**
 * MetaData Page
 * Complete MetaData management interface for Celestial Bodies, Zomes, and Holons
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
  CardActions,
  Divider,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
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
  DataObject,
  FilterList,
  Search,
  Help,
  Info,
  Build,
  Star,
  CheckCircle,
  Schedule,
  TrendingUp,
  Storage,
  Code,
  AccountTree,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { celestialBodyMetaService, zomeMetaService, holonMetaService } from '../services';
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
      id={`metadata-tabpanel-${index}`}
      aria-labelledby={`metadata-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const MetaDataPage: React.FC = () => {
  const navigate = useNavigate();
  const { isDemoMode } = useDemoMode();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState('all');
  const [sortBy, setSortBy] = useState('newest');
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [selectedMetaData, setSelectedMetaData] = useState<any>(null);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  // Fetch MetaData for each type
  const { data: celestialBodyMeta, isLoading: loadingCelestialBody } = useQuery('celestialBodyMeta', celestialBodyMetaService.getAll);
  const { data: zomeMeta, isLoading: loadingZome } = useQuery('zomeMeta', zomeMetaService.getAll);
  const { data: holonMeta, isLoading: loadingHolon } = useQuery('holonMeta', holonMetaService.getAll);

  // Get current data based on active tab
  const getCurrentData = () => {
    switch (tabValue) {
      case 0: return { data: celestialBodyMeta, loading: loadingCelestialBody, service: celestialBodyMetaService, type: 'Celestial Body' };
      case 1: return { data: zomeMeta, loading: loadingZome, service: zomeMetaService, type: 'Zome' };
      case 2: return { data: holonMeta, loading: loadingHolon, service: holonMetaService, type: 'Holon' };
      default: return { data: null, loading: false, service: null, type: '' };
    }
  };

  const currentData = getCurrentData();

  // Create MetaData mutation
  const createMetaDataMutation = useMutation(
    async (metaData: any) => {
      if (!currentData.service) throw new Error('No service selected');
      const response = await currentData.service.create(metaData);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['celestialBodyMeta', 'zomeMeta', 'holonMeta']);
        toast.success(`${currentData.type} MetaData created successfully!`);
        setCreateDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error(`Failed to create ${currentData.type} MetaData: ${error.message}`);
      },
    }
  );

  // Publish MetaData mutation
  const publishMetaDataMutation = useMutation(
    async (metaDataId: string) => {
      if (!currentData.service) throw new Error('No service selected');
      const response = await currentData.service.publish(metaDataId, {});
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['celestialBodyMeta', 'zomeMeta', 'holonMeta']);
        toast.success(`${currentData.type} MetaData published successfully!`);
      },
      onError: (error: any) => {
        toast.error(`Failed to publish ${currentData.type} MetaData: ${error.message}`);
      },
    }
  );

  const handleCreateMetaData = (metaData: any) => {
    createMetaDataMutation.mutate(metaData);
  };

  const handlePublishMetaData = (metaDataId: string) => {
    publishMetaDataMutation.mutate(metaDataId);
  };

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, metaData: any) => {
    setAnchorEl(event.currentTarget);
    setSelectedMetaData(metaData);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedMetaData(null);
  };

  const filteredMetaData = currentData.data?.result?.filter((item: any) => {
    const matchesSearch = item.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         item.description?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesFilter = filterType === 'all' || item.type === filterType;
    return matchesSearch && matchesFilter;
  }) || [];

  const sortedMetaData = [...filteredMetaData].sort((a: any, b: any) => {
    switch (sortBy) {
      case 'newest':
        return new Date(b.createdOn || 0).getTime() - new Date(a.createdOn || 0).getTime();
      case 'oldest':
        return new Date(a.createdOn || 0).getTime() - new Date(b.createdOn || 0).getTime();
      case 'name':
        return (a.name || '').localeCompare(b.name || '');
      case 'type':
        return (a.type || '').localeCompare(b.type || '');
      default:
        return 0;
    }
  });

  const metaDataStats = {
    total: currentData.data?.result?.length || 0,
    published: currentData.data?.result?.filter((item: any) => item.isPublished).length || 0,
    types: Array.from(new Set(currentData.data?.result?.map((item: any) => item.type) || [])).length,
    averageRating: currentData.data?.result?.reduce((sum: number, item: any) => sum + (item.rating || 0), 0) / (currentData.data?.result?.length || 1) || 0,
  };

  const getIcon = (type: string) => {
    switch (type) {
      case 'Celestial Body': return <Storage />;
      case 'Zome': return <Code />;
      case 'Holon': return <AccountTree />;
      default: return <DataObject />;
    }
  };

  const getColor = (type: string) => {
    switch (type) {
      case 'Celestial Body': return 'primary';
      case 'Zome': return 'secondary';
      case 'Holon': return 'success';
      default: return 'default';
    }
  };

  return (
    <Box sx={{ flexGrow: 1, p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" component="h1" gutterBottom>
            MetaData
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage metadata for Celestial Bodies, Zomes, and Holons
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => setCreateDialogOpen(true)}
          sx={{ borderRadius: 2 }}
        >
          Create MetaData
        </Button>
      </Box>

      {/* Tabs */}
      <Card sx={{ mb: 3 }}>
        <Tabs
          value={tabValue}
          onChange={(e, newValue) => setTabValue(newValue)}
          aria-label="metadata tabs"
        >
          <Tab
            icon={<Storage />}
            label="Celestial Bodies"
            iconPosition="start"
          />
          <Tab
            icon={<Code />}
            label="Zomes"
            iconPosition="start"
          />
          <Tab
            icon={<AccountTree />}
            label="Holons"
            iconPosition="start"
          />
        </Tabs>
      </Card>

      {/* Stats Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                {getIcon(currentData.type)}
                <Box sx={{ ml: 2 }}>
                  <Typography variant="h6">{metaDataStats.total}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total {currentData.type} MetaData
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
                  <Typography variant="h6">{metaDataStats.published}</Typography>
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
                <DataObject color="info" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{metaDataStats.types}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Types
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
                <Star color="warning" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{metaDataStats.averageRating.toFixed(1)}</Typography>
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
                placeholder={`Search ${currentData.type} MetaData...`}
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
                  <MenuItem value="Core">Core</MenuItem>
                  <MenuItem value="Utility">Utility</MenuItem>
                  <MenuItem value="Data">Data</MenuItem>
                  <MenuItem value="Logic">Logic</MenuItem>
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
                  <MenuItem value="type">Type</MenuItem>
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

      {/* MetaData Table */}
      <Card>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Description</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Rating</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {currentData.loading ? (
                Array.from({ length: 5 }).map((_, index) => (
                  <TableRow key={index}>
                    <TableCell colSpan={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center', py: 2 }}>
                        <Avatar sx={{ mr: 2 }} />
                        <Box>
                          <Typography variant="body1">Loading...</Typography>
                          <Typography variant="body2" color="text.secondary">
                            Loading {currentData.type} MetaData...
                          </Typography>
                        </Box>
                      </Box>
                    </TableCell>
                  </TableRow>
                ))
              ) : sortedMetaData.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} sx={{ textAlign: 'center', py: 4 }}>
                    <Box>
                      {getIcon(currentData.type)}
                      <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                        No {currentData.type} MetaData found
                      </Typography>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                        {searchTerm ? 'Try adjusting your search criteria' : `Create your first ${currentData.type} MetaData to get started`}
                      </Typography>
                      <Button
                        variant="contained"
                        startIcon={<Add />}
                        onClick={() => setCreateDialogOpen(true)}
                      >
                        Create {currentData.type} MetaData
                      </Button>
                    </Box>
                  </TableCell>
                </TableRow>
              ) : (
                sortedMetaData.map((item: any) => (
                  <TableRow key={item.id}>
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Avatar sx={{ mr: 2, bgcolor: `${getColor(currentData.type)}.main` }}>
                          {getIcon(currentData.type)}
                        </Avatar>
                        <Box>
                          <Typography variant="body1" fontWeight="medium">
                            {item.name}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            ID: {item.id}
                          </Typography>
                        </Box>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={item.type || 'Unknown'}
                        size="small"
                        color={getColor(currentData.type) as any}
                        variant="outlined"
                      />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" noWrap>
                        {item.description || 'No description'}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={item.isPublished ? 'Published' : 'Draft'}
                        size="small"
                        color={item.isPublished ? 'success' : 'default'}
                        variant="outlined"
                      />
                    </TableCell>
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Star sx={{ fontSize: 16, mr: 0.5, color: 'warning.main' }} />
                        <Typography variant="body2">
                          {item.rating || 0}
                        </Typography>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <IconButton
                        size="small"
                        onClick={(e) => handleMenuClick(e, item)}
                      >
                        <MoreVert />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>

      {/* Create MetaData Dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Create New {currentData.type} MetaData</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="MetaData Name"
                placeholder={`Enter ${currentData.type} MetaData name`}
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                placeholder="Enter MetaData description"
                multiline
                rows={3}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select defaultValue="Core">
                  <MenuItem value="Core">Core</MenuItem>
                  <MenuItem value="Utility">Utility</MenuItem>
                  <MenuItem value="Data">Data</MenuItem>
                  <MenuItem value="Logic">Logic</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Version"
                placeholder="1.0.0"
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Metadata (JSON)"
                placeholder='{"key": "value"}'
                multiline
                rows={4}
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
            onClick={() => handleCreateMetaData({})}
            disabled={createMetaDataMutation.isLoading}
          >
            {createMetaDataMutation.isLoading ? 'Creating...' : `Create ${currentData.type} MetaData`}
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
          if (selectedMetaData) navigate(`/metadata/${selectedMetaData.id}`);
          handleMenuClose();
        }}>
          <Visibility sx={{ mr: 1 }} />
          View Details
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedMetaData) handlePublishMetaData(selectedMetaData.id);
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
        aria-label="create metadata"
        sx={{ position: 'fixed', bottom: 16, right: 16 }}
        onClick={() => setCreateDialogOpen(true)}
      >
        <Add />
      </Fab>
    </Box>
  );
};

export default MetaDataPage;