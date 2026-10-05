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
  Description,
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
  CheckCircle,
  Assignment,
  Bookmark,
  School,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { templateService } from '../services';
import { Template } from '../types/star';
import { toast } from 'react-hot-toast';

const TemplateDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [template, setTemplate] = useState<Template | null>(null);

  const queryClient = useQueryClient();

  // Fetch template details
  const { data: templateData, isLoading, error, refetch } = useQuery(
    ['template', id],
    async () => {
      if (!id) throw new Error('Template ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await templateService.getAll();
      const foundTemplate = response.result?.find((t: Template) => t.id === id);
      if (!foundTemplate) throw new Error('Template not found');
      return { result: foundTemplate };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setTemplate(data.result);
        }
      },
    }
  );

  // Update template mutation
  const updateTemplateMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('Template ID is required');
      return await templateService.update(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['template', id]);
        queryClient.invalidateQueries('allTemplates');
        toast.success('Template updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update template');
        console.error('Update template error:', error);
      },
    }
  );

  // Delete template mutation
  const deleteTemplateMutation = useMutation(
    async () => {
      if (!id) throw new Error('Template ID is required');
      return await templateService.delete(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allTemplates');
        toast.success('Template deleted successfully!');
        navigate('/templates');
      },
      onError: (error: any) => {
        toast.error('Failed to delete template');
        console.error('Delete template error:', error);
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
    updateTemplateMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteTemplateMutation.mutate();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'published': return 'success';
      case 'draft': return 'warning';
      case 'archived': return 'error';
      case 'review': return 'info';
      default: return 'default';
    }
  };

  const getTypeColor = (type: string) => {
    switch (type.toLowerCase()) {
      case 'web': return 'primary';
      case 'mobile': return 'secondary';
      case 'desktop': return 'success';
      case 'api': return 'warning';
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
          Failed to load template details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!template) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Template not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/templates')} sx={{ mt: 2 }}>
          Back to Templates
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/templates')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {template.name}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {template.type} • {template.category}
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
        {/* Template Info */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Template Description
              </Typography>
              <Typography variant="body1" paragraph>
                {template.description}
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
                    label={template.type}
                    color={getTypeColor(template.type || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Flag sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Category
                    </Typography>
                  </Box>
                  <Chip
                    label={template.category}
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
                    label={template.status}
                    color={getStatusColor(template.status || 'Unknown') as any}
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
                    {formatDate(template.lastUpdated ? template.lastUpdated.toISOString() : new Date().toISOString())}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Template Stats and Info */}
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
                        secondary={template.downloads?.toLocaleString() || '0'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Star />
                      </ListItemIcon>
                      <ListItemText
                        primary="Rating"
                        secondary={`${template.rating || 0}/5`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Bookmark />
                      </ListItemIcon>
                      <ListItemText
                        primary="Bookmarks"
                        secondary={template.bookmarks?.toLocaleString() || '0'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Template Info */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Description sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Template Info
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Person />
                      </ListItemIcon>
                      <ListItemText
                        primary="Author"
                        secondary={template.author || 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <CalendarToday />
                      </ListItemIcon>
                      <ListItemText
                        primary="Created"
                        secondary={template.createdDate ? formatDate(template.createdDate.toISOString()) : 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Security />
                      </ListItemIcon>
                      <ListItemText
                        primary="License"
                        secondary={template.license || 'MIT'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Technologies */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Assignment sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Technologies
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {template.technologies?.map((tech, index) => (
                      <Chip
                        key={index}
                        label={tech}
                        size="small"
                        color="secondary"
                      />
                    )) || (
                      <Typography variant="body2" color="text.secondary">
                        No technologies specified
                      </Typography>
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>

        {/* Template Features */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <School sx={{ mr: 1, verticalAlign: 'middle' }} />
                Template Features
              </Typography>
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Size
                    </Typography>
                    <Typography variant="h6">
                      {template.size || 'N/A'} MB
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Files
                    </Typography>
                    <Typography variant="h6">
                      {template.fileCount || 'N/A'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Documentation
                    </Typography>
                    <Typography variant="h6">
                      {template.hasDocumentation ? 'Yes' : 'No'}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Examples
                    </Typography>
                    <Typography variant="h6">
                      {template.exampleCount || '0'}
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
        <DialogTitle>Edit Template</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Name"
                value={template.name}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={template.description}
                multiline
                rows={3}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Type"
                value={template.type}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Category"
                value={template.category}
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
            disabled={updateTemplateMutation.isLoading}
          >
            {updateTemplateMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Template</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{template.name}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteTemplateMutation.isLoading}
          >
            {deleteTemplateMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share Template</DialogTitle>
        <DialogContent>
          <Typography>
            Share this template with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/templates/${template.id}`}
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
              navigator.clipboard.writeText(`${window.location.origin}/templates/${template.id}`);
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

export default TemplateDetailPage;
