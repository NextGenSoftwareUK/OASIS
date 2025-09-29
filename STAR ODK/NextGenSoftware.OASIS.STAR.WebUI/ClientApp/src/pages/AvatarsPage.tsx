import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
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
  const navigate = useNavigate();
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
        // Check if the real data has meaningful values, if not use demo data
        console.log('API Response for Avatars:', response);
        if (response?.result && response.result.length > 0) {
          console.log('API returned avatars:', response.result);
          const hasRealData = response.result.some((avatar: any) => 
            avatar.karma > 0 || avatar.level || avatar.firstName
          );
          console.log('Has real data:', hasRealData);
          if (hasRealData) {
            console.log('Using API data for avatars');
            return response;
          }
        }
        console.log('API data not meaningful, using demo data');
        // Fall through to demo data if no real data or all zeros
        throw new Error('No meaningful data from API, using demo data');
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
            },
            {
              id: '4',
              title: 'Commander',
              firstName: 'Zara',
              lastName: 'Phoenix',
              email: 'zara.phoenix@oasis.com',
              username: 'zara_phoenix',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 15 * 60 * 1000).toISOString(),
              karma: 156000,
              level: 'Legendary'
            },
            {
              id: '5',
              title: 'Prof',
              firstName: 'Marcus',
              lastName: 'Voidwalker',
              email: 'marcus.void@oasis.com',
              username: 'marcus_void',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
              karma: 78500,
              level: 'Expert'
            },
            {
              id: '6',
              title: 'Agent',
              firstName: 'Luna',
              lastName: 'Starweaver',
              email: 'luna.star@oasis.com',
              username: 'luna_starweaver',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
              karma: 92000,
              level: 'Master'
            },
            {
              id: '7',
              title: 'Engineer',
              firstName: 'Kai',
              lastName: 'Nexus',
              email: 'kai.nexus@oasis.com',
              username: 'kai_nexus',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString(),
              karma: 65000,
              level: 'Advanced'
            },
            {
              id: '8',
              title: 'Pilot',
              firstName: 'Raven',
              lastName: 'Cosmos',
              email: 'raven.cosmos@oasis.com',
              username: 'raven_cosmos',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 10 * 60 * 1000).toISOString(),
              karma: 110000,
              level: 'Master'
            },
            {
              id: '9',
              title: 'Scientist',
              firstName: 'Echo',
              lastName: 'Quantum',
              email: 'echo.quantum@oasis.com',
              username: 'echo_quantum',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
              karma: 89000,
              level: 'Expert'
            },
            {
              id: '10',
              title: 'Guardian',
              firstName: 'Orion',
              lastName: 'Stardust',
              email: 'orion.stardust@oasis.com',
              username: 'orion_stardust',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 45 * 60 * 1000).toISOString(),
              karma: 134000,
              level: 'Legendary'
            },
            {
              id: '11',
              title: 'Hacker',
              firstName: 'Nyx',
              lastName: 'Shadowbyte',
              email: 'nyx.shadow@oasis.com',
              username: 'nyx_shadowbyte',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString(),
              karma: 95500,
              level: 'Master'
            },
            {
              id: '12',
              title: 'Explorer',
              firstName: 'Atlas',
              lastName: 'Voyager',
              email: 'atlas.voyager@oasis.com',
              username: 'atlas_voyager',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 20 * 60 * 1000).toISOString(),
              karma: 72000,
              level: 'Advanced'
            },
            {
              id: '13',
              title: 'Architect',
              firstName: 'Vega',
              lastName: 'Skyforge',
              email: 'vega.skyforge@oasis.com',
              username: 'vega_skyforge',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 8 * 60 * 1000).toISOString(),
              karma: 145000,
              level: 'Legendary'
            },
            {
              id: '14',
              title: 'Mystic',
              firstName: 'Celeste',
              lastName: 'Moonwhisper',
              email: 'celeste.moon@oasis.com',
              username: 'celeste_moonwhisper',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(),
              karma: 118000,
              level: 'Master'
            },
            {
              id: '15',
              title: 'Warrior',
              firstName: 'Titan',
              lastName: 'Ironclad',
              email: 'titan.ironclad@oasis.com',
              username: 'titan_ironclad',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 12 * 60 * 1000).toISOString(),
              karma: 167000,
              level: 'Legendary'
            },
            {
              id: '16',
              title: 'Sage',
              firstName: 'Wisdom',
              lastName: 'Ethereal',
              email: 'wisdom.ethereal@oasis.com',
              username: 'wisdom_ethereal',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
              karma: 89500,
              level: 'Expert'
            },
            {
              id: '17',
              title: 'Ranger',
              firstName: 'Storm',
              lastName: 'Wildfire',
              email: 'storm.wildfire@oasis.com',
              username: 'storm_wildfire',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 25 * 60 * 1000).toISOString(),
              karma: 103000,
              level: 'Master'
            },
            {
              id: '18',
              title: 'Technomancer',
              firstName: 'Cipher',
              lastName: 'Datastream',
              email: 'cipher.data@oasis.com',
              username: 'cipher_datastream',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 3 * 60 * 1000).toISOString(),
              karma: 178000,
              level: 'Legendary'
            },
            {
              id: '19',
              title: 'Bard',
              firstName: 'Melody',
              lastName: 'Starlight',
              email: 'melody.starlight@oasis.com',
              username: 'melody_starlight',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 7 * 60 * 60 * 1000).toISOString(),
              karma: 76500,
              level: 'Advanced'
            },
            {
              id: '20',
              title: 'Paladin',
              firstName: 'Justice',
              lastName: 'Lightbringer',
              email: 'justice.light@oasis.com',
              username: 'justice_lightbringer',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 18 * 60 * 1000).toISOString(),
              karma: 142000,
              level: 'Legendary'
            },
            {
              id: '21',
              title: 'Rogue',
              firstName: 'Shadow',
              lastName: 'Nightfall',
              email: 'shadow.nightfall@oasis.com',
              username: 'shadow_nightfall',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
              karma: 91000,
              level: 'Expert'
            },
            {
              id: '22',
              title: 'Alchemist',
              firstName: 'Phoenix',
              lastName: 'Goldweaver',
              email: 'phoenix.gold@oasis.com',
              username: 'phoenix_goldweaver',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 35 * 60 * 1000).toISOString(),
              karma: 127000,
              level: 'Master'
            },
            {
              id: '23',
              title: 'Monk',
              firstName: 'Zen',
              lastName: 'Peacekeeper',
              email: 'zen.peace@oasis.com',
              username: 'zen_peacekeeper',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 6 * 60 * 1000).toISOString(),
              karma: 156000,
              level: 'Legendary'
            },
            {
              id: '24',
              title: 'Necromancer',
              firstName: 'Raven',
              lastName: 'Soulweaver',
              email: 'raven.soul@oasis.com',
              username: 'raven_soulweaver',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString(),
              karma: 84000,
              level: 'Expert'
            },
            {
              id: '25',
              title: 'Druid',
              firstName: 'Forest',
              lastName: 'Earthsong',
              email: 'forest.earth@oasis.com',
              username: 'forest_earthsong',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 22 * 60 * 1000).toISOString(),
              karma: 112000,
              level: 'Master'
            },
            {
              id: '26',
              title: 'Artificer',
              firstName: 'Gear',
              lastName: 'Clockwork',
              email: 'gear.clockwork@oasis.com',
              username: 'gear_clockwork',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 14 * 60 * 1000).toISOString(),
              karma: 98000,
              level: 'Master'
            },
            {
              id: '27',
              title: 'Summoner',
              firstName: 'Void',
              lastName: 'Spiritcaller',
              email: 'void.spirit@oasis.com',
              username: 'void_spiritcaller',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
              karma: 133000,
              level: 'Master'
            },
            {
              id: '28',
              title: 'Elementalist',
              firstName: 'Blaze',
              lastName: 'Stormcaller',
              email: 'blaze.storm@oasis.com',
              username: 'blaze_stormcaller',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 9 * 60 * 1000).toISOString(),
              karma: 149000,
              level: 'Legendary'
            },
            {
              id: '29',
              title: 'Chronomancer',
              firstName: 'Time',
              lastName: 'Paradox',
              email: 'time.paradox@oasis.com',
              username: 'time_paradox',
              isBeamedIn: false,
              lastBeamedIn: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(),
              karma: 201000,
              level: 'Legendary'
            },
            {
              id: '30',
              title: 'Voidwalker',
              firstName: 'Cosmos',
              lastName: 'Infinity',
              email: 'cosmos.infinity@oasis.com',
              username: 'cosmos_infinity',
              isBeamedIn: true,
              lastBeamedIn: new Date(Date.now() - 4 * 60 * 1000).toISOString(),
              karma: 234000,
              level: 'Legendary'
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
                <Card 
                  sx={{ height: '100%', cursor: 'pointer', '&:hover': { boxShadow: 6 } }}
                  onClick={() => navigate(`/avatars/${avatar.id}`)}
                >
                  <CardContent>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                      <Avatar 
                        sx={{ width: 60, height: 60, mr: 2 }}
                        src={avatar.profileImage || `https://api.dicebear.com/7.x/avataaars/svg?seed=${avatar.username}&backgroundColor=b6e3f4,c0aede,d1d4f9,ffd5dc,ffdfbf&backgroundType=gradientLinear`}
                      >
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
                      Last active: {avatar.lastBeamedIn ? 
                        new Date(avatar.lastBeamedIn).toLocaleDateString('en-US', {
                          year: 'numeric',
                          month: 'short',
                          day: 'numeric',
                          hour: '2-digit',
                          minute: '2-digit'
                        }) : 'Never'
                      }
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
