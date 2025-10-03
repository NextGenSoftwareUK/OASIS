import React, { useState } from 'react';
import {
  Box,
  Container,
  Paper,
  TextField,
  Button,
  Typography,
  Alert,
  CircularProgress,
  Link,
  Divider,
  Grid,
  FormControlLabel,
  Checkbox,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Tooltip,
} from '@mui/material';
import {
  PersonAdd as SignupIcon,
  Login as LoginIcon,
  AccountCircle as AccountIcon,
  Help as HelpIcon,
} from '@mui/icons-material';
import { useNavigate, Link as RouterLink } from 'react-router-dom';
import { useAvatar } from '../contexts/AvatarContext';
import { toast } from 'react-hot-toast';

// OASIS Provider Types - Full ProviderType enum from backend
const OASIS_PROVIDERS = [
  { value: 'Auto', label: 'Auto (Let OASIS Choose)', description: 'OASIS will automatically select the best provider for your needs' },
  { value: 'Default', label: 'Default', description: 'Use the default OASIS provider' },
  { value: 'MongoDBOASIS', label: 'MongoDB', description: 'MongoDB document database - Fast and flexible' },
  { value: 'SQLLiteDBOASIS', label: 'SQLite', description: 'SQLite relational database - Local and lightweight' },
  { value: 'Neo4jOASIS', label: 'Neo4j', description: 'Neo4j graph database - Perfect for relationships' },
  { value: 'EthereumOASIS', label: 'Ethereum', description: 'Ethereum blockchain - Decentralized and secure' },
  { value: 'ArbitrumOASIS', label: 'Arbitrum', description: 'Arbitrum Layer 2 - Fast and cheap transactions' },
  { value: 'PolygonOASIS', label: 'Polygon', description: 'Polygon network - Low-cost Ethereum scaling' },
  { value: 'SolanaOASIS', label: 'Solana', description: 'Solana blockchain - High-speed transactions' },
  { value: 'EOSIOOASIS', label: 'EOSIO', description: 'EOSIO blockchain - Enterprise-grade performance' },
  { value: 'TRONOASIS', label: 'TRON', description: 'TRON blockchain - High throughput network' },
  { value: 'HoloOASIS', label: 'Holochain', description: 'Holochain - Agent-centric distributed computing' },
  { value: 'IPFSOASIS', label: 'IPFS', description: 'InterPlanetary File System - Distributed storage' },
  { value: 'PinataOASIS', label: 'Pinata', description: 'Pinata IPFS service - Reliable IPFS hosting' },
  { value: 'AzureStorageOASIS', label: 'Azure Storage', description: 'Microsoft Azure cloud storage' },
  { value: 'AzureCosmosDBOASIS', label: 'Azure Cosmos DB', description: 'Azure Cosmos DB - Global distributed database' },
  { value: 'AWSOASIS', label: 'AWS', description: 'Amazon Web Services cloud platform' },
  { value: 'GoogleCloudOASIS', label: 'Google Cloud', description: 'Google Cloud Platform services' },
  { value: 'LocalFileOASIS', label: 'Local File', description: 'Local file system storage' },
  { value: 'ActivityPubOASIS', label: 'ActivityPub', description: 'ActivityPub protocol - Federated social web' },
  { value: 'ScuttlebuttOASIS', label: 'Scuttlebutt', description: 'Scuttlebutt - Offline-first social network' },
  { value: 'ThreeFoldOASIS', label: 'ThreeFold', description: 'ThreeFold - Decentralized internet infrastructure' },
  { value: 'UrbitOASIS', label: 'Urbit', description: 'Urbit - Personal server platform' },
  { value: 'SOLIDOASIS', label: 'SOLID', description: 'SOLID - Decentralized web standards' },
  { value: 'HoloWebOASIS', label: 'Holo Web', description: 'Holo Web - Distributed web hosting' },
  { value: 'PLANOASIS', label: 'PLAN', description: 'PLAN protocol - Decentralized planning' },
];

const AvatarSignupPage: React.FC = () => {
  const navigate = useNavigate();
  const { signup, isLoading } = useAvatar();
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
    acceptTerms: false,
    provider: 'Auto', // Default to Auto
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const handleInputChange = (field: string) => (event: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({
      ...prev,
      [field]: event.target.type === 'checkbox' ? event.target.checked : event.target.value,
    }));
    
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({
        ...prev,
        [field]: '',
      }));
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    
    if (!formData.firstName.trim()) {
      newErrors.firstName = 'First name is required';
    }
    
    if (!formData.lastName.trim()) {
      newErrors.lastName = 'Last name is required';
    }
    
    if (!formData.username.trim()) {
      newErrors.username = 'Username is required';
    } else if (formData.username.length < 3) {
      newErrors.username = 'Username must be at least 3 characters';
    }
    
    if (!formData.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      newErrors.email = 'Email is invalid';
    }
    
    if (!formData.password) {
      newErrors.password = 'Password is required';
    } else if (formData.password.length < 6) {
      newErrors.password = 'Password must be at least 6 characters';
    }
    
    if (!formData.confirmPassword) {
      newErrors.confirmPassword = 'Please confirm your password';
    } else if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Passwords do not match';
    }
    
    if (!formData.acceptTerms) {
      newErrors.acceptTerms = 'You must accept the terms and conditions';
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    const success = await signup({
      firstName: formData.firstName,
      lastName: formData.lastName,
      username: formData.username,
      email: formData.email,
      password: formData.password,
      confirmPassword: formData.confirmPassword,
      avatarType: 'User',
      acceptTerms: formData.acceptTerms,
    });
    
    if (success) {
      navigate('/');
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
        py: 4,
      }}
    >
      <Container maxWidth="md">
        <Paper elevation={3} sx={{ p: 4 }}>
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <AccountIcon sx={{ fontSize: 80, color: 'primary.main', mb: 2 }} />
            <Typography variant="h4" component="h1" gutterBottom>
              Join The OASIS
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Join the OASIS ecosystem and access STARNET
            </Typography>
          </Box>

          <Alert severity="info" sx={{ mb: 3 }}>
            Create your OASIS Avatar to access STARNET and all its features. 
            Your avatar will be your identity across the OASIS ecosystem.
          </Alert>

          <Box component="form" onSubmit={handleSubmit}>
            <Grid container spacing={3}>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="First Name"
                  value={formData.firstName}
                  onChange={handleInputChange('firstName')}
                  error={!!errors.firstName}
                  helperText={errors.firstName}
                  disabled={isLoading}
                  autoComplete="given-name"
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Last Name"
                  value={formData.lastName}
                  onChange={handleInputChange('lastName')}
                  error={!!errors.lastName}
                  helperText={errors.lastName}
                  disabled={isLoading}
                  autoComplete="family-name"
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Username"
                  value={formData.username}
                  onChange={handleInputChange('username')}
                  error={!!errors.username}
                  helperText={errors.username}
                  disabled={isLoading}
                  autoComplete="username"
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Email"
                  type="email"
                  value={formData.email}
                  onChange={handleInputChange('email')}
                  error={!!errors.email}
                  helperText={errors.email}
                  disabled={isLoading}
                  autoComplete="email"
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Password"
                  type="password"
                  value={formData.password}
                  onChange={handleInputChange('password')}
                  error={!!errors.password}
                  helperText={errors.password}
                  disabled={isLoading}
                  autoComplete="new-password"
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Confirm Password"
                  type="password"
                  value={formData.confirmPassword}
                  onChange={handleInputChange('confirmPassword')}
                  error={!!errors.confirmPassword}
                  helperText={errors.confirmPassword}
                  disabled={isLoading}
                  autoComplete="new-password"
                />
              </Grid>
              <Grid item xs={12}>
                <FormControl fullWidth disabled={isLoading}>
                  <InputLabel>OASIS Provider</InputLabel>
                  <Select
                    value={formData.provider}
                    onChange={(e) => setFormData(prev => ({ ...prev, provider: e.target.value }))}
                    label="OASIS Provider"
                  >
                    {OASIS_PROVIDERS.map((provider) => (
                      <MenuItem key={provider.value} value={provider.value}>
                        <Box sx={{ display: 'flex', alignItems: 'center', width: '100%' }}>
                          <Box sx={{ flexGrow: 1 }}>
                            <Typography variant="body1">{provider.label}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              {provider.description}
                            </Typography>
                          </Box>
                          <Tooltip title={provider.description} arrow>
                            <HelpIcon sx={{ ml: 1, color: 'text.secondary' }} />
                          </Tooltip>
                        </Box>
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12}>
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={formData.acceptTerms}
                      onChange={handleInputChange('acceptTerms')}
                      disabled={isLoading}
                    />
                  }
                  label="I accept the terms and conditions"
                />
                {errors.acceptTerms && (
                  <Typography variant="caption" color="error" sx={{ ml: 4, display: 'block' }}>
                    {errors.acceptTerms}
                  </Typography>
                )}
              </Grid>
            </Grid>

            <Button
              type="submit"
              variant="contained"
              size="large"
              fullWidth
              disabled={isLoading}
              startIcon={isLoading ? <CircularProgress size={20} /> : <SignupIcon />}
              sx={{ mt: 4 }}
            >
              {isLoading ? 'Creating Avatar...' : 'Create Avatar'}
            </Button>
          </Box>

          <Divider sx={{ my: 3 }}>
            <Typography variant="body2" color="text.secondary">
              OR
            </Typography>
          </Divider>

          <Box sx={{ textAlign: 'center' }}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Already have an OASIS Avatar?
            </Typography>
            <Button
              component={RouterLink}
              to="/avatar/signin"
              variant="outlined"
              startIcon={<LoginIcon />}
              disabled={isLoading}
            >
              Sign In
            </Button>
          </Box>

          <Box sx={{ mt: 3, textAlign: 'center' }}>
            <Link component={RouterLink} to="/" variant="body2">
              ← Back to STARNET
            </Link>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
};

export default AvatarSignupPage;
