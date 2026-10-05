import React, { useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Typography,
  Avatar as MuiAvatar,
  IconButton,
  Alert,
  CircularProgress,
  Divider,
  Card,
  CardContent,
  FormControlLabel,
  Checkbox,
} from '@mui/material';
import {
  Login as LoginIcon,
  PersonAdd as SignupIcon,
  Close as CloseIcon,
  AccountCircle as AccountIcon,
} from '@mui/icons-material';
import { toast } from 'react-hot-toast';
import { useAvatar } from '../contexts/AvatarContext';

interface AvatarAuthProps {
  onLogin?: (avatar: any) => void;
  onSignup?: (avatar: any) => void;
  onLogout?: () => void;
  isLoggedIn?: boolean;
  currentAvatar?: any;
  variant?: 'button' | 'popup' | 'embedded';
  size?: 'small' | 'medium' | 'large';
  showSignup?: boolean;
  showSignin?: boolean;
}

export const AvatarAuth: React.FC<AvatarAuthProps> = ({
  onLogin,
  onSignup,
  onLogout,
  isLoggedIn = false,
  currentAvatar,
  variant = 'button',
  size = 'medium',
  showSignup = true,
  showSignin = true,
}) => {
  const { signin, signup, signout, currentAvatar: contextAvatar, isLoggedIn: contextIsLoggedIn } = useAvatar();
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<'signin' | 'signup'>('signin');
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    email: '',
    username: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    avatarType: 'User',
    acceptTerms: false,
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const handleOpen = (authMode: 'signin' | 'signup') => {
    setMode(authMode);
    setOpen(true);
  };

  const handleClose = () => {
    setOpen(false);
    setFormData({
      email: '',
      username: '',
      password: '',
      confirmPassword: '',
      firstName: '',
      lastName: '',
      avatarType: 'User',
      acceptTerms: false,
    });
    setErrors({});
  };

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
    
    if (mode === 'signin') {
      if (!formData.username.trim()) {
        newErrors.username = 'Username is required';
      }
      if (!formData.password) {
        newErrors.password = 'Password is required';
      }
    } else {
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
      if (!formData.avatarType) {
        newErrors.avatarType = 'Avatar type is required';
      }
      if (!formData.acceptTerms) {
        newErrors.acceptTerms = 'You must accept the terms and conditions';
      }
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSignin = async () => {
    if (!validateForm()) {
      return;
    }

    setLoading(true);
    try {
      const success = await signin(formData.username, formData.password);
      if (success) {
        onLogin?.(contextAvatar);
        toast.success('Welcome back to The OASIS! 🌟');
        handleClose();
      } else {
        toast.error('Avatar not found. Please check your credentials.');
      }
    } catch (error) {
      toast.error('Sign in failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleSignup = async () => {
    if (!validateForm()) {
      return;
    }
    
    setLoading(true);
    try {
      const success = await signup({
        firstName: formData.firstName,
        lastName: formData.lastName,
        avatarType: formData.avatarType,
        email: formData.email,
        username: formData.username,
        password: formData.password,
        confirmPassword: formData.confirmPassword,
        acceptTerms: formData.acceptTerms,
      });
      
      if (success) {
        onSignup?.(contextAvatar);
        toast.success('Welcome to The OASIS! 🚀');
        handleClose();
      } else {
        toast.error('Failed to create avatar. Please try again.');
      }
    } catch (error) {
      toast.error('Sign up failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = async () => {
    try {
      await signout();
      onLogout?.();
      toast.success('Signed out successfully! 👋');
    } catch (error) {
      toast.error('Sign out failed. Please try again.');
    }
  };

  const renderAuthButton = () => {
    const avatar = currentAvatar || contextAvatar;
    const loggedIn = isLoggedIn || contextIsLoggedIn;
    
    if (loggedIn && avatar) {
      return (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <MuiAvatar 
            src={avatar.avatar} 
            alt={avatar.username}
            sx={{ width: 32, height: 32 }}
          />
          <Typography variant="body2" color="text.secondary">
            {avatar.username}
          </Typography>
          <Button
            variant="outlined"
            size="small"
            onClick={handleLogout}
            startIcon={<CloseIcon />}
          >
            Sign Out
          </Button>
        </Box>
      );
    }

    return (
      <Box sx={{ display: 'flex', gap: 1 }}>
        {showSignin && (
          <Button
            variant="contained"
            startIcon={<LoginIcon />}
            onClick={() => handleOpen('signin')}
            size={size}
          >
            Sign In
          </Button>
        )}
        {showSignup && (
          <Button
            variant="outlined"
            startIcon={<SignupIcon />}
            onClick={() => handleOpen('signup')}
            size={size}
          >
            Sign Up
          </Button>
        )}
      </Box>
    );
  };

  const renderAuthDialog = () => (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <AccountIcon color="primary" />
        <Typography variant="h6">
          {mode === 'signin' ? 'Sign In to OASIS' : 'Join OASIS'}
        </Typography>
        <IconButton
          onClick={handleClose}
          sx={{ ml: 'auto' }}
        >
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      
      <DialogContent>
        <Box sx={{ mt: 2 }}>
          <Alert severity="info" sx={{ mb: 2 }}>
            {mode === 'signin' 
              ? 'Sign in with your OASIS Avatar to access STARNET'
              : 'Create your OASIS Avatar to join STARNET'
            }
          </Alert>

          {mode === 'signin' ? (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <TextField
                fullWidth
                label="Username or Email"
                value={formData.username}
                onChange={handleInputChange('username')}
                error={!!errors.username}
                helperText={errors.username}
                disabled={loading}
              />
              <TextField
                fullWidth
                label="Password"
                type="password"
                value={formData.password}
                onChange={handleInputChange('password')}
                error={!!errors.password}
                helperText={errors.password}
                disabled={loading}
              />
            </Box>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box sx={{ display: 'flex', gap: 1 }}>
                <TextField
                  fullWidth
                  label="First Name"
                  value={formData.firstName}
                  onChange={handleInputChange('firstName')}
                  error={!!errors.firstName}
                  helperText={errors.firstName}
                  disabled={loading}
                />
                <TextField
                  fullWidth
                  label="Last Name"
                  value={formData.lastName}
                  onChange={handleInputChange('lastName')}
                  error={!!errors.lastName}
                  helperText={errors.lastName}
                  disabled={loading}
                />
              </Box>
              <TextField
                fullWidth
                label="Username"
                value={formData.username}
                onChange={handleInputChange('username')}
                error={!!errors.username}
                helperText={errors.username}
                disabled={loading}
              />
              <TextField
                fullWidth
                label="Email"
                type="email"
                value={formData.email}
                onChange={handleInputChange('email')}
                error={!!errors.email}
                helperText={errors.email}
                disabled={loading}
              />
              <TextField
                fullWidth
                label="Password"
                type="password"
                value={formData.password}
                onChange={handleInputChange('password')}
                error={!!errors.password}
                helperText={errors.password}
                disabled={loading}
              />
              <TextField
                fullWidth
                label="Confirm Password"
                type="password"
                value={formData.confirmPassword}
                onChange={handleInputChange('confirmPassword')}
                error={!!errors.confirmPassword}
                helperText={errors.confirmPassword}
                disabled={loading}
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={formData.acceptTerms}
                    onChange={(e) => setFormData(prev => ({ ...prev, acceptTerms: e.target.checked }))}
                    disabled={loading}
                  />
                }
                label="I accept the terms and conditions"
              />
              {errors.acceptTerms && (
                <Typography variant="caption" color="error" sx={{ ml: 4 }}>
                  {errors.acceptTerms}
                </Typography>
              )}
            </Box>
          )}
        </Box>
      </DialogContent>

      <DialogActions sx={{ p: 2 }}>
        <Button onClick={handleClose} disabled={loading}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={mode === 'signin' ? handleSignin : handleSignup}
          disabled={loading}
          startIcon={loading ? <CircularProgress size={16} /> : null}
        >
          {loading 
            ? 'Processing...' 
            : mode === 'signin' ? 'Sign In' : 'Create Avatar'
          }
        </Button>
      </DialogActions>
    </Dialog>
  );

  if (variant === 'embedded') {
    return (
      <Card sx={{ maxWidth: 400, mx: 'auto' }}>
        <CardContent>
          <Typography variant="h6" gutterBottom align="center">
            OASIS Avatar Authentication
          </Typography>
          <Divider sx={{ my: 2 }} />
          {renderAuthButton()}
        </CardContent>
      </Card>
    );
  }

  return (
    <>
      {renderAuthButton()}
      {renderAuthDialog()}
    </>
  );
};

export default AvatarAuth;

