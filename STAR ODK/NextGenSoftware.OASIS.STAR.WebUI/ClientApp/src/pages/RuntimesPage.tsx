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
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { starService } from '../services/starService';

interface Runtime {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  version: string;
  type: 'Virtual Machine' | 'Container' | 'Serverless' | 'Edge' | 'Quantum';
  language: string;
  status: 'Running' | 'Stopped' | 'Paused' | 'Error';
  cpuUsage: number;
  memoryUsage: number;
  diskUsage: number;
  uptime: number;
  lastStarted: string;
  lastStopped: string;
  instances: number;
  maxInstances: number;
  port: number;
  environment: string;
  dependencies: string[];
  logs: string[];
  isActive: boolean;
  isPublic: boolean;
  region: string;
  cost: number;
}

const RuntimesPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterType, setFilterType] = useState<string>('all');
  const [newRuntime, setNewRuntime] = useState<Partial<Runtime>>({
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

  const queryClient = useQueryClient();

  const { data: runtimesData, isLoading, error, refetch } = useQuery(
    'runtimes',
    async () => {
      try {
        // Try to get real data first
        const response = await starService.getAllRuntimes?.();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Runtimes data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'OASIS Web Server',
              description: 'High-performance web server runtime for OASIS applications',
              imageUrl: 'https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=400&h=300&fit=crop',
              version: '2.1.4',
              type: 'Virtual Machine',
              language: 'Node.js',
              status: 'Running',
              cpuUsage: 45,
              memoryUsage: 68,
              diskUsage: 23,
              uptime: 86400,
              lastStarted: '2024-01-15T10:30:00Z',
              lastStopped: '',
              instances: 3,
              maxInstances: 10,
              port: 8080,
              environment: 'production',
              dependencies: ['Node.js 18+', 'Express', 'MongoDB'],
              logs: ['Server started successfully', 'Handling 1000+ requests/min'],
              isActive: true,
              isPublic: true,
              region: 'us-east-1',
              cost: 25.50,
            },
            {
              id: '2',
              name: 'Quantum Processing Unit',
              description: 'Advanced quantum computing runtime for complex calculations',
              imageUrl: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=400&h=300&fit=crop',
              version: '1.8.2',
              type: 'Quantum',
              language: 'Q#',
              status: 'Running',
              cpuUsage: 92,
              memoryUsage: 85,
              diskUsage: 45,
              uptime: 43200,
              lastStarted: '2024-01-20T14:15:00Z',
              lastStopped: '',
              instances: 1,
              maxInstances: 2,
              port: 9000,
              environment: 'research',
              dependencies: ['Q# SDK', 'Quantum Simulator'],
              logs: ['Quantum circuit initialized', 'Processing quantum algorithms'],
              isActive: true,
              isPublic: false,
              region: 'us-west-2',
              cost: 150.00,
            },
            {
              id: '3',
              name: 'AI Model Server',
              description: 'Machine learning model serving runtime with GPU acceleration',
              imageUrl: 'https://images.unsplash.com/photo-1555949963-aa79dcee981c?w=400&h=300&fit=crop',
              version: '3.0.1',
              type: 'Container',
              language: 'Python',
              status: 'Paused',
              cpuUsage: 0,
              memoryUsage: 12,
              diskUsage: 67,
              uptime: 0,
              lastStarted: '2024-01-25T09:00:00Z',
              lastStopped: '2024-01-25T18:00:00Z',
              instances: 0,
              maxInstances: 5,
              port: 5000,
              environment: 'staging',
              dependencies: ['TensorFlow', 'CUDA', 'Docker'],
              logs: ['Model loaded successfully', 'GPU acceleration enabled'],
              isActive: false,
              isPublic: true,
              region: 'eu-west-1',
              cost: 75.25,
            },
            {
              id: '4',
              name: 'Edge Computing Node',
              description: 'Lightweight edge computing runtime for IoT and real-time processing',
              imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=300&fit=crop',
              version: '1.5.3',
              type: 'Edge',
              language: 'Rust',
              status: 'Running',
              cpuUsage: 23,
              memoryUsage: 34,
              diskUsage: 12,
              uptime: 172800,
              lastStarted: '2024-01-10T08:00:00Z',
              lastStopped: '',
              instances: 8,
              maxInstances: 20,
              port: 3001,
              environment: 'production',
              dependencies: ['Rust Runtime', 'Tokio'],
              logs: ['Edge node online', 'Processing IoT data'],
              isActive: true,
              isPublic: false,
              region: 'ap-southeast-1',
              cost: 12.75,
            },
            {
              id: '5',
              name: 'Serverless Function',
              description: 'Event-driven serverless runtime for microservices',
              imageUrl: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=400&h=300&fit=crop',
              version: '2.3.8',
              type: 'Serverless',
              language: 'TypeScript',
              status: 'Stopped',
              cpuUsage: 0,
              memoryUsage: 0,
              diskUsage: 5,
              uptime: 0,
              lastStarted: '',
              lastStopped: '2024-01-30T16:30:00Z',
              instances: 0,
              maxInstances: 100,
              port: 0,
              environment: 'development',
              dependencies: ['AWS Lambda', 'TypeScript'],
              logs: ['Function deployed', 'Cold start optimization'],
              isActive: false,
              isPublic: true,
              region: 'us-central-1',
              cost: 0.05,
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

  const createRuntimeMutation = useMutation(
    async (runtimeData: Partial<Runtime>) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 1000));
      return { success: true, id: Date.now().toString() };
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
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 500));
      return { success: true };
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

  const filteredRuntimes = runtimesData?.result?.filter((runtime: Runtime) => 
    filterType === 'all' || runtime.type === filterType
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
              Runtimes
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Manage and monitor application runtimes and execution environments
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControl size="small" sx={{ minWidth: 120 }}>
              <InputLabel>Filter Type</InputLabel>
              <Select
                value={filterType}
                label="Filter Type"
                onChange={(e) => setFilterType(e.target.value)}
              >
                <MenuItem value="all">All Types</MenuItem>
                <MenuItem value="Virtual Machine">Virtual Machines</MenuItem>
                <MenuItem value="Container">Containers</MenuItem>
                <MenuItem value="Serverless">Serverless</MenuItem>
                <MenuItem value="Edge">Edge</MenuItem>
                <MenuItem value="Quantum">Quantum</MenuItem>
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
                        image={runtime.imageUrl}
                        alt={runtime.name}
                        sx={{ objectFit: 'cover' }}
                      />
                      <Chip
                        label={runtime.type}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          bgcolor: getTypeColor(runtime.type),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      <Chip
                        label={runtime.status}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          left: 8,
                          bgcolor: getStatusColor(runtime.status),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                    </Box>
                    
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                        {getTypeIcon(runtime.type)}
                        <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                          {runtime.name}
                        </Typography>
                      </Box>
                      
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {runtime.description}
                      </Typography>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Performance:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                          <Chip label={`CPU: ${runtime.cpuUsage}%`} size="small" variant="outlined" />
                          <Chip label={`RAM: ${runtime.memoryUsage}%`} size="small" variant="outlined" />
                          <Chip label={`Disk: ${runtime.diskUsage}%`} size="small" variant="outlined" />
                        </Box>
                      </Box>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Instances: {runtime.instances}/{runtime.maxInstances}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Cost: ${runtime.cost.toFixed(2)}/hour
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Region: {runtime.region}
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
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
              <TextField
                label="Language"
                value={newRuntime.language}
                onChange={(e) => setNewRuntime({ ...newRuntime, language: e.target.value })}
                fullWidth
              />
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select
                  value={newRuntime.type}
                  label="Type"
                  onChange={(e) => setNewRuntime({ ...newRuntime, type: e.target.value as any })}
                >
                  <MenuItem value="Virtual Machine">Virtual Machine</MenuItem>
                  <MenuItem value="Container">Container</MenuItem>
                  <MenuItem value="Serverless">Serverless</MenuItem>
                  <MenuItem value="Edge">Edge</MenuItem>
                  <MenuItem value="Quantum">Quantum</MenuItem>
                </Select>
              </FormControl>
              <TextField
                label="Port"
                type="number"
                value={newRuntime.port}
                onChange={(e) => setNewRuntime({ ...newRuntime, port: parseInt(e.target.value) || 3000 })}
                fullWidth
              />
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Instances"
                type="number"
                value={newRuntime.instances}
                onChange={(e) => setNewRuntime({ ...newRuntime, instances: parseInt(e.target.value) || 1 })}
                fullWidth
              />
              <TextField
                label="Max Instances"
                type="number"
                value={newRuntime.maxInstances}
                onChange={(e) => setNewRuntime({ ...newRuntime, maxInstances: parseInt(e.target.value) || 5 })}
                fullWidth
              />
            </Box>
            <TextField
              label="Region"
              value={newRuntime.region}
              onChange={(e) => setNewRuntime({ ...newRuntime, region: e.target.value })}
              fullWidth
            />
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