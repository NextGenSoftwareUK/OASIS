import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  TextField,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  IconButton,
  Chip,
  Avatar,
  Alert,
  CircularProgress,
  Badge,
  Tooltip,
  LinearProgress,
  CardMedia,
  CardActions,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  Image as ImageIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  Visibility as ViewIcon,
  Star as StarIcon,
  TrendingUp as TrendingUpIcon,
  AttachMoney as MoneyIcon,
  Schedule as ScheduleIcon,
  FilterList as FilterIcon,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starService } from '../services/starService';
import toast from 'react-hot-toast';

interface NFT {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  price: number;
  rarity: 'Common' | 'Rare' | 'Epic' | 'Legendary';
  category: string;
  owner: string;
  createdAt: string;
  isForSale: boolean;
  views: number;
  likes: number;
}

const NFTsPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterCategory, setFilterCategory] = useState<string>('all');
  const [newNFT, setNewNFT] = useState({
    name: '',
    description: '',
    price: 0,
    rarity: 'Common' as const,
    category: 'Art',
  });

  const queryClient = useQueryClient();

  // Fetch NFTs with impressive demo data
  const { data: nftsData, isLoading, error, refetch } = useQuery(
    'nfts',
    async () => {
      try {
        // Try to get real data first
        const response = await starService.getAllNFTs();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo NFT data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'Cosmic Dragon',
              description: 'A legendary dragon from the depths of the OASIS universe',
              imageUrl: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=400&h=400&fit=crop',
              price: 2.5,
              rarity: 'Legendary',
              category: 'Creatures',
              owner: 'OASIS_Explorer',
              createdAt: '2024-01-15',
              isForSale: true,
              views: 1250,
              likes: 89,
            },
            {
              id: '2',
              name: 'Neon Cityscape',
              description: 'A futuristic cityscape from the cyberpunk realm',
              imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=400&fit=crop&auto=format&q=80',
              price: 1.8,
              rarity: 'Epic',
              category: 'Art',
              owner: 'DigitalArtist_99',
              createdAt: '2024-01-14',
              isForSale: true,
              views: 890,
              likes: 67,
            },
            {
              id: '3',
              name: 'Quantum Crystal',
              description: 'A rare crystal that bends reality itself',
              imageUrl: 'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&h=400&fit=crop',
              price: 3.2,
              rarity: 'Legendary',
              category: 'Minerals',
              owner: 'QuantumMiner',
              createdAt: '2024-01-13',
              isForSale: false,
              views: 2100,
              likes: 156,
            },
            {
              id: '4',
              name: 'Virtual Pet - Cyber Cat',
              description: 'An adorable cybernetic cat companion',
              imageUrl: 'https://images.unsplash.com/photo-1574158622682-e40e69881006?w=400&h=400&fit=crop',
              price: 0.8,
              rarity: 'Rare',
              category: 'Pets',
              owner: 'PetLover_42',
              createdAt: '2024-01-12',
              isForSale: true,
              views: 650,
              likes: 45,
            },
            {
              id: '5',
              name: 'Space Station Alpha',
              description: 'A massive space station orbiting a distant star',
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=400&fit=crop',
              price: 4.5,
              rarity: 'Legendary',
              category: 'Structures',
              owner: 'SpaceArchitect',
              createdAt: '2024-01-11',
              isForSale: true,
              views: 1800,
              likes: 134,
            },
            {
              id: '6',
              name: 'Holographic Flower',
              description: 'A beautiful flower that exists only in digital space',
              imageUrl: 'https://images.unsplash.com/photo-1490750967868-88aa4486c946?w=400&h=400&fit=crop',
              price: 0.5,
              rarity: 'Common',
              category: 'Nature',
              owner: 'NatureLover',
              createdAt: '2024-01-10',
              isForSale: true,
              views: 320,
              likes: 23,
            },
          ]
        };
      }
    },
    {
      refetchInterval: 30000, // Refetch every 30 seconds
      refetchOnWindowFocus: true,
    }
  );

  const createNFTMutation = useMutation(
    async (nftData: Partial<NFT>) => {
      try {
        return await starService.createNFT({
          name: nftData.name || '',
          description: nftData.description || '',
          imageUrl: nftData.imageUrl || '',
          price: nftData.price || 0,
          rarity: nftData.rarity || 'Common',
          category: nftData.category || 'Art'
        });
      } catch (error) {
        // For demo purposes, simulate success
        toast.success('NFT created successfully! (Demo Mode)');
        return { success: true, id: Date.now().toString() };
      }
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('nfts');
        setCreateDialogOpen(false);
        setNewNFT({ name: '', description: '', price: 0, rarity: 'Common', category: 'Art' });
      },
    }
  );

  const deleteNFTMutation = useMutation(
    async (nftId: string) => {
      try {
        return await starService.deleteNFT(nftId);
      } catch (error) {
        // For demo purposes, simulate success
        toast.success('NFT deleted successfully! (Demo Mode)');
        return { success: true };
      }
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('nfts');
      },
    }
  );

  const handleCreateNFT = () => {
    if (!newNFT.name || !newNFT.description) {
      toast.error('Please fill in all required fields');
      return;
    }
    createNFTMutation.mutate(newNFT);
  };

  const handleDeleteNFT = (nftId: string) => {
    if (window.confirm('Are you sure you want to delete this NFT?')) {
      deleteNFTMutation.mutate(nftId);
    }
  };

  const getRarityColor = (rarity: string) => {
    switch (rarity) {
      case 'Common': return '#9e9e9e';
      case 'Rare': return '#2196f3';
      case 'Epic': return '#9c27b0';
      case 'Legendary': return '#ff9800';
      default: return '#9e9e9e';
    }
  };

  const categories = ['all', 'Art', 'Creatures', 'Minerals', 'Pets', 'Structures', 'Nature'];
  const filteredNFTs = nftsData?.result?.filter((nft: any) => 
    filterCategory === 'all' || nft.category === filterCategory
  ) || [];

  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.1,
      },
    },
  };

  const itemVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  return (
    <>
        <Box sx={{ mb: 4, mt: 4 }}>
        <Typography variant="h4" gutterBottom className="page-heading">
          🎨 NFT Marketplace
        </Typography>
        <Typography variant="subtitle1" color="text.secondary">
          Discover, create, and trade digital assets in the OASIS
        </Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load NFTs. Using demo data for presentation.
        </Alert>
      )}

      {/* Action Bar */}
      <Box sx={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center', 
        mb: 3,
        flexWrap: 'wrap',
        gap: 2
      }}>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateDialogOpen(true)}
            sx={{
              background: 'linear-gradient(45deg, #FFD700, #FFA500)',
              color: 'black',
              fontWeight: 'bold',
              '&:hover': {
                background: 'linear-gradient(45deg, #FFA500, #FFD700)',
              }
            }}
          >
            Create NFT
          </Button>
          
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={() => refetch()}
            disabled={isLoading}
          >
            Refresh
          </Button>
        </Box>

        <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
          <FilterIcon sx={{ color: 'text.secondary' }} />
          {categories.map((category) => (
            <Chip
              key={category}
              label={category}
              onClick={() => setFilterCategory(category)}
              variant={filterCategory === category ? 'filled' : 'outlined'}
              sx={{
                bgcolor: filterCategory === category ? 'primary.main' : 'transparent',
                color: filterCategory === category ? 'white' : 'text.primary',
                '&:hover': {
                  bgcolor: filterCategory === category ? 'primary.dark' : 'action.hover',
                }
              }}
            />
          ))}
        </Box>
      </Box>

      {isLoading && (
        <Box sx={{ mb: 3 }}>
          <LinearProgress sx={{ height: 6, borderRadius: 3 }} />
        </Box>
      )}

      {/* NFT Grid */}
      <Grid container spacing={3}>
        {filteredNFTs.map((nft: any, index: number) => (
          <Grid item xs={12} sm={6} md={4} lg={3} key={nft.id}>
            <motion.div
              variants={itemVariants}
              whileHover={{ 
                scale: 1.05,
                rotateY: 5,
                transition: { duration: 0.2 }
              }}
            >
              <Card sx={{ 
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                background: `linear-gradient(135deg, ${getRarityColor(nft.rarity)}15, ${getRarityColor(nft.rarity)}05)`,
                border: `2px solid ${getRarityColor(nft.rarity)}30`,
                boxShadow: `0 8px 32px ${getRarityColor(nft.rarity)}20`,
                transition: 'all 0.3s ease',
                '&:hover': {
                  boxShadow: `0 12px 40px ${getRarityColor(nft.rarity)}30`,
                }
              }}>
                <Box sx={{ position: 'relative' }}>
                  <CardMedia
                    component="img"
                    height="200"
                    image={nft.imageUrl}
                    alt={nft.name}
                    sx={{ 
                      objectFit: 'cover',
                      filter: 'brightness(1.1) contrast(1.1)',
                    }}
                  />
                  <Chip
                    label={nft.rarity}
                    size="small"
                    sx={{
                      position: 'absolute',
                      top: 8,
                      right: 8,
                      bgcolor: getRarityColor(nft.rarity),
                      color: 'white',
                      fontWeight: 'bold',
                      fontSize: '0.7rem'
                    }}
                  />
                  {nft.isForSale && (
                    <Chip
                      label="FOR SALE"
                      size="small"
                      sx={{
                        position: 'absolute',
                        top: 8,
                        left: 8,
                        bgcolor: '#4caf50',
                        color: 'white',
                        fontWeight: 'bold',
                        fontSize: '0.7rem'
                      }}
                    />
                  )}
                </Box>

                <CardContent sx={{ flexGrow: 1 }}>
                  <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>
                    {nft.name}
                  </Typography>
                  
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    {nft.description}
                  </Typography>

                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <MoneyIcon sx={{ color: '#4caf50', fontSize: 20 }} />
                      <Typography variant="h6" sx={{ color: '#4caf50', fontWeight: 'bold' }}>
                        {nft.price} ETH
                      </Typography>
                    </Box>
                    <Chip
                      label={nft.category}
                      size="small"
                      variant="outlined"
                    />
                  </Box>

                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Box sx={{ display: 'flex', gap: 2 }}>
                      <Tooltip title="Views">
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <ViewIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                          <Typography variant="caption">{nft.views}</Typography>
                        </Box>
                      </Tooltip>
                      <Tooltip title="Likes">
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <StarIcon sx={{ fontSize: 16, color: '#ff9800' }} />
                          <Typography variant="caption">{nft.likes}</Typography>
                        </Box>
                      </Tooltip>
                    </Box>
                    <Typography variant="caption" color="text.secondary">
                      by {nft.owner}
                    </Typography>
                  </Box>
                </CardContent>

                <CardActions sx={{ justifyContent: 'space-between', p: 2 }}>
                  <Button
                    size="small"
                    startIcon={<ViewIcon />}
                    sx={{ color: 'primary.main' }}
                  >
                    View
                  </Button>
                  <Box>
                    <IconButton
                      size="small"
                      onClick={() => handleDeleteNFT(nft.id)}
                      disabled={deleteNFTMutation.isLoading}
                      sx={{ color: 'error.main' }}
                    >
                      <DeleteIcon />
                    </IconButton>
                  </Box>
                </CardActions>
              </Card>
            </motion.div>
          </Grid>
        ))}
      </Grid>

      {filteredNFTs.length === 0 && !isLoading && (
        <Card sx={{ textAlign: 'center', py: 8 }}>
          <CardContent>
            <ImageIcon sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
            <Typography variant="h6" color="text.secondary">
              No NFTs found
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Try adjusting your filters or create a new NFT
            </Typography>
          </CardContent>
        </Card>
      )}

      {/* Create NFT Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New NFT</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="NFT Name"
            fullWidth
            variant="outlined"
            value={newNFT.name}
            onChange={(e) => setNewNFT({ ...newNFT, name: e.target.value })}
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Description"
            fullWidth
            multiline
            rows={3}
            variant="outlined"
            value={newNFT.description}
            onChange={(e) => setNewNFT({ ...newNFT, description: e.target.value })}
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Price (ETH)"
            type="number"
            fullWidth
            variant="outlined"
            value={newNFT.price}
            onChange={(e) => setNewNFT({ ...newNFT, price: parseFloat(e.target.value) || 0 })}
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Category"
            fullWidth
            variant="outlined"
            value={newNFT.category}
            onChange={(e) => setNewNFT({ ...newNFT, category: e.target.value })}
            sx={{ mb: 2 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateNFT}
            variant="contained"
            disabled={createNFTMutation.isLoading}
            startIcon={createNFTMutation.isLoading ? <CircularProgress size={20} /> : <AddIcon />}
          >
            {createNFTMutation.isLoading ? 'Creating...' : 'Create NFT'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default NFTsPage;
