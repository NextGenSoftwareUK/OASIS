import React, { useState } from 'react';
import {
  Box, Typography, Card, CardContent, Grid, Button, Chip, TextField,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
  IconButton, Tooltip, Alert, AlertTitle, CircularProgress, Divider,
  List, ListItem, ListItemText, ListItemIcon, ListItemSecondaryAction,
  Dialog, DialogTitle, DialogContent, DialogActions, DialogContentText,
  FormControl, InputLabel, Select, MenuItem, Avatar, LinearProgress,
  Tabs, Tab, Badge, Switch, FormControlLabel
} from '@mui/material';
import {
  Storage, PlayArrow, Stop, Settings, Speed, Refresh,
  TrendingUp, HealthAndSafety, Assessment, Info,
  BugReport, Memory, NetworkCheck, Cloud, CheckCircle, Error
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useQueryClient } from 'react-query';
import { onodeService } from '../services/core/onodeService';

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
      id={`onode-tabpanel-${index}`}
      aria-labelledby={`onode-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const ONODEPage: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  const [isStarting, setIsStarting] = useState(false);
  const [isStopping, setIsStopping] = useState(false);
  const [isRestarting, setIsRestarting] = useState(false);
  const queryClient = useQueryClient();

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  // Queries
  const { data: statusData, isLoading: statusLoading } = useQuery(
    'onode-status',
    () => onodeService.getStatus(),
    { refetchInterval: 5000 }
  );

  const { data: providersData, isLoading: providersLoading } = useQuery(
    'onode-providers',
    () => onodeService.getProviders()
  );

  const { data: configData, isLoading: configLoading } = useQuery(
    'onode-config',
    () => onodeService.getConfig()
  );

  const { data: logsData, isLoading: logsLoading } = useQuery(
    'onode-logs',
    () => onodeService.getLogs(50)
  );

  const { data: metricsData, isLoading: metricsLoading } = useQuery(
    'onode-metrics',
    () => onodeService.getMetrics()
  );

  const { data: healthData, isLoading: healthLoading } = useQuery(
    'onode-health',
    () => onodeService.getHealth()
  );

  const { data: statisticsData, isLoading: statisticsLoading } = useQuery(
    'onode-statistics',
    () => onodeService.getStatistics()
  );

  const handleStart = async () => {
    setIsStarting(true);
    try {
      await onodeService.start();
      queryClient.invalidateQueries('onode-status');
      queryClient.invalidateQueries('onode-health');
    } catch (error) {
      console.error('Failed to start ONODE:', error);
    } finally {
      setIsStarting(false);
    }
  };

  const handleStop = async () => {
    setIsStopping(true);
    try {
      await onodeService.stop();
      queryClient.invalidateQueries('onode-status');
      queryClient.invalidateQueries('onode-health');
    } catch (error) {
      console.error('Failed to stop ONODE:', error);
    } finally {
      setIsStopping(false);
    }
  };

  const handleRestart = async () => {
    setIsRestarting(true);
    try {
      await onodeService.restart();
      queryClient.invalidateQueries('onode-status');
      queryClient.invalidateQueries('onode-health');
    } catch (error) {
      console.error('Failed to restart ONODE:', error);
    } finally {
      setIsRestarting(false);
    }
  };

  const handleStartProvider = async (providerId: string) => {
    try {
      await onodeService.startProvider(providerId);
      queryClient.invalidateQueries('onode-providers');
    } catch (error) {
      console.error('Failed to start provider:', error);
    }
  };

  const handleStopProvider = async (providerId: string) => {
    try {
      await onodeService.stopProvider(providerId);
      queryClient.invalidateQueries('onode-providers');
    } catch (error) {
      console.error('Failed to stop provider:', error);
    }
  };

  const handleRefresh = () => {
    queryClient.invalidateQueries('onode-status');
    queryClient.invalidateQueries('onode-providers');
    queryClient.invalidateQueries('onode-logs');
    queryClient.invalidateQueries('onode-metrics');
    queryClient.invalidateQueries('onode-health');
    queryClient.invalidateQueries('onode-statistics');
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
      case 'healthy': return 'success';
      case 'inactive':
      case 'unhealthy': return 'error';
      default: return 'default';
    }
  };

  const getLogLevelColor = (level: string) => {
    switch (level) {
      case 'error': return 'error';
      case 'warn': return 'warning';
      case 'info': return 'info';
      case 'debug': return 'default';
      default: return 'default';
    }
  };

  const getProviderIcon = (type: string) => {
    switch (type) {
      case 'Holochain': return <Cloud />;
      case 'Ethereum': return <NetworkCheck />;
      case 'IPFS': return <Storage />;
      default: return <Storage />;
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        {/* Header */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Typography variant="h4" component="h1" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Storage color="primary" />
            ONODE Management
          </Typography>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={handleRefresh}
            >
              Refresh
            </Button>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={handleRestart}
              disabled={isRestarting}
            >
              {isRestarting ? <CircularProgress size={20} /> : 'Restart'}
            </Button>
            {statusData?.result?.isRunning ? (
              <Button
                variant="contained"
                color="error"
                startIcon={<Stop />}
                onClick={handleStop}
                disabled={isStopping}
              >
                {isStopping ? <CircularProgress size={20} /> : 'Stop ONODE'}
              </Button>
            ) : (
              <Button
                variant="contained"
                color="success"
                startIcon={<PlayArrow />}
                onClick={handleStart}
                disabled={isStarting}
              >
                {isStarting ? <CircularProgress size={20} /> : 'Start ONODE'}
              </Button>
            )}
          </Box>
        </Box>

        {/* Status Overview */}
        {statusData?.result && (
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Storage color="primary" />
                ONODE Status
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="h4" color={statusData.result.isRunning ? 'success.main' : 'error.main'}>
                      {statusData.result.isRunning ? 'Running' : 'Stopped'}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Status
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="h4" color="primary">
                      {statusData.result.version}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Version
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="h4" color="primary">
                      {statusData.result.uptime}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Uptime
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="h4" color="primary">
                      {statusData.result.nodes?.active}/{statusData.result.nodes?.total}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Active Nodes
                    </Typography>
                  </Box>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        )}

        {/* Health Status */}
        {healthData?.result && (
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <HealthAndSafety color="primary" />
                Health Status
              </Typography>
              <Alert 
                severity={healthData.result.status === 'healthy' ? 'success' : 'error'}
                sx={{ mb: 2 }}
              >
                <AlertTitle>
                  {healthData.result.status === 'healthy' ? 'All Systems Healthy' : 'System Issues Detected'}
                </AlertTitle>
                Last checked: {new Date(healthData.result.lastChecked).toLocaleString()}
              </Alert>
              <Grid container spacing={2}>
                {Object.entries(healthData.result.checks || {}).map(([check, status]) => (
                  <Grid item xs={6} sm={3} key={check}>
                    <Box sx={{ textAlign: 'center' }}>
                      <Chip 
                        label={status as string}
                        color={getStatusColor(status as string) as any}
                        variant="outlined"
                      />
                      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                        {check.charAt(0).toUpperCase() + check.slice(1)}
                      </Typography>
                    </Box>
                  </Grid>
                ))}
              </Grid>
            </CardContent>
          </Card>
        )}

        {/* Tabs */}
        <Card>
          <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
            <Tabs value={tabValue} onChange={handleTabChange} aria-label="ONODE tabs">
              <Tab label="Overview" />
              <Tab label="Providers" />
              <Tab label="Logs" />
              <Tab label="Metrics" />
              <Tab label="Configuration" />
            </Tabs>
          </Box>

          {/* Overview Tab */}
          <TabPanel value={tabValue} index={0}>
            <Grid container spacing={3}>
              {/* Statistics */}
              <Grid item xs={12} md={6}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Assessment color="primary" />
                      Statistics
                    </Typography>
                    {statisticsLoading ? (
                      <CircularProgress />
                    ) : statisticsData?.result ? (
                      <Grid container spacing={2}>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h5" color="primary">
                              {statisticsData.result.requests?.toLocaleString()}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Total Requests
                            </Typography>
                          </Box>
                        </Grid>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h5" color="primary">
                              {statisticsData.result.errors}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Errors
                            </Typography>
                          </Box>
                        </Grid>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h5" color="primary">
                              {statisticsData.result.averageResponseTime}ms
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Avg Response Time
                            </Typography>
                          </Box>
                        </Grid>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h5" color="primary">
                              {statisticsData.result.currentConnections}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Current Connections
                            </Typography>
                          </Box>
                        </Grid>
                      </Grid>
                    ) : (
                      <Typography color="text.secondary">No statistics data available</Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>

              {/* Performance Metrics */}
              <Grid item xs={12} md={6}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Speed color="primary" />
                      Performance
                    </Typography>
                    {metricsLoading ? (
                      <CircularProgress />
                    ) : metricsData?.result ? (
                      <Box>
                        <Box sx={{ mb: 2 }}>
                          <Typography variant="body2" color="text.secondary" gutterBottom>
                            CPU Usage
                          </Typography>
                          <LinearProgress 
                            variant="determinate" 
                            value={metricsData.result.performance?.cpu} 
                            sx={{ mb: 1 }}
                          />
                          <Typography variant="body2" color="text.secondary">
                            {metricsData.result.performance?.cpu}%
                          </Typography>
                        </Box>
                        <Box sx={{ mb: 2 }}>
                          <Typography variant="body2" color="text.secondary" gutterBottom>
                            Memory Usage
                          </Typography>
                          <LinearProgress 
                            variant="determinate" 
                            value={(metricsData.result.performance?.memory / 1024) * 100} 
                            sx={{ mb: 1 }}
                          />
                          <Typography variant="body2" color="text.secondary">
                            {metricsData.result.performance?.memory} MB
                          </Typography>
                        </Box>
                        <Box sx={{ mb: 2 }}>
                          <Typography variant="body2" color="text.secondary" gutterBottom>
                            Network Usage
                          </Typography>
                          <LinearProgress 
                            variant="determinate" 
                            value={metricsData.result.performance?.network} 
                            sx={{ mb: 1 }}
                          />
                          <Typography variant="body2" color="text.secondary">
                            {metricsData.result.performance?.network} Mbps
                          </Typography>
                        </Box>
                      </Box>
                    ) : (
                      <Typography color="text.secondary">No performance data available</Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </TabPanel>

          {/* Providers Tab */}
          <TabPanel value={tabValue} index={1}>
            <Typography variant="h6" gutterBottom>Provider Management</Typography>
            {providersLoading ? (
              <CircularProgress />
            ) : providersData?.result ? (
              <Grid container spacing={2}>
                {providersData.result.map((provider: any) => (
                  <Grid item xs={12} md={6} key={provider.id}>
                    <Card>
                      <CardContent>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            {getProviderIcon(provider.type)}
                            <Typography variant="h6">{provider.name}</Typography>
                          </Box>
                          <Chip 
                            label={provider.status}
                            color={getStatusColor(provider.status) as any}
                            size="small"
                          />
                        </Box>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                          Type: {provider.type} | Version: {provider.version}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                          Uptime: {provider.uptime}
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1 }}>
                          <Button
                            size="small"
                            variant="outlined"
                            color="success"
                            onClick={() => handleStartProvider(provider.id)}
                            disabled={provider.status === 'active'}
                          >
                            Start
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            color="error"
                            onClick={() => handleStopProvider(provider.id)}
                            disabled={provider.status === 'inactive'}
                          >
                            Stop
                          </Button>
                        </Box>
                      </CardContent>
                    </Card>
                  </Grid>
                ))}
              </Grid>
            ) : (
              <Typography color="text.secondary">No providers available</Typography>
            )}
          </TabPanel>

          {/* Logs Tab */}
          <TabPanel value={tabValue} index={2}>
            <Typography variant="h6" gutterBottom>System Logs</Typography>
            {logsLoading ? (
              <CircularProgress />
            ) : logsData?.result ? (
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Level</TableCell>
                      <TableCell>Message</TableCell>
                      <TableCell>Source</TableCell>
                      <TableCell>Timestamp</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {logsData.result.map((log: any) => (
                      <TableRow key={log.id}>
                        <TableCell>
                          <Chip 
                            label={log.level}
                            color={getLogLevelColor(log.level) as any}
                            size="small"
                          />
                        </TableCell>
                        <TableCell>{log.message}</TableCell>
                        <TableCell>{log.source}</TableCell>
                        <TableCell>
                          {new Date(log.timestamp).toLocaleString()}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            ) : (
              <Typography color="text.secondary">No logs available</Typography>
            )}
          </TabPanel>

          {/* Metrics Tab */}
          <TabPanel value={tabValue} index={3}>
            <Typography variant="h6" gutterBottom>Detailed Metrics</Typography>
            {metricsLoading ? (
              <CircularProgress />
            ) : metricsData?.result ? (
              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <Card>
                    <CardContent>
                      <Typography variant="h6" gutterBottom>Request Metrics</Typography>
                      <Grid container spacing={2}>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h4" color="primary">
                              {metricsData.result.requests?.total?.toLocaleString()}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Total Requests
                            </Typography>
                          </Box>
                        </Grid>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h4" color="success.main">
                              {metricsData.result.requests?.successful?.toLocaleString()}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Successful
                            </Typography>
                          </Box>
                        </Grid>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h4" color="error.main">
                              {metricsData.result.requests?.failed}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Failed
                            </Typography>
                          </Box>
                        </Grid>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h4" color="primary">
                              {metricsData.result.requests?.rate}/s
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Rate
                            </Typography>
                          </Box>
                        </Grid>
                      </Grid>
                    </CardContent>
                  </Card>
                </Grid>

                <Grid item xs={12} md={6}>
                  <Card>
                    <CardContent>
                      <Typography variant="h6" gutterBottom>Provider Metrics</Typography>
                      {Object.entries(metricsData.result.providers || {}).map(([provider, data]: [string, any]) => (
                        <Box key={provider} sx={{ mb: 2 }}>
                          <Typography variant="subtitle2" sx={{ textTransform: 'capitalize' }}>
                            {provider}
                          </Typography>
                          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                            <Typography variant="body2" color="text.secondary">
                              Requests: {data.requests}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Latency: {data.latency}ms
                            </Typography>
                          </Box>
                          <LinearProgress 
                            variant="determinate" 
                            value={(data.requests / 5000) * 100} 
                            sx={{ height: 4, borderRadius: 2 }}
                          />
                        </Box>
                      ))}
                    </CardContent>
                  </Card>
                </Grid>
              </Grid>
            ) : (
              <Typography color="text.secondary">No metrics data available</Typography>
            )}
          </TabPanel>

          {/* Configuration Tab */}
          <TabPanel value={tabValue} index={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Settings color="primary" />
                  ONODE Configuration
                </Typography>
                {configLoading ? (
                  <CircularProgress />
                ) : configData?.result ? (
                  <Grid container spacing={3}>
                    <Grid item xs={12} md={6}>
                      <Typography variant="subtitle1" gutterBottom>Basic Settings</Typography>
                      <Grid container spacing={2}>
                        <Grid item xs={6}>
                          <TextField
                            fullWidth
                            label="Port"
                            value={configData.result.port}
                            disabled
                            variant="outlined"
                          />
                        </Grid>
                        <Grid item xs={6}>
                          <TextField
                            fullWidth
                            label="Host"
                            value={configData.result.host}
                            disabled
                            variant="outlined"
                          />
                        </Grid>
                      </Grid>
                    </Grid>

                    <Grid item xs={12} md={6}>
                      <Typography variant="subtitle1" gutterBottom>Logging Settings</Typography>
                      <Grid container spacing={2}>
                        <Grid item xs={6}>
                          <TextField
                            fullWidth
                            label="Log Level"
                            value={configData.result.logging?.level}
                            disabled
                            variant="outlined"
                          />
                        </Grid>
                        <Grid item xs={6}>
                          <TextField
                            fullWidth
                            label="Log File"
                            value={configData.result.logging?.file}
                            disabled
                            variant="outlined"
                          />
                        </Grid>
                      </Grid>
                    </Grid>

                    <Grid item xs={12}>
                      <Typography variant="subtitle1" gutterBottom>Provider Settings</Typography>
                      <Grid container spacing={2}>
                        {Object.entries(configData.result.providers || {}).map(([provider, config]: [string, any]) => (
                          <Grid item xs={12} md={4} key={provider}>
                            <Card variant="outlined">
                              <CardContent>
                                <Typography variant="h6" sx={{ textTransform: 'capitalize', mb: 2 }}>
                                  {provider}
                                </Typography>
                                <FormControlLabel
                                  control={<Switch checked={config.enabled} disabled />}
                                  label="Enabled"
                                />
                                {config.port && (
                                  <TextField
                                    fullWidth
                                    label="Port"
                                    value={config.port}
                                    disabled
                                    size="small"
                                    sx={{ mt: 1 }}
                                  />
                                )}
                                {config.network && (
                                  <TextField
                                    fullWidth
                                    label="Network"
                                    value={config.network}
                                    disabled
                                    size="small"
                                    sx={{ mt: 1 }}
                                  />
                                )}
                              </CardContent>
                            </Card>
                          </Grid>
                        ))}
                      </Grid>
                    </Grid>
                  </Grid>
                ) : (
                  <Typography color="text.secondary">No configuration data available</Typography>
                )}
              </CardContent>
            </Card>
          </TabPanel>
        </Card>
      </motion.div>
    </Box>
  );
};

export default ONODEPage;
