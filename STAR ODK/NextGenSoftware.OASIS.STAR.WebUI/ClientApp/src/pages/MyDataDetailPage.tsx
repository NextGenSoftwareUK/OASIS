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
} from '@mui/material';
import {
  ArrowBack,
  Storage,
  CloudUpload,
  CloudDownload,
  Security,
  Timeline,
  Edit,
  Delete,
  Add,
  CheckCircle,
  Cancel,
  Visibility,
  Lock,
  Public,
  DataUsage,
  Backup,
  Restore,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starCoreService, avatarService } from '../services';
import { toast } from 'react-hot-toast';

interface DataFile {
  id: string;
  name: string;
  type: string;
  size: number;
  uploadDate: Date;
  lastModified: Date;
  isPublic: boolean;
  isEncrypted: boolean;
  downloadCount: number;
  tags: string[];
  description: string;
}

interface DataBackup {
  id: string;
  name: string;
  date: Date;
  size: number;
  status: 'completed' | 'in_progress' | 'failed';
  type: 'full' | 'incremental';
}

interface MyDataDetail {
  id: string;
  totalFiles: number;
  totalSize: number;
  publicFiles: number;
  privateFiles: number;
  encryptedFiles: number;
  recentUploads: DataFile[];
  dataFiles: DataFile[];
  backups: DataBackup[];
  storageUsage: {
    used: number;
    available: number;
    total: number;
  };
  security: {
    encryptionEnabled: boolean;
    twoFactorEnabled: boolean;
    lastSecurityCheck: Date;
  };
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
      id={`data-tabpanel-${index}`}
      aria-labelledby={`data-tab-${index}`}
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

const MyDataDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedFile, setSelectedFile] = useState<DataFile | null>(null);

  // Fetch my data detail
  const { data, isLoading, error } = useQuery(
    ['myDataDetail', id],
    async () => {
      // Demo data for now
      const demoData: MyDataDetail = {
        id: id || '1',
        totalFiles: 47,
        totalSize: 2.4 * 1024 * 1024 * 1024, // 2.4 GB
        publicFiles: 12,
        privateFiles: 35,
        encryptedFiles: 28,
        recentUploads: [
          {
            id: '1',
            name: 'avatar_profile_v2.json',
            type: 'JSON',
            size: 1024 * 1024, // 1 MB
            uploadDate: new Date('2024-01-15'),
            lastModified: new Date('2024-01-15'),
            isPublic: false,
            isEncrypted: true,
            downloadCount: 0,
            tags: ['avatar', 'profile', 'personal'],
            description: 'Updated avatar profile configuration'
          },
          {
            id: '2',
            name: 'mission_data_export.csv',
            type: 'CSV',
            size: 512 * 1024, // 512 KB
            uploadDate: new Date('2024-01-14'),
            lastModified: new Date('2024-01-14'),
            isPublic: true,
            isEncrypted: false,
            downloadCount: 5,
            tags: ['mission', 'export', 'analytics'],
            description: 'Mission completion data export'
          }
        ],
        dataFiles: [
          {
            id: '1',
            name: 'avatar_profile_v2.json',
            type: 'JSON',
            size: 1024 * 1024,
            uploadDate: new Date('2024-01-15'),
            lastModified: new Date('2024-01-15'),
            isPublic: false,
            isEncrypted: true,
            downloadCount: 0,
            tags: ['avatar', 'profile', 'personal'],
            description: 'Updated avatar profile configuration'
          },
          {
            id: '2',
            name: 'mission_data_export.csv',
            type: 'CSV',
            size: 512 * 1024,
            uploadDate: new Date('2024-01-14'),
            lastModified: new Date('2024-01-14'),
            isPublic: true,
            isEncrypted: false,
            downloadCount: 5,
            tags: ['mission', 'export', 'analytics'],
            description: 'Mission completion data export'
          },
          {
            id: '3',
            name: 'oasis_settings_backup.xml',
            type: 'XML',
            size: 256 * 1024,
            uploadDate: new Date('2024-01-13'),
            lastModified: new Date('2024-01-13'),
            isPublic: false,
            isEncrypted: true,
            downloadCount: 0,
            tags: ['settings', 'backup', 'configuration'],
            description: 'OASIS application settings backup'
          },
          {
            id: '4',
            name: 'quantum_calculation_results.dat',
            type: 'DAT',
            size: 2 * 1024 * 1024,
            uploadDate: new Date('2024-01-12'),
            lastModified: new Date('2024-01-12'),
            isPublic: false,
            isEncrypted: true,
            downloadCount: 0,
            tags: ['quantum', 'calculations', 'research'],
            description: 'Quantum computing calculation results'
          }
        ],
        backups: [
          {
            id: '1',
            name: 'Full Backup - 2024-01-15',
            date: new Date('2024-01-15'),
            size: 2.4 * 1024 * 1024 * 1024,
            status: 'completed',
            type: 'full'
          },
          {
            id: '2',
            name: 'Incremental Backup - 2024-01-14',
            date: new Date('2024-01-14'),
            size: 150 * 1024 * 1024,
            status: 'completed',
            type: 'incremental'
          },
          {
            id: '3',
            name: 'Incremental Backup - 2024-01-13',
            date: new Date('2024-01-13'),
            size: 75 * 1024 * 1024,
            status: 'completed',
            type: 'incremental'
          }
        ],
        storageUsage: {
          used: 2.4 * 1024 * 1024 * 1024,
          available: 7.6 * 1024 * 1024 * 1024,
          total: 10 * 1024 * 1024 * 1024
        },
        security: {
          encryptionEnabled: true,
          twoFactorEnabled: true,
          lastSecurityCheck: new Date('2024-01-15')
        }
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
      case 'completed': return 'success';
      case 'in_progress': return 'warning';
      case 'failed': return 'error';
      default: return 'default';
    }
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  if (isLoading) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Loading data details...</Typography>
      </Box>
    );
  }

  if (error || !data) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography color="error">Error loading data details</Typography>
        <Button onClick={() => navigate('/mydata')} startIcon={<ArrowBack />}>
          Back to My Data
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
          <IconButton onClick={() => navigate('/mydata')} sx={{ mr: 2 }}>
            <ArrowBack />
          </IconButton>
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h4" gutterBottom>
              My Data Dashboard
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Manage your personal data and storage
            </Typography>
          </Box>
          <Button
            variant="outlined"
            startIcon={<CloudUpload />}
            sx={{ mr: 1 }}
          >
            Upload File
          </Button>
          <Button
            variant="contained"
            startIcon={<Backup />}
          >
            Create Backup
          </Button>
        </Box>

        {/* Data Overview */}
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Storage color="primary" sx={{ mr: 1 }} />
                  <Typography variant="h6">Total Files</Typography>
                </Box>
                <Typography variant="h3" color="primary">
                  {data.totalFiles}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Files stored
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <DataUsage color="secondary" sx={{ mr: 1 }} />
                  <Typography variant="h6">Storage Used</Typography>
                </Box>
                <Typography variant="h3" color="secondary">
                  {formatFileSize(data.totalSize)}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  of {formatFileSize(data.storageUsage.total)}
                </Typography>
                <Box sx={{ mt: 2 }}>
                  <LinearProgress 
                    variant="determinate" 
                    value={(data.storageUsage.used / data.storageUsage.total) * 100} 
                  />
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Public color="success" sx={{ mr: 1 }} />
                  <Typography variant="h6">Public Files</Typography>
                </Box>
                <Typography variant="h3" color="success.main">
                  {data.publicFiles}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Shared publicly
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Lock color="warning" sx={{ mr: 1 }} />
                  <Typography variant="h6">Encrypted</Typography>
                </Box>
                <Typography variant="h3" color="warning.main">
                  {data.encryptedFiles}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Secured files
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Security Status */}
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              <Security sx={{ mr: 1, verticalAlign: 'middle' }} />
              Security Status
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <Box sx={{ display: 'flex', alignItems: 'center', p: 2, bgcolor: 'background.paper', borderRadius: 1 }}>
                  <CheckCircle color={data.security.encryptionEnabled ? 'success' : 'error'} sx={{ mr: 2 }} />
                  <Box>
                    <Typography variant="subtitle1">Encryption</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {data.security.encryptionEnabled ? 'Enabled' : 'Disabled'}
                    </Typography>
                  </Box>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Box sx={{ display: 'flex', alignItems: 'center', p: 2, bgcolor: 'background.paper', borderRadius: 1 }}>
                  <CheckCircle color={data.security.twoFactorEnabled ? 'success' : 'error'} sx={{ mr: 2 }} />
                  <Box>
                    <Typography variant="subtitle1">Two-Factor Auth</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {data.security.twoFactorEnabled ? 'Enabled' : 'Disabled'}
                    </Typography>
                  </Box>
                </Box>
              </Grid>
            </Grid>
            <Typography variant="caption" color="text.secondary" sx={{ mt: 2, display: 'block' }}>
              Last security check: {formatDate(data.security.lastSecurityCheck)}
            </Typography>
          </CardContent>
        </Card>

        {/* Tabs */}
        <Card>
          <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
            <Tabs value={tabValue} onChange={handleTabChange}>
              <Tab label="All Files" />
              <Tab label="Recent Uploads" />
              <Tab label="Backups" />
            </Tabs>
          </Box>

          <TabPanel value={tabValue} index={0}>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Name</TableCell>
                    <TableCell>Type</TableCell>
                    <TableCell>Size</TableCell>
                    <TableCell>Upload Date</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data.dataFiles.map((file) => (
                    <TableRow key={file.id}>
                      <TableCell>
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <Storage sx={{ mr: 1 }} />
                          <Box>
                            <Typography variant="subtitle2">{file.name}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              {file.description}
                            </Typography>
                          </Box>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Chip label={file.type} size="small" />
                      </TableCell>
                      <TableCell>{formatFileSize(file.size)}</TableCell>
                      <TableCell>{formatDate(file.uploadDate)}</TableCell>
                      <TableCell>
                        <Box sx={{ display: 'flex', gap: 0.5 }}>
                          {file.isPublic && <Chip label="Public" color="success" size="small" />}
                          {file.isEncrypted && <Chip label="Encrypted" color="warning" size="small" />}
                        </Box>
                      </TableCell>
                      <TableCell>
                        <IconButton size="small">
                          <Visibility />
                        </IconButton>
                        <IconButton size="small">
                          <CloudDownload />
                        </IconButton>
                        <IconButton size="small">
                          <Delete />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </TabPanel>

          <TabPanel value={tabValue} index={1}>
            <List>
              {data.recentUploads.map((file, index) => (
                <React.Fragment key={file.id}>
                  <ListItem>
                    <ListItemIcon>
                      <Avatar sx={{ bgcolor: 'primary.main' }}>
                        <Storage />
                      </Avatar>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="subtitle1">{file.name}</Typography>
                          <Box sx={{ display: 'flex', gap: 0.5 }}>
                            {file.isPublic && <Chip label="Public" color="success" size="small" />}
                            {file.isEncrypted && <Chip label="Encrypted" color="warning" size="small" />}
                          </Box>
                        </Box>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="text.secondary">
                            {file.description}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {formatFileSize(file.size)} • {formatDate(file.uploadDate)}
                          </Typography>
                        </Box>
                      }
                    />
                  </ListItem>
                  {index < data.recentUploads.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          </TabPanel>

          <TabPanel value={tabValue} index={2}>
            <List>
              {data.backups.map((backup, index) => (
                <React.Fragment key={backup.id}>
                  <ListItem>
                    <ListItemIcon>
                      <Avatar sx={{ bgcolor: `${getStatusColor(backup.status)}.main` }}>
                        <Backup />
                      </Avatar>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="subtitle1">{backup.name}</Typography>
                          <Box sx={{ display: 'flex', gap: 0.5 }}>
                            <Chip 
                              label={backup.status} 
                              color={getStatusColor(backup.status)} 
                              size="small" 
                            />
                            <Chip 
                              label={backup.type} 
                              color="default" 
                              size="small" 
                            />
                          </Box>
                        </Box>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="text.secondary">
                            {formatFileSize(backup.size)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {formatDate(backup.date)}
                          </Typography>
                        </Box>
                      }
                    />
                    <IconButton>
                      <Restore />
                    </IconButton>
                  </ListItem>
                  {index < data.backups.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          </TabPanel>
        </Card>
      </motion.div>
    </Box>
  );
};

export default MyDataDetailPage;

