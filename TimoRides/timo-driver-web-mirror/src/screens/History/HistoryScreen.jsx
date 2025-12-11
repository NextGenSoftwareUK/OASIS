import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Avatar,
  IconButton,
  Chip,
  Divider,
  Tabs,
  Tab,
  Rating,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  LocationOn as LocationOnIcon,
  CalendarToday as CalendarIcon,
  AccountCircle as RiderIcon,
} from '@mui/icons-material';
import { TimoColors } from '../../utils/theme';

const HistoryScreen = () => {
  const navigate = useNavigate();
  const [tabValue, setTabValue] = useState(0);

  // Mock data - in production, this would come from Redux/API
  const rides = [
    {
      id: 1,
      rider: 'Alvin Armstrong',
      pickup: 'Durban Central',
      destination: 'Umhlanga Beach',
      date: '2025-01-15',
      time: '14:30',
      rating: 5,
      earnings: 'R250',
      status: 'completed',
    },
    {
      id: 2,
      rider: 'Sarah Johnson',
      pickup: 'Umhlanga',
      destination: 'Durban Airport',
      date: '2025-01-10',
      time: '09:15',
      rating: 4,
      earnings: 'R320',
      status: 'completed',
    },
    {
      id: 3,
      rider: 'Mike Thompson',
      pickup: 'Gateway Mall',
      destination: 'Durban Central',
      date: '2025-01-05',
      time: '18:45',
      rating: 5,
      earnings: 'R180',
      status: 'completed',
    },
  ];

  const filteredRides = rides; // In production, filter based on tabValue

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: TimoColors.backgroundLight }}>
      {/* Header */}
      <Box
        sx={{
          backgroundColor: TimoColors.primary,
          color: 'white',
          p: 2,
          display: 'flex',
          alignItems: 'center',
          gap: 2,
        }}
      >
        <IconButton onClick={() => navigate('/home')} sx={{ color: 'white' }}>
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h6" sx={{ flex: 1, fontWeight: 600 }}>
          Ride History
        </Typography>
      </Box>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', bgcolor: 'white' }}>
        <Tabs
          value={tabValue}
          onChange={(e, newValue) => setTabValue(newValue)}
          sx={{
            '& .MuiTab-root': {
              textTransform: 'none',
              fontWeight: 500,
            },
          }}
        >
          <Tab label="All Rides" />
          <Tab label="This Month" />
          <Tab label="This Year" />
        </Tabs>
      </Box>

      {/* Ride List */}
      <Box sx={{ p: 2, pb: 10 }}>
        {filteredRides.length > 0 ? (
          filteredRides.map((ride) => (
            <Card
              key={ride.id}
              sx={{
                mb: 2,
                background: 'rgba(255, 255, 255, 0.95)',
                backdropFilter: 'blur(10px)',
                border: '1px solid rgba(40, 71, 188, 0.1)',
                transition: 'all 0.3s ease',
                '&:hover': {
                  boxShadow: '0 8px 30px rgba(40, 71, 188, 0.15)',
                  transform: 'translateY(-2px)',
                },
              }}
            >
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
                  <Avatar
                    sx={{
                      width: 56,
                      height: 56,
                      bgcolor: TimoColors.primary,
                      boxShadow: '0 4px 15px rgba(40, 71, 188, 0.2)',
                    }}
                  >
                    <RiderIcon />
                  </Avatar>
                  <Box sx={{ flex: 1 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                      <Typography variant="subtitle1" fontWeight={600}>
                        {ride.rider}
                      </Typography>
                      <Chip
                        label={ride.status}
                        size="small"
                        color="success"
                        sx={{ height: 20, fontSize: '0.7rem' }}
                      />
                    </Box>

                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                      <LocationOnIcon fontSize="small" color="primary" />
                      <Typography variant="body2" fontWeight={600}>
                        {ride.pickup}
                      </Typography>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1, ml: 3 }}>
                      <Typography variant="body2" color="text.secondary">
                        → {ride.destination}
                      </Typography>
                    </Box>

                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                        <CalendarIcon fontSize="small" color="text.secondary" />
                        <Typography variant="body2" color="text.secondary">
                          {ride.date} at {ride.time}
                        </Typography>
                      </Box>
                    </Box>

                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography variant="body2" color="text.secondary">
                        Rating:
                      </Typography>
                      <Rating value={ride.rating} readOnly size="small" />
                    </Box>
                  </Box>
                  <Box sx={{ textAlign: 'right' }}>
                    <Typography
                      variant="h6"
                      fontWeight={700}
                      color="primary"
                      sx={{
                        textShadow: '0 0 10px rgba(40, 71, 188, 0.3)',
                      }}
                    >
                      {ride.earnings}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                      Earnings
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          ))
        ) : (
          <Box
            sx={{
              textAlign: 'center',
              py: 8,
            }}
          >
            <Typography variant="h6" color="text.secondary" sx={{ mb: 1 }}>
              No rides yet
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Your completed rides will appear here
            </Typography>
          </Box>
        )}
      </Box>
    </Box>
  );
};

export default HistoryScreen;


