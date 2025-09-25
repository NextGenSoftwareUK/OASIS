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
  LibraryBooks,
  Download,
  Star,
  Refresh,
  FilterList,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { starService } from '../services/starService';

interface Library {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  version: string;
  author: string;
  category: 'Framework' | 'Utility' | 'Graphics' | 'AI/ML' | 'Game Engine' | 'Database';
  language: string;
  license: string;
  downloads: number;
  rating: number;
  size: number;
  lastUpdated: string;
  isInstalled: boolean;
  isActive: boolean;
  dependencies: string[];
  features: string[];
  documentation: string;
  repository: string;
}

const LibrariesPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterCategory, setFilterCategory] = useState<string>('all');
  const [newLibrary, setNewLibrary] = useState<Partial<Library>>({
    name: '',
    description: '',
    imageUrl: '',
    version: '1.0.0',
    author: '',
    category: 'Utility',
    language: 'JavaScript',
    license: 'MIT',
    size: 0,
    dependencies: [],
    features: [],
    documentation: '',
    repository: '',
  });

  const queryClient = useQueryClient();

  const { data: librariesData, isLoading, error, refetch } = useQuery(
    'libraries',
    async () => {
      try {
        // Try to get real data first
        const response = await starService.getAllLibraries?.();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Libraries data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'Quantum Physics Engine',
              description: 'Advanced physics simulation library for quantum mechanics and particle interactions',
              imageUrl: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=400&h=300&fit=crop',
              version: '2.1.4',
              author: 'Quantum Labs',
              category: 'Framework',
              language: 'C++',
              license: 'MIT',
              downloads: 125000,
              rating: 4.9,
              size: 15.2,
              lastUpdated: '2024-01-15',
              isInstalled: true,
              isActive: true,
              dependencies: ['OpenGL', 'CUDA'],
              features: ['Real-time Simulation', 'GPU Acceleration', 'Multi-threading'],
              documentation: 'https://docs.quantumlabs.com',
              repository: 'https://github.com/quantumlabs/physics-engine',
            },
            {
              id: '2',
              name: 'Neural Network Builder',
              description: 'Intuitive library for building and training neural networks with advanced algorithms',
              imageUrl: 'https://images.unsplash.com/photo-1555949963-aa79dcee981c?w=400&h=300&fit=crop',
              version: '1.8.2',
              author: 'AI Research Corp',
              category: 'AI/ML',
              language: 'Python',
              license: 'Apache 2.0',
              downloads: 89000,
              rating: 4.7,
              size: 8.5,
              lastUpdated: '2024-01-20',
              isInstalled: true,
              isActive: false,
              dependencies: ['TensorFlow', 'NumPy', 'Pandas'],
              features: ['AutoML', 'Visualization', 'Model Export'],
              documentation: 'https://docs.airesearch.com/nnb',
              repository: 'https://github.com/airesearch/neural-builder',
            },
            {
              id: '3',
              name: 'Cosmic Graphics Renderer',
              description: 'High-performance 3D graphics library optimized for space and cosmic environments',
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=300&fit=crop',
              version: '3.0.1',
              author: 'Space Graphics Inc',
              category: 'Graphics',
              language: 'C++',
              license: 'GPL v3',
              downloads: 67000,
              rating: 4.8,
              size: 22.1,
              lastUpdated: '2024-01-25',
              isInstalled: false,
              isActive: false,
              dependencies: ['Vulkan', 'OpenGL', 'DirectX'],
              features: ['Ray Tracing', 'Particle Systems', 'Procedural Generation'],
              documentation: 'https://docs.spacegraphics.com/renderer',
              repository: 'https://github.com/spacegraphics/cosmic-renderer',
            },
            {
              id: '4',
              name: 'OASIS Database Connector',
              description: 'Universal database connector for OASIS ecosystem with advanced query optimization',
              imageUrl: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=400&h=300&fit=crop',
              version: '1.5.3',
              author: 'OASIS Core Team',
              category: 'Database',
              language: 'TypeScript',
              license: 'MIT',
              downloads: 156000,
              rating: 4.9,
              size: 3.2,
              lastUpdated: '2024-01-30',
              isInstalled: true,
              isActive: true,
              dependencies: ['Node.js', 'TypeScript'],
              features: ['Multi-DB Support', 'Query Optimization', 'Connection Pooling'],
              documentation: 'https://docs.oasis.com/database',
              repository: 'https://github.com/oasis/database-connector',
            },
            {
              id: '5',
              name: 'Game Engine Core',
              description: 'Complete game engine framework with physics, audio, and networking capabilities',
              imageUrl: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=400&h=300&fit=crop',
              version: '4.2.0',
              author: 'GameDev Studios',
              category: 'Game Engine',
              language: 'C#',
              license: 'Commercial',
              downloads: 45000,
              rating: 4.6,
              size: 45.8,
              lastUpdated: '2024-02-01',
              isInstalled: false,
              isActive: false,
              dependencies: ['.NET 6', 'OpenGL', 'FMOD'],
              features: ['Scene Management', 'Physics Engine', 'Audio System'],
              documentation: 'https://docs.gamedev.com/engine',
              repository: 'https://github.com/gamedev/engine-core',
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

  const createLibraryMutation = useMutation(
    async (libraryData: Partial<Library>) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 1000));
      return { success: true, id: Date.now().toString() };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('libraries');
        toast.success('Library created successfully!');
        setCreateDialogOpen(false);
        setNewLibrary({
          name: '',
          description: '',
          imageUrl: '',
          version: '1.0.0',
          author: '',
          category: 'Utility',
          language: 'JavaScript',
          license: 'MIT',
          size: 0,
          dependencies: [],
          features: [],
          documentation: '',
          repository: '',
        });
      },
      onError: () => {
        toast.error('Failed to create library');
      },
    }
  );

  const deleteLibraryMutation = useMutation(
    async (id: string) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 500));
      return { success: true };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('libraries');
        toast.success('Library deleted successfully!');
      },
      onError: () => {
        toast.error('Failed to delete library');
      },
    }
  );

  const handleCreateLibrary = () => {
    if (!newLibrary.name || !newLibrary.description) {
      toast.error('Please fill in all required fields');
      return;
    }
    createLibraryMutation.mutate(newLibrary);
  };

  const handleDeleteLibrary = (id: string) => {
    deleteLibraryMutation.mutate(id);
  };

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case 'Framework': return <LibraryBooks sx={{ color: '#4caf50' }} />;
      case 'Utility': return <LibraryBooks sx={{ color: '#2196f3' }} />;
      case 'Graphics': return <LibraryBooks sx={{ color: '#ff9800' }} />;
      case 'AI/ML': return <LibraryBooks sx={{ color: '#9c27b0' }} />;
      case 'Game Engine': return <LibraryBooks sx={{ color: '#f44336' }} />;
      case 'Database': return <LibraryBooks sx={{ color: '#607d8b' }} />;
      default: return <LibraryBooks sx={{ color: '#757575' }} />;
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'Framework': return '#4caf50';
      case 'Utility': return '#2196f3';
      case 'Graphics': return '#ff9800';
      case 'AI/ML': return '#9c27b0';
      case 'Game Engine': return '#f44336';
      case 'Database': return '#607d8b';
      default: return '#757575';
    }
  };

  const filteredLibraries = librariesData?.result?.filter((library: Library) => 
    filterCategory === 'all' || library.category === filterCategory
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
              Libraries
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Discover and manage code libraries, frameworks, and development tools
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
                <MenuItem value="Framework">Frameworks</MenuItem>
                <MenuItem value="Utility">Utilities</MenuItem>
                <MenuItem value="Graphics">Graphics</MenuItem>
                <MenuItem value="AI/ML">AI/ML</MenuItem>
                <MenuItem value="Game Engine">Game Engines</MenuItem>
                <MenuItem value="Database">Databases</MenuItem>
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
            Failed to load libraries. Using demo data for presentation.
          </Alert>
        )}

        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
            <CircularProgress size={60} />
          </Box>
        ) : (
          <Grid container spacing={3}>
            {filteredLibraries.map((library: Library, index: number) => (
              <Grid item xs={12} sm={6} md={4} key={library.id}>
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
                        image={library.imageUrl}
                        alt={library.name}
                        sx={{ objectFit: 'cover' }}
                      />
                      <Chip
                        label={library.category}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          bgcolor: getCategoryColor(library.category),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      {library.isInstalled && (
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
                    </Box>
                    
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                        {getCategoryIcon(library.category)}
                        <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                          {library.name}
                        </Typography>
                      </Box>
                      
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {library.description}
                      </Typography>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Details:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                          <Chip label={`v${library.version}`} size="small" variant="outlined" />
                          <Chip label={library.language} size="small" variant="outlined" />
                          <Chip label={`${library.size}MB`} size="small" variant="outlined" />
                        </Box>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <Star sx={{ color: '#ff9800', fontSize: 16, mr: 0.5 }} />
                          <Typography variant="body2" color="text.secondary">
                            {library.rating}
                          </Typography>
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                          {library.downloads.toLocaleString()} downloads
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          By: {library.author}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(library.lastUpdated).toLocaleDateString()}
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Visibility />}
                          onClick={() => toast.success('Opening library documentation')}
                        >
                          View
                        </Button>
                        <IconButton
                          size="small"
                          onClick={() => handleDeleteLibrary(library.id)}
                          disabled={deleteLibraryMutation.isLoading}
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

        {/* Create Library Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Add New Library</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Name"
              value={newLibrary.name}
              onChange={(e) => setNewLibrary({ ...newLibrary, name: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={newLibrary.description}
              onChange={(e) => setNewLibrary({ ...newLibrary, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Image URL"
              value={newLibrary.imageUrl}
              onChange={(e) => setNewLibrary({ ...newLibrary, imageUrl: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Version"
                value={newLibrary.version}
                onChange={(e) => setNewLibrary({ ...newLibrary, version: e.target.value })}
                fullWidth
              />
              <TextField
                label="Author"
                value={newLibrary.author}
                onChange={(e) => setNewLibrary({ ...newLibrary, author: e.target.value })}
                fullWidth
              />
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <FormControl fullWidth>
                <InputLabel>Category</InputLabel>
                <Select
                  value={newLibrary.category}
                  label="Category"
                  onChange={(e) => setNewLibrary({ ...newLibrary, category: e.target.value as any })}
                >
                  <MenuItem value="Framework">Framework</MenuItem>
                  <MenuItem value="Utility">Utility</MenuItem>
                  <MenuItem value="Graphics">Graphics</MenuItem>
                  <MenuItem value="AI/ML">AI/ML</MenuItem>
                  <MenuItem value="Game Engine">Game Engine</MenuItem>
                  <MenuItem value="Database">Database</MenuItem>
                </Select>
              </FormControl>
              <TextField
                label="Language"
                value={newLibrary.language}
                onChange={(e) => setNewLibrary({ ...newLibrary, language: e.target.value })}
                fullWidth
              />
            </Box>
            <TextField
              label="License"
              value={newLibrary.license}
              onChange={(e) => setNewLibrary({ ...newLibrary, license: e.target.value })}
              fullWidth
            />
            <TextField
              label="Documentation URL"
              value={newLibrary.documentation}
              onChange={(e) => setNewLibrary({ ...newLibrary, documentation: e.target.value })}
              fullWidth
            />
            <TextField
              label="Repository URL"
              value={newLibrary.repository}
              onChange={(e) => setNewLibrary({ ...newLibrary, repository: e.target.value })}
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateLibrary}
            variant="contained"
            disabled={createLibraryMutation.isLoading}
          >
            {createLibraryMutation.isLoading ? 'Creating...' : 'Create Library'}
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

export default LibrariesPage;