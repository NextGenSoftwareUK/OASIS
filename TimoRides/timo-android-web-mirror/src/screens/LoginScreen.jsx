import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  TextField,
  Typography,
  Tabs,
  Tab,
  InputAdornment,
  IconButton,
  Divider,
  Card,
  CardContent,
} from '@mui/material'
import {
  Phone as PhoneIcon,
  Email as EmailIcon,
  Lock as LockIcon,
  Visibility,
  VisibilityOff,
  Facebook as FacebookIcon,
} from '@mui/icons-material'
import { motion } from 'framer-motion'

const LoginScreen = () => {
  const navigate = useNavigate()
  const [tabValue, setTabValue] = useState(0)
  const [showPassword, setShowPassword] = useState(false)
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  const handleSubmit = (e) => {
    e?.preventDefault()
    // Navigate to home after login
    console.log('Navigating to home...')
    navigate('/home')
  }

  const handleFacebookLogin = () => {
    console.log('Facebook login clicked')
    navigate('/home')
  }

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
            alt="TimoRides"
            sx={{
              width: 120,
              height: 120,
              mb: 2,
            }}
          />
        </motion.div>
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
                    disabled={!email || !password}
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
                    Create Account
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
                <form onSubmit={handleSubmit}>
                  <TextField
                    fullWidth
                    label="Phone Number"
                    type="tel"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
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
                          <PhoneIcon 
                            color="primary"
                            sx={{
                              filter: 'drop-shadow(0 0 6px rgba(40, 71, 188, 0.5))',
                            }}
                          />
                        </InputAdornment>
                      ),
                    }}
                  />
                  <Button
                    fullWidth
                    variant="contained"
                    size="large"
                    type="submit"
                    disabled={!phone}
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
                    Continue
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

            {/* Facebook Login */}
            <Button
              fullWidth
              variant="outlined"
              size="large"
              startIcon={<FacebookIcon />}
              onClick={handleFacebookLogin}
              sx={{
                borderColor: '#1877F2',
                borderWidth: '2px',
                color: '#1877F2',
                transition: 'all 0.3s ease',
                '&:hover': {
                  borderColor: '#1877F2',
                  borderWidth: '2px',
                  backgroundColor: 'rgba(24, 119, 242, 0.1)',
                  boxShadow: '0 4px 20px rgba(24, 119, 242, 0.3), 0 0 20px rgba(24, 119, 242, 0.2)',
                  transform: 'translateY(-2px)',
                },
              }}
            >
              Continue with Facebook
            </Button>

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
              onClick={() => navigate('/home')}
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
  )
}

export default LoginScreen

