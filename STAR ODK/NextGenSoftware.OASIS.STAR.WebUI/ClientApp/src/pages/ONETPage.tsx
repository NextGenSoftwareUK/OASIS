import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Button,
  Chip,
  LinearProgress,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Alert,
  CircularProgress,
  Paper,
  Divider,
  Tooltip,
} from '@mui/material';
import {
  PlayArrow as StartIcon,
  Stop as StopIcon,
  Refresh as RefreshIcon,
  Add as AddIcon,
  Remove as RemoveIcon,
  NetworkCheck as NetworkIcon,
  Info as InfoIcon,
  Speed as SpeedIcon,
  Storage as StorageIcon,
  Security as SecurityIcon,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { toast } from 'react-toastify';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { onetService, NetworkStatus, NetworkNode, NetworkStats, ConnectNodeRequest, DisconnectNodeRequest, BroadcastMessageRequest } from '../services/core/onetService';

const ONETPage: React.FC = () => {
  const [networkStatus, setNetworkStatus] = useState<NetworkStatus>({
    isRunning: false,
    connectedNodesCount: 0,
    networkId: 'onet-network',
    lastUpdated: new Date().toISOString(),
  });
  const [connectedNodes, setConnectedNodes] = useState<NetworkNode[]>([]);
  const [networkStats, setNetworkStats] = useState<NetworkStats>({
    totalNodes: 0,
    activeConnections: 0,
    messagesPerSecond: 0,
    averageLatency: 0,
    networkHealth: 0,
  });
  const [loading, setLoading] = useState(false);
  const [showInfoBar, setShowInfoBar] = useState(true);
  const [connectDialogOpen, setConnectDialogOpen] = useState(false);
  const [newNodeId, setNewNodeId] = useState('');
  const [newNodeAddress, setNewNodeAddress] = useState('');

  useEffect(() => {
    loadNetworkData();
    const interval = setInterval(loadNetworkData, 30000); // Refresh every 30 seconds
    return () => clearInterval(interval);
  }, []);

  const loadNetworkData = async () => {
    setLoading(true);
    try {
      console.log('Loading ONET network data...');
      
      // Load network status
      const statusResult = await onetService.getNetworkStatus();
      if (!statusResult.isError && statusResult.result) {
        setNetworkStatus(statusResult.result);
      }
      
      // Load connected nodes
      const nodesResult = await onetService.getConnectedNodes();
      if (!nodesResult.isError && nodesResult.result) {
        setConnectedNodes(nodesResult.result);
      }
      
      // Load network stats
      const statsResult = await onetService.getNetworkStats();
      if (!statsResult.isError && statsResult.result) {
        setNetworkStats({
          totalNodes: statsResult.result.totalNodes || 0,
          activeConnections: statsResult.result.activeConnections || 0,
          messagesPerSecond: statsResult.result.messagesPerSecond || 0,
          averageLatency: statsResult.result.averageLatency || 0,
          networkHealth: statsResult.result.networkHealth || 0,
        });
      }
    } catch (error) {
      console.error('Error loading network data:', error);
      toast.error('Failed to load network data');
    } finally {
      setLoading(false);
    }
  };

  const handleStartNetwork = async () => {
    try {
      console.log('Starting ONET network...');
      const result = await onetService.startNetwork();
      if (!result.isError) {
        toast.success('Network started successfully!');
        loadNetworkData();
      } else {
        toast.error(result.message || 'Failed to start network');
      }
    } catch (error) {
      console.error('Error starting network:', error);
      toast.error('Failed to start network');
    }
  };

  const handleStopNetwork = async () => {
    try {
      console.log('Stopping ONET network...');
      const result = await onetService.stopNetwork();
      if (!result.isError) {
        toast.success('Network stopped successfully!');
        loadNetworkData();
      } else {
        toast.error(result.message || 'Failed to stop network');
      }
    } catch (error) {
      console.error('Error stopping network:', error);
      toast.error('Failed to stop network');
    }
  };

  const handleConnectNode = async () => {
    if (!newNodeId || !newNodeAddress) {
      toast.error('Please enter both Node ID and Address');
      return;
    }

    try {
      console.log('Connecting to node:', newNodeId, newNodeAddress);
      const result = await onetService.connectToNode({
        nodeId: newNodeId,
        nodeAddress: newNodeAddress,
      });
      if (!result.isError) {
        toast.success(`Connected to node ${newNodeId}`);
        setConnectDialogOpen(false);
        setNewNodeId('');
        setNewNodeAddress('');
        loadNetworkData();
      } else {
        toast.error(result.message || 'Failed to connect to node');
      }
    } catch (error) {
      console.error('Error connecting to node:', error);
      toast.error('Failed to connect to node');
    }
  };

  const handleDisconnectNode = async (nodeId: string) => {
    try {
      console.log('Disconnecting from node:', nodeId);
      const result = await onetService.disconnectFromNode({ nodeId });
      if (!result.isError) {
        toast.success(`Disconnected from node ${nodeId}`);
        loadNetworkData();
      } else {
        toast.error(result.message || 'Failed to disconnect from node');
      }
    } catch (error) {
      console.error('Error disconnecting from node:', error);
      toast.error('Failed to disconnect from node');
    }
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      transition={{ duration: 0.3 }}
    >
      <Box sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
          <NetworkIcon sx={{ mr: 2, fontSize: 32, color: 'primary.main' }} />
          <Typography variant="h4" component="h1">
            ONET P2P Network Management
          </Typography>
        </Box>

        {showInfoBar && (
          <Box sx={{ mt: 1, p: 2, bgcolor: '#0d47a1', color: 'white', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
            <InfoIcon sx={{ color: 'white' }} />
            <Typography variant="body2" sx={{ color: 'white', flexGrow: 1 }}>
              Manage the OASIS P2P Network (ONET). Monitor connections, network status, and peer nodes.
            </Typography>
            <IconButton size="small" onClick={() => setShowInfoBar(false)} sx={{ color: 'white' }}>
              ×
            </IconButton>
          </Box>
        )}

        <Grid container spacing={3}>
          {/* Network Status */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <NetworkIcon sx={{ mr: 1, color: 'primary.main' }} />
                  <Typography variant="h6">Network Status</Typography>
                </Box>
                <Box sx={{ mb: 2 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Status</Typography>
                    <Chip
                      label={networkStatus.isRunning ? 'Running' : 'Stopped'}
                      color={networkStatus.isRunning ? 'success' : 'error'}
                      size="small"
                    />
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Connected Nodes</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {networkStatus.connectedNodesCount}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Network ID</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {networkStatus.networkId}
                    </Typography>
                  </Box>
                </Box>
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <Button
                    variant="contained"
                    startIcon={<StartIcon />}
                    onClick={handleStartNetwork}
                    disabled={networkStatus.isRunning}
                    size="small"
                  >
                    Start
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<StopIcon />}
                    onClick={handleStopNetwork}
                    disabled={!networkStatus.isRunning}
                    size="small"
                  >
                    Stop
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<RefreshIcon />}
                    onClick={loadNetworkData}
                    disabled={loading}
                    size="small"
                  >
                    Refresh
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {/* Network Statistics */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <SpeedIcon sx={{ mr: 1, color: 'secondary.main' }} />
                  <Typography variant="h6">Network Statistics</Typography>
                </Box>
                <Box sx={{ mb: 2 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Total Nodes</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {networkStats.totalNodes}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Network Health</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {networkStats.networkHealth}%
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Messages/sec</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {networkStats.messagesPerSecond}
                    </Typography>
                  </Box>
                </Box>
                {networkStatus.isRunning && (
                  <Box sx={{ mt: 2 }}>
                    <Typography variant="body2" color="text.secondary" gutterBottom>
                      Network Performance
                    </Typography>
                    <LinearProgress variant="determinate" value={75} sx={{ mb: 1 }} />
                    <Typography variant="caption" color="text.secondary">
                      75% efficiency
                    </Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* Connected Nodes */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <StorageIcon sx={{ mr: 1, color: 'success.main' }} />
                    <Typography variant="h6">Connected Nodes</Typography>
                  </Box>
                  <Button
                    variant="contained"
                    startIcon={<AddIcon />}
                    onClick={() => setConnectDialogOpen(true)}
                    size="small"
                  >
                    Connect Node
                  </Button>
                </Box>
                {connectedNodes.length === 0 ? (
                  <Alert severity="info">No nodes connected</Alert>
                ) : (
                  <List>
                    {connectedNodes.map((node, index) => (
                      <ListItem key={node.id} divider={index < connectedNodes.length - 1}>
                        <ListItemIcon>
                          <NetworkIcon color="primary" />
                        </ListItemIcon>
                        <ListItemText
                          primary={node.id}
                          secondary={`${node.address} • Last seen: ${new Date(node.lastSeen).toLocaleString()}`}
                        />
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Chip
                            label={node.status}
                            color="success"
                            size="small"
                          />
                          <Tooltip title="Disconnect">
                            <IconButton
                              size="small"
                              onClick={() => handleDisconnectNode(node.id)}
                            >
                              <RemoveIcon />
                            </IconButton>
                          </Tooltip>
                        </Box>
                      </ListItem>
                    ))}
                  </List>
                )}
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Connect Node Dialog */}
        <Dialog open={connectDialogOpen} onClose={() => setConnectDialogOpen(false)} maxWidth="sm" fullWidth>
          <DialogTitle>Connect to Node</DialogTitle>
          <DialogContent>
            <TextField
              fullWidth
              label="Node ID"
              value={newNodeId}
              onChange={(e) => setNewNodeId(e.target.value)}
              margin="normal"
              helperText="Unique identifier for the node"
            />
            <TextField
              fullWidth
              label="Node Address"
              value={newNodeAddress}
              onChange={(e) => setNewNodeAddress(e.target.value)}
              margin="normal"
              helperText="IP address and port (e.g., 192.168.1.100:8080)"
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setConnectDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleConnectNode} variant="contained">
              Connect
            </Button>
          </DialogActions>
        </Dialog>
      </Box>
    </motion.div>
  );
};

export default ONETPage;