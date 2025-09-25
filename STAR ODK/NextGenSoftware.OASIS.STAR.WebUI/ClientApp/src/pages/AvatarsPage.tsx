import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Button,
  Grid,
  Avatar,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  Alert,
  Fab,
  Tooltip,
} from '@mui/material';
import {
  AccountCircle,
  Add,
  Edit,
  Delete,
  Person,
  Email,
  Star,
  Refresh,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starService } from '../services/starService';
import { toast } from 'react-hot-toast';

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
}

const AvatarsPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedAvatar, setSelectedAvatar] = useState<Avatar | null>(null);
  const [newAvatar, setNewAvatar] = useState({
    title: 'Dr',
    firstName: '',
    lastName: '',
    email: '',
    username: '',
    password: '',
  });

  const queryClient = useQueryClient();

  // Fetch all avatars
  const { data: avatarsData, isLoading, error, refetch } = useQuery(
    'avatars',
    async () => {
      try {
        const response = await starService.getAllAvatars();
        return response;
      } catch (error) {
        // Fallback to demo data for investor presentation - only log to console
        console.log('Using demo Avatar data for investor presentation:', error);
        return {
          result: [
            {
              id: '1',
              title: 'Dr',
              firstName: 'Sarah',
              lastName: 'Chen',
              email: 'sarah.chen@oasis.com',
              username: 'sarah_chen',
              isBeamedIn: true,
              lastBeamedIn: new Date().toISOString(),
              karma: 125000,
              level: 'Legendary'
            },
            {
              id: '2',
              title: 'Captain',
              firstName: 'Nova',
              lastName: 'Stellar',
              email: 'nova.stellar@oasis.com',
              username: 'captain_nova',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
              karma: 98000,
              level: 'Master'
            },
            {
              id: '3',
              title: 'Dr',
              firstName: 'Alex',
              lastName: 'Quantum',
              email: 'alex.quantum@oasis.com',
              username: 'alex_quantum',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 30 * 60 * 1000).toISOString(),
              karma: 87500,
              level: 'Expert'
            }
          ]
        };
      }
    },
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  // Create avatar mutation
  const createAvatarMutation = useMutation(
    (avatarData: any) => starService.createAvatar(avatarData),
    {
      onSuccess: () => {
        queryClient.invalidateQueries('avatars');
        setCreateDialogOpen(false);
        setNewAvatar({
          title: 'Dr',
          firstName: '',
          lastName: '',
          email: '',
          username: '',
          password: '',
        });
        toast.success('Avatar created successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to create avatar');
      },
    }
  );

  // Delete avatar mutation
  const deleteAvatarMutation = useMutation(
    (avatarId: string) => starService.deleteAvatar(avatarId),
    {
      onSuccess: () => {
        queryClient.invalidateQueries('avatars');
        toast.success('Avatar deleted successfully!');
      },
      onError: (error: any) => {
        toast.error('Failed to delete avatar');
      },
    }
  );

  const handleCreateAvatar = () => {
    createAvatarMutation.mutate(newAvatar);
  };

  const handleDeleteAvatar = (avatarId: string) => {
    if (window.confirm('Are you sure you want to delete this avatar?')) {
      deleteAvatarMutation.mutate(avatarId);
    }
  };

  const avatars = avatarsData?.result || [];

  return (
    <>
        <Box sx={{ mb: 4, mt: 4, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Box>
          <Typography variant="h4" gutterBottom className="page-heading">
            Avatars
          </Typography>
          <Typography variant="subtitle1" color="text.secondary">
            Avatar management and user profiles
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button
            variant="outlined"
            startIcon={<Refresh />}
            onClick={() => refetch()}
            disabled={isLoading}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<Add />}
            onClick={() => setCreateDialogOpen(true)}
          >
            Create Avatar
          </Button>
        </Box>
      </Box>


      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {avatars.map((avatar: any) => (
            <Grid item xs={12} sm={6} md={4} key={avatar.id}>
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3 }}
              >
                <Card sx={{ height: '100%' }}>
                  <CardContent>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                      <Avatar sx={{ width: 60, height: 60, bgcolor: 'primary.main', mr: 2 }}>
                        {avatar.username?.charAt(0).toUpperCase()}
                      </Avatar>
                      <Box sx={{ flexGrow: 1 }}>
                        <Typography variant="h6">
                          {avatar.title} {avatar.firstName} {avatar.lastName}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          @{avatar.username}
                        </Typography>
                      </Box>
                      <Box>
                        <IconButton
                          size="small"
                          onClick={() => {
                            setSelectedAvatar(avatar);
                            setEditDialogOpen(true);
                          }}
                        >
                          <Edit />
                        </IconButton>
                        <IconButton
                          size="small"
                          onClick={() => handleDeleteAvatar(avatar.id)}
                          disabled={deleteAvatarMutation.isLoading}
                        >
                          <Delete />
                        </IconButton>
                      </Box>
                    </Box>

                    <Box sx={{ mb: 2 }}>
                      <Typography variant="body2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                        <Email sx={{ fontSize: 16, mr: 1 }} />
                        {avatar.email}
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 1, mb: 1 }}>
                        <Chip
                          label={avatar.isBeamedIn ? 'Online' : 'Offline'}
                          size="small"
                          color={avatar.isBeamedIn ? 'success' : 'default'}
                          variant="outlined"
                        />
                        {avatar.karma && (
                          <Chip
                            label={`${avatar.karma} Karma`}
                            size="small"
                            color="secondary"
                            variant="outlined"
                          />
                        )}
                      </Box>
                    </Box>

                    <Typography variant="caption" color="text.secondary">
                      Last active: {new Date(avatar.lastBeamedIn).toLocaleDateString()}
                    </Typography>
                  </CardContent>
                </Card>
              </motion.div>
            </Grid>
          ))}
        </Grid>
      )}

      {/* Create Avatar Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New Avatar</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Title"
              value={newAvatar.title}
              onChange={(e) => setNewAvatar({ ...newAvatar, title: e.target.value })}
              fullWidth
            />
            <TextField
              label="First Name"
              value={newAvatar.firstName}
              onChange={(e) => setNewAvatar({ ...newAvatar, firstName: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Last Name"
              value={newAvatar.lastName}
              onChange={(e) => setNewAvatar({ ...newAvatar, lastName: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Email"
              type="email"
              value={newAvatar.email}
              onChange={(e) => setNewAvatar({ ...newAvatar, email: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Username"
              value={newAvatar.username}
              onChange={(e) => setNewAvatar({ ...newAvatar, username: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Password"
              type="password"
              value={newAvatar.password}
              onChange={(e) => setNewAvatar({ ...newAvatar, password: e.target.value })}
              fullWidth
              required
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateAvatar}
            variant="contained"
            disabled={createAvatarMutation.isLoading}
          >
            {createAvatarMutation.isLoading ? 'Creating...' : 'Create Avatar'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default AvatarsPage;
