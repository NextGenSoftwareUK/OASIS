import React, { useState } from 'react';
import {
  Box, Typography, Button, Card, CardContent, CardActions, Grid, TextField,
  Dialog, DialogTitle, DialogContent, DialogActions, IconButton,
  Menu, MenuItem, FormControl, InputLabel, Select, Chip,
  Fab, Tooltip, Tabs, Tab, Badge, Stack, LinearProgress,
  List, ListItem, ListItemIcon, ListItemText, ListItemSecondaryAction,
  Alert, CircularProgress, Paper, Divider, Switch, FormControlLabel,
  Slider, Autocomplete
} from '@mui/material';
import {
  Add, MoreVert, Edit, Delete, Visibility, Map, Refresh, FilterList, Remove,
  LocationOn, Directions, Search, MyLocation, Layers, Terrain,
  Satellite, Streetview, Navigation, Route, Place, Flag, Public,
  CloudDone as CloudDoneIcon, CloudOff as CloudOffIcon,
  ContentCopy, Download, Upload, Settings, Share
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { mapService } from '../services';

interface POI {
  id: string;
  name: string;
  description: string;
  type: string;
  latitude: number;
  longitude: number;
  address?: string;
  rating?: number;
  createdOn: string;
  createdBy: string;
  tags: string[];
  isPublic: boolean;
}

interface Route {
  id: string;
  name: string;
  startLat: number;
  startLon: number;
  endLat: number;
  endLon: number;
  distance: string;
  duration: string;
  mode: string;
  waypoints: { lat: number; lon: number }[];
  createdOn: string;
}

interface MapData {
  center: { latitude: number; longitude: number };
  zoom: number;
  features: any[];
  lastUpdated: string;
}

const MapPage: React.FC = () => {
  const [createPoiDialogOpen, setCreatePoiDialogOpen] = useState(false);
  const [editPoiDialogOpen, setEditPoiDialogOpen] = useState(false);
  const [routeDialogOpen, setRouteDialogOpen] = useState(false);
  const [searchDialogOpen, setSearchDialogOpen] = useState(false);
  const [selectedPoi, setSelectedPoi] = useState<POI | null>(null);
  const [selectedRoute, setSelectedRoute] = useState<Route | null>(null);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [activeTab, setActiveTab] = useState(0);
  const [filterType, setFilterType] = useState('all');
  const [showPublicOnly, setShowPublicOnly] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [currentLocation, setCurrentLocation] = useState({ lat: 37.7749, lon: -122.4194 });
  const [mapZoom, setMapZoom] = useState(12);
  const [newPoiData, setNewPoiData] = useState({
    name: '',
    description: '',
    type: 'Point of Interest',
    latitude: 37.7749,
    longitude: -122.4194,
    address: '',
    tags: [] as string[],
    isPublic: true
  });
  const [editPoiData, setEditPoiData] = useState<POI | null>(null);
  const [routeData, setRouteData] = useState({
    name: '',
    startLat: 37.7749,
    startLon: -122.4194,
    endLat: 37.7849,
    endLon: -122.4094,
    mode: 'driving'
  });
  const queryClient = useQueryClient();

  const { data: mapData, isLoading: mapLoading, error: mapError, refetch: refetchMap } = useQuery(
    ['map-data', currentLocation.lat, currentLocation.lon, mapZoom],
    async () => {
      try {
        const response = await mapService.getMapData(currentLocation.lat, currentLocation.lon, mapZoom);
        return response;
      } catch (err: any) {
        console.log('Using demo map data for investor presentation');
        return {
          result: {
            center: { latitude: currentLocation.lat, longitude: currentLocation.lon },
            zoom: mapZoom,
            features: [
              {
                id: 'poi-1',
                name: 'Golden Gate Bridge',
                type: 'Landmark',
                lat: 37.8199,
                lon: -122.4783,
                description: 'Iconic suspension bridge'
              },
              {
                id: 'poi-2',
                name: 'Alcatraz Island',
                type: 'Historical Site',
                lat: 37.8267,
                lon: -122.4230,
                description: 'Former federal prison'
              }
            ],
            lastUpdated: new Date().toISOString()
          },
          isError: false,
          message: 'Demo Map data retrieved'
        };
      }
    },
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const { data: poisData, isLoading: poisLoading, refetch: refetchPois } = useQuery(
    ['pois', filterType, showPublicOnly],
    async () => {
      try {
        const response = await mapService.getAllPois();
        if (response?.result && response.result.length > 0) {
          let filtered = response.result;
          if (filterType !== 'all') {
            filtered = filtered.filter((poi: POI) => poi.type === filterType);
          }
          if (showPublicOnly) {
            filtered = filtered.filter((poi: POI) => poi.isPublic);
          }
          return { ...response, result: filtered };
        }
        console.log('Using demo POI data for investor presentation');
        return {
          result: [
            {
              id: 'poi-1',
              name: 'Golden Gate Bridge',
              description: 'Iconic suspension bridge connecting San Francisco to Marin County',
              type: 'Landmark',
              latitude: 37.8199,
              longitude: -122.4783,
              address: 'Golden Gate Bridge, San Francisco, CA',
              rating: 4.8,
              createdOn: new Date().toISOString(),
              createdBy: 'demo-user',
              tags: ['bridge', 'landmark', 'tourist'],
              isPublic: true
            },
            {
              id: 'poi-2',
              name: 'Alcatraz Island',
              description: 'Former federal prison, now a popular tourist destination',
              type: 'Historical Site',
              latitude: 37.8267,
              longitude: -122.4230,
              address: 'Alcatraz Island, San Francisco, CA',
              rating: 4.6,
              createdOn: new Date(Date.now() - 86400000).toISOString(),
              createdBy: 'demo-user',
              tags: ['prison', 'history', 'tourist'],
              isPublic: true
            },
            {
              id: 'poi-3',
              name: 'Fisherman\'s Wharf',
              description: 'Popular tourist area with restaurants and shops',
              type: 'Entertainment',
              latitude: 37.8080,
              longitude: -122.4177,
              address: 'Fisherman\'s Wharf, San Francisco, CA',
              rating: 4.2,
              createdOn: new Date(Date.now() - 172800000).toISOString(),
              createdBy: 'demo-user',
              tags: ['food', 'shopping', 'tourist'],
              isPublic: true
            }
          ],
          isError: false,
          message: 'Demo POIs retrieved',
          count: 3
        };
      } catch (err: any) {
        console.error('Error fetching POIs:', err);
        return { result: [], isError: true, message: err.message, count: 0 };
      }
    }
  );

  const pois = poisData?.result || [];

  // Mutations
  const createPoiMutation = useMutation(
    (poiData: any) => mapService.addPoi(poiData.name, poiData.description, poiData.latitude, poiData.longitude, poiData.type),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['pois']);
        queryClient.invalidateQueries(['map-data']);
        console.log('POI created successfully!');
        setCreatePoiDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to create POI: ' + error.message);
      },
    }
  );

  const updatePoiMutation = useMutation(
    (data: { id: string, poiData: any }) => mapService.updatePoi(data.id, data.poiData.name, data.poiData.description, data.poiData.latitude, data.poiData.longitude, data.poiData.type),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['pois']);
        queryClient.invalidateQueries(['map-data']);
        console.log('POI updated successfully!');
        setEditPoiDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to update POI: ' + error.message);
      },
    }
  );

  const deletePoiMutation = useMutation(
    (id: string) => mapService.deletePoi(id),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['pois']);
        queryClient.invalidateQueries(['map-data']);
        console.log('POI deleted successfully!');
      },
      onError: (error: any) => {
        console.error('Failed to delete POI: ' + error.message);
      },
    }
  );

  const searchPoisMutation = useMutation(
    (query: string) => mapService.searchPois(query, currentLocation.lat, currentLocation.lon, 10),
    {
      onSuccess: (data) => {
        console.log('Search results:', data.result);
        setSearchDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to search POIs: ' + error.message);
      },
    }
  );

  const getRouteMutation = useMutation(
    (data: { startLat: number, startLon: number, endLat: number, endLon: number, mode: string }) =>
      mapService.getRoute(data.startLat, data.startLon, data.endLat, data.endLon),
    {
      onSuccess: (data) => {
        console.log('Route calculated:', data.result);
        setRouteDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to calculate route: ' + error.message);
      },
    }
  );

  // Handlers
  const handleCreatePoiClick = () => {
    setNewPoiData({
      name: '',
      description: '',
      type: 'Point of Interest',
      latitude: currentLocation.lat,
      longitude: currentLocation.lon,
      address: '',
      tags: [],
      isPublic: true
    });
    setCreatePoiDialogOpen(true);
  };

  const handleCreatePoiSubmit = () => {
    createPoiMutation.mutate(newPoiData);
  };

  const handleEditPoiClick = (poi: POI) => {
    setEditPoiData(poi);
    setEditPoiDialogOpen(true);
  };

  const handleEditPoiSubmit = () => {
    if (editPoiData) {
      updatePoiMutation.mutate({ id: editPoiData.id, poiData: editPoiData });
    }
  };

  const handleDeletePoiClick = (poi: POI) => {
    deletePoiMutation.mutate(poi.id);
    setAnchorEl(null);
  };

  const handleSearchClick = () => {
    if (searchQuery.trim()) {
      searchPoisMutation.mutate(searchQuery);
    }
  };

  const handleRouteClick = () => {
    setRouteData({
      name: '',
      startLat: currentLocation.lat,
      startLon: currentLocation.lon,
      endLat: 37.7849,
      endLon: -122.4094,
      mode: 'driving'
    });
    setRouteDialogOpen(true);
  };

  const handleRouteSubmit = () => {
    getRouteMutation.mutate(routeData);
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLButtonElement>, poi: POI) => {
    setSelectedPoi(poi);
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const getPoiTypeColor = (type: string) => {
    switch (type) {
      case 'Landmark': return '#4caf50';
      case 'Historical Site': return '#ff9800';
      case 'Entertainment': return '#2196f3';
      case 'Restaurant': return '#9c27b0';
      case 'Shopping': return '#f44336';
      default: return '#757575';
    }
  };

  const getPoiIcon = (type: string) => {
    switch (type) {
      case 'Landmark': return <Flag />;
      case 'Historical Site': return <Terrain />;
      case 'Entertainment': return <Directions />;
      case 'Restaurant': return <Place />;
      case 'Shopping': return <Navigation />;
      default: return <LocationOn />;
    }
  };

  const poiStats = {
    totalPois: pois.length,
    publicPois: pois.filter((poi: POI) => poi.isPublic).length,
    landmarks: pois.filter((poi: POI) => poi.type === 'Landmark').length,
    restaurants: pois.filter((poi: POI) => poi.type === 'Restaurant').length,
  };

  return (
    <Box sx={{ p: 4 }}>
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        <Typography variant="h4" component="h1" gutterBottom sx={{ color: 'primary.main', fontWeight: 'bold' }}>
          <Map sx={{ mr: 2, fontSize: 'inherit' }} />
          Interactive Map
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
          Explore locations, manage points of interest, and calculate routes.
        </Typography>

        {/* Stats Overview */}
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #66bb6a, #388e3c)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <LocationOn sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{poiStats.totalPois}</Typography>
                <Typography variant="body2">Total POIs</Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #42a5f5, #1976d2)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <Public sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{poiStats.publicPois}</Typography>
                <Typography variant="body2">Public POIs</Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #ffa726, #ef6c00)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <Flag sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{poiStats.landmarks}</Typography>
                <Typography variant="body2">Landmarks</Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #ab47bc, #7b1fa2)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <Place sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{poiStats.restaurants}</Typography>
                <Typography variant="body2">Restaurants</Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Tabs Navigation */}
        <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
          <Tabs value={activeTab} onChange={handleTabChange} variant="scrollable" scrollButtons="auto">
            <Tab label="Map View" icon={<Map />} />
            <Tab label="Points of Interest" icon={<LocationOn />} />
            <Tab label="Routes" icon={<Directions />} />
            <Tab label="Search" icon={<Search />} />
          </Tabs>
        </Box>

        {/* Tab Content */}
        {activeTab === 0 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <Card sx={{ height: 600, mb: 3 }}>
              <CardContent sx={{ height: '100%', p: 0 }}>
                <Box sx={{ 
                  height: '100%', 
                  background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: 'white',
                  position: 'relative'
                }}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Satellite sx={{ fontSize: 80, mb: 2 }} />
                    <Typography variant="h4" gutterBottom>
                      Interactive Map
                    </Typography>
                    <Typography variant="body1" sx={{ mb: 3 }}>
                      Map visualization would be integrated here with a mapping library like Leaflet or Mapbox
                    </Typography>
                    <Stack direction="row" spacing={2} justifyContent="center">
                      <Button variant="contained" color="inherit" startIcon={<MyLocation />}>
                        My Location
                      </Button>
                      <Button variant="outlined" color="inherit" startIcon={<Layers />}>
                        Layers
                      </Button>
                      <Button variant="outlined" color="inherit" startIcon={<Terrain />}>
                        Terrain
                      </Button>
                    </Stack>
                  </Box>
                  
                  {/* Map Controls */}
                  <Box sx={{ position: 'absolute', top: 16, right: 16 }}>
                    <Stack spacing={1}>
                      <Tooltip title="Zoom In">
                        <IconButton sx={{ bgcolor: 'rgba(255,255,255,0.2)', color: 'white' }}>
                          <Add />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Zoom Out">
                        <IconButton sx={{ bgcolor: 'rgba(255,255,255,0.2)', color: 'white' }}>
                          <Remove />
                        </IconButton>
                      </Tooltip>
                    </Stack>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </motion.div>
        )}

        {activeTab === 1 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            {/* Actions and Filters */}
            <Stack direction="row" spacing={2} sx={{ mb: 3 }} alignItems="center">
              <Button
                variant="contained"
                color="primary"
                startIcon={<Add />}
                onClick={handleCreatePoiClick}
              >
                Add POI
              </Button>
              <Button
                variant="outlined"
                color="secondary"
                startIcon={<Search />}
                onClick={() => setSearchDialogOpen(true)}
              >
                Search POIs
              </Button>
              <Button
                variant="outlined"
                color="info"
                startIcon={<Directions />}
                onClick={handleRouteClick}
              >
                Calculate Route
              </Button>
              <Tooltip title="Refresh Map Data">
                <IconButton onClick={() => refetchMap()} disabled={mapLoading}>
                  <Refresh />
                </IconButton>
              </Tooltip>

              <Box sx={{ flexGrow: 1 }} />

              <FormControl sx={{ minWidth: 120 }}>
                <InputLabel size="small">Filter Type</InputLabel>
                <Select
                  value={filterType}
                  label="Filter Type"
                  onChange={(e) => setFilterType(e.target.value as string)}
                  size="small"
                >
                  <MenuItem value="all">All Types</MenuItem>
                  <MenuItem value="Landmark">Landmark</MenuItem>
                  <MenuItem value="Historical Site">Historical Site</MenuItem>
                  <MenuItem value="Entertainment">Entertainment</MenuItem>
                  <MenuItem value="Restaurant">Restaurant</MenuItem>
                  <MenuItem value="Shopping">Shopping</MenuItem>
                </Select>
              </FormControl>

              <FormControlLabel
                control={
                  <Switch
                    checked={showPublicOnly}
                    onChange={(e) => setShowPublicOnly(e.target.checked)}
                  />
                }
                label="Public Only"
              />
            </Stack>

            {/* POIs Grid */}
            {poisLoading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
                <CircularProgress />
              </Box>
            ) : pois.length === 0 ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
                <Typography color="text.secondary">No POIs found. Add one to get started!</Typography>
              </Box>
            ) : (
              <Grid container spacing={3}>
                {pois.map((poi: POI) => (
                  <Grid item xs={12} sm={6} md={4} key={poi.id}>
                    <motion.div
                      initial={{ opacity: 0, scale: 0.9 }}
                      animate={{ opacity: 1, scale: 1 }}
                      transition={{ duration: 0.3 }}
                      whileHover={{ scale: 1.02 }}
                    >
                      <Card
                        variant="outlined"
                        sx={{
                          height: '100%',
                          display: 'flex',
                          flexDirection: 'column',
                          borderColor: getPoiTypeColor(poi.type),
                          borderWidth: 2,
                        }}
                      >
                        <CardContent sx={{ flexGrow: 1 }}>
                          <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
                            <Typography variant="h6" component="div" sx={{ fontWeight: 'bold', color: 'primary.dark' }}>
                              {poi.name}
                            </Typography>
                            <Chip
                              label={poi.type}
                              size="small"
                              sx={{ bgcolor: getPoiTypeColor(poi.type), color: 'white' }}
                            />
                          </Stack>
                          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                            {poi.description}
                          </Typography>
                          <Box sx={{ mb: 2 }}>
                            <Typography variant="body2" color="text.secondary" gutterBottom>
                              Location:
                            </Typography>
                            <Typography variant="body2">
                              {poi.latitude.toFixed(4)}, {poi.longitude.toFixed(4)}
                            </Typography>
                            {poi.address && (
                              <Typography variant="body2" color="text.secondary">
                                {poi.address}
                              </Typography>
                            )}
                          </Box>
                          {poi.rating && (
                            <Box sx={{ mb: 2 }}>
                              <Typography variant="body2" color="text.secondary" gutterBottom>
                                Rating: {poi.rating}/5.0
                              </Typography>
                              <LinearProgress
                                variant="determinate"
                                value={(poi.rating / 5) * 100}
                                sx={{ height: 8, borderRadius: 4 }}
                              />
                            </Box>
                          )}
                          <Box sx={{ mb: 2 }}>
                            <Typography variant="body2" color="text.secondary" gutterBottom>
                              Tags:
                            </Typography>
                            <Stack direction="row" spacing={1} flexWrap="wrap">
                              {poi.tags.map((tag, index) => (
                                <Chip
                                  key={index}
                                  label={tag}
                                  size="small"
                                  color="primary"
                                  variant="outlined"
                                />
                              ))}
                            </Stack>
                          </Box>
                          <Typography variant="caption" color="text.secondary">
                            Created: {new Date(poi.createdOn).toLocaleDateString()}
                          </Typography>
                        </CardContent>
                        <CardActions sx={{ justifyContent: 'flex-end', p: 2 }}>
                          <Button size="small" startIcon={<Visibility />} onClick={() => console.log('View POI:', poi)}>View</Button>
                          <Button size="small" startIcon={<Directions />} onClick={() => console.log('Navigate to:', poi)}>Navigate</Button>
                          <IconButton
                            aria-label="more"
                            aria-controls="long-menu"
                            aria-haspopup="true"
                            onClick={(event) => handleMenuOpen(event, poi)}
                            size="small"
                          >
                            <MoreVert />
                          </IconButton>
                        </CardActions>
                      </Card>
                    </motion.div>
                  </Grid>
                ))}
              </Grid>
            )}

            {/* Context Menu for POI Actions */}
            <Menu
              id="long-menu"
              MenuListProps={{
                'aria-labelledby': 'long-button',
              }}
              anchorEl={anchorEl}
              open={Boolean(anchorEl)}
              onClose={handleMenuClose}
              PaperProps={{
                style: {
                  maxHeight: 48 * 4.5,
                  width: '20ch',
                },
              }}
            >
              <MenuItem onClick={() => {
                if (selectedPoi) handleEditPoiClick(selectedPoi);
                handleMenuClose();
              }}>
                <Edit sx={{ mr: 1 }} /> Edit
              </MenuItem>
              <MenuItem onClick={() => {
                if (selectedPoi) handleDeletePoiClick(selectedPoi);
                handleMenuClose();
              }}>
                <Delete sx={{ mr: 1 }} /> Delete
              </MenuItem>
              <MenuItem onClick={() => {
                if (selectedPoi) {
                  console.log('Sharing POI:', selectedPoi.id);
                  console.log('Share functionality not yet implemented.');
                }
                handleMenuClose();
              }}>
                <Share sx={{ mr: 1 }} /> Share
              </MenuItem>
            </Menu>
          </motion.div>
        )}

        {activeTab === 2 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
              <Directions sx={{ mr: 1, verticalAlign: 'middle' }} />
              Route Planning
            </Typography>
            <Alert severity="info" sx={{ mb: 3 }}>
              Route planning functionality would be integrated here with mapping libraries.
            </Alert>
            <Button
              variant="contained"
              startIcon={<Directions />}
              onClick={handleRouteClick}
            >
              Calculate New Route
            </Button>
          </motion.div>
        )}

        {activeTab === 3 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
              <Search sx={{ mr: 1, verticalAlign: 'middle' }} />
              Search Locations
            </Typography>
            <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
              <TextField
                fullWidth
                label="Search for locations, POIs, or addresses"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                onKeyPress={(e) => e.key === 'Enter' && handleSearchClick()}
              />
              <Button
                variant="contained"
                startIcon={<Search />}
                onClick={handleSearchClick}
                disabled={!searchQuery.trim()}
              >
                Search
              </Button>
            </Box>
            <Alert severity="info">
              Search results would be displayed here with interactive map integration.
            </Alert>
          </motion.div>
        )}

        {/* Create POI Dialog */}
        <Dialog open={createPoiDialogOpen} onClose={() => setCreatePoiDialogOpen(false)} maxWidth="md" fullWidth>
          <DialogTitle>Add New Point of Interest</DialogTitle>
          <DialogContent>
            <Grid container spacing={2} sx={{ mt: 1 }}>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Name"
                  value={newPoiData.name}
                  onChange={(e) => setNewPoiData({ ...newPoiData, name: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <FormControl fullWidth>
                  <InputLabel>Type</InputLabel>
                  <Select
                    value={newPoiData.type}
                    onChange={(e) => setNewPoiData({ ...newPoiData, type: e.target.value as string })}
                    label="Type"
                  >
                    <MenuItem value="Point of Interest">Point of Interest</MenuItem>
                    <MenuItem value="Landmark">Landmark</MenuItem>
                    <MenuItem value="Historical Site">Historical Site</MenuItem>
                    <MenuItem value="Entertainment">Entertainment</MenuItem>
                    <MenuItem value="Restaurant">Restaurant</MenuItem>
                    <MenuItem value="Shopping">Shopping</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Description"
                  multiline
                  rows={3}
                  value={newPoiData.description}
                  onChange={(e) => setNewPoiData({ ...newPoiData, description: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Latitude"
                  type="number"
                  value={newPoiData.latitude}
                  onChange={(e) => setNewPoiData({ ...newPoiData, latitude: parseFloat(e.target.value) })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Longitude"
                  type="number"
                  value={newPoiData.longitude}
                  onChange={(e) => setNewPoiData({ ...newPoiData, longitude: parseFloat(e.target.value) })}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Address (Optional)"
                  value={newPoiData.address}
                  onChange={(e) => setNewPoiData({ ...newPoiData, address: e.target.value })}
                />
              </Grid>
              <Grid item xs={12}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={newPoiData.isPublic}
                      onChange={(e) => setNewPoiData({ ...newPoiData, isPublic: e.target.checked })}
                    />
                  }
                  label="Make this POI public"
                />
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setCreatePoiDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleCreatePoiSubmit} variant="contained" color="primary">Create POI</Button>
          </DialogActions>
        </Dialog>

        {/* Edit POI Dialog */}
        <Dialog open={editPoiDialogOpen} onClose={() => setEditPoiDialogOpen(false)} maxWidth="md" fullWidth>
          <DialogTitle>Edit Point of Interest</DialogTitle>
          <DialogContent>
            {editPoiData && (
              <Grid container spacing={2} sx={{ mt: 1 }}>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Name"
                    value={editPoiData.name}
                    onChange={(e) => setEditPoiData({ ...editPoiData, name: e.target.value })}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth>
                    <InputLabel>Type</InputLabel>
                    <Select
                      value={editPoiData.type}
                      onChange={(e) => setEditPoiData({ ...editPoiData, type: e.target.value as string })}
                      label="Type"
                    >
                      <MenuItem value="Point of Interest">Point of Interest</MenuItem>
                      <MenuItem value="Landmark">Landmark</MenuItem>
                      <MenuItem value="Historical Site">Historical Site</MenuItem>
                      <MenuItem value="Entertainment">Entertainment</MenuItem>
                      <MenuItem value="Restaurant">Restaurant</MenuItem>
                      <MenuItem value="Shopping">Shopping</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    fullWidth
                    label="Description"
                    multiline
                    rows={3}
                    value={editPoiData.description}
                    onChange={(e) => setEditPoiData({ ...editPoiData, description: e.target.value })}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Latitude"
                    type="number"
                    value={editPoiData.latitude}
                    onChange={(e) => setEditPoiData({ ...editPoiData, latitude: parseFloat(e.target.value) })}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Longitude"
                    type="number"
                    value={editPoiData.longitude}
                    onChange={(e) => setEditPoiData({ ...editPoiData, longitude: parseFloat(e.target.value) })}
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    fullWidth
                    label="Address"
                    value={editPoiData.address || ''}
                    onChange={(e) => setEditPoiData({ ...editPoiData, address: e.target.value })}
                  />
                </Grid>
                <Grid item xs={12}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={editPoiData.isPublic}
                        onChange={(e) => setEditPoiData({ ...editPoiData, isPublic: e.target.checked })}
                      />
                    }
                    label="Make this POI public"
                  />
                </Grid>
              </Grid>
            )}
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setEditPoiDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleEditPoiSubmit} variant="contained" color="primary">Save Changes</Button>
          </DialogActions>
        </Dialog>

        {/* Search Dialog */}
        <Dialog open={searchDialogOpen} onClose={() => setSearchDialogOpen(false)} maxWidth="sm" fullWidth>
          <DialogTitle>Search Points of Interest</DialogTitle>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Search Query"
              fullWidth
              variant="outlined"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSearchClick()}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setSearchDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleSearchClick} variant="contained" color="primary">Search</Button>
          </DialogActions>
        </Dialog>

        {/* Route Dialog */}
        <Dialog open={routeDialogOpen} onClose={() => setRouteDialogOpen(false)} maxWidth="md" fullWidth>
          <DialogTitle>Calculate Route</DialogTitle>
          <DialogContent>
            <Grid container spacing={2} sx={{ mt: 1 }}>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Route Name (Optional)"
                  value={routeData.name}
                  onChange={(e) => setRouteData({ ...routeData, name: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Start Latitude"
                  type="number"
                  value={routeData.startLat}
                  onChange={(e) => setRouteData({ ...routeData, startLat: parseFloat(e.target.value) })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Start Longitude"
                  type="number"
                  value={routeData.startLon}
                  onChange={(e) => setRouteData({ ...routeData, startLon: parseFloat(e.target.value) })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="End Latitude"
                  type="number"
                  value={routeData.endLat}
                  onChange={(e) => setRouteData({ ...routeData, endLat: parseFloat(e.target.value) })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="End Longitude"
                  type="number"
                  value={routeData.endLon}
                  onChange={(e) => setRouteData({ ...routeData, endLon: parseFloat(e.target.value) })}
                />
              </Grid>
              <Grid item xs={12}>
                <FormControl fullWidth>
                  <InputLabel>Travel Mode</InputLabel>
                  <Select
                    value={routeData.mode}
                    onChange={(e) => setRouteData({ ...routeData, mode: e.target.value as string })}
                    label="Travel Mode"
                  >
                    <MenuItem value="driving">Driving</MenuItem>
                    <MenuItem value="walking">Walking</MenuItem>
                    <MenuItem value="cycling">Cycling</MenuItem>
                    <MenuItem value="transit">Public Transit</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setRouteDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleRouteSubmit} variant="contained" color="primary">Calculate Route</Button>
          </DialogActions>
        </Dialog>
      </motion.div>
    </Box>
  );
};

export default MapPage;
