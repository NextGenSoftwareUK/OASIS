import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box,
  Card,
  CardContent,
  Button,
  Typography,
  Avatar,
  Chip,
  IconButton,
  Drawer,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Badge,
} from '@mui/material';
import {
  Menu as MenuIcon,
  LocationOn,
  DirectionsCar,
  MyLocation as MyLocationIcon,
  Home as HomeIcon,
  AccountBalanceWallet as WalletIcon,
  History as HistoryIcon,
  Settings as SettingsIcon,
  Logout as LogoutIcon,
  Notifications as NotificationsIcon,
  Help as HelpIcon,
  Add as AddIcon,
  NotificationsActive as NotificationsActiveIcon,
} from '@mui/icons-material';
import { getDriverStatus, updateDriverStatus, setTestStatus } from '../../store/slices/driverSlice';
import { fetchBookings, addSimulatedBooking } from '../../store/slices/bookingSlice';
import { logout } from '../../store/slices/authSlice';
import { TimoColors } from '../../utils/theme';
import MapView from '../../components/MapView';

const HomeScreen = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { driverId, user } = useSelector((state) => state.auth);
  const { isOnline, isOffline, car } = useSelector((state) => state.driver);
  const { pendingBookings, activeBooking } = useSelector((state) => state.bookings);
  const [drawerOpen, setDrawerOpen] = useState(false);

  // Get driverId from state or localStorage
  const currentDriverId = driverId || localStorage.getItem('timo_driver_driver_id');
  const currentUser = user || (() => {
    try {
      const userData = localStorage.getItem('timo_driver_user_data');
      return userData ? JSON.parse(userData) : null;
    } catch {
      return null;
    }
  })();

  useEffect(() => {
    // Only fetch if we have a real driverId (not test)
    if (currentDriverId && currentDriverId !== 'test-driver-id') {
      dispatch(getDriverStatus(currentDriverId));
      dispatch(fetchBookings());
    }
  }, [currentDriverId, dispatch]);

  // Poll for bookings when online
  useEffect(() => {
    if (!isOnline || !currentDriverId || currentDriverId === 'test-driver-id') return;

    const interval = setInterval(() => {
      dispatch(fetchBookings());
    }, 10000); // Every 10 seconds

    return () => clearInterval(interval);
  }, [isOnline, currentDriverId, dispatch]);

  const handleToggleAvailability = async () => {
    const newStatus = {
      isOffline: !isOffline,
      isActive: !isOffline,
    };
    
    if (currentDriverId && currentDriverId !== 'test-driver-id') {
      // Real API call
      await dispatch(updateDriverStatus({
        driverId: currentDriverId,
        statusData: newStatus,
      }));
    } else {
      // For test mode, update local state directly
      dispatch(setTestStatus(newStatus));
    }
  };

  const handleRideRequest = (booking) => {
    navigate(`/ride-request/${booking.id}`, { state: { booking } });
  };

  const menuItems = [
    { icon: <HomeIcon />, text: 'Home', path: '/home' },
    { icon: <WalletIcon />, text: 'Earnings', path: '/earnings' },
    { icon: <HistoryIcon />, text: 'Ride History', path: '/history' },
    { icon: <NotificationsIcon />, text: 'Notifications', path: '/notifications', badge: 0 },
    { icon: <SettingsIcon />, text: 'Settings', path: '/settings' },
    { icon: <HelpIcon />, text: 'Help & Support', path: '/help' },
  ];

  const handleMenuClick = (path) => {
    setDrawerOpen(false);
    if (path === '/login') {
      dispatch(logout());
      navigate('/login');
    } else if (path) {
      navigate(path);
    }
  };

  const handleSimulateRideRequest = () => {
    // Durban area locations with proper coordinates
    const locations = [
      { 
        pickup: 'Durban Central', 
        pickupLat: -29.8587, 
        pickupLng: 31.0218,
        destination: 'Umhlanga Beach', 
        destLat: -29.7284, 
        destLng: 31.0819,
        amount: 250 
      },
      { 
        pickup: 'Umhlanga', 
        pickupLat: -29.7284, 
        pickupLng: 31.0819,
        destination: 'Durban Airport', 
        destLat: -29.6144, 
        destLng: 31.1197,
        amount: 320 
      },
      { 
        pickup: 'Gateway Mall', 
        pickupLat: -29.7284, 
        pickupLng: 31.0819,
        destination: 'Durban Central', 
        destLat: -29.8587, 
        destLng: 31.0218,
        amount: 180 
      },
      { 
        pickup: 'Berea', 
        pickupLat: -29.8500, 
        pickupLng: 30.9900,
        destination: 'Glenwood', 
        destLat: -29.8700, 
        destLng: 31.0000,
        amount: 150 
      },
      { 
        pickup: 'Morningside', 
        pickupLat: -29.8300, 
        pickupLng: 31.0100,
        destination: 'Westville', 
        destLat: -29.8200, 
        destLng: 30.9300,
        amount: 200 
      },
    ];
    
    const randomLocation = locations[Math.floor(Math.random() * locations.length)];
    const riders = ['Alvin Armstrong', 'Sarah Johnson', 'Mike Thompson', 'Emma Davis', 'John Smith'];
    const randomRider = riders[Math.floor(Math.random() * riders.length)];
    
    // Calculate distance and duration based on coordinates
    const latDiff = Math.abs(randomLocation.destLat - randomLocation.pickupLat);
    const lngDiff = Math.abs(randomLocation.destLng - randomLocation.pickupLng);
    const distanceKm = Math.sqrt(latDiff * latDiff + lngDiff * lngDiff) * 111; // Rough conversion
    const durationMin = Math.round(distanceKm * 2); // Rough estimate: 2 min per km
    
    // Generate booking ID first
    const bookingId = `sim-${Date.now()}`;
    
    const mockBooking = {
      id: bookingId,
      status: 'pending',
      fullName: randomRider,
      phoneNumber: `+27 82 ${Math.floor(Math.random() * 9000) + 1000} ${Math.floor(Math.random() * 9000) + 1000}`,
      sourceLocation: {
        address: randomLocation.pickup,
        latitude: randomLocation.pickupLat,
        longitude: randomLocation.pickupLng,
      },
      destinationLocation: {
        address: randomLocation.destination,
        latitude: randomLocation.destLat,
        longitude: randomLocation.destLng,
      },
      tripAmount: randomLocation.amount,
      tripDistance: `${distanceKm.toFixed(1)} km`,
      tripDuration: `${durationMin} min`,
      passengers: Math.floor(Math.random() * 3) + 1,
      isCash: Math.random() > 0.5,
      currency: {
        symbol: 'R',
        code: 'ZAR',
      },
      createdAt: new Date().toISOString(),
    };

    dispatch(addSimulatedBooking(mockBooking));
    
    // Navigate to the ride request screen after a short delay
    setTimeout(() => {
      navigate(`/ride-request/${bookingId}`, { state: { booking: mockBooking } });
    }, 300);
  };

  return (
    <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
      {/* Map Area */}
      <Box
        sx={{
          flex: 1,
          position: 'relative',
          overflow: 'hidden',
        }}
      >
        {/* Actual Map View */}
        {activeBooking ? (
          <MapView
            pickupLocation={activeBooking.sourceLocation}
            destinationLocation={activeBooking.destinationLocation}
            height="100%"
            showRoute={true}
          />
        ) : (
          <MapView
            pickupLocation={null}
            destinationLocation={null}
            height="100%"
            showRoute={false}
          />
        )}

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
            sx={{
              color: TimoColors.primary,
              filter: 'drop-shadow(0 0 8px rgba(40, 71, 188, 0.6))',
            }}
          />
        </IconButton>

        {/* Simulate Ride Request Button (Dev/Test Mode) */}
        <Button
          variant="contained"
          startIcon={<NotificationsActiveIcon />}
          onClick={handleSimulateRideRequest}
          sx={{
            position: 'absolute',
            bottom: 280,
            right: 16,
            backgroundColor: TimoColors.accent,
            color: 'black',
            boxShadow: '0 4px 20px rgba(254, 217, 2, 0.5), 0 0 20px rgba(254, 217, 2, 0.3)',
            zIndex: 10,
            transition: 'all 0.3s ease',
            '&:hover': {
              backgroundColor: TimoColors.accentDark,
              boxShadow: '0 6px 30px rgba(254, 217, 2, 0.7), 0 0 30px rgba(254, 217, 2, 0.5)',
              transform: 'translateY(-2px)',
            },
          }}
        >
          Simulate Ride
        </Button>
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

          {/* Status Header */}
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
            <Box>
              <Typography variant="h6" fontWeight={600}>
                {currentUser?.fullName || user?.fullName || 'Driver'}
              </Typography>
              <Chip
                label={isOnline ? '🟢 Online' : '⚫ Offline'}
                color={isOnline ? 'success' : 'default'}
                size="small"
                sx={{ mt: 0.5 }}
              />
            </Box>
            <Avatar
              sx={{
                bgcolor: TimoColors.primary,
                width: 56,
                height: 56,
                cursor: 'pointer',
                boxShadow: '0 4px 15px rgba(40, 71, 188, 0.3)',
              }}
              onClick={() => navigate('/profile')}
            >
              {(currentUser?.fullName || user?.fullName)?.charAt(0) || 'D'}
            </Avatar>
          </Box>

          {/* Active Ride Banner */}
          {activeBooking && (
            <Card
              sx={{
                mb: 3,
                bgcolor: TimoColors.accent,
                cursor: 'pointer',
                transition: 'all 0.3s ease',
                '&:hover': { 
                  transform: 'translateY(-2px)',
                  boxShadow: '0 8px 25px rgba(254, 217, 2, 0.4)',
                },
              }}
              onClick={() => navigate(`/active-ride/${activeBooking.id}`, { state: { booking: activeBooking } })}
            >
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <DirectionsCar sx={{ fontSize: 40, color: 'white' }} />
                  <Box sx={{ flex: 1 }}>
                    <Typography variant="h6" fontWeight="bold" color="white">
                      Active Ride
                    </Typography>
                    <Typography variant="body2" color="white">
                      {activeBooking.destinationLocation?.address || 'Destination'}
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          )}

          {/* Pending Ride Requests */}
          {pendingBookings.length > 0 ? (
            <>
              <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
                New Ride Requests ({pendingBookings.length})
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {pendingBookings.map((booking) => (
                  <Card
                    key={booking.id}
                    onClick={() => handleRideRequest(booking)}
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
                    <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
                      <Box sx={{ flex: 1 }}>
                        <Typography variant="subtitle1" fontWeight={600} color="primary" sx={{ mb: 0.5 }}>
                          New Ride Request
                        </Typography>
                        <Typography variant="body2" sx={{ mb: 0.5 }}>
                          📍 {booking.sourceLocation?.address || 'Pickup location'}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          → {booking.destinationLocation?.address || 'Destination'}
                        </Typography>
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
                          {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
                        </Typography>
                      </Box>
                    </Box>
                  </Card>
                ))}
              </Box>
            </>
          ) : (
            <Box sx={{ textAlign: 'center', py: 4 }}>
              <Typography variant="body1" color="text.secondary">
                {isOnline ? 'Waiting for ride requests...' : 'Go online to receive ride requests'}
              </Typography>
            </Box>
          )}

          {/* Availability Toggle */}
          <Button
            fullWidth
            variant="contained"
            size="large"
            onClick={handleToggleAvailability}
            sx={{
              mt: 3,
              py: 1.5,
              bgcolor: isOnline ? TimoColors.online : TimoColors.offline,
              background: isOnline 
                ? 'linear-gradient(135deg, #4ACC12 0%, #3db00f 100%)'
                : 'linear-gradient(135deg, #9E9E9E 0%, #757575 100%)',
              boxShadow: isOnline
                ? '0 4px 20px rgba(74, 204, 18, 0.4), 0 0 20px rgba(74, 204, 18, 0.2)'
                : '0 4px 20px rgba(158, 158, 158, 0.3)',
              '&:hover': {
                bgcolor: isOnline ? TimoColors.online : TimoColors.offline,
                boxShadow: isOnline
                  ? '0 6px 30px rgba(74, 204, 18, 0.6), 0 0 30px rgba(74, 204, 18, 0.4)'
                  : '0 6px 30px rgba(158, 158, 158, 0.5)',
                transform: 'translateY(-2px)',
              },
            }}
          >
            {isOnline ? 'GO OFFLINE' : 'GO ONLINE'}
          </Button>
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
                bgcolor: TimoColors.primary,
              }}
            >
              {(currentUser?.fullName || user?.fullName)?.charAt(0) || 'D'}
            </Avatar>
            <Typography 
              variant="h6" 
              fontWeight={600}
              sx={{
                textShadow: '0 0 10px rgba(254, 217, 2, 0.5)',
                zIndex: 1,
              }}
            >
              {currentUser?.fullName || user?.fullName || 'Driver'}
            </Typography>
            <Chip
              label={isOnline ? '🟢 Online' : '⚫ Offline'}
              sx={{
                mt: 1,
                bgcolor: isOnline ? 'rgba(74, 204, 18, 0.3)' : 'rgba(158, 158, 158, 0.3)',
                color: 'white',
                border: '1px solid rgba(255, 255, 255, 0.3)',
                zIndex: 1,
              }}
              size="small"
            />
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
                  {item.badge !== undefined && item.badge > 0 ? (
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
            <ListItem
              button
              onClick={() => handleMenuClick('/login')}
            >
              <ListItemIcon>
                <LogoutIcon />
              </ListItemIcon>
              <ListItemText primary="Logout" />
            </ListItem>
          </List>
        </Box>
      </Drawer>
    </Box>
  );
};

export default HomeScreen;

