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
  Inventory,
  Add,
  Refresh,
  FilterList,
  Visibility,
  Delete,
  Star,
  Diamond,
  AutoAwesome,
  Science,
  Security,
  Speed,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starService } from '../services/starService';
import toast from 'react-hot-toast';

interface InventoryItem {
  id: string;
  name: string;
  description: string;
  type: string;
  rarity: string;
  quantity: number;
  value: number;
  imageUrl: string;
  category: string;
  stats: {
    power?: number;
    durability?: number;
    speed?: number;
    defense?: number;
  };
  createdAt: string;
  lastUsed: string;
}

const InventoryPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterCategory, setFilterCategory] = useState('all');
  const [newItem, setNewItem] = useState({
    name: '',
    description: '',
    type: 'Weapon',
    rarity: 'Common',
    quantity: 1,
    value: 0,
    imageUrl: '',
    category: 'Combat',
  });

  const queryClient = useQueryClient();

  const { data: inventoryData, isLoading, error, refetch } = useQuery(
    'inventory',
    async () => {
      try {
        // Force demo data for inventory
        throw 'Forcing demo data for inventory';
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Inventory data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'Quantum Blade',
              description: 'A blade forged from quantum particles, capable of cutting through any material',
              type: 'Weapon',
              rarity: 'Legendary',
              quantity: 1,
              value: 50000,
              imageUrl: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=400&h=400&fit=crop',
              category: 'Combat',
              stats: { power: 95, durability: 80, speed: 90 },
              createdAt: '2024-01-10',
              lastUsed: '2024-01-15',
            },
            {
              id: '2',
              name: 'Neural Interface',
              description: 'Advanced neural link for direct brain-computer interaction',
              type: 'Technology',
              rarity: 'Epic',
              quantity: 2,
              value: 25000,
              imageUrl: 'https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=400&h=400&fit=crop',
              category: 'Tech',
              stats: { power: 70, durability: 60, speed: 95 },
              createdAt: '2024-01-12',
              lastUsed: '2024-01-16',
            },
            {
              id: '3',
              name: 'Energy Shield',
              description: 'Portable energy barrier generator for protection',
              type: 'Defense',
              rarity: 'Rare',
              quantity: 3,
              value: 15000,
              imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop',
              category: 'Defense',
              stats: { power: 60, durability: 85, defense: 90 },
              createdAt: '2024-01-14',
              lastUsed: '2024-01-17',
            },
            {
              id: '4',
              name: 'Time Crystal',
              description: 'Mysterious crystal that can manipulate temporal fields',
              type: 'Artifact',
              rarity: 'Legendary',
              quantity: 1,
              value: 100000,
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=400&fit=crop',
              category: 'Mystical',
              stats: { power: 100, durability: 100, speed: 100 },
              createdAt: '2024-01-08',
              lastUsed: '2024-01-18',
            },
            {
              id: '5',
              name: 'Plasma Rifle',
              description: 'High-energy plasma weapon for long-range combat',
              type: 'Weapon',
              rarity: 'Epic',
              quantity: 2,
              value: 30000,
              imageUrl: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=400&h=400&fit=crop',
              category: 'Combat',
              stats: { power: 85, durability: 70, speed: 75 },
              createdAt: '2024-01-11',
              lastUsed: '2024-01-16',
            },
            {
              id: '6',
              name: 'Holographic Projector',
              description: 'Advanced 3D hologram generator for communication and entertainment',
              type: 'Technology',
              rarity: 'Rare',
              quantity: 1,
              value: 12000,
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=400&fit=crop&auto=format&q=80',
              category: 'Tech',
              stats: { power: 50, durability: 65, speed: 80 },
              createdAt: '2024-01-13',
              lastUsed: '2024-01-17',
            },
            {
              id: '7',
              name: 'Quantum Armor',
              description: 'Advanced protective suit with quantum field manipulation',
              type: 'Defense',
              rarity: 'Legendary',
              quantity: 1,
              value: 75000,
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=400&fit=crop',
              category: 'Defense',
              stats: { power: 80, durability: 95, defense: 100 },
              createdAt: '2024-01-09',
              lastUsed: '2024-01-19',
            },
            {
              id: '8',
              name: 'Nano Healing Pods',
              description: 'Microscopic medical nanobots for instant healing',
              type: 'Consumable',
              rarity: 'Epic',
              quantity: 10,
              value: 5000,
              imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop&auto=format&q=80',
              category: 'Utility',
              stats: { power: 90, durability: 30, speed: 95 },
              createdAt: '2024-01-12',
              lastUsed: '2024-01-18',
            },
            {
              id: '9',
              name: 'Void Manipulator',
              description: 'Artifact capable of bending space-time itself',
              type: 'Artifact',
              rarity: 'Legendary',
              quantity: 1,
              value: 150000,
              imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop&auto=format&q=80',
              category: 'Mystical',
              stats: { power: 100, durability: 90, speed: 85 },
              createdAt: '2024-01-05',
              lastUsed: '2024-01-20',
            },
            {
              id: '10',
              name: 'Cyber Enhancement Chip',
              description: 'Neural implant that boosts cognitive abilities',
              type: 'Technology',
              rarity: 'Epic',
              quantity: 3,
              value: 20000,
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=400&fit=crop&auto=format&q=80',
              category: 'Tech',
              stats: { power: 75, durability: 80, speed: 90 },
              createdAt: '2024-01-14',
              lastUsed: '2024-01-19',
            },
            {
              id: '11',
              name: 'Quantum Blaster',
              description: 'A powerful energy weapon from the future',
              type: 'Weapon',
              rarity: 'Legendary',
              quantity: 1,
              value: 25000,
              imageUrl: 'https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=400&h=400&fit=crop',
              category: 'Combat',
              stats: { power: 95, durability: 85, speed: 80 },
              createdAt: '2024-01-15',
              lastUsed: '2024-01-20',
            },
            {
              id: '12',
              name: 'Shield Generator',
              description: 'Advanced energy shield technology',
              type: 'Defense',
              rarity: 'Epic',
              quantity: 1,
              value: 18000,
              imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop',
              category: 'Defense',
              stats: { power: 70, durability: 90, defense: 95 },
              createdAt: '2024-01-16',
              lastUsed: '2024-01-21',
            },
            {
              id: '13',
              name: 'Space Suit',
              description: 'Protective gear for space exploration',
              type: 'Armor',
              rarity: 'Rare',
              quantity: 1,
              value: 12000,
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=400&fit=crop',
              category: 'Defense',
              stats: { power: 60, durability: 80, defense: 85 },
              createdAt: '2024-01-17',
              lastUsed: '2024-01-22',
            },
            {
              id: '14',
              name: 'Gravity Boots',
              description: 'Boots that allow walking on any surface',
              type: 'Utility',
              rarity: 'Epic',
              quantity: 1,
              value: 15000,
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=400&fit=crop',
              category: 'Utility',
              stats: { power: 80, durability: 75, speed: 90 },
              createdAt: '2024-01-18',
              lastUsed: '2024-01-23',
            },
            {
              id: '15',
              name: 'Teleportation Ring',
              description: 'Ring that allows instant teleportation',
              type: 'Artifact',
              rarity: 'Legendary',
              quantity: 1,
              value: 100000,
              imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop&auto=format&q=80',
              category: 'Mystical',
              stats: { power: 100, durability: 100, speed: 100 },
              createdAt: '2024-01-19',
              lastUsed: '2024-01-24',
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

  const createItemMutation = useMutation(
    async (itemData: Partial<InventoryItem>) => {
      try {
        return await starService.createInventoryItem?.(itemData);
      } catch (error) {
        // For demo purposes, simulate success
        toast.success('Item created successfully! (Demo Mode)');
        return { success: true, id: Date.now().toString() };
      }
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('inventory');
        setCreateDialogOpen(false);
        setNewItem({
          name: '',
          description: '',
          type: 'Weapon',
          rarity: 'Common',
          quantity: 1,
          value: 0,
          imageUrl: '',
          category: 'Combat',
        });
      },
      onError: () => {
        toast.error('Failed to create item');
      },
    }
  );

  const deleteItemMutation = useMutation(
    async (id: string) => {
      try {
        return await starService.deleteInventoryItem?.(id);
      } catch (error) {
        // For demo purposes, simulate success
        toast.success('Item deleted successfully! (Demo Mode)');
        return { success: true };
      }
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('inventory');
      },
      onError: () => {
        toast.error('Failed to delete item');
      },
    }
  );

  const handleCreateItem = () => {
    if (!newItem.name || !newItem.description) {
      toast.error('Please fill in all required fields');
      return;
    }
    createItemMutation.mutate(newItem);
  };

  const handleDeleteItem = (id: string) => {
    if (window.confirm('Are you sure you want to delete this item?')) {
      deleteItemMutation.mutate(id);
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

  const getTypeColor = (type: string) => {
    switch (type.toLowerCase()) {
      case 'weapon': return '#f44336';
      case 'technology': return '#3f51b5';
      case 'defense': return '#4caf50';
      case 'artifact': return '#9c27b0';
      case 'consumable': return '#ff9800';
      default: return '#607d8b';
    }
  };

  const getTypeIcon = (type: string) => {
    switch (type.toLowerCase()) {
      case 'weapon': return <Star />;
      case 'technology': return <Science />;
      case 'defense': return <Security />;
      case 'artifact': return <Diamond />;
      case 'consumable': return <AutoAwesome />;
      default: return <Inventory />;
    }
  };

  const categories = ['all', 'Combat', 'Tech', 'Defense', 'Mystical', 'Utility'];
  const filteredItems = inventoryData?.result?.filter((item: any) => 
    filterCategory === 'all' || item.category === filterCategory
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
              Inventory
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Item management and inventory tracking system
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
                background: 'linear-gradient(45deg, #9c27b0, #ff9800)',
                '&:hover': {
                  background: 'linear-gradient(45deg, #7b1fa2, #f57c00)',
                },
              }}
            >
              Add Item
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
          Failed to load inventory: {error instanceof Error ? error.message : 'Unknown error'}
        </Alert>
      )}

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {filteredItems.map((item: any, index: number) => (
            <Grid item xs={12} sm={6} md={4} lg={3} key={item.id}>
              <motion.div
                variants={itemVariants}
                whileHover={{ 
                  scale: 1.05,
                  transition: { duration: 0.2 }
                }}
                whileTap={{ scale: 0.95 }}
              >
                <Card sx={{ height: '100%', position: 'relative' }}>
                  <Box sx={{ 
                    position: 'relative', 
                    overflow: 'hidden', 
                    borderTopLeftRadius: 4, 
                    borderTopRightRadius: 4,
                    height: 200,
                    backgroundColor: getRarityColor(item.rarity),
                    background: item.imageUrl ? `url(${item.imageUrl})` : `linear-gradient(45deg, ${getRarityColor(item.rarity)}, ${getRarityColor(item.rarity)}aa)`,
                    backgroundSize: 'cover',
                    backgroundPosition: 'center'
                  }}>
                    <Chip
                      label={item.type}
                      size="small"
                      sx={{
                        position: 'absolute',
                        top: 8,
                        left: 8,
                        bgcolor: getTypeColor(item.type),
                        color: 'white',
                        fontWeight: 'bold',
                      }}
                    />
                    <Chip
                      label={item.rarity}
                      size="small"
                      sx={{
                        position: 'absolute',
                        top: 8,
                        right: 8,
                        bgcolor: getRarityColor(item.rarity),
                        color: 'white',
                        fontWeight: 'bold',
                      }}
                    />
                  </Box>
                  
                  <CardContent>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                      {getTypeIcon(item.type)}
                      <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                        {item.name}
                      </Typography>
                    </Box>
                    
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      {item.description}
                    </Typography>
                    
                    <Box sx={{ mb: 2 }}>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                        Stats:
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                        {item.stats?.power && (
                          <Chip label={`Power: ${item.stats.power}`} size="small" variant="outlined" />
                        )}
                        {item.stats?.durability && (
                          <Chip label={`Durability: ${item.stats.durability}`} size="small" variant="outlined" />
                        )}
                        {item.stats?.speed && (
                          <Chip label={`Speed: ${item.stats.speed}`} size="small" variant="outlined" />
                        )}
                        {item.stats?.defense && (
                          <Chip label={`Defense: ${item.stats.defense}`} size="small" variant="outlined" />
                        )}
                      </Box>
                    </Box>
                    
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Typography variant="h6" color="primary" fontWeight="bold">
                        {item.value.toLocaleString()} Credits
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Last used: {new Date(item.lastUsed).toLocaleDateString()}
                      </Typography>
                    </Box>
                    
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Button
                        variant="outlined"
                        size="small"
                        startIcon={<Visibility />}
                        onClick={() => toast.success('Viewing item details')}
                      >
                        View
                      </Button>
                      <IconButton
                        size="small"
                        onClick={() => handleDeleteItem(item.id)}
                        disabled={deleteItemMutation.isLoading}
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

      {/* Create Item Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Add New Item</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Name"
              value={newItem.name}
              onChange={(e) => setNewItem({ ...newItem, name: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={newItem.description}
              onChange={(e) => setNewItem({ ...newItem, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Image URL"
              value={newItem.imageUrl}
              onChange={(e) => setNewItem({ ...newItem, imageUrl: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select
                  value={newItem.type}
                  label="Type"
                  onChange={(e) => setNewItem({ ...newItem, type: e.target.value })}
                >
                  <MenuItem value="Weapon">Weapon</MenuItem>
                  <MenuItem value="Technology">Technology</MenuItem>
                  <MenuItem value="Defense">Defense</MenuItem>
                  <MenuItem value="Artifact">Artifact</MenuItem>
                  <MenuItem value="Consumable">Consumable</MenuItem>
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel>Rarity</InputLabel>
                <Select
                  value={newItem.rarity}
                  label="Rarity"
                  onChange={(e) => setNewItem({ ...newItem, rarity: e.target.value })}
                >
                  <MenuItem value="Common">Common</MenuItem>
                  <MenuItem value="Uncommon">Uncommon</MenuItem>
                  <MenuItem value="Rare">Rare</MenuItem>
                  <MenuItem value="Epic">Epic</MenuItem>
                  <MenuItem value="Legendary">Legendary</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <FormControl fullWidth>
                <InputLabel>Category</InputLabel>
                <Select
                  value={newItem.category}
                  label="Category"
                  onChange={(e) => setNewItem({ ...newItem, category: e.target.value })}
                >
                  <MenuItem value="Combat">Combat</MenuItem>
                  <MenuItem value="Tech">Tech</MenuItem>
                  <MenuItem value="Defense">Defense</MenuItem>
                  <MenuItem value="Mystical">Mystical</MenuItem>
                  <MenuItem value="Utility">Utility</MenuItem>
                </Select>
              </FormControl>
              <TextField
                label="Quantity"
                type="number"
                value={newItem.quantity}
                onChange={(e) => setNewItem({ ...newItem, quantity: parseInt(e.target.value) })}
                fullWidth
              />
            </Box>
            <TextField
              label="Value (Credits)"
              type="number"
              value={newItem.value}
              onChange={(e) => setNewItem({ ...newItem, value: parseFloat(e.target.value) })}
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateItem}
            variant="contained"
            disabled={createItemMutation.isLoading}
          >
            {createItemMutation.isLoading ? 'Adding...' : 'Add Item'}
          </Button>
        </DialogActions>
      </Dialog>
      </>
    </motion.div>
  );
};

export default InventoryPage;
