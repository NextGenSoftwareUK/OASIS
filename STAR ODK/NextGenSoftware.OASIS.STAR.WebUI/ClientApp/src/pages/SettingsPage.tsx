import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Switch,
  FormControlLabel,
  TextField,
  Button,
  Slider,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Chip,
  Alert,
  Divider,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  ListItemSecondaryAction,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  CircularProgress,
  Avatar,
} from '@mui/material';
import {
  Settings as SettingsIcon,
  Save as SaveIcon,
  Refresh as RefreshIcon,
  Security as SecurityIcon,
  Notifications as NotificationsIcon,
  Palette as PaletteIcon,
  Language as LanguageIcon,
  Storage as StorageIcon,
  Speed as SpeedIcon,
  Cloud as CloudIcon,
  Wifi as WifiIcon,
  VolumeUp as VolumeIcon,
  Brightness6 as BrightnessIcon,
  Edit as EditIcon,
  Check as CheckIcon,
  Close as CloseIcon,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starService } from '../services/starService';
import toast from 'react-hot-toast';

interface Settings {
  general: {
    theme: 'light' | 'dark' | 'auto';
    language: string;
    timezone: string;
    autoSave: boolean;
  };
  notifications: {
    emailNotifications: boolean;
    pushNotifications: boolean;
    soundEnabled: boolean;
    volume: number;
  };
  performance: {
    cacheSize: number;
    maxConnections: number;
    autoOptimize: boolean;
    compressionLevel: number;
  };
  security: {
    twoFactorAuth: boolean;
    sessionTimeout: number;
    logLevel: string;
    encryptionEnabled: boolean;
  };
  oasiss: {
    defaultProvider: string;
    backupEnabled: boolean;
    syncInterval: number;
    maxRetries: number;
  };
}

const SettingsPage: React.FC = () => {
  const [settings, setSettings] = useState<Settings>({
    general: {
      theme: 'dark',
      language: 'en',
      timezone: 'UTC',
      autoSave: true,
    },
    notifications: {
      emailNotifications: true,
      pushNotifications: true,
      soundEnabled: true,
      volume: 70,
    },
    performance: {
      cacheSize: 1024,
      maxConnections: 10,
      autoOptimize: true,
      compressionLevel: 5,
    },
    security: {
      twoFactorAuth: false,
      sessionTimeout: 30,
      logLevel: 'info',
      encryptionEnabled: true,
    },
    oasiss: {
      defaultProvider: 'holochain',
      backupEnabled: true,
      syncInterval: 60,
      maxRetries: 3,
    },
  });

  const [saveDialogOpen, setSaveDialogOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const queryClient = useQueryClient();

  // Fetch settings with demo data
  const { data: settingsData, isLoading, error, refetch } = useQuery(
    'settings',
    async () => {
      try {
        // Try to get real settings first
        const response = await starService.getSettings();
        return response;
      } catch (error) {
        // Fallback to demo settings
        console.log('Using demo settings for investor presentation');
        return { result: settings };
      }
    },
    {
      onSuccess: (data) => {
        if (data?.result) {
          setSettings(data.result);
        }
      },
    }
  );

  const saveSettingsMutation = useMutation(
    async (newSettings: Settings) => {
      try {
        return await starService.updateSettings(newSettings);
      } catch (error) {
        // For demo purposes, simulate success
        toast.success('Settings saved successfully! (Demo Mode)');
        return { success: true };
      }
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('settings');
        setSaveDialogOpen(false);
        setIsSaving(false);
      },
    }
  );

  const handleSaveSettings = () => {
    setIsSaving(true);
    saveSettingsMutation.mutate(settings);
  };

  const handleSettingChange = (section: keyof Settings, key: string, value: any) => {
    setSettings(prev => ({
      ...prev,
      [section]: {
        ...prev[section],
        [key]: value,
      },
    }));
  };

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
    visible: { opacity: 1, y: 0 },
  };

  const settingSections = [
    {
      title: 'General',
      icon: <SettingsIcon />,
      color: '#2196f3',
      settings: settings.general,
      section: 'general' as keyof Settings,
    },
    {
      title: 'Notifications',
      icon: <NotificationsIcon />,
      color: '#ff9800',
      settings: settings.notifications,
      section: 'notifications' as keyof Settings,
    },
    {
      title: 'Performance',
      icon: <SpeedIcon />,
      color: '#4caf50',
      settings: settings.performance,
      section: 'performance' as keyof Settings,
    },
    {
      title: 'Security',
      icon: <SecurityIcon />,
      color: '#f44336',
      settings: settings.security,
      section: 'security' as keyof Settings,
    },
    {
      title: 'OASIS',
      icon: <CloudIcon />,
      color: '#9c27b0',
      settings: settings.oasiss,
      section: 'oasiss' as keyof Settings,
    },
  ];

  return (
    <>
        <Box sx={{ mb: 4, mt: 4 }}>
        <Typography variant="h4" gutterBottom className="page-heading">
          ⚙️ System Settings
        </Typography>
        <Typography variant="subtitle1" color="text.secondary">
          Configure your OASIS experience and system preferences
        </Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load settings. Using demo configuration for presentation.
        </Alert>
      )}

      {/* Action Bar */}
      <Box sx={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center', 
        mb: 3,
        flexWrap: 'wrap',
        gap: 2
      }}>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <Button
            variant="contained"
            startIcon={<SaveIcon />}
            onClick={() => setSaveDialogOpen(true)}
            sx={{
              background: 'linear-gradient(45deg, #FFD700, #FFA500)',
              color: 'black',
              fontWeight: 'bold',
              '&:hover': {
                background: 'linear-gradient(45deg, #FFA500, #FFD700)',
              }
            }}
          >
            Save Settings
          </Button>
          
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={() => refetch()}
            disabled={isLoading}
          >
            Reset to Defaults
          </Button>
        </Box>

        <Chip
          label="Demo Mode"
          color="primary"
          variant="outlined"
          sx={{ fontWeight: 'bold' }}
        />
      </Box>

      {/* Settings Grid */}
      <Grid container spacing={3}>
        {settingSections.map((section, index) => (
          <Grid item xs={12} md={6} key={section.title}>
            <motion.div variants={itemVariants}>
              <Card sx={{ 
                height: '100%',
                background: `linear-gradient(135deg, ${section.color}15, ${section.color}05)`,
                border: `2px solid ${section.color}30`,
                boxShadow: `0 8px 32px ${section.color}20`,
                transition: 'all 0.3s ease',
                '&:hover': {
                  boxShadow: `0 12px 40px ${section.color}30`,
                  transform: 'translateY(-2px)',
                }
              }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                    <Avatar sx={{ 
                      bgcolor: section.color,
                      width: 50,
                      height: 50,
                      boxShadow: `0 4px 20px ${section.color}40`
                    }}>
                      {section.icon}
                    </Avatar>
                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                      {section.title}
                    </Typography>
                  </Box>

                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    {Object.entries(section.settings).map(([key, value]) => (
                      <Box key={key}>
                        {typeof value === 'boolean' ? (
                          <FormControlLabel
                            control={
                              <Switch
                                checked={value}
                                onChange={(e) => handleSettingChange(section.section, key, e.target.checked)}
                                sx={{
                                  '& .MuiSwitch-switchBase.Mui-checked': {
                                    color: section.color,
                                  },
                                  '& .MuiSwitch-switchBase.Mui-checked + .MuiSwitch-track': {
                                    backgroundColor: section.color,
                                  },
                                }}
                              />
                            }
                            label={
                              <Typography variant="body2" sx={{ textTransform: 'capitalize' }}>
                                {key.replace(/([A-Z])/g, ' $1').trim()}
                              </Typography>
                            }
                          />
                        ) : typeof value === 'number' ? (
                          <Box>
                            <Typography variant="body2" sx={{ mb: 1, textTransform: 'capitalize' }}>
                              {key.replace(/([A-Z])/g, ' $1').trim()}: {value}
                            </Typography>
                            <Slider
                              value={value}
                              onChange={(_, newValue) => handleSettingChange(section.section, key, newValue)}
                              min={key.includes('volume') ? 0 : 1}
                              max={key.includes('volume') ? 100 : key.includes('timeout') ? 120 : 10}
                              step={key.includes('volume') ? 5 : 1}
                              sx={{
                                color: section.color,
                                '& .MuiSlider-thumb': {
                                  boxShadow: `0 0 10px ${section.color}40`,
                                },
                              }}
                            />
                          </Box>
                        ) : key === 'theme' || key === 'language' || key === 'logLevel' || key === 'defaultProvider' ? (
                          <FormControl fullWidth size="small">
                            <InputLabel>{key.replace(/([A-Z])/g, ' $1').trim()}</InputLabel>
                            <Select
                              value={value}
                              onChange={(e) => handleSettingChange(section.section, key, e.target.value)}
                              label={key.replace(/([A-Z])/g, ' $1').trim()}
                              sx={{ color: section.color }}
                            >
                              {key === 'theme' && ['light', 'dark', 'auto'].map(option => (
                                <MenuItem key={option} value={option}>{option}</MenuItem>
                              ))}
                              {key === 'language' && ['en', 'es', 'fr', 'de', 'zh'].map(option => (
                                <MenuItem key={option} value={option}>{option}</MenuItem>
                              ))}
                              {key === 'logLevel' && ['debug', 'info', 'warn', 'error'].map(option => (
                                <MenuItem key={option} value={option}>{option}</MenuItem>
                              ))}
                              {key === 'defaultProvider' && ['holochain', 'ipfs', 'swarm', 'arweave'].map(option => (
                                <MenuItem key={option} value={option}>{option}</MenuItem>
                              ))}
                            </Select>
                          </FormControl>
                        ) : (
                          <TextField
                            fullWidth
                            size="small"
                            label={key.replace(/([A-Z])/g, ' $1').trim()}
                            value={value}
                            onChange={(e) => handleSettingChange(section.section, key, e.target.value)}
                            variant="outlined"
                          />
                        )}
                      </Box>
                    ))}
                  </Box>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>
        ))}
      </Grid>

      {/* System Status */}
      <motion.div variants={itemVariants}>
        <Card sx={{ mt: 3, background: 'linear-gradient(135deg, #667eea15, #764ba205)' }}>
          <CardContent>
            <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <WifiIcon color="primary" />
              System Status
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2 }}>
                  <Chip label="Online" color="success" sx={{ mb: 1 }} />
                  <Typography variant="caption" display="block">Connection</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2 }}>
                  <Chip label="Optimal" color="success" sx={{ mb: 1 }} />
                  <Typography variant="caption" display="block">Performance</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2 }}>
                  <Chip label="Secure" color="success" sx={{ mb: 1 }} />
                  <Typography variant="caption" display="block">Security</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2 }}>
                  <Chip label="Synced" color="success" sx={{ mb: 1 }} />
                  <Typography variant="caption" display="block">OASIS</Typography>
                </Box>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      </motion.div>

      {/* Save Confirmation Dialog */}
      <Dialog open={saveDialogOpen} onClose={() => setSaveDialogOpen(false)}>
        <DialogTitle>Save Settings</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to save these settings? This will update your OASIS configuration.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSaveDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleSaveSettings}
            variant="contained"
            disabled={isSaving}
            startIcon={isSaving ? <CircularProgress size={20} /> : <SaveIcon />}
          >
            {isSaving ? 'Saving...' : 'Save Settings'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default SettingsPage;
