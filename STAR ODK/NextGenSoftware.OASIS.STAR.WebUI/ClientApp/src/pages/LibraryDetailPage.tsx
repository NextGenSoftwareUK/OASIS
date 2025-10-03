import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  Alert,
  Divider,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Paper,
  Avatar,
  Badge,
  LinearProgress,
} from '@mui/material';
import {
  ArrowBack,
  Edit,
  Delete,
  Share,
  Download,
  Visibility,
  LibraryBooks,
  Star,
  Person,
  CalendarToday,
  Category,
  Code,
  Security,
  Speed,
  Memory,
  Storage,
  AttachMoney,
  TrendingUp,
  Flag,
  Assignment,
  Bookmark,
  School,
  CheckCircle,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starNetService } from '../services/starNetService';
import { Library } from '../types/star';
import { toast } from 'react-hot-toast';

const LibraryDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [library, setLibrary] = useState<Library | null>(null);

  const queryClient = useQueryClient();

  // Fetch library details
  const { data: libraryData, isLoading, error, refetch } = useQuery(
    ['library', id],
    async () => {
      if (!id) throw new Error('Library ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await starNetService.getAllLibraries();
      const foundLibrary = response.result?.find((l: Library) => l.id === id);
      if (!foundLibrary) throw new Error('Library not found');
      return { result: foundLibrary };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setLibrary(data.result);
        }
      },
    }
  );

  // Update library mutation
  const updateLibraryMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('Library ID is required');
      return await starNetService.updateLibrary(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['library', id]);
        queryClient.invalidateQueries('allLibraries');
        toast.success('Library updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update library');
        console.error('Update library error:', error);
      },
    }
  );

  // Delete library mutation
  const deleteLibraryMutation = useMutation(
    async () => {
      if (!id) throw new Error('Library ID is required');
      return await starNetService.deleteLibrary(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allLibraries');
        toast.success('Library deleted successfully!');
        navigate('/libraries');
      },
      onError: (error: any) => {
        toast.error('Failed to delete library');
        console.error('Delete library error:', error);
      },
    }
  );

  const handleEdit = () => {
    setEditDialogOpen(true);
  };

  const handleDelete = () => {
    setDeleteDialogOpen(true);
  };

  const handleShare = () => {
    setShareDialogOpen(true);
  };

  const handleEditSubmit = () => {
    updateLibraryMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteLibraryMutation.mutate();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active': return 'success';
      case 'deprecated': return 'error';
      case 'beta': return 'warning';
      case 'stable': return 'primary';
      default: return 'default';
    }
  };

  const getTypeColor = (type: string) => {
    switch (type.toLowerCase()) {
      case 'framework': return 'primary';
      case 'utility': return 'secondary';
      case 'plugin': return 'success';
      case 'template': return 'warning';
      default: return 'default';
    }
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <CircularProgress size={60} />
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load library details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!library) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Library not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/libraries')} sx={{ mt: 2 }}>
          Back to Libraries
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/libraries')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {library.name}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {library.type} • {library.version}
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<Share />}
            onClick={handleShare}
          >
            Share
          </Button>
          <Button
            variant="outlined"
            startIcon={<Edit />}
            onClick={handleEdit}
          >
            Edit
          </Button>
          <Button
            variant="outlined"
            color="error"
            startIcon={<Delete />}
            onClick={handleDelete}
          >
            Delete
          </Button>
        </Box>
      </Box>

      <Grid container spacing={3}>
        {/* Library Info */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Library Description
              </Typography>
              <Typography variant="body1" paragraph>
                {library.description}
              </Typography>
              
              <Divider sx={{ my: 2 }} />
              
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Category sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Type
                    </Typography>
                  </Box>
                  <Chip
                    label={library.type}
                    color={getTypeColor(library.type || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Flag sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Version
                    </Typography>
                  </Box>
                  <Chip
                    label={library.version}
                    color="primary"
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <CheckCircle sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Status
                    </Typography>
                  </Box>
                  <Chip
                    label={library.status}
                    color={getStatusColor(library.status || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <CalendarToday sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Last Updated
                    </Typography>
                  </Box>
                  <Typography variant="body1">
                    {formatDate(library.lastUpdated ? library.lastUpdated.toISOString() : new Date().toISOString())}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Library Stats and Info */}
        <Grid item xs={12} md={4}>
          <Grid container spacing={2}>
            {/* Usage Stats Card */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <TrendingUp sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Usage Statistics
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Download />
                      </ListItemIcon>
                      <ListItemText
                        primary="Downloads"
                        secondary={library.downloads?.toLocaleString() || '0'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Star />
                      </ListItemIcon>
                      <ListItemText
                        primary="Rating"
                        secondary={`${library.rating || 0}/5`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Bookmark />
                      </ListItemIcon>
                      <ListItemText
                        primary="Bookmarks"
                        secondary={library.bookmarks?.toLocaleString() || '0'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Library Info */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <LibraryBooks sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Library Info
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Person />
                      </ListItemIcon>
                      <ListItemText
                        primary="Author"
                        secondary={library.author || 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <CalendarToday />
                      </ListItemIcon>
                      <ListItemText
                        primary="Created"
                        secondary={library.createdDate ? formatDate(library.createdDate.toISOString()) : 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Security />
                      </ListItemIcon>
                      <ListItemText
                        primary="License"
                        secondary={library.license || 'MIT'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Dependencies */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Assignment sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Dependencies
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {library.dependencies?.map((dep, index) => (
                      <Chip
                        key={index}
                        label={dep}
                        size="small"
                        color="secondary"
                      />
                    )) || (
                      <Typography variant="body2" color="text.secondary">
                        No dependencies
                      </Typography>
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>

        {/* Library Features */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <School sx={{ mr: 1, verticalAlign: 'middle' }} />
                Library Features
              </Typography>
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Size
                    </Typography>
                    <Typography variant="h6">
                      {library.size || 'N/A'} KB
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Files
                    </Typography>
                    <Typography variant="h6">
                      {library.fileCount || 'N/A'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Documentation
                    </Typography>
                    <Typography variant="h6">
                      {library.hasDocumentation ? 'Yes' : 'No'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Examples
                    </Typography>
                    <Typography variant="h6">
                      {library.exampleCount || '0'}
                    </Typography>
                  </Box>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Edit Dialog */}
      <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Library</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Name"
                value={library.name}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={library.description}
                multiline
                rows={3}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Type"
                value={library.type}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Version"
                value={library.version}
                disabled
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleEditSubmit}
            variant="contained"
            disabled={updateLibraryMutation.isLoading}
          >
            {updateLibraryMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Library</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{library.name}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteLibraryMutation.isLoading}
          >
            {deleteLibraryMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share Library</DialogTitle>
        <DialogContent>
          <Typography>
            Share this library with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/libraries/${library.id}`}
            sx={{ mt: 2 }}
            InputProps={{
              readOnly: true,
            }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShareDialogOpen(false)}>Close</Button>
          <Button
            onClick={() => {
              navigator.clipboard.writeText(`${window.location.origin}/libraries/${library.id}`);
              toast.success('Link copied to clipboard!');
              setShareDialogOpen(false);
            }}
            variant="contained"
          >
            Copy Link
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default LibraryDetailPage;
