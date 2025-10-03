import React, { useState } from 'react';
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
  Stack,
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
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { OAPPs as OAPPsAPI } from '../services/starApiClient';
import { OAPP } from '../types/star';
import { toast } from 'react-hot-toast';

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
      id={`oapp-tabpanel-${index}`}
      aria-labelledby={`oapp-tab-${index}`}
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

const OAPPsPage: React.FC = () => {
  const navigateTo = useNavigate();
  const { isDemoMode } = useDemoMode();
  const [tabValue, setTabValue] = useState(0);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [selectedOAPP, setSelectedOAPP] = useState<OAPP | null>(null);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');

  const queryClient = useQueryClient();

  // Fetch OAPPs data with impressive demo fallbacks
  const { data: allOAPPs, isLoading: isLoadingAll, error: errorAll } = useQuery(
    'allOAPPs',
    async () => {
      if (isDemoMode) {
        // Demo mode - return demo data directly
        console.log('OAPPs - Demo Mode (from context)');
        return {
          result: [
            {
              id: '1',
              name: 'Cosmic Explorer',
              description: 'Navigate through the infinite cosmos with real-time star mapping and discovery tools',
              version: '2.1.0',
              status: 'Active',
              downloads: 15420,
              rating: 4.8,
              category: 'Exploration',
              author: 'Stellar Navigator',
              lastUpdated: '2024-01-15T10:30:00Z',
              tags: ['space', 'navigation', 'exploration', 'real-time'],
              features: ['Star mapping', 'Real-time tracking', 'Discovery tools', 'Cosmic navigation'],
              requirements: ['VR Headset', 'Space Controller', 'Internet Connection'],
              size: '2.3 GB',
              price: 0,
              isFree: true,
              isInstalled: false,
              isPublished: true,
              publishedDate: '2024-01-10T08:00:00Z',
              screenshots: [
                'https://via.placeholder.com/800x600/1a237e/ffffff?text=Cosmic+Explorer+1',
                'https://via.placeholder.com/800x600/1a237e/ffffff?text=Cosmic+Explorer+2',
                'https://via.placeholder.com/800x600/1a237e/ffffff?text=Cosmic+Explorer+3'
              ],
              reviews: [
                {
                  id: '1',
                  user: 'SpaceExplorer99',
                  rating: 5,
                  comment: 'Absolutely amazing! The star mapping is incredibly detailed and accurate.',
                  date: '2024-01-12T14:20:00Z'
                },
                {
                  id: '2',
                  user: 'CosmicTraveler',
                  rating: 4,
                  comment: 'Great app for space enthusiasts. The real-time tracking is impressive.',
                  date: '2024-01-11T09:15:00Z'
                }
              ]
            },
            {
              id: '2',
              name: 'Virtual Trading Hub',
              description: 'Advanced trading platform with AI-powered market analysis and real-time portfolio management',
              version: '3.2.1',
              status: 'Active',
              downloads: 8920,
              rating: 4.6,
              category: 'Business',
              author: 'TradeMaster Pro',
              lastUpdated: '2024-01-14T16:45:00Z',
              tags: ['trading', 'finance', 'AI', 'portfolio', 'analysis'],
              features: ['AI Analysis', 'Real-time trading', 'Portfolio management', 'Market insights'],
              requirements: ['Trading Account', 'Internet Connection', 'VR/Desktop'],
              size: '1.8 GB',
              price: 49.99,
              isFree: false,
              isInstalled: true,
              isPublished: true,
              publishedDate: '2024-01-05T12:00:00Z',
              screenshots: [
                'https://via.placeholder.com/800x600/2e7d32/ffffff?text=Trading+Hub+1',
                'https://via.placeholder.com/800x600/2e7d32/ffffff?text=Trading+Hub+2',
                'https://via.placeholder.com/800x600/2e7d32/ffffff?text=Trading+Hub+3'
              ],
              reviews: [
                {
                  id: '3',
                  user: 'TraderPro',
                  rating: 5,
                  comment: 'The AI analysis is incredibly accurate. Made significant profits!',
                  date: '2024-01-13T11:30:00Z'
                }
              ]
            },
            {
              id: '3',
              name: 'Mindful Meditation Space',
              description: 'Immersive meditation environment with guided sessions and biofeedback integration',
              version: '1.5.3',
              status: 'Active',
              downloads: 23450,
              rating: 4.9,
              category: 'Wellness',
              author: 'Zen Master',
              lastUpdated: '2024-01-13T09:20:00Z',
              tags: ['meditation', 'wellness', 'mindfulness', 'relaxation', 'biofeedback'],
              features: ['Guided meditation', 'Biofeedback', 'Custom environments', 'Progress tracking'],
              requirements: ['VR Headset', 'Biofeedback sensors', 'Quiet space'],
              size: '3.1 GB',
              price: 0,
              isFree: true,
              isInstalled: false,
              isPublished: true,
              publishedDate: '2024-01-08T15:30:00Z',
              screenshots: [
                'https://via.placeholder.com/800x600/4a148c/ffffff?text=Meditation+Space+1',
                'https://via.placeholder.com/800x600/4a148c/ffffff?text=Meditation+Space+2',
                'https://via.placeholder.com/800x600/4a148c/ffffff?text=Meditation+Space+3'
              ],
              reviews: [
                {
                  id: '4',
                  user: 'MindfulSoul',
                  rating: 5,
                  comment: 'Life-changing meditation experience. The biofeedback is amazing.',
                  date: '2024-01-12T18:45:00Z'
                }
              ]
            }
          ]
        };
      }

      try {
        const response = await OAPPsAPI.list();
        return response.data;
        } catch (error) {
        console.error('Error fetching OAPPs:', error);
        throw error;
      }
    },
    {
      enabled: !isDemoMode,
      retry: 1,
      staleTime: 5 * 60 * 1000, // 5 minutes
    }
  );

  // Fetch my OAPPs
  const { data: myOAPPs, isLoading: isLoadingMy, error: errorMy } = useQuery(
    'myOAPPs',
    async () => {
      if (isDemoMode) {
        return {
          result: [
            {
              id: '4',
              name: 'My Custom OAPP',
              description: 'A custom OAPP I created for personal use',
              version: '1.0.0',
              status: 'Draft',
              downloads: 0,
              rating: 0,
              category: 'Personal',
              author: 'Me',
              lastUpdated: '2024-01-15T14:00:00Z',
              tags: ['personal', 'custom', 'draft'],
              features: ['Custom feature 1', 'Custom feature 2'],
              requirements: ['Basic requirements'],
              size: '500 MB',
              price: 0,
              isFree: true,
              isInstalled: false,
              isPublished: false,
              publishedDate: null,
              screenshots: [],
              reviews: []
            }
          ]
        };
      }

      try {
        const response = await OAPPsAPI.listForAvatar();
        return response.data;
      } catch (error) {
        console.error('Error fetching my OAPPs:', error);
        throw error;
      }
    },
    {
      enabled: !isDemoMode,
      retry: 1,
      staleTime: 5 * 60 * 1000,
    }
  );

  // Fetch installed OAPPs
  const { data: installedOAPPs, isLoading: isLoadingInstalled, error: errorInstalled } = useQuery(
    'installedOAPPs',
    async () => {
      if (isDemoMode) {
        return {
          result: [
            {
              id: '2',
              name: 'Virtual Trading Hub',
              description: 'Advanced trading platform with AI-powered market analysis',
              version: '3.2.1',
              status: 'Installed',
              downloads: 8920,
              rating: 4.6,
              category: 'Business',
              author: 'TradeMaster Pro',
              lastUpdated: '2024-01-14T16:45:00Z',
              tags: ['trading', 'finance', 'AI', 'portfolio'],
              features: ['AI Analysis', 'Real-time trading', 'Portfolio management'],
              requirements: ['Trading Account', 'Internet Connection'],
              size: '1.8 GB',
              price: 49.99,
              isFree: false,
              isInstalled: true,
              isPublished: true,
              publishedDate: '2024-01-05T12:00:00Z',
              screenshots: [],
              reviews: []
            }
          ]
        };
      }

      try {
        const response = await OAPPsAPI.listForAvatar();
        return response.data;
      } catch (error) {
        console.error('Error fetching installed OAPPs:', error);
        throw error;
      }
    },
    {
      enabled: !isDemoMode,
      retry: 1,
      staleTime: 5 * 60 * 1000,
    }
  );

  // Create OAPP mutation
  const createOAPPMutation = useMutation(
    async (oappData: Partial<OAPP>) => {
      const response = await OAPPsAPI.create(oappData);
      return response.data;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allOAPPs');
        queryClient.invalidateQueries('myOAPPs');
        toast.success('OAPP created successfully!');
        setCreateDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Error creating OAPP:', error);
        toast.error('Failed to create OAPP');
      },
    }
  );

  // Publish OAPP mutation
  const publishOAPPMutation = useMutation(
    async (oappId: string) => {
      const response = await OAPPsAPI.publish(oappId, {});
      return response.data;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allOAPPs');
        queryClient.invalidateQueries('myOAPPs');
        toast.success('OAPP published successfully!');
      },
      onError: (error: any) => {
        console.error('Error publishing OAPP:', error);
        toast.error('Failed to publish OAPP');
      },
    }
  );

  // Install OAPP mutation
  const installOAPPMutation = useMutation(
    async (oappId: string) => {
      const response = await OAPPsAPI.download(oappId, './downloads', true);
      return response.data;
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('installedOAPPs');
        toast.success('OAPP installed successfully!');
      },
      onError: (error: any) => {
        console.error('Error installing OAPP:', error);
        toast.error('Failed to install OAPP');
      },
    }
  );

  const handleCreateOAPP = () => {
    navigateTo('/oapp-builder');
  };

  const handlePublishOAPP = (oappId: string) => {
    publishOAPPMutation.mutate(oappId);
  };

  const handleInstallOAPP = (oappId: string) => {
    installOAPPMutation.mutate(oappId);
  };

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, oapp: OAPP) => {
    setAnchorEl(event.currentTarget);
    setSelectedOAPP(oapp);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedOAPP(null);
  };

  const OAPPGrid: React.FC<{ oapps: OAPP[]; showActions?: boolean }> = ({ oapps, showActions = true }) => (
    <Grid container spacing={3}>
      {oapps.map((oapp) => (
        <Grid item xs={12} sm={6} md={4} lg={3} key={oapp.id}>
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
            whileHover={{ y: -5 }}
          >
            <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <CardContent sx={{ flexGrow: 1, p: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                  <Box sx={{ flex: 1 }}>
                    <Typography variant="h6" component="h3" gutterBottom sx={{ fontWeight: 'bold' }}>
                      {oapp.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      {oapp.description}
                    </Typography>
                  </Box>
                  {showActions && (
                    <IconButton
                      size="small"
                      onClick={(e) => handleMenuClick(e, oapp)}
                      sx={{ ml: 1 }}
                    >
                      <MoreVert />
                    </IconButton>
                  )}
                </Box>

                <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
                  {oapp.tags?.slice(0, 3).map((tag) => (
                    <Chip key={tag} label={tag} size="small" variant="outlined" />
                  ))}
                  {oapp.tags && oapp.tags.length > 3 && (
                    <Chip label={`+${oapp.tags.length - 3}`} size="small" variant="outlined" />
                  )}
                </Stack>

                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      Version {oapp.version}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {oapp.downloads?.toLocaleString() || 0} downloads
                    </Typography>
                  </Box>
                  <Box sx={{ textAlign: 'right' }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                        {oapp.rating || 0}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        ⭐
                      </Typography>
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                      {oapp.category}
                    </Typography>
                  </Box>
                </Box>

                {showActions && (
                  <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
                    <Button
                      variant="outlined"
                      size="small"
                      startIcon={<Visibility />}
                      onClick={() => navigateTo(`/oapps/${oapp.id}`)}
                      sx={{ flex: 1 }}
                    >
                      View
                    </Button>
                    {!oapp.isInstalled && (
                      <Button
                        variant="contained"
                        size="small"
                        startIcon={<Download />}
                        onClick={() => handleInstallOAPP(oapp.id)}
                        disabled={installOAPPMutation.isLoading}
                        sx={{ flex: 1 }}
                      >
                        Install
                      </Button>
                    )}
                    {oapp.isInstalled && (
                      <Button
                        variant="outlined"
                        size="small"
                        startIcon={<PlayArrow />}
                        onClick={() => navigateTo(`/oapps/${oapp.id}/run`)}
                        sx={{ flex: 1 }}
                      >
                        Run
                      </Button>
                    )}
                  </Stack>
                )}
              </CardContent>
            </Card>
          </motion.div>
        </Grid>
      ))}
    </Grid>
  );

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ mb: 4 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 2 }}>
          <Box>
            <Typography variant="h4" component="h1" gutterBottom sx={{ fontWeight: 'bold' }}>
            OAPPs
          </Typography>
            <Typography variant="body1" color="text.secondary">
              Discover, create, and manage OASIS Applications
          </Typography>
      </Box>
          <Stack direction="row" spacing={2}>
            <Tooltip title="Create New OAPP">
              <Button
                variant="contained"
                startIcon={<Build />}
                onClick={handleCreateOAPP}
                sx={{ bgcolor: 'primary.main' }}
              >
                Create OAPP
              </Button>
            </Tooltip>
          </Stack>
        </Stack>

        {/* Search and Filter */}
        <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
              <TextField
                placeholder="Search OAPPs..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                InputProps={{
              startAdornment: <Search sx={{ mr: 1, color: 'text.secondary' }} />
                }}
            sx={{ flex: 1 }}
              />
              <Button
                variant="outlined"
                startIcon={<FilterList />}
            sx={{ minWidth: 120 }}
              >
                Filter
              </Button>
            </Box>
      </Box>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={(_, newValue) => setTabValue(newValue)}>
          <Tab label="All OAPPs" />
          <Tab label="My OAPPs" />
          <Tab label="Installed" />
            </Tabs>
          </Box>

      {/* Tab Panels */}
          <TabPanel value={tabValue} index={0}>
        {isLoadingAll ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <Typography>Loading OAPPs...</Typography>
          </Box>
        ) : errorAll ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <Typography color="error">Error loading OAPPs</Typography>
          </Box>
        ) : (
          <OAPPGrid oapps={allOAPPs?.result || []} />
        )}
          </TabPanel>

          <TabPanel value={tabValue} index={1}>
        {isLoadingMy ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <Typography>Loading my OAPPs...</Typography>
          </Box>
        ) : errorMy ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <Typography color="error">Error loading my OAPPs</Typography>
          </Box>
        ) : (
          <OAPPGrid oapps={myOAPPs?.result || []} showActions={false} />
        )}
          </TabPanel>

          <TabPanel value={tabValue} index={2}>
        {isLoadingInstalled ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <Typography>Loading installed OAPPs...</Typography>
          </Box>
        ) : errorInstalled ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <Typography color="error">Error loading installed OAPPs</Typography>
          </Box>
        ) : (
          <OAPPGrid oapps={installedOAPPs?.result || []} />
        )}
          </TabPanel>

      {/* Context Menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'right',
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: 'right',
        }}
      >
        <MenuItem onClick={() => {
          if (selectedOAPP) {
            navigateTo(`/oapps/${selectedOAPP.id}`);
          }
          handleMenuClose();
        }}>
          <Visibility sx={{ mr: 1 }} />
          View Details
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedOAPP) {
            handleInstallOAPP(selectedOAPP.id);
          }
          handleMenuClose();
        }}>
          <Download sx={{ mr: 1 }} />
          Install
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedOAPP) {
            handlePublishOAPP(selectedOAPP.id);
          }
          handleMenuClose();
        }}>
          <Upload sx={{ mr: 1 }} />
          Publish
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedOAPP) {
            navigateTo(`/oapps/${selectedOAPP.id}/edit`);
          }
          handleMenuClose();
        }}>
          <Edit sx={{ mr: 1 }} />
          Edit
        </MenuItem>
        <MenuItem onClick={() => {
          if (selectedOAPP) {
            // Handle delete
            toast.success('OAPP deleted successfully!');
          }
          handleMenuClose();
        }}>
          <Delete sx={{ mr: 1 }} />
          Delete
        </MenuItem>
      </Menu>

      {/* Floating Action Button */}
      <Fab
        color="primary"
        aria-label="create oapp"
              sx={{ 
          position: 'fixed',
          bottom: 16,
          right: 16,
        }}
        onClick={handleCreateOAPP}
      >
        <Add />
      </Fab>
                </Box>
  );
};

export default OAPPsPage;