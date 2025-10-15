import React, { useState, useEffect } from 'react';
import {
  Box, Typography, Card, CardContent, Grid, Button, Chip, LinearProgress,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
  IconButton, Tooltip, Alert, AlertTitle, CircularProgress, Divider,
  List, ListItem, ListItemText, ListItemIcon, ListItemSecondaryAction
} from '@mui/material';
import {
  NetworkCheck, PlayArrow, Stop, Settings, Storage, Speed,
  TrendingUp, HealthAndSafety, Assessment, Refresh, Info
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useQueryClient } from 'react-query';
import { hypernetService } from '../services/core/hypernetService';

const HyperNETPage: React.FC = () => {
  const [isStarting, setIsStarting] = useState(false);
  const [isStopping, setIsStopping] = useState(false);
  const queryClient = useQueryClient();

  // Queries
  const { data: statusData, isLoading: statusLoading } = useQuery(
    'hypernet-status',
    () => hypernetService.getStatus(),
    { refetchInterval: 5000 }
  );

  const { data: configData, isLoading: configLoading } = useQuery(
    'hypernet-config',
    () => hypernetService.getConfig()
  );

  const { data: nodesData, isLoading: nodesLoading } = useQuery(
    'hypernet-nodes',
    () => hypernetService.getNodes()
  );

  const { data: blocksData, isLoading: blocksLoading } = useQuery(
    'hypernet-blocks',
    () => hypernetService.getBlocks(10)
  );

  const { data: transactionsData, isLoading: transactionsLoading } = useQuery(
    'hypernet-transactions',
    () => hypernetService.getTransactions(10)
  );

  const { data: metricsData, isLoading: metricsLoading } = useQuery(
    'hypernet-metrics',
    () => hypernetService.getMetrics()
  );

  const { data: healthData, isLoading: healthLoading } = useQuery(
    'hypernet-health',
    () => hypernetService.getHealth()
  );

  const { data: statisticsData, isLoading: statisticsLoading } = useQuery(
    'hypernet-statistics',
    () => hypernetService.getStatistics()
  );

  const handleStart = async () => {
    setIsStarting(true);
    try {
      await hypernetService.start();
      queryClient.invalidateQueries('hypernet-status');
      queryClient.invalidateQueries('hypernet-health');
    } catch (error) {
      console.error('Failed to start HyperNET:', error);
    } finally {
      setIsStarting(false);
    }
  };

  const handleStop = async () => {
    setIsStopping(true);
    try {
      await hypernetService.stop();
      queryClient.invalidateQueries('hypernet-status');
      queryClient.invalidateQueries('hypernet-health');
    } catch (error) {
      console.error('Failed to stop HyperNET:', error);
    } finally {
      setIsStopping(false);
    }
  };

  const handleRefresh = () => {
    queryClient.invalidateQueries('hypernet-status');
    queryClient.invalidateQueries('hypernet-nodes');
    queryClient.invalidateQueries('hypernet-blocks');
    queryClient.invalidateQueries('hypernet-transactions');
    queryClient.invalidateQueries('hypernet-metrics');
    queryClient.invalidateQueries('hypernet-health');
    queryClient.invalidateQueries('hypernet-statistics');
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active': return 'success';
      case 'inactive': return 'error';
      case 'healthy': return 'success';
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
            HyperNET Management
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
                {isStopping ? <CircularProgress size={20} /> : 'Stop HyperNET'}
              </Button>
            ) : (
              <Button
                variant="contained"
                color="success"
                startIcon={<PlayArrow />}
                onClick={handleStart}
                disabled={isStarting}
              >
                {isStarting ? <CircularProgress size={20} /> : 'Start HyperNET'}
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
                HyperNET Status
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
                      <ListItemText primary="Consensus" secondary={configData.result.consensus} />
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="Block Time" secondary={`${configData.result.blockTime}s`} />
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
                          {metricsData.result.blocks?.total?.toLocaleString()}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Total Blocks
                        </Typography>
                      </Box>
                    </Grid>
                    <Grid item xs={6}>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h5" color="primary">
                          {metricsData.result.transactions?.total?.toLocaleString()}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Total Transactions
                        </Typography>
                      </Box>
                    </Grid>
                    <Grid item xs={6}>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h5" color="primary">
                          {metricsData.result.network?.activeNodes}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Active Nodes
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

          {/* Recent Blocks */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Storage color="primary" />
                  Recent Blocks
                </Typography>
                {blocksLoading ? (
                  <CircularProgress />
                ) : blocksData?.result ? (
                  <TableContainer component={Paper} variant="outlined">
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Height</TableCell>
                          <TableCell>Hash</TableCell>
                          <TableCell>Transactions</TableCell>
                          <TableCell>Size</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {blocksData.result.map((block: any) => (
                          <TableRow key={block.id}>
                            <TableCell>{block.height}</TableCell>
                            <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                              {block.hash.slice(0, 10)}...
                            </TableCell>
                            <TableCell>{block.transactions}</TableCell>
                            <TableCell>{block.size} bytes</TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                ) : (
                  <Typography color="text.secondary">No blocks data available</Typography>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* Recent Transactions */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Speed color="primary" />
                  Recent Transactions
                </Typography>
                {transactionsLoading ? (
                  <CircularProgress />
                ) : transactionsData?.result ? (
                  <TableContainer component={Paper} variant="outlined">
                    <Table>
                      <TableHead>
                        <TableRow>
                          <TableCell>Hash</TableCell>
                          <TableCell>From</TableCell>
                          <TableCell>To</TableCell>
                          <TableCell>Amount</TableCell>
                          <TableCell>Status</TableCell>
                          <TableCell>Timestamp</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {transactionsData.result.map((tx: any) => (
                          <TableRow key={tx.id}>
                            <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                              {tx.hash.slice(0, 10)}...
                            </TableCell>
                            <TableCell>{tx.from}</TableCell>
                            <TableCell>{tx.to}</TableCell>
                            <TableCell>{tx.amount}</TableCell>
                            <TableCell>
                              <Chip 
                                label={tx.status}
                                color={getStatusColor(tx.status) as any}
                                size="small"
                              />
                            </TableCell>
                            <TableCell>
                              {new Date(tx.timestamp).toLocaleString()}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                ) : (
                  <Typography color="text.secondary">No transactions data available</Typography>
                )}
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </motion.div>
    </Box>
  );
};

export default HyperNETPage;
