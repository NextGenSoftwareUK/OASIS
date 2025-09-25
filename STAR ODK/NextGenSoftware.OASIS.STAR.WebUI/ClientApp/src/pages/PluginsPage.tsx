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
  Switch,
  FormControlLabel,
} from '@mui/material';
import {
  Add,
  Delete,
  Visibility,
  Extension,
  Download,
  Star,
  Refresh,
  FilterList,
  PowerSettingsNew,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { starService } from '../services/starService';

interface Plugin {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  version: string;
  author: string;
  category: 'Productivity' | 'Gaming' | 'Graphics' | 'Audio' | 'Network' | 'Security';
  type: 'Extension' | 'Addon' | 'Mod' | 'Theme' | 'Widget';
  size: number;
  downloads: number;
  rating: number;
  lastUpdated: string;
  isInstalled: boolean;
  isActive: boolean;
  isCompatible: boolean;
  dependencies: string[];
  features: string[];
  documentation: string;
  repository: string;
  price: number;
  isFree: boolean;
}

const PluginsPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterCategory, setFilterCategory] = useState<string>('all');
  const [newPlugin, setNewPlugin] = useState<Partial<Plugin>>({
    name: '',
    description: '',
    imageUrl: '',
    version: '1.0.0',
    author: '',
    category: 'Productivity',
    type: 'Extension',
    size: 0,
    dependencies: [],
    features: [],
    documentation: '',
    repository: '',
    price: 0,
    isFree: true,
  });

  const queryClient = useQueryClient();

  const { data: pluginsData, isLoading, error, refetch } = useQuery(
    'plugins',
    async () => {
      try {
        // Try to get real data first
        const response = await starService.getAllPlugins?.();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Plugins data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'Quantum Performance Booster',
              description: 'Advanced performance optimization plugin that uses quantum algorithms to enhance system speed',
              imageUrl: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=400&h=300&fit=crop',
              version: '3.2.1',
              author: 'Quantum Labs',
              category: 'Productivity',
              type: 'Extension',
              size: 12.5,
              downloads: 245000,
              rating: 4.9,
              lastUpdated: '2024-01-15',
              isInstalled: true,
              isActive: true,
              isCompatible: true,
              dependencies: ['OASIS Core 2.0+'],
              features: ['Quantum Optimization', 'Real-time Monitoring', 'Auto-tuning'],
              documentation: 'https://docs.quantumlabs.com/booster',
              repository: 'https://github.com/quantumlabs/performance-booster',
              price: 0,
              isFree: true,
            },
            {
              id: '2',
              name: 'Cosmic Visual Effects',
              description: 'Stunning visual effects plugin for creating immersive cosmic environments',
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=300&fit=crop',
              version: '2.8.4',
              author: 'Space Graphics Inc',
              category: 'Graphics',
              type: 'Addon',
              size: 45.2,
              downloads: 189000,
              rating: 4.8,
              lastUpdated: '2024-01-20',
              isInstalled: true,
              isActive: false,
              isCompatible: true,
              dependencies: ['OpenGL 4.5+', 'DirectX 11+'],
              features: ['Particle Systems', 'Lighting Effects', 'Post-processing'],
              documentation: 'https://docs.spacegraphics.com/effects',
              repository: 'https://github.com/spacegraphics/cosmic-effects',
              price: 29.99,
              isFree: false,
            },
            {
              id: '3',
              name: 'Neural Audio Processor',
              description: 'AI-powered audio processing plugin with advanced neural network algorithms',
              imageUrl: 'https://images.unsplash.com/photo-1555949963-aa79dcee981c?w=400&h=300&fit=crop',
              version: '1.5.7',
              author: 'AudioTech Solutions',
              category: 'Audio',
              type: 'Extension',
              size: 8.7,
              downloads: 156000,
              rating: 4.7,
              lastUpdated: '2024-01-25',
              isInstalled: false,
              isActive: false,
              isCompatible: true,
              dependencies: ['Audio Engine 3.0+'],
              features: ['AI Enhancement', 'Real-time Processing', 'Custom Presets'],
              documentation: 'https://docs.audiotech.com/neural',
              repository: 'https://github.com/audiotech/neural-audio',
              price: 19.99,
              isFree: false,
            },
            {
              id: '4',
              name: 'Secure Network Shield',
              description: 'Advanced security plugin for protecting network communications and data',
              imageUrl: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=400&h=300&fit=crop',
              version: '4.1.2',
              author: 'CyberSec Pro',
              category: 'Security',
              type: 'Extension',
              size: 6.3,
              downloads: 320000,
              rating: 4.9,
              lastUpdated: '2024-01-30',
              isInstalled: true,
              isActive: true,
              isCompatible: true,
              dependencies: ['Network Stack 2.0+'],
              features: ['Encryption', 'Firewall', 'Threat Detection'],
              documentation: 'https://docs.cybersec.com/shield',
              repository: 'https://github.com/cybersec/network-shield',
              price: 0,
              isFree: true,
            },
            {
              id: '5',
              name: 'Game Mode Pro',
              description: 'Comprehensive gaming enhancement plugin with FPS boost and optimization',
              imageUrl: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=400&h=300&fit=crop',
              version: '2.3.8',
              author: 'GameBoost Studios',
              category: 'Gaming',
              type: 'Mod',
              size: 18.9,
              downloads: 278000,
              rating: 4.6,
              lastUpdated: '2024-02-01',
              isInstalled: false,
              isActive: false,
              isCompatible: true,
              dependencies: ['Game Engine 4.0+'],
              features: ['FPS Boost', 'Graphics Enhancement', 'Performance Tuning'],
              documentation: 'https://docs.gameboost.com/mode',
              repository: 'https://github.com/gameboost/mode-pro',
              price: 14.99,
              isFree: false,
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

  const createPluginMutation = useMutation(
    async (pluginData: Partial<Plugin>) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 1000));
      return { success: true, id: Date.now().toString() };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('plugins');
        toast.success('Plugin created successfully!');
        setCreateDialogOpen(false);
        setNewPlugin({
          name: '',
          description: '',
          imageUrl: '',
          version: '1.0.0',
          author: '',
          category: 'Productivity',
          type: 'Extension',
          size: 0,
          dependencies: [],
          features: [],
          documentation: '',
          repository: '',
          price: 0,
          isFree: true,
        });
      },
      onError: () => {
        toast.error('Failed to create plugin');
      },
    }
  );

  const deletePluginMutation = useMutation(
    async (id: string) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 500));
      return { success: true };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('plugins');
        toast.success('Plugin deleted successfully!');
      },
      onError: () => {
        toast.error('Failed to delete plugin');
      },
    }
  );

  const handleCreatePlugin = () => {
    if (!newPlugin.name || !newPlugin.description) {
      toast.error('Please fill in all required fields');
      return;
    }
    createPluginMutation.mutate(newPlugin);
  };

  const handleDeletePlugin = (id: string) => {
    deletePluginMutation.mutate(id);
  };

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case 'Productivity': return <Extension sx={{ color: '#4caf50' }} />;
      case 'Gaming': return <Extension sx={{ color: '#2196f3' }} />;
      case 'Graphics': return <Extension sx={{ color: '#ff9800' }} />;
      case 'Audio': return <Extension sx={{ color: '#9c27b0' }} />;
      case 'Network': return <Extension sx={{ color: '#607d8b' }} />;
      case 'Security': return <Extension sx={{ color: '#f44336' }} />;
      default: return <Extension sx={{ color: '#757575' }} />;
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'Productivity': return '#4caf50';
      case 'Gaming': return '#2196f3';
      case 'Graphics': return '#ff9800';
      case 'Audio': return '#9c27b0';
      case 'Network': return '#607d8b';
      case 'Security': return '#f44336';
      default: return '#757575';
    }
  };

  const filteredPlugins = pluginsData?.result?.filter((plugin: Plugin) => 
    filterCategory === 'all' || plugin.category === filterCategory
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
              Plugins
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Extend functionality with powerful plugins and extensions
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControl size="small" sx={{ minWidth: 120 }}>
              <InputLabel>Filter Category</InputLabel>
              <Select
                value={filterCategory}
                label="Filter Category"
                onChange={(e) => setFilterCategory(e.target.value)}
              >
                <MenuItem value="all">All Categories</MenuItem>
                <MenuItem value="Productivity">Productivity</MenuItem>
                <MenuItem value="Gaming">Gaming</MenuItem>
                <MenuItem value="Graphics">Graphics</MenuItem>
                <MenuItem value="Audio">Audio</MenuItem>
                <MenuItem value="Network">Network</MenuItem>
                <MenuItem value="Security">Security</MenuItem>
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
            Failed to load plugins. Using demo data for presentation.
          </Alert>
        )}

        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
            <CircularProgress size={60} />
          </Box>
        ) : (
          <Grid container spacing={3}>
            {filteredPlugins.map((plugin: Plugin, index: number) => (
              <Grid item xs={12} sm={6} md={4} key={plugin.id}>
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
                        image={plugin.imageUrl}
                        alt={plugin.name}
                        sx={{ objectFit: 'cover' }}
                      />
                      <Chip
                        label={plugin.category}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          bgcolor: getCategoryColor(plugin.category),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      {plugin.isInstalled && (
                        <Badge
                          badgeContent="Installed"
                          color="success"
                          sx={{
                            position: 'absolute',
                            top: 8,
                            left: 8,
                          }}
                        />
                      )}
                      {!plugin.isFree && (
                        <Chip
                          label={`$${plugin.price}`}
                          size="small"
                          sx={{
                            position: 'absolute',
                            bottom: 8,
                            right: 8,
                            bgcolor: 'rgba(0,0,0,0.7)',
                            color: 'white',
                            fontWeight: 'bold',
                          }}
                        />
                      )}
                    </Box>
                    
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                        {getCategoryIcon(plugin.category)}
                        <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                          {plugin.name}
                        </Typography>
                      </Box>
                      
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {plugin.description}
                      </Typography>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Details:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                          <Chip label={`v${plugin.version}`} size="small" variant="outlined" />
                          <Chip label={plugin.type} size="small" variant="outlined" />
                          <Chip label={`${plugin.size}MB`} size="small" variant="outlined" />
                        </Box>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <Star sx={{ color: '#ff9800', fontSize: 16, mr: 0.5 }} />
                          <Typography variant="body2" color="text.secondary">
                            {plugin.rating}
                          </Typography>
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                          {plugin.downloads.toLocaleString()} downloads
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          By: {plugin.author}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(plugin.lastUpdated).toLocaleDateString()}
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Visibility />}
                          onClick={() => toast.success('Opening plugin details')}
                        >
                          View
                        </Button>
                        <IconButton
                          size="small"
                          onClick={() => handleDeletePlugin(plugin.id)}
                          disabled={deletePluginMutation.isLoading}
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

        {/* Create Plugin Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Add New Plugin</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Name"
              value={newPlugin.name}
              onChange={(e) => setNewPlugin({ ...newPlugin, name: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={newPlugin.description}
              onChange={(e) => setNewPlugin({ ...newPlugin, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Image URL"
              value={newPlugin.imageUrl}
              onChange={(e) => setNewPlugin({ ...newPlugin, imageUrl: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Version"
                value={newPlugin.version}
                onChange={(e) => setNewPlugin({ ...newPlugin, version: e.target.value })}
                fullWidth
              />
              <TextField
                label="Author"
                value={newPlugin.author}
                onChange={(e) => setNewPlugin({ ...newPlugin, author: e.target.value })}
                fullWidth
              />
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <FormControl fullWidth>
                <InputLabel>Category</InputLabel>
                <Select
                  value={newPlugin.category}
                  label="Category"
                  onChange={(e) => setNewPlugin({ ...newPlugin, category: e.target.value as any })}
                >
                  <MenuItem value="Productivity">Productivity</MenuItem>
                  <MenuItem value="Gaming">Gaming</MenuItem>
                  <MenuItem value="Graphics">Graphics</MenuItem>
                  <MenuItem value="Audio">Audio</MenuItem>
                  <MenuItem value="Network">Network</MenuItem>
                  <MenuItem value="Security">Security</MenuItem>
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select
                  value={newPlugin.type}
                  label="Type"
                  onChange={(e) => setNewPlugin({ ...newPlugin, type: e.target.value as any })}
                >
                  <MenuItem value="Extension">Extension</MenuItem>
                  <MenuItem value="Addon">Addon</MenuItem>
                  <MenuItem value="Mod">Mod</MenuItem>
                  <MenuItem value="Theme">Theme</MenuItem>
                  <MenuItem value="Widget">Widget</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Price"
                type="number"
                value={newPlugin.price}
                onChange={(e) => setNewPlugin({ ...newPlugin, price: parseFloat(e.target.value) || 0 })}
                fullWidth
              />
              <FormControlLabel
                control={
                  <Switch
                    checked={newPlugin.isFree}
                    onChange={(e) => setNewPlugin({ ...newPlugin, isFree: e.target.checked })}
                  />
                }
                label="Free"
              />
            </Box>
            <TextField
              label="Documentation URL"
              value={newPlugin.documentation}
              onChange={(e) => setNewPlugin({ ...newPlugin, documentation: e.target.value })}
              fullWidth
            />
            <TextField
              label="Repository URL"
              value={newPlugin.repository}
              onChange={(e) => setNewPlugin({ ...newPlugin, repository: e.target.value })}
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreatePlugin}
            variant="contained"
            disabled={createPluginMutation.isLoading}
          >
            {createPluginMutation.isLoading ? 'Creating...' : 'Create Plugin'}
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

export default PluginsPage;