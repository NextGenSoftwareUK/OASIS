import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import {
  Container,
  Card,
  CardContent,
  Typography,
  Button,
  Box,
  Divider,
  Rating,
  Avatar,
  IconButton,
} from '@mui/material';
import {
  CheckCircle,
  Star,
  AttachMoney,
  ArrowBack,
} from '@mui/icons-material';
import { clearActiveBooking } from '../../store/slices/bookingSlice';
import { TimoColors } from '../../utils/theme';

const TripCompleteScreen = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const location = useLocation();
  const booking = location.state?.booking;

  const [rating, setRating] = useState(0);
  const [ratingSubmitted, setRatingSubmitted] = useState(false);

  if (!booking) {
    return (
      <Container>
        <Typography>No trip data found</Typography>
        <Button onClick={() => navigate('/home')}>Go Home</Button>
      </Container>
    );
  }

  const handleSubmitRating = () => {
    setRatingSubmitted(true);
    // In a real app, this would submit the rating to the backend
    setTimeout(() => {
      dispatch(clearActiveBooking());
      navigate('/home');
    }, 2000);
  };

  const handleSkipRating = () => {
    dispatch(clearActiveBooking());
    navigate('/home');
  };

  return (
    <Container maxWidth="sm" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/home')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Typography variant="h5" fontWeight="bold" sx={{ flex: 1 }}>
          Trip Complete
        </Typography>
      </Box>

      {/* Success Message */}
      <Card sx={{ borderRadius: 3, mb: 3, bgcolor: TimoColors.success, color: 'white' }}>
        <CardContent sx={{ p: 4, textAlign: 'center' }}>
          <CheckCircle sx={{ fontSize: 64, mb: 2 }} />
          <Typography variant="h4" fontWeight="bold" gutterBottom>
            Trip Completed!
          </Typography>
          <Typography variant="body1">
            Thank you for the safe ride
          </Typography>
        </CardContent>
      </Card>

      {/* Payment Summary */}
      <Card sx={{ borderRadius: 3, mb: 3 }}>
        <CardContent sx={{ p: 4 }}>
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
                {booking.sourceLocation?.address || 'Pickup'} → {booking.destinationLocation?.address || 'Destination'}
              </Typography>
            </Box>
          </Box>

          <Divider sx={{ my: 3 }} />

          {/* Trip Details */}
          <Box sx={{ mb: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Distance
              </Typography>
              <Typography variant="body2" fontWeight={600}>
                {booking.tripDistance || 'N/A'}
              </Typography>
            </Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Duration
              </Typography>
              <Typography variant="body2" fontWeight={600}>
                {booking.tripDuration || 'N/A'}
              </Typography>
            </Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Payment Method
              </Typography>
              <Typography variant="body2" fontWeight={600}>
                {booking.isCash ? 'Cash' : 'Online'}
              </Typography>
            </Box>
          </Box>

          <Divider sx={{ my: 3 }} />

          {/* Fare Amount */}
          <Box
            sx={{
              bgcolor: TimoColors.backgroundLight,
              p: 3,
              borderRadius: 2,
              textAlign: 'center',
            }}
          >
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Total Fare
            </Typography>
            <Typography
              variant="h3"
              fontWeight={700}
              color={TimoColors.accent}
            >
              {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
            </Typography>
            {!booking.isCash && (
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mt: 2, gap: 1 }}>
                <AttachMoney sx={{ fontSize: 20, color: TimoColors.success }} />
                <Typography variant="body2" color={TimoColors.success} fontWeight={600}>
                  Payment Received
                </Typography>
              </Box>
            )}
          </Box>
        </CardContent>
      </Card>

      {/* Rating Section */}
      {!ratingSubmitted ? (
        <Card sx={{ borderRadius: 3 }}>
          <CardContent sx={{ p: 4 }}>
            <Typography variant="h6" fontWeight="bold" gutterBottom textAlign="center">
              Rate Your Rider
            </Typography>
            <Typography variant="body2" color="text.secondary" textAlign="center" sx={{ mb: 3 }}>
              How was your experience with {booking.fullName || 'this rider'}?
            </Typography>

            <Box sx={{ display: 'flex', justifyContent: 'center', mb: 3 }}>
              <Rating
                name="rider-rating"
                value={rating}
                onChange={(event, newValue) => {
                  setRating(newValue);
                }}
                size="large"
                sx={{
                  '& .MuiRating-iconFilled': {
                    color: TimoColors.accent,
                  },
                  '& .MuiRating-iconEmpty': {
                    color: '#E0E0E0',
                  },
                  fontSize: '3rem',
                }}
              />
            </Box>

            {rating > 0 && (
              <Box sx={{ display: 'flex', gap: 2, mt: 3 }}>
                <Button
                  variant="outlined"
                  fullWidth
                  onClick={handleSkipRating}
                  sx={{
                    borderColor: TimoColors.primary,
                    color: TimoColors.primary,
                  }}
                >
                  Skip
                </Button>
                <Button
                  variant="contained"
                  fullWidth
                  onClick={handleSubmitRating}
                  sx={{
                    bgcolor: TimoColors.primary,
                  }}
                >
                  Submit Rating
                </Button>
              </Box>
            )}
          </CardContent>
        </Card>
      ) : (
        <Card sx={{ borderRadius: 3, bgcolor: TimoColors.backgroundLight }}>
          <CardContent sx={{ p: 4, textAlign: 'center' }}>
            <Star sx={{ fontSize: 48, color: TimoColors.accent, mb: 2 }} />
            <Typography variant="h6" fontWeight="bold" gutterBottom>
              Thank You!
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Your rating has been submitted
            </Typography>
          </CardContent>
        </Card>
      )}
    </Container>
  );
};

export default TripCompleteScreen;

