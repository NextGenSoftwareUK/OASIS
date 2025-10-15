import React, { useState, useEffect } from 'react';
import {
  Box, Typography, Card, CardContent, Grid, Button, Chip, TextField,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
  IconButton, Tooltip, Alert, AlertTitle, CircularProgress, Divider,
  List, ListItem, ListItemText, ListItemIcon, ListItemSecondaryAction,
  Dialog, DialogTitle, DialogContent, DialogActions, DialogContentText,
  FormControl, InputLabel, Select, MenuItem, Avatar, LinearProgress,
  Tabs, Tab, Badge, Autocomplete, InputAdornment
} from '@mui/material';
import {
  Search, FilterList, Sort, Apps, Code, LibraryBooks, Image,
  Public, EmojiEvents, FlightTakeoff, Inventory, Star, Refresh,
  TrendingUp, History, Clear, ViewList, ViewModule
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useQueryClient } from 'react-query';
import { searchService } from '../services/core/searchService';

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
      id={`search-tabpanel-${index}`}
      aria-labelledby={`search-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const SearchPage: React.FC = () => {
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<any>(null);
  const [isSearching, setIsSearching] = useState(false);
  const [tabValue, setTabValue] = useState(0);
  const [viewMode, setViewMode] = useState<'list' | 'grid'>('list');
  const [sortBy, setSortBy] = useState('relevance');
  const [filterType, setFilterType] = useState('all');
  const [searchHistory, setSearchHistory] = useState<string[]>([]);
  const queryClient = useQueryClient();

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleSearch = async (query: string) => {
    if (!query.trim()) return;
    
    setIsSearching(true);
    try {
      const results = await searchService.globalSearch(query, {
        type: filterType !== 'all' ? filterType : undefined,
        sortBy
      });
      setSearchResults(results.result);
      
      // Add to search history
      setSearchHistory(prev => {
        const newHistory = [query, ...prev.filter(h => h !== query)];
        return newHistory.slice(0, 10);
      });
    } catch (error) {
      console.error('Search failed:', error);
    } finally {
      setIsSearching(false);
    }
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    handleSearch(searchQuery);
  };

  const getEntityIcon = (type: string) => {
    switch (type) {
      case 'oapps': return <Apps />;
      case 'templates': return <Code />;
      case 'runtimes': return <Code />;
      case 'libraries': return <LibraryBooks />;
      case 'nfts': return <Image />;
      case 'geonfts': return <Public />;
      case 'quests': return <EmojiEvents />;
      case 'missions': return <FlightTakeoff />;
      case 'inventory': return <Inventory />;
      default: return <Star />;
    }
  };

  const getEntityColor = (type: string) => {
    switch (type) {
      case 'oapps': return 'primary';
      case 'templates': return 'secondary';
      case 'runtimes': return 'info';
      case 'libraries': return 'success';
      case 'nfts': return 'warning';
      case 'geonfts': return 'error';
      case 'quests': return 'primary';
      case 'missions': return 'secondary';
      default: return 'default';
    }
  };

  const renderSearchResults = (results: any[], type: string) => {
    if (!results || results.length === 0) {
      return (
        <Typography color="text.secondary" sx={{ textAlign: 'center', py: 4 }}>
          No {type} found
        </Typography>
      );
    }

    if (viewMode === 'grid') {
      return (
        <Grid container spacing={2}>
          {results.map((item: any) => (
            <Grid item xs={12} sm={6} md={4} key={item.id}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Avatar sx={{ bgcolor: `${getEntityColor(type)}.main`, mr: 2 }}>
                      {getEntityIcon(type)}
                    </Avatar>
                    <Box sx={{ flexGrow: 1 }}>
                      <Typography variant="h6" noWrap>
                        {item.name}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {item.type}
                      </Typography>
                    </Box>
                    <Chip 
                      label={`${Math.round(item.relevance * 100)}%`}
                      color={getEntityColor(type) as any}
                      size="small"
                    />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Relevance: {Math.round(item.relevance * 100)}%
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      );
    }

    return (
      <List>
        {results.map((item: any) => (
          <ListItem key={item.id} divider>
            <ListItemIcon>
              {getEntityIcon(type)}
            </ListItemIcon>
            <ListItemText
              primary={item.name}
              secondary={`Type: ${item.type} • Relevance: ${Math.round(item.relevance * 100)}%`}
            />
            <ListItemSecondaryAction>
              <Chip 
                label={`${Math.round(item.relevance * 100)}%`}
                color={getEntityColor(type) as any}
                size="small"
              />
            </ListItemSecondaryAction>
          </ListItem>
        ))}
      </List>
    );
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
            <Search color="primary" />
            Global Search
          </Typography>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="outlined"
              startIcon={viewMode === 'list' ? <ViewModule /> : <ViewList />}
              onClick={() => setViewMode(viewMode === 'list' ? 'grid' : 'list')}
            >
              {viewMode === 'list' ? 'Grid View' : 'List View'}
            </Button>
          </Box>
        </Box>

        {/* Search Bar */}
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <form onSubmit={handleSearchSubmit}>
              <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                <TextField
                  fullWidth
                  placeholder="Search across all STARNET entities..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Search />
                      </InputAdornment>
                    ),
                    endAdornment: searchQuery && (
                      <InputAdornment position="end">
                        <IconButton onClick={() => setSearchQuery('')} edge="end">
                          <Clear />
                        </IconButton>
                      </InputAdornment>
                    )
                  }}
                />
                <Button
                  type="submit"
                  variant="contained"
                  disabled={!searchQuery.trim() || isSearching}
                  startIcon={isSearching ? <CircularProgress size={20} /> : <Search />}
                >
                  {isSearching ? 'Searching...' : 'Search'}
                </Button>
              </Box>
              
              <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                <FormControl size="small" sx={{ minWidth: 120 }}>
                  <InputLabel>Filter Type</InputLabel>
                  <Select
                    value={filterType}
                    onChange={(e) => setFilterType(e.target.value)}
                    label="Filter Type"
                  >
                    <MenuItem value="all">All Types</MenuItem>
                    <MenuItem value="oapps">OAPPs</MenuItem>
                    <MenuItem value="templates">Templates</MenuItem>
                    <MenuItem value="runtimes">Runtimes</MenuItem>
                    <MenuItem value="libraries">Libraries</MenuItem>
                    <MenuItem value="nfts">NFTs</MenuItem>
                    <MenuItem value="geonfts">GeoNFTs</MenuItem>
                    <MenuItem value="quests">Quests</MenuItem>
                    <MenuItem value="missions">Missions</MenuItem>
                  </Select>
                </FormControl>
                
                <FormControl size="small" sx={{ minWidth: 120 }}>
                  <InputLabel>Sort By</InputLabel>
                  <Select
                    value={sortBy}
                    onChange={(e) => setSortBy(e.target.value)}
                    label="Sort By"
                  >
                    <MenuItem value="relevance">Relevance</MenuItem>
                    <MenuItem value="name">Name</MenuItem>
                    <MenuItem value="type">Type</MenuItem>
                    <MenuItem value="date">Date</MenuItem>
                  </Select>
                </FormControl>
              </Box>
            </form>
          </CardContent>
        </Card>

        {/* Search History */}
        {searchHistory.length > 0 && (
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <History color="primary" />
                Recent Searches
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {searchHistory.map((query, index) => (
                  <Chip
                    key={index}
                    label={query}
                    onClick={() => {
                      setSearchQuery(query);
                      handleSearch(query);
                    }}
                    variant="outlined"
                    size="small"
                  />
                ))}
              </Box>
            </CardContent>
          </Card>
        )}

        {/* Search Results */}
        {searchResults && (
          <Card>
            <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
              <Tabs value={tabValue} onChange={handleTabChange} aria-label="Search results tabs">
                {Object.entries(searchResults.results || {}).map(([type, results]: [string, any]) => (
                  <Tab 
                    key={type}
                    label={
                      <Badge badgeContent={results?.length || 0} color="primary">
                        {type.charAt(0).toUpperCase() + type.slice(1)}
                      </Badge>
                    }
                  />
                ))}
              </Tabs>
            </Box>

            {Object.entries(searchResults.results || {}).map(([type, results]: [string, any], index) => (
              <TabPanel key={type} value={tabValue} index={index}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                  <Typography variant="h6">
                    {type.charAt(0).toUpperCase() + type.slice(1)} ({results?.length || 0} results)
                  </Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography variant="body2" color="text.secondary">
                      Total: {searchResults.totalResults}
                    </Typography>
                  </Box>
                </Box>
                {renderSearchResults(results || [], type)}
              </TabPanel>
            ))}
          </Card>
        )}

        {/* No Results */}
        {searchResults && searchResults.totalResults === 0 && (
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 4 }}>
              <Search sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
              <Typography variant="h6" color="text.secondary" gutterBottom>
                No results found for "{searchQuery}"
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Try adjusting your search terms or filters
              </Typography>
            </CardContent>
          </Card>
        )}

        {/* Search Tips */}
        {!searchResults && (
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <TrendingUp color="primary" />
                Search Tips
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" gutterBottom>
                    Search Tips:
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemText primary="Use specific keywords for better results" />
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="Try different search terms if no results" />
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="Use filters to narrow down results" />
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="Search is case-insensitive" />
                    </ListItem>
                  </List>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" gutterBottom>
                    Searchable Entities:
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {['OAPPs', 'Templates', 'Runtimes', 'Libraries', 'NFTs', 'GeoNFTs', 'Quests', 'Missions'].map((entity) => (
                      <Chip key={entity} label={entity} size="small" variant="outlined" />
                    ))}
                  </Box>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        )}
      </motion.div>
    </Box>
  );
};

export default SearchPage;
