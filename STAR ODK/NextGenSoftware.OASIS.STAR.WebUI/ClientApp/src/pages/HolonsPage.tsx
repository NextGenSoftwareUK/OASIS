import React, { useState } from 'react';
import {
  Container,
  Grid,
  Card,
  CardContent,
  CardMedia,
  Typography,
  Box,
  Chip,
  IconButton,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  InputAdornment,
  CircularProgress,
  Alert,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Avatar,
  Stack,
  LinearProgress,
  Badge
} from '@mui/material';
import {
  Search,
  FilterList,
  CloudDownload,
  Star,
  Visibility,
  Code,
  DataObject,
  Storage,
  Memory,
  Security,
  Category,
  Download,
  Info
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery } from 'react-query';
import { holonService } from '../services';
import { Link } from 'react-router-dom';

interface Holon {
  id: string;
  name: string;
  description: string;
  imageUrl?: string;
  version: string;
  category: string;
  type: string;
  author: string;
  downloads: number;
  rating: number;
  size: number;
  lastUpdated: string;
  isPublic: boolean;
  isFeatured: boolean;
  tags: string[];
  dataSchema: any;
  properties: string[];
  methods: string[];
  events: string[];
  documentation?: string;
  repository?: string;
  license: string;
  price: number;
  isFree: boolean;
  isInstalled?: boolean;
}

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.1 }
  }
};

const itemVariants = {
  hidden: { y: 20, opacity: 0 },
  visible: {
    y: 0,
    opacity: 1
  }
};

const HolonsPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('All');
  const [viewFilter, setViewFilter] = useState('All');
  const [selectedHolon, setSelectedHolon] = useState<Holon | null>(null);
  const [detailsDialogOpen, setDetailsDialogOpen] = useState(false);

  // Fetch data based on view filter
  const { data: holonsData, isLoading, error } = useQuery(
    ['holons', viewFilter],
    async () => {
      if (viewFilter === 'Installed') {
        return holonService.getInstalled();
      } else if (viewFilter === 'My Holons') {
        return holonService.getForAvatar('current-avatar-id');
      } else {
        return holonService.getAll();
      }
    }
  );

  const holons: Holon[] = holonsData?.result || [];

  // Filter holons based on search and category
  const filteredHolons = (holons || []).filter((holon) => {
    const matchesSearch = holon.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         holon.description.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesCategory = categoryFilter === 'All' || holon.category === categoryFilter;
    return matchesSearch && matchesCategory;
  });

  const categories = ['All', ...Array.from(new Set(holons.map(h => h.category)))];

  const handleViewDetails = (holon: Holon) => {
    setSelectedHolon(holon);
    setDetailsDialogOpen(true);
  };

  if (error) {
    return (
      <Container maxWidth="xl" sx={{ mt: 4 }}>
        <Alert severity="error">Failed to load holons. Please try again later.</Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ py: 4 }}>
      <motion.div
        variants={containerVariants}
        initial="hidden"
        animate="visible"
      >
        {/* Header */}
        <Box sx={{ mb: 4, mt: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 56, height: 56 }}>
              <DataObject fontSize="large" />
            </Avatar>
            <Box>
              <Typography variant="h4" gutterBottom className="page-heading">
                🌐 Holons (OASIS Data Objects)
              </Typography>
              <Typography variant="subtitle1" color="text.secondary">
                Reusable data structures and objects for building your OAPP
              </Typography>
              <Box sx={{ mt: 1, p: 2, bgcolor: '#0d47a1', color: 'white', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
                <Info sx={{ color: 'white' }} />
                <Typography variant="body2" sx={{ color: 'white' }}>
                  Create, manage and reuse data structures. Build modular OAPPs with reusable components.
                </Typography>
              </Box>
            </Box>
          </Box>
        </Box>

        {/* Filters */}
        <Grid container spacing={2} sx={{ mb: 4 }}>
          <Grid item xs={12} md={3}>
            <FormControl fullWidth>
              <InputLabel>View</InputLabel>
              <Select
                value={viewFilter}
                onChange={(e) => setViewFilter(e.target.value)}
                label="View"
              >
                <MenuItem value="All">All</MenuItem>
                <MenuItem value="Installed">Installed</MenuItem>
                <MenuItem value="My Holons">My Holons</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          <Grid item xs={12} md={3}>
            <FormControl fullWidth>
              <InputLabel>Category</InputLabel>
              <Select
                value={categoryFilter}
                onChange={(e) => setCategoryFilter(e.target.value)}
                label="Category"
              >
                {categories.map((category) => (
                  <MenuItem key={category} value={category}>
                    {category}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>

          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              placeholder="Search holons..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Search />
                  </InputAdornment>
                ),
              }}
            />
          </Grid>
        </Grid>

        {/* Loading State */}
        {isLoading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
            <CircularProgress />
          </Box>
        )}

        {/* Holons Grid */}
        {!isLoading && (
          <Grid container spacing={3}>
            {filteredHolons.map((holon) => (
              <Grid item xs={12} sm={6} md={4} lg={3} key={holon.id}>
                <motion.div variants={itemVariants}>
                  <Card
                    sx={{
                      height: '100%',
                      display: 'flex',
                      flexDirection: 'column',
                      position: 'relative',
                      transition: 'transform 0.2s, box-shadow 0.2s',
                      '&:hover': {
                        transform: 'translateY(-4px)',
                        boxShadow: 6,
                      },
                    }}
                  >
                    {holon.imageUrl && (
                      <CardMedia
                        component="img"
                        height="140"
                        image={holon.imageUrl}
                        alt={holon.name}
                      />
                    )}

                    {/* Installed Badge */}
                    {holon.isInstalled && (
                      <Chip
                        label="Installed"
                        size="small"
                        color="success"
                        sx={{
                          position: 'absolute',
                          bottom: 8,
                          right: 8,
                          fontWeight: 'bold',
                          zIndex: 1
                        }}
                      />
                    )}

                    <CardContent sx={{ flexGrow: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                        <DataObject color="primary" />
                        <Typography variant="h6" component="div" noWrap>
                          {holon.name}
                        </Typography>
                      </Box>

                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2, minHeight: 40 }}>
                        {holon.description.length > 80
                          ? `${holon.description.substring(0, 80)}...`
                          : holon.description}
                      </Typography>

                      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap', gap: 0.5 }}>
                        <Chip label={holon.category} size="small" color="primary" variant="outlined" />
                        <Chip label={holon.type} size="small" />
                      </Stack>

                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Star sx={{ color: '#ffc107', fontSize: 18 }} />
                          <Typography variant="body2">{holon.rating}</Typography>
                        </Box>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <CloudDownload sx={{ fontSize: 18, color: 'text.secondary' }} />
                          <Typography variant="body2" color="text.secondary">
                            {holon.downloads.toLocaleString()}
                          </Typography>
                        </Box>
                      </Box>

                      <Typography variant="caption" color="text.secondary">
                        v{holon.version} • {holon.size} MB
                      </Typography>
                    </CardContent>

                    <Box sx={{ p: 2, pt: 0 }}>
                      <Button
                        fullWidth
                        variant="contained"
                        startIcon={<Visibility />}
                        onClick={() => handleViewDetails(holon)}
                      >
                        View Details
                      </Button>
                    </Box>
                  </Card>
                </motion.div>
              </Grid>
            ))}
          </Grid>
        )}

        {/* Empty State */}
        {!isLoading && filteredHolons.length === 0 && (
          <Box sx={{ textAlign: 'center', py: 8 }}>
            <DataObject sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
            <Typography variant="h6" color="text.secondary">
              No holons found
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Try adjusting your filters or search term
            </Typography>
          </Box>
        )}

        {/* Details Dialog */}
        <Dialog
          open={detailsDialogOpen}
          onClose={() => setDetailsDialogOpen(false)}
          maxWidth="md"
          fullWidth
        >
          {selectedHolon && (
            <>
              <DialogTitle>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <Avatar sx={{ bgcolor: 'primary.main' }}>
                    <DataObject />
                  </Avatar>
                  <Box>
                    <Typography variant="h6">{selectedHolon.name}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      by {selectedHolon.author}
                    </Typography>
                  </Box>
                </Box>
              </DialogTitle>
              <DialogContent dividers>
                <Grid container spacing={3}>
                  <Grid item xs={12}>
                    <Typography variant="body1" paragraph>
                      {selectedHolon.description}
                    </Typography>
                  </Grid>

                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle2" gutterBottom>
                      Properties:
                    </Typography>
                    <Stack spacing={0.5}>
                      {selectedHolon.properties.map((prop, idx) => (
                        <Chip key={idx} label={prop} size="small" icon={<Memory />} />
                      ))}
                    </Stack>
                  </Grid>

                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle2" gutterBottom>
                      Methods:
                    </Typography>
                    <Stack spacing={0.5}>
                      {selectedHolon.methods.map((method, idx) => (
                        <Chip key={idx} label={method} size="small" icon={<Code />} />
                      ))}
                    </Stack>
                  </Grid>

                  <Grid item xs={12}>
                    <Typography variant="subtitle2" gutterBottom>
                      Tags:
                    </Typography>
                    <Stack direction="row" spacing={1} flexWrap="wrap" gap={1}>
                      {selectedHolon.tags.map((tag) => (
                        <Chip key={tag} label={tag} size="small" />
                      ))}
                    </Stack>
                  </Grid>

                  <Grid item xs={12}>
                    <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                      <Chip icon={<Star />} label={`${selectedHolon.rating} rating`} />
                      <Chip icon={<CloudDownload />} label={`${selectedHolon.downloads.toLocaleString()} downloads`} />
                      <Chip label={`v${selectedHolon.version}`} />
                      <Chip label={`${selectedHolon.size} MB`} />
                      <Chip label={selectedHolon.license} />
                    </Box>
                  </Grid>
                </Grid>
              </DialogContent>
              <DialogActions>
                <Button onClick={() => setDetailsDialogOpen(false)}>Close</Button>
                {selectedHolon.repository && (
                  <Button
                    variant="outlined"
                    startIcon={<Code />}
                    href={selectedHolon.repository}
                    target="_blank"
                  >
                    View Code
                  </Button>
                )}
                <Button
                  variant="contained"
                  startIcon={<Download />}
                  color="primary"
                >
                  Download
                </Button>
              </DialogActions>
            </>
          )}
        </Dialog>
      </motion.div>
    </Container>
  );
};

export default HolonsPage;


