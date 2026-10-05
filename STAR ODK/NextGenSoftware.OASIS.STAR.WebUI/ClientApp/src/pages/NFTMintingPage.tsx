/**
 * NFT Minting Page
 * Comprehensive NFT minting interface with cross-chain support
 */

import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useDemoMode } from '../contexts/DemoModeContext';
import {
  Box,
  Typography,
  Button,
  Card,
  CardContent,
  Grid,
  Chip,
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
  Fab,
  Tooltip,
  Tabs,
  Tab,
  Badge,
  Stack,
  Avatar,
  CardMedia,
  CardActions,
  Divider,
  Switch,
  FormControlLabel,
  Slider,
  Alert,
  LinearProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Checkbox,
  Radio,
  RadioGroup,
  FormLabel,
} from '@mui/material';
import {
  Add,
  Image,
  LocationOn,
  Link,
  Settings,
  CheckCircle,
  Error,
  Warning,
  Info,
  ExpandMore,
  Upload,
  Download,
  Share,
  Visibility,
  Edit,
  Delete,
  Save,
  Cancel,
  PlayArrow,
  Pause,
  Stop,
  Refresh,
  Search,
  FilterList,
  Sort,
  ViewList,
  ViewModule,
  Star,
  StarBorder,
  Favorite,
  FavoriteBorder,
  Bookmark,
  BookmarkBorder,
  Share as ShareIcon,
  MoreVert,
} from '@mui/icons-material';
import { nftService } from '../services/data/nftService';
import { geoNftService } from '../services/data/geoNftService';

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
      id={`nft-minting-tabpanel-${index}`}
      aria-labelledby={`nft-minting-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

interface NFTMintingPageProps {}

const NFTMintingPage: React.FC<NFTMintingPageProps> = () => {
  const navigate = useNavigate();
  const { isDemoMode } = useDemoMode();
  const [activeTab, setActiveTab] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [mintingProgress, setMintingProgress] = useState(0);
  const [mintingStatus, setMintingStatus] = useState<'idle' | 'minting' | 'success' | 'error'>('idle');

  // NFT Form State
  const [nftForm, setNftForm] = useState({
    name: '',
    description: '',
    image: '',
    metadata: '',
    category: '',
    tags: [] as string[],
    rarity: 'common',
    supply: 1,
    price: 0,
    currency: 'ETH',
    royalties: 0,
    attributes: [] as Array<{ trait_type: string; value: string }>,
  });

  // Geospatial Form State
  const [geoForm, setGeoForm] = useState({
    latitude: 0,
    longitude: 0,
    radius: 100,
    locationName: '',
    locationDescription: '',
    isPublic: true,
    requiresProximity: false,
    proximityRadius: 50,
  });

  // Cross-Chain Settings
  const [crossChainSettings, setCrossChainSettings] = useState({
    enabled: true,
    targetChains: [] as string[],
    autoSync: true,
    gasOptimization: true,
    priorityFee: 'medium',
  });

  // Available Chains
  const availableChains = [
    { id: 'ethereum', name: 'Ethereum', icon: '🔷', gasPrice: 'High' },
    { id: 'solana', name: 'Solana', icon: '🟣', gasPrice: 'Low' },
    { id: 'polygon', name: 'Polygon', icon: '🟣', gasPrice: 'Low' },
    { id: 'arbitrum', name: 'Arbitrum', icon: '🔵', gasPrice: 'Medium' },
    { id: 'optimism', name: 'Optimism', icon: '🔴', gasPrice: 'Medium' },
    { id: 'base', name: 'Base', icon: '🔵', gasPrice: 'Low' },
    { id: 'avalanche', name: 'Avalanche', icon: '🔴', gasPrice: 'Low' },
    { id: 'bnb', name: 'BNB Chain', icon: '🟡', gasPrice: 'Low' },
    { id: 'fantom', name: 'Fantom', icon: '🔵', gasPrice: 'Low' },
    { id: 'cardano', name: 'Cardano', icon: '🔵', gasPrice: 'Low' },
    { id: 'polkadot', name: 'Polkadot', icon: '🟣', gasPrice: 'Medium' },
    { id: 'bitcoin', name: 'Bitcoin', icon: '🟠', gasPrice: 'High' },
    { id: 'near', name: 'NEAR', icon: '🟢', gasPrice: 'Low' },
    { id: 'sui', name: 'Sui', icon: '🔵', gasPrice: 'Low' },
    { id: 'aptos', name: 'Aptos', icon: '🔵', gasPrice: 'Low' },
    { id: 'cosmos', name: 'Cosmos', icon: '🔵', gasPrice: 'Low' },
    { id: 'eosio', name: 'EOSIO', icon: '🔵', gasPrice: 'Low' },
    { id: 'telos', name: 'Telos', icon: '🔵', gasPrice: 'Low' },
    { id: 'seeds', name: 'SEEDS', icon: '🟢', gasPrice: 'Low' },
  ];

  // NFT Categories
  const nftCategories = [
    'Art',
    'Collectibles',
    'Gaming',
    'Music',
    'Sports',
    'Utility',
    'Virtual Real Estate',
    'Domain Names',
    'Tickets',
    'Membership',
    'Other',
  ];

  // Rarity Levels
  const rarityLevels = [
    { value: 'common', label: 'Common', color: '#9E9E9E' },
    { value: 'uncommon', label: 'Uncommon', color: '#4CAF50' },
    { value: 'rare', label: 'Rare', color: '#2196F3' },
    { value: 'epic', label: 'Epic', color: '#9C27B0' },
    { value: 'legendary', label: 'Legendary', color: '#FF9800' },
    { value: 'mythic', label: 'Mythic', color: '#F44336' },
  ];

  // Currencies
  const currencies = [
    { value: 'ETH', label: 'Ethereum (ETH)' },
    { value: 'SOL', label: 'Solana (SOL)' },
    { value: 'MATIC', label: 'Polygon (MATIC)' },
    { value: 'AVAX', label: 'Avalanche (AVAX)' },
    { value: 'BNB', label: 'BNB Chain (BNB)' },
    { value: 'FTM', label: 'Fantom (FTM)' },
    { value: 'ADA', label: 'Cardano (ADA)' },
    { value: 'DOT', label: 'Polkadot (DOT)' },
    { value: 'BTC', label: 'Bitcoin (BTC)' },
    { value: 'NEAR', label: 'NEAR (NEAR)' },
  ];

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleNftFormChange = (field: string, value: any) => {
    setNftForm(prev => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleGeoFormChange = (field: string, value: any) => {
    setGeoForm(prev => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleCrossChainChange = (field: string, value: any) => {
    setCrossChainSettings(prev => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleChainToggle = (chainId: string) => {
    setCrossChainSettings(prev => ({
      ...prev,
      targetChains: prev.targetChains.includes(chainId)
        ? prev.targetChains.filter(id => id !== chainId)
        : [...prev.targetChains, chainId],
    }));
  };

  const handleAttributeAdd = () => {
    setNftForm(prev => ({
      ...prev,
      attributes: [...prev.attributes, { trait_type: '', value: '' }],
    }));
  };

  const handleAttributeChange = (index: number, field: string, value: string) => {
    setNftForm(prev => ({
      ...prev,
      attributes: prev.attributes.map((attr, i) =>
        i === index ? { ...attr, [field]: value } : attr
      ),
    }));
  };

  const handleAttributeRemove = (index: number) => {
    setNftForm(prev => ({
      ...prev,
      attributes: prev.attributes.filter((_, i) => i !== index),
    }));
  };

  const handleMintNFT = async () => {
    setIsLoading(true);
    setMintingStatus('minting');
    setMintingProgress(0);

    try {
      // Simulate minting progress
      const progressInterval = setInterval(() => {
        setMintingProgress(prev => {
          if (prev >= 100) {
            clearInterval(progressInterval);
            setMintingStatus('success');
            setIsLoading(false);
            return 100;
          }
          return prev + 10;
        });
      }, 500);

      // Here you would call the actual minting API
      // await nftService.mintNFT({
      //   ...nftForm,
      //   geoLocation: geoForm,
      //   crossChain: crossChainSettings,
      // });

    } catch (error) {
      console.error('Error minting NFT:', error);
      setMintingStatus('error');
      setIsLoading(false);
    }
  };

  const handleMintGeoNFT = async () => {
    setIsLoading(true);
    setMintingStatus('minting');
    setMintingProgress(0);

    try {
      // Simulate minting progress
      const progressInterval = setInterval(() => {
        setMintingProgress(prev => {
          if (prev >= 100) {
            clearInterval(progressInterval);
            setMintingStatus('success');
            setIsLoading(false);
            return 100;
          }
          return prev + 10;
        });
      }, 500);

      // Here you would call the actual GeoNFT minting API
      // await geoNftService.mintGeoNFT({
      //   ...nftForm,
      //   geoLocation: geoForm,
      //   crossChain: crossChainSettings,
      // });

    } catch (error) {
      console.error('Error minting GeoNFT:', error);
      setMintingStatus('error');
      setIsLoading(false);
    }
  };

  const handleMintSTARNFT = async () => {
    setIsLoading(true);
    setMintingStatus('minting');
    setMintingProgress(0);

    try {
      // Simulate minting progress
      const progressInterval = setInterval(() => {
        setMintingProgress(prev => {
          if (prev >= 100) {
            clearInterval(progressInterval);
            setMintingStatus('success');
            setIsLoading(false);
            return 100;
          }
          return prev + 10;
        });
      }, 500);

      // Here you would call the actual STAR NFT minting API
      // await nftService.mintSTARNFT({
      //   ...nftForm,
      //   geoLocation: geoForm,
      //   crossChain: crossChainSettings,
      // });

    } catch (error) {
      console.error('Error minting STAR NFT:', error);
      setMintingStatus('error');
      setIsLoading(false);
    }
  };

  const handleMintSTARGeoNFT = async () => {
    setIsLoading(true);
    setMintingStatus('minting');
    setMintingProgress(0);

    try {
      // Simulate minting progress
      const progressInterval = setInterval(() => {
        setMintingProgress(prev => {
          if (prev >= 100) {
            clearInterval(progressInterval);
            setMintingStatus('success');
            setIsLoading(false);
            return 100;
          }
          return prev + 10;
        });
      }, 500);

      // Here you would call the actual STAR GeoNFT minting API
      // await geoNftService.mintSTARGeoNFT({
      //   ...nftForm,
      //   geoLocation: geoForm,
      //   crossChain: crossChainSettings,
      // });

    } catch (error) {
      console.error('Error minting STAR GeoNFT:', error);
      setMintingStatus('error');
      setIsLoading(false);
    }
  };

  const getStatusIcon = () => {
    switch (mintingStatus) {
      case 'minting':
        return <LinearProgress variant="determinate" value={mintingProgress} />;
      case 'success':
        return <CheckCircle color="success" />;
      case 'error':
        return <Error color="error" />;
      default:
        return null;
    }
  };

  const getStatusMessage = () => {
    switch (mintingStatus) {
      case 'minting':
        return `Minting NFT... ${mintingProgress}%`;
      case 'success':
        return 'NFT minted successfully!';
      case 'error':
        return 'Error minting NFT. Please try again.';
      default:
        return '';
    }
  };

  return (
    <Box sx={{ flexGrow: 1, p: 3 }}>
      {/* Header */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          NFT Minting Studio
        </Typography>
        <Typography variant="subtitle1" color="text.secondary">
          Create, mint, and manage NFTs with cross-chain support
        </Typography>
      </Box>

      {/* Status Alert */}
      {mintingStatus !== 'idle' && (
        <Alert
          severity={mintingStatus === 'error' ? 'error' : mintingStatus === 'success' ? 'success' : 'info'}
          sx={{ mb: 3 }}
          icon={getStatusIcon()}
        >
          {getStatusMessage()}
        </Alert>
      )}

      {/* Main Content */}
      <Card>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={activeTab} onChange={handleTabChange} aria-label="NFT minting tabs">
            <Tab label="WEB4 NFT" />
            <Tab label="WEB4 GEO-NFT" />
            <Tab label="WEB5 STAR NFT" />
            <Tab label="WEB5 STAR GEO-NFT" />
            <Tab label="Cross-Chain Settings" />
          </Tabs>
        </Box>

        {/* WEB4 NFT Tab */}
        <TabPanel value={activeTab} index={0}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                NFT Details
              </Typography>
              
              <Stack spacing={2}>
                <TextField
                  fullWidth
                  label="NFT Name"
                  value={nftForm.name}
                  onChange={(e) => handleNftFormChange('name', e.target.value)}
                  placeholder="Enter NFT name"
                />
                
                <TextField
                  fullWidth
                  multiline
                  rows={3}
                  label="Description"
                  value={nftForm.description}
                  onChange={(e) => handleNftFormChange('description', e.target.value)}
                  placeholder="Describe your NFT"
                />
                
                <TextField
                  fullWidth
                  label="Image URL"
                  value={nftForm.image}
                  onChange={(e) => handleNftFormChange('image', e.target.value)}
                  placeholder="https://example.com/image.png"
                />
                
                <FormControl fullWidth>
                  <InputLabel>Category</InputLabel>
                  <Select
                    value={nftForm.category}
                    onChange={(e) => handleNftFormChange('category', e.target.value)}
                  >
                    {nftCategories.map((category) => (
                      <MenuItem key={category} value={category}>
                        {category}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                
                <FormControl fullWidth>
                  <InputLabel>Rarity</InputLabel>
                  <Select
                    value={nftForm.rarity}
                    onChange={(e) => handleNftFormChange('rarity', e.target.value)}
                  >
                    {rarityLevels.map((rarity) => (
                      <MenuItem key={rarity.value} value={rarity.value}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Box
                            sx={{
                              width: 12,
                              height: 12,
                              borderRadius: '50%',
                              backgroundColor: rarity.color,
                            }}
                          />
                          {rarity.label}
                        </Box>
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                
                <TextField
                  fullWidth
                  type="number"
                  label="Supply"
                  value={nftForm.supply}
                  onChange={(e) => handleNftFormChange('supply', parseInt(e.target.value))}
                  inputProps={{ min: 1 }}
                />
                
                <TextField
                  fullWidth
                  type="number"
                  label="Price"
                  value={nftForm.price}
                  onChange={(e) => handleNftFormChange('price', parseFloat(e.target.value))}
                  inputProps={{ min: 0, step: 0.01 }}
                />
                
                <FormControl fullWidth>
                  <InputLabel>Currency</InputLabel>
                  <Select
                    value={nftForm.currency}
                    onChange={(e) => handleNftFormChange('currency', e.target.value)}
                  >
                    {currencies.map((currency) => (
                      <MenuItem key={currency.value} value={currency.value}>
                        {currency.label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                
                <TextField
                  fullWidth
                  type="number"
                  label="Royalties (%)"
                  value={nftForm.royalties}
                  onChange={(e) => handleNftFormChange('royalties', parseFloat(e.target.value))}
                  inputProps={{ min: 0, max: 100, step: 0.1 }}
                />
              </Stack>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                Attributes
              </Typography>
              
              <Stack spacing={2}>
                {nftForm.attributes.map((attr, index) => (
                  <Box key={index} sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                    <TextField
                      label="Trait Type"
                      value={attr.trait_type}
                      onChange={(e) => handleAttributeChange(index, 'trait_type', e.target.value)}
                      size="small"
                    />
                    <TextField
                      label="Value"
                      value={attr.value}
                      onChange={(e) => handleAttributeChange(index, 'value', e.target.value)}
                      size="small"
                    />
                    <IconButton
                      onClick={() => handleAttributeRemove(index)}
                      color="error"
                    >
                      <Delete />
                    </IconButton>
                  </Box>
                ))}
                
                <Button
                  startIcon={<Add />}
                  onClick={handleAttributeAdd}
                  variant="outlined"
                >
                  Add Attribute
                </Button>
              </Stack>
              
              <Box sx={{ mt: 3 }}>
                <Button
                  variant="contained"
                  size="large"
                  onClick={handleMintNFT}
                  disabled={isLoading || !nftForm.name}
                  startIcon={<Add />}
                  fullWidth
                >
                  Mint Basic NFT
                </Button>
              </Box>
            </Grid>
          </Grid>
        </TabPanel>

        {/* WEB4 GEO-NFT Tab */}
        <TabPanel value={activeTab} index={1}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                NFT Details
              </Typography>
              
              <Stack spacing={2}>
                <TextField
                  fullWidth
                  label="NFT Name"
                  value={nftForm.name}
                  onChange={(e) => handleNftFormChange('name', e.target.value)}
                  placeholder="Enter NFT name"
                />
                
                <TextField
                  fullWidth
                  multiline
                  rows={3}
                  label="Description"
                  value={nftForm.description}
                  onChange={(e) => handleNftFormChange('description', e.target.value)}
                  placeholder="Describe your NFT"
                />
                
                <TextField
                  fullWidth
                  label="Image URL"
                  value={nftForm.image}
                  onChange={(e) => handleNftFormChange('image', e.target.value)}
                  placeholder="https://example.com/image.png"
                />
              </Stack>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                Location Details
              </Typography>
              
              <Stack spacing={2}>
                <TextField
                  fullWidth
                  label="Location Name"
                  value={geoForm.locationName}
                  onChange={(e) => handleGeoFormChange('locationName', e.target.value)}
                  placeholder="Enter location name"
                />
                
                <TextField
                  fullWidth
                  multiline
                  rows={2}
                  label="Location Description"
                  value={geoForm.locationDescription}
                  onChange={(e) => handleGeoFormChange('locationDescription', e.target.value)}
                  placeholder="Describe the location"
                />
                
                <TextField
                  fullWidth
                  type="number"
                  label="Latitude"
                  value={geoForm.latitude}
                  onChange={(e) => handleGeoFormChange('latitude', parseFloat(e.target.value))}
                  inputProps={{ min: -90, max: 90, step: 0.000001 }}
                />
                
                <TextField
                  fullWidth
                  type="number"
                  label="Longitude"
                  value={geoForm.longitude}
                  onChange={(e) => handleGeoFormChange('longitude', parseFloat(e.target.value))}
                  inputProps={{ min: -180, max: 180, step: 0.000001 }}
                />
                
                <TextField
                  fullWidth
                  type="number"
                  label="Radius (meters)"
                  value={geoForm.radius}
                  onChange={(e) => handleGeoFormChange('radius', parseInt(e.target.value))}
                  inputProps={{ min: 1, max: 10000 }}
                />
                
                <FormControlLabel
                  control={
                    <Switch
                      checked={geoForm.isPublic}
                      onChange={(e) => handleGeoFormChange('isPublic', e.target.checked)}
                    />
                  }
                  label="Public Location"
                />
                
                <FormControlLabel
                  control={
                    <Switch
                      checked={geoForm.requiresProximity}
                      onChange={(e) => handleGeoFormChange('requiresProximity', e.target.checked)}
                    />
                  }
                  label="Requires Proximity"
                />
                
                {geoForm.requiresProximity && (
                  <TextField
                    fullWidth
                    type="number"
                    label="Proximity Radius (meters)"
                    value={geoForm.proximityRadius}
                    onChange={(e) => handleGeoFormChange('proximityRadius', parseInt(e.target.value))}
                    inputProps={{ min: 1, max: 1000 }}
                  />
                )}
              </Stack>
              
              <Box sx={{ mt: 3 }}>
                <Button
                  variant="contained"
                  size="large"
                  onClick={handleMintGeoNFT}
                  disabled={isLoading || !nftForm.name || !geoForm.locationName}
                  startIcon={<LocationOn />}
                  fullWidth
                >
                  Mint Geo-NFT
                </Button>
              </Box>
            </Grid>
          </Grid>
        </TabPanel>

        {/* WEB5 STAR NFT Tab */}
        <TabPanel value={activeTab} index={2}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                STAR NFT Details
              </Typography>
              
              <Stack spacing={2}>
                <TextField
                  fullWidth
                  label="NFT Name"
                  value={nftForm.name}
                  onChange={(e) => handleNftFormChange('name', e.target.value)}
                  placeholder="Enter NFT name"
                />
                
                <TextField
                  fullWidth
                  multiline
                  rows={3}
                  label="Description"
                  value={nftForm.description}
                  onChange={(e) => handleNftFormChange('description', e.target.value)}
                  placeholder="Describe your NFT"
                />
                
                <TextField
                  fullWidth
                  label="Image URL"
                  value={nftForm.image}
                  onChange={(e) => handleNftFormChange('image', e.target.value)}
                  placeholder="https://example.com/image.png"
                />
                
                <FormControl fullWidth>
                  <InputLabel>Category</InputLabel>
                  <Select
                    value={nftForm.category}
                    onChange={(e) => handleNftFormChange('category', e.target.value)}
                  >
                    {nftCategories.map((category) => (
                      <MenuItem key={category} value={category}>
                        {category}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Stack>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                STARNET Features
              </Typography>
              
              <Stack spacing={2}>
                <FormControlLabel
                  control={<Switch defaultChecked />}
                  label="Version Control"
                />
                
                <FormControlLabel
                  control={<Switch defaultChecked />}
                  label="Change Tracking"
                />
                
                <FormControlLabel
                  control={<Switch defaultChecked />}
                  label="Publishing"
                />
                
                <FormControlLabel
                  control={<Switch defaultChecked />}
                  label="Search & Discovery"
                />
                
                <FormControlLabel
                  control={<Switch defaultChecked />}
                  label="Download & Install"
                />
              </Stack>
              
              <Box sx={{ mt: 3 }}>
                <Button
                  variant="contained"
                  size="large"
                  onClick={handleMintSTARNFT}
                  disabled={isLoading || !nftForm.name}
                  startIcon={<Star />}
                  fullWidth
                >
                  Mint STAR NFT
                </Button>
              </Box>
            </Grid>
          </Grid>
        </TabPanel>

        {/* WEB5 STAR GEO-NFT Tab */}
        <TabPanel value={activeTab} index={3}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                STAR Geo-NFT Details
              </Typography>
              
              <Stack spacing={2}>
                <TextField
                  fullWidth
                  label="NFT Name"
                  value={nftForm.name}
                  onChange={(e) => handleNftFormChange('name', e.target.value)}
                  placeholder="Enter NFT name"
                />
                
                <TextField
                  fullWidth
                  multiline
                  rows={3}
                  label="Description"
                  value={nftForm.description}
                  onChange={(e) => handleNftFormChange('description', e.target.value)}
                  placeholder="Describe your NFT"
                />
                
                <TextField
                  fullWidth
                  label="Image URL"
                  value={nftForm.image}
                  onChange={(e) => handleNftFormChange('image', e.target.value)}
                  placeholder="https://example.com/image.png"
                />
              </Stack>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                Location & STARNET Features
              </Typography>
              
              <Stack spacing={2}>
                <TextField
                  fullWidth
                  label="Location Name"
                  value={geoForm.locationName}
                  onChange={(e) => handleGeoFormChange('locationName', e.target.value)}
                  placeholder="Enter location name"
                />
                
                <TextField
                  fullWidth
                  type="number"
                  label="Latitude"
                  value={geoForm.latitude}
                  onChange={(e) => handleGeoFormChange('latitude', parseFloat(e.target.value))}
                  inputProps={{ min: -90, max: 90, step: 0.000001 }}
                />
                
                <TextField
                  fullWidth
                  type="number"
                  label="Longitude"
                  value={geoForm.longitude}
                  onChange={(e) => handleGeoFormChange('longitude', parseFloat(e.target.value))}
                  inputProps={{ min: -180, max: 180, step: 0.000001 }}
                />
                
                <FormControlLabel
                  control={<Switch defaultChecked />}
                  label="Version Control"
                />
                
                <FormControlLabel
                  control={<Switch defaultChecked />}
                  label="Change Tracking"
                />
                
                <FormControlLabel
                  control={<Switch defaultChecked />}
                  label="Publishing"
                />
                
                <FormControlLabel
                  control={<Switch defaultChecked />}
                  label="Search & Discovery"
                />
                
                <FormControlLabel
                  control={<Switch defaultChecked />}
                  label="Download & Install"
                />
              </Stack>
              
              <Box sx={{ mt: 3 }}>
                <Button
                  variant="contained"
                  size="large"
                  onClick={handleMintSTARGeoNFT}
                  disabled={isLoading || !nftForm.name || !geoForm.locationName}
                  startIcon={<Star />}
                  fullWidth
                >
                  Mint STAR Geo-NFT
                </Button>
              </Box>
            </Grid>
          </Grid>
        </TabPanel>

        {/* Cross-Chain Settings Tab */}
        <TabPanel value={activeTab} index={4}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Typography variant="h6" gutterBottom>
                Cross-Chain Configuration
              </Typography>
              
              <Stack spacing={3}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={crossChainSettings.enabled}
                      onChange={(e) => handleCrossChainChange('enabled', e.target.checked)}
                    />
                  }
                  label="Enable Cross-Chain Minting"
                />
                
                <FormControlLabel
                  control={
                    <Switch
                      checked={crossChainSettings.autoSync}
                      onChange={(e) => handleCrossChainChange('autoSync', e.target.checked)}
                    />
                  }
                  label="Auto-Sync Across Chains"
                />
                
                <FormControlLabel
                  control={
                    <Switch
                      checked={crossChainSettings.gasOptimization}
                      onChange={(e) => handleCrossChainChange('gasOptimization', e.target.checked)}
                    />
                  }
                  label="Gas Optimization"
                />
                
                <FormControl fullWidth>
                  <InputLabel>Priority Fee</InputLabel>
                  <Select
                    value={crossChainSettings.priorityFee}
                    onChange={(e) => handleCrossChainChange('priorityFee', e.target.value)}
                  >
                    <MenuItem value="low">Low</MenuItem>
                    <MenuItem value="medium">Medium</MenuItem>
                    <MenuItem value="high">High</MenuItem>
                  </Select>
                </FormControl>
              </Stack>
            </Grid>
            
            <Grid item xs={12}>
              <Typography variant="h6" gutterBottom>
                Target Chains
              </Typography>
              
              <Grid container spacing={2}>
                {availableChains.map((chain) => (
                  <Grid item xs={12} sm={6} md={4} key={chain.id}>
                    <Card
                      sx={{
                        cursor: 'pointer',
                        border: crossChainSettings.targetChains.includes(chain.id) ? 2 : 1,
                        borderColor: crossChainSettings.targetChains.includes(chain.id) ? 'primary.main' : 'divider',
                      }}
                      onClick={() => handleChainToggle(chain.id)}
                    >
                      <CardContent sx={{ p: 2 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                          <Typography variant="h6">{chain.icon}</Typography>
                          <Typography variant="subtitle1">{chain.name}</Typography>
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                          Gas Price: {chain.gasPrice}
                        </Typography>
                        <Checkbox
                          checked={crossChainSettings.targetChains.includes(chain.id)}
                          onChange={() => handleChainToggle(chain.id)}
                          sx={{ position: 'absolute', top: 8, right: 8 }}
                        />
                      </CardContent>
                    </Card>
                  </Grid>
                ))}
              </Grid>
            </Grid>
          </Grid>
        </TabPanel>
      </Card>
    </Box>
  );
};

export default NFTMintingPage;
