import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box,
  Card,
  CardContent,
  TextField,
  Button,
  Typography,
  Tabs,
  Tab,
  InputAdornment,
  IconButton,
  Divider,
} from '@mui/material';
import {
  Phone as PhoneIcon,
  Email as EmailIcon,
  Lock as LockIcon,
  Visibility,
  VisibilityOff,
  AccountCircle as OASISIcon,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { login, checkAuth } from '../../store/slices/authSlice';
import { oasisService } from '../../services/api/oasis';

const LoginScreen = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { isLoading, error } = useSelector((state) => state.auth);

  const [tabValue, setTabValue] = useState(1); // Start on Sign In tab
  const [showPassword, setShowPassword] = useState(false);
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = async (e) => {
    e?.preventDefault();
    if (tabValue === 1) {
      // Sign In
      if (!phone) return;
      // For now, just navigate - you can add actual login logic later
      navigate('/home');
    } else {
      // Sign Up
      if (!email || !password) return;
      // For now, just navigate
      navigate('/home');
    }
  };

  const handleLogin = async () => {
    if (!email || !password) return;
    const result = await dispatch(login({ email, password }));
    if (login.fulfilled.match(result)) {
      navigate('/home');
    }
  };

  const handleOASISLogin = async () => {
    try {
      // For now, use a demo OASIS login flow
      // In production, this would open OASIS OAuth or use OASIS Avatar credentials
      console.log('OASIS login clicked');
      
      // Option 1: OAuth-style redirect (if OASIS supports it)
      // window.location.href = `${OASIS_API_BASE_URL}/oauth/authorize?client_id=${CLIENT_ID}&redirect_uri=${REDIRECT_URI}`;
      
      // Option 2: Direct authentication (for demo)
      // For now, navigate to home with OASIS branding
      // In production, you'd authenticate with OASIS first
      const mockOASISUser = {
        id: 'oasis-avatar-id',
        fullName: 'OASIS Driver',
        email: 'driver@oasis.io',
        role: 'driver',
        avatarId: 'oasis-avatar-id',
        avatarType: 'Driver',
      };
      
      localStorage.setItem('timo_driver_user_data', JSON.stringify(mockOASISUser));
      localStorage.setItem('timo_driver_auth_token', 'oasis-jwt-token');
      localStorage.setItem('timo_driver_driver_id', 'oasis-avatar-id');
      localStorage.setItem('oasis_avatar_id', 'oasis-avatar-id');
      
      await dispatch(checkAuth());
      navigate('/home');
    } catch (error) {
      console.error('OASIS login error:', error);
    }
  };

  const handleSkipLogin = async () => {
    // Set a mock user in localStorage to bypass auth check
    const mockUser = {
      id: 'test-driver-id',
      fullName: 'Test Driver',
      email: 'test@driver.com',
      role: 'driver',
    };
    localStorage.setItem('timo_driver_user_data', JSON.stringify(mockUser));
    localStorage.setItem('timo_driver_auth_token', 'test-token');
    localStorage.setItem('timo_driver_driver_id', 'test-driver-id');
    
    // Dispatch checkAuth to update Redux state
    await dispatch(checkAuth());
    navigate('/home');
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        background: 'linear-gradient(180deg, #2847bc 0%, #1534aa 100%)',
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      {/* Header with Logo */}
      <Box
        sx={{
          pt: 8,
          pb: 4,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <motion.div
          initial={{ scale: 0.8, opacity: 0 }}
          animate={{ scale: 1, opacity: 1 }}
          transition={{ duration: 0.5 }}
        >
          <Box
            component="img"
            src="/timo-logo.png"
            alt="TimoRides Driver"
            onError={(e) => {
              e.target.style.display = 'none';
            }}
            sx={{
              width: 120,
              height: 120,
              mb: 2,
            }}
          />
        </motion.div>
        <Typography
          variant="h5"
          sx={{
            color: 'white',
            fontWeight: 600,
            textShadow: '0 2px 10px rgba(0, 0, 0, 0.3)',
          }}
        >
          Driver Portal
        </Typography>
      </Box>

      {/* Login Card */}
      <Box
        sx={{
          px: 3,
          pb: 4,
        }}
      >
        <Card
          sx={{
            borderRadius: 4,
            background: 'rgba(255, 255, 255, 0.95)',
            backdropFilter: 'blur(20px)',
            boxShadow: '0 8px 32px rgba(0, 0, 0, 0.2), 0 0 0 1px rgba(255, 255, 255, 0.3) inset',
            border: '1px solid rgba(255, 255, 255, 0.3)',
          }}
        >
          <CardContent sx={{ p: 3 }}>
            {/* Error Message */}
            {error && (
              <Box
                sx={{
                  mb: 2,
                  p: 2,
                  bgcolor: 'error.light',
                  borderRadius: 2,
                  color: 'error.main',
                }}
              >
                <Typography variant="body2">{error}</Typography>
              </Box>
            )}

            {/* Tabs */}
            <Tabs
              value={tabValue}
              onChange={(e, newValue) => setTabValue(newValue)}
              sx={{
                mb: 3,
                '& .MuiTab-root': {
                  textTransform: 'none',
                  fontSize: '18px',
                  fontWeight: 600,
                },
                '& .MuiTabs-indicator': {
                  backgroundColor: 'primary.main',
                  height: 3,
                },
              }}
            >
              <Tab label="Sign Up" />
              <Tab label="Sign In" />
            </Tabs>

            {/* Sign Up Form */}
            {tabValue === 0 && (
              <motion.div
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ duration: 0.3 }}
              >
                <form onSubmit={handleSubmit}>
                  <TextField
                    fullWidth
                    label="Email Address"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    sx={{
                      mb: 2,
                      '& .MuiOutlinedInput-root': {
                        background: 'rgba(255, 255, 255, 0.9)',
                        backdropFilter: 'blur(10px)',
                      },
                    }}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <EmailIcon
                            color="primary"
                            sx={{
                              filter: 'drop-shadow(0 0 6px rgba(40, 71, 188, 0.5))',
                            }}
                          />
                        </InputAdornment>
                      ),
                    }}
                  />
                  <TextField
                    fullWidth
                    label="Password"
                    type={showPassword ? 'text' : 'password'}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    sx={{
                      mb: 3,
                      '& .MuiOutlinedInput-root': {
                        background: 'rgba(255, 255, 255, 0.9)',
                        backdropFilter: 'blur(10px)',
                      },
                    }}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <LockIcon
                            color="primary"
                            sx={{
                              filter: 'drop-shadow(0 0 6px rgba(40, 71, 188, 0.5))',
                            }}
                          />
                        </InputAdornment>
                      ),
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            onClick={() => setShowPassword(!showPassword)}
                            edge="end"
                            type="button"
                          >
                            {showPassword ? <VisibilityOff /> : <Visibility />}
                          </IconButton>
                        </InputAdornment>
                      ),
                    }}
                  />
                  <Button
                    fullWidth
                    variant="contained"
                    size="large"
                    type="submit"
                    disabled={!email || !password || isLoading}
                    sx={{
                      mb: 2,
                      py: 1.5,
                      background: 'linear-gradient(135deg, #2847bc 0%, #3d5ed9 50%, #2847bc 100%)',
                      backgroundSize: '200% 200%',
                      boxShadow: '0 4px 20px rgba(40, 71, 188, 0.5), 0 0 30px rgba(40, 71, 188, 0.3)',
                      animation: 'gradientShift 3s ease infinite',
                      '&:hover': {
                        boxShadow: '0 6px 30px rgba(40, 71, 188, 0.7), 0 0 40px rgba(40, 71, 188, 0.5)',
                        transform: 'translateY(-2px)',
                      },
                      '&:disabled': {
                        background: 'rgba(0, 0, 0, 0.12)',
                        boxShadow: 'none',
                      },
                    }}
                  >
                    {isLoading ? 'Creating Account...' : 'Create Account'}
                  </Button>
                </form>
              </motion.div>
            )}

            {/* Sign In Form */}
            {tabValue === 1 && (
              <motion.div
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ duration: 0.3 }}
              >
                <form onSubmit={handleLogin}>
                  <TextField
                    fullWidth
                    label="Email"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    sx={{
                      mb: 2,
                      '& .MuiOutlinedInput-root': {
                        background: 'rgba(255, 255, 255, 0.9)',
                        backdropFilter: 'blur(10px)',
                      },
                    }}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <EmailIcon
                            color="primary"
                            sx={{
                              filter: 'drop-shadow(0 0 6px rgba(40, 71, 188, 0.5))',
                            }}
                          />
                        </InputAdornment>
                      ),
                    }}
                  />
                  <TextField
                    fullWidth
                    label="Password"
                    type={showPassword ? 'text' : 'password'}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    sx={{
                      mb: 3,
                      '& .MuiOutlinedInput-root': {
                        background: 'rgba(255, 255, 255, 0.9)',
                        backdropFilter: 'blur(10px)',
                      },
                    }}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <LockIcon
                            color="primary"
                            sx={{
                              filter: 'drop-shadow(0 0 6px rgba(40, 71, 188, 0.5))',
                            }}
                          />
                        </InputAdornment>
                      ),
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            onClick={() => setShowPassword(!showPassword)}
                            edge="end"
                            type="button"
                          >
                            {showPassword ? <VisibilityOff /> : <Visibility />}
                          </IconButton>
                        </InputAdornment>
                      ),
                    }}
                  />
                  <Button
                    fullWidth
                    variant="contained"
                    size="large"
                    type="submit"
                    disabled={!email || !password || isLoading}
                    onClick={handleLogin}
                    sx={{
                      mb: 2,
                      py: 1.5,
                      background: 'linear-gradient(135deg, #2847bc 0%, #3d5ed9 50%, #2847bc 100%)',
                      backgroundSize: '200% 200%',
                      boxShadow: '0 4px 20px rgba(40, 71, 188, 0.5), 0 0 30px rgba(40, 71, 188, 0.3)',
                      animation: 'gradientShift 3s ease infinite',
                      '&:hover': {
                        boxShadow: '0 6px 30px rgba(40, 71, 188, 0.7), 0 0 40px rgba(40, 71, 188, 0.5)',
                        transform: 'translateY(-2px)',
                      },
                      '&:disabled': {
                        background: 'rgba(0, 0, 0, 0.12)',
                        boxShadow: 'none',
                      },
                    }}
                  >
                    {isLoading ? 'Logging in...' : 'Continue'}
                  </Button>
                </form>
              </motion.div>
            )}

            {/* Divider */}
            <Box sx={{ display: 'flex', alignItems: 'center', my: 3 }}>
              <Divider sx={{ flex: 1 }} />
              <Typography variant="body2" sx={{ px: 2, color: 'text.secondary' }}>
                OR
              </Typography>
              <Divider sx={{ flex: 1 }} />
            </Box>

            {/* OASIS Login */}
            <Button
              fullWidth
              variant="outlined"
              size="large"
              startIcon={<OASISIcon />}
              onClick={handleOASISLogin}
              sx={{
                borderColor: '#2847bc',
                borderWidth: '2px',
                color: '#2847bc',
                background: 'linear-gradient(135deg, rgba(40, 71, 188, 0.05) 0%, rgba(61, 94, 217, 0.05) 100%)',
                transition: 'all 0.3s ease',
                '&:hover': {
                  borderColor: '#2847bc',
                  borderWidth: '2px',
                  backgroundColor: 'rgba(40, 71, 188, 0.1)',
                  boxShadow: '0 4px 20px rgba(40, 71, 188, 0.3), 0 0 20px rgba(40, 71, 188, 0.2)',
                  transform: 'translateY(-2px)',
                },
              }}
            >
              Continue with OASIS
            </Button>
            
            <Typography
              variant="caption"
              sx={{
                mt: 1,
                textAlign: 'center',
                color: 'text.secondary',
                fontSize: '11px',
              }}
            >
              Login with your OASIS Avatar identity
            </Typography>

            {/* Terms */}
            <Typography
              variant="body2"
              sx={{
                mt: 3,
                textAlign: 'center',
                color: 'text.secondary',
                fontSize: '12px',
              }}
            >
              By clicking start, you agree to our{' '}
              <Typography
                component="span"
                sx={{ color: 'primary.main', fontWeight: 600, cursor: 'pointer' }}
              >
                Terms and Conditions
              </Typography>
            </Typography>

            {/* Skip Login for Testing */}
            <Button
              fullWidth
              variant="text"
              size="small"
              onClick={handleSkipLogin}
              sx={{
                mt: 2,
                color: 'text.secondary',
                fontSize: '12px',
              }}
            >
              Skip Login (Testing)
            </Button>
          </CardContent>
        </Card>
      </Box>
    </Box>
  );
};

export default LoginScreen;
