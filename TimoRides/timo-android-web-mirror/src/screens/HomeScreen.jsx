import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  TextField,
  Typography,
  Card,
  CardContent,
  IconButton,
  Drawer,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Avatar,
  Chip,
  Badge,
} from '@mui/material'
import {
  Menu as MenuIcon,
  LocationOn as LocationOnIcon,
  MyLocation as MyLocationIcon,
  Home as HomeIcon,
  Wallet as WalletIcon,
  History as HistoryIcon,
  Notifications as NotificationsIcon,
  Settings as SettingsIcon,
  Logout as LogoutIcon,
  Star as StarIcon,
} from '@mui/icons-material'

const HomeScreen = () => {
  const navigate = useNavigate()
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [pickup, setPickup] = useState('Current Location')
  const [destination, setDestination] = useState('')

  const menuItems = [
    { icon: <HomeIcon />, text: 'Home', path: '/home' },
    { icon: <WalletIcon />, text: 'My Wallet', path: '/wallet' },
    { icon: <HistoryIcon />, text: 'History', path: '/history' },
    { icon: <NotificationsIcon />, text: 'Notifications', path: '/notifications', badge: 3 },
    { icon: <SettingsIcon />, text: 'Settings', path: '/settings' },
    { icon: <LogoutIcon />, text: 'Logout', path: '/login' },
  ]

  const drivers = [
    {
      id: 1,
      name: 'John M.',
      rating: 4.9,
      rides: 328,
      vehicle: 'Toyota Camry • Silver',
      features: ['WiFi', 'English'],
      price: 'R250',
      eta: '8 min',
      karma: 850,
    },
    {
      id: 2,
      name: 'Sarah K.',
      rating: 4.8,
      rides: 245,
      vehicle: 'Honda Accord • Black',
      features: ['WiFi', 'Zulu', 'Child Seat'],
      price: 'R280',
      eta: '12 min',
      karma: 720,
    },
    {
      id: 3,
      name: 'Mike T.',
      rating: 5.0,
      rides: 512,
      vehicle: 'BMW 3 Series • White',
      features: ['WiFi', 'English', 'Premium'],
      price: 'R350',
      eta: '5 min',
      karma: 1200,
    },
  ]

  const handleDriverSelect = (driverId) => {
    navigate(`/drivers?driver=${driverId}`)
  }

  const handleMenuClick = (path) => {
    setDrawerOpen(false)
    if (path === '/login') {
      // Handle logout
      navigate('/login')
    } else {
      navigate(path)
    }
  }

  return (
    <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
      {/* Map Area (Placeholder) */}
      <Box
        sx={{
          flex: 1,
          background: 'linear-gradient(135deg, #E3F2FD 0%, #BBDEFB 50%, #90CAF9 100%)',
          position: 'relative',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: 'radial-gradient(circle at 30% 30%, rgba(40, 71, 188, 0.1) 0%, transparent 50%)',
            animation: 'pulseGlow 4s ease-in-out infinite',
          },
        }}
      >
        {/* Map Placeholder */}
        <Typography 
          variant="h6" 
          sx={{ 
            color: 'text.secondary',
            zIndex: 1,
            textShadow: '0 2px 10px rgba(0, 0, 0, 0.1)',
          }}
        >
          Google Maps Integration
        </Typography>

        {/* Menu Button */}
        <IconButton
          onClick={() => setDrawerOpen(true)}
          sx={{
            position: 'absolute',
            top: 16,
            left: 16,
            backgroundColor: 'rgba(255, 255, 255, 0.95)',
            backdropFilter: 'blur(10px)',
            boxShadow: '0 4px 20px rgba(0, 0, 0, 0.15), 0 0 0 1px rgba(255, 255, 255, 0.2)',
            zIndex: 10,
            transition: 'all 0.3s ease',
            '&:hover': {
              backgroundColor: 'rgba(255, 255, 255, 1)',
              boxShadow: '0 6px 25px rgba(40, 71, 188, 0.3), 0 0 0 1px rgba(40, 71, 188, 0.2)',
              transform: 'scale(1.05)',
            },
          }}
        >
          <MenuIcon />
        </IconButton>

        {/* My Location Button */}
        <IconButton
          sx={{
            position: 'absolute',
            bottom: 200,
            right: 16,
            backgroundColor: 'rgba(255, 255, 255, 0.95)',
            backdropFilter: 'blur(10px)',
            boxShadow: '0 4px 20px rgba(40, 71, 188, 0.3), 0 0 20px rgba(40, 71, 188, 0.2)',
            zIndex: 10,
            transition: 'all 0.3s ease',
            '&:hover': {
              backgroundColor: 'rgba(255, 255, 255, 1)',
              boxShadow: '0 6px 30px rgba(40, 71, 188, 0.5), 0 0 30px rgba(40, 71, 188, 0.3)',
              transform: 'scale(1.1)',
            },
          }}
        >
          <MyLocationIcon 
            color="primary" 
            sx={{
              filter: 'drop-shadow(0 0 8px rgba(40, 71, 188, 0.6))',
            }}
          />
        </IconButton>
      </Box>

      {/* Bottom Sheet */}
      <Card
        sx={{
          position: 'absolute',
          bottom: 0,
          left: 0,
          right: 0,
          borderTopLeftRadius: 24,
          borderTopRightRadius: 24,
          background: 'rgba(255, 255, 255, 0.98)',
          backdropFilter: 'blur(20px)',
          boxShadow: '0 -8px 32px rgba(0, 0, 0, 0.15), 0 0 0 1px rgba(255, 255, 255, 0.2) inset',
          maxHeight: '60vh',
          overflow: 'auto',
          border: '1px solid rgba(255, 255, 255, 0.3)',
        }}
      >
        <CardContent sx={{ p: 3 }}>
          {/* Handle */}
          <Box
            sx={{
              width: 40,
              height: 4,
              background: 'linear-gradient(90deg, #2847bc, #3d5ed9, #2847bc)',
              borderRadius: 2,
              mx: 'auto',
              mb: 2,
              boxShadow: '0 2px 8px rgba(40, 71, 188, 0.3)',
            }}
          />

          {/* Pickup Location */}
          <TextField
            fullWidth
            value={pickup}
            onChange={(e) => setPickup(e.target.value)}
            placeholder="Pickup Location"
            sx={{ 
              mb: 2,
              '& .MuiOutlinedInput-root': {
                background: 'rgba(255, 255, 255, 0.8)',
                backdropFilter: 'blur(10px)',
              },
            }}
            InputProps={{
              startAdornment: (
                <LocationOnIcon 
                  sx={{ 
                    color: 'primary.main', 
                    mr: 1,
                    filter: 'drop-shadow(0 0 6px rgba(40, 71, 188, 0.5))',
                  }} 
                />
              ),
            }}
          />

          {/* Destination */}
          <TextField
            fullWidth
            value={destination}
            onChange={(e) => setDestination(e.target.value)}
            placeholder="Where to?"
            sx={{ 
              mb: 3,
              '& .MuiOutlinedInput-root': {
                background: 'rgba(255, 255, 255, 0.8)',
                backdropFilter: 'blur(10px)',
              },
            }}
            InputProps={{
              startAdornment: (
                <LocationOnIcon 
                  sx={{ 
                    color: 'secondary.main', 
                    mr: 1,
                    filter: 'drop-shadow(0 0 6px rgba(254, 217, 2, 0.5))',
                  }} 
                />
              ),
            }}
          />

          {/* Available Drivers Section */}
          <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
            Available Drivers
          </Typography>

          {/* Driver Cards */}
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {drivers.map((driver) => (
              <Card
                key={driver.id}
                onClick={() => handleDriverSelect(driver.id)}
                sx={{
                  p: 2,
                  cursor: 'pointer',
                  background: 'rgba(255, 255, 255, 0.95)',
                  backdropFilter: 'blur(10px)',
                  border: '1px solid rgba(40, 71, 188, 0.1)',
                  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
                  position: 'relative',
                  overflow: 'hidden',
                  '&::before': {
                    content: '""',
                    position: 'absolute',
                    top: 0,
                    left: '-100%',
                    width: '100%',
                    height: '100%',
                    background: 'linear-gradient(90deg, transparent, rgba(40, 71, 188, 0.1), transparent)',
                    transition: 'left 0.5s',
                  },
                  '&:hover': {
                    boxShadow: '0 8px 30px rgba(40, 71, 188, 0.25), 0 0 0 1px rgba(40, 71, 188, 0.2)',
                    transform: 'translateY(-4px) scale(1.02)',
                    borderColor: 'rgba(40, 71, 188, 0.3)',
                    '&::before': {
                      left: '100%',
                    },
                  },
                }}
              >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <Avatar sx={{ width: 56, height: 56 }} />
                  <Box sx={{ flex: 1 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                      <Typography variant="subtitle1" fontWeight={600}>
                        {driver.name}
                      </Typography>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, ml: 1 }}>
                        <StarIcon sx={{ fontSize: 16, color: 'secondary.main' }} />
                        <Typography variant="body2" fontWeight={600}>
                          {driver.rating}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          ({driver.rides})
                        </Typography>
                      </Box>
                    </Box>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                      {driver.vehicle}
                    </Typography>
                    <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mt: 1 }}>
                      {driver.features.map((feature) => (
                        <Chip key={feature} label={feature} size="small" />
                      ))}
                    </Box>
                  </Box>
                  <Box sx={{ textAlign: 'right' }}>
                    <Typography 
                      variant="h6" 
                      fontWeight={700} 
                      color="primary"
                      sx={{
                        textShadow: '0 0 10px rgba(40, 71, 188, 0.4)',
                      }}
                    >
                      {driver.price}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {driver.eta} away
                    </Typography>
                  </Box>
                </Box>
              </Card>
            ))}
          </Box>
        </CardContent>
      </Card>

      {/* Navigation Drawer */}
      <Drawer
        anchor="left"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
      >
        <Box sx={{ width: 280, pt: 2 }}>
          {/* Profile Section */}
          <Box
            sx={{
              background: 'linear-gradient(135deg, #2847bc 0%, #3d5ed9 50%, #1534aa 100%)',
              backgroundSize: '200% 200%',
              p: 3,
              color: 'white',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              boxShadow: '0 4px 20px rgba(40, 71, 188, 0.3)',
              animation: 'gradientShift 3s ease infinite',
              position: 'relative',
              overflow: 'hidden',
              '&::before': {
                content: '""',
                position: 'absolute',
                top: '-50%',
                right: '-50%',
                width: '200%',
                height: '200%',
                background: 'radial-gradient(circle, rgba(254, 217, 2, 0.1) 0%, transparent 70%)',
                animation: 'pulseGlow 4s ease-in-out infinite',
              },
            }}
          >
            <Avatar 
              sx={{ 
                width: 80, 
                height: 80, 
                mb: 2,
                border: '3px solid rgba(254, 217, 2, 0.5)',
                boxShadow: '0 0 20px rgba(254, 217, 2, 0.4)',
                zIndex: 1,
              }} 
            />
            <Typography 
              variant="h6" 
              fontWeight={600}
              sx={{
                textShadow: '0 0 10px rgba(254, 217, 2, 0.5)',
                zIndex: 1,
              }}
            >
              Alvin Armstrong
            </Typography>
          </Box>

          {/* Menu Items */}
          <List>
            {menuItems.map((item) => (
              <ListItem
                key={item.text}
                button
                onClick={() => handleMenuClick(item.path)}
              >
                <ListItemIcon>
                  {item.badge ? (
                    <Badge badgeContent={item.badge} color="error">
                      {item.icon}
                    </Badge>
                  ) : (
                    item.icon
                  )}
                </ListItemIcon>
                <ListItemText primary={item.text} />
              </ListItem>
            ))}
          </List>
        </Box>
      </Drawer>
    </Box>
  )
}

export default HomeScreen

