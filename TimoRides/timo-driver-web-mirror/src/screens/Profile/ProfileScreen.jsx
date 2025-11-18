import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import {
  Container,
  Card,
  CardContent,
  Typography,
  Avatar,
  Button,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Box,
  Chip,
} from '@mui/material';
import {
  ArrowBack,
  Cash,
  Settings,
  Help,
  Logout,
} from '@mui/icons-material';
import { logout } from '../../store/slices/authSlice';
import { TimoColors } from '../../utils/theme';

const ProfileScreen = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { user } = useSelector((state) => state.auth);
  const { isOnline } = useSelector((state) => state.driver);

  const handleLogout = () => {
    dispatch(logout());
    navigate('/login');
  };

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <ArrowBack
          sx={{ cursor: 'pointer', mr: 2 }}
          onClick={() => navigate('/home')}
        />
        <Typography variant="h5" fontWeight="bold">
          Profile
        </Typography>
      </Box>

      <Card sx={{ borderRadius: 3, mb: 2 }}>
        <CardContent sx={{ textAlign: 'center', py: 4 }}>
          <Avatar
            sx={{
              width: 100,
              height: 100,
              bgcolor: TimoColors.primary,
              mx: 'auto',
              mb: 2,
            }}
          >
            {user?.fullName?.charAt(0) || 'D'}
          </Avatar>
          <Typography variant="h5" fontWeight="bold">
            {user?.fullName || 'Driver'}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            {user?.email || ''}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {user?.phone || ''}
          </Typography>
          <Chip
            label={isOnline ? '🟢 Online' : '⚫ Offline'}
            color={isOnline ? 'success' : 'default'}
            sx={{ mt: 2 }}
          />
        </CardContent>
      </Card>

      <Card sx={{ borderRadius: 3 }}>
        <List>
          <ListItem
            button
            onClick={() => navigate('/earnings')}
            sx={{ borderRadius: 2, mb: 1 }}
          >
            <ListItemIcon>
              <Cash color="primary" />
            </ListItemIcon>
            <ListItemText primary="Earnings" />
          </ListItem>
          <ListItem button sx={{ borderRadius: 2, mb: 1 }}>
            <ListItemIcon>
              <Settings color="primary" />
            </ListItemIcon>
            <ListItemText primary="Settings" />
          </ListItem>
          <ListItem button sx={{ borderRadius: 2 }}>
            <ListItemIcon>
              <Help color="primary" />
            </ListItemIcon>
            <ListItemText primary="Help & Support" />
          </ListItem>
        </List>
      </Card>

      <Box sx={{ mt: 3 }}>
        <Button
          fullWidth
          variant="contained"
          color="error"
          startIcon={<Logout />}
          onClick={handleLogout}
          sx={{ py: 1.5 }}
        >
          Logout
        </Button>
      </Box>
    </Container>
  );
};

export default ProfileScreen;

