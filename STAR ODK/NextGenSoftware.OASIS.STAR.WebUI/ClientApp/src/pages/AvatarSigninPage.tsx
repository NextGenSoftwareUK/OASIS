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
  Card,
  CardContent,
  Avatar as MuiAvatar,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Tooltip,
} from '@mui/material';
import {
  Login as LoginIcon,
  PersonAdd as SignupIcon,
  AccountCircle as AccountIcon,
  Help as HelpIcon,
} from '@mui/icons-material';
import { useNavigate, Link as RouterLink } from 'react-router-dom';
import { useAvatar } from '../contexts/AvatarContext';
import { toast } from 'react-hot-toast';

import { OASIS_PROVIDERS } from '../constants/providers';

const AvatarSigninPage: React.FC = () => {
  const navigate = useNavigate();
  const { signin, isLoading } = useAvatar();
  const [formData, setFormData] = useState({
    username: '',
    password: '',
    provider: 'Auto', // Default to Auto
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const handleInputChange = (field: string) => (event: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({
      ...prev,
      [field]: event.target.value,
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
    
    if (!formData.username.trim()) {
      newErrors.username = 'Username is required';
    }
    
    if (!formData.password) {
      newErrors.password = 'Password is required';
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    // Additional validation before API call
    if (!formData.username.trim() || !formData.password) {
      toast.error('Please fill in all required fields!');
      return;
    }

    const success = await signin(formData.username, formData.password, formData.provider);
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
      <Container maxWidth="sm">
        <Paper elevation={3} sx={{ p: 4 }}>
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <MuiAvatar
              sx={{
                width: 80,
                height: 80,
                mx: 'auto',
                mb: 2,
                bgcolor: 'primary.main',
              }}
            >
              <AccountIcon sx={{ fontSize: 40 }} />
            </MuiAvatar>
            <Typography variant="h4" component="h1" gutterBottom>
              Sign In to OASIS
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Access STARNET with your OASIS Avatar
            </Typography>
          </Box>

          <Alert severity="info" sx={{ mb: 3 }}>
            Sign in with your OASIS Avatar to access STARNET and all its features.
          </Alert>

          <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            <TextField
              fullWidth
              label="Username or Email"
              value={formData.username}
              onChange={handleInputChange('username')}
              error={!!errors.username}
              helperText={errors.username}
              disabled={isLoading}
              autoComplete="username"
            />
            
            <TextField
              fullWidth
              label="Password"
              type="password"
              value={formData.password}
              onChange={handleInputChange('password')}
              error={!!errors.password}
              helperText={errors.password}
              disabled={isLoading}
              autoComplete="current-password"
            />

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

            <Button
              type="submit"
              variant="contained"
              size="large"
              fullWidth
              disabled={isLoading}
              startIcon={isLoading ? <CircularProgress size={20} /> : <LoginIcon />}
              sx={{ mt: 2 }}
            >
              {isLoading ? 'Signing In...' : 'Sign In'}
            </Button>
          </Box>

          <Divider sx={{ my: 3 }}>
            <Typography variant="body2" color="text.secondary">
              OR
            </Typography>
          </Divider>

          <Box sx={{ textAlign: 'center' }}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Don't have an OASIS Avatar yet?
            </Typography>
            <Button
              component={RouterLink}
              to="/avatar/signup"
              variant="outlined"
              startIcon={<SignupIcon />}
              disabled={isLoading}
            >
              Create Avatar
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

export default AvatarSigninPage;
