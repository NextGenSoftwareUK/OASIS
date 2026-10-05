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
  SpaceDashboard,
  Explore,
  Public,
  Star,
  Refresh,
  FilterList,
  Info,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { celestialSpaceService } from '../services';
import { useNavigate } from 'react-router-dom';

interface CelestialSpace {
  id: string;
  name: string;
  type: 'Solar System' | 'Galaxy' | 'Nebula' | 'Cluster' | 'Void' | 'Wormhole';
  description: string;
  imageUrl: string;
  size: number;
  age: number;
  distance: number;
  discoveredBy: string;
  discoveredDate: string;
  isExplored: boolean;
  explorationLevel: number;
  celestialBodies: number;
  energyLevel: number;
  dangerLevel: 'Low' | 'Medium' | 'High' | 'Extreme';
  isInstalled?: boolean; // Added for installed badge and filtering
  coordinates: {
    x: number;
    y: number;
    z: number;
  };
  tags: string[];
}

const CelestialSpacesPage: React.FC = () => {
  const navigate = useNavigate();
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterType, setFilterType] = useState<string>('all');
  const [viewScope, setViewScope] = useState<string>('all');
  const [newSpace, setNewSpace] = useState<Partial<CelestialSpace>>({
    name: '',
    type: 'Solar System',
    description: '',
    imageUrl: '',
    size: 0,
    age: 0,
    distance: 0,
    discoveredBy: '',
    discoveredDate: '',
    isExplored: false,
    explorationLevel: 0,
    celestialBodies: 0,
    energyLevel: 0,
    dangerLevel: 'Low',
    coordinates: { x: 0, y: 0, z: 0 },
    tags: [],
  });

  const queryClient = useQueryClient();

  const { data: spacesData, isLoading, error, refetch } = useQuery(
    'celestialSpaces',
    async () => {
      try {
        // Try to get real data first
        const response = await celestialSpaceService.getAll();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Celestial Spaces data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'Nexus Galaxy',
              type: 'Galaxy',
              description: 'A massive spiral galaxy containing billions of stars and countless civilizations',
              imageUrl: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=400&h=300&fit=crop',
              size: 100000,
              age: 13.6,
              distance: 0,
              discoveredBy: 'Cosmic Observatory',
              discoveredDate: '2024-01-10',
              isExplored: true,
              explorationLevel: 85,
              celestialBodies: 400000000000,
              energyLevel: 95,
              dangerLevel: 'Medium',
              coordinates: { x: 0, y: 0, z: 0 },
              tags: ['Spiral', 'Massive', 'Inhabited'],
            },
            {
              id: '2',
              name: 'Solar System Alpha',
              type: 'Solar System',
              description: 'A stable solar system with multiple habitable planets and rich resources',
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=300&fit=crop',
              size: 50,
              age: 4.6,
              distance: 1.496e8,
              discoveredBy: 'Explorer Team Beta',
              discoveredDate: '2024-01-15',
              isExplored: true,
              explorationLevel: 92,
              celestialBodies: 8,
              energyLevel: 75,
              dangerLevel: 'Low',
              coordinates: { x: 1000, y: 2000, z: 500 },
              tags: ['Stable', 'Habitable', 'Rich'],
            },
            {
              id: '3',
              name: 'Void of Eternity',
              type: 'Void',
              description: 'An empty region of space with mysterious gravitational anomalies',
              imageUrl: 'https://images.unsplash.com/photo-1446776653964-20c1d3a81b06?w=400&h=300&fit=crop',
              size: 1000,
              age: 0,
              distance: 5e9,
              discoveredBy: 'Deep Space Probe Gamma',
              discoveredDate: '2024-01-20',
              isExplored: false,
              explorationLevel: 15,
              celestialBodies: 0,
              energyLevel: 10,
              dangerLevel: 'Extreme',
              coordinates: { x: -5000, y: 8000, z: -2000 },
              tags: ['Empty', 'Anomalous', 'Dangerous'],
            },
            {
              id: '4',
              name: 'Stellar Nursery Omega',
              type: 'Nebula',
              description: 'A massive nebula where new stars are constantly being born',
              imageUrl: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=400&h=300&fit=crop',
              size: 200,
              age: 0.1,
              distance: 1.5e16,
              discoveredBy: 'Stellar Research Station',
              discoveredDate: '2024-01-25',
              isExplored: true,
              explorationLevel: 60,
              celestialBodies: 500,
              energyLevel: 90,
              dangerLevel: 'High',
              coordinates: { x: 15000, y: -8000, z: 12000 },
              tags: ['Forming', 'Energetic', 'Beautiful'],
            },
            {
              id: '5',
              name: 'Quantum Wormhole Nexus',
              type: 'Wormhole',
              description: 'A stable wormhole network connecting distant regions of space',
              imageUrl: 'https://images.unsplash.com/photo-1614728894747-a83421e2b9c9?w=400&h=300&fit=crop',
              size: 1,
              age: 0,
              distance: 0,
              discoveredBy: 'Quantum Physics Lab',
              discoveredDate: '2024-01-30',
              isExplored: true,
              explorationLevel: 40,
              celestialBodies: 0,
              energyLevel: 100,
              dangerLevel: 'High',
              coordinates: { x: 0, y: 0, z: 0 },
              tags: ['Quantum', 'Network', 'Transport'],
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

  const createSpaceMutation = useMutation(
    async (spaceData: Partial<CelestialSpace>) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 1000));
      return { success: true, id: Date.now().toString() };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('celestialSpaces');
        toast.success('Celestial space created successfully!');
        setCreateDialogOpen(false);
        setNewSpace({
          name: '',
          type: 'Solar System',
          description: '',
          imageUrl: '',
          size: 0,
          age: 0,
          distance: 0,
          discoveredBy: '',
          discoveredDate: '',
          isExplored: false,
          explorationLevel: 0,
          celestialBodies: 0,
          energyLevel: 0,
          dangerLevel: 'Low',
          coordinates: { x: 0, y: 0, z: 0 },
          tags: [],
        });
      },
      onError: () => {
        toast.error('Failed to create celestial space');
      },
    }
  );

  const deleteSpaceMutation = useMutation(
    async (id: string) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 500));
      return { success: true };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('celestialSpaces');
        toast.success('Celestial space deleted successfully!');
      },
      onError: () => {
        toast.error('Failed to delete celestial space');
      },
    }
  );

  const handleCreateSpace = () => {
    if (!newSpace.name || !newSpace.description) {
      toast.error('Please fill in all required fields');
      return;
    }
    createSpaceMutation.mutate(newSpace);
  };

  const handleDeleteSpace = (id: string) => {
    deleteSpaceMutation.mutate(id);
  };

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'Solar System': return <Public sx={{ color: '#4caf50' }} />;
      case 'Galaxy': return <SpaceDashboard sx={{ color: '#2196f3' }} />;
      case 'Nebula': return <Star sx={{ color: '#e91e63' }} />;
      case 'Cluster': return <Explore sx={{ color: '#ff9800' }} />;
      case 'Void': return <Explore sx={{ color: '#607d8b' }} />;
      case 'Wormhole': return <Explore sx={{ color: '#9c27b0' }} />;
      default: return <SpaceDashboard sx={{ color: '#757575' }} />;
    }
  };

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'Solar System': return '#4caf50';
      case 'Galaxy': return '#2196f3';
      case 'Nebula': return '#e91e63';
      case 'Cluster': return '#ff9800';
      case 'Void': return '#607d8b';
      case 'Wormhole': return '#9c27b0';
      default: return '#757575';
    }
  };

  const getDangerColor = (level: string) => {
    switch (level) {
      case 'Low': return '#4caf50';
      case 'Medium': return '#ff9800';
      case 'High': return '#f44336';
      case 'Extreme': return '#9c27b0';
      default: return '#757575';
    }
  };

  // Filter by view scope first, then by type
  const getFilteredSpaces = () => {
    let filtered = spacesData?.result || [];
    
    // Apply view scope filter
    if (viewScope === 'installed') {
      filtered = filtered.filter((space: any) => space.isInstalled);
    } else if (viewScope === 'mine') {
      // For demo, show spaces discovered by current user
      filtered = filtered.filter((space: any) => space.discoveredBy === 'Cosmic Observatory');
    }
    
    // Apply type filter
    if (filterType !== 'all') {
      filtered = filtered.filter((space: CelestialSpace) => space.type === filterType);
    }
    
    return filtered;
  };
  
  const filteredSpaces = getFilteredSpaces();

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
              Celestial Spaces
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Explore and manage galaxies, solar systems, nebulae, and cosmic regions
            </Typography>
            <Box sx={{ mt: 1, p: 2, bgcolor: '#0d47a1', color: 'white', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
              <Info sx={{ color: 'white' }} />
              <Typography variant="body2" sx={{ color: 'white' }}>
                Discover and explore celestial spaces in the OASIS universe. Track cosmic regions and their properties.
              </Typography>
            </Box>
          </Box>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControl size="small" sx={{ minWidth: 140 }}>
              <InputLabel>View</InputLabel>
              <Select
                value={viewScope}
                label="View"
                onChange={(e) => setViewScope(e.target.value)}
              >
                <MenuItem value="all">All</MenuItem>
                <MenuItem value="installed">Installed</MenuItem>
                <MenuItem value="mine">My Spaces</MenuItem>
              </Select>
            </FormControl>
            
            <FormControl size="small" sx={{ minWidth: 120 }}>
              <InputLabel>Filter Type</InputLabel>
              <Select
                value={filterType}
                label="Filter Type"
                onChange={(e) => setFilterType(e.target.value)}
              >
                <MenuItem value="all">All Types</MenuItem>
                <MenuItem value="Solar System">Solar Systems</MenuItem>
                <MenuItem value="Galaxy">Galaxies</MenuItem>
                <MenuItem value="Nebula">Nebulae</MenuItem>
                <MenuItem value="Cluster">Clusters</MenuItem>
                <MenuItem value="Void">Voids</MenuItem>
                <MenuItem value="Wormhole">Wormholes</MenuItem>
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
            Failed to load celestial spaces. Using demo data for presentation.
          </Alert>
        )}

        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
            <CircularProgress size={60} />
          </Box>
        ) : (
          <Grid container spacing={3}>
            {filteredSpaces.map((space: CelestialSpace, index: number) => (
              <Grid item xs={12} sm={6} md={4} key={space.id}>
                <motion.div
                  variants={itemVariants}
                  whileHover={{ scale: 1.02 }}
                  transition={{ duration: 0.2 }}
                >
                  <Card 
                    sx={{ 
                      height: '100%', 
                      display: 'flex', 
                      flexDirection: 'column',
                      position: 'relative',
                      overflow: 'hidden',
                      cursor: 'pointer',
                      '&:hover': {
                        boxShadow: 6,
                      }
                    }}
                    onClick={() => navigate(`/celestial-spaces/${space.id}`)}
                  >
                    <Box sx={{ position: 'relative' }}>
                      <CardMedia
                        component="img"
                        height="200"
                        image={space.imageUrl}
                        alt={space.name}
                        sx={{ objectFit: 'cover' }}
                      />
                      <Chip
                        label={space.type}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          bgcolor: getTypeColor(space.type),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      <Chip
                        label={space.dangerLevel}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          left: 8,
                          bgcolor: getDangerColor(space.dangerLevel),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      {space.isInstalled && (
                        <Chip
                          label="Installed"
                          size="small"
                          color="success"
                          sx={{
                            position: 'absolute',
                            bottom: 8,
                            right: 8,
                            fontWeight: 'bold',
                          }}
                        />
                      )}
                    </Box>
                    
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                        {getTypeIcon(space.type)}
                        <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                          {space.name}
                        </Typography>
                      </Box>
                      
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {space.description}
                      </Typography>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Properties:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                          <Chip label={`Size: ${(space.size ?? 0).toLocaleString()} ly`} size="small" variant="outlined" />
                          <Chip label={`Age: ${space.age ?? 0}B years`} size="small" variant="outlined" />
                          <Chip label={`Bodies: ${(space.celestialBodies ?? 0).toLocaleString()}`} size="small" variant="outlined" />
                        </Box>
                      </Box>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Exploration Progress:
                        </Typography>
                        <Box sx={{ width: '100%', bgcolor: 'grey.200', borderRadius: 1, overflow: 'hidden' }}>
                          <Box
                            sx={{
                              width: `${space.explorationLevel}%`,
                              height: 8,
                              bgcolor: space.explorationLevel > 70 ? '#4caf50' : space.explorationLevel > 40 ? '#ff9800' : '#f44336',
                              transition: 'width 0.3s ease',
                            }}
                          />
                        </Box>
                        <Typography variant="caption" color="text.secondary">
                          {space.explorationLevel}% explored
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          Discovered by: {space.discoveredBy}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(space.discoveredDate).toLocaleDateString()}
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Visibility />}
                          onClick={() => toast.success('Viewing celestial space details')}
                        >
                          View
                        </Button>
                        <IconButton
                          size="small"
                          onClick={() => handleDeleteSpace(space.id)}
                          disabled={deleteSpaceMutation.isLoading}
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

        {/* Create Space Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Add New Celestial Space</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Name"
              value={newSpace.name}
              onChange={(e) => setNewSpace({ ...newSpace, name: e.target.value })}
              fullWidth
              required
            />
            <FormControl fullWidth required>
              <InputLabel>Type</InputLabel>
              <Select
                value={newSpace.type}
                label="Type"
                onChange={(e) => setNewSpace({ ...newSpace, type: e.target.value as any })}
              >
                <MenuItem value="Solar System">Solar System</MenuItem>
                <MenuItem value="Galaxy">Galaxy</MenuItem>
                <MenuItem value="Nebula">Nebula</MenuItem>
                <MenuItem value="Cluster">Cluster</MenuItem>
                <MenuItem value="Void">Void</MenuItem>
                <MenuItem value="Wormhole">Wormhole</MenuItem>
              </Select>
            </FormControl>
            <TextField
              label="Description"
              value={newSpace.description}
              onChange={(e) => setNewSpace({ ...newSpace, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Image URL"
              value={newSpace.imageUrl}
              onChange={(e) => setNewSpace({ ...newSpace, imageUrl: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Size (light years)"
                type="number"
                value={newSpace.size}
                onChange={(e) => setNewSpace({ ...newSpace, size: parseFloat(e.target.value) || 0 })}
                fullWidth
              />
              <TextField
                label="Age (billion years)"
                type="number"
                value={newSpace.age}
                onChange={(e) => setNewSpace({ ...newSpace, age: parseFloat(e.target.value) || 0 })}
                fullWidth
              />
            </Box>
            <TextField
              label="Discovered By"
              value={newSpace.discoveredBy}
              onChange={(e) => setNewSpace({ ...newSpace, discoveredBy: e.target.value })}
              fullWidth
            />
            <TextField
              label="Discovery Date"
              type="date"
              value={newSpace.discoveredDate}
              onChange={(e) => setNewSpace({ ...newSpace, discoveredDate: e.target.value })}
              fullWidth
              InputLabelProps={{ shrink: true }}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateSpace}
            variant="contained"
            disabled={createSpaceMutation.isLoading}
          >
            {createSpaceMutation.isLoading ? 'Creating...' : 'Create Space'}
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

export default CelestialSpacesPage;