import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Chip,
  Button,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Avatar,
  LinearProgress,
  Divider,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Paper,
  Tabs,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Switch,
  FormControlLabel,
} from '@mui/material';
import {
  ArrowBack,
  Extension,
  PowerSettingsNew,
  Settings,
  Timeline,
  Edit,
  Delete,
  Add,
  CheckCircle,
  Cancel,
  Visibility,
  Download,
  Upload,
  Code,
  Security,
  Speed,
  Memory,
  Storage,
  BugReport,
  Update,
  Star,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starService } from '../services/starService';
import { toast } from 'react-hot-toast';

interface Plugin {
  id: string;
  name: string;
  version: string;
  description: string;
  author: string;
  category: string;
  type: 'core' | 'extension' | 'theme' | 'utility';
  status: 'active' | 'inactive' | 'error' | 'updating';
  isInstalled: boolean;
  isEnabled: boolean;
  isPublic: boolean;
  downloadCount: number;
  rating: number;
  size: number;
  lastUpdated: Date;
  dependencies: string[];
  features: string[];
  changelog: Array<{
    version: string;
    date: Date;
    changes: string[];
  }>;
  performance: {
    memoryUsage: number;
    cpuUsage: number;
    loadTime: number;
  };
  permissions: string[];
  documentation: string;
  repository: string;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`plugin-tabpanel-${index}`}
      aria-labelledby={`plugin-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ p: 3 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

const PluginDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [settingsDialogOpen, setSettingsDialogOpen] = useState(false);

  // Fetch plugin detail
  const { data, isLoading, error } = useQuery(
    ['pluginDetail', id],
    async () => {
      // Demo data for now
      const demoData: Plugin = {
        id: id || '1',
        name: 'Quantum Analytics Pro',
        version: '2.1.4',
        description: 'Advanced analytics plugin for quantum computing data visualization and analysis. Provides real-time monitoring, predictive modeling, and comprehensive reporting capabilities.',
        author: 'Quantum Labs',
        category: 'Analytics',
        type: 'extension',
        status: 'active',
        isInstalled: true,
        isEnabled: true,
        isPublic: true,
        downloadCount: 15420,
        rating: 4.8,
        size: 15.2 * 1024 * 1024, // 15.2 MB
        lastUpdated: new Date('2024-01-15'),
        dependencies: ['Quantum Core v1.2+', 'Data Visualization Engine v3.0+'],
        features: [
          'Real-time quantum state monitoring',
          'Advanced data visualization',
          'Predictive analytics',
          'Custom dashboard creation',
          'API integration',
          'Export capabilities'
        ],
        changelog: [
          {
            version: '2.1.4',
            date: new Date('2024-01-15'),
            changes: [
              'Fixed memory leak in data processing',
              'Improved visualization performance',
              'Added new chart types',
              'Enhanced API documentation'
            ]
          },
          {
            version: '2.1.3',
            date: new Date('2024-01-10'),
            changes: [
              'Added support for new quantum algorithms',
              'Improved error handling',
              'Updated UI components'
            ]
          }
        ],
        performance: {
          memoryUsage: 45.2,
          cpuUsage: 12.8,
          loadTime: 1.2
        },
        permissions: [
          'Read quantum data',
          'Write analysis results',
          'Access visualization engine',
          'Export data files'
        ],
        documentation: 'https://docs.quantumlabs.com/analytics-pro',
        repository: 'https://github.com/quantumlabs/analytics-pro'
      };
      return demoData;
    }
  );

  const formatFileSize = (bytes: number) => {
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    if (bytes === 0) return '0 Bytes';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
  };

  const formatDate = (date: Date) => {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active': return 'success';
      case 'inactive': return 'default';
      case 'error': return 'error';
      case 'updating': return 'warning';
      default: return 'default';
    }
  };

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'core': return 'primary';
      case 'extension': return 'secondary';
      case 'theme': return 'warning';
      case 'utility': return 'info';
      default: return 'default';
    }
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleTogglePlugin = () => {
    // Toggle plugin enabled state
    toast.success(`Plugin ${data?.isEnabled ? 'disabled' : 'enabled'} successfully`);
  };

  if (isLoading) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Loading plugin details...</Typography>
      </Box>
    );
  }

  if (error || !data) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography color="error">Error loading plugin details</Typography>
        <Button onClick={() => navigate('/plugins')} startIcon={<ArrowBack />}>
          Back to Plugins
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        {/* Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
          <IconButton onClick={() => navigate('/plugins')} sx={{ mr: 2 }}>
            <ArrowBack />
          </IconButton>
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h4" gutterBottom>
              {data.name}
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              {data.description}
            </Typography>
          </Box>
          <Button
            variant="outlined"
            startIcon={<Settings />}
            onClick={() => setSettingsDialogOpen(true)}
            sx={{ mr: 1 }}
          >
            Settings
          </Button>
          <Button
            variant="contained"
            startIcon={data.isEnabled ? <PowerSettingsNew /> : <PowerSettingsNew />}
            onClick={handleTogglePlugin}
            color={data.isEnabled ? 'error' : 'success'}
          >
            {data.isEnabled ? 'Disable' : 'Enable'}
          </Button>
        </Box>

        {/* Plugin Overview */}
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Extension color="primary" sx={{ mr: 1 }} />
                  <Typography variant="h6">Status</Typography>
                </Box>
                <Chip 
                  label={data.status} 
                  color={getStatusColor(data.status)} 
                  sx={{ mb: 1 }}
                />
                <Typography variant="body2" color="text.secondary">
                  {data.isEnabled ? 'Plugin is active' : 'Plugin is disabled'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Update color="secondary" sx={{ mr: 1 }} />
                  <Typography variant="h6">Version</Typography>
                </Box>
                <Typography variant="h4" color="secondary">
                  {data.version}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Last updated: {formatDate(data.lastUpdated)}
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Download color="success" sx={{ mr: 1 }} />
                  <Typography variant="h6">Downloads</Typography>
                </Box>
                <Typography variant="h4" color="success.main">
                  {data.downloadCount.toLocaleString()}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Total downloads
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Storage color="warning" sx={{ mr: 1 }} />
                  <Typography variant="h6">Size</Typography>
                </Box>
                <Typography variant="h4" color="warning.main">
                  {formatFileSize(data.size)}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Plugin size
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Performance Metrics */}
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              <Speed sx={{ mr: 1, verticalAlign: 'middle' }} />
              Performance Metrics
            </Typography>
            <Grid container spacing={3}>
              <Grid item xs={12} md={4}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Memory color="primary" sx={{ mr: 1 }} />
                  <Typography variant="subtitle1">Memory Usage</Typography>
                </Box>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <Box sx={{ width: '100%', mr: 1 }}>
                    <LinearProgress variant="determinate" value={data.performance.memoryUsage} />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    {data.performance.memoryUsage}%
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={12} md={4}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Speed color="secondary" sx={{ mr: 1 }} />
                  <Typography variant="subtitle1">CPU Usage</Typography>
                </Box>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <Box sx={{ width: '100%', mr: 1 }}>
                    <LinearProgress variant="determinate" value={data.performance.cpuUsage} />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    {data.performance.cpuUsage}%
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={12} md={4}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Timeline color="success" sx={{ mr: 1 }} />
                  <Typography variant="subtitle1">Load Time</Typography>
                </Box>
                <Typography variant="h6" color="success.main">
                  {data.performance.loadTime}s
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Average load time
                </Typography>
              </Grid>
            </Grid>
          </CardContent>
        </Card>

        {/* Tabs */}
        <Card>
          <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
            <Tabs value={tabValue} onChange={handleTabChange}>
              <Tab label="Overview" />
              <Tab label="Features" />
              <Tab label="Dependencies" />
              <Tab label="Changelog" />
              <Tab label="Permissions" />
            </Tabs>
          </Box>

          <TabPanel value={tabValue} index={0}>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Typography variant="h6" gutterBottom>
                  Plugin Information
                </Typography>
                <List>
                  <ListItem>
                    <ListItemText
                      primary="Author"
                      secondary={data.author}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemText
                      primary="Category"
                      secondary={
                        <Chip label={data.category} color={getTypeColor(data.type)} size="small" />
                      }
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemText
                      primary="Type"
                      secondary={
                        <Chip label={data.type} color={getTypeColor(data.type)} size="small" />
                      }
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemText
                      primary="Rating"
                      secondary={
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <Typography variant="body2" sx={{ mr: 1 }}>
                            {data.rating}/5.0
                          </Typography>
                          <Box sx={{ display: 'flex' }}>
                            {[...Array(5)].map((_, i) => (
                              <Star
                                key={i}
                                sx={{
                                  color: i < Math.floor(data.rating) ? 'gold' : 'grey',
                                  fontSize: 16
                                }}
                              />
                            ))}
                          </Box>
                        </Box>
                      }
                    />
                  </ListItem>
                </List>
              </Grid>
              <Grid item xs={12} md={6}>
                <Typography variant="h6" gutterBottom>
                  Links & Resources
                </Typography>
                <List>
                  <ListItem>
                    <ListItemIcon>
                      <Code />
                    </ListItemIcon>
                    <ListItemText
                      primary="Documentation"
                      secondary={
                        <Button
                          variant="text"
                          size="small"
                          onClick={() => window.open(data.documentation, '_blank')}
                        >
                          View Documentation
                        </Button>
                      }
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <Extension />
                    </ListItemIcon>
                    <ListItemText
                      primary="Repository"
                      secondary={
                        <Button
                          variant="text"
                          size="small"
                          onClick={() => window.open(data.repository, '_blank')}
                        >
                          View Source Code
                        </Button>
                      }
                    />
                  </ListItem>
                </List>
              </Grid>
            </Grid>
          </TabPanel>

          <TabPanel value={tabValue} index={1}>
            <Typography variant="h6" gutterBottom>
              Plugin Features
            </Typography>
            <Grid container spacing={2}>
              {data.features.map((feature, index) => (
                <Grid item xs={12} sm={6} md={4} key={index}>
                  <Paper sx={{ p: 2, textAlign: 'center' }}>
                    <CheckCircle color="success" sx={{ fontSize: 40, mb: 1 }} />
                    <Typography variant="subtitle1">{feature}</Typography>
                  </Paper>
                </Grid>
              ))}
            </Grid>
          </TabPanel>

          <TabPanel value={tabValue} index={2}>
            <Typography variant="h6" gutterBottom>
              Dependencies
            </Typography>
            <List>
              {data.dependencies.map((dependency, index) => (
                <ListItem key={index}>
                  <ListItemIcon>
                    <Avatar sx={{ bgcolor: 'primary.main' }}>
                      <Extension />
                    </Avatar>
                  </ListItemIcon>
                  <ListItemText
                    primary={dependency}
                    secondary="Required for plugin functionality"
                  />
                </ListItem>
              ))}
            </List>
          </TabPanel>

          <TabPanel value={tabValue} index={3}>
            <Typography variant="h6" gutterBottom>
              Version History
            </Typography>
            <List>
              {data.changelog.map((entry, index) => (
                <React.Fragment key={entry.version}>
                  <ListItem>
                    <ListItemIcon>
                      <Avatar sx={{ bgcolor: 'secondary.main' }}>
                        <Update />
                      </Avatar>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="subtitle1">
                            Version {entry.version}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {formatDate(entry.date)}
                          </Typography>
                        </Box>
                      }
                      secondary={
                        <Box>
                          {entry.changes.map((change, changeIndex) => (
                            <Typography key={changeIndex} variant="body2" color="text.secondary">
                              • {change}
                            </Typography>
                          ))}
                        </Box>
                      }
                    />
                  </ListItem>
                  {index < data.changelog.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          </TabPanel>

          <TabPanel value={tabValue} index={4}>
            <Typography variant="h6" gutterBottom>
              <Security sx={{ mr: 1, verticalAlign: 'middle' }} />
              Permissions
            </Typography>
            <List>
              {data.permissions.map((permission, index) => (
                <ListItem key={index}>
                  <ListItemIcon>
                    <CheckCircle color="success" />
                  </ListItemIcon>
                  <ListItemText
                    primary={permission}
                    secondary="Granted permission"
                  />
                </ListItem>
              ))}
            </List>
          </TabPanel>
        </Card>
      </motion.div>
    </Box>
  );
};

export default PluginDetailPage;
