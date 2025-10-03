import React, { useState } from 'react';
import {
  Container,
  Grid,
  Card,
  CardContent,
  CardMedia,
  Typography,
  Box,
  Button,
  Chip,
  TextField,
  InputAdornment,
  Tab,
  Tabs,
  Rating,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import {
  Search,
  Download,
  Extension,
  Code,
  Settings,
  Cloud,
  Storage,
  Security,
  Speed,
  Close
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery } from 'react-query';
import { starService } from '../services/starService';
import toast from 'react-hot-toast';

interface Plugin {
  id: string;
  name: string;
  description: string;
  category: 'provider' | 'integration' | 'utility' | 'security' | 'performance';
  version: string;
  author: string;
  downloads: number;
  rating: number;
  imageUrl: string;
  tags: string[];
  compatible: string[];
  size: string;
  lastUpdated: string;
  downloadUrl: string;
  documentation: string;
  codeExample: string;
}

const STARPluginsPage: React.FC = () => {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [selectedPlugin, setSelectedPlugin] = useState<Plugin | null>(null);
  const [detailsOpen, setDetailsOpen] = useState(false);

  const { data: pluginsData, isLoading } = useQuery(
    'star-plugins',
    () => starService.getSTARPlugins()
  );

  const plugins = pluginsData?.result || [];

  const categories = [
    { value: 'all', label: 'All Plugins', icon: <Extension /> },
    { value: 'provider', label: 'Storage Providers', icon: <Storage /> },
    { value: 'integration', label: 'Integrations', icon: <Code /> },
    { value: 'utility', label: 'Utilities', icon: <Settings /> },
    { value: 'security', label: 'Security', icon: <Security /> },
    { value: 'performance', label: 'Performance', icon: <Speed /> },
  ];

  const filteredPlugins = plugins.filter((plugin: Plugin) => {
    const matchesSearch = plugin.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      plugin.description.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesCategory = selectedCategory === 'all' || plugin.category === selectedCategory;
    return matchesSearch && matchesCategory;
  });

  const handleDownload = (plugin: Plugin) => {
    toast.success(`Downloading ${plugin.name}...`);
    window.location.href = plugin.downloadUrl;
  };

  const handleViewDetails = (plugin: Plugin) => {
    setSelectedPlugin(plugin);
    setDetailsOpen(true);
  };

  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: { staggerChildren: 0.1 }
    }
  };

  const itemVariants = {
    hidden: { y: 20, opacity: 0 },
    visible: { y: 0, opacity: 1 }
  };

  return (
    <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
      <motion.div
        variants={containerVariants}
        initial="hidden"
        animate="visible"
      >
        {/* Header */}
        <motion.div variants={itemVariants}>
          <Box sx={{ mb: 4 }}>
            <Typography variant="h3" component="h1" gutterBottom sx={{ fontWeight: 'bold' }}>
              STAR Plugins
            </Typography>
            <Typography variant="h6" color="text.secondary" paragraph>
              Extend STAR and STARNET with powerful plugins for storage providers, integrations, and more
            </Typography>
          </Box>
        </motion.div>

        {/* Search and Filters */}
        <motion.div variants={itemVariants}>
          <Box sx={{ mb: 4 }}>
            <TextField
              fullWidth
              placeholder="Search plugins..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Search />
                  </InputAdornment>
                ),
              }}
              sx={{ mb: 3 }}
            />

            <Tabs
              value={selectedCategory}
              onChange={(_, newValue) => setSelectedCategory(newValue)}
              variant="scrollable"
              scrollButtons="auto"
            >
              {categories.map((cat) => (
                <Tab
                  key={cat.value}
                  value={cat.value}
                  label={cat.label}
                  icon={cat.icon}
                  iconPosition="start"
                />
              ))}
            </Tabs>
          </Box>
        </motion.div>

        {/* Plugins Grid */}
        {isLoading ? (
          <Box sx={{ textAlign: 'center', py: 8 }}>
            <Typography>Loading plugins...</Typography>
          </Box>
        ) : (
          <Grid container spacing={3}>
            {filteredPlugins.map((plugin: Plugin, index: number) => (
              <Grid item xs={12} sm={6} md={4} key={plugin.id}>
                <motion.div
                  variants={itemVariants}
                  initial="hidden"
                  animate="visible"
                  transition={{ delay: index * 0.05 }}
                >
                  <Card
                    sx={{
                      height: '100%',
                      display: 'flex',
                      flexDirection: 'column',
                      transition: 'transform 0.2s, box-shadow 0.2s',
                      '&:hover': {
                        transform: 'translateY(-4px)',
                        boxShadow: 6,
                      },
                    }}
                  >
                    <CardMedia
                      component="img"
                      height="160"
                      image={plugin.imageUrl}
                      alt={plugin.name}
                    />
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Typography variant="h6" gutterBottom>
                        {plugin.name}
                      </Typography>
                      <Typography variant="body2" color="text.secondary" paragraph>
                        {plugin.description}
                      </Typography>

                      <Box sx={{ mb: 2 }}>
                        <Rating value={plugin.rating} readOnly size="small" />
                        <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>
                          ({plugin.downloads.toLocaleString()} downloads)
                        </Typography>
                      </Box>

                      <Box sx={{ mb: 2, display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                        {plugin.tags.slice(0, 3).map((tag) => (
                          <Chip key={tag} label={tag} size="small" />
                        ))}
                      </Box>

                      <Box sx={{ display: 'flex', gap: 1, flexDirection: 'column' }}>
                        <Button
                          variant="contained"
                          startIcon={<Download />}
                          onClick={() => handleDownload(plugin)}
                          fullWidth
                        >
                          Download
                        </Button>
                        <Button
                          variant="outlined"
                          onClick={() => handleViewDetails(plugin)}
                          fullWidth
                        >
                          View Details
                        </Button>
                      </Box>
                    </CardContent>
                  </Card>
                </motion.div>
              </Grid>
            ))}
          </Grid>
        )}

        {filteredPlugins.length === 0 && !isLoading && (
          <Box sx={{ textAlign: 'center', py: 8 }}>
            <Typography variant="h6" color="text.secondary">
              No plugins found matching your criteria
            </Typography>
          </Box>
        )}

        {/* Plugin Details Dialog */}
        <Dialog
          open={detailsOpen}
          onClose={() => setDetailsOpen(false)}
          maxWidth="md"
          fullWidth
        >
          {selectedPlugin && (
            <>
              <DialogTitle>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <Typography variant="h5">{selectedPlugin.name}</Typography>
                  <IconButton onClick={() => setDetailsOpen(false)}>
                    <Close />
                  </IconButton>
                </Box>
              </DialogTitle>
              <DialogContent>
                <Box sx={{ mb: 3 }}>
                  <img
                    src={selectedPlugin.imageUrl}
                    alt={selectedPlugin.name}
                    style={{ width: '100%', borderRadius: '8px', marginBottom: '16px' }}
                  />
                  <Typography paragraph>{selectedPlugin.description}</Typography>

                  <Grid container spacing={2} sx={{ mb: 3 }}>
                    <Grid item xs={6}>
                      <Typography variant="subtitle2" color="text.secondary">Version</Typography>
                      <Typography>{selectedPlugin.version}</Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="subtitle2" color="text.secondary">Author</Typography>
                      <Typography>{selectedPlugin.author}</Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="subtitle2" color="text.secondary">Size</Typography>
                      <Typography>{selectedPlugin.size}</Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="subtitle2" color="text.secondary">Category</Typography>
                      <Typography sx={{ textTransform: 'capitalize' }}>{selectedPlugin.category}</Typography>
                    </Grid>
                  </Grid>

                  <Box sx={{ mb: 2 }}>
                    <Typography variant="subtitle2" gutterBottom>Compatible With:</Typography>
                    <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                      {selectedPlugin.compatible.map((comp) => (
                        <Chip key={comp} label={comp} size="small" color="primary" variant="outlined" />
                      ))}
                    </Box>
                  </Box>

                  {selectedPlugin.codeExample && (
                    <Box sx={{ mb: 2 }}>
                      <Typography variant="subtitle2" gutterBottom>Code Example:</Typography>
                      <Box
                        component="pre"
                        sx={{
                          p: 2,
                          bgcolor: 'grey.900',
                          color: 'grey.100',
                          borderRadius: 1,
                          overflow: 'auto',
                          fontSize: '0.875rem',
                        }}
                      >
                        {selectedPlugin.codeExample}
                      </Box>
                    </Box>
                  )}
                </Box>
              </DialogContent>
              <DialogActions>
                <Button onClick={() => setDetailsOpen(false)}>Close</Button>
                <Button
                  variant="contained"
                  startIcon={<Download />}
                  onClick={() => {
                    handleDownload(selectedPlugin);
                    setDetailsOpen(false);
                  }}
                >
                  Download Plugin
                </Button>
              </DialogActions>
            </>
          )}
        </Dialog>
      </motion.div>
    </Container>
  );
};

export default STARPluginsPage;

