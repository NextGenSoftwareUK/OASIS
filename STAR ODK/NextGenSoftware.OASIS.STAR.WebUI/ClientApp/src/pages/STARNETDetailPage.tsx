import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Chip,
  Button,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Avatar,
  LinearProgress,
  Divider,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Paper,
  Tabs,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Rating,
} from '@mui/material';
import {
  ArrowBack,
  Store,
  ShoppingCart,
  Favorite,
  Share,
  Download,
  Visibility,
  Timeline,
  Edit,
  Delete,
  Add,
  CheckCircle,
  Cancel,
  Star,
  TrendingUp,
  People,
  Reviews,
  Security,
  Speed,
  Memory,
  Storage,
  Update,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { oappService, templateService, runtimeService, libraryService } from '../services';
import { toast } from 'react-hot-toast';

interface STARNETItem {
  id: string;
  name: string;
  description: string;
  type: 'oapp' | 'plugin' | 'template' | 'library' | 'service';
  category: string;
  version: string;
  author: string;
  price: number;
  currency: string;
  isFree: boolean;
  isFeatured: boolean;
  isPopular: boolean;
  rating: number;
  reviewCount: number;
  downloadCount: number;
  size: number;
  lastUpdated: Date;
  tags: string[];
  screenshots: string[];
  features: string[];
  requirements: string[];
  changelog: Array<{
    version: string;
    date: Date;
    changes: string[];
  }>;
  reviews: Array<{
    id: string;
    user: string;
    rating: number;
    comment: string;
    date: Date;
    helpful: number;
  }>;
  authorInfo: {
    name: string;
    avatar: string;
    verified: boolean;
    itemCount: number;
    totalDownloads: number;
    rating: number;
  };
  statistics: {
    views: number;
    downloads: number;
    favorites: number;
    shares: number;
  };
}

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
      id={`starnet-tabpanel-${index}`}
      aria-labelledby={`starnet-tab-${index}`}
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

const STARNETDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [reviewDialogOpen, setReviewDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);

  // Fetch STARNET item detail
  const { data, isLoading, error } = useQuery(
    ['starnetDetail', id],
    async () => {
      // Demo data for now
      const demoData: STARNETItem = {
        id: id || '1',
        name: 'Quantum Analytics Suite',
        description: 'Comprehensive analytics platform for quantum computing data analysis, visualization, and reporting. Features advanced algorithms, real-time monitoring, and customizable dashboards.',
        type: 'oapp',
        category: 'Analytics',
        version: '3.2.1',
        author: 'Quantum Labs',
        price: 299.99,
        currency: 'USD',
        isFree: false,
        isFeatured: true,
        isPopular: true,
        rating: 4.8,
        reviewCount: 1247,
        downloadCount: 15420,
        size: 45.2 * 1024 * 1024, // 45.2 MB
        lastUpdated: new Date('2024-01-15'),
        tags: ['quantum', 'analytics', 'visualization', 'data-science', 'ai'],
        screenshots: [
          '/screenshots/quantum-analytics-1.png',
          '/screenshots/quantum-analytics-2.png',
          '/screenshots/quantum-analytics-3.png'
        ],
        features: [
          'Real-time quantum state monitoring',
          'Advanced data visualization',
          'Machine learning integration',
          'Custom dashboard creation',
          'API and webhook support',
          'Multi-user collaboration',
          'Export to multiple formats',
          'Cloud synchronization'
        ],
        requirements: [
          'OASIS Runtime v2.0+',
          'Quantum Core Plugin v1.5+',
          'Minimum 4GB RAM',
          'WebGL 2.0 support'
        ],
        changelog: [
          {
            version: '3.2.1',
            date: new Date('2024-01-15'),
            changes: [
              'Added support for new quantum algorithms',
              'Improved visualization performance',
              'Fixed memory leak in data processing',
              'Enhanced API documentation'
            ]
          },
          {
            version: '3.2.0',
            date: new Date('2024-01-10'),
            changes: [
              'Major UI redesign',
              'Added collaborative features',
              'Improved data export options'
            ]
          }
        ],
        reviews: [
          {
            id: '1',
            user: 'QuantumResearcher',
            rating: 5,
            comment: 'Excellent tool for quantum data analysis. The visualization features are outstanding and the performance is top-notch.',
            date: new Date('2024-01-14'),
            helpful: 23
          },
          {
            id: '2',
            user: 'DataScientist42',
            rating: 4,
            comment: 'Great analytics platform with powerful features. The learning curve is a bit steep but worth it.',
            date: new Date('2024-01-12'),
            helpful: 15
          },
          {
            id: '3',
            user: 'TechEnthusiast',
            rating: 5,
            comment: 'This has revolutionized how we analyze quantum data. Highly recommended!',
            date: new Date('2024-01-10'),
            helpful: 31
          }
        ],
        authorInfo: {
          name: 'Quantum Labs',
          avatar: '/avatars/quantum-labs.png',
          verified: true,
          itemCount: 12,
          totalDownloads: 125000,
          rating: 4.9
        },
        statistics: {
          views: 45620,
          downloads: 15420,
          favorites: 3240,
          shares: 890
        }
      };
      return demoData;
    }
  );

  const formatFileSize = (bytes: number) => {
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    if (bytes === 0) return '0 Bytes';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
  };

  const formatDate = (date: Date) => {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  };

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'oapp': return 'primary';
      case 'plugin': return 'secondary';
      case 'template': return 'warning';
      case 'library': return 'info';
      case 'service': return 'success';
      default: return 'default';
    }
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handlePurchase = () => {
    toast.success('Item added to cart successfully!');
  };

  const handleDownload = () => {
    toast.success('Download started!');
  };

  const handleAddToFavorites = () => {
    toast.success('Added to favorites!');
  };

  if (isLoading) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Loading item details...</Typography>
      </Box>
    );
  }

  if (error || !data) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography color="error">Error loading item details</Typography>
        <Button onClick={() => navigate('/starnet')} startIcon={<ArrowBack />}>
          Back to STARNET Store
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        {/* Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
          <IconButton onClick={() => navigate('/starnet')} sx={{ mr: 2 }}>
            <ArrowBack />
          </IconButton>
          <Box sx={{ flexGrow: 1 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
              <Typography variant="h4">
                {data.name}
              </Typography>
              {data.isFeatured && <Chip label="Featured" color="primary" size="small" />}
              {data.isPopular && <Chip label="Popular" color="secondary" size="small" />}
            </Box>
            <Typography variant="subtitle1" color="text.secondary">
              {data.description}
            </Typography>
          </Box>
          <Button
            variant="outlined"
            startIcon={<Share />}
            onClick={() => setShareDialogOpen(true)}
            sx={{ mr: 1 }}
          >
            Share
          </Button>
          <Button
            variant="outlined"
            startIcon={<Favorite />}
            onClick={handleAddToFavorites}
            sx={{ mr: 1 }}
          >
            Favorite
          </Button>
          <Button
            variant="contained"
            startIcon={data.isFree ? <Download /> : <ShoppingCart />}
            onClick={data.isFree ? handleDownload : handlePurchase}
            color="primary"
          >
            {data.isFree ? 'Download Free' : `Buy $${data.price}`}
          </Button>
        </Box>

        {/* Item Overview */}
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Grid container spacing={3}>
                  <Grid item xs={12} md={6}>
                    <Typography variant="h6" gutterBottom>
                      <Store sx={{ mr: 1, verticalAlign: 'middle' }} />
                      Item Information
                    </Typography>
                    <List>
                      <ListItem>
                        <ListItemText
                          primary="Type"
                          secondary={
                            <Chip label={data.type} color={getTypeColor(data.type)} size="small" />
                          }
                        />
                      </ListItem>
                      <ListItem>
                        <ListItemText
                          primary="Category"
                          secondary={data.category}
                        />
                      </ListItem>
                      <ListItem>
                        <ListItemText
                          primary="Version"
                          secondary={data.version}
                        />
                      </ListItem>
                      <ListItem>
                        <ListItemText
                          primary="Size"
                          secondary={formatFileSize(data.size)}
                        />
                      </ListItem>
                      <ListItem>
                        <ListItemText
                          primary="Last Updated"
                          secondary={formatDate(data.lastUpdated)}
                        />
                      </ListItem>
                    </List>
                  </Grid>
                  <Grid item xs={12} md={6}>
                    <Typography variant="h6" gutterBottom>
                      <Star sx={{ mr: 1, verticalAlign: 'middle' }} />
                      Ratings & Reviews
                    </Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                      <Rating value={data.rating} precision={0.1} readOnly sx={{ mr: 1 }} />
                      <Typography variant="h6" sx={{ mr: 1 }}>
                        {data.rating}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        ({data.reviewCount} reviews)
                      </Typography>
                    </Box>
                    <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h6" color="primary">
                          {data.downloadCount.toLocaleString()}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Downloads
                        </Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h6" color="secondary">
                          {data.statistics.views.toLocaleString()}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Views
                        </Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h6" color="success.main">
                          {data.statistics.favorites.toLocaleString()}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Favorites
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  <People sx={{ mr: 1, verticalAlign: 'middle' }} />
                  Author
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Avatar sx={{ mr: 2, width: 56, height: 56 }}>
                    {data.authorInfo.name.charAt(0)}
                  </Avatar>
                  <Box>
                    <Typography variant="subtitle1">
                      {data.authorInfo.name}
                      {data.authorInfo.verified && (
                        <CheckCircle color="primary" sx={{ ml: 1, fontSize: 16 }} />
                      )}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {data.authorInfo.itemCount} items • {data.authorInfo.totalDownloads.toLocaleString()} downloads
                    </Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                      <Rating value={data.authorInfo.rating} precision={0.1} readOnly size="small" />
                      <Typography variant="caption" sx={{ ml: 1 }}>
                        {data.authorInfo.rating}
                      </Typography>
                    </Box>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Tabs */}
        <Card>
          <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
            <Tabs value={tabValue} onChange={handleTabChange}>
              <Tab label="Features" />
              <Tab label="Requirements" />
              <Tab label="Changelog" />
              <Tab label="Reviews" />
            </Tabs>
          </Box>

          <TabPanel value={tabValue} index={0}>
            <Typography variant="h6" gutterBottom>
              Key Features
            </Typography>
            <Grid container spacing={2}>
              {data.features.map((feature, index) => (
                <Grid item xs={12} sm={6} md={4} key={index}>
                  <Paper sx={{ p: 2, textAlign: 'center' }}>
                    <CheckCircle color="success" sx={{ fontSize: 40, mb: 1 }} />
                    <Typography variant="subtitle1">{feature}</Typography>
                  </Paper>
                </Grid>
              ))}
            </Grid>
          </TabPanel>

          <TabPanel value={tabValue} index={1}>
            <Typography variant="h6" gutterBottom>
              System Requirements
            </Typography>
            <List>
              {data.requirements.map((requirement, index) => (
                <ListItem key={index}>
                  <ListItemIcon>
                    <Avatar sx={{ bgcolor: 'primary.main' }}>
                      <CheckCircle />
                    </Avatar>
                  </ListItemIcon>
                  <ListItemText
                    primary={requirement}
                    secondary="Required for optimal performance"
                  />
                </ListItem>
              ))}
            </List>
          </TabPanel>

          <TabPanel value={tabValue} index={2}>
            <Typography variant="h6" gutterBottom>
              Version History
            </Typography>
            <List>
              {data.changelog.map((entry, index) => (
                <React.Fragment key={entry.version}>
                  <ListItem>
                    <ListItemIcon>
                      <Avatar sx={{ bgcolor: 'secondary.main' }}>
                        <Update />
                      </Avatar>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="subtitle1">
                            Version {entry.version}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {formatDate(entry.date)}
                          </Typography>
                        </Box>
                      }
                      secondary={
                        <Box>
                          {entry.changes.map((change, changeIndex) => (
                            <Typography key={changeIndex} variant="body2" color="text.secondary">
                              • {change}
                            </Typography>
                          ))}
                        </Box>
                      }
                    />
                  </ListItem>
                  {index < data.changelog.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          </TabPanel>

          <TabPanel value={tabValue} index={3}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6">
                <Reviews sx={{ mr: 1, verticalAlign: 'middle' }} />
                User Reviews
              </Typography>
              <Button
                variant="outlined"
                startIcon={<Add />}
                onClick={() => setReviewDialogOpen(true)}
              >
                Write Review
              </Button>
            </Box>
            <List>
              {data.reviews.map((review, index) => (
                <React.Fragment key={review.id}>
                  <ListItem>
                    <ListItemIcon>
                      <Avatar>
                        {review.user.charAt(0)}
                      </Avatar>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="subtitle1">
                            {review.user}
                          </Typography>
                          <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            <Rating value={review.rating} readOnly size="small" />
                            <Typography variant="caption" sx={{ ml: 1 }}>
                              {formatDate(review.date)}
                            </Typography>
                          </Box>
                        </Box>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                            {review.comment}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {review.helpful} people found this helpful
                          </Typography>
                        </Box>
                      }
                    />
                  </ListItem>
                  {index < data.reviews.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          </TabPanel>
        </Card>
      </motion.div>
    </Box>
  );
};

export default STARNETDetailPage;
