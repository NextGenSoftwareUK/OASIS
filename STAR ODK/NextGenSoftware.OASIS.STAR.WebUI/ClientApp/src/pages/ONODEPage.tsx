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
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from '@mui/material';
import {
  PlayArrow as StartIcon,
  Stop as StopIcon,
  Refresh as RefreshIcon,
  RestartAlt as RestartIcon,
  Storage as StorageIcon,
  Info as InfoIcon,
  Speed as SpeedIcon,
  Security as SecurityIcon,
  Settings as SettingsIcon,
  Assessment as MetricsIcon,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { toast } from 'react-toastify';
import { onodeService, NodeStatus, NodeInfo, NodeMetrics, PeerNode, NodeStats } from '../services/core/onodeService';

interface NodeStatus {
  isRunning: boolean;
  connectedPeersCount: number;
  nodeId: string;
  lastUpdated: string;
  uptime: string;
}

interface NodeInfo {
  nodeId: string;
  version: string;
  platform: string;
  architecture: string;
  isRunning: boolean;
  connectedPeers: number;
  lastStarted: string;
}

interface NodeMetrics {
  cpuUsage: number;
  memoryUsage: number;
  diskUsage: number;
  networkIn: number;
  networkOut: number;
  connectedPeers: number;
  lastUpdated: string;
}

interface PeerNode {
  id: string;
  address: string;
  connectedAt: string;
  status: string;
  version: string;
}

const ONODEPage: React.FC = () => {
  const [nodeStatus, setNodeStatus] = useState<NodeStatus>({
    isRunning: false,
    connectedPeersCount: 0,
    nodeId: 'onode-001',
    lastUpdated: new Date().toISOString(),
    uptime: '0h 0m',
  });
  const [nodeInfo, setNodeInfo] = useState<NodeInfo>({
    nodeId: 'onode-001',
    version: '1.0.0',
    platform: 'Windows',
    architecture: 'x64',
    isRunning: false,
    connectedPeers: 0,
    lastStarted: new Date().toISOString(),
  });
  const [nodeMetrics, setNodeMetrics] = useState<NodeMetrics>({
    cpuUsage: 15.5,
    memoryUsage: 256.7,
    diskUsage: 1024.3,
    networkIn: 1024,
    networkOut: 2048,
    connectedPeers: 0,
    lastUpdated: new Date().toISOString(),
  });
  const [connectedPeers, setConnectedPeers] = useState<PeerNode[]>([]);
  const [nodeLogs, setNodeLogs] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [showInfoBar, setShowInfoBar] = useState(true);
  const [logsDialogOpen, setLogsDialogOpen] = useState(false);

  useEffect(() => {
    loadNodeData();
    const interval = setInterval(loadNodeData, 30000); // Refresh every 30 seconds
    return () => clearInterval(interval);
  }, []);

  const loadNodeData = async () => {
    setLoading(true);
    try {
      // In a real implementation, this would load from the API
      // For now, we'll use demo data
      console.log('Loading ONODE data...');
      
      // Simulate API calls
      setNodeStatus({
        isRunning: true,
        connectedPeersCount: 3,
        nodeId: 'onode-001',
        lastUpdated: new Date().toISOString(),
        uptime: '2h 30m',
      });

      setNodeInfo({
        nodeId: 'onode-001',
        version: '1.0.0',
        platform: 'Windows',
        architecture: 'x64',
        isRunning: true,
        connectedPeers: 3,
        lastStarted: new Date(Date.now() - 9000000).toISOString(),
      });

      setNodeMetrics({
        cpuUsage: 15.5,
        memoryUsage: 256.7,
        diskUsage: 1024.3,
        networkIn: 1024,
        networkOut: 2048,
        connectedPeers: 3,
        lastUpdated: new Date().toISOString(),
      });

      setConnectedPeers([
        {
          id: 'peer-001',
          address: '192.168.1.100:8080',
          connectedAt: new Date(Date.now() - 3600000).toISOString(),
          status: 'Connected',
          version: '1.0.0',
        },
        {
          id: 'peer-002',
          address: '192.168.1.101:8080',
          connectedAt: new Date(Date.now() - 7200000).toISOString(),
          status: 'Connected',
          version: '1.0.0',
        },
        {
          id: 'peer-003',
          address: '192.168.1.102:8080',
          connectedAt: new Date(Date.now() - 10800000).toISOString(),
          status: 'Connected',
          version: '1.0.0',
        },
      ]);

      setNodeLogs([
        `[${new Date().toISOString()}] ONODE started successfully`,
        `[${new Date(Date.now() - 300000).toISOString()}] Connected to peer peer-001`,
        `[${new Date(Date.now() - 600000).toISOString()}] Connected to peer peer-002`,
        `[${new Date(Date.now() - 900000).toISOString()}] Connected to peer peer-003`,
        `[${new Date(Date.now() - 1200000).toISOString()}] Node metrics updated`,
        `[${new Date(Date.now() - 1500000).toISOString()}] Network topology updated`,
      ]);
    } catch (error) {
      console.error('Error loading node data:', error);
      toast.error('Failed to load node data');
    } finally {
      setLoading(false);
    }
  };

  const handleStartNode = async () => {
    try {
      // In a real implementation, this would call the API
      console.log('Starting ONODE...');
      toast.success('ONODE started successfully!');
      loadNodeData();
    } catch (error) {
      console.error('Error starting node:', error);
      toast.error('Failed to start node');
    }
  };

  const handleStopNode = async () => {
    try {
      // In a real implementation, this would call the API
      console.log('Stopping ONODE...');
      toast.success('ONODE stopped successfully!');
      loadNodeData();
    } catch (error) {
      console.error('Error stopping node:', error);
      toast.error('Failed to stop node');
    }
  };

  const handleRestartNode = async () => {
    try {
      // In a real implementation, this would call the API
      console.log('Restarting ONODE...');
      toast.success('ONODE restarted successfully!');
      loadNodeData();
    } catch (error) {
      console.error('Error restarting node:', error);
      toast.error('Failed to restart node');
    }
  };

  const handleViewLogs = () => {
    setLogsDialogOpen(true);
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
          <StorageIcon sx={{ mr: 2, fontSize: 32, color: 'primary.main' }} />
          <Typography variant="h4" component="h1">
            ONODE Management
          </Typography>
        </Box>

        {showInfoBar && (
          <Box sx={{ mt: 1, p: 2, bgcolor: '#0d47a1', color: 'white', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
            <InfoIcon sx={{ color: 'white' }} />
            <Typography variant="body2" sx={{ color: 'white', flexGrow: 1 }}>
              Manage the OASIS Node (ONODE). Monitor performance, connected peers, and node operations.
            </Typography>
            <IconButton size="small" onClick={() => setShowInfoBar(false)} sx={{ color: 'white' }}>
              ×
            </IconButton>
          </Box>
        )}

        <Grid container spacing={3}>
          {/* Node Status */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <StorageIcon sx={{ mr: 1, color: 'primary.main' }} />
                  <Typography variant="h6">Node Status</Typography>
                </Box>
                <Box sx={{ mb: 2 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Status</Typography>
                    <Chip
                      label={nodeStatus.isRunning ? 'Running' : 'Stopped'}
                      color={nodeStatus.isRunning ? 'success' : 'error'}
                      size="small"
                    />
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Node ID</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeStatus.nodeId}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Connected Peers</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeStatus.connectedPeersCount}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Uptime</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeStatus.uptime}
                    </Typography>
                  </Box>
                </Box>
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <Button
                    variant="contained"
                    startIcon={<StartIcon />}
                    onClick={handleStartNode}
                    disabled={nodeStatus.isRunning}
                    size="small"
                  >
                    Start
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<StopIcon />}
                    onClick={handleStopNode}
                    disabled={!nodeStatus.isRunning}
                    size="small"
                  >
                    Stop
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<RestartIcon />}
                    onClick={handleRestartNode}
                    disabled={!nodeStatus.isRunning}
                    size="small"
                  >
                    Restart
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {/* Node Information */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <InfoIcon sx={{ mr: 1, color: 'secondary.main' }} />
                  <Typography variant="h6">Node Information</Typography>
                </Box>
                <Box sx={{ mb: 2 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Version</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeInfo.version}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Platform</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeInfo.platform}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Architecture</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeInfo.architecture}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Last Started</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {new Date(nodeInfo.lastStarted).toLocaleString()}
                    </Typography>
                  </Box>
                </Box>
                <Button
                  variant="outlined"
                  startIcon={<RefreshIcon />}
                  onClick={loadNodeData}
                  disabled={loading}
                  size="small"
                >
                  Refresh
                </Button>
              </CardContent>
            </Card>
          </Grid>

          {/* Performance Metrics */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <MetricsIcon sx={{ mr: 1, color: 'success.main' }} />
                  <Typography variant="h6">Performance Metrics</Typography>
                </Box>
                <Box sx={{ mb: 2 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">CPU Usage</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeMetrics.cpuUsage}%
                    </Typography>
                  </Box>
                  <LinearProgress variant="determinate" value={nodeMetrics.cpuUsage} sx={{ mb: 2 }} />
                  
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Memory Usage</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeMetrics.memoryUsage} MB
                    </Typography>
                  </Box>
                  <LinearProgress variant="determinate" value={(nodeMetrics.memoryUsage / 1000) * 100} sx={{ mb: 2 }} />
                  
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Disk Usage</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeMetrics.diskUsage} MB
                    </Typography>
                  </Box>
                  <LinearProgress variant="determinate" value={(nodeMetrics.diskUsage / 10000) * 100} sx={{ mb: 2 }} />
                  
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Network In</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeMetrics.networkIn} KB/s
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2">Network Out</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {nodeMetrics.networkOut} KB/s
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {/* Connected Peers */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <SecurityIcon sx={{ mr: 1, color: 'info.main' }} />
                  <Typography variant="h6">Connected Peers</Typography>
                </Box>
                {connectedPeers.length === 0 ? (
                  <Alert severity="info">No peers connected</Alert>
                ) : (
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Peer ID</TableCell>
                          <TableCell>Address</TableCell>
                          <TableCell>Status</TableCell>
                          <TableCell>Version</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {connectedPeers.map((peer) => (
                          <TableRow key={peer.id}>
                            <TableCell>{peer.id}</TableCell>
                            <TableCell>{peer.address}</TableCell>
                            <TableCell>
                              <Chip
                                label={peer.status}
                                color="success"
                                size="small"
                              />
                            </TableCell>
                            <TableCell>{peer.version}</TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* Node Logs */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <SettingsIcon sx={{ mr: 1, color: 'warning.main' }} />
                    <Typography variant="h6">Node Logs</Typography>
                  </Box>
                  <Button
                    variant="outlined"
                    startIcon={<SettingsIcon />}
                    onClick={handleViewLogs}
                    size="small"
                  >
                    View All Logs
                  </Button>
                </Box>
                <Box sx={{ maxHeight: 200, overflow: 'auto' }}>
                  {nodeLogs.slice(-5).map((log, index) => (
                    <Typography
                      key={index}
                      variant="body2"
                      sx={{
                        fontFamily: 'monospace',
                        fontSize: '0.75rem',
                        mb: 0.5,
                        color: 'text.secondary',
                      }}
                    >
                      {log}
                    </Typography>
                  ))}
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Logs Dialog */}
        <Dialog open={logsDialogOpen} onClose={() => setLogsDialogOpen(false)} maxWidth="md" fullWidth>
          <DialogTitle>Node Logs</DialogTitle>
          <DialogContent>
            <Box sx={{ maxHeight: 400, overflow: 'auto' }}>
              {nodeLogs.map((log, index) => (
                <Typography
                  key={index}
                  variant="body2"
                  sx={{
                    fontFamily: 'monospace',
                    fontSize: '0.75rem',
                    mb: 0.5,
                    color: 'text.secondary',
                  }}
                >
                  {log}
                </Typography>
              ))}
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setLogsDialogOpen(false)}>Close</Button>
          </DialogActions>
        </Dialog>
      </Box>
    </motion.div>
  );
};

export default ONODEPage;