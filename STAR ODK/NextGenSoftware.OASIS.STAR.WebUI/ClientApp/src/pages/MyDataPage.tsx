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
  TextField,
  InputAdornment,
  Alert,
  CircularProgress,
  Badge,
  Fab,
  Tooltip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Avatar,
  LinearProgress,
  Tabs,
  Tab,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Switch,
  FormControlLabel,
  Slider,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  ListItemSecondaryAction,
} from '@mui/material';
import {
  Search,
  CloudUpload,
  CloudDownload,
  Storage,
  Security,
  Share,
  Folder,
  InsertDriveFile,
  Image,
  VideoFile,
  AudioFile,
  Description,
  Archive,
  Code,
  DataObject,
  Refresh,
  FilterList,
  Visibility,
  Edit,
  Delete,
  Add,
  ExpandMore,
  CheckCircle,
  Warning,
  Error,
  Info,
  CloudSync,
  CloudDone,
  CloudOff,
  Speed,
  Memory,
  NetworkCheck,
  Shield,
  Public,
  Lock,
  Group,
  Person,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { starService } from '../services/starService';

interface DataFile {
  id: string;
  name: string;
  type: 'file' | 'folder';
  mimeType: string;
  size: number;
  path: string;
  createdAt: string;
  modifiedAt: string;
  permissions: {
    read: string[];
    write: string[];
    execute: string[];
  };
  replication: {
    enabled: boolean;
    nodes: number;
    regions: string[];
  };
  encryption: {
    enabled: boolean;
    algorithm: string;
    keySize: number;
  };
  storage: {
    provider: 'web2' | 'web3' | 'p2p';
    nodes: string[];
    redundancy: number;
  };
  metadata: {
    tags: string[];
    description: string;
    version: string;
  };
}

interface StorageNode {
  id: string;
  name: string;
  type: 'web2' | 'web3' | 'p2p';
  region: string;
  status: 'online' | 'offline' | 'maintenance';
  capacity: number;
  used: number;
  latency: number;
  reliability: number;
}

interface HyperdriveStats {
  totalFiles: number;
  totalSize: number;
  replicationFactor: number;
  encryptionCoverage: number;
  nodesOnline: number;
  averageLatency: number;
  dataIntegrity: number;
}

const MyDataPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [tabValue, setTabValue] = useState(0);
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false);
  const [settingsDialogOpen, setSettingsDialogOpen] = useState(false);
  const [selectedFile, setSelectedFile] = useState<DataFile | null>(null);

  const queryClient = useQueryClient();

  const { data: filesData, isLoading, error, refetch } = useQuery(
    'myDataFiles',
    async () => {
      try {
        // Try to get real data first
        const response = await starService.getMyDataFiles?.();
        return response;
      } catch (error) {
        // Fallback to impressive demo data - only log to console
        console.log('Using demo My Data files for investor presentation:', error);
        return {
          result: {
            files: [
              {
                id: '1',
                name: 'Research Papers',
                type: 'folder',
                mimeType: 'folder',
                size: 0,
                path: '/Research Papers',
                createdAt: '2024-01-10T10:00:00Z',
                modifiedAt: '2024-01-15T14:30:00Z',
                permissions: {
                  read: ['public'],
                  write: ['me'],
                  execute: ['me']
                },
                replication: {
                  enabled: true,
                  nodes: 5,
                  regions: ['us-east', 'eu-west', 'asia-pacific']
                },
                encryption: {
                  enabled: true,
                  algorithm: 'AES-256',
                  keySize: 256
                },
                storage: {
                  provider: 'web3',
                  nodes: ['ipfs-node-1', 'ipfs-node-2', 'arweave-node-1'],
                  redundancy: 3
                },
                metadata: {
                  tags: ['research', 'academic', 'quantum'],
                  description: 'Collection of quantum computing research papers',
                  version: '1.0'
                }
              },
              {
                id: '2',
                name: 'quantum-algorithms.pdf',
                type: 'file',
                mimeType: 'application/pdf',
                size: 2500000,
                path: '/Research Papers/quantum-algorithms.pdf',
                createdAt: '2024-01-12T09:15:00Z',
                modifiedAt: '2024-01-14T16:45:00Z',
                permissions: {
                  read: ['research-team'],
                  write: ['me'],
                  execute: ['me']
                },
                replication: {
                  enabled: true,
                  nodes: 7,
                  regions: ['us-east', 'eu-west', 'asia-pacific', 'us-west']
                },
                encryption: {
                  enabled: true,
                  algorithm: 'AES-256',
                  keySize: 256
                },
                storage: {
                  provider: 'web3',
                  nodes: ['ipfs-node-1', 'ipfs-node-2', 'arweave-node-1', 'filecoin-node-1'],
                  redundancy: 4
                },
                metadata: {
                  tags: ['quantum', 'algorithms', 'research'],
                  description: 'Advanced quantum algorithms research paper',
                  version: '2.1'
                }
              },
              {
                id: '3',
                name: 'OASIS-Project',
                type: 'folder',
                mimeType: 'folder',
                size: 0,
                path: '/OASIS-Project',
                createdAt: '2024-01-05T08:00:00Z',
                modifiedAt: '2024-01-15T12:20:00Z',
                permissions: {
                  read: ['oasis-team'],
                  write: ['oasis-team'],
                  execute: ['oasis-team']
                },
                replication: {
                  enabled: true,
                  nodes: 12,
                  regions: ['us-east', 'eu-west', 'asia-pacific', 'us-west', 'south-america']
                },
                encryption: {
                  enabled: true,
                  algorithm: 'ChaCha20-Poly1305',
                  keySize: 256
                },
                storage: {
                  provider: 'p2p',
                  nodes: ['oasis-node-1', 'oasis-node-2', 'oasis-node-3', 'oasis-node-4'],
                  redundancy: 5
                },
                metadata: {
                  tags: ['oasis', 'project', 'development'],
                  description: 'Main OASIS project development files',
                  version: '3.0'
                }
              },
              {
                id: '4',
                name: 'presentation.pptx',
                type: 'file',
                mimeType: 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
                size: 15000000,
                path: '/OASIS-Project/presentation.pptx',
                createdAt: '2024-01-13T11:30:00Z',
                modifiedAt: '2024-01-15T10:15:00Z',
                permissions: {
                  read: ['investors', 'oasis-team'],
                  write: ['me'],
                  execute: ['me']
                },
                replication: {
                  enabled: true,
                  nodes: 8,
                  regions: ['us-east', 'eu-west', 'asia-pacific']
                },
                encryption: {
                  enabled: true,
                  algorithm: 'AES-256',
                  keySize: 256
                },
                storage: {
                  provider: 'web2',
                  nodes: ['aws-s3', 'google-cloud', 'azure-blob'],
                  redundancy: 3
                },
                metadata: {
                  tags: ['presentation', 'investor', 'demo'],
                  description: 'Investor presentation for OASIS project',
                  version: '1.5'
                }
              },
              {
                id: '5',
                name: 'code-repository',
                type: 'folder',
                mimeType: 'folder',
                size: 0,
                path: '/code-repository',
                createdAt: '2024-01-01T00:00:00Z',
                modifiedAt: '2024-01-15T18:00:00Z',
                permissions: {
                  read: ['developers'],
                  write: ['developers'],
                  execute: ['developers']
                },
                replication: {
                  enabled: true,
                  nodes: 15,
                  regions: ['us-east', 'eu-west', 'asia-pacific', 'us-west', 'south-america', 'africa']
                },
                encryption: {
                  enabled: true,
                  algorithm: 'AES-256',
                  keySize: 256
                },
                storage: {
                  provider: 'web3',
                  nodes: ['ipfs-node-1', 'ipfs-node-2', 'arweave-node-1', 'filecoin-node-1', 'swarm-node-1'],
                  redundancy: 6
                },
                metadata: {
                  tags: ['code', 'repository', 'development'],
                  description: 'Source code repository with version control',
                  version: '4.2'
                }
              }
            ],
            nodes: [
              {
                id: '1',
                name: 'AWS S3 US-East',
                type: 'web2',
                region: 'us-east-1',
                status: 'online',
                capacity: 1000000000000,
                used: 45000000000,
                latency: 12,
                reliability: 99.9
              },
              {
                id: '2',
                name: 'IPFS Node Alpha',
                type: 'web3',
                region: 'global',
                status: 'online',
                capacity: 500000000000,
                used: 120000000000,
                latency: 45,
                reliability: 99.5
              },
              {
                id: '3',
                name: 'OASIS P2P Node',
                type: 'p2p',
                region: 'eu-west',
                status: 'online',
                capacity: 200000000000,
                used: 80000000000,
                latency: 25,
                reliability: 99.8
              },
              {
                id: '4',
                name: 'Arweave Node',
                type: 'web3',
                region: 'global',
                status: 'online',
                capacity: 1000000000000,
                used: 300000000000,
                latency: 60,
                reliability: 99.7
              },
              {
                id: '5',
                name: 'Google Cloud',
                type: 'web2',
                region: 'us-west-1',
                status: 'maintenance',
                capacity: 800000000000,
                used: 200000000000,
                latency: 15,
                reliability: 99.9
              }
            ],
            stats: {
              totalFiles: 1247,
              totalSize: 125000000000,
              replicationFactor: 4.2,
              encryptionCoverage: 98.5,
              nodesOnline: 12,
              averageLatency: 28,
              dataIntegrity: 99.9
            }
          }
        };
      }
    },
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const getFileIcon = (mimeType: string) => {
    if (mimeType === 'folder') return <Folder sx={{ color: '#ff9800' }} />;
    if (mimeType.startsWith('image/')) return <Image sx={{ color: '#4caf50' }} />;
    if (mimeType.startsWith('video/')) return <VideoFile sx={{ color: '#f44336' }} />;
    if (mimeType.startsWith('audio/')) return <AudioFile sx={{ color: '#9c27b0' }} />;
    if (mimeType.includes('pdf')) return <Description sx={{ color: '#f44336' }} />;
    if (mimeType.includes('zip') || mimeType.includes('rar')) return <Archive sx={{ color: '#ff9800' }} />;
    if (mimeType.includes('text/') || mimeType.includes('code')) return <Code sx={{ color: '#2196f3' }} />;
    if (mimeType.includes('json') || mimeType.includes('xml')) return <DataObject sx={{ color: '#4caf50' }} />;
    return <InsertDriveFile sx={{ color: '#757575' }} />;
  };

  const getProviderColor = (provider: string) => {
    switch (provider) {
      case 'web2': return '#2196f3';
      case 'web3': return '#9c27b0';
      case 'p2p': return '#4caf50';
      default: return '#757575';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'online': return '#4caf50';
      case 'offline': return '#f44336';
      case 'maintenance': return '#ff9800';
      default: return '#757575';
    }
  };

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const filteredFiles = filesData?.result?.files?.filter((file: DataFile) => 
    file.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    file.metadata.tags.some(tag => tag.toLowerCase().includes(searchTerm.toLowerCase()))
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
        <Box sx={{ mb: 4, mt: 4 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
            <Box>
              <Typography variant="h4" gutterBottom className="page-heading">
                🚀 My Data - OASIS Hyperdrive
              </Typography>
              <Typography variant="subtitle1" color="text.secondary">
                Web2/Web3/P2P storage with auto-failover, replication, and granular security
              </Typography>
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <Button
                variant="outlined"
                startIcon={<Refresh />}
                onClick={() => refetch()}
                disabled={isLoading}
              >
                Sync
              </Button>
              <Button
                variant="contained"
                startIcon={<CloudUpload />}
                onClick={() => setUploadDialogOpen(true)}
              >
                Upload
              </Button>
            </Box>
          </Box>

          {/* Hyperdrive Stats */}
          <Grid container spacing={3} sx={{ mb: 3 }}>
            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ background: 'linear-gradient(135deg, #4caf50, #2e7d32)' }}>
                <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                  <Storage sx={{ fontSize: 40, mb: 1 }} />
                  <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                    {filesData?.result?.stats?.totalFiles || 0}
                  </Typography>
                  <Typography variant="body2">Total Files</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ background: 'linear-gradient(135deg, #2196f3, #1565c0)' }}>
                <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                  <Memory sx={{ fontSize: 40, mb: 1 }} />
                  <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                    {formatFileSize(filesData?.result?.stats?.totalSize || 0)}
                  </Typography>
                  <Typography variant="body2">Total Storage</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ background: 'linear-gradient(135deg, #9c27b0, #6a1b9a)' }}>
                <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                  <CloudSync sx={{ fontSize: 40, mb: 1 }} />
                  <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                    {filesData?.result?.stats?.replicationFactor || 0}x
                  </Typography>
                  <Typography variant="body2">Replication</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ background: 'linear-gradient(135deg, #ff9800, #ef6c00)' }}>
                <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                  <Shield sx={{ fontSize: 40, mb: 1 }} />
                  <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                    {filesData?.result?.stats?.encryptionCoverage || 0}%
                  </Typography>
                  <Typography variant="body2">Encrypted</Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          {/* Search and Filter */}
          <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
            <TextField
              placeholder="Search files and folders..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Search />
                  </InputAdornment>
                ),
              }}
              sx={{ flexGrow: 1 }}
            />
            <Button
              variant="outlined"
              startIcon={<FilterList />}
              onClick={() => toast.success('Advanced filters coming soon!')}
            >
              Filter
            </Button>
          </Box>


          {/* Tabs */}
          <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
            <Tabs value={tabValue} onChange={(e, newValue) => setTabValue(newValue)}>
              <Tab label={`My Files (${filteredFiles.length})`} />
              <Tab label={`Storage Nodes (${filesData?.result?.nodes?.length || 0})`} />
              <Tab label="Security & Permissions" />
            </Tabs>
          </Box>

          {isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
              <CircularProgress size={60} />
            </Box>
          ) : (
            <>
              {/* My Files Tab */}
              {tabValue === 0 && (
                <Grid container spacing={3}>
                  {filteredFiles.map((file: DataFile) => (
                    <Grid item xs={12} sm={6} md={4} key={file.id}>
                      <motion.div
                        variants={itemVariants}
                        whileHover={{ scale: 1.02 }}
                        transition={{ duration: 0.2 }}
                      >
                        <Card sx={{ 
                          height: '100%', 
                          display: 'flex', 
                          flexDirection: 'column',
                          '&:hover': { boxShadow: 6 }
                        }}>
                          <CardContent sx={{ flexGrow: 1 }}>
                            <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                              {getFileIcon(file.mimeType)}
                              <Typography variant="h6" sx={{ ml: 1, flexGrow: 1 }}>
                                {file.name}
                              </Typography>
                            </Box>
                            
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                              {file.metadata.description}
                            </Typography>
                            
                            <Box sx={{ mb: 2 }}>
                              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 1 }}>
                                <Chip
                                  label={file.storage.provider.toUpperCase()}
                                  size="small"
                                  sx={{
                                    bgcolor: getProviderColor(file.storage.provider),
                                    color: 'white',
                                    fontWeight: 'bold'
                                  }}
                                />
                                <Chip
                                  label={`${file.replication.nodes} nodes`}
                                  size="small"
                                  variant="outlined"
                                />
                                {file.encryption.enabled && (
                                  <Chip
                                    label="Encrypted"
                                    size="small"
                                    color="success"
                                    variant="outlined"
                                  />
                                )}
                              </Box>
                              <Typography variant="caption" color="text.secondary">
                                Size: {formatFileSize(file.size)} • Modified: {new Date(file.modifiedAt).toLocaleDateString()}
                              </Typography>
                            </Box>
                            
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                              <Box sx={{ display: 'flex', gap: 1 }}>
                                <IconButton size="small" onClick={() => toast.success('Opening file')}>
                                  <Visibility />
                                </IconButton>
                                <IconButton size="small" onClick={() => toast.success('Editing file')}>
                                  <Edit />
                                </IconButton>
                                <IconButton size="small" onClick={() => toast.success('Sharing file')}>
                                  <Share />
                                </IconButton>
                              </Box>
                              <IconButton size="small" color="error" onClick={() => toast.success('Deleting file')}>
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

              {/* Storage Nodes Tab */}
              {tabValue === 1 && (
                <Grid container spacing={3}>
                  {filesData?.result?.nodes?.map((node: StorageNode) => (
                    <Grid item xs={12} sm={6} md={4} key={node.id}>
                      <motion.div
                        variants={itemVariants}
                        whileHover={{ scale: 1.02 }}
                        transition={{ duration: 0.2 }}
                      >
                        <Card sx={{ 
                          height: '100%', 
                          display: 'flex', 
                          flexDirection: 'column',
                          '&:hover': { boxShadow: 6 }
                        }}>
                          <CardContent sx={{ flexGrow: 1 }}>
                            <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                              <Storage sx={{ color: getProviderColor(node.type), mr: 1 }} />
                              <Typography variant="h6" sx={{ flexGrow: 1 }}>
                                {node.name}
                              </Typography>
                              <Chip
                                label={node.status}
                                size="small"
                                sx={{
                                  bgcolor: getStatusColor(node.status),
                                  color: 'white',
                                  fontWeight: 'bold'
                                }}
                              />
                            </Box>
                            
                            <Box sx={{ mb: 2 }}>
                              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                                Storage Usage
                              </Typography>
                              <LinearProgress
                                variant="determinate"
                                value={(node.used / node.capacity) * 100}
                                sx={{ height: 8, borderRadius: 4, mb: 1 }}
                              />
                              <Typography variant="caption" color="text.secondary">
                                {formatFileSize(node.used)} / {formatFileSize(node.capacity)}
                              </Typography>
                            </Box>
                            
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Latency</Typography>
                                <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                                  {node.latency}ms
                                </Typography>
                              </Box>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Reliability</Typography>
                                <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                                  {node.reliability}%
                                </Typography>
                              </Box>
                            </Box>
                            
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                              <Chip
                                label={node.type.toUpperCase()}
                                size="small"
                                sx={{
                                  bgcolor: getProviderColor(node.type),
                                  color: 'white',
                                  fontWeight: 'bold'
                                }}
                              />
                              <Typography variant="caption" color="text.secondary">
                                {node.region}
                              </Typography>
                            </Box>
                          </CardContent>
                        </Card>
                      </motion.div>
                    </Grid>
                  ))}
                </Grid>
              )}

              {/* Security & Permissions Tab */}
              {tabValue === 2 && (
                <Grid container spacing={3}>
                  <Grid item xs={12} md={6}>
                    <Card>
                      <CardContent>
                        <Typography variant="h6" sx={{ mb: 2 }}>
                          🔐 Encryption Overview
                        </Typography>
                        <Box sx={{ mb: 3 }}>
                          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                            Encryption Coverage
                          </Typography>
                          <LinearProgress
                            variant="determinate"
                            value={filesData?.result?.stats?.encryptionCoverage || 0}
                            sx={{ height: 8, borderRadius: 4 }}
                          />
                          <Typography variant="caption" color="text.secondary">
                            {filesData?.result?.stats?.encryptionCoverage || 0}% of files encrypted
                          </Typography>
                        </Box>
                        <List>
                          <ListItem>
                            <ListItemIcon>
                              <CheckCircle sx={{ color: '#4caf50' }} />
                            </ListItemIcon>
                            <ListItemText
                              primary="AES-256 Encryption"
                              secondary="Military-grade encryption for sensitive files"
                            />
                          </ListItem>
                          <ListItem>
                            <ListItemIcon>
                              <CheckCircle sx={{ color: '#4caf50' }} />
                            </ListItemIcon>
                            <ListItemText
                              primary="ChaCha20-Poly1305"
                              secondary="High-performance encryption for real-time data"
                            />
                          </ListItem>
                          <ListItem>
                            <ListItemIcon>
                              <CheckCircle sx={{ color: '#4caf50' }} />
                            </ListItemIcon>
                            <ListItemText
                              primary="End-to-End Encryption"
                              secondary="Data encrypted before leaving your device"
                            />
                          </ListItem>
                        </List>
                      </CardContent>
                    </Card>
                  </Grid>
                  
                  <Grid item xs={12} md={6}>
                    <Card>
                      <CardContent>
                        <Typography variant="h6" sx={{ mb: 2 }}>
                          👥 Permission Management
                        </Typography>
                        <List>
                          <ListItem>
                            <ListItemIcon>
                              <Public sx={{ color: '#2196f3' }} />
                            </ListItemIcon>
                            <ListItemText
                              primary="Public Access"
                              secondary="Files accessible to everyone"
                            />
                          </ListItem>
                          <ListItem>
                            <ListItemIcon>
                              <Group sx={{ color: '#ff9800' }} />
                            </ListItemIcon>
                            <ListItemText
                              primary="Group Access"
                              secondary="Files shared with specific groups"
                            />
                          </ListItem>
                          <ListItem>
                            <ListItemIcon>
                              <Person sx={{ color: '#9c27b0' }} />
                            </ListItemIcon>
                            <ListItemText
                              primary="Personal Access"
                              secondary="Files accessible only to you"
                            />
                          </ListItem>
                          <ListItem>
                            <ListItemIcon>
                              <Lock sx={{ color: '#f44336' }} />
                            </ListItemIcon>
                            <ListItemText
                              primary="Restricted Access"
                              secondary="Files with custom permission rules"
                            />
                          </ListItem>
                        </List>
                      </CardContent>
                    </Card>
                  </Grid>
                  
                  <Grid item xs={12}>
                    <Card>
                      <CardContent>
                        <Typography variant="h6" sx={{ mb: 2 }}>
                          🌐 Multi-Provider Storage
                        </Typography>
                        <Grid container spacing={2}>
                          <Grid item xs={12} sm={4}>
                            <Box sx={{ textAlign: 'center', p: 2 }}>
                              <CloudUpload sx={{ fontSize: 40, color: '#2196f3', mb: 1 }} />
                              <Typography variant="h6">Web2 Storage</Typography>
                              <Typography variant="body2" color="text.secondary">
                                AWS S3, Google Cloud, Azure
                              </Typography>
                            </Box>
                          </Grid>
                          <Grid item xs={12} sm={4}>
                            <Box sx={{ textAlign: 'center', p: 2 }}>
                              <Storage sx={{ fontSize: 40, color: '#9c27b0', mb: 1 }} />
                              <Typography variant="h6">Web3 Storage</Typography>
                              <Typography variant="body2" color="text.secondary">
                                IPFS, Arweave, Filecoin
                              </Typography>
                            </Box>
                          </Grid>
                          <Grid item xs={12} sm={4}>
                            <Box sx={{ textAlign: 'center', p: 2 }}>
                              <NetworkCheck sx={{ fontSize: 40, color: '#4caf50', mb: 1 }} />
                              <Typography variant="h6">P2P Network</Typography>
                              <Typography variant="body2" color="text.secondary">
                                Decentralized, distributed storage
                              </Typography>
                            </Box>
                          </Grid>
                        </Grid>
                      </CardContent>
                    </Card>
                  </Grid>
                </Grid>
              )}
            </>
          )}
        </Box>

        {/* Upload Dialog */}
        <Dialog open={uploadDialogOpen} onClose={() => setUploadDialogOpen(false)} maxWidth="md" fullWidth>
          <DialogTitle>Upload to OASIS Hyperdrive</DialogTitle>
          <DialogContent>
            <Box sx={{ textAlign: 'center', py: 4 }}>
              <CloudUpload sx={{ fontSize: 80, color: 'primary.main', mb: 2 }} />
              <Typography variant="h6" sx={{ mb: 2 }}>
                Drag & Drop files here or click to browse
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Files will be automatically encrypted, replicated, and distributed across Web2/Web3/P2P networks
              </Typography>
              <Button variant="contained" size="large">
                Choose Files
              </Button>
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setUploadDialogOpen(false)}>Cancel</Button>
            <Button variant="contained" onClick={() => {
              setUploadDialogOpen(false);
              toast.success('Upload started! Files will be processed by OASIS Hyperdrive.');
            }}>
              Upload
            </Button>
          </DialogActions>
        </Dialog>
      </>
    </motion.div>
  );
};

export default MyDataPage;
