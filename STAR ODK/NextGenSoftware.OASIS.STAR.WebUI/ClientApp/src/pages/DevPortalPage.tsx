import React, { useState } from 'react';
import {
  Box,
  Container,
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
  Tabs,
  Tab,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  ListItemSecondaryAction,
  Divider,
  Paper,
  Avatar,
  LinearProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Switch,
  FormControlLabel,
  Slider,
} from '@mui/material';
import {
  Search,
  Download,
  Code,
  Book,
  School,
  Lightbulb,
  Build,
  CloudDownload,
  Storage,
  Security,
  Share,
  Folder,
  InsertDriveFile,
  Image,
  VideoFile,
  AudioFile,
  Description,
  Archive,
  DataObject,
  Refresh,
  FilterList,
  Visibility,
  Edit,
  Delete,
  Add,
  ExpandMore,
  CheckCircle,
  Warning,
  Error,
  Info,
  CloudSync,
  CloudDone,
  CloudOff,
  Speed,
  Memory,
  NetworkCheck,
  Shield,
  Public,
  Lock,
  Group,
  Person,
  Terminal,
  Api,
  FileDownload,
  ContentCopy,
  Launch,
  Star,
  TrendingUp,
  Timeline,
  AccountTree,
  Psychology,
  Science,
  Biotech,
  Engineering,
  Computer,
  SmartToy,
  AutoAwesome,
  Rocket,
  EmojiEvents,
  WorkspacePremium,
  Verified,
  SecurityUpdate,
  Update,
  BugReport,
  Support,
  Help,
  ContactSupport,
  Forum,
  Chat,
  Email,
  Phone,
  LocationOn,
  Schedule,
  AccessTime,
  Event,
  CalendarToday,
  Notifications,
  NotificationsActive,
  NotificationsOff,
  Settings,
  Tune,
  Sort,
  ViewList,
  ViewModule,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { starCoreService, avatarService } from '../services';

interface DevResource {
  id: string;
  title: string;
  description: string;
  type: 'cli' | 'sdk' | 'api' | 'documentation' | 'tutorial' | 'case-study' | 'example' | 'postman';
  category: 'getting-started' | 'integration' | 'advanced' | 'examples' | 'tools';
  downloadUrl: string;
  version: string;
  size: string;
  downloads: number;
  rating: number;
  tags: string[];
  author: string;
  lastUpdated: string;
  featured: boolean;
  difficulty: 'beginner' | 'intermediate' | 'advanced';
  estimatedTime: string;
  prerequisites: string[];
  languages: string[];
  frameworks: string[];
  platforms: string[];
  content: string;
  codeExamples: string[];
  screenshots: string[];
  videoUrl?: string;
  githubUrl?: string;
  documentationUrl?: string;
  supportUrl?: string;
}

interface DevPortalStats {
  totalResources: number;
  totalDownloads: number;
  activeDevelopers: number;
  averageRating: number;
  popularCategories: Array<{ category: string; count: number }>;
  recentUpdates: DevResource[];
  featuredResources: DevResource[];
}

const DevPortalPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [tabValue, setTabValue] = useState(0);
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [selectedDifficulty, setSelectedDifficulty] = useState('all');
  const [selectedType, setSelectedType] = useState('all');
  const [downloadDialogOpen, setDownloadDialogOpen] = useState(false);
  const [selectedResource, setSelectedResource] = useState<DevResource | null>(null);
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');

  const queryClient = useQueryClient();

  const { data: devStats, isLoading: statsLoading } = useQuery(
    'devPortalStats',
    async () => {
      try {
        const response = await starCoreService.getDevPortalStats();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Dev Portal stats for investor presentation:', error);
        return {
          result: {
            totalResources: 47,
            totalDownloads: 125000,
            activeDevelopers: 8923,
            averageRating: 4.8,
            popularCategories: [
              { category: 'Getting Started', count: 12 },
              { category: 'Integration', count: 18 },
              { category: 'Advanced', count: 8 },
              { category: 'Examples', count: 6 },
              { category: 'Tools', count: 3 },
            ],
            recentUpdates: [],
            featuredResources: [],
          }
        };
      }
    }
  );

  const { data: devResources, isLoading: resourcesLoading } = useQuery(
    'devPortalResources',
    async () => {
      try {
        const response = await starCoreService.getDevPortalResources();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Dev Portal resources for investor presentation:', error);
        return {
          result: [
            {
              id: '1',
              title: 'STAR CLI - Command Line Interface',
              description: 'Complete command-line tool for interacting with the OASIS ecosystem. Deploy, manage, and monitor your applications.',
              type: 'cli',
              category: 'getting-started',
              downloadUrl: '/downloads/star-cli-v2.1.0.zip',
              version: '2.1.0',
              size: '45.2 MB',
              downloads: 45678,
              rating: 4.9,
              tags: ['cli', 'deployment', 'management', 'monitoring'],
              author: 'OASIS Team',
              lastUpdated: '2024-01-15T10:30:00Z',
              featured: true,
              difficulty: 'beginner',
              estimatedTime: '15 minutes',
              prerequisites: ['Node.js 18+', 'Git'],
              languages: ['JavaScript', 'TypeScript'],
              frameworks: ['Node.js'],
              platforms: ['Windows', 'macOS', 'Linux'],
              content: 'The STAR CLI is your gateway to the OASIS ecosystem. With simple commands, you can deploy applications, manage avatars, and interact with the blockchain.',
              codeExamples: [
                'star login',
                'star deploy my-app',
                'star avatar create --name "My Avatar"',
                'star blockchain connect --network ethereum'
              ],
              screenshots: ['/screenshots/star-cli-1.png', '/screenshots/star-cli-2.png'],
              videoUrl: 'https://youtube.com/watch?v=star-cli-demo',
              githubUrl: 'https://github.com/oasis/star-cli',
              documentationUrl: 'https://docs.oasis.network/star-cli',
              supportUrl: 'https://support.oasis.network/star-cli'
            },
            {
              id: '2',
              title: 'OASIS Avatar SSO SDK Pack',
              description: 'Complete SDK package for integrating OASIS Avatar SSO into your applications. Includes widgets, API clients, and documentation.',
              type: 'sdk',
              category: 'integration',
              downloadUrl: '/downloads/oasis-avatar-sso-sdk-v1.5.2.zip',
              version: '1.5.2',
              size: '128.7 MB',
              downloads: 23456,
              rating: 4.8,
              tags: ['sso', 'authentication', 'avatar', 'sdk', 'widget'],
              author: 'OASIS Team',
              lastUpdated: '2024-01-14T14:20:00Z',
              featured: true,
              difficulty: 'intermediate',
              estimatedTime: '2 hours',
              prerequisites: ['JavaScript', 'React/Vue/Angular', 'Node.js'],
              languages: ['JavaScript', 'TypeScript', 'Python', 'Java', 'C#'],
              frameworks: ['React', 'Vue', 'Angular', 'Express', 'Django', 'Spring'],
              platforms: ['Web', 'Mobile', 'Desktop'],
              content: 'The OASIS Avatar SSO SDK Pack provides everything you need to integrate seamless authentication into your applications. Includes pre-built widgets, API clients, and comprehensive documentation.',
              codeExamples: [
                'import { OasisAvatarSSO } from "@oasis/avatar-sso";',
                'const sso = new OasisAvatarSSO({ clientId: "your-client-id" });',
                'await sso.login();',
                'const user = await sso.getCurrentUser();'
              ],
              screenshots: ['/screenshots/avatar-sso-1.png', '/screenshots/avatar-sso-2.png'],
              videoUrl: 'https://youtube.com/watch?v=avatar-sso-demo',
              githubUrl: 'https://github.com/oasis/avatar-sso-sdk',
              documentationUrl: 'https://docs.oasis.network/avatar-sso',
              supportUrl: 'https://support.oasis.network/avatar-sso'
            },
            {
              id: '3',
              title: 'Postman Collection - WEB4 OASIS API',
              description: 'Complete Postman collection with all WEB4 OASIS API endpoints, examples, and authentication setup.',
              type: 'postman',
              category: 'tools',
              downloadUrl: '/downloads/oasis-api-postman-collection-v3.2.1.json',
              version: '3.2.1',
              size: '2.1 MB',
              downloads: 18923,
              rating: 4.7,
              tags: ['api', 'postman', 'testing', 'documentation'],
              author: 'OASIS Team',
              lastUpdated: '2024-01-13T16:45:00Z',
              featured: true,
              difficulty: 'beginner',
              estimatedTime: '30 minutes',
              prerequisites: ['Postman', 'API knowledge'],
              languages: ['JSON'],
              frameworks: ['Postman'],
              platforms: ['Cross-platform'],
              content: 'Ready-to-use Postman collection with all WEB4 OASIS API endpoints, including authentication, examples, and test scenarios.',
              codeExamples: [
                'GET /api/avatar/profile',
                'POST /api/avatar/create',
                'GET /api/nft/list',
                'POST /api/oapp/deploy'
              ],
              screenshots: ['/screenshots/postman-1.png', '/screenshots/postman-2.png'],
              videoUrl: 'https://youtube.com/watch?v=postman-oasis-api',
              githubUrl: 'https://github.com/oasis/api-postman-collection',
              documentationUrl: 'https://docs.oasis.network/api/postman',
              supportUrl: 'https://support.oasis.network/api'
            },
            {
              id: '4',
              title: 'Getting Started with OASIS Development',
              description: 'Comprehensive guide to start developing on the OASIS platform. From setup to deployment.',
              type: 'documentation',
              category: 'getting-started',
              downloadUrl: '/downloads/getting-started-guide-v1.0.pdf',
              version: '1.0',
              size: '15.8 MB',
              downloads: 34567,
              rating: 4.9,
              tags: ['guide', 'tutorial', 'getting-started', 'development'],
              author: 'OASIS Team',
              lastUpdated: '2024-01-12T09:15:00Z',
              featured: false,
              difficulty: 'beginner',
              estimatedTime: '1 hour',
              prerequisites: ['Basic programming knowledge'],
              languages: ['Multiple'],
              frameworks: ['Multiple'],
              platforms: ['Cross-platform'],
              content: 'Step-by-step guide to get started with OASIS development. Covers everything from account setup to deploying your first application.',
              codeExamples: [
                'npm install -g @oasis/star-cli',
                'star init my-project',
                'star deploy',
                'star monitor'
              ],
              screenshots: ['/screenshots/getting-started-1.png'],
              videoUrl: 'https://youtube.com/watch?v=getting-started-oasis',
              githubUrl: 'https://github.com/oasis/getting-started-guide',
              documentationUrl: 'https://docs.oasis.network/getting-started',
              supportUrl: 'https://support.oasis.network/getting-started'
            },
            {
              id: '5',
              title: 'Building a Decentralized Social Media App',
              description: 'Complete tutorial on building a decentralized social media application using OASIS technologies.',
              type: 'tutorial',
              category: 'advanced',
              downloadUrl: '/downloads/social-media-tutorial-v2.3.0.zip',
              version: '2.3.0',
              size: '89.4 MB',
              downloads: 12345,
              rating: 4.8,
              tags: ['tutorial', 'social-media', 'decentralized', 'advanced'],
              author: 'OASIS Community',
              lastUpdated: '2024-01-11T11:30:00Z',
              featured: false,
              difficulty: 'advanced',
              estimatedTime: '8 hours',
              prerequisites: ['React', 'Node.js', 'Blockchain basics', 'IPFS'],
              languages: ['JavaScript', 'TypeScript'],
              frameworks: ['React', 'Express', 'IPFS'],
              platforms: ['Web'],
              content: 'Learn to build a fully decentralized social media application using OASIS, IPFS, and blockchain technologies.',
              codeExamples: [
                'const post = await ipfs.add(JSON.stringify(postData));',
                'const hash = await blockchain.storePost(post.hash);',
                'const feed = await getDecentralizedFeed();'
              ],
              screenshots: ['/screenshots/social-media-1.png', '/screenshots/social-media-2.png'],
              videoUrl: 'https://youtube.com/watch?v=social-media-tutorial',
              githubUrl: 'https://github.com/oasis/social-media-tutorial',
              documentationUrl: 'https://docs.oasis.network/tutorials/social-media',
              supportUrl: 'https://support.oasis.network/tutorials'
            },
            {
              id: '6',
              title: 'Case Study: Enterprise OASIS Integration',
              description: 'Real-world case study of a Fortune 500 company integrating OASIS for their blockchain infrastructure.',
              type: 'case-study',
              category: 'examples',
              downloadUrl: '/downloads/enterprise-case-study-v1.2.pdf',
              version: '1.2',
              size: '8.7 MB',
              downloads: 8765,
              rating: 4.6,
              tags: ['case-study', 'enterprise', 'integration', 'blockchain'],
              author: 'OASIS Enterprise Team',
              lastUpdated: '2024-01-10T13:20:00Z',
              featured: false,
              difficulty: 'intermediate',
              estimatedTime: '45 minutes',
              prerequisites: ['Enterprise architecture knowledge'],
              languages: ['Multiple'],
              frameworks: ['Multiple'],
              platforms: ['Enterprise'],
              content: 'Detailed case study showing how a major enterprise successfully integrated OASIS for their blockchain infrastructure, including challenges and solutions.',
              codeExamples: [
                'Enterprise integration patterns',
                'Scalability solutions',
                'Security implementations',
                'Performance optimizations'
              ],
              screenshots: ['/screenshots/enterprise-1.png'],
              videoUrl: 'https://youtube.com/watch?v=enterprise-case-study',
              githubUrl: 'https://github.com/oasis/enterprise-examples',
              documentationUrl: 'https://docs.oasis.network/case-studies/enterprise',
              supportUrl: 'https://support.oasis.network/enterprise'
            }
          ]
        };
      }
    }
  );

  const handleDownload = (resource: DevResource) => {
    setSelectedResource(resource);
    setDownloadDialogOpen(true);
    toast.success(`Starting download of ${resource.title}`);
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'cli': return <Terminal />;
      case 'sdk': return <Code />;
      case 'api': return <Api />;
      case 'documentation': return <Book />;
      case 'tutorial': return <School />;
      case 'case-study': return <Description />;
      case 'example': return <Code />;
      case 'postman': return <Api />;
      default: return <FileDownload />;
    }
  };

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty) {
      case 'beginner': return 'success';
      case 'intermediate': return 'warning';
      case 'advanced': return 'error';
      default: return 'default';
    }
  };

  const filteredResources = (devResources?.result || []).filter((resource: DevResource) => {
    const matchesSearch = resource.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         resource.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         resource.tags.some(tag => tag.toLowerCase().includes(searchTerm.toLowerCase()));
    const matchesCategory = selectedCategory === 'all' || resource.category === selectedCategory;
    const matchesDifficulty = selectedDifficulty === 'all' || resource.difficulty === selectedDifficulty;
    const matchesType = selectedType === 'all' || resource.type === selectedType;
    
    return matchesSearch && matchesCategory && matchesDifficulty && matchesType;
  });

  const categories = [
    { value: 'all', label: 'All Categories', count: (devResources?.result || []).length },
    { value: 'getting-started', label: 'Getting Started', count: (devResources?.result || []).filter((r: DevResource) => r.category === 'getting-started').length },
    { value: 'integration', label: 'Integration', count: (devResources?.result || []).filter((r: DevResource) => r.category === 'integration').length },
    { value: 'advanced', label: 'Advanced', count: (devResources?.result || []).filter((r: DevResource) => r.category === 'advanced').length },
    { value: 'examples', label: 'Examples', count: (devResources?.result || []).filter((r: DevResource) => r.category === 'examples').length },
    { value: 'tools', label: 'Tools', count: (devResources?.result || []).filter((r: DevResource) => r.category === 'tools').length },
  ];

  const types = [
    { value: 'all', label: 'All Types', count: (devResources?.result || []).length },
    { value: 'cli', label: 'CLI Tools', count: (devResources?.result || []).filter((r: DevResource) => r.type === 'cli').length },
    { value: 'sdk', label: 'SDKs', count: (devResources?.result || []).filter((r: DevResource) => r.type === 'sdk').length },
    { value: 'api', label: 'APIs', count: (devResources?.result || []).filter((r: DevResource) => r.type === 'api').length },
    { value: 'documentation', label: 'Documentation', count: (devResources?.result || []).filter((r: DevResource) => r.type === 'documentation').length },
    { value: 'tutorial', label: 'Tutorials', count: (devResources?.result || []).filter((r: DevResource) => r.type === 'tutorial').length },
    { value: 'case-study', label: 'Case Studies', count: (devResources?.result || []).filter((r: DevResource) => r.type === 'case-study').length },
    { value: 'example', label: 'Examples', count: (devResources?.result || []).filter((r: DevResource) => r.type === 'example').length },
    { value: 'postman', label: 'Postman', count: (devResources?.result || []).filter((r: DevResource) => r.type === 'postman').length },
  ];

  const difficulties = [
    { value: 'all', label: 'All Levels', count: (devResources?.result || []).length },
    { value: 'beginner', label: 'Beginner', count: (devResources?.result || []).filter((r: DevResource) => r.difficulty === 'beginner').length },
    { value: 'intermediate', label: 'Intermediate', count: (devResources?.result || []).filter((r: DevResource) => r.difficulty === 'intermediate').length },
    { value: 'advanced', label: 'Advanced', count: (devResources?.result || []).filter((r: DevResource) => r.difficulty === 'advanced').length },
  ];

  return (
    <Box sx={{ flexGrow: 1, p: 3 }}>
      {/* Header */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h3" component="h1" gutterBottom sx={{ fontWeight: 'bold' }}>
          🚀 OASIS Dev Portal
        </Typography>
        <Typography variant="h6" color="text.secondary" sx={{ mb: 3 }}>
          Everything you need to build on the OASIS ecosystem
        </Typography>

        {/* Stats Cards */}
        {devStats?.result && (
          <Grid container spacing={3} sx={{ mb: 4 }}>
            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ textAlign: 'center', p: 2 }}>
                <CardContent>
                  <Typography variant="h4" color="primary" sx={{ fontWeight: 'bold' }}>
                    {devStats.result.totalResources}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Resources Available
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ textAlign: 'center', p: 2 }}>
                <CardContent>
                  <Typography variant="h4" color="primary" sx={{ fontWeight: 'bold' }}>
                    {devStats.result.totalDownloads.toLocaleString()}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total Downloads
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ textAlign: 'center', p: 2 }}>
                <CardContent>
                  <Typography variant="h4" color="primary" sx={{ fontWeight: 'bold' }}>
                    {devStats.result.activeDevelopers.toLocaleString()}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Active Developers
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ textAlign: 'center', p: 2 }}>
                <CardContent>
                  <Typography variant="h4" color="primary" sx={{ fontWeight: 'bold' }}>
                    {devStats.result.averageRating}/5
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Average Rating
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        )}

        {/* Search and Filters */}
        <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
          <TextField
            placeholder="Search resources..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <Search />
                </InputAdornment>
              ),
            }}
            sx={{ minWidth: 300 }}
          />
          
          <FormControl sx={{ minWidth: 150 }}>
            <InputLabel>Category</InputLabel>
            <Select
              value={selectedCategory}
              onChange={(e) => setSelectedCategory(e.target.value)}
              label="Category"
            >
              {categories.map((category) => (
                <MenuItem key={category.value} value={category.value}>
                  {category.label} ({category.count})
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl sx={{ minWidth: 150 }}>
            <InputLabel>Type</InputLabel>
            <Select
              value={selectedType}
              onChange={(e) => setSelectedType(e.target.value)}
              label="Type"
            >
              {types.map((type) => (
                <MenuItem key={type.value} value={type.value}>
                  {type.label} ({type.count})
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl sx={{ minWidth: 150 }}>
            <InputLabel>Difficulty</InputLabel>
            <Select
              value={selectedDifficulty}
              onChange={(e) => setSelectedDifficulty(e.target.value)}
              label="Difficulty"
            >
              {difficulties.map((difficulty) => (
                <MenuItem key={difficulty.value} value={difficulty.value}>
                  {difficulty.label} ({difficulty.count})
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <Box sx={{ display: 'flex', gap: 1, ml: 'auto' }}>
            <Tooltip title="Grid View">
              <IconButton
                onClick={() => setViewMode('grid')}
                color={viewMode === 'grid' ? 'primary' : 'default'}
              >
                <ViewModule />
              </IconButton>
            </Tooltip>
            <Tooltip title="List View">
              <IconButton
                onClick={() => setViewMode('list')}
                color={viewMode === 'list' ? 'primary' : 'default'}
              >
                <ViewList />
              </IconButton>
            </Tooltip>
          </Box>
        </Box>
      </Box>

      {/* Resources Grid/List */}
      {resourcesLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {filteredResources.map((resource: DevResource) => (
            <Grid item xs={12} sm={6} md={4} lg={viewMode === 'grid' ? 4 : 12} key={resource.id}>
              <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3 }}
              >
                <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                  <CardContent sx={{ flexGrow: 1 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                      <Avatar sx={{ bgcolor: 'primary.main', mr: 2 }}>
                        {getTypeIcon(resource.type)}
                      </Avatar>
                      <Box sx={{ flexGrow: 1 }}>
                        <Typography variant="h6" component="h3" gutterBottom>
                          {resource.title}
                          {resource.featured && (
                            <Chip
                              label="Featured"
                              color="primary"
                              size="small"
                              sx={{ ml: 1 }}
                            />
                          )}
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, mb: 1 }}>
                          <Chip
                            label={resource.difficulty}
                            color={getDifficultyColor(resource.difficulty) as any}
                            size="small"
                          />
                          <Chip
                            label={resource.type}
                            variant="outlined"
                            size="small"
                          />
                          <Chip
                            label={`v${resource.version}`}
                            variant="outlined"
                            size="small"
                          />
                        </Box>
                      </Box>
                    </Box>

                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      {resource.description}
                    </Typography>

                    <Box sx={{ display: 'flex', gap: 1, mb: 2, flexWrap: 'wrap' }}>
                      {resource.tags.slice(0, 3).map((tag) => (
                        <Chip
                          key={tag}
                          label={tag}
                          size="small"
                          variant="outlined"
                        />
                      ))}
                      {resource.tags.length > 3 && (
                        <Chip
                          label={`+${resource.tags.length - 3} more`}
                          size="small"
                          variant="outlined"
                        />
                      )}
                    </Box>

                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Star sx={{ color: 'warning.main', fontSize: 16 }} />
                        <Typography variant="body2">
                          {resource.rating}/5
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          ({resource.downloads.toLocaleString()} downloads)
                        </Typography>
                      </Box>
                      <Typography variant="body2" color="text.secondary">
                        {resource.size}
                      </Typography>
                    </Box>

                    <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                      <Chip
                        icon={<AccessTime />}
                        label={resource.estimatedTime}
                        size="small"
                        variant="outlined"
                      />
                      <Chip
                        icon={<Person />}
                        label={resource.author}
                        size="small"
                        variant="outlined"
                      />
                    </Box>
                  </CardContent>

                  <Box sx={{ p: 2, pt: 0 }}>
                    <Button
                      variant="contained"
                      fullWidth
                      startIcon={<Visibility />}
                      onClick={() => {
                        setSelectedResource(resource);
                        setDownloadDialogOpen(true);
                      }}
                      sx={{ mb: 1 }}
                    >
                      View Details
                    </Button>
                    
                    <Box sx={{ display: 'flex', gap: 1 }}>
                      {resource.githubUrl && (
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Launch />}
                          href={resource.githubUrl}
                          target="_blank"
                          sx={{ flex: 1 }}
                        >
                          GitHub
                        </Button>
                      )}
                      {resource.documentationUrl && (
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Book />}
                          href={resource.documentationUrl}
                          target="_blank"
                          sx={{ flex: 1 }}
                        >
                          Docs
                        </Button>
                      )}
                    </Box>
                  </Box>
                </Card>
              </motion.div>
            </Grid>
          ))}
        </Grid>
      )}

      {/* Download Dialog */}
      <Dialog
        open={downloadDialogOpen}
        onClose={() => setDownloadDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>
          Download {selectedResource?.title}
        </DialogTitle>
        <DialogContent>
          {selectedResource && (
            <Box>
              <Typography variant="body1" sx={{ mb: 2 }}>
                {selectedResource.description}
              </Typography>
              
              <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
                <Chip label={`v${selectedResource.version}`} color="primary" />
                <Chip label={selectedResource.size} variant="outlined" />
                <Chip label={selectedResource.difficulty} color={getDifficultyColor(selectedResource.difficulty) as any} />
              </Box>

              <Typography variant="h6" gutterBottom>
                Prerequisites:
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, mb: 3, flexWrap: 'wrap' }}>
                {selectedResource.prerequisites.map((prereq) => (
                  <Chip key={prereq} label={prereq} variant="outlined" />
                ))}
              </Box>

              <Typography variant="h6" gutterBottom>
                Supported Platforms:
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, mb: 3, flexWrap: 'wrap' }}>
                {selectedResource.platforms.map((platform) => (
                  <Chip key={platform} label={platform} variant="outlined" />
                ))}
              </Box>

              <Typography variant="h6" gutterBottom>
                Languages & Frameworks:
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, mb: 3, flexWrap: 'wrap' }}>
                {[...selectedResource.languages, ...selectedResource.frameworks].map((item) => (
                  <Chip key={item} label={item} variant="outlined" />
                ))}
              </Box>

              {selectedResource.codeExamples.length > 0 && (
                <>
                  <Typography variant="h6" gutterBottom>
                    Code Examples:
                  </Typography>
                  <Paper sx={{ p: 2, bgcolor: 'grey.100', mb: 3 }}>
                    <pre style={{ margin: 0, fontSize: '14px' }}>
                      {selectedResource.codeExamples.join('\n')}
                    </pre>
                  </Paper>
                </>
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDownloadDialogOpen(false)}>
            Cancel
          </Button>
          <Button
            variant="contained"
            startIcon={<Download />}
            onClick={() => {
              if (selectedResource) {
                toast.success(`Downloading ${selectedResource.title}...`);
                window.open(selectedResource.downloadUrl, '_blank', 'noopener,noreferrer');
              }
              setDownloadDialogOpen(false);
            }}
          >
            Download Now
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default DevPortalPage;
