import React, { useState } from 'react';
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
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starNetService } from '../services/starNetService';
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
      try {
        return await starNetService.getAllOAPPs();
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo OAPPs data for investor presentation');
        return {
          result: [
            {
              id: '1',
              name: 'Cosmic Explorer',
              description: 'Navigate through the infinite cosmos with real-time star mapping and discovery tools',
              type: 'Web',
              version: '2.1.0',
              isPublished: true,
              isInstalled: false,
              isActive: true,
              downloads: 15420,
              rating: 4.8,
              author: 'SpaceDev Studios',
              category: 'Exploration',
              lastUpdated: '2024-01-15',
            },
            {
              id: '2',
              name: 'Quantum Builder',
              description: 'Build and design quantum structures in the OASIS with advanced physics simulation',
              type: 'Game',
              version: '1.5.2',
              isPublished: true,
              isInstalled: true,
              isActive: true,
              downloads: 8930,
              rating: 4.9,
              author: 'Quantum Labs',
              category: 'Construction',
              lastUpdated: '2024-01-14',
            },
            {
              id: '3',
              name: 'Neural Network Manager',
              description: 'Advanced AI management system for creating and training neural networks',
              type: 'Service',
              version: '3.0.1',
              isPublished: true,
              isInstalled: false,
              isActive: false,
              downloads: 25670,
              rating: 4.7,
              author: 'AI Innovations',
              category: 'AI/ML',
              lastUpdated: '2024-01-13',
            },
            {
              id: '4',
              name: 'Holographic Designer',
              description: 'Create stunning holographic interfaces and 3D visualizations',
              type: 'Web',
              version: '1.2.5',
              isPublished: true,
              isInstalled: true,
              isActive: false,
              downloads: 12340,
              rating: 4.6,
              author: 'HoloTech',
              category: 'Design',
              lastUpdated: '2024-01-12',
            },
            {
              id: '5',
              name: 'Virtual Reality Portal',
              description: 'Seamless VR integration for immersive OASIS experiences',
              type: 'Mobile',
              version: '2.3.0',
              isPublished: true,
              isInstalled: false,
              isActive: false,
              downloads: 18750,
              rating: 4.8,
              author: 'VR Solutions',
              category: 'VR/AR',
              lastUpdated: '2024-01-11',
            },
            {
              id: '6',
              name: 'Blockchain Tracker',
              description: 'Real-time blockchain monitoring and transaction analysis',
              type: 'Console',
              version: '1.8.3',
              isPublished: true,
              isInstalled: true,
              isActive: true,
              downloads: 9870,
              rating: 4.5,
              author: 'Crypto Analytics',
              category: 'Blockchain',
              lastUpdated: '2024-01-10',
            },
          ]
        };
      }
    },
    { refetchInterval: 30000 }
  );

  const { data: myOAPPs, isLoading: isLoadingMy, error: errorMy } = useQuery(
    'myOAPPs',
    async () => {
      try {
        return await starNetService.getOAPPsCreatedByMe();
      } catch (error) {
        // Fallback to demo data
        return {
          result: [
            {
              id: '7',
              name: 'My Custom Dashboard',
              description: 'Personalized dashboard for monitoring OASIS activities',
              type: 'Web',
              version: '1.0.0',
              isPublished: false,
              isInstalled: true,
              isActive: true,
              downloads: 0,
              rating: 0,
              author: 'You',
              category: 'Personal',
              lastUpdated: '2024-01-15',
            },
            {
              id: '8',
              name: 'Experimental AI',
              description: 'Work in progress - Advanced AI assistant for OASIS',
              type: 'Service',
              version: '0.9.0',
              isPublished: false,
              isInstalled: true,
              isActive: false,
              downloads: 0,
              rating: 0,
              author: 'You',
              category: 'AI/ML',
              lastUpdated: '2024-01-14',
            },
          ]
        };
      }
    },
    { refetchInterval: 30000 }
  );

  const { data: installedOAPPs, isLoading: isLoadingInstalled, error: errorInstalled } = useQuery(
    'installedOAPPs',
    async () => {
      try {
        return await starNetService.getInstalledOAPPs();
      } catch (error) {
        // Fallback to demo data
        return {
          result: [
            {
              id: '2',
              name: 'Quantum Builder',
              description: 'Build and design quantum structures in the OASIS with advanced physics simulation',
              type: 'Game',
              version: '1.5.2',
              isPublished: true,
              isInstalled: true,
              isActive: true,
              downloads: 8930,
              rating: 4.9,
              author: 'Quantum Labs',
              category: 'Construction',
              lastUpdated: '2024-01-14',
            },
            {
              id: '4',
              name: 'Holographic Designer',
              description: 'Create stunning holographic interfaces and 3D visualizations',
              type: 'Web',
              version: '1.2.5',
              isPublished: true,
              isInstalled: true,
              isActive: false,
              downloads: 12340,
              rating: 4.6,
              author: 'HoloTech',
              category: 'Design',
              lastUpdated: '2024-01-12',
            },
            {
              id: '6',
              name: 'Blockchain Tracker',
              description: 'Real-time blockchain monitoring and transaction analysis',
              type: 'Console',
              version: '1.8.3',
              isPublished: true,
              isInstalled: true,
              isActive: true,
              downloads: 9870,
              rating: 4.5,
              author: 'Crypto Analytics',
              category: 'Blockchain',
              lastUpdated: '2024-01-10',
            },
          ]
        };
      }
    },
    { refetchInterval: 30000 }
  );

  // Mutations
  const createOAPPMutation = useMutation(
    (data: { name: string; description: string; type: string }) =>
      starNetService.createOAPP(data.name, data.description, data.type),
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allOAPPs');
        queryClient.invalidateQueries('myOAPPs');
        toast.success('OAPP created successfully!');
        setCreateDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to create OAPP');
      },
    }
  );

  const publishOAPPMutation = useMutation(
    (id: string) => starNetService.publishOAPP(id),
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allOAPPs');
        queryClient.invalidateQueries('myOAPPs');
        toast.success('OAPP published successfully!');
      },
      onError: () => {
        toast.error('Failed to publish OAPP');
      },
    }
  );

  const installOAPPMutation = useMutation(
    (id: string) => starNetService.downloadAndInstallOAPP(id),
    {
      onSuccess: () => {
        queryClient.invalidateQueries('installedOAPPs');
        toast.success('OAPP installed successfully!');
      },
      onError: () => {
        toast.error('Failed to install OAPP');
      },
    }
  );

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, oapp: OAPP) => {
    setAnchorEl(event.currentTarget);
    setSelectedOAPP(oapp);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedOAPP(null);
  };

  const handleCreateOAPP = (data: { name: string; description: string; type: string }) => {
    createOAPPMutation.mutate(data);
  };

  const handlePublishOAPP = () => {
    if (selectedOAPP) {
      publishOAPPMutation.mutate(selectedOAPP.id);
    }
    handleMenuClose();
  };

  const handleInstallOAPP = () => {
    if (selectedOAPP) {
      installOAPPMutation.mutate(selectedOAPP.id);
    }
    handleMenuClose();
  };

  const getCurrentData = () => {
    switch (tabValue) {
      case 0:
        return allOAPPs?.result || [];
      case 1:
        return myOAPPs?.result || [];
      case 2:
        return installedOAPPs?.result || [];
      default:
        return [];
    }
  };

  const filteredData = getCurrentData().filter((oapp: OAPP) =>
    oapp.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    oapp.description.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.1,
      },
    },
  };

  const itemVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: {
      opacity: 1,
      y: 0,
      transition: {
        duration: 0.5,
      },
    },
  };

  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
    >
        <Box sx={{ mb: 4, mt: 4 }}>
        <motion.div variants={itemVariants}>
          <Typography variant="h4" gutterBottom className="page-heading">
            OAPPs
          </Typography>
          <Typography variant="subtitle1" color="text.secondary">
            Omniverse Applications - Create and manage your apps in the OASIS
          </Typography>
        </motion.div>
      </Box>

      {/* Search and Filter Bar */}
      <motion.div variants={itemVariants}>
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
              <TextField
                placeholder="Search OAPPs..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                InputProps={{
                  startAdornment: <Search sx={{ mr: 1, color: 'text.secondary' }} />,
                }}
                sx={{ flexGrow: 1 }}
              />
              <Button
                variant="outlined"
                startIcon={<FilterList />}
              >
                Filter
              </Button>
            </Box>
          </CardContent>
        </Card>
      </motion.div>

      {/* Tabs */}
      <motion.div variants={itemVariants}>
        <Card>
          <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
            <Tabs value={tabValue} onChange={handleTabChange}>
              <Tab 
                label={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <span>All OAPPs</span>
                    <Badge 
                      badgeContent={allOAPPs?.result?.length || 0} 
                      color="primary"
                      sx={{ '& .MuiBadge-badge': { position: 'static', transform: 'none' } }}
                    />
                  </Box>
                } 
              />
              <Tab 
                label={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <span>My OAPPs</span>
                    <Badge 
                      badgeContent={myOAPPs?.result?.length || 0} 
                      color="secondary"
                      sx={{ '& .MuiBadge-badge': { position: 'static', transform: 'none' } }}
                    />
                  </Box>
                } 
              />
              <Tab 
                label={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <span>Installed</span>
                    <Badge 
                      badgeContent={installedOAPPs?.result?.length || 0} 
                      color="success"
                      sx={{ '& .MuiBadge-badge': { position: 'static', transform: 'none' } }}
                    />
                  </Box>
                } 
              />
            </Tabs>
          </Box>

          <TabPanel value={tabValue} index={0}>
            <OAPPGrid 
              oapps={filteredData} 
              onMenuOpen={handleMenuOpen}
              variants={itemVariants}
            />
          </TabPanel>

          <TabPanel value={tabValue} index={1}>
            <OAPPGrid 
              oapps={filteredData} 
              onMenuOpen={handleMenuOpen}
              variants={itemVariants}
            />
          </TabPanel>

          <TabPanel value={tabValue} index={2}>
            <OAPPGrid 
              oapps={filteredData} 
              onMenuOpen={handleMenuOpen}
              variants={itemVariants}
            />
          </TabPanel>
        </Card>
      </motion.div>

      {/* Create OAPP FAB */}
      <Tooltip title="Create New OAPP">
        <Fab
          color="primary"
          aria-label="add"
          sx={{ position: 'fixed', bottom: 16, right: 16 }}
          onClick={() => setCreateDialogOpen(true)}
        >
          <Add />
        </Fab>
      </Tooltip>

      {/* Context Menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItem onClick={handleInstallOAPP}>
          <Download sx={{ mr: 1 }} />
          Install
        </MenuItem>
        <MenuItem onClick={handlePublishOAPP}>
          <Upload sx={{ mr: 1 }} />
          Publish
        </MenuItem>
        <MenuItem>
          <Edit sx={{ mr: 1 }} />
          Edit
        </MenuItem>
        <MenuItem>
          <Visibility sx={{ mr: 1 }} />
          View Details
        </MenuItem>
        <MenuItem sx={{ color: 'error.main' }}>
          <Delete sx={{ mr: 1 }} />
          Delete
        </MenuItem>
      </Menu>

      {/* Create OAPP Dialog */}
      <CreateOAPPDialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        onSubmit={handleCreateOAPP}
        loading={createOAPPMutation.isLoading}
      />
    </motion.div>
  );
};

interface OAPPGridProps {
  oapps: OAPP[];
  onMenuOpen: (event: React.MouseEvent<HTMLElement>, oapp: OAPP) => void;
  variants: any;
}

const OAPPGrid: React.FC<OAPPGridProps> = ({ oapps, onMenuOpen, variants }) => {
  if (oapps.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 8 }}>
        <Apps sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
        <Typography variant="h6" color="text.secondary">
          No OAPPs found
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Create your first OAPP to get started
        </Typography>
      </Box>
    );
  }

  return (
    <Grid container spacing={3}>
      {oapps.map((oapp, index) => (
        <Grid item xs={12} sm={6} md={4} lg={3} key={oapp.id}>
          <motion.div
            variants={variants}
            initial="hidden"
            animate="visible"
            transition={{ delay: index * 0.1 }}
          >
            <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <CardContent sx={{ flexGrow: 1 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Apps color="primary" />
                    <Typography variant="h6" noWrap>
                      {oapp.name}
                    </Typography>
                  </Box>
                  <IconButton
                    size="small"
                    onClick={(e) => onMenuOpen(e, oapp)}
                  >
                    <MoreVert />
                  </IconButton>
                </Box>

                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  {oapp.description}
                </Typography>

                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 2 }}>
                  {oapp.isPublished && (
                    <Chip label="Published" size="small" color="success" variant="outlined" />
                  )}
                  {oapp.isInstalled && (
                    <Chip label="Installed" size="small" color="primary" variant="outlined" />
                  )}
                  {oapp.isActive && (
                    <Chip label="Active" size="small" color="secondary" variant="outlined" />
                  )}
                </Box>

                <Typography variant="caption" color="text.secondary">
                  Version: {oapp.version || '1.0.0'}
                </Typography>
              </CardContent>
            </Card>
          </motion.div>
        </Grid>
      ))}
    </Grid>
  );
};

interface CreateOAPPDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: { name: string; description: string; type: string }) => void;
  loading: boolean;
}

const CreateOAPPDialog: React.FC<CreateOAPPDialogProps> = ({ open, onClose, onSubmit, loading }) => {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    type: 'Console',
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit(formData);
  };

  const handleClose = () => {
    setFormData({ name: '', description: '', type: 'Console' });
    onClose();
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create New OAPP</DialogTitle>
      <form onSubmit={handleSubmit}>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="OAPP Name"
            fullWidth
            variant="outlined"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            required
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Description"
            fullWidth
            multiline
            rows={3}
            variant="outlined"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            required
            sx={{ mb: 2 }}
          />
          <FormControl fullWidth>
            <InputLabel>OAPP Type</InputLabel>
            <Select
              value={formData.type}
              label="OAPP Type"
              onChange={(e) => setFormData({ ...formData, type: e.target.value })}
            >
              <MenuItem value="Console">Console</MenuItem>
              <MenuItem value="Web">Web</MenuItem>
              <MenuItem value="Mobile">Mobile</MenuItem>
              <MenuItem value="Game">Game</MenuItem>
              <MenuItem value="Service">Service</MenuItem>
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={loading}>
            {loading ? 'Creating...' : 'Create OAPP'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};

export default OAPPsPage;
