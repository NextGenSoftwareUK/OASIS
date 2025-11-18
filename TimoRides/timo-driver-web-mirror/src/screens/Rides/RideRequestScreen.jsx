import React from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import {
  Container,
  Card,
  CardContent,
  Typography,
  Button,
  Box,
  Divider,
} from '@mui/material';
import { ArrowBack } from '@mui/icons-material';
import { acceptBooking, cancelBooking } from '../../store/slices/bookingSlice';
import { TimoColors } from '../../utils/theme';

const RideRequestScreen = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const location = useLocation();
  const booking = location.state?.booking;

  if (!booking) {
    return (
      <Container>
        <Typography>No booking found</Typography>
      </Container>
    );
  }

  const handleAccept = async () => {
    await dispatch(acceptBooking(booking.id));
    navigate('/home');
  };

  const handleDecline = async () => {
    await dispatch(cancelBooking(booking.id));
    navigate('/home');
  };

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Button
        startIcon={<ArrowBack />}
        onClick={() => navigate('/home')}
        sx={{ mb: 2 }}
      >
        Back
      </Button>

      <Card sx={{ borderRadius: 3 }}>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="h5" gutterBottom fontWeight="bold">
            Ride Request
          </Typography>

          <Box sx={{ my: 3 }}>
            <Typography variant="subtitle1" fontWeight="bold" color="primary">
              Rider Information
            </Typography>
            <Typography variant="h6" sx={{ mt: 1 }}>
              {booking.fullName}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {booking.phoneNumber}
            </Typography>
          </Box>

          <Divider sx={{ my: 3 }} />

          <Box sx={{ my: 3 }}>
            <Typography variant="subtitle1" fontWeight="bold" color="primary">
              📍 Pickup Location
            </Typography>
            <Typography variant="body1" sx={{ mt: 1 }}>
              {booking.sourceLocation?.address || 'N/A'}
            </Typography>
          </Box>

          <Box sx={{ my: 3 }}>
            <Typography variant="subtitle1" fontWeight="bold" color="primary">
              📍 Destination
            </Typography>
            <Typography variant="body1" sx={{ mt: 1 }}>
              {booking.destinationLocation?.address || 'N/A'}
            </Typography>
          </Box>

          <Box sx={{ my: 3 }}>
            <Typography variant="subtitle1" fontWeight="bold" color="primary">
              Trip Details
            </Typography>
            <Typography variant="body2" sx={{ mt: 1 }}>
              Distance: {booking.tripDistance || 'N/A'}
            </Typography>
            <Typography variant="body2">
              Duration: {booking.tripDuration || 'N/A'}
            </Typography>
            <Typography variant="body2">
              Passengers: {booking.passengers || 1}
            </Typography>
          </Box>

          <Box
            sx={{
              bgcolor: TimoColors.backgroundLight,
              p: 3,
              borderRadius: 2,
              textAlign: 'center',
              my: 3,
            }}
          >
            <Typography variant="h4" fontWeight="bold" color={TimoColors.accent}>
              {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              Payment: {booking.isCash ? 'Cash' : 'Online'}
            </Typography>
          </Box>

          <Box sx={{ display: 'flex', gap: 2, mt: 3 }}>
            <Button
              variant="outlined"
              fullWidth
              onClick={handleDecline}
              sx={{ borderColor: TimoColors.error, color: TimoColors.error }}
            >
              Decline
            </Button>
            <Button
              variant="contained"
              fullWidth
              onClick={handleAccept}
              sx={{ bgcolor: TimoColors.primary }}
            >
              Accept Ride
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Container>
  );
};

export default RideRequestScreen;

