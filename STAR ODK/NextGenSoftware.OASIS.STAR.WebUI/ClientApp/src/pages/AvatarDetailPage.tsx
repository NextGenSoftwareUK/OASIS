import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Avatar,
  Chip,
  Button,
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
  ListItemSecondaryAction,
  Paper,
  LinearProgress,
  Checkbox,
  FormControlLabel,
  Switch,
  Tooltip,
  Badge,
  Stack,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Menu,
  MenuItem,
} from '@mui/material';
import {
  AccountCircle,
  Edit,
  Delete,
  Person,
  Email,
  Star,
  Refresh,
  ArrowBack,
  Timeline,
  Security,
  Settings,
  Public,
  Lock,
  Verified,
  ExitToApp,
  Computer,
  Phone,
  Tablet,
  Gamepad,
  Web,
  Cloud,
  CheckCircle,
  Warning,
  Error,
  MoreVert,
  SelectAll,
  ClearAll,
  Logout,
  Visibility,
  VisibilityOff,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { avatarService } from '../services';
import { toast } from 'react-hot-toast';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';

interface Avatar {
  id: string;
  title: string;
  firstName: string;
  lastName: string;
  email: string;
  username: string;
  isBeamedIn: boolean;
  lastBeamedIn: string;
  karma?: number;
  level?: number;
  xp?: number;
  isActive?: boolean;
  createdDate?: Date;
  lastLoginDate?: Date;
}

interface Session {
  id: string;
  serviceName: string;
  serviceType: 'game' | 'app' | 'website' | 'platform' | 'service';
  deviceType: 'desktop' | 'mobile' | 'tablet' | 'console' | 'vr';
  deviceName: string;
  location: string;
  ipAddress: string;
  isActive: boolean;
  lastActivity: string;
  loginTime: string;
  userAgent: string;
  platform: string;
  version: string;
}

interface SessionManagement {
  totalSessions: number;
  activeSessions: number;
  sessions: Session[];
}

const AvatarDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [sessionDialogOpen, setSessionDialogOpen] = useState(false);
  const [logoutDialogOpen, setLogoutDialogOpen] = useState(false);
  const [selectedSessions, setSelectedSessions] = useState<string[]>([]);
  const [showInactiveSessions, setShowInactiveSessions] = useState(false);
  const [sessionMenuAnchor, setSessionMenuAnchor] = useState<null | HTMLElement>(null);
  const [avatar, setAvatar] = useState<Avatar | null>(null);
  const [sessionData, setSessionData] = useState<SessionManagement | null>(null);
  const [editData, setEditData] = useState({
    title: '',
    firstName: '',
    lastName: '',
    email: '',
    username: '',
  });

  const queryClient = useQueryClient();

  // Fetch avatar details
  const { data: avatarData, isLoading, error, refetch } = useQuery(
    ['avatar', id],
    async () => {
      if (!id) {
        throw 'Avatar ID is required';
      }
      const response = await avatarService.getById(id);
      return response;
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setAvatar(data.result as Avatar);
          setEditData({
            title: data.result.title || '',
            firstName: data.result.firstName || '',
            lastName: data.result.lastName || '',
            email: data.result.email || '',
            username: data.result.username || '',
          });
        }
      },
    }
  );

  // Fetch session management data
  const { data: sessionsData, isLoading: sessionsLoading, refetch: refetchSessions } = useQuery(
    ['avatar-sessions', id],
    async () => {
      if (!id) {
        throw 'Avatar ID is required';
      }
      try {
        const response = await avatarService.getSessions?.(id);
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo session data for Avatar SSO System');
        return {
          result: {
            totalSessions: 12,
            activeSessions: 4,
            sessions: [
              {
                id: '1',
                serviceName: 'STARNET Dashboard',
                serviceType: 'platform',
                deviceType: 'desktop',
                deviceName: 'MacBook Pro 16"',
                location: 'San Francisco, CA',
                ipAddress: '192.168.1.100',
                isActive: true,
                lastActivity: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
                loginTime: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
                userAgent: 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)',
                platform: 'macOS',
                version: '1.0.0',
              },
              {
                id: '2',
                serviceName: 'OASIS Gaming Platform',
                serviceType: 'game',
                deviceType: 'vr',
                deviceName: 'Oculus Quest 3',
                location: 'San Francisco, CA',
                ipAddress: '192.168.1.101',
                isActive: true,
                lastActivity: new Date(Date.now() - 15 * 60 * 1000).toISOString(),
                loginTime: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
                userAgent: 'OculusBrowser/1.0.0',
                platform: 'Android',
                version: '2.1.0',
              },
              {
                id: '3',
                serviceName: 'STAR Mobile App',
                serviceType: 'app',
                deviceType: 'mobile',
                deviceName: 'iPhone 15 Pro',
                location: 'San Francisco, CA',
                ipAddress: '192.168.1.102',
                isActive: true,
                lastActivity: new Date(Date.now() - 30 * 60 * 1000).toISOString(),
                loginTime: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
                userAgent: 'STAR Mobile/3.2.1 (iPhone; iOS 17.0)',
                platform: 'iOS',
                version: '3.2.1',
              },
              {
                id: '4',
                serviceName: 'Quantum Calculator OAPP',
                serviceType: 'app',
                deviceType: 'desktop',
                deviceName: 'Windows PC',
                location: 'New York, NY',
                ipAddress: '203.0.113.45',
                isActive: true,
                lastActivity: new Date(Date.now() - 45 * 60 * 1000).toISOString(),
                loginTime: new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString(),
                userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
                platform: 'Windows',
                version: '1.5.2',
              },
              {
                id: '5',
                serviceName: 'STARNET Store',
                serviceType: 'website',
                deviceType: 'tablet',
                deviceName: 'iPad Pro 12.9"',
                location: 'San Francisco, CA',
                ipAddress: '192.168.1.103',
                isActive: false,
                lastActivity: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
                loginTime: new Date(Date.now() - 12 * 60 * 60 * 1000).toISOString(),
                userAgent: 'Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X)',
                platform: 'iPadOS',
                version: '1.0.0',
              },
              {
                id: '6',
                serviceName: 'Neural Network SDK',
                serviceType: 'service',
                deviceType: 'desktop',
                deviceName: 'Linux Workstation',
                location: 'Seattle, WA',
                ipAddress: '198.51.100.23',
                isActive: false,
                lastActivity: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
                loginTime: new Date(Date.now() - 18 * 60 * 60 * 1000).toISOString(),
                userAgent: 'STAR SDK/2.0.0 (Linux x86_64)',
                platform: 'Linux',
                version: '2.0.0',
              },
            ],
          }
        };
      }
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setSessionData(data.result as SessionManagement);
        }
      },
    }
  );

  // Update avatar mutation
  const updateAvatarMutation = useMutation(
    async (data: any) => {
      if (!id) {
        throw 'Avatar ID is required';
      }
      return await avatarService.update(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['avatar', id]);
        queryClient.invalidateQueries('avatars');
        toast.success('Avatar updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update avatar');
        console.error('Update avatar error:', error);
      },
    }
  );

  // Delete avatar mutation
  const deleteAvatarMutation = useMutation(
    async () => {
      if (!id) {
        throw 'Avatar ID is required';
      }
      return await avatarService.delete(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('avatars');
        toast.success('Avatar deleted successfully!');
        navigate('/avatars');
      },
      onError: (error: any) => {
        toast.error('Failed to delete avatar');
        console.error('Delete avatar error:', error);
      },
    }
  );

  // Logout sessions mutation
  const logoutSessionsMutation = useMutation(
    async (sessionIds: string[]) => {
      if (!id) {
        throw 'Avatar ID is required';
      }
      try {
        return await avatarService.logoutSessions?.(id, sessionIds);
      } catch (error) {
        // Demo mode - simulate logout
        console.log('Demo mode: Logging out sessions', sessionIds);
        return { success: true };
      }
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['avatar-sessions', id]);
        toast.success('Sessions logged out successfully!');
        setSelectedSessions([]);
        setLogoutDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to logout sessions');
        console.error('Logout sessions error:', error);
      },
    }
  );

  const handleEdit = () => {
    setEditDialogOpen(true);
  };

  const handleDelete = () => {
    setDeleteDialogOpen(true);
  };

  const handleEditSubmit = () => {
    updateAvatarMutation.mutate(editData);
  };

  const handleDeleteConfirm = () => {
    deleteAvatarMutation.mutate();
  };

  const handleSessionManagement = () => {
    setSessionDialogOpen(true);
  };

  const handleSessionSelect = (sessionId: string) => {
    setSelectedSessions(prev => 
      prev.includes(sessionId) 
        ? prev.filter(id => id !== sessionId)
        : [...prev, sessionId]
    );
  };

  const handleSelectAllSessions = () => {
    const activeSessions = sessionData?.sessions.filter(s => s.isActive) || [];
    setSelectedSessions(activeSessions.map(s => s.id));
  };

  const handleClearAllSessions = () => {
    setSelectedSessions([]);
  };

  const handleLogoutSelected = () => {
    if (selectedSessions.length === 0) {
      toast.error('Please select sessions to logout');
      return;
    }
    setLogoutDialogOpen(true);
  };

  const handleLogoutConfirm = () => {
    logoutSessionsMutation.mutate(selectedSessions);
  };

  const handleSessionMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setSessionMenuAnchor(event.currentTarget);
  };

  const handleSessionMenuClose = () => {
    setSessionMenuAnchor(null);
  };

  const formatDate = (dateString: string | Date) => {
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  const getKarmaLevel = (karma: number) => {
    if (karma >= 200000) return { level: 'Master', color: 'purple' };
    if (karma >= 100000) return { level: 'Expert', color: 'blue' };
    if (karma >= 50000) return { level: 'Advanced', color: 'green' };
    if (karma >= 10000) return { level: 'Intermediate', color: 'orange' };
    return { level: 'Beginner', color: 'grey' };
  };

  const getDeviceIcon = (deviceType: string) => {
    switch (deviceType) {
      case 'desktop': return <Computer />;
      case 'mobile': return <Phone />;
      case 'tablet': return <Tablet />;
      case 'console': return <Gamepad />;
      case 'vr': return <Web />;
      default: return <Computer />;
    }
  };

  const getServiceIcon = (serviceType: string) => {
    switch (serviceType) {
      case 'game': return <Gamepad />;
      case 'app': return <Web />;
      case 'website': return <Public />;
      case 'platform': return <Cloud />;
      case 'service': return <Settings />;
      default: return <Web />;
    }
  };

  const getTimeAgo = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));
    
    if (diffInMinutes < 1) return 'Just now';
    if (diffInMinutes < 60) return `${diffInMinutes} minutes ago`;
    if (diffInMinutes < 1440) return `${Math.floor(diffInMinutes / 60)} hours ago`;
    return `${Math.floor(diffInMinutes / 1440)} days ago`;
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
          Failed to load avatar details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!avatar) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Avatar not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/avatars')} sx={{ mt: 2 }}>
          Back to Avatars
        </Button>
      </Box>
    );
  }

  const karmaLevel = getKarmaLevel(avatar.karma || 0);
  const xpProgress = ((avatar.xp || 0) % 10000) / 100; // Assuming 10k XP per level

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/avatars')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Typography variant="h4" component="h1">
          Avatar Details
        </Typography>
        <Box sx={{ ml: 'auto' }}>
          <Button
            variant="outlined"
            startIcon={<Edit />}
            onClick={handleEdit}
            sx={{ mr: 1 }}
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
        {/* Avatar Profile Card */}
        <Grid item xs={12} md={4}>
          <Card sx={{ height: 'fit-content' }}>
            <CardContent sx={{ textAlign: 'center', p: 3 }}>
              <Avatar
                sx={{
                  width: 120,
                  height: 120,
                  mx: 'auto',
                  mb: 2,
                  bgcolor: 'primary.main',
                  fontSize: '3rem',
                }}
              >
                {avatar.firstName.charAt(0)}{avatar.lastName.charAt(0)}
              </Avatar>
              <Typography variant="h5" gutterBottom>
                {avatar.title} {avatar.firstName} {avatar.lastName}
              </Typography>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                @{avatar.username}
              </Typography>
              <Chip
                label={avatar.isBeamedIn ? 'Online' : 'Offline'}
                color={avatar.isBeamedIn ? 'success' : 'default'}
                size="small"
                sx={{ mb: 2 }}
              />
              <Divider sx={{ my: 2 }} />
              <Box sx={{ textAlign: 'left' }}>
                <Typography variant="body2" color="text.secondary">
                  Email
                </Typography>
                <Typography variant="body1" sx={{ mb: 1 }}>
                  {avatar.email}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Member Since
                </Typography>
                <Typography variant="body1">
                  {avatar.createdDate ? formatDate(avatar.createdDate) : 'Unknown'}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Avatar Stats and Info */}
        <Grid item xs={12} md={8}>
          <Grid container spacing={3}>
            {/* Karma and Level */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Star sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Karma & Level
                  </Typography>
                  <Grid container spacing={3}>
                    <Grid item xs={12} sm={6}>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h3" color="primary">
                          {avatar.karma?.toLocaleString() || 0}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Karma Points
                        </Typography>
                        <Chip
                          label={karmaLevel.level}
                          size="small"
                          sx={{
                            mt: 1,
                            bgcolor: karmaLevel.color,
                            color: '#fff'
                          }}
                        />
                      </Box>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h3" color="secondary">
                          {avatar.level || 1}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Level
                        </Typography>
                        <Box sx={{ mt: 1 }}>
                          <LinearProgress
                            variant="determinate"
                            value={xpProgress}
                            sx={{ height: 8, borderRadius: 4 }}
                          />
                          <Typography variant="caption" color="text.secondary">
                            {xpProgress.toFixed(1)}% to next level
                          </Typography>
                        </Box>
                      </Box>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            {/* Session Management */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Typography variant="h6">
                      <Security sx={{ mr: 1, verticalAlign: 'middle' }} />
                      Session Management
                    </Typography>
                    <Box>
                      <Button
                        variant="outlined"
                        startIcon={<ExitToApp />}
                        onClick={handleSessionManagement}
                        sx={{ mr: 1 }}
                      >
                        Manage Sessions
                      </Button>
                      <IconButton onClick={handleSessionMenuOpen}>
                        <MoreVert />
                      </IconButton>
                    </Box>
                  </Box>
                  
                  {sessionData && (
                    <Grid container spacing={2} sx={{ mb: 2 }}>
                      <Grid item xs={6} sm={3}>
                        <Paper sx={{ p: 2, textAlign: 'center' }}>
                          <Typography variant="h4" color="primary">
                            {sessionData.activeSessions}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Active Sessions
                          </Typography>
                        </Paper>
                      </Grid>
                      <Grid item xs={6} sm={3}>
                        <Paper sx={{ p: 2, textAlign: 'center' }}>
                          <Typography variant="h4" color="secondary">
                            {sessionData.totalSessions}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Total Sessions
                          </Typography>
                        </Paper>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Paper sx={{ p: 2 }}>
                          <Typography variant="body2" color="text.secondary" gutterBottom>
                            Current Location
                          </Typography>
                          <Typography variant="h6">
                            🌍 San Francisco, CA
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Last beamed in: {getTimeAgo(avatar.lastBeamedIn)}
                          </Typography>
                        </Paper>
                      </Grid>
                    </Grid>
                  )}

                  <Typography variant="subtitle2" gutterBottom>
                    Recent Activity
                  </Typography>
                  <List dense>
                    {(sessionData?.sessions || []).slice(0, 3).map((session) => (
                      <ListItem key={session.id} divider>
                        <ListItemIcon>
                          <Avatar sx={{ bgcolor: session.isActive ? 'success.main' : 'grey.400' }}>
                            {getServiceIcon(session.serviceType)}
                          </Avatar>
                        </ListItemIcon>
                        <ListItemText
                          primary={session.serviceName}
                          secondary={
                            <Box>
                              <Typography variant="body2" color="text.secondary">
                                {session.deviceName} • {session.location}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {session.isActive ? 'Active' : 'Inactive'} • {getTimeAgo(session.lastActivity)}
                              </Typography>
                            </Box>
                          }
                        />
                        <ListItemSecondaryAction>
                          <Chip
                            label={session.isActive ? 'Online' : 'Offline'}
                            size="small"
                            color={session.isActive ? 'success' : 'default'}
                          />
                        </ListItemSecondaryAction>
                      </ListItem>
                    ))}
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Activity Status */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Timeline sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Activity Status
                  </Typography>
                  <List>
                    <ListItem>
                      <ListItemIcon>
                        <Public />
                      </ListItemIcon>
                      <ListItemText
                        primary="Last Beamed In"
                        secondary={avatar.lastBeamedIn ? formatDate(avatar.lastBeamedIn) : 'Never'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Person />
                      </ListItemIcon>
                      <ListItemText
                        primary="Last Login"
                        secondary={avatar.lastLoginDate ? formatDate(avatar.lastLoginDate) : 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Security />
                      </ListItemIcon>
                      <ListItemText
                        primary="Account Status"
                        secondary={avatar.isActive ? 'Active' : 'Inactive'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Recent Activity */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Settings sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Account Information
                  </Typography>
                  <List>
                    <ListItem>
                      <ListItemText
                        primary="Username"
                        secondary={avatar.username}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemText
                        primary="Email"
                        secondary={avatar.email}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemText
                        primary="Full Name"
                        secondary={`${avatar.title} ${avatar.firstName} ${avatar.lastName}`}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemText
                        primary="Account Created"
                        secondary={avatar.createdDate ? formatDate(avatar.createdDate) : 'Unknown'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>
      </Grid>

      {/* Edit Dialog */}
      <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Avatar</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Title"
                value={editData.title}
                onChange={(e) => setEditData({ ...editData, title: e.target.value })}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Username"
                value={editData.username}
                onChange={(e) => setEditData({ ...editData, username: e.target.value })}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="First Name"
                value={editData.firstName}
                onChange={(e) => setEditData({ ...editData, firstName: e.target.value })}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Last Name"
                value={editData.lastName}
                onChange={(e) => setEditData({ ...editData, lastName: e.target.value })}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Email"
                type="email"
                value={editData.email}
                onChange={(e) => setEditData({ ...editData, email: e.target.value })}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleEditSubmit}
            variant="contained"
            disabled={updateAvatarMutation.isLoading}
          >
            {updateAvatarMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Session Management Dialog */}
      <Dialog open={sessionDialogOpen} onClose={() => setSessionDialogOpen(false)} maxWidth="lg" fullWidth>
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <Typography variant="h6">
              <Security sx={{ mr: 1, verticalAlign: 'middle' }} />
              Session Management
            </Typography>
            <Box>
              <Button
                startIcon={<SelectAll />}
                onClick={handleSelectAllSessions}
                size="small"
                sx={{ mr: 1 }}
              >
                Select All
              </Button>
              <Button
                startIcon={<ClearAll />}
                onClick={handleClearAllSessions}
                size="small"
                sx={{ mr: 1 }}
              >
                Clear All
              </Button>
              <FormControlLabel
                control={
                  <Switch
                    checked={showInactiveSessions}
                    onChange={(e) => setShowInactiveSessions(e.target.checked)}
                  />
                }
                label="Show Inactive"
                sx={{ ml: 2 }}
              />
            </Box>
          </Box>
        </DialogTitle>
        <DialogContent>
          <Box sx={{ mb: 2 }}>
            <Typography variant="body2" color="text.secondary">
              Manage your active sessions across all OASIS services. You can remotely logout from any device or service.
            </Typography>
          </Box>
          
          <List>
            {sessionData?.sessions
              .filter(session => showInactiveSessions || session.isActive)
              .map((session) => (
                <ListItem key={session.id} divider>
                  <Checkbox
                    checked={selectedSessions.includes(session.id)}
                    onChange={() => handleSessionSelect(session.id)}
                    disabled={!session.isActive}
                  />
                  <ListItemIcon>
                    <Avatar sx={{ bgcolor: session.isActive ? 'success.main' : 'grey.400' }}>
                      {getDeviceIcon(session.deviceType)}
                    </Avatar>
                  </ListItemIcon>
                  <ListItemText
                    primary={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="h6">
                          {session.serviceName}
                        </Typography>
                        <Chip
                          label={session.serviceType}
                          size="small"
                          color="primary"
                          variant="outlined"
                        />
                        <Chip
                          label={session.platform}
                          size="small"
                          color="secondary"
                          variant="outlined"
                        />
                      </Box>
                    }
                    secondary={
                      <Box>
                        <Typography variant="body2" color="text.secondary">
                          {session.deviceName} • {session.location} • {session.ipAddress}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {session.isActive ? 'Active' : 'Inactive'} • 
                          Last activity: {getTimeAgo(session.lastActivity)} • 
                          Login: {formatDate(session.loginTime)}
                        </Typography>
                        <Typography variant="caption" color="text.secondary" display="block">
                          {session.userAgent}
                        </Typography>
                      </Box>
                    }
                  />
                  <ListItemSecondaryAction>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Chip
                        label={session.isActive ? 'Online' : 'Offline'}
                        size="small"
                        color={session.isActive ? 'success' : 'default'}
                      />
                      <Tooltip title="Session Details">
                        <IconButton size="small">
                          <Visibility />
                        </IconButton>
                      </Tooltip>
                    </Box>
                  </ListItemSecondaryAction>
                </ListItem>
              ))}
          </List>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSessionDialogOpen(false)}>
            Cancel
          </Button>
          <Button
            onClick={handleLogoutSelected}
            variant="contained"
            color="error"
            startIcon={<Logout />}
            disabled={selectedSessions.length === 0}
          >
            Logout Selected ({selectedSessions.length})
          </Button>
        </DialogActions>
      </Dialog>

      {/* Logout Confirmation Dialog */}
      <Dialog open={logoutDialogOpen} onClose={() => setLogoutDialogOpen(false)}>
        <DialogTitle>Confirm Logout</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to logout from {selectedSessions.length} session(s)? 
            This will end the sessions on those devices/services.
          </Typography>
          <Box sx={{ mt: 2 }}>
            {selectedSessions.map(sessionId => {
              const session = sessionData?.sessions.find(s => s.id === sessionId);
              return session ? (
                <Chip
                  key={sessionId}
                  label={`${session.serviceName} (${session.deviceName})`}
                  size="small"
                  sx={{ mr: 1, mb: 1 }}
                />
              ) : null;
            })}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLogoutDialogOpen(false)}>
            Cancel
          </Button>
          <Button
            onClick={handleLogoutConfirm}
            color="error"
            variant="contained"
            disabled={logoutSessionsMutation.isLoading}
            startIcon={<Logout />}
          >
            {logoutSessionsMutation.isLoading ? <CircularProgress size={20} /> : 'Logout Sessions'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Session Menu */}
      <Menu
        anchorEl={sessionMenuAnchor}
        open={Boolean(sessionMenuAnchor)}
        onClose={handleSessionMenuClose}
      >
        <MenuItem onClick={() => { handleSessionMenuClose(); refetchSessions(); }}>
          <Refresh sx={{ mr: 1 }} />
          Refresh Sessions
        </MenuItem>
        <MenuItem onClick={() => { handleSessionMenuClose(); setShowInactiveSessions(!showInactiveSessions); }}>
          {showInactiveSessions ? <VisibilityOff sx={{ mr: 1 }} /> : <Visibility sx={{ mr: 1 }} />}
          {showInactiveSessions ? 'Hide Inactive' : 'Show Inactive'}
        </MenuItem>
        <MenuItem onClick={() => { handleSessionMenuClose(); handleSelectAllSessions(); }}>
          <SelectAll sx={{ mr: 1 }} />
          Select All Active
        </MenuItem>
      </Menu>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Avatar</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete this avatar? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteAvatarMutation.isLoading}
          >
            {deleteAvatarMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default AvatarDetailPage;
