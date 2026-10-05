import React from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  IconButton,
  Chip,
} from '@mui/material';
import {
  Close,
  Star,
  LocationOn,
  AccessTime,
  DirectionsCar,
} from '@mui/icons-material';
import { acceptBooking, cancelBooking, setActiveBooking, removePendingBooking, updateBookingStatus } from '../../store/slices/bookingSlice';
import { TimoColors } from '../../utils/theme';
import MapView from '../../components/MapView';

const RideRequestScreen = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const location = useLocation();
  const { id } = useParams();
  const { pendingBookings } = useSelector((state) => state.bookings);
  
  // Try to get booking from location state, or find it in pendingBookings
  let booking = location.state?.booking;
  if (!booking && id) {
    booking = pendingBookings.find((b) => b.id === id);
  }

  if (!booking) {
    return (
      <Box sx={{ height: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <Typography>No booking found</Typography>
        <Button onClick={() => navigate('/home')}>Go Home</Button>
      </Box>
    );
  }

  const handleAccept = async () => {
    // For simulated bookings, handle directly without API call
    if (booking.id.startsWith('sim-')) {
      const acceptedBooking = { ...booking, status: 'accepted' };
      dispatch(setActiveBooking(acceptedBooking));
      dispatch(removePendingBooking(booking.id));
      dispatch(updateBookingStatus({ id: booking.id, status: 'accepted' }));
      navigate('/home');
      return;
    }

    // For real bookings, call the API
    try {
      const result = await dispatch(acceptBooking(booking.id));
      if (acceptBooking.fulfilled.match(result)) {
        navigate('/home');
      } else if (acceptBooking.rejected.match(result)) {
        console.error('Failed to accept booking:', result.error);
      }
    } catch (error) {
      console.error('Error accepting booking:', error);
    }
  };

  const handleDecline = async () => {
    // For simulated bookings, handle directly without API call
    if (booking.id.startsWith('sim-')) {
      dispatch(removePendingBooking(booking.id));
      navigate('/home');
      return;
    }

    // For real bookings, call the API
    try {
      const result = await dispatch(cancelBooking(booking.id));
      if (cancelBooking.fulfilled.match(result)) {
        navigate('/home');
      } else if (cancelBooking.rejected.match(result)) {
        console.error('Failed to cancel booking:', result.error);
      }
    } catch (error) {
      console.error('Error canceling booking:', error);
    }
  };

  // Parse trip distance and duration for display
  const tripDistance = booking.tripDistance || 'N/A';
  const tripDuration = booking.tripDuration || 'N/A';
  
  // Calculate pickup distance/time (mock for now - would come from backend)
  const pickupDistance = '5.2 km'; // Would be calculated from driver location
  const pickupTime = '12 mins'; // Would be calculated from driver location

  // Get rider rating (mock for now)
  const riderRating = booking.riderRating || 4.8;

  return (
    <Box sx={{ height: '100vh', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
      {/* Map Section - Top Half */}
      <Box sx={{ flex: 1, position: 'relative' }}>
        <MapView
          pickupLocation={booking.sourceLocation}
          destinationLocation={booking.destinationLocation}
          height="100%"
          showRoute={true}
        />
      </Box>

      {/* Ride Request Card - Bottom Half */}
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
          boxShadow: '0 -8px 32px rgba(0, 0, 0, 0.15)',
          maxHeight: '55vh',
          overflow: 'auto',
        }}
      >
        <CardContent sx={{ p: 3, pb: 2, pt: 2 }}>
          {/* Handle Indicator */}
          <Box
            sx={{
              width: 40,
              height: 4,
              bgcolor: '#E0E0E0',
              borderRadius: 2,
              mx: 'auto',
              mb: 2,
            }}
          />

          {/* Header */}
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Chip
                label="TimoRides"
                sx={{
                  bgcolor: TimoColors.primary,
                  color: 'white',
                  fontWeight: 600,
                  height: 28,
                }}
              />
            </Box>
            <IconButton
              onClick={handleDecline}
              sx={{
                bgcolor: 'rgba(0, 0, 0, 0.05)',
                '&:hover': { bgcolor: 'rgba(0, 0, 0, 0.1)' },
              }}
            >
              <Close />
            </IconButton>
          </Box>

          {/* Fare Amount */}
          <Box sx={{ mb: 2 }}>
            <Typography
              variant="h3"
              fontWeight={700}
              sx={{
                fontSize: { xs: '2rem', sm: '2.5rem' },
                color: TimoColors.primary,
              }}
            >
              {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
            </Typography>
          </Box>

          {/* Rider Rating */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 3 }}>
            <Star sx={{ fontSize: 18, color: TimoColors.accent }} />
            <Typography variant="body2" fontWeight={600}>
              {riderRating.toFixed(1)}
            </Typography>
          </Box>

          {/* Pickup Details */}
          <Box sx={{ mb: 2.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
              <Box
                sx={{
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  mt: 0.5,
                }}
              >
                <Box
                  sx={{
                    width: 12,
                    height: 12,
                    borderRadius: '50%',
                    bgcolor: TimoColors.primary,
                    border: '2px solid white',
                    boxShadow: '0 2px 8px rgba(0, 0, 0, 0.2)',
                  }}
                />
                <Box
                  sx={{
                    width: 2,
                    height: 40,
                    bgcolor: '#E0E0E0',
                    mt: 0.5,
                  }}
                />
              </Box>
              <Box sx={{ flex: 1 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                  <AccessTime sx={{ fontSize: 16, color: 'text.secondary' }} />
                  <Typography variant="body2" color="text.secondary" fontWeight={500}>
                    {pickupTime} ({pickupDistance}) away
                  </Typography>
                </Box>
                <Typography variant="body1" fontWeight={500}>
                  {booking.sourceLocation?.address || 'Pickup location'}
                </Typography>
              </Box>
            </Box>
          </Box>

          {/* Trip Details */}
          <Box sx={{ mb: 3 }}>
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
              <Box
                sx={{
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  mt: 0.5,
                }}
              >
                <Box
                  sx={{
                    width: 2,
                    height: 40,
                    bgcolor: '#E0E0E0',
                    mb: 0.5,
                  }}
                />
                <Box
                  sx={{
                    width: 12,
                    height: 12,
                    borderRadius: '50%',
                    bgcolor: TimoColors.error,
                    border: '2px solid white',
                    boxShadow: '0 2px 8px rgba(0, 0, 0, 0.2)',
                    position: 'relative',
                    '&::after': {
                      content: '""',
                      position: 'absolute',
                      top: '50%',
                      left: '50%',
                      transform: 'translate(-50%, -50%)',
                      width: 0,
                      height: 0,
                      borderLeft: '4px solid white',
                      borderTop: '3px solid transparent',
                      borderBottom: '3px solid transparent',
                      ml: 0.5,
                    },
                  }}
                />
              </Box>
              <Box sx={{ flex: 1 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                  <DirectionsCar sx={{ fontSize: 16, color: 'text.secondary' }} />
                  <Typography variant="body2" color="text.secondary" fontWeight={500}>
                    {tripDuration} ({tripDistance}) trip
                  </Typography>
                </Box>
                <Typography variant="body1" fontWeight={500}>
                  {booking.destinationLocation?.address || 'Destination'}
                </Typography>
              </Box>
            </Box>
          </Box>

          {/* Accept Button */}
          <Button
            variant="contained"
            fullWidth
            onClick={handleAccept}
            sx={{
              bgcolor: TimoColors.primary,
              color: 'white',
              py: 1.5,
              fontSize: '1.1rem',
              fontWeight: 600,
              borderRadius: 2,
              textTransform: 'none',
              boxShadow: '0 4px 20px rgba(40, 71, 188, 0.4)',
              '&:hover': {
                bgcolor: TimoColors.primaryDark,
                boxShadow: '0 6px 30px rgba(40, 71, 188, 0.6)',
                transform: 'translateY(-2px)',
              },
            }}
          >
            Accept
          </Button>
        </CardContent>
      </Card>
    </Box>
  );
};

export default RideRequestScreen;
