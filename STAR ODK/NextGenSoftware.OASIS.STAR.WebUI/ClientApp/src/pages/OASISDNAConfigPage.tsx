import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  TextField,
  Switch,
  FormControlLabel,
  Button,
  Alert,
  CircularProgress,
  Tabs,
  Tab,
  Paper,
  Divider,
  Chip,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Save as SaveIcon,
  Refresh as RefreshIcon,
  Info as InfoIcon,
  Settings as SettingsIcon,
  NetworkCheck as NetworkIcon,
  Storage as StorageIcon,
  Security as SecurityIcon,
  Speed as PerformanceIcon,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { toast } from 'react-toastify';
import { onetService } from '../services/core/onetService';
import { onodeService } from '../services/core/onodeService';

interface OASISDNAConfig {
  OASIS: {
    NetworkId: string;
    EnableLogging: boolean;
    LogLevel: string;
    StatsCacheEnabled: boolean;
    StatsCacheTtlSeconds: number;
    OASISAPIURL: string;
    SettingsLookupHolonId: string;
  };
  ONET: {
    NetworkId: string;
    EnableP2P: boolean;
    MaxConnections: number;
    DiscoveryTimeout: number;
  };
  ONODE: {
    NodeId: string;
    EnableNode: boolean;
    Port: number;
    MaxPeers: number;
    SyncInterval: number;
  };
}

const OASISDNAConfigPage: React.FC = () => {
  const [config, setConfig] = useState<OASISDNAConfig>({
    OASIS: {
      NetworkId: 'oasis-network',
      EnableLogging: true,
      LogLevel: 'Info',
      StatsCacheEnabled: false,
      StatsCacheTtlSeconds: 45,
      OASISAPIURL: 'https://api.oasis.network',
      SettingsLookupHolonId: '',
    },
    ONET: {
      NetworkId: 'onet-network',
      EnableP2P: true,
      MaxConnections: 100,
      DiscoveryTimeout: 30,
    },
    ONODE: {
      NodeId: 'onode-001',
      EnableNode: true,
      Port: 8080,
      MaxPeers: 50,
      SyncInterval: 60,
    },
  });
  const [activeTab, setActiveTab] = useState(0);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [showInfoBar, setShowInfoBar] = useState(true);

  useEffect(() => {
    loadConfig();
  }, []);

  const loadConfig = async () => {
    setLoading(true);
    try {
      // In a real implementation, this would load from the API
      // For now, we'll use the default config
      console.log('Loading OASISDNA configuration...');
    } catch (error) {
      console.error('Error loading config:', error);
      toast.error('Failed to load configuration');
    } finally {
      setLoading(false);
    }
  };

  const saveConfig = async () => {
    setSaving(true);
    try {
      // In a real implementation, this would save to the API
      console.log('Saving OASISDNA configuration:', config);
      toast.success('Configuration saved successfully!');
    } catch (error) {
      console.error('Error saving config:', error);
      toast.error('Failed to save configuration');
    } finally {
      setSaving(false);
    }
  };

  const handleConfigChange = (section: keyof OASISDNAConfig, field: string, value: any) => {
    setConfig(prev => ({
      ...prev,
      [section]: {
        ...prev[section],
        [field]: value,
      },
    }));
  };

  const TabPanel: React.FC<{ children: React.ReactNode; value: number; index: number }> = ({ children, value, index }) => (
    <div hidden={value !== index}>
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      transition={{ duration: 0.3 }}
    >
      <Box sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
          <SettingsIcon sx={{ mr: 2, fontSize: 32, color: 'primary.main' }} />
          <Typography variant="h4" component="h1">
            OASISDNA Configuration
          </Typography>
        </Box>

        {showInfoBar && (
          <Box sx={{ mt: 1, p: 2, bgcolor: '#0d47a1', color: 'white', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
            <InfoIcon sx={{ color: 'white' }} />
            <Typography variant="body2" sx={{ color: 'white', flexGrow: 1 }}>
              Configure OASIS DNA settings for ONET (P2P Network) and ONODE (OASIS Node) components. Changes are applied immediately.
            </Typography>
            <IconButton size="small" onClick={() => setShowInfoBar(false)} sx={{ color: 'white' }}>
              ×
            </IconButton>
          </Box>
        )}

        <Card>
          <CardContent>
            <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
              <Tabs value={activeTab} onChange={(e, newValue) => setActiveTab(newValue)}>
                <Tab label="OASIS Core" icon={<SettingsIcon />} />
                <Tab label="ONET (P2P)" icon={<NetworkIcon />} />
                <Tab label="ONODE (Node)" icon={<StorageIcon />} />
              </Tabs>
            </Box>

            {/* OASIS Core Configuration */}
            <TabPanel value={activeTab} index={0}>
              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Network ID"
                    value={config.OASIS.NetworkId}
                    onChange={(e) => handleConfigChange('OASIS', 'NetworkId', e.target.value)}
                    helperText="Unique identifier for the OASIS network"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="OASIS API URL"
                    value={config.OASIS.OASISAPIURL}
                    onChange={(e) => handleConfigChange('OASIS', 'OASISAPIURL', e.target.value)}
                    helperText="Base URL for OASIS API endpoints"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Settings Lookup Holon ID"
                    value={config.OASIS.SettingsLookupHolonId}
                    onChange={(e) => handleConfigChange('OASIS', 'SettingsLookupHolonId', e.target.value)}
                    helperText="Holon ID for settings storage"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    select
                    label="Log Level"
                    value={config.OASIS.LogLevel}
                    onChange={(e) => handleConfigChange('OASIS', 'LogLevel', e.target.value)}
                    SelectProps={{ native: true }}
                  >
                    <option value="Debug">Debug</option>
                    <option value="Info">Info</option>
                    <option value="Warning">Warning</option>
                    <option value="Error">Error</option>
                  </TextField>
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={config.OASIS.EnableLogging}
                        onChange={(e) => handleConfigChange('OASIS', 'EnableLogging', e.target.checked)}
                      />
                    }
                    label="Enable Logging"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={config.OASIS.StatsCacheEnabled}
                        onChange={(e) => handleConfigChange('OASIS', 'StatsCacheEnabled', e.target.checked)}
                      />
                    }
                    label="Enable Stats Caching"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    type="number"
                    label="Cache TTL (seconds)"
                    value={config.OASIS.StatsCacheTtlSeconds}
                    onChange={(e) => handleConfigChange('OASIS', 'StatsCacheTtlSeconds', parseInt(e.target.value))}
                    disabled={!config.OASIS.StatsCacheEnabled}
                  />
                </Grid>
              </Grid>
            </TabPanel>

            {/* ONET (P2P Network) Configuration */}
            <TabPanel value={activeTab} index={1}>
              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Network ID"
                    value={config.ONET.NetworkId}
                    onChange={(e) => handleConfigChange('ONET', 'NetworkId', e.target.value)}
                    helperText="Unique identifier for the P2P network"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    type="number"
                    label="Max Connections"
                    value={config.ONET.MaxConnections}
                    onChange={(e) => handleConfigChange('ONET', 'MaxConnections', parseInt(e.target.value))}
                    helperText="Maximum number of P2P connections"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    type="number"
                    label="Discovery Timeout (seconds)"
                    value={config.ONET.DiscoveryTimeout}
                    onChange={(e) => handleConfigChange('ONET', 'DiscoveryTimeout', parseInt(e.target.value))}
                    helperText="Timeout for node discovery"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={config.ONET.EnableP2P}
                        onChange={(e) => handleConfigChange('ONET', 'EnableP2P', e.target.checked)}
                      />
                    }
                    label="Enable P2P Network"
                  />
                </Grid>
              </Grid>
            </TabPanel>

            {/* ONODE (OASIS Node) Configuration */}
            <TabPanel value={activeTab} index={2}>
              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Node ID"
                    value={config.ONODE.NodeId}
                    onChange={(e) => handleConfigChange('ONODE', 'NodeId', e.target.value)}
                    helperText="Unique identifier for this OASIS node"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    type="number"
                    label="Port"
                    value={config.ONODE.Port}
                    onChange={(e) => handleConfigChange('ONODE', 'Port', parseInt(e.target.value))}
                    helperText="Port number for the OASIS node"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    type="number"
                    label="Max Peers"
                    value={config.ONODE.MaxPeers}
                    onChange={(e) => handleConfigChange('ONODE', 'MaxPeers', parseInt(e.target.value))}
                    helperText="Maximum number of peer connections"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    type="number"
                    label="Sync Interval (seconds)"
                    value={config.ONODE.SyncInterval}
                    onChange={(e) => handleConfigChange('ONODE', 'SyncInterval', parseInt(e.target.value))}
                    helperText="Interval for data synchronization"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={config.ONODE.EnableNode}
                        onChange={(e) => handleConfigChange('ONODE', 'EnableNode', e.target.checked)}
                      />
                    }
                    label="Enable OASIS Node"
                  />
                </Grid>
              </Grid>
            </TabPanel>

            <Divider sx={{ my: 3 }} />

            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Box sx={{ display: 'flex', gap: 1 }}>
                <Chip
                  icon={<SettingsIcon />}
                  label="OASIS Core"
                  color="primary"
                  variant="outlined"
                />
                <Chip
                  icon={<NetworkIcon />}
                  label="ONET P2P"
                  color="secondary"
                  variant="outlined"
                />
                <Chip
                  icon={<StorageIcon />}
                  label="ONODE Node"
                  color="success"
                  variant="outlined"
                />
              </Box>
              <Box sx={{ display: 'flex', gap: 2 }}>
                <Button
                  variant="outlined"
                  startIcon={<RefreshIcon />}
                  onClick={loadConfig}
                  disabled={loading}
                >
                  Refresh
                </Button>
                <Button
                  variant="contained"
                  startIcon={<SaveIcon />}
                  onClick={saveConfig}
                  disabled={saving}
                >
                  {saving ? <CircularProgress size={20} /> : 'Save Configuration'}
                </Button>
              </Box>
            </Box>
          </CardContent>
        </Card>
      </Box>
    </motion.div>
  );
};

export default OASISDNAConfigPage;