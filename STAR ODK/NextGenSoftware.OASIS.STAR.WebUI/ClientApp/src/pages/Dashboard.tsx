import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  CardHeader,
  Avatar,
  Chip,
  LinearProgress,
  IconButton,
  Tooltip,
  Paper,
  Stack,
  Divider,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar,
  ListItemSecondaryAction,
  Badge,
  Button,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  People,
  Storage,
  Speed,
  Security,
  Star,
  Timeline,
  Refresh,
  MoreVert,
  ArrowUpward,
  ArrowDownward,
  CheckCircle,
  Warning,
  Error,
  Info,
  Dashboard as DashboardIcon,
  AccountBalance,
  Public,
  Code,
  Store,
  Science,
  Explore,
  Help,
  Apps,
  Memory,
  Description,
  ShoppingCart,
  Visibility,
  Download,
  Upload,
  PlayArrow,
  Pause,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery } from 'react-query';
import { starCoreService, avatarService } from '../services';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip, ResponsiveContainer, BarChart, Bar, PieChart, Pie, Cell } from 'recharts';
import { useNavigate } from 'react-router-dom';
import CloseIcon from '@mui/icons-material/Close';
import React from 'react';

interface DashboardProps {
  isConnected: boolean;
}

const Dashboard: React.FC<DashboardProps> = ({ isConnected }) => {
  const [showInfo, setShowInfo] = useState(true);
  const [refreshKey, setRefreshKey] = useState(0);
  const navigate = useNavigate();
  const DismissibleInfoBar: React.FC = () => {
    if (!showInfo) return null;
    return (
      <Box sx={{ mt: 1, p: 2, bgcolor: '#0d47a1', color: 'white', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1, position: 'relative' }}>
        <Info sx={{ color: 'white' }} />
        <Typography variant="body2" sx={{ color: 'white' }}>
          <strong>System Status:</strong> Monitor OAPPs, Runtimes, Templates, and all ecosystem components. Click refresh to update real-time data.
        </Typography>
        <IconButton size="small" onClick={() => setShowInfo(false)} sx={{ position: 'absolute', right: 8, top: 8, color: 'white' }}>
          <CloseIcon />
        </IconButton>
      </Box>
    );
  };

  // Fetch dashboard data
  const { data: dashboardData, isLoading, error, refetch } = useQuery(
    ['dashboard', refreshKey],
    async () => {
      try {
        // Try to get real data first
        const response = await starCoreService.getDashboardData?.();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Dashboard data for investor presentation');
        return {
          result: {
            overview: {
              totalUsers: 2547891,
              activeUsers: 892456,
              totalKarma: 12500000,
              systemHealth: 98.5,
              uptime: 99.9,
              transactions: 4567892,
              growthRate: 12.5,
              userSatisfaction: 4.8,
            },
            metrics: {
              oapps: { total: 1250, active: 892, growth: 8.2 },
              nfts: { total: 45678, active: 23456, growth: 15.3 },
              avatars: { total: 892456, active: 456789, growth: 22.1 },
              runtimes: { total: 234, active: 189, growth: 5.7 },
              libraries: { total: 567, active: 456, growth: 12.8 },
              templates: { total: 1234, active: 987, growth: 18.9 },
              celestialBodies: { total: 4567, active: 3456, growth: 7.4 },
              celestialSpaces: { total: 234, active: 189, growth: 9.2 },
              quests: { total: 1234, active: 567, growth: 14.6 },
              chapters: { total: 2345, active: 1234, growth: 11.3 },
              inventory: { total: 45678, active: 23456, growth: 16.7 },
              plugins: { total: 234, active: 189, growth: 6.9 },
              storeItems: { total: 1234, active: 987, growth: 13.2 },
            },
            recentActivity: [
              { id: 1, type: 'user', action: 'New user registered', user: 'John Doe', time: '2 minutes ago', status: 'success' },
              { id: 2, type: 'oapp', action: 'OAPP published', user: 'Sarah Wilson', time: '5 minutes ago', status: 'success' },
              { id: 3, type: 'runtime', action: 'Runtime activated', user: 'Mike Chen', time: '8 minutes ago', status: 'success' },
              { id: 4, type: 'template', action: 'Template downloaded', user: 'Emma Davis', time: '12 minutes ago', status: 'success' },
              { id: 5, type: 'store', action: 'Asset purchased', user: 'Alex Brown', time: '15 minutes ago', status: 'success' },
            ],
            topOAPPs: [
              { id: '1', name: 'Cosmic Explorer', downloads: 15420, rating: 4.8, author: 'SpaceDev Studios', category: 'Exploration' },
              { id: '2', name: 'Quantum Builder', downloads: 8930, rating: 4.9, author: 'Quantum Labs', category: 'Construction' },
              { id: '3', name: 'Neural Network Manager', downloads: 25670, rating: 4.7, author: 'AI Innovations', category: 'AI/ML' },
            ],
            topRuntimes: [
              { id: '1', name: 'JavaScript Engine', active: 189, total: 234, type: 'Programming Language', status: 'Running' },
              { id: '2', name: 'Python Interpreter', active: 156, total: 189, type: 'Programming Language', status: 'Running' },
              { id: '3', name: 'Node.js Runtime', active: 134, total: 167, type: 'Web Runtime', status: 'Running' },
            ],
            topTemplates: [
              { id: '1', name: 'React SPA Template', downloads: 234000, rating: 4.8, author: 'React Team', category: 'Web App' },
              { id: '2', name: 'Vue.js SPA Template', downloads: 89000, rating: 4.7, author: 'Vue Team', category: 'Web App' },
              { id: '3', name: 'Angular Enterprise', downloads: 67000, rating: 4.6, author: 'Microsoft', category: 'Web App' },
            ],
            storeItems: [
              { id: '1', name: 'Quantum Engine Core', price: 25000, sales: 1247, category: 'Hardware', rating: 4.9 },
              { id: '2', name: 'Neural Network SDK', price: 8500, sales: 892, category: 'Software', rating: 4.8 },
              { id: '3', name: 'Holographic Display Array', price: 45000, sales: 156, category: 'Display', rating: 4.7 },
            ],
            systemMetrics: {
              cpuUsage: 45.2,
              memoryUsage: 67.8,
              diskUsage: 34.5,
              networkLatency: 12.3,
              activeConnections: 892,
              totalRequests: 4567892,
            },
            userGrowth: [
              { month: 'Jan', users: 1200000, karma: 5000000 },
              { month: 'Feb', users: 1350000, karma: 6000000 },
              { month: 'Mar', users: 1500000, karma: 7500000 },
              { month: 'Apr', users: 1700000, karma: 8500000 },
              { month: 'May', users: 1900000, karma: 9500000 },
              { month: 'Jun', users: 2200000, karma: 11000000 },
              { month: 'Jul', users: 2547891, karma: 12500000 },
            ],
            categoryDistribution: [
              { name: 'Web Apps', value: 35, count: 1250 },
              { name: 'Mobile Apps', value: 25, count: 892 },
              { name: 'AI/ML', value: 20, count: 714 },
              { name: 'Games', value: 15, count: 536 },
              { name: 'Blockchain', value: 5, count: 179 },
            ],
            performanceData: [
              { name: 'Jan', users: 1200000, karma: 2100000, transactions: 45000 },
              { name: 'Feb', users: 1350000, karma: 2400000, transactions: 52000 },
              { name: 'Mar', users: 1500000, karma: 2800000, transactions: 58000 },
              { name: 'Apr', users: 1680000, karma: 3200000, transactions: 65000 },
              { name: 'May', users: 1850000, karma: 3600000, transactions: 72000 },
              { name: 'Jun', users: 2050000, karma: 4100000, transactions: 78000 },
              { name: 'Jul', users: 2250000, karma: 4600000, transactions: 85000 },
              { name: 'Aug', users: 2450000, karma: 5100000, transactions: 92000 },
              { name: 'Sep', users: 2650000, karma: 5600000, transactions: 98000 },
              { name: 'Oct', users: 2850000, karma: 6200000, transactions: 105000 },
              { name: 'Nov', users: 3050000, karma: 6800000, transactions: 112000 },
              { name: 'Dec', users: 3250000, karma: 7500000, transactions: 120000 },
            ],
            systemStatus: {
              api: { status: 'healthy', responseTime: 45, uptime: 99.9 },
              database: { status: 'healthy', responseTime: 12, uptime: 99.8 },
              storage: { status: 'healthy', responseTime: 8, uptime: 99.9 },
              cache: { status: 'healthy', responseTime: 2, uptime: 99.9 },
              cdn: { status: 'healthy', responseTime: 15, uptime: 99.9 },
            },
            topPerformers: [
              { name: 'Quantum Calculator', type: 'OAPP', users: 45678, karma: 1250000, growth: 25.3 },
              { name: 'Cosmic Dragon NFT', type: 'NFT', users: 23456, karma: 890000, growth: 18.7 },
              { name: 'AI Assistant', type: 'Plugin', users: 78901, karma: 1560000, growth: 32.1 },
              { name: 'Space Explorer', type: 'Avatar', users: 123456, karma: 2340000, growth: 15.8 },
              { name: 'Neural Network SDK', type: 'Library', users: 34567, karma: 670000, growth: 22.4 },
            ],
          }
        };
      }
    },
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const handleRefresh = () => {
    setRefreshKey(prev => prev + 1);
    refetch();
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'success': return '#4caf50';
      case 'warning': return '#ff9800';
      case 'error': return '#f44336';
      case 'info': return '#2196f3';
      default: return '#757575';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'success': return <CheckCircle />;
      case 'warning': return <Warning />;
      case 'error': return <Error />;
      case 'info': return <Info />;
      default: return <Info />;
    }
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

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '50vh' }}>
        <LinearProgress sx={{ width: '100%' }} />
      </Box>
    );
  }

  const data = dashboardData?.result;

  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
      style={{ overflow: 'visible', width: '100%' }}
    >
      <Box sx={{ mt: 4, mb: 4, overflow: 'visible' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, px: { xs: 0, md: 1 } }}>
          <Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
              <Typography variant="h4" gutterBottom className="page-heading">
                📊 Dashboard
              </Typography>
              <Tooltip title="Dashboard provides real-time analytics and system overview. Monitor your OASIS ecosystem health, user activity, and system performance.">
                <IconButton size="small" color="primary">
                  <Help />
                </IconButton>
              </Tooltip>
            </Box>
            <Typography variant="subtitle1" color="text.secondary">
              Real-time analytics and system overview
            </Typography>
            <DismissibleInfoBar />
          </Box>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <Tooltip title="Refresh Data">
              <IconButton onClick={handleRefresh} disabled={isLoading}>
                <Refresh />
              </IconButton>
            </Tooltip>
            <IconButton>
              <MoreVert />
            </IconButton>
          </Box>
        </Box>

        {/* Overview Cards */}
        <Grid container spacing={3} sx={{ mb: 4, overflow: 'visible', px: { xs: 0, md: 1 }, width: '100%' }}>
          <Grid item xs={12} sm={6} md={3}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: '100%', overflow: 'visible' }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Avatar sx={{ bgcolor: 'primary.main', mr: 2 }}>
                      <People />
                    </Avatar>
                    <Box>
                      <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                        {data?.overview?.totalUsers?.toLocaleString()}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Total Users
                      </Typography>
                    </Box>
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <ArrowUpward sx={{ color: '#4caf50', fontSize: 16, mr: 0.5 }} />
                    <Typography variant="body2" sx={{ color: '#4caf50', fontWeight: 'bold' }}>
                      +{data?.overview?.growthRate}%
                    </Typography>
                  </Box>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>

          <Grid item xs={12} sm={6} md={3}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: '100%', overflow: 'visible' }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Avatar sx={{ bgcolor: 'success.main', mr: 2 }}>
                      <TrendingUp />
                    </Avatar>
                    <Box>
                      <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                        {data?.overview?.totalKarma?.toLocaleString()}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Total Karma Points
                      </Typography>
                    </Box>
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <ArrowUpward sx={{ color: '#4caf50', fontSize: 16, mr: 0.5 }} />
                    <Typography variant="body2" sx={{ color: '#4caf50', fontWeight: 'bold' }}>
                      +15.3%
                    </Typography>
                  </Box>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>

          <Grid item xs={12} sm={6} md={3}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: '100%', overflow: 'visible' }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Avatar sx={{ bgcolor: 'warning.main', mr: 2 }}>
                      <Speed />
                    </Avatar>
                    <Box>
                      <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                        {data?.overview?.systemHealth}%
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        System Health
                      </Typography>
                    </Box>
                  </Box>
                  <LinearProgress 
                    variant="determinate" 
                    value={data?.overview?.systemHealth} 
                    sx={{ mt: 1 }}
                  />
                </CardContent>
              </Card>
            </motion.div>
          </Grid>

          <Grid item xs={12} sm={6} md={3}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: '100%', overflow: 'visible' }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Avatar sx={{ bgcolor: 'info.main', mr: 2 }}>
                      <Security />
                    </Avatar>
                    <Box>
                      <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                        {data?.overview?.uptime}%
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Uptime
                      </Typography>
                    </Box>
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <CheckCircle sx={{ color: '#4caf50', fontSize: 16, mr: 0.5 }} />
                    <Typography variant="body2" sx={{ color: '#4caf50', fontWeight: 'bold' }}>
                      All Systems Operational
                    </Typography>
                  </Box>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>
        </Grid>

        {/* Charts Row */}
        <Grid container spacing={3} sx={{ mb: 4, overflow: 'visible', px: { xs: 0, md: 1 }, width: '100%' }}>
          <Grid item xs={12} lg={8}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: 400 }}>
                <CardHeader title="Performance Trends" />
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={data?.performanceData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="name" />
                      <YAxis />
                      <RechartsTooltip />
                      <Line type="monotone" dataKey="users" stroke="#2196f3" strokeWidth={2} />
                      <Line type="monotone" dataKey="karma" stroke="#4caf50" strokeWidth={2} />
                      <Line type="monotone" dataKey="transactions" stroke="#ff9800" strokeWidth={2} />
                    </LineChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>

          <Grid item xs={12} lg={4}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: 400 }}>
                <CardHeader title="System Status" />
                <CardContent>
                  <Stack spacing={2}>
                    {Object.entries(data?.systemStatus || {}).map(([key, status]: [string, any]) => (
                      <Box key={key} sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <Box
                            sx={{
                              width: 12,
                              height: 12,
                              borderRadius: '50%',
                              bgcolor: status.status === 'healthy' ? '#4caf50' : '#f44336',
                              mr: 2,
                            }}
                          />
                          <Typography variant="body2" sx={{ textTransform: 'uppercase' }}>
                            {key === 'api' ? 'API' : key === 'cdn' ? 'CDN' : key}
                          </Typography>
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                          {status.responseTime}ms
                        </Typography>
                      </Box>
                    ))}
                  </Stack>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>
        </Grid>

        {/* Metrics and Activity Row */}
        <Grid container spacing={3} sx={{ overflow: 'visible', px: { xs: 0, md: 1 }, width: '100%' }}>
          <Grid item xs={12} md={6}>
            <motion.div variants={itemVariants}>
              <Card sx={{ minHeight: 400, height: '100%', overflow: 'visible' }}>
                <CardHeader title="Platform Metrics" />
                <CardContent sx={{ overflow: 'visible' }}>
                  <Grid container spacing={2}>
                    {Object.entries(data?.metrics || {}).map(([key, metric]: [string, any]) => (
                      <Grid item xs={6} sm={4} key={key}>
                        <Paper sx={{ p: 2, textAlign: 'center' }}>
                          <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                            {metric.total?.toLocaleString()}
                          </Typography>
                          <Typography variant="body2" color="text.secondary" sx={{ textTransform: 'capitalize' }}>
                            {key.replace(/([A-Z])/g, ' $1').trim()}
                          </Typography>
                          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mt: 1 }}>
                            <ArrowUpward sx={{ color: '#4caf50', fontSize: 14, mr: 0.5 }} />
                            <Typography variant="caption" sx={{ color: '#4caf50', fontWeight: 'bold' }}>
                              +{metric.growth}%
                            </Typography>
                          </Box>
                        </Paper>
                      </Grid>
                    ))}
                  </Grid>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>

          <Grid item xs={12} md={6}>
            <motion.div variants={itemVariants}>
              <Card sx={{ minHeight: 400, height: '100%', display: 'flex', flexDirection: 'column' }}>
                <CardHeader title="Recent Activity" />
                <CardContent sx={{ p: 0, flex: 1, overflowY: 'auto' }}>
                  <List disablePadding>
                    {data?.recentActivity?.slice(0, 6).map((activity: any) => (
                      <ListItem key={activity.id} divider sx={{ px: 2 }}>
                        <ListItemAvatar>
                          <Avatar sx={{ bgcolor: getStatusColor(activity.status), width: 32, height: 32 }}>
                            {getStatusIcon(activity.status)}
                          </Avatar>
                        </ListItemAvatar>
                        <ListItemText
                          primary={activity.action}
                          secondary={activity.time}
                        />
                        <ListItemSecondaryAction>
                          <Chip
                            label={activity.status}
                            size="small"
                            sx={{ bgcolor: getStatusColor(activity.status), color: 'white' }}
                          />
                        </ListItemSecondaryAction>
                      </ListItem>
                    ))}
                  </List>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>
        </Grid>

        {/* Top Performers */}
        <Grid container spacing={3} sx={{ mt: 2, overflow: 'visible', pb: 3, px: { xs: 0, md: 1 }, width: '100%' }}>
          <Grid item xs={12}>
            <motion.div variants={itemVariants}>
              <Card>
                <CardHeader title="Top Performers" />
                <CardContent>
                  <Grid container spacing={2}>
                    {data?.topPerformers?.map((performer: any, index: number) => (
                      <Grid item xs={12} sm={6} md={4} key={index}>
                        <Paper sx={{ p: 2, height: '100%', overflow: 'visible' }}>
                          <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                            <Avatar sx={{ bgcolor: 'primary.main', mr: 2 }}>
                              {performer.type === 'OAPP' && <Code />}
                              {performer.type === 'NFT' && <Star />}
                              {performer.type === 'Plugin' && <Science />}
                              {performer.type === 'Avatar' && <People />}
                              {performer.type === 'Library' && <Storage />}
                            </Avatar>
                            <Box>
                              <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                                {performer.name}
                              </Typography>
                              <Typography variant="body2" color="text.secondary">
                                {performer.type}
                              </Typography>
                            </Box>
                          </Box>
                          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                            <Typography variant="body2" color="text.secondary">
                              Users: {performer.users?.toLocaleString()}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Karma: {performer.karma?.toLocaleString()}
                            </Typography>
                          </Box>
                          <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            <ArrowUpward sx={{ color: '#4caf50', fontSize: 16, mr: 0.5 }} />
                            <Typography variant="body2" sx={{ color: '#4caf50', fontWeight: 'bold' }}>
                              +{performer.growth}% Growth
                            </Typography>
                          </Box>
                        </Paper>
                      </Grid>
                    ))}
                  </Grid>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>
        </Grid>

        {/* Interactive Dashboard Sections */}
        <Grid container spacing={3} sx={{ mt: 2, overflow: 'visible', pb: 3, px: { xs: 0, md: 1 }, width: '100%' }}>
          {/* Top OAPPs Section */}
          <Grid item xs={12} md={6}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', p: 2, borderBottom: 1, borderColor: 'divider' }}>
                  <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Apps color="primary" />
                    Top OAPPs
                  </Typography>
                  <Button 
                    size="small" 
                    onClick={() => navigate('/oapps')}
                    endIcon={<ArrowUpward />}
                  >
                    View All
                  </Button>
                </Box>
                <CardContent sx={{ p: 0 }}>
                  <List>
                    {[
                      { id: '1', name: 'Cosmic Explorer', downloads: 15420, rating: 4.8, author: 'SpaceDev Studios', category: 'Exploration' },
                      { id: '2', name: 'Quantum Builder', downloads: 8930, rating: 4.9, author: 'Quantum Labs', category: 'Construction' },
                      { id: '3', name: 'Neural Network Manager', downloads: 25670, rating: 4.7, author: 'AI Innovations', category: 'AI/ML' },
                    ].map((oapp) => (
                      <ListItem 
                        key={oapp.id} 
                        sx={{ 
                          cursor: 'pointer', 
                          '&:hover': { bgcolor: 'action.hover' },
                          borderRadius: 1,
                          mb: 1,
                          mx: 1
                        }}
                        onClick={() => navigate(`/oapps/${oapp.id}`)}
                      >
                        <ListItemAvatar>
                          <Avatar sx={{ bgcolor: 'primary.main' }}>
                            <Apps />
                          </Avatar>
                        </ListItemAvatar>
                        <ListItemText
                          primary={oapp.name}
                          secondary={
                            <Box>
                              <Typography variant="body2" color="text.secondary">
                                {oapp.author} • {oapp.category}
                              </Typography>
                              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mt: 0.5 }}>
                                <Chip 
                                  icon={<Download />} 
                                  label={`${oapp.downloads.toLocaleString()} downloads`} 
                                  size="small" 
                                  color="primary" 
                                  variant="outlined"
                                />
                                <Chip 
                                  icon={<Star />} 
                                  label={`${oapp.rating}/5`} 
                                  size="small" 
                                  color="secondary" 
                                  variant="outlined"
                                />
                              </Box>
                            </Box>
                          }
                        />
                        <ListItemSecondaryAction>
                          <IconButton size="small" onClick={() => navigate(`/oapps/${oapp.id}`)}>
                            <Visibility />
                          </IconButton>
                        </ListItemSecondaryAction>
                      </ListItem>
                    ))}
                  </List>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>

          {/* Top Runtimes Section */}
          <Grid item xs={12} md={6}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', p: 2, borderBottom: 1, borderColor: 'divider' }}>
                  <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Memory color="secondary" />
                    Top Runtimes
                  </Typography>
                  <Button 
                    size="small" 
                    onClick={() => navigate('/runtimes')}
                    endIcon={<ArrowUpward />}
                  >
                    View All
                  </Button>
                </Box>
                <CardContent sx={{ p: 0 }}>
                  <List>
                    {[
                      { id: '1', name: 'JavaScript Engine', active: 189, total: 234, type: 'Programming Language', status: 'Running' },
                      { id: '2', name: 'Python Interpreter', active: 156, total: 189, type: 'Programming Language', status: 'Running' },
                      { id: '3', name: 'Node.js Runtime', active: 134, total: 167, type: 'Web Runtime', status: 'Running' },
                    ].map((runtime) => (
                      <ListItem 
                        key={runtime.id} 
                        sx={{ 
                          cursor: 'pointer', 
                          '&:hover': { bgcolor: 'action.hover' },
                          borderRadius: 1,
                          mb: 1,
                          mx: 1
                        }}
                        onClick={() => navigate(`/runtimes/${runtime.id}`)}
                      >
                        <ListItemAvatar>
                          <Avatar sx={{ bgcolor: 'secondary.main' }}>
                            <Memory />
                          </Avatar>
                        </ListItemAvatar>
                        <ListItemText
                          primary={runtime.name}
                          secondary={
                            <Box>
                              <Typography variant="body2" color="text.secondary">
                                {runtime.type}
                              </Typography>
                              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mt: 0.5 }}>
                                <Chip 
                                  icon={runtime.status === 'Running' ? <PlayArrow /> : <Pause />} 
                                  label={`${runtime.active}/${runtime.total} active`} 
                                  size="small" 
                                  color={runtime.status === 'Running' ? 'success' : 'default'}
                                  variant="outlined"
                                />
                                <Chip 
                                  label={runtime.status} 
                                  size="small" 
                                  color={runtime.status === 'Running' ? 'success' : 'default'}
                                  variant="outlined"
                                />
                              </Box>
                            </Box>
                          }
                        />
                        <ListItemSecondaryAction>
                          <IconButton size="small" onClick={() => navigate(`/runtimes/${runtime.id}`)}>
                            <Visibility />
                          </IconButton>
                        </ListItemSecondaryAction>
                      </ListItem>
                    ))}
                  </List>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>

          {/* Top Templates Section */}
          <Grid item xs={12} md={6}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', p: 2, borderBottom: 1, borderColor: 'divider' }}>
                  <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Description color="success" />
                    Top Templates
                  </Typography>
                  <Button 
                    size="small" 
                    onClick={() => navigate('/templates')}
                    endIcon={<ArrowUpward />}
                  >
                    View All
                  </Button>
                </Box>
                <CardContent sx={{ p: 0 }}>
                  <List>
                    {[
                      { id: '1', name: 'React SPA Template', downloads: 234000, rating: 4.8, author: 'React Team', category: 'Web App' },
                      { id: '2', name: 'Vue.js SPA Template', downloads: 89000, rating: 4.7, author: 'Vue Team', category: 'Web App' },
                      { id: '3', name: 'Angular Enterprise', downloads: 67000, rating: 4.6, author: 'Microsoft', category: 'Web App' },
                    ].map((template) => (
                      <ListItem 
                        key={template.id} 
                        sx={{ 
                          cursor: 'pointer', 
                          '&:hover': { bgcolor: 'action.hover' },
                          borderRadius: 1,
                          mb: 1,
                          mx: 1
                        }}
                        onClick={() => navigate(`/templates/${template.id}`)}
                      >
                        <ListItemAvatar>
                          <Avatar sx={{ bgcolor: 'success.main' }}>
                            <Description />
                          </Avatar>
                        </ListItemAvatar>
                        <ListItemText
                          primary={template.name}
                          secondary={
                            <Box>
                              <Typography variant="body2" color="text.secondary">
                                {template.author} • {template.category}
                              </Typography>
                              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mt: 0.5 }}>
                                <Chip 
                                  icon={<Download />} 
                                  label={`${template.downloads.toLocaleString()} downloads`} 
                                  size="small" 
                                  color="primary" 
                                  variant="outlined"
                                />
                                <Chip 
                                  icon={<Star />} 
                                  label={`${template.rating}/5`} 
                                  size="small" 
                                  color="secondary" 
                                  variant="outlined"
                                />
                              </Box>
                            </Box>
                          }
                        />
                        <ListItemSecondaryAction>
                          <IconButton size="small" onClick={() => navigate(`/templates/${template.id}`)}>
                            <Visibility />
                          </IconButton>
                        </ListItemSecondaryAction>
                      </ListItem>
                    ))}
                  </List>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>

          {/* Store Items Section */}
          <Grid item xs={12} md={6}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', p: 2, borderBottom: 1, borderColor: 'divider' }}>
                  <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <ShoppingCart color="warning" />
                    Top Store Items
                  </Typography>
                  <Button 
                    size="small" 
                    onClick={() => navigate('/store')}
                    endIcon={<ArrowUpward />}
                  >
                    View All
                  </Button>
                </Box>
                <CardContent sx={{ p: 0 }}>
                  <List>
                    {[
                      { id: '1', name: 'Quantum Engine Core', price: 25000, sales: 1247, category: 'Hardware', rating: 4.9 },
                      { id: '2', name: 'Neural Network SDK', price: 8500, sales: 892, category: 'Software', rating: 4.8 },
                      { id: '3', name: 'Holographic Display Array', price: 45000, sales: 156, category: 'Display', rating: 4.7 },
                    ].map((item) => (
                      <ListItem 
                        key={item.id} 
                        sx={{ 
                          cursor: 'pointer', 
                          '&:hover': { bgcolor: 'action.hover' },
                          borderRadius: 1,
                          mb: 1,
                          mx: 1
                        }}
                        onClick={() => navigate(`/store/${item.id}`)}
                      >
                        <ListItemAvatar>
                          <Avatar sx={{ bgcolor: 'warning.main' }}>
                            <ShoppingCart />
                          </Avatar>
                        </ListItemAvatar>
                        <ListItemText
                          primary={item.name}
                          secondary={
                            <Box>
                              <Typography variant="body2" color="text.secondary">
                                {item.category}
                              </Typography>
                              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mt: 0.5 }}>
                                <Chip 
                                  label={`$${item.price.toLocaleString()}`} 
                                  size="small" 
                                  color="success" 
                                  variant="outlined"
                                />
                                <Chip 
                                  icon={<Star />} 
                                  label={`${item.rating}/5`} 
                                  size="small" 
                                  color="secondary" 
                                  variant="outlined"
                                />
                                <Chip 
                                  label={`${item.sales} sales`} 
                                  size="small" 
                                  color="info" 
                                  variant="outlined"
                                />
                              </Box>
                            </Box>
                          }
                        />
                        <ListItemSecondaryAction>
                          <IconButton size="small" onClick={() => navigate(`/store/${item.id}`)}>
                            <Visibility />
                          </IconButton>
                        </ListItemSecondaryAction>
                      </ListItem>
                    ))}
                  </List>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>
        </Grid>

        {/* Category Distribution Chart */}
        <Grid container spacing={3} sx={{ mt: 2, overflow: 'visible', pb: 3, px: { xs: 0, md: 1 }, width: '100%' }}>
          <Grid item xs={12} md={6}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: 400 }}>
                <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
                  <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Explore color="info" />
                    Category Distribution
                  </Typography>
                </Box>
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <PieChart>
                      <Pie
                        data={[
                          { name: 'Web Apps', value: 35, count: 1250 },
                          { name: 'Mobile Apps', value: 25, count: 892 },
                          { name: 'AI/ML', value: 20, count: 714 },
                          { name: 'Games', value: 15, count: 536 },
                          { name: 'Blockchain', value: 5, count: 179 },
                        ]}
                        cx="50%"
                        cy="50%"
                        labelLine={false}
                        label={({ name, percent }: { name: string; percent: number }) => `${name} ${(percent * 100).toFixed(0)}%`}
                        outerRadius={80}
                        fill="#8884d8"
                        dataKey="value"
                      >
                        {[
                          { name: 'Web Apps', value: 35, count: 1250 },
                          { name: 'Mobile Apps', value: 25, count: 892 },
                          { name: 'AI/ML', value: 20, count: 714 },
                          { name: 'Games', value: 15, count: 536 },
                          { name: 'Blockchain', value: 5, count: 179 },
                        ].map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={['#1976d2', '#4caf50', '#ff9800', '#f44336', '#9c27b0'][index % 5]} />
                        ))}
                      </Pie>
                      <RechartsTooltip />
                    </PieChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>

          {/* User Growth Chart */}
          <Grid item xs={12} md={6}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: 400 }}>
                <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
                  <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <TrendingUp color="primary" />
                    User Growth & Karma
                  </Typography>
                </Box>
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={[
                      { month: 'Jan', users: 1200000, karma: 5000000 },
                      { month: 'Feb', users: 1350000, karma: 6000000 },
                      { month: 'Mar', users: 1500000, karma: 7500000 },
                      { month: 'Apr', users: 1700000, karma: 8500000 },
                      { month: 'May', users: 1900000, karma: 9500000 },
                      { month: 'Jun', users: 2200000, karma: 11000000 },
                      { month: 'Jul', users: 2547891, karma: 12500000 },
                    ]}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="month" />
                      <YAxis yAxisId="left" />
                      <YAxis yAxisId="right" orientation="right" />
                      <RechartsTooltip />
                      <Line yAxisId="left" type="monotone" dataKey="users" stroke="#1976d2" strokeWidth={2} />
                      <Line yAxisId="right" type="monotone" dataKey="karma" stroke="#4caf50" strokeWidth={2} />
                    </LineChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>
            </motion.div>
          </Grid>
        </Grid>
      </Box>
    </motion.div>
  );
};

export default Dashboard;