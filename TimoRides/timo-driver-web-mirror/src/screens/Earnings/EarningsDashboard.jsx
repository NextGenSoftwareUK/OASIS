import React from 'react';
import { useSelector } from 'react-redux';
import {
  Container,
  Card,
  CardContent,
  Typography,
  Box,
  Grid,
} from '@mui/material';
import { ArrowBack } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { TimoColors } from '../../utils/theme';

const EarningsDashboard = () => {
  const navigate = useNavigate();
  const { bookings } = useSelector((state) => state.bookings);

  const completedBookings = bookings.filter((b) => b.status === 'completed');
  
  const todayEarnings = completedBookings
    .filter((b) => {
      const today = new Date();
      const bookingDate = new Date(b.completedAt || b.createdAt);
      return bookingDate.toDateString() === today.toDateString();
    })
    .reduce((sum, b) => sum + (parseFloat(b.tripAmount) || 0), 0);

  const weekEarnings = completedBookings
    .filter((b) => {
      const weekAgo = new Date();
      weekAgo.setDate(weekAgo.getDate() - 7);
      const bookingDate = new Date(b.completedAt || b.createdAt);
      return bookingDate >= weekAgo;
    })
    .reduce((sum, b) => sum + (parseFloat(b.tripAmount) || 0), 0);

  const monthEarnings = completedBookings
    .filter((b) => {
      const monthAgo = new Date();
      monthAgo.setMonth(monthAgo.getMonth() - 1);
      const bookingDate = new Date(b.completedAt || b.createdAt);
      return bookingDate >= monthAgo;
    })
    .reduce((sum, b) => sum + (parseFloat(b.tripAmount) || 0), 0);

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <ArrowBack
          sx={{ cursor: 'pointer', mr: 2 }}
          onClick={() => navigate('/home')}
        />
        <Typography variant="h5" fontWeight="bold">
          Earnings
        </Typography>
      </Box>

      <Card
        sx={{
          bgcolor: TimoColors.primary,
          color: 'white',
          mb: 3,
          borderRadius: 3,
        }}
      >
        <CardContent sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Today's Earnings
          </Typography>
          <Typography variant="h3" fontWeight="bold" color={TimoColors.accent}>
            R {todayEarnings.toFixed(2)}
          </Typography>
          <Typography variant="body2" sx={{ mt: 1 }}>
            {completedBookings.filter((b) => {
              const today = new Date();
              const bookingDate = new Date(b.completedAt || b.createdAt);
              return bookingDate.toDateString() === today.toDateString();
            }).length} rides
          </Typography>
        </CardContent>
      </Card>

      <Grid container spacing={2}>
        <Grid item xs={6}>
          <Card sx={{ borderRadius: 3 }}>
            <CardContent>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                This Week
              </Typography>
              <Typography variant="h5" fontWeight="bold" color="primary">
                R {weekEarnings.toFixed(2)}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6}>
          <Card sx={{ borderRadius: 3 }}>
            <CardContent>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                This Month
              </Typography>
              <Typography variant="h5" fontWeight="bold" color="primary">
                R {monthEarnings.toFixed(2)}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Container>
  );
};

export default EarningsDashboard;

