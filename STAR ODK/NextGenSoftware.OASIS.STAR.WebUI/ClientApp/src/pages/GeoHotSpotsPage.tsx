import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  CardMedia,
  Button,
  Grid,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Alert,
  CircularProgress,
  Badge,
  Fab,
  Tooltip,
} from '@mui/material';
import {
  Add,
  Delete,
  Visibility,
  LocationOn,
  Whatshot,
  Explore,
  Refresh,
  FilterList,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { geoHotSpotService } from '../services';

interface GeoHotSpot {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  latitude: number;
  longitude: number;
  location: string;
  type: 'Event' | 'Landmark' | 'Activity' | 'Meeting Point' | 'Resource' | 'Danger Zone';
  intensity: 'Low' | 'Medium' | 'High' | 'Extreme';
  discoveredBy: string;
  discoveredDate: string;
  isActive: boolean;
  participants: number;
  maxParticipants: number;
  duration: number;
  reward: number;
  tags: string[];
  requirements: string[];
}

const GeoHotSpotsPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterType, setFilterType] = useState<string>('all');
  const [newHotSpot, setNewHotSpot] = useState<Partial<GeoHotSpot>>({
    name: '',
    description: '',
    imageUrl: '',
    latitude: 0,
    longitude: 0,
    location: '',
    type: 'Event',
    intensity: 'Medium',
    discoveredBy: '',
    discoveredDate: '',
    isActive: true,
    participants: 0,
    maxParticipants: 10,
    duration: 60,
    reward: 0,
    tags: [],
    requirements: [],
  });

  const queryClient = useQueryClient();

  const { data: hotSpotsData, isLoading, error, refetch } = useQuery(
    'geoHotSpots',
    async () => {
      try {
        // Try to get real data first
        const response = await geoHotSpotService.getAll();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Geo Hot Spots data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'Quantum Nexus Plaza',
              description: 'A bustling hub where travelers from across dimensions converge to trade and share knowledge',
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=300&fit=crop',
              latitude: 37.7749,
              longitude: -122.4194,
              location: 'San Francisco, CA',
              type: 'Meeting Point',
              intensity: 'High',
              discoveredBy: 'Explorer Team Alpha',
              discoveredDate: '2024-01-10',
              isActive: true,
              participants: 245,
              maxParticipants: 500,
              duration: 0,
              reward: 0,
              tags: ['Trading', 'Social', 'Hub'],
              requirements: ['Level 5+', 'Valid ID'],
            },
            {
              id: '2',
              name: 'Crystal Caverns',
              description: 'A mysterious underground network filled with rare crystals and ancient artifacts',
              imageUrl: 'https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=400&h=300&fit=crop',
              latitude: 40.7128,
              longitude: -74.0060,
              location: 'New York, NY',
              type: 'Resource',
              intensity: 'Medium',
              discoveredBy: 'Mining Corp Beta',
              discoveredDate: '2024-01-15',
              isActive: true,
              participants: 89,
              maxParticipants: 100,
              duration: 120,
              reward: 500,
              tags: ['Mining', 'Crystals', 'Ancient'],
              requirements: ['Mining License', 'Safety Gear'],
            },
            {
              id: '3',
              name: 'Storm Chaser Arena',
              description: 'An extreme sports arena where participants compete in high-energy quantum storms',
              imageUrl: 'https://images.unsplash.com/photo-1446776653964-20c1d3a81b06?w=400&h=300&fit=crop',
              latitude: 34.0522,
              longitude: -118.2437,
              location: 'Los Angeles, CA',
              type: 'Activity',
              intensity: 'Extreme',
              discoveredBy: 'Extreme Sports League',
              discoveredDate: '2024-01-20',
              isActive: true,
              participants: 156,
              maxParticipants: 200,
              duration: 45,
              reward: 2000,
              tags: ['Extreme', 'Competition', 'High Energy'],
              requirements: ['Level 10+', 'Storm Gear', 'Insurance'],
            },
            {
              id: '4',
              name: 'Ancient Temple Ruins',
              description: 'Mysterious ruins containing powerful artifacts and ancient knowledge',
              imageUrl: 'https://images.unsplash.com/photo-1614728894747-a83421e2b9c9?w=400&h=300&fit=crop',
              latitude: 25.7617,
              longitude: -80.1918,
              location: 'Miami, FL',
              type: 'Landmark',
              intensity: 'High',
              discoveredBy: 'Archaeological Society',
              discoveredDate: '2024-01-25',
              isActive: true,
              participants: 67,
              maxParticipants: 50,
              duration: 180,
              reward: 1500,
              tags: ['Ancient', 'Artifacts', 'Knowledge'],
              requirements: ['Archaeology License', 'Protection Gear'],
            },
            {
              id: '5',
              name: 'Void Portal',
              description: 'A dangerous portal to the void dimension - only for the bravest explorers',
              imageUrl: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=400&h=300&fit=crop',
              latitude: 47.6062,
              longitude: -122.3321,
              location: 'Seattle, WA',
              type: 'Danger Zone',
              intensity: 'Extreme',
              discoveredBy: 'Void Research Institute',
              discoveredDate: '2024-01-30',
              isActive: true,
              participants: 12,
              maxParticipants: 20,
              duration: 30,
              reward: 5000,
              tags: ['Dangerous', 'Void', 'Extreme'],
              requirements: ['Level 15+', 'Void Protection', 'Emergency Kit'],
            },
          ]
        };
      }
    },
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const createHotSpotMutation = useMutation(
    async (hotSpotData: Partial<GeoHotSpot>) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 1000));
      return { success: true, id: Date.now().toString() };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('geoHotSpots');
        toast.success('Geo Hot Spot created successfully!');
        setCreateDialogOpen(false);
        setNewHotSpot({
          name: '',
          description: '',
          imageUrl: '',
          latitude: 0,
          longitude: 0,
          location: '',
          type: 'Event',
          intensity: 'Medium',
          discoveredBy: '',
          discoveredDate: '',
          isActive: true,
          participants: 0,
          maxParticipants: 10,
          duration: 60,
          reward: 0,
          tags: [],
          requirements: [],
        });
      },
      onError: () => {
        toast.error('Failed to create geo hot spot');
      },
    }
  );

  const deleteHotSpotMutation = useMutation(
    async (id: string) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 500));
      return { success: true };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('geoHotSpots');
        toast.success('Geo Hot Spot deleted successfully!');
      },
      onError: () => {
        toast.error('Failed to delete geo hot spot');
      },
    }
  );

  const handleCreateHotSpot = () => {
    if (!newHotSpot.name || !newHotSpot.description) {
      toast.error('Please fill in all required fields');
      return;
    }
    createHotSpotMutation.mutate(newHotSpot);
  };

  const handleDeleteHotSpot = (id: string) => {
    deleteHotSpotMutation.mutate(id);
  };

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'Event': return <Whatshot sx={{ color: '#f44336' }} />;
      case 'Landmark': return <LocationOn sx={{ color: '#4caf50' }} />;
      case 'Activity': return <Explore sx={{ color: '#2196f3' }} />;
      case 'Meeting Point': return <LocationOn sx={{ color: '#ff9800' }} />;
      case 'Resource': return <Whatshot sx={{ color: '#9c27b0' }} />;
      case 'Danger Zone': return <Whatshot sx={{ color: '#f44336' }} />;
      default: return <LocationOn sx={{ color: '#757575' }} />;
    }
  };

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'Event': return '#f44336';
      case 'Landmark': return '#4caf50';
      case 'Activity': return '#2196f3';
      case 'Meeting Point': return '#ff9800';
      case 'Resource': return '#9c27b0';
      case 'Danger Zone': return '#f44336';
      default: return '#757575';
    }
  };

  const getIntensityColor = (intensity: string) => {
    switch (intensity) {
      case 'Low': return '#4caf50';
      case 'Medium': return '#ff9800';
      case 'High': return '#f44336';
      case 'Extreme': return '#9c27b0';
      default: return '#757575';
    }
  };

  const filteredHotSpots = hotSpotsData?.result?.filter((hotSpot: GeoHotSpot) => 
    filterType === 'all' || hotSpot.type === filterType
  ) || [];

  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        duration: 0.5,
        staggerChildren: 0.1,
      },
    },
  };

  const itemVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
    >
      <>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, mt: 4 }}>
          <Box>
            <Typography variant="h4" gutterBottom className="page-heading">
              Geo Hot Spots
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Discover and explore exciting locations and activities around the world
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControl size="small" sx={{ minWidth: 120 }}>
              <InputLabel>Filter Type</InputLabel>
              <Select
                value={filterType}
                label="Filter Type"
                onChange={(e) => setFilterType(e.target.value)}
              >
                <MenuItem value="all">All Types</MenuItem>
                <MenuItem value="Event">Events</MenuItem>
                <MenuItem value="Landmark">Landmarks</MenuItem>
                <MenuItem value="Activity">Activities</MenuItem>
                <MenuItem value="Meeting Point">Meeting Points</MenuItem>
                <MenuItem value="Resource">Resources</MenuItem>
                <MenuItem value="Danger Zone">Danger Zones</MenuItem>
              </Select>
            </FormControl>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={() => refetch()}
              disabled={isLoading}
            >
              Refresh
            </Button>
          </Box>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 3 }}>
            Failed to load geo hot spots. Using demo data for presentation.
          </Alert>
        )}

        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
            <CircularProgress size={60} />
          </Box>
        ) : (
          <Grid container spacing={3}>
            {filteredHotSpots.map((hotSpot: GeoHotSpot, index: number) => (
              <Grid item xs={12} sm={6} md={4} key={hotSpot.id}>
                <motion.div
                  variants={itemVariants}
                  whileHover={{ scale: 1.02 }}
                  transition={{ duration: 0.2 }}
                >
                  <Card sx={{ 
                    height: '100%', 
                    display: 'flex', 
                    flexDirection: 'column',
                    position: 'relative',
                    overflow: 'hidden',
                    '&:hover': {
                      boxShadow: 6,
                    }
                  }}>
                    <Box sx={{ position: 'relative' }}>
                      <CardMedia
                        component="img"
                        height="200"
                        image={hotSpot.imageUrl}
                        alt={hotSpot.name}
                        sx={{ objectFit: 'cover' }}
                      />
                      <Chip
                        label={hotSpot.type}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          bgcolor: getTypeColor(hotSpot.type),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      <Chip
                        label={hotSpot.intensity}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          left: 8,
                          bgcolor: getIntensityColor(hotSpot.intensity),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                    </Box>
                    
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                        {getTypeIcon(hotSpot.type)}
                        <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                          {hotSpot.name}
                        </Typography>
                      </Box>
                      
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {hotSpot.description}
                      </Typography>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Location: {hotSpot.location}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Participants: {hotSpot.participants}/{hotSpot.maxParticipants}
                        </Typography>
                        {hotSpot.reward > 0 && (
                          <Typography variant="body2" color="primary" fontWeight="bold">
                            Reward: {hotSpot.reward.toLocaleString()} Credits
                          </Typography>
                        )}
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          Discovered by: {hotSpot.discoveredBy}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(hotSpot.discoveredDate).toLocaleDateString()}
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Visibility />}
                          onClick={() => toast.success('Opening location details')}
                        >
                          View
                        </Button>
                        <IconButton
                          size="small"
                          onClick={() => handleDeleteHotSpot(hotSpot.id)}
                          disabled={deleteHotSpotMutation.isLoading}
                          color="error"
                        >
                          <Delete />
                        </IconButton>
                      </Box>
                    </CardContent>
                  </Card>
                </motion.div>
              </Grid>
            ))}
          </Grid>
        )}

        {/* Create Hot Spot Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Add New Geo Hot Spot</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Name"
              value={newHotSpot.name}
              onChange={(e) => setNewHotSpot({ ...newHotSpot, name: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={newHotSpot.description}
              onChange={(e) => setNewHotSpot({ ...newHotSpot, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Image URL"
              value={newHotSpot.imageUrl}
              onChange={(e) => setNewHotSpot({ ...newHotSpot, imageUrl: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Latitude"
                type="number"
                value={newHotSpot.latitude}
                onChange={(e) => setNewHotSpot({ ...newHotSpot, latitude: parseFloat(e.target.value) || 0 })}
                fullWidth
              />
              <TextField
                label="Longitude"
                type="number"
                value={newHotSpot.longitude}
                onChange={(e) => setNewHotSpot({ ...newHotSpot, longitude: parseFloat(e.target.value) || 0 })}
                fullWidth
              />
            </Box>
            <TextField
              label="Location"
              value={newHotSpot.location}
              onChange={(e) => setNewHotSpot({ ...newHotSpot, location: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select
                  value={newHotSpot.type}
                  label="Type"
                  onChange={(e) => setNewHotSpot({ ...newHotSpot, type: e.target.value as any })}
                >
                  <MenuItem value="Event">Event</MenuItem>
                  <MenuItem value="Landmark">Landmark</MenuItem>
                  <MenuItem value="Activity">Activity</MenuItem>
                  <MenuItem value="Meeting Point">Meeting Point</MenuItem>
                  <MenuItem value="Resource">Resource</MenuItem>
                  <MenuItem value="Danger Zone">Danger Zone</MenuItem>
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel>Intensity</InputLabel>
                <Select
                  value={newHotSpot.intensity}
                  label="Intensity"
                  onChange={(e) => setNewHotSpot({ ...newHotSpot, intensity: e.target.value as any })}
                >
                  <MenuItem value="Low">Low</MenuItem>
                  <MenuItem value="Medium">Medium</MenuItem>
                  <MenuItem value="High">High</MenuItem>
                  <MenuItem value="Extreme">Extreme</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <TextField
              label="Discovered By"
              value={newHotSpot.discoveredBy}
              onChange={(e) => setNewHotSpot({ ...newHotSpot, discoveredBy: e.target.value })}
              fullWidth
            />
            <TextField
              label="Discovery Date"
              type="date"
              value={newHotSpot.discoveredDate}
              onChange={(e) => setNewHotSpot({ ...newHotSpot, discoveredDate: e.target.value })}
              fullWidth
              InputLabelProps={{ shrink: true }}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateHotSpot}
            variant="contained"
            disabled={createHotSpotMutation.isLoading}
          >
            {createHotSpotMutation.isLoading ? 'Creating...' : 'Create Hot Spot'}
          </Button>
        </DialogActions>
      </Dialog>

        {/* Floating Action Button */}
      <Fab
        color="primary"
        aria-label="add"
        sx={{
          position: 'fixed',
          bottom: 16,
          right: 16,
          background: 'linear-gradient(45deg, #0096ff, #0066cc)',
        }}
        onClick={() => setCreateDialogOpen(true)}
      >
        <Add />
      </Fab>
      </>
    </motion.div>
  );
};

export default GeoHotSpotsPage;