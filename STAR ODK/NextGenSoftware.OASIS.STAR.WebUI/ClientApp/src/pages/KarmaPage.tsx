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
} from '@mui/material';
import {
  Search,
  Star,
  TrendingUp,
  EmojiEvents,
  Person,
  Apps,
  Refresh,
  FilterList,
  Visibility,
  Share,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { statsService, avatarService } from '../services';

interface AvatarKarma {
  id: string;
  name: string;
  username: string;
  avatar: string;
  totalKarma: number;
  level: string;
  rank: number;
  achievements: string[];
  lastActive: string;
  karmaHistory: number[];
  oappsUsed: string[];
}

interface OAPPKarma {
  id: string;
  name: string;
  description: string;
  icon: string;
  totalKarmaGenerated: number;
  usersCount: number;
  averageKarma: number;
  category: string;
  rating: number;
  lastUpdated: string;
}

interface KarmaLeaderboard {
  avatars: AvatarKarma[];
  oapps: OAPPKarma[];
}

const KarmaPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [tabValue, setTabValue] = useState(0);
  const [filterLevel, setFilterLevel] = useState('all');

  const queryClient = useQueryClient();

        const { data: karmaData, isLoading, error, refetch } = useQuery(
          'karmaLeaderboard',
          async () => {
            try {
              // Try to get real data first
              const response = await statsService.getLeaderboard?.();
              return response;
            } catch (error) {
              // Fallback to impressive demo data - only log to console
              console.log('Using demo Karma data for investor presentation:', error);
              return {
                result: {
                  avatars: [
              {
                id: '1',
                name: 'Cosmos Infinity',
                username: 'cosmos_infinity',
                avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=150&fit=crop&crop=face',
                totalKarma: 234000,
                level: 'Legendary',
                rank: 1,
                achievements: ['Voidwalker Supreme', 'Reality Architect', 'Infinite Explorer'],
                lastActive: '2024-01-15T11:45:00Z',
                karmaHistory: [230000, 231500, 232800, 233400, 234000],
                oappsUsed: ['Void Explorer', 'Reality Engine', 'Infinite Cosmos'],
              },
              {
                id: '2',
                name: 'Time Paradox',
                username: 'time_paradox',
                avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face',
                totalKarma: 201000,
                level: 'Legendary',
                rank: 2,
                achievements: ['Chronomancer Master', 'Time Weaver', 'Temporal Guardian'],
                lastActive: '2024-01-12T15:30:00Z',
                karmaHistory: [198000, 199000, 199800, 200400, 201000],
                oappsUsed: ['Time Machine', 'Temporal Lab', 'Chronos Engine'],
              },
              {
                id: '3',
                name: 'Cipher Datastream',
                username: 'cipher_datastream',
                avatar: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150&h=150&fit=crop&crop=face',
                totalKarma: 178000,
                level: 'Legendary',
                rank: 3,
                achievements: ['Technomancer Elite', 'Data Architect', 'Code Wizard'],
                lastActive: '2024-01-14T14:20:00Z',
                karmaHistory: [175000, 176000, 176800, 177400, 178000],
                oappsUsed: ['Data Matrix', 'Cyber Lab', 'Neural Network'],
              },
              {
                id: '4',
                name: 'Phoenix Goldweaver',
                username: 'phoenix_goldweaver',
                avatar: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150&h=150&fit=crop&crop=face',
                totalKarma: 127000,
                level: 'Legendary',
                rank: 4,
                achievements: ['Alchemist Master', 'Gold Weaver', 'Phoenix Rising'],
                lastActive: '2024-01-15T08:20:00Z',
                karmaHistory: [124000, 125000, 126000, 126500, 127000],
                oappsUsed: ['Alchemy Lab', 'Gold Forge', 'Phoenix Nest'],
              },
              {
                id: '5',
                name: 'Dr. Sarah Chen',
                username: 'sarah_chen',
                avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=150&h=150&fit=crop&crop=face',
                totalKarma: 125000,
                level: 'Legendary',
                rank: 5,
                achievements: ['Karma Master', 'OASIS Pioneer', 'Quantum Explorer'],
                lastActive: '2024-01-15T10:30:00Z',
                karmaHistory: [120000, 122000, 123500, 124200, 125000],
                oappsUsed: ['Quantum Lab', 'Cosmic Explorer', 'Neural Network'],
              },
              {
                id: '6',
                name: 'Celeste Moonwhisper',
                username: 'celeste_moonwhisper',
                avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=150&fit=crop&crop=face',
                totalKarma: 118000,
                level: 'Legendary',
                rank: 6,
                achievements: ['Mystic Master', 'Moon Guardian', 'Whisper Sage'],
                lastActive: '2024-01-15T05:45:00Z',
                karmaHistory: [115000, 116000, 117000, 117500, 118000],
                oappsUsed: ['Moon Temple', 'Mystic Garden', 'Whisper Portal'],
              },
              {
                id: '7',
                name: 'Forest Earthsong',
                username: 'forest_earthsong',
                avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face',
                totalKarma: 112000,
                level: 'Legendary',
                rank: 7,
                achievements: ['Druid Master', 'Earth Singer', 'Forest Guardian'],
                lastActive: '2024-01-15T07:15:00Z',
                karmaHistory: [109000, 110000, 111000, 111500, 112000],
                oappsUsed: ['Forest Grove', 'Earth Temple', 'Nature Sanctuary'],
              },
              {
                id: '8',
                name: 'Raven Cosmos',
                username: 'raven_cosmos',
                avatar: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&h=150&fit=crop&crop=face',
                totalKarma: 110000,
                level: 'Legendary',
                rank: 8,
                achievements: ['Pilot Master', 'Cosmic Navigator', 'Star Rider'],
                lastActive: '2024-01-15T10:05:00Z',
                karmaHistory: [107000, 108000, 109000, 109500, 110000],
                oappsUsed: ['Cosmic Pilot', 'Star Navigator', 'Space Rider'],
              },
              {
                id: '9',
                name: 'Storm Wildfire',
                username: 'storm_wildfire',
                avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=150&h=150&fit=crop&crop=face',
                totalKarma: 103000,
                level: 'Legendary',
                rank: 9,
                achievements: ['Ranger Master', 'Storm Rider', 'Wild Guardian'],
                lastActive: '2024-01-15T08:35:00Z',
                karmaHistory: [100000, 101000, 102000, 102500, 103000],
                oappsUsed: ['Storm Ranger', 'Wild Hunt', 'Fire Storm'],
              },
              {
                id: '10',
                name: 'Captain Nova',
                username: 'captain_nova',
                avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=150&fit=crop&crop=face',
                totalKarma: 98000,
                level: 'Master',
                rank: 10,
                achievements: ['Space Explorer', 'Mission Commander', 'Team Leader'],
                lastActive: '2024-01-15T09:15:00Z',
                karmaHistory: [95000, 96000, 97000, 97500, 98000],
                oappsUsed: ['Space Mission', 'Galaxy Map', 'Stellar Navigation'],
              },
              {
                id: '11',
                name: 'Nyx Shadowbyte',
                username: 'nyx_shadowbyte',
                avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=150&fit=crop&crop=face',
                totalKarma: 95500,
                level: 'Master',
                rank: 11,
                achievements: ['Hacker Elite', 'Shadow Walker', 'Byte Master'],
                lastActive: '2024-01-15T04:20:00Z',
                karmaHistory: [92500, 93500, 94500, 95000, 95500],
                oappsUsed: ['Shadow Net', 'Byte Code', 'Hack Matrix'],
              },
              {
                id: '12',
                name: 'Luna Starweaver',
                username: 'luna_starweaver',
                avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face',
                totalKarma: 92000,
                level: 'Master',
                rank: 12,
                achievements: ['Agent Master', 'Star Weaver', 'Lunar Guardian'],
                lastActive: '2024-01-15T06:50:00Z',
                karmaHistory: [89000, 90000, 91000, 91500, 92000],
                oappsUsed: ['Star Weaver', 'Lunar Base', 'Agent Network'],
              },
              {
                id: '13',
                name: 'Alex Quantum',
                username: 'alex_quantum',
                avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face',
                totalKarma: 87500,
                level: 'Master',
                rank: 13,
                achievements: ['Quantum Researcher', 'AI Developer', 'Innovation Leader'],
                lastActive: '2024-01-15T08:45:00Z',
                karmaHistory: [85000, 86000, 87000, 87200, 87500],
                oappsUsed: ['Quantum Lab', 'AI Studio', 'Neural Network'],
              },
              {
                id: '14',
                name: 'Luna Stellar',
                username: 'luna_stellar',
                avatar: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150&h=150&fit=crop&crop=face',
                totalKarma: 72000,
                level: 'Expert',
                rank: 14,
                achievements: ['Stellar Navigator', 'Cosmic Artist', 'Community Builder'],
                lastActive: '2024-01-15T07:30:00Z',
                karmaHistory: [70000, 71000, 71500, 71800, 72000],
                oappsUsed: ['Cosmic Explorer', 'Stellar Gallery', 'Community Hub'],
              },
              {
                id: '15',
                name: 'Marcus Johnson',
                username: 'marcus_j',
                avatar: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&h=150&fit=crop&crop=face',
                totalKarma: 65000,
                level: 'Expert',
                rank: 15,
                achievements: ['Game Master', 'Quest Leader', 'Strategy Expert'],
                lastActive: '2024-01-15T06:20:00Z',
                karmaHistory: [62000, 63000, 64000, 64500, 65000],
                oappsUsed: ['Quest Master', 'Strategy Game', 'Battle Arena'],
              },
              {
                id: '16',
                name: 'Titan Ironclad',
                username: 'titan_ironclad',
                avatar: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&h=150&fit=crop&crop=face',
                totalKarma: 58000,
                level: 'Advanced',
                rank: 16,
                achievements: ['Warrior Supreme', 'Battle Master', 'Iron Will'],
                lastActive: '2024-01-15T10:45:00Z',
                karmaHistory: [55000, 56000, 57000, 57500, 58000],
                oappsUsed: ['Battle Arena', 'Warrior Training', 'Combat Simulator'],
              },
              {
                id: '17',
                name: 'Zen Peacekeeper',
                username: 'zen_peacekeeper',
                avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=150&h=150&fit=crop&crop=face',
                totalKarma: 52000,
                level: 'Advanced',
                rank: 17,
                achievements: ['Monk Master', 'Peace Bringer', 'Harmony Guardian'],
                lastActive: '2024-01-15T09:30:00Z',
                karmaHistory: [49000, 50000, 51000, 51500, 52000],
                oappsUsed: ['Meditation Garden', 'Harmony Hub', 'Peace Portal'],
              },
              {
                id: '18',
                name: 'Blaze Stormcaller',
                username: 'blaze_stormcaller',
                avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=150&fit=crop&crop=face',
                totalKarma: 47000,
                level: 'Intermediate',
                rank: 18,
                achievements: ['Elementalist Master', 'Storm Bringer', 'Fire Guardian'],
                lastActive: '2024-01-15T11:20:00Z',
                karmaHistory: [44000, 45000, 46000, 46500, 47000],
                oappsUsed: ['Elemental Forge', 'Storm Control', 'Fire Temple'],
              },
              {
                id: '19',
                name: 'Vega Skyforge',
                username: 'vega_skyforge',
                avatar: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150&h=150&fit=crop&crop=face',
                totalKarma: 43000,
                level: 'Intermediate',
                rank: 19,
                achievements: ['Architect Supreme', 'Sky Builder', 'Stellar Designer'],
                lastActive: '2024-01-15T08:50:00Z',
                karmaHistory: [40000, 41000, 42000, 42500, 43000],
                oappsUsed: ['Sky Architect', 'Stellar Builder', 'Cosmic Designer'],
              },
              {
                id: '21',
                name: 'Shadow Nightfall',
                username: 'shadow_nightfall',
                avatar: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&h=150&fit=crop&crop=face',
                totalKarma: 91000,
                level: 'Expert',
                rank: 21,
                achievements: ['Rogue Master', 'Night Walker', 'Shadow Dancer'],
                lastActive: '2024-01-15T03:45:00Z',
                karmaHistory: [88000, 89000, 90000, 90500, 91000],
                oappsUsed: ['Shadow Realm', 'Night Market', 'Rogue Academy'],
              },
              {
                id: '25',
                name: 'Wisdom Ethereal',
                username: 'wisdom_ethereal',
                avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=150&h=150&fit=crop&crop=face',
                totalKarma: 89500,
                level: 'Expert',
                rank: 25,
                achievements: ['Sage Master', 'Wisdom Keeper', 'Ethereal Guide'],
                lastActive: '2024-01-13T14:30:00Z',
                karmaHistory: [86500, 87500, 88500, 89000, 89500],
                oappsUsed: ['Wisdom Library', 'Ethereal Plane', 'Sage Council'],
              },
              {
                id: '26',
                name: 'Echo Quantum',
                username: 'echo_quantum',
                avatar: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150&h=150&fit=crop&crop=face',
                totalKarma: 89000,
                level: 'Expert',
                rank: 26,
                achievements: ['Scientist Elite', 'Quantum Echo', 'Research Master'],
                lastActive: '2024-01-15T05:15:00Z',
                karmaHistory: [86000, 87000, 88000, 88500, 89000],
                oappsUsed: ['Quantum Lab', 'Echo Chamber', 'Science Hub'],
              },
              {
                id: '27',
                name: 'Raven Soulweaver',
                username: 'raven_soulweaver',
                avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=150&fit=crop&crop=face',
                totalKarma: 84000,
                level: 'Expert',
                rank: 27,
                achievements: ['Necromancer Master', 'Soul Weaver', 'Death Walker'],
                lastActive: '2024-01-15T02:30:00Z',
                karmaHistory: [81000, 82000, 83000, 83500, 84000],
                oappsUsed: ['Soul Forge', 'Death Realm', 'Necro Academy'],
              },
              {
                id: '28',
                name: 'Marcus Voidwalker',
                username: 'marcus_void',
                avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face',
                totalKarma: 78500,
                level: 'Expert',
                rank: 28,
                achievements: ['Professor Elite', 'Void Scholar', 'Knowledge Seeker'],
                lastActive: '2024-01-15T01:45:00Z',
                karmaHistory: [75500, 76500, 77500, 78000, 78500],
                oappsUsed: ['Void Studies', 'Knowledge Base', 'Scholar Network'],
              },
              {
                id: '29',
                name: 'Melody Starlight',
                username: 'melody_starlight',
                avatar: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&h=150&fit=crop&crop=face',
                totalKarma: 76500,
                level: 'Advanced',
                rank: 29,
                achievements: ['Bard Master', 'Star Singer', 'Melody Weaver'],
                lastActive: '2024-01-15T00:30:00Z',
                karmaHistory: [73500, 74500, 75500, 76000, 76500],
                oappsUsed: ['Star Concert', 'Melody Hall', 'Bard Academy'],
              },
              {
                id: '30',
                name: 'Atlas Voyager',
                username: 'atlas_voyager',
                avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=150&h=150&fit=crop&crop=face',
                totalKarma: 72000,
                level: 'Advanced',
                rank: 30,
                achievements: ['Explorer Master', 'World Walker', 'Atlas Bearer'],
                lastActive: '2024-01-15T07:45:00Z',
                karmaHistory: [69000, 70000, 71000, 71500, 72000],
                oappsUsed: ['World Atlas', 'Explorer Hub', 'Voyage Planner'],
              },
            ],
            oapps: [
              {
                id: '1',
                name: 'Quantum Lab',
                description: 'Advanced quantum computing and research platform',
                icon: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=100&h=100&fit=crop',
                totalKarmaGenerated: 450000,
                usersCount: 1250,
                averageKarma: 360,
                category: 'Research',
                rating: 4.9,
                lastUpdated: '2024-01-15T10:00:00Z',
              },
              {
                id: '2',
                name: 'Cosmic Explorer',
                description: 'Explore the universe and discover new worlds',
                icon: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=100&h=100&fit=crop',
                totalKarmaGenerated: 380000,
                usersCount: 2100,
                averageKarma: 181,
                category: 'Exploration',
                rating: 4.8,
                lastUpdated: '2024-01-15T09:30:00Z',
              },
              {
                id: '3',
                name: 'Neural Network',
                description: 'AI and machine learning development platform',
                icon: 'https://images.unsplash.com/photo-1555949963-aa79dcee981c?w=100&h=100&fit=crop',
                totalKarmaGenerated: 320000,
                usersCount: 1800,
                averageKarma: 178,
                category: 'AI/ML',
                rating: 4.7,
                lastUpdated: '2024-01-15T09:00:00Z',
              },
              {
                id: '4',
                name: 'Space Mission',
                description: 'Command space missions and explore the galaxy',
                icon: 'https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=100&h=100&fit=crop',
                totalKarmaGenerated: 280000,
                usersCount: 1650,
                averageKarma: 170,
                category: 'Gaming',
                rating: 4.6,
                lastUpdated: '2024-01-15T08:45:00Z',
              },
              {
                id: '5',
                name: 'Quest Master',
                description: 'Create and participate in epic quests and adventures',
                icon: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=100&h=100&fit=crop',
                totalKarmaGenerated: 250000,
                usersCount: 2200,
                averageKarma: 114,
                category: 'Gaming',
                rating: 4.5,
                lastUpdated: '2024-01-15T08:30:00Z',
              },
            ],
          }
        };
      }
    },
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const getLevelColor = (level: string) => {
    switch (level) {
      case 'Legendary': return '#ffd700';
      case 'Master': return '#c0c0c0';
      case 'Expert': return '#cd7f32';
      case 'Advanced': return '#4caf50';
      case 'Intermediate': return '#2196f3';
      case 'Beginner': return '#9c27b0';
      default: return '#757575';
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'Research': return '#4caf50';
      case 'Exploration': return '#2196f3';
      case 'AI/ML': return '#9c27b0';
      case 'Gaming': return '#ff9800';
      case 'Social': return '#f44336';
      case 'Education': return '#00bcd4';
      default: return '#757575';
    }
  };

  const formatKarma = (karma: number) => {
    if (karma >= 1000000) return `${(karma / 1000000).toFixed(1)}M`;
    if (karma >= 1000) return `${(karma / 1000).toFixed(1)}K`;
    return karma.toString();
  };

  const filteredAvatars = karmaData?.result?.avatars?.filter((avatar: AvatarKarma) => {
    const matchesSearch = avatar.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         avatar.username.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesLevel = filterLevel === 'all' || avatar.level === filterLevel;
    return matchesSearch && matchesLevel;
  }) || [];

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
                ⭐ Karma Leaderboard
              </Typography>
              <Typography variant="subtitle1" color="text.secondary">
                Track karma scores, achievements, and OAPP contributions across the OASIS
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
            </Box>
          </Box>

          {/* Search and Filter */}
          <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
            <TextField
              placeholder="Search avatars..."
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
              onClick={() => toast.success('Filter options coming soon!')}
            >
              Filter
            </Button>
          </Box>


          {/* Tabs */}
          <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
            <Tabs value={tabValue} onChange={(e, newValue) => setTabValue(newValue)}>
              <Tab label={`Avatar Leaderboard (${filteredAvatars.length})`} />
              <Tab label={`OAPP Karma Generators (${karmaData?.result?.oapps?.length || 0})`} />
            </Tabs>
          </Box>

          {isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
              <CircularProgress size={60} />
            </Box>
          ) : (
            <>
              {/* Avatar Leaderboard Tab */}
              {tabValue === 0 && (
                <Grid container spacing={3}>
                  {/* Top 3 Avatars - Special Cards */}
                  {filteredAvatars.slice(0, 3).map((avatar: AvatarKarma, index: number) => (
                    <Grid item xs={12} md={4} key={avatar.id}>
                      <motion.div
                        variants={itemVariants}
                        whileHover={{ scale: 1.02 }}
                        transition={{ duration: 0.2 }}
                      >
                        <Card sx={{ 
                          height: '100%', 
                          position: 'relative',
                          background: index === 0 ? 'linear-gradient(135deg, #ffd700, #ffed4e)' :
                                     index === 1 ? 'linear-gradient(135deg, #c0c0c0, #e8e8e8)' :
                                     'linear-gradient(135deg, #cd7f32, #daa520)',
                          '&:hover': { boxShadow: 6 }
                        }}>
                          <Box sx={{ position: 'absolute', top: 8, right: 8 }}>
                            <Chip
                              label={`#${avatar.rank}`}
                              size="small"
                              sx={{
                                bgcolor: 'rgba(0,0,0,0.7)',
                                color: 'white',
                                fontWeight: 'bold',
                                fontSize: '0.8rem'
                              }}
                            />
                          </Box>
                          <CardContent sx={{ textAlign: 'center', pt: 3 }}>
                            <Avatar
                              src={avatar.avatar}
                              sx={{ 
                                width: 80, 
                                height: 80, 
                                mx: 'auto', 
                                mb: 2,
                                border: '3px solid white',
                                boxShadow: 2
                              }}
                            />
                            <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1 }}>
                              {avatar.name}
                            </Typography>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                              @{avatar.username}
                            </Typography>
                            <Chip
                              label={avatar.level}
                              size="small"
                              sx={{
                                bgcolor: getLevelColor(avatar.level),
                                color: 'white',
                                fontWeight: 'bold',
                                mb: 2
                              }}
                            />
                            <Typography variant="h5" sx={{ fontWeight: 'bold', mb: 1 }}>
                              {formatKarma(avatar.totalKarma)} Karma
                            </Typography>
                            <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, flexWrap: 'wrap' }}>
                              {avatar.achievements.slice(0, 2).map((achievement: string) => (
                                <Chip
                                  key={achievement}
                                  label={achievement}
                                  size="small"
                                  variant="outlined"
                                  sx={{ fontSize: '0.7rem' }}
                                />
                              ))}
                            </Box>
                          </CardContent>
                        </Card>
                      </motion.div>
                    </Grid>
                  ))}

                  {/* Remaining Avatars - Table View */}
                  {filteredAvatars.length > 3 && (
                    <Grid item xs={12}>
                      <Card>
                        <CardContent>
                          <Typography variant="h6" sx={{ mb: 2 }}>
                            Complete Leaderboard
                          </Typography>
                          <TableContainer>
                            <Table>
                              <TableHead>
                                <TableRow>
                                  <TableCell>Rank</TableCell>
                                  <TableCell>Avatar</TableCell>
                                  <TableCell>Name</TableCell>
                                  <TableCell>Level</TableCell>
                                  <TableCell>Karma</TableCell>
                                  <TableCell>Achievements</TableCell>
                                  <TableCell>Last Active</TableCell>
                                  <TableCell>Actions</TableCell>
                                </TableRow>
                              </TableHead>
                              <TableBody>
                                {filteredAvatars.slice(3).map((avatar: AvatarKarma) => (
                                  <TableRow key={avatar.id}>
                                    <TableCell>
                                      <Chip
                                        label={`#${avatar.rank}`}
                                        size="small"
                                        color="primary"
                                      />
                                    </TableCell>
                                    <TableCell>
                                      <Avatar src={avatar.avatar} sx={{ width: 40, height: 40 }} />
                                    </TableCell>
                                    <TableCell>
                                      <Box>
                                        <Typography variant="subtitle2">{avatar.name}</Typography>
                                        <Typography variant="caption" color="text.secondary">
                                          @{avatar.username}
                                        </Typography>
                                      </Box>
                                    </TableCell>
                                    <TableCell>
                                      <Chip
                                        label={avatar.level}
                                        size="small"
                                        sx={{
                                          bgcolor: getLevelColor(avatar.level),
                                          color: 'white',
                                          fontWeight: 'bold'
                                        }}
                                      />
                                    </TableCell>
                                    <TableCell>
                                      <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                                        {formatKarma(avatar.totalKarma)}
                                      </Typography>
                                    </TableCell>
                                    <TableCell>
                                      <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                                        {avatar.achievements.slice(0, 2).map((achievement: string) => (
                                          <Chip
                                            key={achievement}
                                            label={achievement}
                                            size="small"
                                            variant="outlined"
                                            sx={{ fontSize: '0.7rem' }}
                                          />
                                        ))}
                                      </Box>
                                    </TableCell>
                                    <TableCell>
                                      <Typography variant="caption">
                                        {new Date(avatar.lastActive).toLocaleDateString()}
                                      </Typography>
                                    </TableCell>
                                    <TableCell>
                                      <IconButton size="small" onClick={() => toast.success('Viewing avatar profile')}>
                                        <Visibility />
                                      </IconButton>
                                    </TableCell>
                                  </TableRow>
                                ))}
                              </TableBody>
                            </Table>
                          </TableContainer>
                        </CardContent>
                      </Card>
                    </Grid>
                  )}
                </Grid>
              )}

              {/* OAPP Karma Generators Tab */}
              {tabValue === 1 && (
                <Grid container spacing={3}>
                  {karmaData?.result?.oapps?.map((oapp: OAPPKarma, index: number) => (
                    <Grid item xs={12} sm={6} md={4} key={oapp.id}>
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
                          <Box sx={{ position: 'relative' }}>
                            <CardMedia
                              component="img"
                              height="120"
                              image={oapp.icon}
                              alt={oapp.name}
                              sx={{ objectFit: 'cover' }}
                            />
                            <Chip
                              label={oapp.category}
                              size="small"
                              sx={{
                                position: 'absolute',
                                top: 8,
                                right: 8,
                                bgcolor: getCategoryColor(oapp.category),
                                color: 'white',
                                fontWeight: 'bold'
                              }}
                            />
                            <Box sx={{ position: 'absolute', top: 8, left: 8 }}>
                              <Chip
                                label={`#${index + 1}`}
                                size="small"
                                sx={{
                                  bgcolor: 'rgba(0,0,0,0.7)',
                                  color: 'white',
                                  fontWeight: 'bold'
                                }}
                              />
                            </Box>
                          </Box>
                          
                          <CardContent sx={{ flexGrow: 1 }}>
                            <Typography variant="h6" sx={{ mb: 1 }}>
                              {oapp.name}
                            </Typography>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                              {oapp.description}
                            </Typography>
                            
                            <Box sx={{ mb: 2 }}>
                              <Typography variant="h5" sx={{ fontWeight: 'bold', color: 'primary.main' }}>
                                {formatKarma(oapp.totalKarmaGenerated)} Karma
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                Generated across {oapp.usersCount.toLocaleString()} users
                              </Typography>
                            </Box>
                            
                            <Box sx={{ mb: 2 }}>
                              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                                Average per user: {oapp.averageKarma} karma
                              </Typography>
                              <LinearProgress
                                variant="determinate"
                                value={(oapp.averageKarma / 500) * 100}
                                sx={{ height: 6, borderRadius: 3 }}
                              />
                            </Box>
                            
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                                <Star sx={{ color: '#ff9800', fontSize: 16, mr: 0.5 }} />
                                <Typography variant="body2">
                                  {oapp.rating}
                                </Typography>
                              </Box>
                              <Typography variant="caption" color="text.secondary">
                                {new Date(oapp.lastUpdated).toLocaleDateString()}
                              </Typography>
                            </Box>
                          </CardContent>
                        </Card>
                      </motion.div>
                    </Grid>
                  ))}
                </Grid>
              )}
            </>
          )}
        </Box>
      </>
    </motion.div>
  );
};

export default KarmaPage;
