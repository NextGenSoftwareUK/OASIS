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
  Fab,
} from '@mui/material';
import {
  Menu as MenuIcon,
  LocationOn,
  DirectionsCar,
} from '@mui/icons-material';
import { getDriverStatus, updateDriverStatus } from '../../store/slices/driverSlice';
import { fetchBookings } from '../../store/slices/bookingSlice';
import { TimoColors } from '../../utils/theme';

const HomeScreen = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { driverId, user } = useSelector((state) => state.auth);
  const { isOnline, isOffline, car } = useSelector((state) => state.driver);
  const { pendingBookings, activeBooking } = useSelector((state) => state.bookings);

  useEffect(() => {
    if (driverId) {
      dispatch(getDriverStatus(driverId));
      dispatch(fetchBookings());
    }
  }, [driverId, dispatch]);

  // Poll for bookings when online
  useEffect(() => {
    if (!isOnline || !driverId) return;

    const interval = setInterval(() => {
      dispatch(fetchBookings());
    }, 10000); // Every 10 seconds

    return () => clearInterval(interval);
  }, [isOnline, driverId, dispatch]);

  const handleToggleAvailability = async () => {
    if (driverId) {
      await dispatch(updateDriverStatus({
        driverId,
        statusData: {
          isOffline: !isOffline,
          isActive: !isOffline,
        },
      }));
    }
  };

  const handleRideRequest = (booking) => {
    navigate(`/ride-request/${booking.id}`, { state: { booking } });
  };

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: TimoColors.backgroundLight }}>
      {/* Map Area (Placeholder) */}
      <Box
        sx={{
          height: '60vh',
          bgcolor: TimoColors.primaryLight,
          background: `linear-gradient(135deg, ${TimoColors.primary} 0%, ${TimoColors.primaryLight} 100%)`,
          position: 'relative',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: 'white',
        }}
      >
        <Box textAlign="center">
          <LocationOn sx={{ fontSize: 60, mb: 2 }} />
          <Typography variant="h6">Map View</Typography>
          <Typography variant="body2" sx={{ mt: 1 }}>
            {isOnline ? '🟢 Online - Ready for rides' : '⚫ Offline'}
          </Typography>
        </Box>
      </Box>

      {/* Header Card */}
      <Card
        sx={{
          mx: 2,
          mt: -4,
          position: 'relative',
          zIndex: 1,
          borderRadius: 3,
        }}
      >
        <CardContent sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Box>
            <Typography variant="h6" fontWeight="bold">
              Welcome, {user?.fullName || 'Driver'}
            </Typography>
            <Chip
              label={isOnline ? '🟢 Online' : '⚫ Offline'}
              color={isOnline ? 'success' : 'default'}
              size="small"
              sx={{ mt: 1 }}
            />
          </Box>
          <Avatar
            sx={{
              bgcolor: TimoColors.primary,
              width: 56,
              height: 56,
              cursor: 'pointer',
            }}
            onClick={() => navigate('/profile')}
          >
            {user?.fullName?.charAt(0) || 'D'}
          </Avatar>
        </CardContent>
      </Card>

      {/* Pending Ride Requests */}
      {pendingBookings.length > 0 && (
        <Box sx={{ px: 2, mt: 2 }}>
          <Typography variant="h6" sx={{ mb: 2, fontWeight: 'bold' }}>
            New Ride Requests
          </Typography>
          {pendingBookings.slice(0, 2).map((booking) => (
            <Card
              key={booking.id}
              sx={{ mb: 2, cursor: 'pointer', '&:hover': { transform: 'translateY(-2px)' } }}
              onClick={() => handleRideRequest(booking)}
            >
              <CardContent>
                <Typography variant="subtitle1" fontWeight="bold" color="primary">
                  New Ride Request
                </Typography>
                <Typography variant="body2" sx={{ mt: 1 }}>
                  📍 {booking.sourceLocation?.address || 'Pickup location'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  → {booking.destinationLocation?.address || 'Destination'}
                </Typography>
                <Typography
                  variant="h6"
                  sx={{ mt: 1, color: TimoColors.accent, fontWeight: 'bold' }}
                >
                  {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
                </Typography>
              </CardContent>
            </Card>
          ))}
        </Box>
      )}

      {/* Active Ride Banner */}
      {activeBooking && (
        <Box sx={{ px: 2, mt: 2 }}>
          <Card
            sx={{
              bgcolor: TimoColors.accent,
              cursor: 'pointer',
              '&:hover': { transform: 'translateY(-2px)' },
            }}
            onClick={() => navigate(`/active-ride/${activeBooking.id}`, { state: { booking: activeBooking } })}
          >
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <DirectionsCar sx={{ fontSize: 40, color: 'white' }} />
                <Box>
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
        </Box>
      )}

      {/* Availability Toggle */}
      <Box sx={{ px: 2, py: 3, position: 'sticky', bottom: 0, bgcolor: 'white' }}>
        <Button
          fullWidth
          variant="contained"
          size="large"
          onClick={handleToggleAvailability}
          sx={{
            py: 2,
            bgcolor: isOnline ? TimoColors.online : TimoColors.offline,
            '&:hover': {
              bgcolor: isOnline ? TimoColors.online : TimoColors.offline,
              boxShadow: `0 6px 30px ${isOnline ? 'rgba(74, 204, 18, 0.6)' : 'rgba(158, 158, 158, 0.6)'}`,
            },
          }}
        >
          {isOnline ? 'GO OFFLINE' : 'GO ONLINE'}
        </Button>
      </Box>

      {/* Menu FAB */}
      <Fab
        color="primary"
        sx={{ position: 'fixed', bottom: 100, right: 16 }}
        onClick={() => {
          // Open menu
        }}
      >
        <MenuIcon />
      </Fab>
    </Box>
  );
};

export default HomeScreen;

