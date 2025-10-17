import React, { useEffect, useState } from 'react';
import {
  Box, Typography, Card, CardContent, Grid, Button, Chip, TextField,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
  IconButton, Tooltip, Alert, AlertTitle, CircularProgress, Divider,
  List, ListItem, ListItemText, ListItemIcon, ListItemSecondaryAction,
  Dialog, DialogTitle, DialogContent, DialogActions, DialogContentText,
  FormControl, InputLabel, Select, MenuItem, Avatar, LinearProgress,
  Tabs, Tab, Badge
} from '@mui/material';
import {
  NetworkCheck, PlayArrow, Stop, Settings, Storage, Speed,
  TrendingUp, HealthAndSafety, Assessment, Refresh, Info,
  Message, Send, Group, Chat, Person, Wifi, WifiOff
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useQueryClient } from 'react-query';
import { onetService } from '../services/core/onetService';
import signalRService from '../services/signalRService';

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
      id={`onet-tabpanel-${index}`}
      aria-labelledby={`onet-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const ONETPage: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  const [isStarting, setIsStarting] = useState(false);
  const [isStopping, setIsStopping] = useState(false);
  const [messageDialogOpen, setMessageDialogOpen] = useState(false);
  const [messageTo, setMessageTo] = useState('');
  const [messageContent, setMessageContent] = useState('');
  const [messageType, setMessageType] = useState('text');
  const [liveMessages, setLiveMessages] = useState<any[]>([]);
  const queryClient = useQueryClient();

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  // Initialize SignalR and subscribe to live message events
  useEffect(() => {
    let isMounted = true;
    (async () => {
      try {
        await signalRService.start();
      } catch (e) {
        console.error('SignalR start failed:', e);
      }
    })();

    const onMessageReceived = ({ user, message }: any) => {
      if (!isMounted) return;
      setLiveMessages(prev => [
        {
          id: `live-${Date.now()}`,
          from: user,
          to: 'me',
          content: message,
          status: 'received',
          timestamp: new Date().toISOString()
        },
        ...prev
      ]);
    };

    signalRService.on('messageReceived', onMessageReceived);

    return () => {
      isMounted = false;
      signalRService.off('messageReceived', onMessageReceived);
    };
  }, []);

  // Queries
  const { data: statusData, isLoading: statusLoading } = useQuery(
    'onet-status',
    () => onetService.getStatus(),
    { refetchInterval: 5000 }
  );

  const { data: configData, isLoading: configLoading } = useQuery(
    'onet-config',
    () => onetService.getConfig()
  );

  const { data: nodesData, isLoading: nodesLoading } = useQuery(
    'onet-nodes',
    () => onetService.getNodes()
  );

  const { data: peersData, isLoading: peersLoading } = useQuery(
    'onet-peers',
    () => onetService.getPeers()
  );

  const { data: messagesData, isLoading: messagesLoading } = useQuery(
    'onet-messages',
    () => onetService.getMessages(20)
  );

  const { data: channelsData, isLoading: channelsLoading } = useQuery(
    'onet-channels',
    () => onetService.getChannels()
  );

  const { data: metricsData, isLoading: metricsLoading } = useQuery(
    'onet-metrics',
    () => onetService.getMetrics()
  );

  const { data: healthData, isLoading: healthLoading } = useQuery(
    'onet-health',
    () => onetService.getHealth()
  );

  const { data: statisticsData, isLoading: statisticsLoading } = useQuery(
    'onet-statistics',
    () => onetService.getStatistics()
  );

  const handleStart = async () => {
    setIsStarting(true);
    try {
      await onetService.start();
      queryClient.invalidateQueries('onet-status');
      queryClient.invalidateQueries('onet-health');
    } catch (error) {
      console.error('Failed to start ONET:', error);
    } finally {
      setIsStarting(false);
    }
  };

  const handleStop = async () => {
    setIsStopping(true);
    try {
      await onetService.stop();
      queryClient.invalidateQueries('onet-status');
      queryClient.invalidateQueries('onet-health');
    } catch (error) {
      console.error('Failed to stop ONET:', error);
    } finally {
      setIsStopping(false);
    }
  };

  const handleSendMessage = async () => {
    try {
      await onetService.sendMessage(messageTo, messageContent, messageType);
      // Also send via SignalR for realtime fanout where supported
      try { await signalRService.sendMessage(messageTo, messageContent); } catch {}
      queryClient.invalidateQueries('onet-messages');
      setMessageDialogOpen(false);
      setMessageTo('');
      setMessageContent('');
      setMessageType('text');
    } catch (error) {
      console.error('Failed to send message:', error);
    }
  };

  const handleJoinChannel = async (channelId: string) => {
    try {
      await onetService.joinChannel(channelId);
      queryClient.invalidateQueries('onet-channels');
    } catch (error) {
      console.error('Failed to join channel:', error);
    }
  };

  const handleLeaveChannel = async (channelId: string) => {
    try {
      await onetService.leaveChannel(channelId);
      queryClient.invalidateQueries('onet-channels');
    } catch (error) {
      console.error('Failed to leave channel:', error);
    }
  };

  const handleRefresh = () => {
    queryClient.invalidateQueries('onet-status');
    queryClient.invalidateQueries('onet-nodes');
    queryClient.invalidateQueries('onet-peers');
    queryClient.invalidateQueries('onet-messages');
    queryClient.invalidateQueries('onet-channels');
    queryClient.invalidateQueries('onet-metrics');
    queryClient.invalidateQueries('onet-health');
    queryClient.invalidateQueries('onet-statistics');
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
      case 'connected':
      case 'healthy': return 'success';
      case 'inactive':
      case 'disconnected':
      case 'unhealthy': return 'error';
      default: return 'default';
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
            <NetworkCheck color="primary" />
            ONET Management
          </Typography>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={handleRefresh}
            >
              Refresh
            </Button>
            {statusData?.result?.isRunning ? (
              <Button
                variant="contained"
                color="error"
                startIcon={<Stop />}
                onClick={handleStop}
                disabled={isStopping}
              >
                {isStopping ? <CircularProgress size={20} /> : 'Stop ONET'}
              </Button>
            ) : (
              <Button
                variant="contained"
                color="success"
                startIcon={<PlayArrow />}
                onClick={handleStart}
                disabled={isStarting}
              >
                {isStarting ? <CircularProgress size={20} /> : 'Start ONET'}
              </Button>
            )}
          </Box>
        </Box>

        {/* Status Overview */}
        {statusData?.result && (
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <NetworkCheck color="primary" />
                ONET Status
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
            <Tabs value={tabValue} onChange={handleTabChange} aria-label="ONET tabs">
              <Tab label="Overview" />
              <Tab label="Messages" />
              <Tab label="Channels" />
              <Tab label="Network" />
              <Tab label="Configuration" />
            </Tabs>
          </Box>

          {/* Overview Tab */}
          <TabPanel value={tabValue} index={0}>
            <Grid container spacing={3}>
              {/* Configuration */}
              <Grid item xs={12} md={6}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Settings color="primary" />
                      Configuration
                    </Typography>
                    {configLoading ? (
                      <CircularProgress />
                    ) : configData?.result ? (
                      <List dense>
                        <ListItem>
                          <ListItemText primary="Port" secondary={configData.result.port} />
                        </ListItem>
                        <ListItem>
                          <ListItemText primary="Host" secondary={configData.result.host} />
                        </ListItem>
                        <ListItem>
                          <ListItemText primary="Network" secondary={configData.result.network} />
                        </ListItem>
                        <ListItem>
                          <ListItemText primary="Protocol" secondary={configData.result.protocol} />
                        </ListItem>
                        <ListItem>
                          <ListItemText primary="Encryption" secondary={configData.result.encryption} />
                        </ListItem>
                      </List>
                    ) : (
                      <Typography color="text.secondary">No configuration data available</Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>

              {/* Metrics */}
              <Grid item xs={12} md={6}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Assessment color="primary" />
                      Network Metrics
                    </Typography>
                    {metricsLoading ? (
                      <CircularProgress />
                    ) : metricsData?.result ? (
                      <Grid container spacing={2}>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h5" color="primary">
                              {metricsData.result.messages?.total?.toLocaleString()}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Total Messages
                            </Typography>
                          </Box>
                        </Grid>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h5" color="primary">
                              {metricsData.result.channels?.total}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Total Channels
                            </Typography>
                          </Box>
                        </Grid>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h5" color="primary">
                              {metricsData.result.network?.activePeers}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Active Peers
                            </Typography>
                          </Box>
                        </Grid>
                        <Grid item xs={6}>
                          <Box sx={{ textAlign: 'center' }}>
                            <Typography variant="h5" color="primary">
                              {metricsData.result.network?.averageLatency}ms
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Avg Latency
                            </Typography>
                          </Box>
                        </Grid>
                      </Grid>
                    ) : (
                      <Typography color="text.secondary">No metrics data available</Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </TabPanel>

          {/* Messages Tab removed; use dedicated Messaging page */}
          <TabPanel value={tabValue} index={1}>
            <Typography color="text.secondary">Messaging has moved to the Messaging page.</Typography>
          </TabPanel>

          {/* Channels Tab */}
          <TabPanel value={tabValue} index={2}>
            <Typography variant="h6" gutterBottom>Channels</Typography>
            {channelsLoading ? (
              <CircularProgress />
            ) : channelsData?.result ? (
              <Grid container spacing={2}>
                {channelsData.result.map((channel: any) => (
                  <Grid item xs={12} md={6} key={channel.id}>
                    <Card>
                      <CardContent>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                          <Typography variant="h6">{channel.name}</Typography>
                          <Box sx={{ display: 'flex', gap: 1 }}>
                            <Button
                              size="small"
                              variant="outlined"
                              onClick={() => handleJoinChannel(channel.id)}
                            >
                              Join
                            </Button>
                            <Button
                              size="small"
                              variant="outlined"
                              color="error"
                              onClick={() => handleLeaveChannel(channel.id)}
                            >
                              Leave
                            </Button>
                          </Box>
                        </Box>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                          {channel.description}
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 2 }}>
                          <Chip 
                            icon={<Group />}
                            label={`${channel.members} members`}
                            size="small"
                          />
                          <Chip 
                            icon={<Message />}
                            label={`${channel.messages} messages`}
                            size="small"
                          />
                        </Box>
                      </CardContent>
                    </Card>
                  </Grid>
                ))}
              </Grid>
            ) : (
              <Typography color="text.secondary">No channels available</Typography>
            )}
          </TabPanel>

          {/* Network Tab */}
          <TabPanel value={tabValue} index={3}>
            <Grid container spacing={3}>
              {/* Nodes */}
              <Grid item xs={12} md={6}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <NetworkCheck color="primary" />
                      Network Nodes
                    </Typography>
                    {nodesLoading ? (
                      <CircularProgress />
                    ) : nodesData?.result ? (
                      <List dense>
                        {nodesData.result.map((node: any) => (
                          <ListItem key={node.id}>
                            <ListItemIcon>
                              <NetworkCheck color={node.status === 'active' ? 'success' : 'error'} />
                            </ListItemIcon>
                            <ListItemText
                              primary={node.name}
                              secondary={`Version: ${node.version} | Uptime: ${node.uptime}`}
                            />
                            <ListItemSecondaryAction>
                              <Chip 
                                label={node.status}
                                color={getStatusColor(node.status) as any}
                                size="small"
                              />
                            </ListItemSecondaryAction>
                          </ListItem>
                        ))}
                      </List>
                    ) : (
                      <Typography color="text.secondary">No nodes data available</Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>

              {/* Peers */}
              <Grid item xs={12} md={6}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Person color="primary" />
                      Network Peers
                    </Typography>
                    {peersLoading ? (
                      <CircularProgress />
                    ) : peersData?.result ? (
                      <List dense>
                        {peersData.result.map((peer: any) => (
                          <ListItem key={peer.id}>
                            <ListItemIcon>
                              {peer.status === 'connected' ? <Wifi color="success" /> : <WifiOff color="error" />}
                            </ListItemIcon>
                            <ListItemText
                              primary={peer.address}
                              secondary={`Latency: ${peer.latency}ms`}
                            />
                            <ListItemSecondaryAction>
                              <Chip 
                                label={peer.status}
                                color={getStatusColor(peer.status) as any}
                                size="small"
                              />
                            </ListItemSecondaryAction>
                          </ListItem>
                        ))}
                      </List>
                    ) : (
                      <Typography color="text.secondary">No peers data available</Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </TabPanel>

          {/* Configuration Tab */}
          <TabPanel value={tabValue} index={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Settings color="primary" />
                  Advanced Configuration
                </Typography>
                {configLoading ? (
                  <CircularProgress />
                ) : configData?.result ? (
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={6}>
                      <TextField
                        fullWidth
                        label="Port"
                        value={configData.result.port}
                        disabled
                        variant="outlined"
                      />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <TextField
                        fullWidth
                        label="Host"
                        value={configData.result.host}
                        disabled
                        variant="outlined"
                      />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <TextField
                        fullWidth
                        label="Network"
                        value={configData.result.network}
                        disabled
                        variant="outlined"
                      />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <TextField
                        fullWidth
                        label="Protocol"
                        value={configData.result.protocol}
                        disabled
                        variant="outlined"
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <TextField
                        fullWidth
                        label="Encryption"
                        value={configData.result.encryption}
                        disabled
                        variant="outlined"
                      />
                    </Grid>
                  </Grid>
                ) : (
                  <Typography color="text.secondary">No configuration data available</Typography>
                )}
              </CardContent>
            </Card>
          </TabPanel>
        </Card>

        {/* Send Message Dialog removed */}
      </motion.div>
    </Box>
  );
};

export default ONETPage;
