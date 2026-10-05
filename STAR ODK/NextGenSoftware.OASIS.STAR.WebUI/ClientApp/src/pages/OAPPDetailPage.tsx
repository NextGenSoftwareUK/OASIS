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
  LinearProgress,
  Rating,
  Avatar,
} from '@mui/material';
import {
  ArrowBack,
  PlayArrow,
  Pause,
  Download,
  Upload,
  Delete,
  Edit,
  Visibility,
  Apps,
  Star,
  Person,
  CalendarToday,
  Category,
  Code,
  Security,
  Speed,
  Memory,
  Storage,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { oappService } from '../services';
import { OAPP } from '../types/star';
import { toast } from 'react-hot-toast';

const OAPPDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [installDialogOpen, setInstallDialogOpen] = useState(false);
  const [uninstallDialogOpen, setUninstallDialogOpen] = useState(false);
  const [oapp, setOapp] = useState<OAPP | null>(null);

  const queryClient = useQueryClient();

  // Fetch OAPP details
  const { data: oappData, isLoading, error, refetch } = useQuery(
    ['oapp', id],
    async () => {
      if (!id) throw new Error('OAPP ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await oappService.getAll();
      const foundOapp = response.result?.find((o: OAPP) => o.id === id);
      if (!foundOapp) throw new Error('OAPP not found');
      return { result: foundOapp };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setOapp(data.result);
        }
      },
    }
  );

  // Install OAPP mutation
  const installOappMutation = useMutation(
    async () => {
      if (!id) throw new Error('OAPP ID is required');
      return await oappService.install(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['oapp', id]);
        queryClient.invalidateQueries('allOAPPs');
        toast.success('OAPP installed successfully!');
        setInstallDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to install OAPP');
        console.error('Install OAPP error:', error);
      },
    }
  );

  // Uninstall OAPP mutation
  const uninstallOappMutation = useMutation(
    async () => {
      if (!id) throw new Error('OAPP ID is required');
      return await oappService.uninstall(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['oapp', id]);
        queryClient.invalidateQueries('allOAPPs');
        toast.success('OAPP uninstalled successfully!');
        setUninstallDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to uninstall OAPP');
        console.error('Uninstall OAPP error:', error);
      },
    }
  );

  const handleInstall = () => {
    setInstallDialogOpen(true);
  };

  const handleUninstall = () => {
    setUninstallDialogOpen(true);
  };

  const handleInstallConfirm = () => {
    installOappMutation.mutate();
  };

  const handleUninstallConfirm = () => {
    uninstallOappMutation.mutate();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  const getTypeColor = (type: string) => {
    switch (type.toLowerCase()) {
      case 'web': return 'primary';
      case 'desktop': return 'secondary';
      case 'mobile': return 'success';
      case 'api': return 'warning';
      default: return 'default';
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category.toLowerCase()) {
      case 'exploration': return 'primary';
      case 'analytics': return 'secondary';
      case 'machine learning': return 'success';
      case 'gaming': return 'warning';
      case 'productivity': return 'info';
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
          Failed to load OAPP details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!oapp) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          OAPP not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/oapps')} sx={{ mt: 2 }}>
          Back to OAPPs
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/oapps')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {oapp.name}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            by {oapp.author}
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {oapp.isInstalled ? (
            <Button
              variant="outlined"
              color="error"
              startIcon={<Pause />}
              onClick={handleUninstall}
            >
              Uninstall
            </Button>
          ) : (
            <Button
              variant="contained"
              startIcon={<Download />}
              onClick={handleInstall}
            >
              Install
            </Button>
          )}
          <Button
            variant="outlined"
            startIcon={<Visibility />}
          >
            Preview
          </Button>
        </Box>
      </Box>

      <Grid container spacing={3}>
        {/* OAPP Info Card */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Description
              </Typography>
              <Typography variant="body1" paragraph>
                {oapp.description}
              </Typography>
              
              <Divider sx={{ my: 2 }} />
              
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Category sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Category
                    </Typography>
                  </Box>
                  <Chip
                    label={oapp.category}
                    color={getCategoryColor(oapp.category || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Code sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Type
                    </Typography>
                  </Box>
                  <Chip
                    label={oapp.type}
                    color={getTypeColor(oapp.type || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Speed sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Version
                    </Typography>
                  </Box>
                  <Typography variant="body1">
                    {oapp.version}
                  </Typography>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <CalendarToday sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Last Updated
                    </Typography>
                  </Box>
                  <Typography variant="body1">
                    {formatDate(oapp.lastUpdated ? oapp.lastUpdated.toISOString() : new Date().toISOString())}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Stats and Actions */}
        <Grid item xs={12} md={4}>
          <Grid container spacing={2}>
            {/* Rating Card */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Star sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Rating & Reviews
                  </Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Rating value={oapp.rating} precision={0.1} readOnly />
                    <Typography variant="h6" sx={{ ml: 1 }}>
                      {oapp.rating}
                    </Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Based on {oapp.downloads?.toLocaleString() || '0'} downloads
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            {/* Status Card */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Security sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Status
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Apps />
                      </ListItemIcon>
                      <ListItemText
                        primary="Installation Status"
                        secondary={oapp.isInstalled ? 'Installed' : 'Not Installed'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Visibility />
                      </ListItemIcon>
                      <ListItemText
                        primary="Publication Status"
                        secondary={oapp.isPublished ? 'Published' : 'Draft'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <PlayArrow />
                      </ListItemIcon>
                      <ListItemText
                        primary="Active Status"
                        secondary={oapp.isActive ? 'Active' : 'Inactive'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Author Info */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Person sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Author
                  </Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <Avatar sx={{ mr: 2 }}>
                      {oapp.author?.charAt(0) || 'A'}
                    </Avatar>
                    <Box>
                      <Typography variant="body1">
                        {oapp.author}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Developer
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>
      </Grid>

      {/* Install Dialog */}
      <Dialog open={installDialogOpen} onClose={() => setInstallDialogOpen(false)}>
        <DialogTitle>Install OAPP</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to install "{oapp.name}"? This will download and set up the application.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setInstallDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleInstallConfirm}
            variant="contained"
            disabled={installOappMutation.isLoading}
          >
            {installOappMutation.isLoading ? <CircularProgress size={20} /> : 'Install'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Uninstall Dialog */}
      <Dialog open={uninstallDialogOpen} onClose={() => setUninstallDialogOpen(false)}>
        <DialogTitle>Uninstall OAPP</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to uninstall "{oapp.name}"? This will remove the application and all its data.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setUninstallDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleUninstallConfirm}
            color="error"
            variant="contained"
            disabled={uninstallOappMutation.isLoading}
          >
            {uninstallOappMutation.isLoading ? <CircularProgress size={20} /> : 'Uninstall'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default OAPPDetailPage;
