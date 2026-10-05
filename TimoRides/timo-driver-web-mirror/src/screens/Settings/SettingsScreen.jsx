import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box,
  Typography,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Switch,
  IconButton,
  Avatar,
  Divider,
  Card,
  CardContent,
  Button,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Notifications as NotificationsIcon,
  LocationOn as LocationOnIcon,
  Language as LanguageIcon,
  Security as SecurityIcon,
  Help as HelpIcon,
  ContactSupport as ContactSupportIcon,
  Logout as LogoutIcon,
  DirectionsCar as DirectionsCarIcon,
  AccountCircle as AccountCircleIcon,
} from '@mui/icons-material';
import { logout } from '../../store/slices/authSlice';
import { TimoColors } from '../../utils/theme';

const SettingsScreen = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { user } = useSelector((state) => state.auth);
  
  const currentUser = user || (() => {
    try {
      const userData = localStorage.getItem('timo_driver_user_data');
      return userData ? JSON.parse(userData) : null;
    } catch {
      return null;
    }
  })();

  const [notifications, setNotifications] = useState(true);
  const [location, setLocation] = useState(true);
  const [backgroundTracking, setBackgroundTracking] = useState(true);

  const handleLogout = () => {
    dispatch(logout());
    navigate('/login');
  };

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
          Settings
        </Typography>
      </Box>

      {/* Profile Section */}
      <Card
        sx={{
          m: 2,
          mb: 0,
          background: 'rgba(255, 255, 255, 0.95)',
          backdropFilter: 'blur(10px)',
          border: '1px solid rgba(40, 71, 188, 0.1)',
        }}
      >
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Avatar
              sx={{
                width: 64,
                height: 64,
                bgcolor: TimoColors.primary,
                boxShadow: '0 4px 15px rgba(40, 71, 188, 0.3)',
              }}
            >
              {(currentUser?.fullName || 'Driver')?.charAt(0) || 'D'}
            </Avatar>
            <Box sx={{ flex: 1 }}>
              <Typography variant="h6" fontWeight={600}>
                {currentUser?.fullName || 'Driver'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {currentUser?.email || 'driver@example.com'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {currentUser?.phone || '+27 82 123 4567'}
              </Typography>
            </Box>
            <Button
              variant="outlined"
              size="small"
              onClick={() => navigate('/profile')}
              sx={{
                borderColor: TimoColors.primary,
                color: TimoColors.primary,
                '&:hover': {
                  borderColor: TimoColors.primaryDark,
                  bgcolor: 'rgba(40, 71, 188, 0.05)',
                },
              }}
            >
              Edit
            </Button>
          </Box>
        </CardContent>
      </Card>

      {/* Settings List */}
      <List sx={{ mt: 2, bgcolor: 'white' }}>
        <ListItem>
          <ListItemIcon>
            <NotificationsIcon sx={{ color: TimoColors.primary }} />
          </ListItemIcon>
          <ListItemText
            primary="Notifications"
            secondary="Push notifications for ride requests and updates"
          />
          <Switch
            checked={notifications}
            onChange={(e) => setNotifications(e.target.checked)}
            color="primary"
          />
        </ListItem>

        <ListItem>
          <ListItemIcon>
            <LocationOnIcon sx={{ color: TimoColors.primary }} />
          </ListItemIcon>
          <ListItemText
            primary="Location Services"
            secondary="Use GPS for accurate location tracking"
          />
          <Switch
            checked={location}
            onChange={(e) => setLocation(e.target.checked)}
            color="primary"
          />
        </ListItem>

        <ListItem>
          <ListItemIcon>
            <DirectionsCarIcon sx={{ color: TimoColors.primary }} />
          </ListItemIcon>
          <ListItemText
            primary="Background Tracking"
            secondary="Track location when app is in background"
          />
          <Switch
            checked={backgroundTracking}
            onChange={(e) => setBackgroundTracking(e.target.checked)}
            color="primary"
          />
        </ListItem>

        <Divider sx={{ my: 1 }} />

        <ListItem button onClick={() => navigate('/profile')}>
          <ListItemIcon>
            <AccountCircleIcon sx={{ color: TimoColors.primary }} />
          </ListItemIcon>
          <ListItemText
            primary="Profile"
            secondary="Edit your driver profile and vehicle info"
          />
        </ListItem>

        <ListItem button>
          <ListItemIcon>
            <LanguageIcon sx={{ color: TimoColors.primary }} />
          </ListItemIcon>
          <ListItemText
            primary="Language"
            secondary="English"
          />
        </ListItem>

        <ListItem button>
          <ListItemIcon>
            <SecurityIcon sx={{ color: TimoColors.primary }} />
          </ListItemIcon>
          <ListItemText
            primary="Security"
            secondary="Password, 2FA, Privacy settings"
          />
        </ListItem>

        <Divider sx={{ my: 1 }} />

        <ListItem button onClick={() => navigate('/help')}>
          <ListItemIcon>
            <HelpIcon sx={{ color: TimoColors.primary }} />
          </ListItemIcon>
          <ListItemText
            primary="Help & Support"
            secondary="FAQs, guides, contact support"
          />
        </ListItem>

        <ListItem button>
          <ListItemIcon>
            <ContactSupportIcon sx={{ color: TimoColors.primary }} />
          </ListItemIcon>
          <ListItemText
            primary="Contact Us"
            secondary="Get in touch with our team"
          />
        </ListItem>

        <Divider sx={{ my: 1 }} />

        <ListItem
          button
          onClick={handleLogout}
          sx={{ color: 'error.main' }}
        >
          <ListItemIcon>
            <LogoutIcon sx={{ color: 'error.main' }} />
          </ListItemIcon>
          <ListItemText
            primary="Logout"
            secondary="Sign out of your account"
          />
        </ListItem>
      </List>
    </Box>
  );
};

export default SettingsScreen;


