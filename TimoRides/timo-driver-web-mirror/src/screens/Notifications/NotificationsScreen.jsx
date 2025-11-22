import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Card,
  CardContent,
  IconButton,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Chip,
  Divider,
  Badge,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Notifications as NotificationsIcon,
  NotificationsActive as NotificationsActiveIcon,
  CheckCircle as CheckCircleIcon,
  Info as InfoIcon,
  Warning as WarningIcon,
  Error as ErrorIcon,
} from '@mui/icons-material';
import { TimoColors } from '../../utils/theme';

const NotificationsScreen = () => {
  const navigate = useNavigate();

  // Mock notifications - in production, this would come from Redux/API
  const notifications = [
    {
      id: 1,
      type: 'ride_request',
      title: 'New Ride Request',
      message: 'You have a new ride request from Umhlanga to Durban Airport',
      time: '2 minutes ago',
      read: false,
      icon: <NotificationsActiveIcon />,
      color: 'primary',
    },
    {
      id: 2,
      type: 'ride_accepted',
      title: 'Ride Accepted',
      message: 'Your ride request has been accepted. Driver is on the way.',
      time: '15 minutes ago',
      read: false,
      icon: <CheckCircleIcon />,
      color: 'success',
    },
    {
      id: 3,
      type: 'payment',
      title: 'Payment Received',
      message: 'You received R250.00 for ride #12345',
      time: '1 hour ago',
      read: true,
      icon: <CheckCircleIcon />,
      color: 'success',
    },
    {
      id: 4,
      type: 'system',
      title: 'System Update',
      message: 'New features available: Enhanced navigation and earnings tracking',
      time: '2 hours ago',
      read: true,
      icon: <InfoIcon />,
      color: 'info',
    },
    {
      id: 5,
      type: 'warning',
      title: 'Low Battery',
      message: 'Your device battery is below 20%. Please charge soon.',
      time: '3 hours ago',
      read: true,
      icon: <WarningIcon />,
      color: 'warning',
    },
  ];

  const unreadCount = notifications.filter(n => !n.read).length;

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
          Notifications
        </Typography>
        {unreadCount > 0 && (
          <Chip
            label={unreadCount}
            size="small"
            sx={{
              bgcolor: TimoColors.accent,
              color: 'black',
              fontWeight: 600,
            }}
          />
        )}
      </Box>

      {/* Notifications List */}
      <Box sx={{ p: 2, pb: 10 }}>
        {notifications.length > 0 ? (
          <List sx={{ p: 0 }}>
            {notifications.map((notification, index) => (
              <React.Fragment key={notification.id}>
                <Card
                  sx={{
                    mb: 2,
                    background: notification.read
                      ? 'rgba(255, 255, 255, 0.95)'
                      : 'rgba(40, 71, 188, 0.05)',
                    backdropFilter: 'blur(10px)',
                    border: notification.read
                      ? '1px solid rgba(40, 71, 188, 0.1)'
                      : '2px solid rgba(40, 71, 188, 0.3)',
                    transition: 'all 0.3s ease',
                    '&:hover': {
                      boxShadow: '0 8px 30px rgba(40, 71, 188, 0.15)',
                      transform: 'translateY(-2px)',
                    },
                  }}
                >
                  <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                    <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
                      <Box
                        sx={{
                          width: 48,
                          height: 48,
                          borderRadius: '50%',
                          bgcolor: `${notification.color}.light`,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          color: `${notification.color}.main`,
                          flexShrink: 0,
                        }}
                      >
                        {notification.icon}
                      </Box>
                      <Box sx={{ flex: 1, minWidth: 0 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                          <Typography
                            variant="subtitle1"
                            fontWeight={notification.read ? 500 : 700}
                            sx={{
                              color: notification.read ? 'text.primary' : TimoColors.primary,
                            }}
                          >
                            {notification.title}
                          </Typography>
                          {!notification.read && (
                            <Box
                              sx={{
                                width: 8,
                                height: 8,
                                borderRadius: '50%',
                                bgcolor: TimoColors.primary,
                                flexShrink: 0,
                              }}
                            />
                          )}
                        </Box>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          sx={{ mb: 1 }}
                        >
                          {notification.message}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {notification.time}
                        </Typography>
                      </Box>
                    </Box>
                  </CardContent>
                </Card>
                {index < notifications.length - 1 && <Divider sx={{ my: 1 }} />}
              </React.Fragment>
            ))}
          </List>
        ) : (
          <Box
            sx={{
              textAlign: 'center',
              py: 8,
            }}
          >
            <NotificationsIcon
              sx={{
                fontSize: 64,
                color: 'text.secondary',
                mb: 2,
                opacity: 0.5,
              }}
            />
            <Typography variant="h6" color="text.secondary" sx={{ mb: 1 }}>
              No notifications
            </Typography>
            <Typography variant="body2" color="text.secondary">
              You're all caught up!
            </Typography>
          </Box>
        )}
      </Box>
    </Box>
  );
};

export default NotificationsScreen;


