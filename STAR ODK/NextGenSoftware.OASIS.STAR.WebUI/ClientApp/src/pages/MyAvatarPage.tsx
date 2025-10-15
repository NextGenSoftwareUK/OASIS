import React, { useState } from 'react';
import {
  Box, Typography, Card, CardContent, Grid, Avatar, Chip, Button,
  IconButton, Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, CircularProgress, Alert, Divider, List, ListItem,
  ListItemIcon, ListItemText, Paper, LinearProgress, Switch,
  Tooltip, Badge, Stack, Accordion, AccordionSummary, AccordionDetails,
  Menu, MenuItem, Tabs, Tab, FormControl, InputLabel,
  Select, Slider, Rating, CardActions, CardMedia, Fab
} from '@mui/material';
import {
  Person, Edit, Star, Security, Timeline, Settings, Public,
  Lock, Verified, Computer, Phone, Tablet, Gamepad, Web, Cloud,
  CheckCircle, Warning, Error, MoreVert, Visibility, VisibilityOff,
  Refresh, Add, Delete, Send, ImportExport, QrCode2, AccountBalance,
  CurrencyExchange, History, AccountBalanceWallet, ExpandMore,
  Inventory, Shield, Diamond, Map, LocationOn,
  ArtTrack, Image, VideoLibrary, MusicNote, Description,
  SportsEsports, School, Work, Home, DirectionsRun, Flight,
  LocalShipping, Build, Science, Psychology, Biotech, AutoAwesome,
  EmojiEvents, AttachMoney, ShoppingCart, Store, Redeem
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { avatarService, questService, inventoryService, nftService, geoNftService, walletService } from '../services';

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
      id={`simple-tabpanel-${index}`}
      aria-labelledby={`simple-tab-${index}`}
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

// Types
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
  avatarType?: string;
  profilePicture2D?: string;
  profilePicture3D?: string;
}

interface Quest {
  id: string;
  title: string;
  description: string;
  status: 'active' | 'completed' | 'available' | 'locked';
  progress: number;
  maxProgress: number;
  rewards: string[];
  difficulty: 'easy' | 'medium' | 'hard' | 'legendary';
  questType: 'main' | 'side' | 'daily' | 'weekly';
  missionId?: string;
  chapterId?: string;
  subQuests?: Quest[];
}

interface Mission {
  id: string;
  title: string;
  description: string;
  status: 'active' | 'completed' | 'available' | 'locked';
  progress: number;
  maxProgress: number;
  quests: Quest[];
  chapterId?: string;
}

interface Chapter {
  id: string;
  title: string;
  description: string;
  status: 'active' | 'completed' | 'available' | 'locked';
  progress: number;
  maxProgress: number;
  missions: Mission[];
}

interface Equipment {
  id: string;
  name: string;
  type: 'weapon' | 'armor' | 'accessory' | 'tool';
  slot: 'head' | 'chest' | 'legs' | 'feet' | 'hands' | 'weapon' | 'shield' | 'accessory';
  rarity: 'common' | 'uncommon' | 'rare' | 'epic' | 'legendary';
  level: number;
  stats: {
    attack?: number;
    defense?: number;
    health?: number;
    mana?: number;
    speed?: number;
  };
  imageUrl?: string;
  description?: string;
}

interface InventoryItem {
  id: string;
  name: string;
  type: 'currency' | 'consumable' | 'material' | 'tool' | 'misc';
  quantity: number;
  maxQuantity: number;
  value: number;
  description?: string;
  imageUrl?: string;
  rarity?: 'common' | 'uncommon' | 'rare' | 'epic' | 'legendary';
}

interface NFT {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  tokenId: string;
  contractAddress: string;
  chain: string;
  rarity: 'common' | 'uncommon' | 'rare' | 'epic' | 'legendary';
  attributes: { trait_type: string; value: string }[];
  value: number;
  lastSalePrice?: number;
}

interface GeoNFT {
  id: string;
  name: string;
  description: string;
  imageUrl: string;
  latitude: number;
  longitude: number;
  location: string;
  rarity: 'common' | 'uncommon' | 'rare' | 'epic' | 'legendary';
  attributes: { trait_type: string; value: string }[];
  value: number;
  discoveredDate: string;
}

const MyAvatarPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [equipmentDialogOpen, setEquipmentDialogOpen] = useState(false);
  const [inventoryDialogOpen, setInventoryDialogOpen] = useState(false);
  const [nftDialogOpen, setNftDialogOpen] = useState(false);
  const [geoNftDialogOpen, setGeoNftDialogOpen] = useState(false);
  const [selectedItem, setSelectedItem] = useState<any>(null);
  const [avatar, setAvatar] = useState<Avatar | null>(null);
  const [editData, setEditData] = useState({
    title: '',
    firstName: '',
    lastName: '',
    email: '',
    username: '',
    profilePicture2D: '',
    profilePicture3D: '',
  });

  const queryClient = useQueryClient();

  // Fetch current avatar
  const { data: avatarData, isLoading: avatarLoading, error: avatarError, refetch: refetchAvatar } = useQuery(
    ['current-avatar'],
    async () => {
      const response = await avatarService.getCurrentAvatar();
      return response;
    },
    {
      onSuccess: (data) => {
        if (data?.result) {
          setAvatar(data.result as Avatar);
          setEditData({
            title: data.result.title || '',
            firstName: data.result.firstName || '',
            lastName: data.result.lastName || '',
            email: data.result.email || '',
            username: data.result.username || '',
            profilePicture2D: (data.result as any)?.profilePicture2D || '',
            profilePicture3D: (data.result as any)?.profilePicture3D || '',
          });
        }
      },
    }
  );

  // Fetch avatar wallets
  const { data: walletsData, isLoading: walletsLoading } = useQuery(
    ['avatar-wallets', avatar?.id],
    async () => {
      if (!avatar?.id) return { result: [] };
      const response = await walletService.getForAvatar(avatar.id);
      return response;
    },
    {
      enabled: !!avatar?.id,
    }
  );

  // Fetch portfolio value
  const { data: portfolioData } = useQuery(
    ['portfolio-value'],
    async () => {
      const response = await walletService.getPortfolioValue();
      return response;
    }
  );

  // Demo data for RPG elements
  const demoQuests: Quest[] = [
    {
      id: 'quest-1',
      title: 'The Quantum Awakening',
      description: 'Master the fundamentals of quantum computing',
      status: 'active',
      progress: 3,
      maxProgress: 5,
      rewards: ['100 XP', 'Quantum Crystal', '50 Karma'],
      difficulty: 'medium',
      questType: 'main',
      missionId: 'mission-1',
      chapterId: 'chapter-1',
      subQuests: [
        { id: 'sub-1', title: 'Install Quantum SDK', description: 'Download and install the quantum development kit', status: 'completed', progress: 1, maxProgress: 1, rewards: ['25 XP'], difficulty: 'easy', questType: 'main' },
        { id: 'sub-2', title: 'Build First Quantum Circuit', description: 'Create your first quantum circuit', status: 'active', progress: 0, maxProgress: 1, rewards: ['50 XP'], difficulty: 'medium', questType: 'main' },
      ]
    },
    {
      id: 'quest-2',
      title: 'Daily Meditation',
      description: 'Complete your daily meditation practice',
      status: 'active',
      progress: 1,
      maxProgress: 1,
      rewards: ['25 XP', '10 Karma'],
      difficulty: 'easy',
      questType: 'daily'
    }
  ];

  const demoMissions: Mission[] = [
    {
      id: 'mission-1',
      title: 'Quantum Mastery',
      description: 'Become a master of quantum technologies',
      status: 'active',
      progress: 2,
      maxProgress: 4,
      quests: demoQuests,
      chapterId: 'chapter-1'
    }
  ];

  const demoChapters: Chapter[] = [
    {
      id: 'chapter-1',
      title: 'The Foundation',
      description: 'Learn the basics of OASIS development',
      status: 'active',
      progress: 1,
      maxProgress: 3,
      missions: demoMissions
    }
  ];

  const demoEquipment: Equipment[] = [
    {
      id: 'equip-1',
      name: 'Quantum Sword',
      type: 'weapon',
      slot: 'weapon',
      rarity: 'epic',
      level: 15,
      stats: { attack: 150, speed: 10 },
      imageUrl: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=200&h=200&fit=crop',
      description: 'A blade forged from quantum particles'
    },
    {
      id: 'equip-2',
      name: 'Neural Armor',
      type: 'armor',
      slot: 'chest',
      rarity: 'rare',
      level: 12,
      stats: { defense: 100, health: 50 },
      imageUrl: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=200&h=200&fit=crop',
      description: 'Armor enhanced with neural networks'
    }
  ];

  const demoInventory: InventoryItem[] = [
    {
      id: 'inv-1',
      name: 'Gold Coins',
      type: 'currency',
      quantity: 2500,
      maxQuantity: 999999,
      value: 1,
      imageUrl: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=100&h=100&fit=crop'
    },
    {
      id: 'inv-2',
      name: 'Healing Potion',
      type: 'consumable',
      quantity: 15,
      maxQuantity: 99,
      value: 50,
      description: 'Restores 100 HP',
      imageUrl: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=100&h=100&fit=crop',
      rarity: 'common'
    },
    {
      id: 'inv-3',
      name: 'Quantum Crystal',
      type: 'material',
      quantity: 3,
      maxQuantity: 50,
      value: 200,
      description: 'Rare material for crafting',
      imageUrl: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=100&h=100&fit=crop',
      rarity: 'rare'
    }
  ];

  const demoNFTs: NFT[] = [
    {
      id: 'nft-1',
      name: 'OASIS Genesis Avatar',
      description: 'The first avatar created in the OASIS metaverse',
      imageUrl: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=300&h=300&fit=crop',
      tokenId: '1',
      contractAddress: '0x123...',
      chain: 'Ethereum',
      rarity: 'legendary',
      attributes: [
        { trait_type: 'Background', value: 'Quantum' },
        { trait_type: 'Eyes', value: 'Cosmic' },
        { trait_type: 'Rarity', value: 'Legendary' }
      ],
      value: 10000,
      lastSalePrice: 8500
    }
  ];

  const demoGeoNFTs: GeoNFT[] = [
    {
      id: 'geonft-1',
      name: 'San Francisco Quantum Node',
      description: 'A quantum computing node located in San Francisco',
      imageUrl: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=300&h=300&fit=crop',
      latitude: 37.7749,
      longitude: -122.4194,
      location: 'San Francisco, CA',
      rarity: 'epic',
      attributes: [
        { trait_type: 'Location', value: 'San Francisco' },
        { trait_type: 'Type', value: 'Quantum Node' },
        { trait_type: 'Power Level', value: 'High' }
      ],
      value: 5000,
      discoveredDate: '2024-01-15T10:30:00Z'
    }
  ];

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const getRarityColor = (rarity: string) => {
    switch (rarity) {
      case 'common': return '#9e9e9e';
      case 'uncommon': return '#4caf50';
      case 'rare': return '#2196f3';
      case 'epic': return '#9c27b0';
      case 'legendary': return '#ff9800';
      default: return '#9e9e9e';
    }
  };

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty) {
      case 'easy': return '#4caf50';
      case 'medium': return '#ff9800';
      case 'hard': return '#f44336';
      case 'legendary': return '#9c27b0';
      default: return '#9e9e9e';
    }
  };

  if (avatarLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <CircularProgress size={60} />
      </Box>
    );
  }

  if (avatarError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load avatar data
        </Alert>
        <Button variant="contained" onClick={() => refetchAvatar()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!avatar) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          No avatar found. Please create an avatar first.
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        <Typography variant="h4" component="h1" gutterBottom sx={{ color: 'primary.main', fontWeight: 'bold' }}>
          <Person sx={{ mr: 2, fontSize: 'inherit' }} />
          My Avatar
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
          Manage your avatar, track your progress, and view your RPG character details.
        </Typography>

        {/* Avatar Profile Card */}
        <Card sx={{ mb: 4, background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', color: 'white' }}>
          <CardContent sx={{ p: 4 }}>
            <Grid container spacing={3} alignItems="center">
              <Grid item xs={12} md={3}>
                <Box sx={{ textAlign: 'center' }}>
                  <Avatar
                    src={avatar.profilePicture2D}
                    sx={{
                      width: 120,
                      height: 120,
                      mx: 'auto',
                      mb: 2,
                      bgcolor: 'rgba(255,255,255,0.2)',
                      fontSize: '3rem',
                      border: '3px solid rgba(255,255,255,0.3)'
                    }}
                  >
                    {avatar.firstName.charAt(0)}{avatar.lastName.charAt(0)}
                  </Avatar>
                  <Button
                    variant="outlined"
                    startIcon={<Edit />}
                    onClick={() => setEditDialogOpen(true)}
                    sx={{ 
                      color: 'white', 
                      borderColor: 'rgba(255,255,255,0.5)',
                      '&:hover': { borderColor: 'white', backgroundColor: 'rgba(255,255,255,0.1)' }
                    }}
                  >
                    Edit Profile
                  </Button>
                </Box>
              </Grid>
              <Grid item xs={12} md={9}>
                <Typography variant="h4" gutterBottom>
                  {avatar.title} {avatar.firstName} {avatar.lastName}
                </Typography>
                <Typography variant="h6" sx={{ mb: 2, opacity: 0.9 }}>
                  @{avatar.username}
                </Typography>
                <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
                  <Chip
                    label={avatar.isBeamedIn ? 'Online' : 'Offline'}
                    color={avatar.isBeamedIn ? 'success' : 'default'}
                    sx={{ bgcolor: 'rgba(255,255,255,0.2)', color: 'white' }}
                  />
                  <Chip
                    label={`Level ${avatar.level || 1}`}
                    sx={{ bgcolor: 'rgba(255,255,255,0.2)', color: 'white' }}
                  />
                  <Chip
                    label={`${avatar.karma?.toLocaleString() || 0} Karma`}
                    sx={{ bgcolor: 'rgba(255,255,255,0.2)', color: 'white' }}
                  />
                </Stack>
                <Box sx={{ display: 'flex', gap: 4 }}>
                  <Box>
                    <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
                      {avatar.karma?.toLocaleString() || 0}
                    </Typography>
                    <Typography variant="body2" sx={{ opacity: 0.8 }}>
                      Karma Points
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
                      {avatar.level || 1}
                    </Typography>
                    <Typography variant="body2" sx={{ opacity: 0.8 }}>
                      Level
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
                      {avatar.xp?.toLocaleString() || 0}
                    </Typography>
                    <Typography variant="body2" sx={{ opacity: 0.8 }}>
                      Experience
                    </Typography>
                  </Box>
                </Box>
              </Grid>
            </Grid>
          </CardContent>
        </Card>

        {/* Wallet Section */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5, delay: 0.2 }}
        >
          <Card sx={{ mb: 4 }}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
                <Typography variant="h5" component="h2" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <AccountBalanceWallet color="primary" />
                  OASIS Universal Wallet
                </Typography>
                <Button
                  variant="outlined"
                  startIcon={<Add />}
                  onClick={() => window.location.href = '/wallets'}
                >
                  Manage Wallets
                </Button>
              </Box>

              {/* Portfolio Overview */}
              {portfolioData?.result && (
                <Box sx={{ mb: 3, p: 2, bgcolor: 'grey.50', borderRadius: 2 }}>
                  <Typography variant="h6" gutterBottom>
                    Portfolio Value
                  </Typography>
                  <Typography variant="h4" color="primary" sx={{ fontWeight: 'bold', mb: 1 }}>
                    ${portfolioData.result.totalValueUSD?.toLocaleString() || '0.00'}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Last updated: {new Date(portfolioData.result.lastUpdated).toLocaleDateString()}
                  </Typography>
                  
                  {/* Portfolio Breakdown by Chain */}
                  {portfolioData.result.breakdown && (
                    <Grid container spacing={1}>
                      {Object.entries(portfolioData.result.breakdown).map(([chain, data]: [string, any]) => (
                        <Grid item xs={6} sm={3} key={chain}>
                          <Box sx={{ textAlign: 'center', p: 1, bgcolor: 'white', borderRadius: 1 }}>
                            <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'capitalize' }}>
                              {chain}
                            </Typography>
                            <Typography variant="body2" fontWeight="bold">
                              ${data.usdValue?.toLocaleString() || '0.00'}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {data.count} wallet{data.count !== 1 ? 's' : ''}
                            </Typography>
                          </Box>
                        </Grid>
                      ))}
                    </Grid>
                  )}
                </Box>
              )}

              {/* Wallets Grid */}
              {walletsLoading ? (
                <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
                  <CircularProgress />
                </Box>
              ) : (
                <Grid container spacing={2}>
                  {walletsData?.result?.slice(0, 4).map((wallet: any) => (
                    <Grid item xs={12} sm={6} md={3} key={wallet.id}>
                      <Card variant="outlined" sx={{ height: '100%' }}>
                        <CardContent>
                          <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                            <Avatar sx={{ bgcolor: 'primary.main', mr: 2, width: 32, height: 32 }}>
                              <AccountBalanceWallet />
                            </Avatar>
                            <Box sx={{ flexGrow: 1 }}>
                              <Typography variant="subtitle2" noWrap>
                                {wallet.name}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {wallet.type}
                              </Typography>
                            </Box>
                          </Box>
                          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                            {wallet.address?.slice(0, 6)}...{wallet.address?.slice(-4)}
                          </Typography>
                          <Typography variant="h6" color="primary">
                            {wallet.balance}
                          </Typography>
                        </CardContent>
                      </Card>
                    </Grid>
                  ))}
                  {(!walletsData?.result || walletsData.result.length === 0) && (
                    <Grid item xs={12}>
                      <Box sx={{ textAlign: 'center', py: 4 }}>
                        <AccountBalanceWallet sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
                        <Typography variant="h6" color="text.secondary" gutterBottom>
                          No Wallets Found
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                          Create your first wallet to start managing your digital assets
                        </Typography>
                        <Button
                          variant="contained"
                          startIcon={<Add />}
                          onClick={() => window.location.href = '/wallets'}
                        >
                          Create Wallet
                        </Button>
                      </Box>
                    </Grid>
                  )}
                </Grid>
              )}
            </CardContent>
          </Card>
        </motion.div>

        {/* Tabs Navigation */}
        <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
          <Tabs value={activeTab} onChange={handleTabChange} variant="scrollable" scrollButtons="auto">
            <Tab label="Quests & Missions" icon={<SportsEsports />} />
            <Tab label="Equipment" icon={<Shield />} />
            <Tab label="Inventory" icon={<Inventory />} />
            <Tab label="NFTs" icon={<ArtTrack />} />
            <Tab label="GeoNFTs" icon={<Map />} />
            <Tab label="Sessions" icon={<Security />} />
          </Tabs>
        </Box>

        {/* Tab Content */}
        {activeTab === 0 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            {/* Chapters */}
            <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
              <School sx={{ mr: 1, verticalAlign: 'middle' }} />
              Chapters
            </Typography>
            <Grid container spacing={3} sx={{ mb: 4 }}>
              {demoChapters.map((chapter) => (
                <Grid item xs={12} md={6} key={chapter.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="h6">{chapter.title}</Typography>
                        <Chip
                          label={chapter.status}
                          color={chapter.status === 'active' ? 'primary' : chapter.status === 'completed' ? 'success' : 'default'}
                          size="small"
                        />
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {chapter.description}
                      </Typography>
                      <Box sx={{ mb: 2 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                          <Typography variant="body2">Progress</Typography>
                          <Typography variant="body2">{chapter.progress}/{chapter.maxProgress}</Typography>
                        </Box>
                        <LinearProgress
                          variant="determinate"
                          value={(chapter.progress / chapter.maxProgress) * 100}
                          sx={{ height: 8, borderRadius: 4 }}
                        />
                      </Box>
                      <Typography variant="body2" color="text.secondary">
                        {chapter.missions.length} Mission(s)
                      </Typography>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>

            {/* Missions */}
            <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
              <Work sx={{ mr: 1, verticalAlign: 'middle' }} />
              Missions
            </Typography>
            <Grid container spacing={3} sx={{ mb: 4 }}>
              {demoMissions.map((mission) => (
                <Grid item xs={12} md={6} key={mission.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="h6">{mission.title}</Typography>
                        <Chip
                          label={mission.status}
                          color={mission.status === 'active' ? 'primary' : mission.status === 'completed' ? 'success' : 'default'}
                          size="small"
                        />
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {mission.description}
                      </Typography>
                      <Box sx={{ mb: 2 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                          <Typography variant="body2">Progress</Typography>
                          <Typography variant="body2">{mission.progress}/{mission.maxProgress}</Typography>
                        </Box>
                        <LinearProgress
                          variant="determinate"
                          value={(mission.progress / mission.maxProgress) * 100}
                          sx={{ height: 8, borderRadius: 4 }}
                        />
                      </Box>
                      <Typography variant="body2" color="text.secondary">
                        {mission.quests.length} Quest(s)
                      </Typography>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>

            {/* Quests */}
            <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
              <DirectionsRun sx={{ mr: 1, verticalAlign: 'middle' }} />
              Active Quests
            </Typography>
            <Grid container spacing={3}>
              {demoQuests.map((quest) => (
                <Grid item xs={12} md={6} key={quest.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="h6">{quest.title}</Typography>
                        <Box sx={{ display: 'flex', gap: 1 }}>
                          <Chip
                            label={quest.difficulty}
                            size="small"
                            sx={{ bgcolor: getDifficultyColor(quest.difficulty), color: 'white' }}
                          />
                          <Chip
                            label={quest.questType}
                            size="small"
                            color="secondary"
                          />
                        </Box>
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {quest.description}
                      </Typography>
                      <Box sx={{ mb: 2 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                          <Typography variant="body2">Progress</Typography>
                          <Typography variant="body2">{quest.progress}/{quest.maxProgress}</Typography>
                        </Box>
                        <LinearProgress
                          variant="determinate"
                          value={(quest.progress / quest.maxProgress) * 100}
                          sx={{ height: 8, borderRadius: 4 }}
                        />
                      </Box>
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" gutterBottom>
                          Rewards:
                        </Typography>
                        <Stack direction="row" spacing={1} flexWrap="wrap">
                          {quest.rewards.map((reward, index) => (
                            <Chip
                              key={index}
                              label={reward}
                              size="small"
                              color="primary"
                              variant="outlined"
                            />
                          ))}
                        </Stack>
                      </Box>
                      {quest.subQuests && quest.subQuests.length > 0 && (
                        <Accordion>
                          <AccordionSummary expandIcon={<ExpandMore />}>
                            <Typography variant="body2">
                              Sub-Quests ({quest.subQuests.length})
                            </Typography>
                          </AccordionSummary>
                          <AccordionDetails>
                            <List dense>
                              {quest.subQuests.map((subQuest) => (
                                <ListItem key={subQuest.id}>
                                  <ListItemIcon>
                                    <CheckCircle color={subQuest.status === 'completed' ? 'success' : 'disabled'} />
                                  </ListItemIcon>
                                  <ListItemText
                                    primary={subQuest.title}
                                    secondary={subQuest.description}
                                  />
                                </ListItem>
                              ))}
                            </List>
                          </AccordionDetails>
                        </Accordion>
                      )}
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </motion.div>
        )}

        {activeTab === 1 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
              <Typography variant="h5">
                <Shield sx={{ mr: 1, verticalAlign: 'middle' }} />
                Equipment
              </Typography>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={() => setEquipmentDialogOpen(true)}
              >
                Manage Equipment
              </Button>
            </Box>
            <Grid container spacing={3}>
              {demoEquipment.map((item) => (
                <Grid item xs={12} sm={6} md={4} key={item.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardMedia
                      component="img"
                      height="200"
                      image={item.imageUrl}
                      alt={item.name}
                      sx={{ objectFit: 'cover' }}
                    />
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                        <Typography variant="h6">{item.name}</Typography>
                        <Chip
                          label={item.rarity}
                          size="small"
                          sx={{ bgcolor: getRarityColor(item.rarity), color: 'white' }}
                        />
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {item.description}
                      </Typography>
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" gutterBottom>
                          Stats:
                        </Typography>
                        <Grid container spacing={1}>
                          {Object.entries(item.stats).map(([stat, value]) => (
                            <Grid item xs={6} key={stat}>
                              <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                <Typography variant="caption" sx={{ textTransform: 'capitalize' }}>
                                  {stat}:
                                </Typography>
                                <Typography variant="caption" sx={{ fontWeight: 'bold' }}>
                                  +{value}
                                </Typography>
                              </Box>
                            </Grid>
                          ))}
                        </Grid>
                      </Box>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Chip
                          label={`Level ${item.level}`}
                          size="small"
                          color="primary"
                          variant="outlined"
                        />
                        <Chip
                          label={item.slot}
                          size="small"
                          color="secondary"
                          variant="outlined"
                        />
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
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
              <Typography variant="h5">
                <Inventory sx={{ mr: 1, verticalAlign: 'middle' }} />
                Inventory
              </Typography>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={() => setInventoryDialogOpen(true)}
              >
                Manage Inventory
              </Button>
            </Box>
            <Grid container spacing={3}>
              {demoInventory.map((item) => (
                <Grid item xs={12} sm={6} md={4} key={item.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardContent>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                        <Avatar
                          src={item.imageUrl}
                          sx={{ width: 50, height: 50, mr: 2 }}
                        />
                        <Box sx={{ flexGrow: 1 }}>
                          <Typography variant="h6">{item.name}</Typography>
                          <Typography variant="body2" color="text.secondary">
                            {item.type.charAt(0).toUpperCase() + item.type.slice(1)}
                          </Typography>
                        </Box>
                        {item.rarity && (
                          <Chip
                            label={item.rarity}
                            size="small"
                            sx={{ bgcolor: getRarityColor(item.rarity), color: 'white' }}
                          />
                        )}
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {item.description}
                      </Typography>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="h6" color="primary">
                          {item.quantity.toLocaleString()}
                          {item.maxQuantity !== 999999 && `/${item.maxQuantity}`}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Value: {item.value.toLocaleString()} gold
                        </Typography>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </motion.div>
        )}

        {activeTab === 3 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
              <Typography variant="h5">
                <ArtTrack sx={{ mr: 1, verticalAlign: 'middle' }} />
                NFTs
              </Typography>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={() => setNftDialogOpen(true)}
              >
                View All NFTs
              </Button>
            </Box>
            <Grid container spacing={3}>
              {demoNFTs.map((nft) => (
                <Grid item xs={12} sm={6} md={4} key={nft.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardMedia
                      component="img"
                      height="200"
                      image={nft.imageUrl}
                      alt={nft.name}
                      sx={{ objectFit: 'cover' }}
                    />
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                        <Typography variant="h6">{nft.name}</Typography>
                        <Chip
                          label={nft.rarity}
                          size="small"
                          sx={{ bgcolor: getRarityColor(nft.rarity), color: 'white' }}
                        />
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {nft.description}
                      </Typography>
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" gutterBottom>
                          Attributes:
                        </Typography>
                        <Stack direction="row" spacing={1} flexWrap="wrap">
                          {nft.attributes.slice(0, 3).map((attr, index) => (
                            <Chip
                              key={index}
                              label={`${attr.trait_type}: ${attr.value}`}
                              size="small"
                              variant="outlined"
                            />
                          ))}
                        </Stack>
                      </Box>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="body2" color="text.secondary">
                          {nft.chain}
                        </Typography>
                        <Typography variant="h6" color="primary">
                          ${nft.value.toLocaleString()}
                        </Typography>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </motion.div>
        )}

        {activeTab === 4 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
              <Typography variant="h5">
                <Map sx={{ mr: 1, verticalAlign: 'middle' }} />
                GeoNFTs
              </Typography>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={() => setGeoNftDialogOpen(true)}
              >
                View All GeoNFTs
              </Button>
            </Box>
            <Grid container spacing={3}>
              {demoGeoNFTs.map((geoNft) => (
                <Grid item xs={12} sm={6} md={4} key={geoNft.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardMedia
                      component="img"
                      height="200"
                      image={geoNft.imageUrl}
                      alt={geoNft.name}
                      sx={{ objectFit: 'cover' }}
                    />
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                        <Typography variant="h6">{geoNft.name}</Typography>
                        <Chip
                          label={geoNft.rarity}
                          size="small"
                          sx={{ bgcolor: getRarityColor(geoNft.rarity), color: 'white' }}
                        />
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {geoNft.description}
                      </Typography>
                      <Box sx={{ mb: 2 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                          <LocationOn sx={{ fontSize: 16, mr: 1, color: 'text.secondary' }} />
                          <Typography variant="body2" color="text.secondary">
                            {geoNft.location}
                          </Typography>
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                          Discovered: {new Date(geoNft.discoveredDate).toLocaleDateString()}
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="body2" color="text.secondary">
                          Coordinates: {geoNft.latitude.toFixed(4)}, {geoNft.longitude.toFixed(4)}
                        </Typography>
                        <Typography variant="h6" color="primary">
                          ${geoNft.value.toLocaleString()}
                        </Typography>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </motion.div>
        )}

        {activeTab === 5 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
          >
            <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
              <Security sx={{ mr: 1, verticalAlign: 'middle' }} />
              Session Management
            </Typography>
            <Alert severity="info" sx={{ mb: 3 }}>
              Session management is available in the Avatar Details page. Click the button below to access it.
            </Alert>
            <Button
              variant="contained"
              startIcon={<Security />}
              onClick={() => window.open('/avatars', '_blank')}
            >
              Open Session Management
            </Button>
          </motion.div>
        )}

        {/* Edit Profile Dialog */}
        <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} maxWidth="md" fullWidth>
          <DialogTitle>Edit Avatar Profile</DialogTitle>
          <DialogContent>
            <Grid container spacing={3} sx={{ mt: 1 }}>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Title"
                  value={editData.title}
                  onChange={(e) => setEditData({ ...editData, title: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Username"
                  value={editData.username}
                  onChange={(e) => setEditData({ ...editData, username: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="First Name"
                  value={editData.firstName}
                  onChange={(e) => setEditData({ ...editData, firstName: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
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
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="2D Profile Picture URL"
                  value={editData.profilePicture2D}
                  onChange={(e) => setEditData({ ...editData, profilePicture2D: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="3D Profile Picture URL"
                  value={editData.profilePicture3D}
                  onChange={(e) => setEditData({ ...editData, profilePicture3D: e.target.value })}
                />
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setEditDialogOpen(false)}>Cancel</Button>
            <Button variant="contained" color="primary">
              Save Changes
            </Button>
          </DialogActions>
        </Dialog>

        {/* Equipment Management Dialog */}
        <Dialog open={equipmentDialogOpen} onClose={() => setEquipmentDialogOpen(false)} maxWidth="lg" fullWidth>
          <DialogTitle>Equipment Management</DialogTitle>
          <DialogContent>
            <Typography variant="body1" sx={{ mb: 3 }}>
              Manage your avatar's equipment. Drag and drop items to equip them.
            </Typography>
            <Grid container spacing={3}>
              {/* Equipment Slots */}
              <Grid item xs={12} md={6}>
                <Typography variant="h6" gutterBottom>Equipment Slots</Typography>
                <Grid container spacing={2}>
                  {['head', 'chest', 'legs', 'feet', 'hands', 'weapon', 'shield', 'accessory'].map((slot) => (
                    <Grid item xs={6} key={slot}>
                      <Card sx={{ minHeight: 100, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <Box sx={{ textAlign: 'center' }}>
                          <Typography variant="body2" sx={{ textTransform: 'capitalize' }}>
                            {slot}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            Empty
                          </Typography>
                        </Box>
                      </Card>
                    </Grid>
                  ))}
                </Grid>
              </Grid>
              {/* Available Equipment */}
              <Grid item xs={12} md={6}>
                <Typography variant="h6" gutterBottom>Available Equipment</Typography>
                <Grid container spacing={2}>
                  {demoEquipment.map((item) => (
                    <Grid item xs={12} key={item.id}>
                      <Card sx={{ cursor: 'pointer', '&:hover': { bgcolor: 'action.hover' } }}>
                        <CardContent sx={{ p: 2 }}>
                          <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            <Avatar src={item.imageUrl} sx={{ width: 40, height: 40, mr: 2 }} />
                            <Box sx={{ flexGrow: 1 }}>
                              <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                                {item.name}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {item.type} • Level {item.level}
                              </Typography>
                            </Box>
                            <Chip
                              label={item.rarity}
                              size="small"
                              sx={{ bgcolor: getRarityColor(item.rarity), color: 'white' }}
                            />
                          </Box>
                        </CardContent>
                      </Card>
                    </Grid>
                  ))}
                </Grid>
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setEquipmentDialogOpen(false)}>Close</Button>
            <Button variant="contained" color="primary">
              Save Equipment
            </Button>
          </DialogActions>
        </Dialog>

        {/* Inventory Management Dialog */}
        <Dialog open={inventoryDialogOpen} onClose={() => setInventoryDialogOpen(false)} maxWidth="lg" fullWidth>
          <DialogTitle>Inventory Management</DialogTitle>
          <DialogContent>
            <Typography variant="body1" sx={{ mb: 3 }}>
              Manage your inventory items. Use, trade, or organize your items.
            </Typography>
            <Grid container spacing={3}>
              {demoInventory.map((item) => (
                <Grid item xs={12} sm={6} md={4} key={item.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardContent>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                        <Avatar src={item.imageUrl} sx={{ width: 50, height: 50, mr: 2 }} />
                        <Box sx={{ flexGrow: 1 }}>
                          <Typography variant="h6">{item.name}</Typography>
                          <Typography variant="body2" color="text.secondary">
                            {item.type.charAt(0).toUpperCase() + item.type.slice(1)}
                          </Typography>
                        </Box>
                        {item.rarity && (
                          <Chip
                            label={item.rarity}
                            size="small"
                            sx={{ bgcolor: getRarityColor(item.rarity), color: 'white' }}
                          />
                        )}
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {item.description}
                      </Typography>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="h6" color="primary">
                          {item.quantity.toLocaleString()}
                          {item.maxQuantity !== 999999 && `/${item.maxQuantity}`}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Value: {item.value.toLocaleString()} gold
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        <Button size="small" variant="outlined" color="primary">
                          Use
                        </Button>
                        <Button size="small" variant="outlined" color="secondary">
                          Trade
                        </Button>
                        <Button size="small" variant="outlined" color="error">
                          Drop
                        </Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setInventoryDialogOpen(false)}>Close</Button>
            <Button variant="contained" color="primary">
              Save Changes
            </Button>
          </DialogActions>
        </Dialog>

        {/* NFT Management Dialog */}
        <Dialog open={nftDialogOpen} onClose={() => setNftDialogOpen(false)} maxWidth="lg" fullWidth>
          <DialogTitle>NFT Collection</DialogTitle>
          <DialogContent>
            <Typography variant="body1" sx={{ mb: 3 }}>
              View and manage your NFT collection. Trade, sell, or display your NFTs.
            </Typography>
            <Grid container spacing={3}>
              {demoNFTs.map((nft) => (
                <Grid item xs={12} sm={6} md={4} key={nft.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardMedia
                      component="img"
                      height="200"
                      image={nft.imageUrl}
                      alt={nft.name}
                      sx={{ objectFit: 'cover' }}
                    />
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                        <Typography variant="h6">{nft.name}</Typography>
                        <Chip
                          label={nft.rarity}
                          size="small"
                          sx={{ bgcolor: getRarityColor(nft.rarity), color: 'white' }}
                        />
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {nft.description}
                      </Typography>
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" gutterBottom>
                          Attributes:
                        </Typography>
                        <Stack direction="row" spacing={1} flexWrap="wrap">
                          {nft.attributes.slice(0, 3).map((attr, index) => (
                            <Chip
                              key={index}
                              label={`${attr.trait_type}: ${attr.value}`}
                              size="small"
                              variant="outlined"
                            />
                          ))}
                        </Stack>
                      </Box>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          {nft.chain}
                        </Typography>
                        <Typography variant="h6" color="primary">
                          ${nft.value.toLocaleString()}
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        <Button size="small" variant="outlined" color="primary">
                          View Details
                        </Button>
                        <Button size="small" variant="outlined" color="secondary">
                          Trade
                        </Button>
                        <Button size="small" variant="outlined" color="error">
                          Sell
                        </Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setNftDialogOpen(false)}>Close</Button>
            <Button variant="contained" color="primary">
              Refresh Collection
            </Button>
          </DialogActions>
        </Dialog>

        {/* GeoNFT Management Dialog */}
        <Dialog open={geoNftDialogOpen} onClose={() => setGeoNftDialogOpen(false)} maxWidth="lg" fullWidth>
          <DialogTitle>GeoNFT Collection</DialogTitle>
          <DialogContent>
            <Typography variant="body1" sx={{ mb: 3 }}>
              View and manage your GeoNFT collection. Explore locations and discover new GeoNFTs.
            </Typography>
            <Grid container spacing={3}>
              {demoGeoNFTs.map((geoNft) => (
                <Grid item xs={12} sm={6} md={4} key={geoNft.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardMedia
                      component="img"
                      height="200"
                      image={geoNft.imageUrl}
                      alt={geoNft.name}
                      sx={{ objectFit: 'cover' }}
                    />
                    <CardContent>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                        <Typography variant="h6">{geoNft.name}</Typography>
                        <Chip
                          label={geoNft.rarity}
                          size="small"
                          sx={{ bgcolor: getRarityColor(geoNft.rarity), color: 'white' }}
                        />
                      </Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {geoNft.description}
                      </Typography>
                      <Box sx={{ mb: 2 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                          <LocationOn sx={{ fontSize: 16, mr: 1, color: 'text.secondary' }} />
                          <Typography variant="body2" color="text.secondary">
                            {geoNft.location}
                          </Typography>
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                          Discovered: {new Date(geoNft.discoveredDate).toLocaleDateString()}
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          {geoNft.latitude.toFixed(4)}, {geoNft.longitude.toFixed(4)}
                        </Typography>
                        <Typography variant="h6" color="primary">
                          ${geoNft.value.toLocaleString()}
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        <Button size="small" variant="outlined" color="primary">
                          View on Map
                        </Button>
                        <Button size="small" variant="outlined" color="secondary">
                          Trade
                        </Button>
                        <Button size="small" variant="outlined" color="error">
                          Sell
                        </Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setGeoNftDialogOpen(false)}>Close</Button>
            <Button variant="contained" color="primary">
              Explore Map
            </Button>
          </DialogActions>
        </Dialog>
      </motion.div>
    </Box>
  );
};

export default MyAvatarPage;
