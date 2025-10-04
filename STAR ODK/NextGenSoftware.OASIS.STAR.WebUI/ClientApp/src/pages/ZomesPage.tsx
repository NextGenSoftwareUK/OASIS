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
  Extension,
  Functions,
  IntegrationInstructions,
  Security,
  Category,
  Download
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery } from 'react-query';
import { zomeService } from '../services';
import { Link } from 'react-router-dom';

interface Zome {
  id: string;
  name: string;
  description: string;
  imageUrl?: string;
  version: string;
  category: string;
  type: string;
  language: string;
  framework: string;
  author: string;
  downloads: number;
  rating: number;
  size: number;
  lastUpdated: string;
  isPublic: boolean;
  isFeatured: boolean;
  tags: string[];
  functions: string[];
  dependencies: string[];
  apis: string[];
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

const ZomesPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('All');
  const [viewFilter, setViewFilter] = useState('All');
  const [selectedZome, setSelectedZome] = useState<Zome | null>(null);
  const [detailsDialogOpen, setDetailsDialogOpen] = useState(false);

  // Fetch data based on view filter
  const { data: zomesData, isLoading, error } = useQuery(
    ['zomes', viewFilter],
    async () => {
      if (viewFilter === 'Installed') {
        return zomeService.getInstalled();
      } else if (viewFilter === 'My Zomes') {
        return zomeService.getForAvatar('current-avatar-id');
      } else {
        return zomeService.getAll();
      }
    }
  );

  const zomes: Zome[] = zomesData?.result || [];

  // Filter zomes based on search and category
  const filteredZomes = zomes.filter((zome) => {
    const matchesSearch = zome.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         zome.description.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesCategory = categoryFilter === 'All' || zome.category === categoryFilter;
    return matchesSearch && matchesCategory;
  });

  const categories = ['All', ...Array.from(new Set(zomes.map(z => z.category)))];

  const handleViewDetails = (zome: Zome) => {
    setSelectedZome(zome);
    setDetailsDialogOpen(true);
  };

  if (error) {
    return (
      <Container maxWidth="xl" sx={{ mt: 4 }}>
        <Alert severity="error">Failed to load zomes. Please try again later.</Alert>
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
            <Avatar sx={{ bgcolor: 'secondary.main', width: 56, height: 56 }}>
              <Extension fontSize="large" />
            </Avatar>
            <Box>
              <Typography variant="h4" gutterBottom className="page-heading">
                ⚡ Zomes (OASIS Code Modules)
              </Typography>
              <Typography variant="subtitle1" color="text.secondary">
                Plug-and-play code modules and functions for rapid OAPP development
              </Typography>
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
                <MenuItem value="My Zomes">My Zomes</MenuItem>
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
              placeholder="Search zomes..."
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

        {/* Zomes Grid */}
        {!isLoading && (
          <Grid container spacing={3}>
            {filteredZomes.map((zome) => (
              <Grid item xs={12} sm={6} md={4} lg={3} key={zome.id}>
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
                    {zome.imageUrl && (
                      <CardMedia
                        component="img"
                        height="140"
                        image={zome.imageUrl}
                        alt={zome.name}
                      />
                    )}

                    {/* Installed Badge */}
                    {zome.isInstalled && (
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
                        <Extension color="secondary" />
                        <Typography variant="h6" component="div" noWrap>
                          {zome.name}
                        </Typography>
                      </Box>

                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2, minHeight: 40 }}>
                        {zome.description.length > 80
                          ? `${zome.description.substring(0, 80)}...`
                          : zome.description}
                      </Typography>

                      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap', gap: 0.5 }}>
                        <Chip label={zome.category} size="small" color="secondary" variant="outlined" />
                        <Chip label={zome.language} size="small" />
                      </Stack>

                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Star sx={{ color: '#ffc107', fontSize: 18 }} />
                          <Typography variant="body2">{zome.rating}</Typography>
                        </Box>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <CloudDownload sx={{ fontSize: 18, color: 'text.secondary' }} />
                          <Typography variant="body2" color="text.secondary">
                            {zome.downloads.toLocaleString()}
                          </Typography>
                        </Box>
                      </Box>

                      <Typography variant="caption" color="text.secondary">
                        v{zome.version} • {zome.size} MB
                      </Typography>
                    </CardContent>

                    <Box sx={{ p: 2, pt: 0 }}>
                      <Button
                        fullWidth
                        variant="contained"
                        color="secondary"
                        startIcon={<Visibility />}
                        onClick={() => handleViewDetails(zome)}
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
        {!isLoading && filteredZomes.length === 0 && (
          <Box sx={{ textAlign: 'center', py: 8 }}>
            <Extension sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
            <Typography variant="h6" color="text.secondary">
              No zomes found
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
          {selectedZome && (
            <>
              <DialogTitle>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <Avatar sx={{ bgcolor: 'secondary.main' }}>
                    <Extension />
                  </Avatar>
                  <Box>
                    <Typography variant="h6">{selectedZome.name}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      by {selectedZome.author}
                    </Typography>
                  </Box>
                </Box>
              </DialogTitle>
              <DialogContent dividers>
                <Grid container spacing={3}>
                  <Grid item xs={12}>
                    <Typography variant="body1" paragraph>
                      {selectedZome.description}
                    </Typography>
                  </Grid>

                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle2" gutterBottom>
                      Functions:
                    </Typography>
                    <Stack spacing={0.5}>
                      {selectedZome.functions.map((func, idx) => (
                        <Chip key={idx} label={func} size="small" icon={<Functions />} />
                      ))}
                    </Stack>
                  </Grid>

                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle2" gutterBottom>
                      APIs:
                    </Typography>
                    <Stack spacing={0.5}>
                      {selectedZome.apis.map((api, idx) => (
                        <Chip key={idx} label={api} size="small" icon={<IntegrationInstructions />} />
                      ))}
                    </Stack>
                  </Grid>

                  <Grid item xs={12}>
                    <Typography variant="subtitle2" gutterBottom>
                      Tags:
                    </Typography>
                    <Stack direction="row" spacing={1} flexWrap="wrap" gap={1}>
                      {selectedZome.tags.map((tag) => (
                        <Chip key={tag} label={tag} size="small" />
                      ))}
                    </Stack>
                  </Grid>

                  <Grid item xs={12}>
                    <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                      <Chip icon={<Star />} label={`${selectedZome.rating} rating`} />
                      <Chip icon={<CloudDownload />} label={`${selectedZome.downloads.toLocaleString()} downloads`} />
                      <Chip label={`v${selectedZome.version}`} />
                      <Chip label={`${selectedZome.size} MB`} />
                      <Chip label={selectedZome.license} />
                      <Chip label={selectedZome.language} />
                      <Chip label={selectedZome.framework} />
                    </Box>
                  </Grid>
                </Grid>
              </DialogContent>
              <DialogActions>
                <Button onClick={() => setDetailsDialogOpen(false)}>Close</Button>
                {selectedZome.repository && (
                  <Button
                    variant="outlined"
                    startIcon={<Code />}
                    href={selectedZome.repository}
                    target="_blank"
                  >
                    View Code
                  </Button>
                )}
                <Button
                  variant="contained"
                  startIcon={<Download />}
                  color="secondary"
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

export default ZomesPage;


