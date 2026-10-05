import React, { useState } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import {
  Container,
  Card,
  CardContent,
  Typography,
  Button,
  Box,
  Divider,
  Chip,
  Avatar,
  IconButton,
} from '@mui/material';
import {
  ArrowBack,
  Phone,
  LocationOn,
  DirectionsCar,
  Person,
  AccessTime,
  AttachMoney,
  CheckCircle,
  Cancel,
  PlayArrow,
} from '@mui/icons-material';
import { clearActiveBooking, updateBookingStatus } from '../../store/slices/bookingSlice';
import { TimoColors } from '../../utils/theme';
import MapView from '../../components/MapView';

const ActiveRideScreen = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const location = useLocation();
  const { id } = useParams();
  const { activeBooking, bookings } = useSelector((state) => state.bookings);
  const [rideStatus, setRideStatus] = useState('accepted'); // accepted, started, completed

  // Try to get booking from location state, activeBooking, or find it in bookings
  let booking = location.state?.booking || activeBooking;
  if (!booking && id) {
    booking = bookings.find((b) => b.id === id);
  }

  if (!booking) {
    return (
      <Container>
        <Typography>No active ride found</Typography>
        <Button onClick={() => navigate('/home')}>Go Home</Button>
      </Container>
    );
  }

  const handleStartRide = () => {
    setRideStatus('started');
    dispatch(updateBookingStatus({ id: booking.id, status: 'started' }));
  };

  const handleCompleteRide = () => {
    setRideStatus('completed');
    dispatch(updateBookingStatus({ id: booking.id, status: 'completed' }));
    // Navigate to trip complete screen for payment and rating
    navigate(`/trip-complete/${booking.id}`, { state: { booking: { ...booking, status: 'completed' } } });
  };

  const handleCancelRide = () => {
    if (window.confirm('Are you sure you want to cancel this ride?')) {
      dispatch(clearActiveBooking());
      dispatch(updateBookingStatus({ id: booking.id, status: 'cancelled' }));
      navigate('/home');
    }
  };

  const handleCallRider = () => {
    if (booking.phoneNumber) {
      window.location.href = `tel:${booking.phoneNumber.replace(/\s/g, '')}`;
    }
  };

  const getStatusColor = (status) => {
    switch (status) {
      case 'accepted':
        return 'info';
      case 'started':
        return 'warning';
      case 'completed':
        return 'success';
      case 'cancelled':
        return 'error';
      default:
        return 'default';
    }
  };

  const getStatusLabel = (status) => {
    switch (status) {
      case 'accepted':
        return 'Accepted';
      case 'started':
        return 'In Progress';
      case 'completed':
        return 'Completed';
      case 'cancelled':
        return 'Cancelled';
      default:
        return status;
    }
  };

  const currentStatus = rideStatus || booking.status || 'accepted';

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/home')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Typography variant="h5" fontWeight="bold" sx={{ flex: 1 }}>
          Active Ride
        </Typography>
        <Chip
          label={getStatusLabel(currentStatus)}
          color={getStatusColor(currentStatus)}
          sx={{ fontWeight: 600 }}
        />
      </Box>

      {/* Map View */}
      <Card sx={{ borderRadius: 3, mb: 3, overflow: 'hidden' }}>
        <MapView
          pickupLocation={booking.sourceLocation}
          destinationLocation={booking.destinationLocation}
          height="400px"
          showRoute={true}
        />
      </Card>

      <Card sx={{ borderRadius: 3, mb: 3 }}>
        <CardContent sx={{ p: 4 }}>
          {/* Rider Information */}
          <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
            <Avatar
              sx={{
                bgcolor: TimoColors.primary,
                width: 64,
                height: 64,
                mr: 2,
                fontSize: '1.5rem',
              }}
            >
              {booking.fullName?.charAt(0) || 'R'}
            </Avatar>
            <Box sx={{ flex: 1 }}>
              <Typography variant="h6" fontWeight="bold">
                {booking.fullName || 'Rider'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {booking.phoneNumber || 'N/A'}
              </Typography>
            </Box>
            <IconButton
              onClick={handleCallRider}
              sx={{
                bgcolor: TimoColors.primary,
                color: 'white',
                '&:hover': { bgcolor: TimoColors.primaryDark },
              }}
            >
              <Phone />
            </IconButton>
          </Box>

          <Divider sx={{ my: 3 }} />

          {/* Pickup Location */}
          <Box sx={{ mb: 3 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
              <LocationOn sx={{ color: TimoColors.success, mr: 1 }} />
              <Typography variant="subtitle1" fontWeight="bold" color="primary">
                Pickup Location
              </Typography>
            </Box>
            <Typography variant="body1" sx={{ ml: 4 }}>
              {booking.sourceLocation?.address || 'N/A'}
            </Typography>
            {booking.sourceLocation?.latitude && booking.sourceLocation?.longitude && (
              <Typography variant="caption" color="text.secondary" sx={{ ml: 4 }}>
                {booking.sourceLocation.latitude.toFixed(4)}, {booking.sourceLocation.longitude.toFixed(4)}
              </Typography>
            )}
          </Box>

          {/* Destination */}
          <Box sx={{ mb: 3 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
              <LocationOn sx={{ color: TimoColors.error, mr: 1 }} />
              <Typography variant="subtitle1" fontWeight="bold" color="primary">
                Destination
              </Typography>
            </Box>
            <Typography variant="body1" sx={{ ml: 4 }}>
              {booking.destinationLocation?.address || 'N/A'}
            </Typography>
            {booking.destinationLocation?.latitude && booking.destinationLocation?.longitude && (
              <Typography variant="caption" color="text.secondary" sx={{ ml: 4 }}>
                {booking.destinationLocation.latitude.toFixed(4)}, {booking.destinationLocation.longitude.toFixed(4)}
              </Typography>
            )}
          </Box>

          <Divider sx={{ my: 3 }} />

          {/* Trip Details */}
          <Box sx={{ mb: 3 }}>
            <Typography variant="subtitle1" fontWeight="bold" color="primary" sx={{ mb: 2 }}>
              Trip Details
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <DirectionsCar sx={{ mr: 2, color: TimoColors.primary }} />
                <Typography variant="body2" sx={{ flex: 1 }}>
                  Distance
                </Typography>
                <Typography variant="body2" fontWeight="bold">
                  {booking.tripDistance || 'N/A'}
                </Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <AccessTime sx={{ mr: 2, color: TimoColors.primary }} />
                <Typography variant="body2" sx={{ flex: 1 }}>
                  Estimated Duration
                </Typography>
                <Typography variant="body2" fontWeight="bold">
                  {booking.tripDuration || 'N/A'}
                </Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <Person sx={{ mr: 2, color: TimoColors.primary }} />
                <Typography variant="body2" sx={{ flex: 1 }}>
                  Passengers
                </Typography>
                <Typography variant="body2" fontWeight="bold">
                  {booking.passengers || 1}
                </Typography>
              </Box>
            </Box>
          </Box>

          {/* Fare Amount */}
          <Box
            sx={{
              bgcolor: TimoColors.backgroundLight,
              p: 3,
              borderRadius: 2,
              textAlign: 'center',
              mb: 3,
            }}
          >
            <Typography variant="h4" fontWeight="bold" color={TimoColors.accent}>
              {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              Payment: {booking.isCash ? 'Cash' : 'Online'}
            </Typography>
          </Box>

          {/* Action Buttons */}
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {currentStatus === 'accepted' && (
              <Button
                variant="contained"
                fullWidth
                startIcon={<PlayArrow />}
                onClick={handleStartRide}
                sx={{
                  bgcolor: TimoColors.primary,
                  py: 1.5,
                  fontSize: '1rem',
                  fontWeight: 600,
                }}
              >
                Start Ride
              </Button>
            )}

            {currentStatus === 'started' && (
              <Button
                variant="contained"
                fullWidth
                startIcon={<CheckCircle />}
                onClick={handleCompleteRide}
                sx={{
                  bgcolor: TimoColors.success,
                  py: 1.5,
                  fontSize: '1rem',
                  fontWeight: 600,
                }}
              >
                Complete Ride
              </Button>
            )}

            {currentStatus !== 'completed' && (
              <Button
                variant="outlined"
                fullWidth
                startIcon={<Cancel />}
                onClick={handleCancelRide}
                sx={{
                  borderColor: TimoColors.error,
                  color: TimoColors.error,
                  py: 1.5,
                  '&:hover': {
                    borderColor: TimoColors.error,
                    bgcolor: 'rgba(244, 67, 54, 0.1)',
                  },
                }}
              >
                Cancel Ride
              </Button>
            )}

            {currentStatus === 'completed' && (
              <Box
                sx={{
                  bgcolor: TimoColors.success,
                  color: 'white',
                  p: 2,
                  borderRadius: 2,
                  textAlign: 'center',
                }}
              >
                <CheckCircle sx={{ fontSize: 48, mb: 1 }} />
                <Typography variant="h6" fontWeight="bold">
                  Ride Completed!
                </Typography>
                <Typography variant="body2" sx={{ mt: 1 }}>
                  Returning to home...
                </Typography>
              </Box>
            )}
          </Box>
        </CardContent>
      </Card>
    </Container>
  );
};

export default ActiveRideScreen;

