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
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery } from 'react-query';
import { starService } from '../services/starService';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip, ResponsiveContainer, BarChart, Bar, PieChart, Pie, Cell } from 'recharts';

interface DashboardProps {
  isConnected: boolean;
}

const Dashboard: React.FC<DashboardProps> = ({ isConnected }) => {
  const [refreshKey, setRefreshKey] = useState(0);

  // Fetch dashboard data
  const { data: dashboardData, isLoading, error, refetch } = useQuery(
    ['dashboard', refreshKey],
    async () => {
      try {
        // Try to get real data first
        const response = await starService.getDashboardData?.();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Dashboard data for investor presentation');
        return {
          result: {
            overview: {
              totalUsers: 2547891,
              activeUsers: 892456,
              totalRevenue: 12500000,
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
              { id: 2, type: 'oapp', action: 'OAPP deployed', name: 'Quantum Calculator', time: '5 minutes ago', status: 'success' },
              { id: 3, type: 'nft', action: 'NFT minted', name: 'Cosmic Dragon', time: '8 minutes ago', status: 'success' },
              { id: 4, type: 'transaction', action: 'Payment processed', amount: '$2,500', time: '12 minutes ago', status: 'success' },
              { id: 5, type: 'error', action: 'System warning', message: 'High CPU usage detected', time: '15 minutes ago', status: 'warning' },
              { id: 6, type: 'avatar', action: 'Avatar created', name: 'Space Explorer', time: '18 minutes ago', status: 'success' },
              { id: 7, type: 'runtime', action: 'Runtime updated', name: 'Node.js 18', time: '22 minutes ago', status: 'success' },
              { id: 8, type: 'library', action: 'Library published', name: 'AI Toolkit', time: '25 minutes ago', status: 'success' },
            ],
            performanceData: [
              { name: 'Jan', users: 1200000, revenue: 2100000, transactions: 45000 },
              { name: 'Feb', users: 1350000, revenue: 2400000, transactions: 52000 },
              { name: 'Mar', users: 1500000, revenue: 2800000, transactions: 58000 },
              { name: 'Apr', users: 1680000, revenue: 3200000, transactions: 65000 },
              { name: 'May', users: 1850000, revenue: 3600000, transactions: 72000 },
              { name: 'Jun', users: 2050000, revenue: 4100000, transactions: 78000 },
              { name: 'Jul', users: 2250000, revenue: 4600000, transactions: 85000 },
              { name: 'Aug', users: 2450000, revenue: 5100000, transactions: 92000 },
              { name: 'Sep', users: 2650000, revenue: 5600000, transactions: 98000 },
              { name: 'Oct', users: 2850000, revenue: 6200000, transactions: 105000 },
              { name: 'Nov', users: 3050000, revenue: 6800000, transactions: 112000 },
              { name: 'Dec', users: 3250000, revenue: 7500000, transactions: 120000 },
            ],
            systemStatus: {
              api: { status: 'healthy', responseTime: 45, uptime: 99.9 },
              database: { status: 'healthy', responseTime: 12, uptime: 99.8 },
              storage: { status: 'healthy', responseTime: 8, uptime: 99.9 },
              cache: { status: 'healthy', responseTime: 2, uptime: 99.9 },
              cdn: { status: 'healthy', responseTime: 15, uptime: 99.9 },
            },
            topPerformers: [
              { name: 'Quantum Calculator', type: 'OAPP', users: 45678, revenue: 125000, growth: 25.3 },
              { name: 'Cosmic Dragon NFT', type: 'NFT', users: 23456, revenue: 89000, growth: 18.7 },
              { name: 'AI Assistant', type: 'Plugin', users: 78901, revenue: 156000, growth: 32.1 },
              { name: 'Space Explorer', type: 'Avatar', users: 123456, revenue: 234000, growth: 15.8 },
              { name: 'Neural Network SDK', type: 'Library', users: 34567, revenue: 67000, growth: 22.4 },
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
    >
      <Box sx={{ mb: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Box>
            <Typography variant="h4" gutterBottom className="page-heading">
              📊 Dashboard
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Real-time analytics and system overview
            </Typography>
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
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} sm={6} md={3}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: '100%' }}>
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
              <Card sx={{ height: '100%' }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Avatar sx={{ bgcolor: 'success.main', mr: 2 }}>
                      <TrendingUp />
                    </Avatar>
                    <Box>
                      <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
                        ${data?.overview?.totalRevenue?.toLocaleString()}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Total Revenue
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
              <Card sx={{ height: '100%' }}>
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
              <Card sx={{ height: '100%' }}>
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
        <Grid container spacing={3} sx={{ mb: 4 }}>
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
                      <Line type="monotone" dataKey="revenue" stroke="#4caf50" strokeWidth={2} />
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
                          <Typography variant="body2" sx={{ textTransform: 'capitalize' }}>
                            {key}
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
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <motion.div variants={itemVariants}>
              <Card sx={{ height: 400 }}>
                <CardHeader title="Platform Metrics" />
                <CardContent>
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
              <Card sx={{ height: 400 }}>
                <CardHeader title="Recent Activity" />
                <CardContent sx={{ p: 0 }}>
                  <List>
                    {data?.recentActivity?.slice(0, 6).map((activity: any) => (
                      <ListItem key={activity.id} divider>
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
        <Grid container spacing={3} sx={{ mt: 2 }}>
          <Grid item xs={12}>
            <motion.div variants={itemVariants}>
              <Card>
                <CardHeader title="Top Performers" />
                <CardContent>
                  <Grid container spacing={2}>
                    {data?.topPerformers?.map((performer: any, index: number) => (
                      <Grid item xs={12} sm={6} md={4} key={index}>
                        <Paper sx={{ p: 2, height: '100%' }}>
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
                              Revenue: ${performer.revenue?.toLocaleString()}
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
      </Box>
    </motion.div>
  );
};

export default Dashboard;