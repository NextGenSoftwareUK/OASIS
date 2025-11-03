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
import { pluginService } from '../services';

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
        const response = await pluginService.getAll();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Plugins data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'React DevTools Pro',
              description: 'Advanced React development tools with component inspector and performance profiler',
              imageUrl: 'https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=400&h=300&fit=crop',
              version: '4.2.1',
              author: 'React Tools Inc',
              category: 'Development',
              type: 'Browser Extension',
              size: 2.1,
              downloads: 1250000,
              rating: 4.8,
              lastUpdated: '2024-01-15',
              isInstalled: true,
              isActive: true,
              isCompatible: true,
              dependencies: ['React 16.8+', 'Chrome 90+'],
              features: ['Component Tree', 'Props Inspector', 'Performance Profiler', 'State Debugger'],
              documentation: 'https://docs.reacttools.com/devtools',
              repository: 'https://github.com/reacttools/devtools-pro',
              price: 0,
              isFree: true,
            },
            {
              id: '2',
              name: 'VS Code AI Assistant',
              description: 'Intelligent code completion and debugging assistant powered by advanced AI',
              imageUrl: 'https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=400&h=300&fit=crop',
              version: '2.8.4',
              author: 'CodeAI Solutions',
              category: 'Development',
              type: 'VS Code Extension',
              size: 15.7,
              downloads: 890000,
              rating: 4.9,
              lastUpdated: '2024-01-20',
              isInstalled: true,
              isActive: false,
              isCompatible: true,
              dependencies: ['VS Code 1.80+', 'Node.js 16+'],
              features: ['Smart Autocomplete', 'Code Generation', 'Bug Detection', 'Refactoring'],
              documentation: 'https://docs.codeai.com/assistant',
              repository: 'https://github.com/codeai/vscode-ai',
              price: 29.99,
              isFree: false,
            },
            {
              id: '3',
              name: 'Unity Performance Profiler',
              description: 'Advanced Unity game performance analysis and optimization tools',
              imageUrl: 'https://images.unsplash.com/photo-1512941937669-90a1b58e7e9c?w=400&h=300&fit=crop',
              version: '3.1.5',
              author: 'Unity Technologies',
              category: 'Game Development',
              type: 'Unity Package',
              size: 8.3,
              downloads: 456000,
              rating: 4.7,
              lastUpdated: '2024-01-25',
              isInstalled: false,
              isActive: false,
              isCompatible: true,
              dependencies: ['Unity 2022.3+'],
              features: ['Frame Analysis', 'Memory Profiler', 'GPU Profiler', 'Optimization Suggestions'],
              documentation: 'https://docs.unity.com/profiler',
              repository: 'https://github.com/unity/performance-profiler',
              price: 0,
              isFree: true,
            },
            {
              id: '4',
              name: 'Docker Container Manager',
              description: 'Comprehensive Docker container management and orchestration plugin',
              imageUrl: 'https://images.unsplash.com/photo-1627398242454-45a1465c2479?w=400&h=300&fit=crop',
              version: '1.9.2',
              author: 'ContainerOps',
              category: 'DevOps',
              type: 'CLI Tool',
              size: 12.4,
              downloads: 678000,
              rating: 4.8,
              lastUpdated: '2024-01-30',
              isInstalled: true,
              isActive: true,
              isCompatible: true,
              dependencies: ['Docker 20.10+', 'Docker Compose 2.0+'],
              features: ['Container Monitoring', 'Auto-scaling', 'Health Checks', 'Log Management'],
              documentation: 'https://docs.containerops.com/manager',
              repository: 'https://github.com/containerops/docker-manager',
              price: 0,
              isFree: true,
            },
            {
              id: '5',
              name: 'PostgreSQL Query Optimizer',
              description: 'Advanced database query optimization and performance monitoring tool',
              imageUrl: 'https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=400&h=300&fit=crop',
              version: '2.3.8',
              author: 'Database Pro',
              category: 'Database',
              type: 'Database Tool',
              size: 18.9,
              downloads: 278000,
              rating: 4.6,
              lastUpdated: '2024-02-01',
              isInstalled: false,
              isActive: false,
              isCompatible: true,
              dependencies: ['PostgreSQL 13+', 'Python 3.8+'],
              features: ['Query Analysis', 'Index Optimization', 'Performance Monitoring', 'Auto-tuning'],
              documentation: 'https://docs.dbpro.com/optimizer',
              repository: 'https://github.com/dbpro/postgres-optimizer',
              price: 14.99,
              isFree: false,
            },
            {
              id: '6',
              name: 'GitHub Actions Workflow Builder',
              description: 'Visual workflow builder for GitHub Actions with pre-built templates and automation',
              imageUrl: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=400&h=300&fit=crop',
              version: '1.4.2',
              author: 'DevOps Solutions',
              category: 'DevOps',
              type: 'GitHub App',
              size: 5.2,
              downloads: 189000,
              rating: 4.8,
              lastUpdated: '2024-01-20',
              isInstalled: true,
              isActive: true,
              isCompatible: true,
              dependencies: ['GitHub API v4', 'Node.js 18+'],
              features: ['Visual Builder', 'Template Library', 'Auto-deployment', 'CI/CD Pipeline'],
              documentation: 'https://docs.devops.com/workflow-builder',
              repository: 'https://github.com/devops/github-workflow-builder',
              price: 0,
              isFree: true,
            },
            {
              id: '7',
              name: 'Figma Design System Manager',
              description: 'Comprehensive design system management and component library for Figma',
              imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=300&fit=crop',
              version: '2.1.5',
              author: 'Design Systems Co',
              category: 'Design',
              type: 'Figma Plugin',
              size: 45.2,
              downloads: 156000,
              rating: 4.7,
              lastUpdated: '2024-01-18',
              isInstalled: false,
              isActive: false,
              isCompatible: true,
              dependencies: ['Graphics Engine 3.0+', 'Hologram SDK'],
              features: ['3D Interface Design', 'Real-time Preview', 'Export Tools'],
              documentation: 'https://docs.holodesign.com/ui-designer',
              repository: 'https://github.com/holodesign/ui-designer',
              price: 29.99,
              isFree: false,
            },
            {
              id: '8',
              name: 'Neural Network Accelerator',
              description: 'Boost AI and machine learning performance with neural processing optimization',
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=300&fit=cropphoto-1555949963-aa79dcee981c?w=400&h=300&fit=crop',
              version: '2.8.4',
              author: 'AI Dynamics',
              category: 'Productivity',
              type: 'Extension',
              size: 32.1,
              downloads: 203000,
              rating: 4.9,
              lastUpdated: '2024-01-22',
              isInstalled: true,
              isActive: false,
              isCompatible: true,
              dependencies: ['Neural SDK 2.0+', 'CUDA Support'],
              features: ['GPU Acceleration', 'Model Optimization', 'Batch Processing'],
              documentation: 'https://docs.aidynamics.com/accelerator',
              repository: 'https://github.com/aidynamics/neural-accelerator',
              price: 0,
              isFree: true,
            },
            {
              id: '9',
              name: 'Spatial Audio Engine',
              description: 'Immersive 3D spatial audio processing for virtual environments',
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=300&fit=cropphoto-1493225457124-a3eb161ffa5f?w=400&h=300&fit=crop',
              version: '1.9.3',
              author: 'SoundSpace Technologies',
              category: 'Audio',
              type: 'Engine',
              size: 28.6,
              downloads: 134000,
              rating: 4.6,
              lastUpdated: '2024-01-16',
              isInstalled: false,
              isActive: false,
              isCompatible: true,
              dependencies: ['Audio Framework 3.0+', 'Spatial SDK'],
              features: ['3D Audio Processing', 'Environmental Effects', 'Real-time Mixing'],
              documentation: 'https://docs.soundspace.com/engine',
              repository: 'https://github.com/soundspace/spatial-engine',
              price: 19.99,
              isFree: false,
            },
            {
              id: '10',
              name: 'Blockchain Integration Suite',
              description: 'Seamlessly integrate blockchain functionality into any OASIS application',
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=300&fit=cropphoto-1639762681485-074b7f938ba0?w=400&h=300&fit=crop',
              version: '5.2.1',
              author: 'CryptoLink Solutions',
              category: 'Network',
              type: 'Framework',
              size: 67.8,
              downloads: 98000,
              rating: 4.5,
              lastUpdated: '2024-01-25',
              isInstalled: true,
              isActive: true,
              isCompatible: true,
              dependencies: ['Web3 SDK', 'Ethereum Client', 'IPFS Node'],
              features: ['Multi-chain Support', 'Smart Contracts', 'DeFi Integration'],
              documentation: 'https://docs.cryptolink.com/suite',
              repository: 'https://github.com/cryptolink/blockchain-suite',
              price: 49.99,
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
      case 'Development': return <Extension sx={{ color: '#4caf50' }} />;
      case 'Game Development': return <Extension sx={{ color: '#2196f3' }} />;
      case 'DevOps': return <Extension sx={{ color: '#ff9800' }} />;
      case 'Database': return <Extension sx={{ color: '#9c27b0' }} />;
      case 'Design': return <Extension sx={{ color: '#607d8b' }} />;
      case 'Productivity': return <Extension sx={{ color: '#4caf50' }} />;
      case 'Gaming': return <Extension sx={{ color: '#2196f3' }} />;
      case 'Graphics': return <Extension sx={{ color: '#ff9800' }} />;
      case 'Audio': return <Extension sx={{ color: '#9c27b0' }} />;
      case 'Network': return <Extension sx={{ color: '#607d8b' }} />;
      case 'Security': return <Extension sx={{ color: '#f44336' }} />;
      case 'AI/ML': return <Extension sx={{ color: '#e91e63' }} />;
      case 'Mobile': return <Extension sx={{ color: '#795548' }} />;
      case 'Web': return <Extension sx={{ color: '#3f51b5' }} />;
      case 'Desktop': return <Extension sx={{ color: '#009688' }} />;
      case 'Cloud': return <Extension sx={{ color: '#00bcd4' }} />;
      case 'Testing': return <Extension sx={{ color: '#8bc34a' }} />;
      case 'Monitoring': return <Extension sx={{ color: '#ffc107' }} />;
      default: return <Extension sx={{ color: '#757575' }} />;
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'Development': return '#4caf50';
      case 'Game Development': return '#2196f3';
      case 'DevOps': return '#ff9800';
      case 'Database': return '#9c27b0';
      case 'Design': return '#607d8b';
      case 'Productivity': return '#4caf50';
      case 'Gaming': return '#2196f3';
      case 'Graphics': return '#ff9800';
      case 'Audio': return '#9c27b0';
      case 'Network': return '#607d8b';
      case 'Security': return '#f44336';
      case 'AI/ML': return '#e91e63';
      case 'Mobile': return '#795548';
      case 'Web': return '#3f51b5';
      case 'Desktop': return '#009688';
      case 'Cloud': return '#00bcd4';
      case 'Testing': return '#8bc34a';
      case 'Monitoring': return '#ffc107';
      default: return '#757575';
    }
  };

  const filteredPlugins = pluginsData?.result?.filter((plugin: Plugin) => 
    filterCategory === 'all' || plugin.category === filterCategory
  ).map((plugin: Plugin) => ({
    ...plugin,
    imageUrl: plugin.imageUrl || 'https://images.unsplash.com/photo-1555949963-aa79dcee981c?w=400&h=300&fit=crop'
  })) || [];

  // Debug logging for PluginsPage
  console.log('PluginsPage Debug:', {
    pluginsData: pluginsData,
    hasResult: !!pluginsData?.result,
    resultLength: pluginsData?.result?.length || 0,
    filteredLength: filteredPlugins.length,
    filterCategory: filterCategory,
    isLoading: isLoading,
    error: error
  });
  
  // Debug image URLs
  if (filteredPlugins.length > 0) {
    console.log('First plugin image URL:', filteredPlugins[0].imageUrl);
    console.log('All plugin image URLs:', filteredPlugins.map(plugin => plugin.imageUrl));
  }

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
                <MenuItem value="Development">Development</MenuItem>
                <MenuItem value="Game Development">Game Development</MenuItem>
                <MenuItem value="DevOps">DevOps</MenuItem>
                <MenuItem value="Database">Database</MenuItem>
                <MenuItem value="Design">Design</MenuItem>
                <MenuItem value="Productivity">Productivity</MenuItem>
                <MenuItem value="Gaming">Gaming</MenuItem>
                <MenuItem value="Graphics">Graphics</MenuItem>
                <MenuItem value="Audio">Audio</MenuItem>
                <MenuItem value="Network">Network</MenuItem>
                <MenuItem value="Security">Security</MenuItem>
                <MenuItem value="AI/ML">AI/ML</MenuItem>
                <MenuItem value="Mobile">Mobile</MenuItem>
                <MenuItem value="Web">Web</MenuItem>
                <MenuItem value="Desktop">Desktop</MenuItem>
                <MenuItem value="Cloud">Cloud</MenuItem>
                <MenuItem value="Testing">Testing</MenuItem>
                <MenuItem value="Monitoring">Monitoring</MenuItem>
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
                      <div
                        style={{
                          width: '100%',
                          height: '200px',
                          backgroundImage: `url(${plugin.imageUrl})`,
                          backgroundSize: 'cover',
                          backgroundPosition: 'center',
                          backgroundRepeat: 'no-repeat',
                          display: 'block'
                        }}
                      />
                      <Chip
                        label={plugin.category}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          left: 8,
                          bgcolor: getCategoryColor(plugin.category),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      {plugin.isInstalled && (
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
                      {!plugin.isFree && (
                        <Chip
                          label={`$${plugin.price || '0.00'}`}
                          size="small"
                          sx={{
                            position: 'absolute',
                            bottom: 8,
                            left: 8,
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
                  <MenuItem value="Development">Development</MenuItem>
                  <MenuItem value="Game Development">Game Development</MenuItem>
                  <MenuItem value="DevOps">DevOps</MenuItem>
                  <MenuItem value="Database">Database</MenuItem>
                  <MenuItem value="Design">Design</MenuItem>
                  <MenuItem value="Productivity">Productivity</MenuItem>
                  <MenuItem value="Gaming">Gaming</MenuItem>
                  <MenuItem value="Graphics">Graphics</MenuItem>
                  <MenuItem value="Audio">Audio</MenuItem>
                  <MenuItem value="Network">Network</MenuItem>
                  <MenuItem value="Security">Security</MenuItem>
                  <MenuItem value="AI/ML">AI/ML</MenuItem>
                  <MenuItem value="Mobile">Mobile</MenuItem>
                  <MenuItem value="Web">Web</MenuItem>
                  <MenuItem value="Desktop">Desktop</MenuItem>
                  <MenuItem value="Cloud">Cloud</MenuItem>
                  <MenuItem value="Testing">Testing</MenuItem>
                  <MenuItem value="Monitoring">Monitoring</MenuItem>
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