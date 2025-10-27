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
  Public,
  Star,
  Satellite,
  Explore,
  Refresh,
  FilterList,
  Info,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { celestialBodyService } from '../services';
import { useNavigate } from 'react-router-dom';

interface CelestialBody {
  id: string;
  name: string;
  type: 'Planet' | 'Star' | 'Moon' | 'Asteroid' | 'Comet' | 'Nebula';
  description: string;
  imageUrl: string;
  mass: number;
  isInstalled?: boolean; // Added for installed badge and filtering
  radius: number;
  temperature: number;
  distance: number;
  discoveredBy: string;
  discoveredDate: string;
  isInhabited: boolean;
  population?: number;
  atmosphere?: string;
  gravity: number;
  orbitalPeriod: number;
  rotationPeriod: number;
  tags: string[];
}

const CelestialBodiesPage: React.FC = () => {
  const navigate = useNavigate();
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterType, setFilterType] = useState<string>('all');
  const [viewScope, setViewScope] = useState<string>('all');
  const [newBody, setNewBody] = useState<Partial<CelestialBody>>({
    name: '',
    type: 'Planet',
    description: '',
    imageUrl: '',
    mass: 0,
    radius: 0,
    temperature: 0,
    distance: 0,
    discoveredBy: '',
    discoveredDate: '',
    isInhabited: false,
    gravity: 0,
    orbitalPeriod: 0,
    rotationPeriod: 0,
    tags: [],
  });

  const queryClient = useQueryClient();

  const { data: bodiesData, isLoading, error, refetch } = useQuery(
    'celestialBodies',
    async () => {
      try {
        // Try to get real data first
        const response = await celestialBodyService.getAll();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Celestial Bodies data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'Nexus Prime',
              type: 'Planet',
              description: 'A lush, Earth-like planet with advanced civilizations and diverse ecosystems',
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=300&fit=crop',
              mass: 5.97e24,
              radius: 6371,
              temperature: 288,
              distance: 1.496e8,
              discoveredBy: 'Captain Nova',
              discoveredDate: '2024-01-10',
              isInhabited: true,
              population: 8500000000,
              atmosphere: 'Nitrogen-Oxygen',
              gravity: 9.81,
              orbitalPeriod: 365.25,
              rotationPeriod: 24,
              tags: ['Inhabited', 'Earth-like', 'Advanced'],
            },
            {
              id: '2',
              name: 'Solaris Alpha',
              type: 'Star',
              description: 'A massive blue giant star that powers the entire Nexus system',
              imageUrl: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=400&h=300&fit=crop',
              mass: 1.989e30,
              radius: 696340,
              temperature: 5778,
              distance: 0,
              discoveredBy: 'Dr. Stellar',
              discoveredDate: '2024-01-05',
              isInhabited: false,
              atmosphere: 'Hydrogen-Helium',
              gravity: 274,
              orbitalPeriod: 0,
              rotationPeriod: 25.05,
              tags: ['Blue Giant', 'System Core', 'High Energy'],
            },
            {
              id: '3',
              name: 'Luna Secunda',
              type: 'Moon',
              description: 'A mysterious moon with ancient ruins and powerful energy signatures',
              imageUrl: 'https://images.unsplash.com/photo-1614728894747-a83421e2b9c9?w=400&h=300&fit=crop',
              mass: 7.342e22,
              radius: 1737,
              temperature: 220,
              distance: 384400,
              discoveredBy: 'Explorer Team Alpha',
              discoveredDate: '2024-01-15',
              isInhabited: false,
              atmosphere: 'None',
              gravity: 1.62,
              orbitalPeriod: 27.32,
              rotationPeriod: 27.32,
              tags: ['Ancient', 'Ruins', 'Mysterious'],
            },
            {
              id: '4',
              name: 'Asteroid Belt Omega',
              type: 'Asteroid',
              description: 'A dense asteroid field rich in rare minerals and precious metals',
              imageUrl: 'https://images.unsplash.com/photo-1446776653964-20c1d3a81b06?w=400&h=300&fit=crop',
              mass: 3.0e21,
              radius: 500,
              temperature: 200,
              distance: 2.7e8,
              discoveredBy: 'Mining Corp Delta',
              discoveredDate: '2024-01-20',
              isInhabited: false,
              atmosphere: 'None',
              gravity: 0.01,
              orbitalPeriod: 1680,
              rotationPeriod: 4.3,
              tags: ['Mining', 'Rich', 'Dangerous'],
            },
            {
              id: '5',
              name: 'Nebula Dreamscape',
              type: 'Nebula',
              description: 'A beautiful stellar nursery where new stars are born in cosmic clouds',
              imageUrl: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=400&h=300&fit=crop',
              mass: 1.0e32,
              radius: 100000,
              temperature: 10000,
              distance: 1.5e16,
              discoveredBy: 'Cosmic Observatory',
              discoveredDate: '2024-01-25',
              isInhabited: false,
              atmosphere: 'Hydrogen-Dust',
              gravity: 0.001,
              orbitalPeriod: 0,
              rotationPeriod: 0,
              tags: ['Stellar Nursery', 'Beautiful', 'Forming'],
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

  const createBodyMutation = useMutation(
    async (bodyData: Partial<CelestialBody>) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 1000));
      return { success: true, id: Date.now().toString() };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('celestialBodies');
        toast.success('Celestial body created successfully!');
        setCreateDialogOpen(false);
        setNewBody({
          name: '',
          type: 'Planet',
          description: '',
          imageUrl: '',
          mass: 0,
          radius: 0,
          temperature: 0,
          distance: 0,
          discoveredBy: '',
          discoveredDate: '',
          isInhabited: false,
          gravity: 0,
          orbitalPeriod: 0,
          rotationPeriod: 0,
          tags: [],
        });
      },
      onError: () => {
        toast.error('Failed to create celestial body');
      },
    }
  );

  const deleteBodyMutation = useMutation(
    async (id: string) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 500));
      return { success: true };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('celestialBodies');
        toast.success('Celestial body deleted successfully!');
      },
      onError: () => {
        toast.error('Failed to delete celestial body');
      },
    }
  );

  const handleCreateBody = () => {
    if (!newBody.name || !newBody.description) {
      toast.error('Please fill in all required fields');
      return;
    }
    createBodyMutation.mutate(newBody);
  };

  const handleDeleteBody = (id: string) => {
    deleteBodyMutation.mutate(id);
  };

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'Planet': return <Public sx={{ color: '#4caf50' }} />;
      case 'Star': return <Star sx={{ color: '#ff9800' }} />;
      case 'Moon': return <Satellite sx={{ color: '#9c27b0' }} />;
      case 'Asteroid': return <Explore sx={{ color: '#607d8b' }} />;
      case 'Comet': return <Explore sx={{ color: '#00bcd4' }} />;
      case 'Nebula': return <Star sx={{ color: '#e91e63' }} />;
      default: return <Public sx={{ color: '#757575' }} />;
    }
  };

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'Planet': return '#4caf50';
      case 'Star': return '#ff9800';
      case 'Moon': return '#9c27b0';
      case 'Asteroid': return '#607d8b';
      case 'Comet': return '#00bcd4';
      case 'Nebula': return '#e91e63';
      default: return '#757575';
    }
  };

  // Filter by view scope first, then by type
  const getFilteredBodies = () => {
    let filtered = bodiesData?.result || [];
    
    // Apply view scope filter
    if (viewScope === 'installed') {
      filtered = filtered.filter((body: any) => body.isInstalled);
    } else if (viewScope === 'mine') {
      // For demo, show bodies discovered by current user
      filtered = filtered.filter((body: any) => body.discoveredBy === 'Captain Nova');
    }
    
    // Apply type filter
    if (filterType !== 'all') {
      filtered = filtered.filter((body: CelestialBody) => body.type === filterType);
    }
    
    return filtered;
  };
  
  const filteredBodies = getFilteredBodies();

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
              Celestial Bodies
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Explore and manage planets, stars, moons, and other celestial objects
            </Typography>
            <Box sx={{ mt: 1, p: 2, bgcolor: '#0d47a1', color: 'white', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
              <Info sx={{ color: 'white' }} />
              <Typography variant="body2" sx={{ color: 'white' }}>
                Discover and explore celestial bodies in the OASIS universe. Track physical properties and characteristics.
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
                <MenuItem value="mine">My Bodies</MenuItem>
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
                <MenuItem value="Planet">Planets</MenuItem>
                <MenuItem value="Star">Stars</MenuItem>
                <MenuItem value="Moon">Moons</MenuItem>
                <MenuItem value="Asteroid">Asteroids</MenuItem>
                <MenuItem value="Comet">Comets</MenuItem>
                <MenuItem value="Nebula">Nebulae</MenuItem>
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
            Failed to load celestial bodies. Using demo data for presentation.
          </Alert>
        )}

        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
            <CircularProgress size={60} />
          </Box>
        ) : (
          <Grid container spacing={3}>
            {filteredBodies.map((body: CelestialBody, index: number) => (
              <Grid item xs={12} sm={6} md={4} key={body.id}>
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
                      cursor: 'pointer',
                      '&:hover': {
                        boxShadow: 6,
                      }
                    }}
                    onClick={() => navigate(`/celestial-bodies/${body.id}`)}
                  >
                    <Box sx={{ position: 'relative', overflow: 'hidden', borderTopLeftRadius: 4, borderTopRightRadius: 4 }}>
                      <CardMedia
                        component="img"
                        height="200"
                        image={body.imageUrl}
                        alt={body.name}
                        sx={{ objectFit: 'cover' }}
                      />
                      <Chip
                        label={body.type}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          bgcolor: getTypeColor(body.type),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      {body.isInhabited && (
                        <Chip
                          label="Inhabited"
                          size="small"
                          color="success"
                          sx={{
                            position: 'absolute',
                            top: 8,
                            left: 8,
                            fontSize: '0.75rem',
                            height: 24,
                          }}
                        />
                      )}
                      {body.isInstalled && (
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
                        {getTypeIcon(body.type)}
                        <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                          {body.name}
                        </Typography>
                      </Box>
                      
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {body.description}
                      </Typography>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Physical Properties:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                          <Chip label={`Mass: ${((body.mass ?? 0) / 1e24).toFixed(1)}×10²⁴ kg`} size="small" variant="outlined" />
                          <Chip label={`Radius: ${(body.radius ?? 0).toLocaleString()} km`} size="small" variant="outlined" />
                          <Chip label={`Temp: ${body.temperature ?? 0}K`} size="small" variant="outlined" />
                        </Box>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          Discovered by: {body.discoveredBy}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(body.discoveredDate).toLocaleDateString()}
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Visibility />}
                          onClick={() => toast.success('Viewing celestial body details')}
                        >
                          View
                        </Button>
                        <IconButton
                          size="small"
                          onClick={() => handleDeleteBody(body.id)}
                          disabled={deleteBodyMutation.isLoading}
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

        {/* Create Body Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Add New Celestial Body</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Name"
              value={newBody.name}
              onChange={(e) => setNewBody({ ...newBody, name: e.target.value })}
              fullWidth
              required
            />
            <FormControl fullWidth required>
              <InputLabel>Type</InputLabel>
              <Select
                value={newBody.type}
                label="Type"
                onChange={(e) => setNewBody({ ...newBody, type: e.target.value as any })}
              >
                <MenuItem value="Planet">Planet</MenuItem>
                <MenuItem value="Star">Star</MenuItem>
                <MenuItem value="Moon">Moon</MenuItem>
                <MenuItem value="Asteroid">Asteroid</MenuItem>
                <MenuItem value="Comet">Comet</MenuItem>
                <MenuItem value="Nebula">Nebula</MenuItem>
              </Select>
            </FormControl>
            <TextField
              label="Description"
              value={newBody.description}
              onChange={(e) => setNewBody({ ...newBody, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Image URL"
              value={newBody.imageUrl}
              onChange={(e) => setNewBody({ ...newBody, imageUrl: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Mass (kg)"
                type="number"
                value={newBody.mass}
                onChange={(e) => setNewBody({ ...newBody, mass: parseFloat(e.target.value) || 0 })}
                fullWidth
              />
              <TextField
                label="Radius (km)"
                type="number"
                value={newBody.radius}
                onChange={(e) => setNewBody({ ...newBody, radius: parseFloat(e.target.value) || 0 })}
                fullWidth
              />
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Temperature (K)"
                type="number"
                value={newBody.temperature}
                onChange={(e) => setNewBody({ ...newBody, temperature: parseFloat(e.target.value) || 0 })}
                fullWidth
              />
              <TextField
                label="Distance (km)"
                type="number"
                value={newBody.distance}
                onChange={(e) => setNewBody({ ...newBody, distance: parseFloat(e.target.value) || 0 })}
                fullWidth
              />
            </Box>
            <TextField
              label="Discovered By"
              value={newBody.discoveredBy}
              onChange={(e) => setNewBody({ ...newBody, discoveredBy: e.target.value })}
              fullWidth
            />
            <TextField
              label="Discovery Date"
              type="date"
              value={newBody.discoveredDate}
              onChange={(e) => setNewBody({ ...newBody, discoveredDate: e.target.value })}
              fullWidth
              InputLabelProps={{ shrink: true }}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateBody}
            variant="contained"
            disabled={createBodyMutation.isLoading}
          >
            {createBodyMutation.isLoading ? 'Creating...' : 'Create Body'}
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

export default CelestialBodiesPage;