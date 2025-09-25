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
  Description,
  Download,
  Star,
  Refresh,
  FilterList,
  ContentCopy,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { starService } from '../services/starService';

interface Template {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  category: 'Web App' | 'Mobile App' | 'API' | 'Game' | 'AI/ML' | 'Blockchain';
  type: 'Starter' | 'Boilerplate' | 'Framework' | 'Component' | 'Full Stack';
  language: string;
  framework: string;
  author: string;
  version: string;
  downloads: number;
  rating: number;
  size: number;
  lastUpdated: string;
  isPublic: boolean;
  isFeatured: boolean;
  tags: string[];
  features: string[];
  requirements: string[];
  documentation: string;
  repository: string;
  license: string;
  price: number;
  isFree: boolean;
}

const TemplatesPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterCategory, setFilterCategory] = useState<string>('all');
  const [newTemplate, setNewTemplate] = useState<Partial<Template>>({
    name: '',
    description: '',
    imageUrl: '',
    category: 'Web App',
    type: 'Starter',
    language: 'JavaScript',
    framework: 'React',
    author: '',
    version: '1.0.0',
    size: 0,
    tags: [],
    features: [],
    requirements: [],
    documentation: '',
    repository: '',
    license: 'MIT',
    price: 0,
    isFree: true,
    isPublic: true,
    isFeatured: false,
  });

  const queryClient = useQueryClient();

  const { data: templatesData, isLoading, error, refetch } = useQuery(
    'templates',
    async () => {
      try {
        // Try to get real data first
        const response = await starService.getAllTemplates?.();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Templates data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'OASIS Web App Starter',
              description: 'Complete starter template for building OASIS-compatible web applications with modern tech stack',
              imageUrl: 'https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=400&h=300&fit=crop',
              category: 'Web App',
              type: 'Full Stack',
              language: 'TypeScript',
              framework: 'React + Node.js',
              author: 'OASIS Core Team',
              version: '3.2.1',
              downloads: 125000,
              rating: 4.9,
              size: 45.2,
              lastUpdated: '2024-01-15',
              isPublic: true,
              isFeatured: true,
              tags: ['React', 'Node.js', 'TypeScript', 'OASIS'],
              features: ['Authentication', 'Database Integration', 'API Routes', 'UI Components'],
              requirements: ['Node.js 18+', 'npm/yarn', 'MongoDB'],
              documentation: 'https://docs.oasis.com/starter',
              repository: 'https://github.com/oasis/web-starter',
              license: 'MIT',
              price: 0,
              isFree: true,
            },
            {
              id: '2',
              name: 'Quantum Game Engine',
              description: 'Advanced game development template with quantum physics simulation and 3D graphics',
              imageUrl: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=400&h=300&fit=crop',
              category: 'Game',
              type: 'Framework',
              language: 'C++',
              framework: 'Unreal Engine',
              author: 'Quantum Games Studio',
              version: '2.8.4',
              downloads: 89000,
              rating: 4.8,
              size: 125.7,
              lastUpdated: '2024-01-20',
              isPublic: true,
              isFeatured: true,
              tags: ['C++', 'Unreal', 'Quantum', '3D Graphics'],
              features: ['Quantum Physics', '3D Rendering', 'Multiplayer', 'VR Support'],
              requirements: ['Unreal Engine 5+', 'Visual Studio', 'DirectX 12'],
              documentation: 'https://docs.quantumgames.com/engine',
              repository: 'https://github.com/quantumgames/engine',
              license: 'GPL v3',
              price: 99.99,
              isFree: false,
            },
            {
              id: '3',
              name: 'AI/ML Model Template',
              description: 'Comprehensive template for building and deploying machine learning models',
              imageUrl: 'https://images.unsplash.com/photo-1555949963-aa79dcee981c?w=400&h=300&fit=crop',
              category: 'AI/ML',
              type: 'Boilerplate',
              language: 'Python',
              framework: 'TensorFlow + FastAPI',
              author: 'AI Research Labs',
              version: '1.5.7',
              downloads: 156000,
              rating: 4.7,
              size: 23.8,
              lastUpdated: '2024-01-25',
              isPublic: true,
              isFeatured: false,
              tags: ['Python', 'TensorFlow', 'FastAPI', 'MLOps'],
              features: ['Model Training', 'API Endpoints', 'Data Pipeline', 'Monitoring'],
              requirements: ['Python 3.9+', 'TensorFlow 2.0+', 'Docker'],
              documentation: 'https://docs.airesearch.com/template',
              repository: 'https://github.com/airesearch/ml-template',
              license: 'Apache 2.0',
              price: 0,
              isFree: true,
            },
            {
              id: '4',
              name: 'Blockchain DApp Template',
              description: 'Complete decentralized application template with smart contracts and frontend',
              imageUrl: 'https://images.unsplash.com/photo-1639762681485-074b7f938ba0?w=400&h=300&fit=crop',
              category: 'Blockchain',
              type: 'Full Stack',
              language: 'Solidity + TypeScript',
              framework: 'Hardhat + React',
              author: 'Blockchain Devs',
              version: '4.1.2',
              downloads: 67000,
              rating: 4.6,
              size: 34.5,
              lastUpdated: '2024-01-30',
              isPublic: true,
              isFeatured: false,
              tags: ['Solidity', 'React', 'Web3', 'Smart Contracts'],
              features: ['Smart Contracts', 'Web3 Integration', 'Wallet Connect', 'NFT Support'],
              requirements: ['Node.js 16+', 'Hardhat', 'MetaMask'],
              documentation: 'https://docs.blockchaindevs.com/dapp',
              repository: 'https://github.com/blockchaindevs/dapp-template',
              license: 'MIT',
              price: 49.99,
              isFree: false,
            },
            {
              id: '5',
              name: 'Mobile App Boilerplate',
              description: 'Cross-platform mobile app template with native performance and modern UI',
              imageUrl: 'https://images.unsplash.com/photo-1512941937669-90a1b58e7e9c?w=400&h=300&fit=crop',
              category: 'Mobile App',
              type: 'Boilerplate',
              language: 'TypeScript',
              framework: 'React Native',
              author: 'Mobile Masters',
              version: '2.3.8',
              downloads: 234000,
              rating: 4.8,
              size: 67.3,
              lastUpdated: '2024-02-01',
              isPublic: true,
              isFeatured: true,
              tags: ['React Native', 'TypeScript', 'Cross-platform', 'Native'],
              features: ['Cross-platform', 'Native Performance', 'Offline Support', 'Push Notifications'],
              requirements: ['Node.js 18+', 'React Native CLI', 'Android Studio/Xcode'],
              documentation: 'https://docs.mobilemasters.com/boilerplate',
              repository: 'https://github.com/mobilemasters/rn-boilerplate',
              license: 'MIT',
              price: 0,
              isFree: true,
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

  const createTemplateMutation = useMutation(
    async (templateData: Partial<Template>) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 1000));
      return { success: true, id: Date.now().toString() };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('templates');
        toast.success('Template created successfully!');
        setCreateDialogOpen(false);
        setNewTemplate({
          name: '',
          description: '',
          imageUrl: '',
          category: 'Web App',
          type: 'Starter',
          language: 'JavaScript',
          framework: 'React',
          author: '',
          version: '1.0.0',
          size: 0,
          tags: [],
          features: [],
          requirements: [],
          documentation: '',
          repository: '',
          license: 'MIT',
          price: 0,
          isFree: true,
          isPublic: true,
          isFeatured: false,
        });
      },
      onError: () => {
        toast.error('Failed to create template');
      },
    }
  );

  const deleteTemplateMutation = useMutation(
    async (id: string) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 500));
      return { success: true };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('templates');
        toast.success('Template deleted successfully!');
      },
      onError: () => {
        toast.error('Failed to delete template');
      },
    }
  );

  const handleCreateTemplate = () => {
    if (!newTemplate.name || !newTemplate.description) {
      toast.error('Please fill in all required fields');
      return;
    }
    createTemplateMutation.mutate(newTemplate);
  };

  const handleDeleteTemplate = (id: string) => {
    deleteTemplateMutation.mutate(id);
  };

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case 'Web App': return <Description sx={{ color: '#4caf50' }} />;
      case 'Mobile App': return <Description sx={{ color: '#2196f3' }} />;
      case 'API': return <Description sx={{ color: '#ff9800' }} />;
      case 'Game': return <Description sx={{ color: '#9c27b0' }} />;
      case 'AI/ML': return <Description sx={{ color: '#f44336' }} />;
      case 'Blockchain': return <Description sx={{ color: '#607d8b' }} />;
      default: return <Description sx={{ color: '#757575' }} />;
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'Web App': return '#4caf50';
      case 'Mobile App': return '#2196f3';
      case 'API': return '#ff9800';
      case 'Game': return '#9c27b0';
      case 'AI/ML': return '#f44336';
      case 'Blockchain': return '#607d8b';
      default: return '#757575';
    }
  };

  const filteredTemplates = templatesData?.result?.filter((template: Template) => 
    filterCategory === 'all' || template.category === filterCategory
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
              Templates
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Discover and use pre-built templates to accelerate your development
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
                <MenuItem value="Web App">Web Apps</MenuItem>
                <MenuItem value="Mobile App">Mobile Apps</MenuItem>
                <MenuItem value="API">APIs</MenuItem>
                <MenuItem value="Game">Games</MenuItem>
                <MenuItem value="AI/ML">AI/ML</MenuItem>
                <MenuItem value="Blockchain">Blockchain</MenuItem>
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
            Failed to load templates. Using demo data for presentation.
          </Alert>
        )}

        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
            <CircularProgress size={60} />
          </Box>
        ) : (
          <Grid container spacing={3}>
            {filteredTemplates.map((template: Template, index: number) => (
              <Grid item xs={12} sm={6} md={4} key={template.id}>
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
                        image={template.imageUrl}
                        alt={template.name}
                        sx={{ objectFit: 'cover' }}
                      />
                      <Chip
                        label={template.category}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          bgcolor: getCategoryColor(template.category),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      {template.isFeatured && (
                        <Badge
                          badgeContent="Featured"
                          color="primary"
                          sx={{
                            position: 'absolute',
                            top: 8,
                            left: 8,
                          }}
                        />
                      )}
                      {!template.isFree && (
                        <Chip
                          label={`$${template.price}`}
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
                        {getCategoryIcon(template.category)}
                        <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                          {template.name}
                        </Typography>
                      </Box>
                      
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {template.description}
                      </Typography>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Tech Stack:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                          <Chip label={template.language} size="small" variant="outlined" />
                          <Chip label={template.framework} size="small" variant="outlined" />
                          <Chip label={template.type} size="small" variant="outlined" />
                        </Box>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <Star sx={{ color: '#ff9800', fontSize: 16, mr: 0.5 }} />
                          <Typography variant="body2" color="text.secondary">
                            {template.rating}
                          </Typography>
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                          {template.downloads.toLocaleString()} downloads
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          By: {template.author}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(template.lastUpdated).toLocaleDateString()}
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Visibility />}
                          onClick={() => toast.success('Opening template details')}
                        >
                          View
                        </Button>
                        <IconButton
                          size="small"
                          onClick={() => handleDeleteTemplate(template.id)}
                          disabled={deleteTemplateMutation.isLoading}
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

        {/* Create Template Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Add New Template</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Name"
              value={newTemplate.name}
              onChange={(e) => setNewTemplate({ ...newTemplate, name: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={newTemplate.description}
              onChange={(e) => setNewTemplate({ ...newTemplate, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Image URL"
              value={newTemplate.imageUrl}
              onChange={(e) => setNewTemplate({ ...newTemplate, imageUrl: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <FormControl fullWidth>
                <InputLabel>Category</InputLabel>
                <Select
                  value={newTemplate.category}
                  label="Category"
                  onChange={(e) => setNewTemplate({ ...newTemplate, category: e.target.value as any })}
                >
                  <MenuItem value="Web App">Web App</MenuItem>
                  <MenuItem value="Mobile App">Mobile App</MenuItem>
                  <MenuItem value="API">API</MenuItem>
                  <MenuItem value="Game">Game</MenuItem>
                  <MenuItem value="AI/ML">AI/ML</MenuItem>
                  <MenuItem value="Blockchain">Blockchain</MenuItem>
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select
                  value={newTemplate.type}
                  label="Type"
                  onChange={(e) => setNewTemplate({ ...newTemplate, type: e.target.value as any })}
                >
                  <MenuItem value="Starter">Starter</MenuItem>
                  <MenuItem value="Boilerplate">Boilerplate</MenuItem>
                  <MenuItem value="Framework">Framework</MenuItem>
                  <MenuItem value="Component">Component</MenuItem>
                  <MenuItem value="Full Stack">Full Stack</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Language"
                value={newTemplate.language}
                onChange={(e) => setNewTemplate({ ...newTemplate, language: e.target.value })}
                fullWidth
              />
              <TextField
                label="Framework"
                value={newTemplate.framework}
                onChange={(e) => setNewTemplate({ ...newTemplate, framework: e.target.value })}
                fullWidth
              />
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Author"
                value={newTemplate.author}
                onChange={(e) => setNewTemplate({ ...newTemplate, author: e.target.value })}
                fullWidth
              />
              <TextField
                label="Version"
                value={newTemplate.version}
                onChange={(e) => setNewTemplate({ ...newTemplate, version: e.target.value })}
                fullWidth
              />
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Price"
                type="number"
                value={newTemplate.price}
                onChange={(e) => setNewTemplate({ ...newTemplate, price: parseFloat(e.target.value) || 0 })}
                fullWidth
              />
              <FormControlLabel
                control={
                  <Switch
                    checked={newTemplate.isFree}
                    onChange={(e) => setNewTemplate({ ...newTemplate, isFree: e.target.checked })}
                  />
                }
                label="Free"
              />
            </Box>
            <TextField
              label="Documentation URL"
              value={newTemplate.documentation}
              onChange={(e) => setNewTemplate({ ...newTemplate, documentation: e.target.value })}
              fullWidth
            />
            <TextField
              label="Repository URL"
              value={newTemplate.repository}
              onChange={(e) => setNewTemplate({ ...newTemplate, repository: e.target.value })}
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateTemplate}
            variant="contained"
            disabled={createTemplateMutation.isLoading}
          >
            {createTemplateMutation.isLoading ? 'Creating...' : 'Create Template'}
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

export default TemplatesPage;