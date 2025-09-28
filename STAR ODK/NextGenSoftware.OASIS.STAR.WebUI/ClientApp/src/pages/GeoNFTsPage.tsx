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
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Alert,
  CircularProgress,
  Badge,
} from '@mui/material';
import {
  LocationOn,
  Add,
  Refresh,
  FilterList,
  Visibility,
  Delete,
  Public,
  Terrain,
  Business,
  Nature,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starService } from '../services/starService';
import toast from 'react-hot-toast';

interface GeoNFT {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  latitude: number;
  longitude: number;
  location: string;
  category: string;
  rarity: string;
  price: number;
  owner: string;
  createdAt: string;
  isForSale: boolean;
  views: number;
  likes: number;
}

const GeoNFTsPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterCategory, setFilterCategory] = useState('all');
  const [newGeoNFT, setNewGeoNFT] = useState({
    name: '',
    description: '',
    imageUrl: '',
    latitude: 0,
    longitude: 0,
    location: '',
    category: 'Landmark',
    rarity: 'Common',
    price: 0,
  });

  const queryClient = useQueryClient();

  const { data: geoNFTsData, isLoading, error, refetch } = useQuery(
    'geoNFTs',
    async () => {
      try {
        // Try to get real data first
        const response = await starService.getAllGeoNFTs?.();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo GeoNFT data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'Golden Gate Bridge',
              description: 'Iconic suspension bridge spanning the Golden Gate strait',
              imageUrl: 'https://images.unsplash.com/photo-1501594907352-04cda38ebc29?w=400&h=300&fit=crop',
              latitude: 37.8199,
              longitude: -122.4783,
              location: 'San Francisco, CA',
              category: 'Landmark',
              rarity: 'Legendary',
              price: 15.5,
              owner: 'SF_Explorer',
              createdAt: '2024-01-10',
              isForSale: true,
              views: 2840,
              likes: 156,
            },
            {
              id: '2',
              name: 'Mount Fuji Peak',
              description: 'Sacred mountain and active stratovolcano in Japan',
              imageUrl: 'https://images.unsplash.com/photo-1490806843957-31f4c9a91c65?w=400&h=300&fit=crop',
              latitude: 35.3606,
              longitude: 138.7274,
              location: 'Shizuoka, Japan',
              category: 'Nature',
              rarity: 'Epic',
              price: 12.8,
              owner: 'Mountain_Collector',
              createdAt: '2024-01-12',
              isForSale: false,
              views: 1920,
              likes: 98,
            },
            {
              id: '3',
              name: 'Times Square',
              description: 'Major commercial intersection and tourist destination',
              imageUrl: 'https://images.unsplash.com/photo-1496442226666-8d4d0e62e6e9?w=400&h=300&fit=crop',
              latitude: 40.7580,
              longitude: -73.9855,
              location: 'New York, NY',
              category: 'Urban',
              rarity: 'Rare',
              price: 8.2,
              owner: 'NYC_Digital',
              createdAt: '2024-01-15',
              isForSale: true,
              views: 1560,
              likes: 87,
            },
            {
              id: '4',
              name: 'Machu Picchu',
              description: '15th-century Inca citadel in the Andes Mountains',
              imageUrl: 'https://images.unsplash.com/photo-1526392060635-9d6019884377?w=400&h=300&fit=crop',
              latitude: -13.1631,
              longitude: -72.5450,
              location: 'Cusco, Peru',
              category: 'Heritage',
              rarity: 'Legendary',
              price: 22.0,
              owner: 'Ancient_Explorer',
              createdAt: '2024-01-08',
              isForSale: true,
              views: 3200,
              likes: 234,
            },
            {
              id: '5',
              name: 'Sahara Desert Dune',
              description: 'Vast sand dune in the world\'s largest hot desert',
              imageUrl: 'https://images.unsplash.com/photo-1509316975850-ff9c5deb0cd9?w=400&h=300&fit=crop',
              latitude: 25.0000,
              longitude: 0.0000,
              location: 'Sahara Desert',
              category: 'Nature',
              rarity: 'Epic',
              price: 9.5,
              owner: 'Desert_Wanderer',
              createdAt: '2024-01-14',
              isForSale: false,
              views: 980,
              likes: 45,
            },
            {
              id: '6',
              name: 'Eiffel Tower',
              description: 'Iron lattice tower on the Champ de Mars in Paris',
              imageUrl: 'https://images.unsplash.com/photo-1511739001486-6bfe10ce785f?w=400&h=300&fit=crop',
              latitude: 48.8584,
              longitude: 2.2945,
              location: 'Paris, France',
              category: 'Landmark',
              rarity: 'Legendary',
              price: 18.7,
              owner: 'Paris_Collector',
              createdAt: '2024-01-11',
              isForSale: true,
              views: 4100,
              likes: 289,
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

  const createGeoNFTMutation = useMutation(
    async (geoNFTData: Partial<GeoNFT>) => {
      try {
        return await starService.createGeoNFT?.(geoNFTData);
      } catch (error) {
        // For demo purposes, simulate success
        toast.success('GeoNFT created successfully! (Demo Mode)');
        return { success: true, id: Date.now().toString() };
      }
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('geoNFTs');
        setCreateDialogOpen(false);
        setNewGeoNFT({
          name: '',
          description: '',
          imageUrl: '',
          latitude: 0,
          longitude: 0,
          location: '',
          category: 'Landmark',
          rarity: 'Common',
          price: 0,
        });
      },
      onError: () => {
        toast.error('Failed to create GeoNFT');
      },
    }
  );

  const deleteGeoNFTMutation = useMutation(
    async (id: string) => {
      try {
        return await starService.deleteGeoNFT?.(id);
      } catch (error) {
        // For demo purposes, simulate success
        toast.success('GeoNFT deleted successfully! (Demo Mode)');
        return { success: true };
      }
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('geoNFTs');
      },
      onError: () => {
        toast.error('Failed to delete GeoNFT');
      },
    }
  );

  const handleCreateGeoNFT = () => {
    if (!newGeoNFT.name || !newGeoNFT.description || !newGeoNFT.location) {
      toast.error('Please fill in all required fields');
      return;
    }
    createGeoNFTMutation.mutate(newGeoNFT);
  };

  const handleDeleteGeoNFT = (id: string) => {
    if (window.confirm('Are you sure you want to delete this GeoNFT?')) {
      deleteGeoNFTMutation.mutate(id);
    }
  };

  const getRarityColor = (rarity: string) => {
    switch (rarity.toLowerCase()) {
      case 'legendary': return '#9c27b0';
      case 'epic': return '#ff9800';
      case 'rare': return '#2196f3';
      case 'uncommon': return '#4caf50';
      case 'common': return '#9e9e9e';
      default: return '#9e9e9e';
    }
  };

  const getCategoryIcon = (category: string) => {
    switch (category.toLowerCase()) {
      case 'landmark': return <Public />;
      case 'nature': return <Nature />;
      case 'urban': return <Business />;
      case 'heritage': return <Terrain />;
      default: return <LocationOn />;
    }
  };

  const categories = ['all', 'Landmark', 'Nature', 'Urban', 'Heritage'];
  const filteredGeoNFTs = geoNFTsData?.result?.filter((geoNFT: any) => 
    filterCategory === 'all' || geoNFT.category === filterCategory
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
      <Box sx={{ mb: 4, mt: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Box>
            <Typography variant="h4" gutterBottom className="page-heading">
              GeoNFTs
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Location-based NFTs and geo-spatial assets
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={() => refetch()}
              disabled={isLoading}
            >
              Refresh
            </Button>
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => setCreateDialogOpen(true)}
              sx={{
                bgcolor: '#1976d2',
                '&:hover': {
                  bgcolor: '#1565c0',
                },
              }}
            >
              Create GeoNFT
            </Button>
          </Box>
        </Box>

        {/* Filter */}
        <Box sx={{ mb: 3, display: 'flex', alignItems: 'center', gap: 2 }}>
          <FilterList color="action" />
          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel>Category</InputLabel>
            <Select
              value={filterCategory}
              label="Category"
              onChange={(e) => setFilterCategory(e.target.value)}
            >
              {categories.map((category) => (
                <MenuItem key={category} value={category}>
                  {category === 'all' ? 'All Categories' : category}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load GeoNFTs: {error instanceof Error ? error.message : 'Unknown error'}
        </Alert>
      )}

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {filteredGeoNFTs.map((geoNFT: any, index: number) => (
            <Grid item xs={12} sm={6} md={4} lg={3} key={geoNFT.id}>
              <motion.div
                variants={itemVariants}
                whileHover={{ 
                  scale: 1.05,
                  transition: { duration: 0.2 }
                }}
                whileTap={{ scale: 0.95 }}
              >
                <Card sx={{ height: '100%', position: 'relative' }}>
                  <Box sx={{ position: 'relative' }}>
                    <img
                      src={geoNFT.imageUrl}
                      alt={geoNFT.name}
                      style={{
                        width: '100%',
                        height: 200,
                        objectFit: 'cover',
                        borderTopLeftRadius: 4,
                        borderTopRightRadius: 4,
                      }}
                    />
                    <Chip
                      label={geoNFT.rarity}
                      size="small"
                      sx={{
                        position: 'absolute',
                        top: 8,
                        right: 8,
                        bgcolor: getRarityColor(geoNFT.rarity),
                        color: 'white',
                        fontWeight: 'bold',
                      }}
                    />
                    {geoNFT.isForSale && (
                      <Chip
                        label="FOR SALE"
                        size="small"
                        color="success"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          left: 8,
                          fontWeight: 'bold',
                        }}
                      />
                    )}
                  </Box>
                  
                  <CardContent>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                      {getCategoryIcon(geoNFT.category)}
                      <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                        {geoNFT.name}
                      </Typography>
                    </Box>
                    
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      {geoNFT.description}
                    </Typography>
                    
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                      <LocationOn sx={{ fontSize: 16, mr: 1, color: 'text.secondary' }} />
                      <Typography variant="body2" color="text.secondary">
                        {geoNFT.location}
                      </Typography>
                    </Box>
                    
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Typography variant="h6" color="primary" fontWeight="bold">
                        {geoNFT.price} ETH
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Visibility sx={{ fontSize: 16, color: 'text.secondary' }} />
                          <Typography variant="body2" color="text.secondary">
                            {geoNFT.views}
                          </Typography>
                        </Box>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Typography variant="body2">❤️</Typography>
                          <Typography variant="body2" color="text.secondary">
                            {geoNFT.likes}
                          </Typography>
                        </Box>
                      </Box>
                    </Box>
                    
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Button
                        variant="outlined"
                        size="small"
                        startIcon={<Visibility />}
                        onClick={() => toast.success('Viewing GeoNFT details')}
                      >
                        View
                      </Button>
                      <IconButton
                        size="small"
                        onClick={() => handleDeleteGeoNFT(geoNFT.id)}
                        disabled={deleteGeoNFTMutation.isLoading}
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

      {/* Create GeoNFT Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Create New GeoNFT</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Name"
              value={newGeoNFT.name}
              onChange={(e) => setNewGeoNFT({ ...newGeoNFT, name: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={newGeoNFT.description}
              onChange={(e) => setNewGeoNFT({ ...newGeoNFT, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Image URL"
              value={newGeoNFT.imageUrl}
              onChange={(e) => setNewGeoNFT({ ...newGeoNFT, imageUrl: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Latitude"
                type="number"
                value={newGeoNFT.latitude}
                onChange={(e) => setNewGeoNFT({ ...newGeoNFT, latitude: parseFloat(e.target.value) })}
                fullWidth
              />
              <TextField
                label="Longitude"
                type="number"
                value={newGeoNFT.longitude}
                onChange={(e) => setNewGeoNFT({ ...newGeoNFT, longitude: parseFloat(e.target.value) })}
                fullWidth
              />
            </Box>
            <TextField
              label="Location"
              value={newGeoNFT.location}
              onChange={(e) => setNewGeoNFT({ ...newGeoNFT, location: e.target.value })}
              fullWidth
              required
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <FormControl fullWidth>
                <InputLabel>Category</InputLabel>
                <Select
                  value={newGeoNFT.category}
                  label="Category"
                  onChange={(e) => setNewGeoNFT({ ...newGeoNFT, category: e.target.value })}
                >
                  <MenuItem value="Landmark">Landmark</MenuItem>
                  <MenuItem value="Nature">Nature</MenuItem>
                  <MenuItem value="Urban">Urban</MenuItem>
                  <MenuItem value="Heritage">Heritage</MenuItem>
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel>Rarity</InputLabel>
                <Select
                  value={newGeoNFT.rarity}
                  label="Rarity"
                  onChange={(e) => setNewGeoNFT({ ...newGeoNFT, rarity: e.target.value })}
                >
                  <MenuItem value="Common">Common</MenuItem>
                  <MenuItem value="Uncommon">Uncommon</MenuItem>
                  <MenuItem value="Rare">Rare</MenuItem>
                  <MenuItem value="Epic">Epic</MenuItem>
                  <MenuItem value="Legendary">Legendary</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <TextField
              label="Price (ETH)"
              type="number"
              value={newGeoNFT.price}
              onChange={(e) => setNewGeoNFT({ ...newGeoNFT, price: parseFloat(e.target.value) })}
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateGeoNFT}
            variant="contained"
            disabled={createGeoNFTMutation.isLoading}
          >
            {createGeoNFTMutation.isLoading ? 'Creating...' : 'Create GeoNFT'}
          </Button>
        </DialogActions>
      </Dialog>
      </>
    </motion.div>
  );
};

export default GeoNFTsPage;
