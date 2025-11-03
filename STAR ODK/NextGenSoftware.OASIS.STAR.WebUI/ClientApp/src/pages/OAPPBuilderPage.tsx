import React, { useState, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useDemoMode } from '../contexts/DemoModeContext';
import {
  Box,
  Typography,
  Button,
  Card,
  CardContent,
  Grid,
  Chip,
  IconButton,
  Menu,
  MenuItem,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  Fab,
  Tooltip,
  Tabs,
  Tab,
  Badge,
  Paper,
  Stack,
  Divider,
  Avatar,
  Switch,
  FormControlLabel,
  Slider,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  ListItemSecondaryAction,
  Alert,
  LinearProgress,
} from '@mui/material';
import {
  Add,
  MoreVert,
  PlayArrow,
  Pause,
  Download,
  Upload,
  Delete,
  Edit,
  Visibility,
  Apps,
  FilterList,
  Search,
  Help,
  Info,
  Build,
  Code,
  DataObject,
  Settings,
  Save,
  Preview,
  DragIndicator,
  Close,
  ExpandMore,
  CheckCircle,
  Warning,
  Error,
  Info as InfoIcon,
  CloudDownload,
  CloudUpload,
  Security,
  Speed,
  Memory,
  Storage,
  NetworkCheck,
  Shield,
  Public,
  Lock,
  Group,
  Person,
  Timeline,
  TrendingUp,
  AutoAwesome,
  Psychology,
  Science,
  Engineering,
  Computer,
  SmartToy,
  Rocket,
  EmojiEvents,
  WorkspacePremium,
  Verified,
  SecurityUpdate,
  Update,
  BugReport,
  Support,
  ContactSupport,
  Forum,
  Chat,
  Email,
  Phone,
  LocationOn,
  Schedule,
  Event,
  CalendarToday,
  Notifications,
  NotificationsActive,
  NotificationsOff,
  Settings as SettingsIcon,
  Tune,
  Sort,
  ContentCopy,
  Refresh,
  FilterList as FilterListIcon,
  Lightbulb,
  Share,
  Folder,
  InsertDriveFile,
  Image,
  VideoFile,
  AudioFile,
  Archive,
  DataObject as DataObjectIcon,
} from '@mui/icons-material';
import { motion, AnimatePresence } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { oappService, templateService, runtimeService, libraryService, celestialBodyService, zomeService, holonService, nftService, geoNftService, questService, missionService, chapterService, inventoryService, geoHotSpotService, pluginService, parkService } from '../services';
import { OAPP } from '../types/star';
import { toast } from 'react-hot-toast';
import axios from 'axios';

// Drag and Drop Types
interface DraggableItem {
  id: string;
  type: 'zome' | 'holon' | 'metadata' | 'runtime' | 'library' | 'nft' | 'geonft' | 'celestialbody' | 'space' | 'inventoryitem';
  name: string;
  description: string;
  icon: React.ReactNode;
  category: string;
  version: string;
  dependencies?: string[];
  properties?: Record<string, any>;
}

interface OAPPComponent {
  id: string;
  type: 'zome' | 'holon' | 'metadata' | 'runtime' | 'library' | 'nft' | 'geonft' | 'celestialbody' | 'space' | 'inventoryitem';
  name: string;
  position: { x: number; y: number };
  properties: Record<string, any>;
  connections: string[];
}

interface OAPPBuilder {
  id: string;
  name: string;
  description: string;
  components: OAPPComponent[];
  metadata: Record<string, any>;
  status: 'draft' | 'building' | 'ready' | 'deployed';
}

const OAPPBuilderPage: React.FC = () => {
  const navigateTo = useNavigate();
  const { isDemoMode } = useDemoMode();
  
  // Builder state
  const [currentBuilder, setCurrentBuilder] = useState<OAPPBuilder | null>(null);
  const [selectedTab, setSelectedTab] = useState(0);
  const [draggedItem, setDraggedItem] = useState<DraggableItem | null>(null);
  const [builderComponents, setBuilderComponents] = useState<OAPPComponent[]>([]);
  const [showPreview, setShowPreview] = useState(false);
  const [showSaveDialog, setShowSaveDialog] = useState(false);
  
  // Form state
  const [oappName, setOappName] = useState('');
  const [oappDescription, setOappDescription] = useState('');
  const [oappCategory, setOappCategory] = useState('');
  
  const queryClient = useQueryClient();

  // API endpoints
  const API_BASE_URL = 'http://localhost:5099/api';

  // Fetch metadata from API
  const { data: celestialBodiesMetaData } = useQuery(
    'celestialBodiesMetaData',
    () => axios.get(`${API_BASE_URL}/CelestialBodiesMetaData`).then(res => res.data),
    { enabled: true }
  );

  const { data: zomesMetaData } = useQuery(
    'zomesMetaData',
    () => axios.get(`${API_BASE_URL}/ZomesMetaData`).then(res => res.data),
    { enabled: true }
  );

  const { data: holonsMetaData } = useQuery(
    'holonsMetaData',
    () => axios.get(`${API_BASE_URL}/HolonsMetaData`).then(res => res.data),
    { enabled: true }
  );

  // Available draggable items - Zomes
  const availableZomes: DraggableItem[] = [
    {
      id: 'auth-zome',
      type: 'zome',
      name: 'Authentication',
      description: 'User authentication and authorization',
      icon: <Security />,
      category: 'Security',
      version: '2.1.0',
      dependencies: ['crypto-zome'],
      properties: { encryption: true, mfa: true }
    },
    {
      id: 'data-zome',
      type: 'zome',
      name: 'Data Management',
      description: 'CRUD operations and data persistence',
      icon: <Storage />,
      category: 'Data',
      version: '3.0.1',
      dependencies: [],
      properties: { caching: true, indexing: true }
    },
    {
      id: 'ai-zome',
      type: 'zome',
      name: 'AI Processing',
      description: 'Machine learning and AI capabilities',
      icon: <Psychology />,
      category: 'AI/ML',
      version: '1.5.2',
      dependencies: ['data-zome'],
      properties: { models: ['gpt', 'claude'], training: true }
    },
    {
      id: 'blockchain-zome',
      type: 'zome',
      name: 'Blockchain',
      description: 'Blockchain integration and smart contracts',
      icon: <Shield />,
      category: 'Web3',
      version: '4.2.0',
      dependencies: ['crypto-zome'],
      properties: { networks: ['ethereum', 'solana'], contracts: true }
    },
    {
      id: 'api-zome',
      type: 'zome',
      name: 'API Gateway',
      description: 'REST and GraphQL API management',
      icon: <NetworkCheck />,
      category: 'Integration',
      version: '2.3.1',
      dependencies: ['auth-zome'],
      properties: { rateLimit: true, versioning: true }
    }
  ];

  // Available draggable items - Holons
  const availableHolons: DraggableItem[] = [
    {
      id: 'user-holon',
      type: 'holon',
      name: 'User Profile',
      description: 'User data and profile information',
      icon: <Person />,
      category: 'Identity',
      version: '1.2.0',
      properties: { fields: ['name', 'email', 'avatar'], validation: true }
    },
    {
      id: 'content-holon',
      type: 'holon',
      name: 'Content',
      description: 'Rich content and media management',
      icon: <InsertDriveFile />,
      category: 'Content',
      version: '2.1.3',
      properties: { types: ['text', 'image', 'video'], metadata: true }
    },
    {
      id: 'transaction-holon',
      type: 'holon',
      name: 'Transaction',
      description: 'Financial transactions and payments',
      icon: <TrendingUp />,
      category: 'Finance',
      version: '3.0.0',
      properties: { currency: true, history: true, security: true }
    },
    {
      id: 'notification-holon',
      type: 'holon',
      name: 'Notifications',
      description: 'Real-time notifications and messaging',
      icon: <Notifications />,
      category: 'Communication',
      version: '1.8.2',
      properties: { channels: ['email', 'push', 'sms'], scheduling: true }
    },
    {
      id: 'analytics-holon',
      type: 'holon',
      name: 'Analytics',
      description: 'Data analytics and reporting',
      icon: <Timeline />,
      category: 'Analytics',
      version: '2.5.1',
      properties: { metrics: true, dashboards: true, exports: true }
    }
  ];

  // Available draggable items - Runtimes
  const availableRuntimes: DraggableItem[] = [
    {
      id: 'nodejs-runtime',
      type: 'runtime',
      name: 'Node.js Runtime',
      description: 'JavaScript runtime environment',
      icon: <Code />,
      category: 'Runtime',
      version: '18.17.0',
      properties: { language: 'javascript', async: true, npm: true }
    },
    {
      id: 'python-runtime',
      type: 'runtime',
      name: 'Python Runtime',
      description: 'Python interpreter and environment',
      icon: <Computer />,
      category: 'Runtime',
      version: '3.11.0',
      properties: { language: 'python', packages: true, ml: true }
    },
    {
      id: 'dotnet-runtime',
      type: 'runtime',
      name: '.NET Runtime',
      description: 'Microsoft .NET runtime environment',
      icon: <Settings />,
      category: 'Runtime',
      version: '8.0.0',
      properties: { language: 'csharp', crossplatform: true, performance: true }
    }
  ];

  // Available draggable items - Libraries
  const availableLibraries: DraggableItem[] = [
    {
      id: 'react-library',
      type: 'library',
      name: 'React Library',
      description: 'UI component library for web applications',
      icon: <Apps />,
      category: 'UI',
      version: '18.2.0',
      properties: { framework: 'react', components: true, hooks: true }
    },
    {
      id: 'express-library',
      type: 'library',
      name: 'Express.js',
      description: 'Web application framework for Node.js',
      icon: <NetworkCheck />,
      category: 'Backend',
      version: '4.18.0',
      properties: { framework: 'express', middleware: true, routing: true }
    },
    {
      id: 'tensorflow-library',
      type: 'library',
      name: 'TensorFlow',
      description: 'Machine learning and AI library',
      icon: <Psychology />,
      category: 'AI/ML',
      version: '2.13.0',
      properties: { ml: true, neural: true, training: true }
    }
  ];

  // Available draggable items - NFTs
  const availableNFTs: DraggableItem[] = [
    {
      id: 'art-nft',
      type: 'nft',
      name: 'Digital Art NFT',
      description: 'Digital artwork and collectibles',
      icon: <Image />,
      category: 'Art',
      version: '1.0.0',
      properties: { media: 'image', rarity: true, ownership: true }
    },
    {
      id: 'music-nft',
      type: 'nft',
      name: 'Music NFT',
      description: 'Musical compositions and audio',
      icon: <AudioFile />,
      category: 'Music',
      version: '1.0.0',
      properties: { media: 'audio', streaming: true, royalties: true }
    },
    {
      id: 'video-nft',
      type: 'nft',
      name: 'Video NFT',
      description: 'Video content and animations',
      icon: <VideoFile />,
      category: 'Video',
      version: '1.0.0',
      properties: { media: 'video', streaming: true, quality: '4k' }
    }
  ];

  // Available draggable items - GeoNFTs
  const availableGeoNFTs: DraggableItem[] = [
    {
      id: 'location-geonft',
      type: 'geonft',
      name: 'Location GeoNFT',
      description: 'Geographic location-based NFT',
      icon: <LocationOn />,
      category: 'Location',
      version: '1.0.0',
      properties: { coordinates: true, radius: true, ar: true }
    },
    {
      id: 'landmark-geonft',
      type: 'geonft',
      name: 'Landmark GeoNFT',
      description: 'Historical landmark or POI NFT',
      icon: <Event />,
      category: 'Landmark',
      version: '1.0.0',
      properties: { historical: true, cultural: true, tourism: true }
    }
  ];

  // Available draggable items - Celestial Bodies
  const availableCelestialBodies: DraggableItem[] = [
    {
      id: 'planet-celestial',
      type: 'celestialbody',
      name: 'Planet',
      description: 'Celestial planet object',
      icon: <Public />,
      category: 'Celestial',
      version: '1.0.0',
      properties: { gravity: true, atmosphere: true, life: true }
    },
    {
      id: 'star-celestial',
      type: 'celestialbody',
      name: 'Star',
      description: 'Stellar object with energy',
      icon: <Public />,
      category: 'Celestial',
      version: '1.0.0',
      properties: { energy: true, light: true, heat: true }
    },
    {
      id: 'moon-celestial',
      type: 'celestialbody',
      name: 'Moon',
      description: 'Natural satellite object',
      icon: <Schedule />,
      category: 'Celestial',
      version: '1.0.0',
      properties: { orbit: true, phases: true, tides: true }
    }
  ];

  // Available draggable items - Spaces
  const availableSpaces: DraggableItem[] = [
    {
      id: 'virtual-space',
      type: 'space',
      name: 'Virtual Space',
      description: '3D virtual environment',
      icon: <Apps />,
      category: 'Virtual',
      version: '1.0.0',
      properties: { dimensions: '3d', physics: true, interaction: true }
    },
    {
      id: 'meeting-space',
      type: 'space',
      name: 'Meeting Space',
      description: 'Collaborative meeting environment',
      icon: <Group />,
      category: 'Collaboration',
      version: '1.0.0',
      properties: { avatars: true, voice: true, screen: true }
    }
  ];

  // Available draggable items - Inventory Items
  const availableInventoryItems: DraggableItem[] = [
    {
      id: 'weapon-item',
      type: 'inventoryitem',
      name: 'Weapon',
      description: 'Combat weapon item',
      icon: <Security />,
      category: 'Combat',
      version: '1.0.0',
      properties: { damage: true, durability: true, rarity: true }
    },
    {
      id: 'armor-item',
      type: 'inventoryitem',
      name: 'Armor',
      description: 'Protective armor item',
      icon: <Shield />,
      category: 'Defense',
      version: '1.0.0',
      properties: { protection: true, weight: true, material: true }
    },
    {
      id: 'tool-item',
      type: 'inventoryitem',
      name: 'Tool',
      description: 'Utility tool item',
      icon: <Build />,
      category: 'Utility',
      version: '1.0.0',
      properties: { function: true, efficiency: true, durability: true }
    }
  ];

  // Available draggable items - Metadata
  const availableMetadata: DraggableItem[] = [
    {
      id: 'permissions-metadata',
      type: 'metadata',
      name: 'Permissions',
      description: 'Access control and permissions',
      icon: <Lock />,
      category: 'Security',
      version: '1.0.0',
      properties: { roles: true, policies: true, inheritance: true }
    },
    {
      id: 'versioning-metadata',
      type: 'metadata',
      name: 'Versioning',
      description: 'Version control and history',
      icon: <Update />,
      category: 'Development',
      version: '1.1.0',
      properties: { branching: true, merging: true, rollback: true }
    },
    {
      id: 'performance-metadata',
      type: 'metadata',
      name: 'Performance',
      description: 'Performance monitoring and optimization',
      icon: <Speed />,
      category: 'Monitoring',
      version: '2.0.0',
      properties: { metrics: true, alerts: true, optimization: true }
    },
    {
      id: 'compliance-metadata',
      type: 'metadata',
      name: 'Compliance',
      description: 'Regulatory compliance and auditing',
      icon: <Verified />,
      category: 'Compliance',
      version: '1.3.0',
      properties: { gdpr: true, audit: true, reporting: true }
    }
  ];

  // Drag and drop handlers
  const handleDragStart = (item: DraggableItem, event: React.DragEvent) => {
    setDraggedItem(item);
    event.dataTransfer.effectAllowed = 'copy';
    event.dataTransfer.setData('text/plain', item.id);
  };

  const handleDragEnd = () => {
    setDraggedItem(null);
  };

  const handleDrop = useCallback((event: React.DragEvent) => {
    event.preventDefault();
    event.stopPropagation();
    if (!draggedItem) return;

    const rect = event.currentTarget.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;

    const newComponent: OAPPComponent = {
      id: `${draggedItem.id}-${Date.now()}`,
      type: draggedItem.type,
      name: draggedItem.name,
      position: { x, y },
      properties: draggedItem.properties || {},
      connections: []
    };

    setBuilderComponents(prev => [...prev, newComponent]);
    setDraggedItem(null);
    toast.success(`${draggedItem.name} added to canvas!`);
  }, [draggedItem]);

  const handleDragOver = (event: React.DragEvent) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'copy';
  };

  // Component actions
  const removeComponent = (componentId: string) => {
    setBuilderComponents(prev => prev.filter(comp => comp.id !== componentId));
  };

  const updateComponent = (componentId: string, updates: Partial<OAPPComponent>) => {
    setBuilderComponents(prev => 
      prev.map(comp => comp.id === componentId ? { ...comp, ...updates } : comp)
    );
  };

  // Save OAPP
  const saveOAPP = () => {
    if (!oappName.trim()) {
      toast.error('Please enter an OAPP name');
      return;
    }

    const newOAPP: OAPPBuilder = {
      id: `oapp-${Date.now()}`,
      name: oappName,
      description: oappDescription,
      components: builderComponents,
      metadata: {
        category: oappCategory,
        created: new Date().toISOString(),
        version: '1.0.0'
      },
      status: 'draft'
    };

    setCurrentBuilder(newOAPP);
    setShowSaveDialog(false);
    toast.success('OAPP saved successfully!');
  };

  // Render draggable item
  const DraggableItemCard: React.FC<{ item: DraggableItem }> = ({ item }) => (
    <motion.div
      draggable
      onDragStart={(e) => handleDragStart(item, e as unknown as React.DragEvent)}
      onDragEnd={handleDragEnd}
      whileHover={{ scale: 1.02 }}
      whileTap={{ scale: 0.98 }}
    >
      <Card 
        sx={{ 
          cursor: 'grab',
          '&:active': { cursor: 'grabbing' },
          border: '2px dashed transparent',
          '&:hover': { borderColor: 'primary.main' }
        }}
      >
        <CardContent sx={{ p: 2 }}>
          <Stack direction="row" spacing={2} alignItems="center">
            <Avatar sx={{ bgcolor: item.type === 'zome' ? 'primary.main' : item.type === 'holon' ? 'secondary.main' : 'success.main' }}>
              {item.icon}
            </Avatar>
            <Box flex={1}>
              <Typography variant="subtitle1" fontWeight="bold">
                {item.name}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {item.description}
              </Typography>
              <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                <Chip label={item.category} size="small" />
                <Chip label={item.version} size="small" variant="outlined" />
              </Stack>
            </Box>
            <DragIndicator color="action" />
          </Stack>
        </CardContent>
      </Card>
    </motion.div>
  );

  // Render builder component
  const BuilderComponent: React.FC<{ component: OAPPComponent }> = ({ component }) => (
    <motion.div
      initial={{ opacity: 0, scale: 0.8 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={{ opacity: 0, scale: 0.8 }}
      style={{
        position: 'absolute',
        left: component.position.x,
        top: component.position.y,
        zIndex: 10
      }}
    >
      <Card 
        sx={{ 
          minWidth: 200,
          cursor: 'move',
          border: '2px solid',
          borderColor: component.type === 'zome' ? 'primary.main' : component.type === 'holon' ? 'secondary.main' : 'success.main'
        }}
      >
        <CardContent sx={{ p: 2 }}>
          <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between">
            <Stack direction="row" spacing={1} alignItems="center">
              <Avatar sx={{ 
                bgcolor: component.type === 'zome' ? 'primary.main' : component.type === 'holon' ? 'secondary.main' : 'success.main',
                width: 24,
                height: 24
              }}>
                {component.type === 'zome' ? <Code /> : component.type === 'holon' ? <DataObject /> : <Settings />}
              </Avatar>
              <Typography variant="subtitle2" fontWeight="bold">
                {component.name}
              </Typography>
            </Stack>
            <IconButton 
              size="small" 
              onClick={() => removeComponent(component.id)}
              sx={{ color: 'error.main' }}
            >
              <Close />
            </IconButton>
          </Stack>
        </CardContent>
      </Card>
    </motion.div>
  );

  // Get all available items
  const getAllItems = () => {
    return [
      ...availableZomes,
      ...availableHolons,
      ...availableRuntimes,
      ...availableLibraries,
      ...availableNFTs,
      ...availableGeoNFTs,
      ...availableCelestialBodies,
      ...availableSpaces,
      ...availableInventoryItems,
      ...availableMetadata
    ];
  };

  return (
    <Box sx={{ height: '100vh', display: 'flex', flexDirection: 'column' }}>
      {/* Header */}
      <Box sx={{ p: 3, borderBottom: 1, borderColor: 'divider', bgcolor: 'background.paper' }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Box>
            <Typography variant="h4" fontWeight="bold" gutterBottom>
              OAPP Builder
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Drag and drop components to create unique OAPPs
            </Typography>
          </Box>
          <Stack direction="row" spacing={2}>
            <Tooltip title="Preview OAPP">
              <Button
                variant="outlined"
                startIcon={<Preview />}
                onClick={() => setShowPreview(true)}
                disabled={builderComponents.length === 0}
              >
                Preview
              </Button>
            </Tooltip>
            <Tooltip title="Save OAPP">
              <Button
                variant="contained"
                startIcon={<Save />}
                onClick={() => setShowSaveDialog(true)}
                disabled={builderComponents.length === 0}
              >
                Save OAPP
              </Button>
            </Tooltip>
          </Stack>
        </Stack>
      </Box>

      <Box sx={{ flex: 1, display: 'flex' }}>
        {/* Left Panel - Component Library */}
        <Box sx={{ width: 350, borderRight: 1, borderColor: 'divider', bgcolor: 'background.paper' }}>
          <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
            <Typography variant="h6" fontWeight="bold">
              Component Library
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Drag components to the canvas
            </Typography>
          </Box>

          <Tabs 
            value={selectedTab} 
            onChange={(_, newValue) => setSelectedTab(newValue)}
            sx={{ borderBottom: 1, borderColor: 'divider' }}
          >
            <Tab label="Zomes" />
            <Tab label="Holons" />
            <Tab label="Runtimes" />
            <Tab label="Libraries" />
            <Tab label="NFTs" />
            <Tab label="GeoNFTs" />
            <Tab label="Celestial" />
            <Tab label="Spaces" />
            <Tab label="Inventory" />
            <Tab label="Metadata" />
          </Tabs>

          <Box sx={{ p: 2, height: 'calc(100vh - 200px)', overflow: 'auto' }}>
            {selectedTab === 0 && (
              <Stack spacing={2}>
                {availableZomes.map((zome) => (
                  <DraggableItemCard key={zome.id} item={zome} />
                ))}
              </Stack>
            )}
            {selectedTab === 1 && (
              <Stack spacing={2}>
                {availableHolons.map((holon) => (
                  <DraggableItemCard key={holon.id} item={holon} />
                ))}
              </Stack>
            )}
            {selectedTab === 2 && (
              <Stack spacing={2}>
                {availableRuntimes.map((runtime) => (
                  <DraggableItemCard key={runtime.id} item={runtime} />
                ))}
              </Stack>
            )}
            {selectedTab === 3 && (
              <Stack spacing={2}>
                {availableLibraries.map((library) => (
                  <DraggableItemCard key={library.id} item={library} />
                ))}
              </Stack>
            )}
            {selectedTab === 4 && (
              <Stack spacing={2}>
                {availableNFTs.map((nft) => (
                  <DraggableItemCard key={nft.id} item={nft} />
                ))}
              </Stack>
            )}
            {selectedTab === 5 && (
              <Stack spacing={2}>
                {availableGeoNFTs.map((geonft) => (
                  <DraggableItemCard key={geonft.id} item={geonft} />
                ))}
              </Stack>
            )}
            {selectedTab === 6 && (
              <Stack spacing={2}>
                {availableCelestialBodies.map((celestial) => (
                  <DraggableItemCard key={celestial.id} item={celestial} />
                ))}
              </Stack>
            )}
            {selectedTab === 7 && (
              <Stack spacing={2}>
                {availableSpaces.map((space) => (
                  <DraggableItemCard key={space.id} item={space} />
                ))}
              </Stack>
            )}
            {selectedTab === 8 && (
              <Stack spacing={2}>
                {availableInventoryItems.map((item) => (
                  <DraggableItemCard key={item.id} item={item} />
                ))}
              </Stack>
            )}
            {selectedTab === 9 && (
              <Stack spacing={2}>
                {availableMetadata.map((metadata) => (
                  <DraggableItemCard key={metadata.id} item={metadata} />
                ))}
              </Stack>
            )}
          </Box>
        </Box>

        {/* Center Panel - Builder Canvas */}
        <Box sx={{ flex: 1, position: 'relative', bgcolor: 'grey.50' }}>
          <Box
            sx={{
              width: '100%',
              height: '100%',
              position: 'relative',
              background: `
                radial-gradient(circle at 20px 20px, rgba(0,0,0,0.1) 1px, transparent 1px),
                linear-gradient(45deg, transparent 25%, rgba(0,0,0,0.05) 25%, rgba(0,0,0,0.05) 50%, transparent 50%, transparent 75%, rgba(0,0,0,0.05) 75%)
              `,
              backgroundSize: '40px 40px, 40px 40px'
            }}
            onDrop={handleDrop}
            onDragOver={handleDragOver}
          >
            {builderComponents.length === 0 ? (
              <Box 
                sx={{ 
                  position: 'absolute',
                  top: '50%',
                  left: '50%',
                  transform: 'translate(-50%, -50%)',
                  textAlign: 'center',
                  color: 'text.secondary'
                }}
              >
                <Apps sx={{ fontSize: 64, mb: 2, opacity: 0.3 }} />
                <Typography variant="h6" gutterBottom>
                  Drag components here to start building
                </Typography>
                <Typography variant="body2">
                  Choose from zomes, holons, runtimes, libraries, NFTs, and more in the left panel
                </Typography>
              </Box>
            ) : (
              <AnimatePresence>
                {builderComponents.map((component) => (
                  <BuilderComponent key={component.id} component={component} />
                ))}
              </AnimatePresence>
            )}
          </Box>
        </Box>

        {/* Right Panel - Properties & Info */}
        <Box sx={{ width: 300, borderLeft: 1, borderColor: 'divider', bgcolor: 'background.paper' }}>
          <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
            <Typography variant="h6" fontWeight="bold">
              OAPP Properties
            </Typography>
          </Box>

          <Box sx={{ p: 2, height: 'calc(100vh - 200px)', overflow: 'auto' }}>
            <Stack spacing={2}>
              <Alert severity="info" sx={{ mb: 2 }}>
                <Typography variant="body2">
                  <strong>Components:</strong> {builderComponents.length}
                </Typography>
                <Typography variant="body2">
                  <strong>Zomes:</strong> {builderComponents.filter(c => c.type === 'zome').length}
                </Typography>
                <Typography variant="body2">
                  <strong>Holons:</strong> {builderComponents.filter(c => c.type === 'holon').length}
                </Typography>
                <Typography variant="body2">
                  <strong>Runtimes:</strong> {builderComponents.filter(c => c.type === 'runtime').length}
                </Typography>
                <Typography variant="body2">
                  <strong>Libraries:</strong> {builderComponents.filter(c => c.type === 'library').length}
                </Typography>
                <Typography variant="body2">
                  <strong>NFTs:</strong> {builderComponents.filter(c => c.type === 'nft').length}
                </Typography>
                <Typography variant="body2">
                  <strong>GeoNFTs:</strong> {builderComponents.filter(c => c.type === 'geonft').length}
                </Typography>
                <Typography variant="body2">
                  <strong>Celestial Bodies:</strong> {builderComponents.filter(c => c.type === 'celestialbody').length}
                </Typography>
                <Typography variant="body2">
                  <strong>Spaces:</strong> {builderComponents.filter(c => c.type === 'space').length}
                </Typography>
                <Typography variant="body2">
                  <strong>Inventory:</strong> {builderComponents.filter(c => c.type === 'inventoryitem').length}
                </Typography>
                <Typography variant="body2">
                  <strong>Metadata:</strong> {builderComponents.filter(c => c.type === 'metadata').length}
                </Typography>
              </Alert>

              {builderComponents.length > 0 && (
                <Accordion>
                  <AccordionSummary expandIcon={<ExpandMore />}>
                    <Typography variant="subtitle2">Component Details</Typography>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Stack spacing={1}>
                      {builderComponents.map((component) => (
                        <Card key={component.id} variant="outlined">
                          <CardContent sx={{ p: 1 }}>
                            <Stack direction="row" spacing={1} alignItems="center">
                              <Avatar sx={{ 
                                bgcolor: component.type === 'zome' ? 'primary.main' : component.type === 'holon' ? 'secondary.main' : 'success.main',
                                width: 20,
                                height: 20
                              }}>
                                {component.type === 'zome' ? <Code /> : component.type === 'holon' ? <DataObject /> : <Settings />}
                              </Avatar>
                              <Typography variant="body2" fontWeight="bold">
                                {component.name}
                              </Typography>
                            </Stack>
                          </CardContent>
                        </Card>
                      ))}
                    </Stack>
                  </AccordionDetails>
                </Accordion>
              )}

              <Accordion>
                <AccordionSummary expandIcon={<ExpandMore />}>
                  <Typography variant="subtitle2">Builder Tips</Typography>
                </AccordionSummary>
                <AccordionDetails>
                  <Stack spacing={1}>
                    <Typography variant="body2">
                      • Start with core zomes for functionality
                    </Typography>
                    <Typography variant="body2">
                      • Add holons for data management
                    </Typography>
                    <Typography variant="body2">
                      • Include runtimes for execution
                    </Typography>
                    <Typography variant="body2">
                      • Add libraries for additional features
                    </Typography>
                    <Typography variant="body2">
                      • Include NFTs and GeoNFTs for digital assets
                    </Typography>
                    <Typography variant="body2">
                      • Add celestial bodies for space environments
                    </Typography>
                    <Typography variant="body2">
                      • Include spaces for virtual environments
                    </Typography>
                    <Typography variant="body2">
                      • Add inventory items for game mechanics
                    </Typography>
                    <Typography variant="body2">
                      • Include metadata for configuration
                    </Typography>
                  </Stack>
                </AccordionDetails>
              </Accordion>
            </Stack>
          </Box>
        </Box>
      </Box>

      {/* Save Dialog */}
      <Dialog open={showSaveDialog} onClose={() => setShowSaveDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Save OAPP</DialogTitle>
        <DialogContent>
          <Stack spacing={3} sx={{ mt: 1 }}>
            <TextField
              label="OAPP Name"
              value={oappName}
              onChange={(e) => setOappName(e.target.value)}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={oappDescription}
              onChange={(e) => setOappDescription(e.target.value)}
              fullWidth
              multiline
              rows={3}
            />
            <FormControl fullWidth>
              <InputLabel>Category</InputLabel>
              <Select
                value={oappCategory}
                onChange={(e) => setOappCategory(e.target.value)}
                label="Category"
              >
                <MenuItem value="productivity">Productivity</MenuItem>
                <MenuItem value="entertainment">Entertainment</MenuItem>
                <MenuItem value="business">Business</MenuItem>
                <MenuItem value="education">Education</MenuItem>
                <MenuItem value="gaming">Gaming</MenuItem>
                <MenuItem value="social">Social</MenuItem>
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowSaveDialog(false)}>Cancel</Button>
          <Button onClick={saveOAPP} variant="contained">Save OAPP</Button>
        </DialogActions>
      </Dialog>

      {/* Preview Dialog */}
      <Dialog open={showPreview} onClose={() => setShowPreview(false)} maxWidth="md" fullWidth>
        <DialogTitle>OAPP Preview</DialogTitle>
        <DialogContent>
          <Box sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              {oappName || 'Untitled OAPP'}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              {oappDescription || 'No description provided'}
            </Typography>
            
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <Card>
                  <CardContent>
                    <Typography variant="subtitle1" fontWeight="bold" gutterBottom>
                      Components ({builderComponents.length})
                    </Typography>
                    <Stack spacing={1}>
                      {builderComponents.map((component) => (
                        <Chip
                          key={component.id}
                          label={component.name}
                          color={component.type === 'zome' ? 'primary' : component.type === 'holon' ? 'secondary' : 'success'}
                          size="small"
                        />
                      ))}
                    </Stack>
                  </CardContent>
                </Card>
              </Grid>
              <Grid item xs={12} md={6}>
                <Card>
                  <CardContent>
                    <Typography variant="subtitle1" fontWeight="bold" gutterBottom>
                      Architecture
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      This OAPP uses a modular architecture with {builderComponents.filter(c => c.type === 'zome').length} zomes, {builderComponents.filter(c => c.type === 'holon').length} holons, {builderComponents.filter(c => c.type === 'runtime').length} runtimes, {builderComponents.filter(c => c.type === 'library').length} libraries, and {builderComponents.filter(c => c.type === 'metadata').length} metadata components.
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowPreview(false)}>Close</Button>
          <Button variant="contained" startIcon={<Rocket />}>Deploy OAPP</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default OAPPBuilderPage;
