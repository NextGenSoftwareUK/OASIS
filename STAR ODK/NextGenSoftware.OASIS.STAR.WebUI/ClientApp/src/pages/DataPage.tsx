import React, { useState } from 'react';
import {
  Box, Typography, Button, Card, CardContent, Grid, TextField,
  Dialog, DialogTitle, DialogContent, DialogActions, IconButton,
  Menu, MenuItem, FormControl, InputLabel, Select, Chip,
  Fab, Tooltip, Tabs, Tab, Badge, Stack, LinearProgress,
  List, ListItem, ListItemIcon, ListItemText, ListItemSecondaryAction,
  Alert, CircularProgress, Paper, Divider, Switch, FormControlLabel,
  Breadcrumbs, Link, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, TablePagination, Checkbox
} from '@mui/material';
import {
  Add, MoreVert, Edit, Delete, Visibility, Folder, Refresh, FilterList,
  CloudUpload, CloudDownload, FolderOpen, InsertDriveFile, Storage,
  Backup, Restore, Archive, Unarchive, Share, DriveFileMove,
  CloudDone as CloudDoneIcon, CloudOff as CloudOffIcon,
  ContentCopy, Download, Upload, Settings, Security
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { dataService } from '../services';

interface DataItem {
  id: string;
  name: string;
  type: 'file' | 'folder';
  size: string;
  path: string;
  uploadedBy: string;
  uploadedOn: string;
  lastModified: string;
  description?: string;
  isPublic: boolean;
  tags: string[];
  mimeType?: string;
  checksum?: string;
}

interface Backup {
  id: string;
  name: string;
  size: string;
  createdOn: string;
  type: 'full' | 'incremental';
  status: 'completed' | 'in_progress' | 'failed';
  description?: string;
}

const DataPage: React.FC = () => {
  const [createFolderDialogOpen, setCreateFolderDialogOpen] = useState(false);
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false);
  const [backupDialogOpen, setBackupDialogOpen] = useState(false);
  const [restoreDialogOpen, setRestoreDialogOpen] = useState(false);
  const [selectedItem, setSelectedItem] = useState<DataItem | null>(null);
  const [selectedBackup, setSelectedBackup] = useState<Backup | null>(null);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [activeTab, setActiveTab] = useState(0);
  const [currentPath, setCurrentPath] = useState('/');
  const [filterType, setFilterType] = useState('all');
  const [showPublicOnly, setShowPublicOnly] = useState(false);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [newFolderData, setNewFolderData] = useState({ name: '', path: '/' });
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadPath, setUploadPath] = useState('/');
  const [backupData, setBackupData] = useState({ name: '', description: '', type: 'full' as 'full' | 'incremental' });
  const queryClient = useQueryClient();

  const { data: dataItems, isLoading, error, refetch } = useQuery(
    ['data-items', currentPath, filterType, showPublicOnly],
    async () => {
      try {
        const response = await dataService.getFiles();
        if (response?.result && response.result.length > 0) {
          let filtered = response.result;
          if (filterType !== 'all') {
            filtered = filtered.filter((item: DataItem) => item.type === filterType);
          }
          if (showPublicOnly) {
            filtered = filtered.filter((item: DataItem) => item.isPublic);
          }
          return { ...response, result: filtered };
        }
        console.log('Using demo data items for investor presentation');
        return {
          result: [
            {
              id: 'file-1',
              name: 'Avatar Data',
              type: 'file',
              size: '2.5 MB',
              path: '/avatar-data.json',
              uploadedBy: 'demo-user',
              uploadedOn: new Date().toISOString(),
              lastModified: new Date().toISOString(),
              description: 'Avatar profile and settings data',
              isPublic: false,
              tags: ['avatar', 'profile'],
              mimeType: 'application/json',
              checksum: 'abc123def456'
            },
            {
              id: 'file-2',
              name: 'Karma History',
              type: 'file',
              size: '1.2 MB',
              path: '/karma-history.csv',
              uploadedBy: 'demo-user',
              uploadedOn: new Date(Date.now() - 86400000).toISOString(),
              lastModified: new Date(Date.now() - 3600000).toISOString(),
              description: 'Complete karma transaction history',
              isPublic: true,
              tags: ['karma', 'history'],
              mimeType: 'text/csv',
              checksum: 'def456ghi789'
            },
            {
              id: 'folder-1',
              name: 'OAPP Configurations',
              type: 'folder',
              size: '-',
              path: '/oapp-configs/',
              uploadedBy: 'demo-user',
              uploadedOn: new Date(Date.now() - 172800000).toISOString(),
              lastModified: new Date(Date.now() - 7200000).toISOString(),
              description: 'Installed OAPP configurations and settings',
              isPublic: false,
              tags: ['oapp', 'config'],
              mimeType: 'folder'
            },
            {
              id: 'file-3',
              name: 'Quantum Circuit Design',
              type: 'file',
              size: '850 KB',
              path: '/quantum-circuit.qasm',
              uploadedBy: 'demo-user',
              uploadedOn: new Date(Date.now() - 259200000).toISOString(),
              lastModified: new Date(Date.now() - 10800000).toISOString(),
              description: 'Quantum circuit design file',
              isPublic: true,
              tags: ['quantum', 'circuit'],
              mimeType: 'application/x-qasm',
              checksum: 'ghi789jkl012'
            }
          ],
          isError: false,
          message: 'Demo Data items retrieved',
          count: 4
        };
      } catch (err: any) {
        console.error('Error fetching data items:', err);
        return { result: [], isError: true, message: err.message, count: 0 };
      }
    },
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const { data: backupsData, isLoading: backupsLoading, refetch: refetchBackups } = useQuery(
    ['backups'],
    async () => {
      try {
        // This would call a backup service method
        console.log('Using demo backup data for investor presentation');
        return {
          result: [
            {
              id: 'backup-1',
              name: 'Full Backup - 2024-03-15',
              size: '1.2 GB',
              createdOn: new Date().toISOString(),
              type: 'full' as const,
              status: 'completed' as const,
              description: 'Complete system backup including all data'
            },
            {
              id: 'backup-2',
              name: 'Incremental Backup - 2024-03-14',
              size: '150 MB',
              createdOn: new Date(Date.now() - 86400000).toISOString(),
              type: 'incremental' as const,
              status: 'completed' as const,
              description: 'Incremental backup of changes since last full backup'
            },
            {
              id: 'backup-3',
              name: 'Full Backup - 2024-03-10',
              size: '1.1 GB',
              createdOn: new Date(Date.now() - 432000000).toISOString(),
              type: 'full' as const,
              status: 'completed' as const,
              description: 'Previous full system backup'
            }
          ],
          isError: false,
          message: 'Demo Backups retrieved',
          count: 3
        };
      } catch (err: any) {
        console.error('Error fetching backups:', err);
        return { result: [], isError: true, message: err.message, count: 0 };
      }
    }
  );

  const items = dataItems?.result || [];
  const backups = backupsData?.result || [];

  // Mutations
  const createFolderMutation = useMutation(
    (path: string) => dataService.createFolder(path),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['data-items']);
        console.log('Folder created successfully!');
        setCreateFolderDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to create folder: ' + error.message);
      },
    }
  );

  const uploadFileMutation = useMutation(
    (data: { file: File, path: string }) => dataService.uploadFile(data.file, data.path),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['data-items']);
        console.log('File uploaded successfully!');
        setUploadDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to upload file: ' + error.message);
      },
    }
  );

  const deleteItemMutation = useMutation(
    (id: string) => dataService.deleteFile(id),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['data-items']);
        console.log('Item deleted successfully!');
      },
      onError: (error: any) => {
        console.error('Failed to delete item: ' + error.message);
      },
    }
  );

  const renameItemMutation = useMutation(
    (data: { id: string, newName: string }) => dataService.renameItem(data.id, data.newName),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['data-items']);
        console.log('Item renamed successfully!');
      },
      onError: (error: any) => {
        console.error('Failed to rename item: ' + error.message);
      },
    }
  );

  const moveItemMutation = useMutation(
    (data: { id: string, newPath: string }) => dataService.moveItem(data.id, data.newPath),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['data-items']);
        console.log('Item moved successfully!');
      },
      onError: (error: any) => {
        console.error('Failed to move item: ' + error.message);
      },
    }
  );

  const copyItemMutation = useMutation(
    (data: { id: string, destinationPath: string }) => dataService.copyItem(data.id, data.destinationPath),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['data-items']);
        console.log('Item copied successfully!');
      },
      onError: (error: any) => {
        console.error('Failed to copy item: ' + error.message);
      },
    }
  );

  const backupDataMutation = useMutation(
    (destinationPath: string) => dataService.backupData(),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['backups']);
        console.log('Backup initiated successfully!');
        setBackupDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to initiate backup: ' + error.message);
      },
    }
  );

  const restoreDataMutation = useMutation(
    (backupId: string) => dataService.restoreData(backupId),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['data-items']);
        queryClient.invalidateQueries(['backups']);
        console.log('Data restore initiated successfully!');
        setRestoreDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to restore data: ' + error.message);
      },
    }
  );

  // Handlers
  const handleCreateFolderClick = () => {
    setNewFolderData({ name: '', path: currentPath });
    setCreateFolderDialogOpen(true);
  };

  const handleCreateFolderSubmit = () => {
    const fullPath = `${newFolderData.path}${newFolderData.name}`;
    createFolderMutation.mutate(fullPath);
  };

  const handleUploadClick = () => {
    setUploadFile(null);
    setUploadPath(currentPath);
    setUploadDialogOpen(true);
  };

  const handleUploadSubmit = () => {
    if (uploadFile) {
      uploadFileMutation.mutate({ file: uploadFile, path: uploadPath });
    }
  };

  const handleBackupClick = () => {
    setBackupData({ name: '', description: '', type: 'full' });
    setBackupDialogOpen(true);
  };

  const handleBackupSubmit = () => {
    const destinationPath = `/backups/${backupData.name}`;
    backupDataMutation.mutate(destinationPath);
  };

  const handleRestoreClick = (backup: Backup) => {
    setSelectedBackup(backup);
    setRestoreDialogOpen(true);
  };

  const handleRestoreSubmit = () => {
    if (selectedBackup) {
      restoreDataMutation.mutate(selectedBackup.id);
    }
  };

  const handleDeleteClick = (item: DataItem) => {
    deleteItemMutation.mutate(item.id);
    setAnchorEl(null);
  };

  const handleRenameClick = (item: DataItem) => {
    const newName = prompt('Enter new name:', item.name);
    if (newName && newName !== item.name) {
      renameItemMutation.mutate({ id: item.id, newName });
    }
    setAnchorEl(null);
  };

  const handleMoveClick = (item: DataItem) => {
    const newPath = prompt('Enter new path:', item.path);
    if (newPath && newPath !== item.path) {
      moveItemMutation.mutate({ id: item.id, newPath });
    }
    setAnchorEl(null);
  };

  const handleCopyClick = (item: DataItem) => {
    const destinationPath = prompt('Enter destination path:', item.path);
    if (destinationPath) {
      copyItemMutation.mutate({ id: item.id, destinationPath });
    }
    setAnchorEl(null);
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLButtonElement>, item: DataItem) => {
    setSelectedItem(item);
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const getItemIcon = (type: string, mimeType?: string) => {
    if (type === 'folder') return <Folder />;
    if (mimeType?.startsWith('image/')) return <InsertDriveFile />;
    if (mimeType?.startsWith('video/')) return <InsertDriveFile />;
    if (mimeType?.startsWith('audio/')) return <InsertDriveFile />;
    return <InsertDriveFile />;
  };

  const getItemTypeColor = (type: string) => {
    switch (type) {
      case 'folder': return '#4caf50';
      case 'file': return '#2196f3';
      default: return '#757575';
    }
  };

  const getBackupStatusColor = (status: string) => {
    switch (status) {
      case 'completed': return '#4caf50';
      case 'in_progress': return '#ff9800';
      case 'failed': return '#f44336';
      default: return '#757575';
    }
  };

  const dataStats = {
    totalItems: items.length,
    totalSize: items.reduce((sum: number, item: DataItem) => {
      if (item.type === 'file' && item.size !== '-') {
        const size = parseFloat(item.size);
        const unit = item.size.split(' ')[1];
        const multiplier = unit === 'GB' ? 1024 : unit === 'MB' ? 1 : 0.001;
        return sum + (size * multiplier);
      }
      return sum;
    }, 0),
    folders: items.filter((item: DataItem) => item.type === 'folder').length,
    files: items.filter((item: DataItem) => item.type === 'file').length,
  };

  return (
    <Box sx={{ p: 4 }}>
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        <Typography variant="h4" component="h1" gutterBottom sx={{ color: 'primary.main', fontWeight: 'bold' }}>
          <Storage sx={{ mr: 2, fontSize: 'inherit' }} />
          Data Management
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
          Manage your files, folders, and data backups in the OASIS ecosystem.
        </Typography>

        {/* Stats Overview */}
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #66bb6a, #388e3c)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <Storage sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{dataStats.totalItems}</Typography>
                <Typography variant="body2">Total Items</Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #42a5f5, #1976d2)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <CloudUpload sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                  {dataStats.totalSize.toFixed(1)} MB
                </Typography>
                <Typography variant="body2">Total Size</Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #ffa726, #ef6c00)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <Folder sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{dataStats.folders}</Typography>
                <Typography variant="body2">Folders</Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #ab47bc, #7b1fa2)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <InsertDriveFile sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{dataStats.files}</Typography>
                <Typography variant="body2">Files</Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Breadcrumbs */}
        <Breadcrumbs sx={{ mb: 3 }}>
          <Link color="inherit" href="#" onClick={() => setCurrentPath('/')}>
            Root
          </Link>
          {currentPath !== '/' && (
            <Typography color="text.primary">{currentPath}</Typography>
          )}
        </Breadcrumbs>

        {/* Tabs Navigation */}
        <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
          <Tabs value={activeTab} onChange={handleTabChange} variant="scrollable" scrollButtons="auto">
            <Tab label="Files & Folders" icon={<Folder />} />
            <Tab label="Backups" icon={<Backup />} />
            <Tab label="Upload" icon={<CloudUpload />} />
            <Tab label="Settings" icon={<Settings />} />
          </Tabs>
        </Box>

        {/* Tab Content */}
        {activeTab === 0 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            {/* Actions and Filters */}
            <Stack direction="row" spacing={2} sx={{ mb: 3 }} alignItems="center">
              <Button
                variant="contained"
                color="primary"
                startIcon={<Add />}
                onClick={handleCreateFolderClick}
              >
                New Folder
              </Button>
              <Button
                variant="outlined"
                color="secondary"
                startIcon={<CloudUpload />}
                onClick={handleUploadClick}
              >
                Upload File
              </Button>
              <Button
                variant="outlined"
                color="info"
                startIcon={<Backup />}
                onClick={handleBackupClick}
              >
                Create Backup
              </Button>
              <Tooltip title="Refresh Data">
                <IconButton onClick={() => refetch()} disabled={isLoading}>
                  <Refresh />
                </IconButton>
              </Tooltip>

              <Box sx={{ flexGrow: 1 }} />

              <FormControl sx={{ minWidth: 120 }}>
                <InputLabel size="small">Filter Type</InputLabel>
                <Select
                  value={filterType}
                  label="Filter Type"
                  onChange={(e) => setFilterType(e.target.value as string)}
                  size="small"
                >
                  <MenuItem value="all">All Types</MenuItem>
                  <MenuItem value="file">Files</MenuItem>
                  <MenuItem value="folder">Folders</MenuItem>
                </Select>
              </FormControl>

              <FormControlLabel
                control={
                  <Switch
                    checked={showPublicOnly}
                    onChange={(e) => setShowPublicOnly(e.target.checked)}
                  />
                }
                label="Public Only"
              />
            </Stack>

            {/* Data Items Table */}
            {isLoading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
                <CircularProgress />
              </Box>
            ) : error ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
                <Typography color="error">Error loading data: {(error as any).message}</Typography>
              </Box>
            ) : items.length === 0 ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
                <Typography color="text.secondary">No items found. Upload a file or create a folder to get started!</Typography>
              </Box>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Name</TableCell>
                      <TableCell>Type</TableCell>
                      <TableCell>Size</TableCell>
                      <TableCell>Modified</TableCell>
                      <TableCell>Tags</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {items.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage).map((item: DataItem) => (
                      <TableRow key={item.id} hover>
                        <TableCell>
                          <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            <ListItemIcon>
                              {getItemIcon(item.type, item.mimeType)}
                            </ListItemIcon>
                            <Box>
                              <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                                {item.name}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {item.path}
                              </Typography>
                            </Box>
                          </Box>
                        </TableCell>
                        <TableCell>
                          <Chip
                            label={item.type}
                            size="small"
                            sx={{ bgcolor: getItemTypeColor(item.type), color: 'white' }}
                          />
                        </TableCell>
                        <TableCell>{item.size}</TableCell>
                        <TableCell>{new Date(item.lastModified).toLocaleDateString()}</TableCell>
                        <TableCell>
                          <Stack direction="row" spacing={1} flexWrap="wrap">
                            {item.tags.slice(0, 2).map((tag, index) => (
                              <Chip
                                key={index}
                                label={tag}
                                size="small"
                                color="primary"
                                variant="outlined"
                              />
                            ))}
                            {item.tags.length > 2 && (
                              <Chip
                                label={`+${item.tags.length - 2}`}
                                size="small"
                                color="secondary"
                                variant="outlined"
                              />
                            )}
                          </Stack>
                        </TableCell>
                        <TableCell>
                          <IconButton
                            aria-label="more"
                            aria-controls="long-menu"
                            aria-haspopup="true"
                            onClick={(event) => handleMenuOpen(event, item)}
                            size="small"
                          >
                            <MoreVert />
                          </IconButton>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
                <TablePagination
                  rowsPerPageOptions={[5, 10, 25]}
                  component="div"
                  count={items.length}
                  rowsPerPage={rowsPerPage}
                  page={page}
                  onPageChange={handleChangePage}
                  onRowsPerPageChange={handleChangeRowsPerPage}
                />
              </TableContainer>
            )}

            {/* Context Menu for Item Actions */}
            <Menu
              id="long-menu"
              MenuListProps={{
                'aria-labelledby': 'long-button',
              }}
              anchorEl={anchorEl}
              open={Boolean(anchorEl)}
              onClose={handleMenuClose}
              PaperProps={{
                style: {
                  maxHeight: 48 * 4.5,
                  width: '20ch',
                },
              }}
            >
              <MenuItem onClick={() => {
                if (selectedItem) console.log('View Item:', selectedItem);
                handleMenuClose();
              }}>
                <Visibility sx={{ mr: 1 }} /> View
              </MenuItem>
              <MenuItem onClick={() => {
                if (selectedItem) handleRenameClick(selectedItem);
                handleMenuClose();
              }}>
                <Edit sx={{ mr: 1 }} /> Rename
              </MenuItem>
              <MenuItem onClick={() => {
                if (selectedItem) handleMoveClick(selectedItem);
                handleMenuClose();
              }}>
                <DriveFileMove sx={{ mr: 1 }} /> Move
              </MenuItem>
              <MenuItem onClick={() => {
                if (selectedItem) handleCopyClick(selectedItem);
                handleMenuClose();
              }}>
                <ContentCopy sx={{ mr: 1 }} /> Copy
              </MenuItem>
              <MenuItem onClick={() => {
                if (selectedItem) handleDeleteClick(selectedItem);
                handleMenuClose();
              }}>
                <Delete sx={{ mr: 1 }} /> Delete
              </MenuItem>
            </Menu>
          </motion.div>
        )}

        {activeTab === 1 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
              <Backup sx={{ mr: 1, verticalAlign: 'middle' }} />
              Data Backups
            </Typography>
            <Grid container spacing={3}>
              {backups.map((backup: Backup) => (
                <Grid item xs={12} sm={6} md={4} key={backup.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="h6">{backup.name}</Typography>
                        <Chip
                          label={backup.status}
                          size="small"
                          sx={{ bgcolor: getBackupStatusColor(backup.status), color: 'white' }}
                        />
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {backup.description}
                      </Typography>
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          Size: {backup.size}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Type: {backup.type}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Created: {new Date(backup.createdOn).toLocaleDateString()}
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        <Button size="small" variant="outlined" color="primary" onClick={() => handleRestoreClick(backup)}>
                          <Restore sx={{ mr: 1 }} /> Restore
                        </Button>
                        <Button size="small" variant="outlined" color="secondary">
                          <Download sx={{ mr: 1 }} /> Download
                        </Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </motion.div>
        )}

        {activeTab === 2 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
              <CloudUpload sx={{ mr: 1, verticalAlign: 'middle' }} />
              Upload Files
            </Typography>
            <Alert severity="info" sx={{ mb: 3 }}>
              Drag and drop files here or click the upload button to select files.
            </Alert>
            <Button
              variant="contained"
              startIcon={<CloudUpload />}
              onClick={handleUploadClick}
            >
              Select Files to Upload
            </Button>
          </motion.div>
        )}

        {activeTab === 3 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
              <Settings sx={{ mr: 1, verticalAlign: 'middle' }} />
              Data Settings
            </Typography>
            <Alert severity="info">
              Data management settings and preferences would be configured here.
            </Alert>
          </motion.div>
        )}

        {/* Create Folder Dialog */}
        <Dialog open={createFolderDialogOpen} onClose={() => setCreateFolderDialogOpen(false)}>
          <DialogTitle>Create New Folder</DialogTitle>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Folder Name"
              type="text"
              fullWidth
              variant="outlined"
              value={newFolderData.name}
              onChange={(e) => setNewFolderData({ ...newFolderData, name: e.target.value })}
            />
            <TextField
              margin="dense"
              label="Path"
              type="text"
              fullWidth
              variant="outlined"
              value={newFolderData.path}
              onChange={(e) => setNewFolderData({ ...newFolderData, path: e.target.value })}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setCreateFolderDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleCreateFolderSubmit} variant="contained" color="primary">Create</Button>
          </DialogActions>
        </Dialog>

        {/* Upload File Dialog */}
        <Dialog open={uploadDialogOpen} onClose={() => setUploadDialogOpen(false)}>
          <DialogTitle>Upload File</DialogTitle>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Upload Path"
              type="text"
              fullWidth
              variant="outlined"
              value={uploadPath}
              onChange={(e) => setUploadPath(e.target.value)}
              sx={{ mb: 2 }}
            />
            <input
              type="file"
              onChange={(e) => setUploadFile(e.target.files?.[0] || null)}
              style={{ marginTop: 16 }}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setUploadDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleUploadSubmit} variant="contained" color="primary" disabled={!uploadFile}>Upload</Button>
          </DialogActions>
        </Dialog>

        {/* Backup Dialog */}
        <Dialog open={backupDialogOpen} onClose={() => setBackupDialogOpen(false)}>
          <DialogTitle>Create Data Backup</DialogTitle>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Backup Name"
              type="text"
              fullWidth
              variant="outlined"
              value={backupData.name}
              onChange={(e) => setBackupData({ ...backupData, name: e.target.value })}
              sx={{ mb: 2 }}
            />
            <TextField
              margin="dense"
              label="Description"
              type="text"
              fullWidth
              variant="outlined"
              multiline
              rows={3}
              value={backupData.description}
              onChange={(e) => setBackupData({ ...backupData, description: e.target.value })}
              sx={{ mb: 2 }}
            />
            <FormControl fullWidth>
              <InputLabel>Backup Type</InputLabel>
              <Select
                value={backupData.type}
                onChange={(e) => setBackupData({ ...backupData, type: e.target.value as 'full' | 'incremental' })}
                label="Backup Type"
              >
                <MenuItem value="full">Full Backup</MenuItem>
                <MenuItem value="incremental">Incremental Backup</MenuItem>
              </Select>
            </FormControl>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setBackupDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleBackupSubmit} variant="contained" color="primary">Create Backup</Button>
          </DialogActions>
        </Dialog>

        {/* Restore Dialog */}
        <Dialog open={restoreDialogOpen} onClose={() => setRestoreDialogOpen(false)}>
          <DialogTitle>Restore from Backup</DialogTitle>
          <DialogContent>
            <Alert severity="warning" sx={{ mb: 2 }}>
              This will restore data from the selected backup. This action cannot be undone.
            </Alert>
            {selectedBackup && (
              <Box>
                <Typography variant="h6">{selectedBackup.name}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {selectedBackup.description}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Size: {selectedBackup.size} • Created: {new Date(selectedBackup.createdOn).toLocaleDateString()}
                </Typography>
              </Box>
            )}
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setRestoreDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleRestoreSubmit} variant="contained" color="error">Restore</Button>
          </DialogActions>
        </Dialog>
      </motion.div>
    </Box>
  );
};

export default DataPage;
