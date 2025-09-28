import React from 'react';
import {
  AppBar,
  Toolbar,
  Typography,
  IconButton,
  Box,
  Chip,
  Tooltip,
  Menu,
  MenuItem,
  Avatar,
  Divider,
} from '@mui/material';
import {
  Menu as MenuIcon,
  PowerSettingsNew,
  PowerOff,
  Settings,
  AccountCircle,
  Notifications,
  Refresh,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-hot-toast';

interface NavbarProps {
  onMenuClick: () => void;
  isConnected: boolean;
  connectionStatus: string;
  igniteSTAR: () => Promise<any>;
  extinguishStar: () => Promise<any>;
  reconnect: () => Promise<any>;
}

const Navbar: React.FC<NavbarProps> = ({ onMenuClick, isConnected, connectionStatus, igniteSTAR, extinguishStar, reconnect }) => {
  const navigate = useNavigate();
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const [notificationsAnchor, setNotificationsAnchor] = React.useState<null | HTMLElement>(null);

  // Debug logging
  console.log('Navbar received props:', { isConnected, connectionStatus });

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleNotificationsMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setNotificationsAnchor(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setNotificationsAnchor(null);
  };

  const handleIgniteSTAR = async () => {
    try {
      await igniteSTAR();
      toast.success('STAR ignited successfully!');
    } catch (error) {
      toast.error('Failed to ignite STAR');
    }
    handleMenuClose();
  };

  const handleExtinguishStar = async () => {
    try {
      await extinguishStar();
      toast.success('STAR extinguished successfully!');
    } catch (error) {
      toast.error('Failed to extinguish STAR');
    }
    handleMenuClose();
  };

  const handleReconnect = async () => {
    try {
      await reconnect();
      toast.success('Reconnected to STAR');
    } catch (error) {
      toast.error('Failed to reconnect');
    }
    handleMenuClose();
  };

  const handleSettingsClick = () => {
    navigate('/settings');
    handleMenuClose();
  };

  const getStatusColor = () => {
    switch (connectionStatus) {
      case 'connected':
        return 'success';
      case 'connecting':
        return 'warning';
      case 'error':
        return 'error';
      default:
        return 'default';
    }
  };

  const getStatusText = () => {
    switch (connectionStatus) {
      case 'connected':
        return 'Connected';
      case 'connecting':
        return 'Connecting...';
      case 'error':
        return 'Error';
      default:
        return 'Disconnected';
    }
  };

  return (
    <AppBar 
      position="fixed" 
      sx={{ 
        zIndex: (theme) => theme.zIndex.drawer + 1,
        background: 'linear-gradient(135deg, #0a0a0a 0%, #1a1a1a 100%)',
        borderBottom: '1px solid rgba(255, 255, 255, 0.1)',
      }}
    >
      <Toolbar>
        <IconButton
          color="inherit"
          aria-label="open drawer"
          onClick={onMenuClick}
          edge="start"
          sx={{ mr: 2 }}
        >
          <MenuIcon />
        </IconButton>

        <Box sx={{ display: 'flex', alignItems: 'center', flexGrow: 1 }}>
          <motion.div
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.5 }}
          >
            <Typography 
              variant="h6" 
              noWrap 
              component="div"
              sx={{ 
                fontWeight: 300,
                background: 'linear-gradient(45deg, #00bcd4, #ff4081)',
                backgroundClip: 'text',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                mr: 2,
              }}
            >
              STAR Web UI
            </Typography>
          </motion.div>

          <Chip
            label={getStatusText()}
            color={getStatusColor() as any}
            size="small"
            variant="outlined"
            sx={{ 
              ml: 2,
              borderColor: isConnected ? '#4caf50' : '#f44336',
              color: isConnected ? '#4caf50' : '#f44336',
            }}
          />
        </Box>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Tooltip title="Notifications">
            <IconButton
              color="inherit"
              onClick={handleNotificationsMenuOpen}
            >
              <Notifications />
            </IconButton>
          </Tooltip>

          <Tooltip title="STAR Controls">
            <IconButton
              color="inherit"
              onClick={handleProfileMenuOpen}
            >
              <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main' }}>
                <PowerSettingsNew />
              </Avatar>
            </IconButton>
          </Tooltip>
        </Box>

        {/* STAR Controls Menu */}
        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={handleMenuClose}
          PaperProps={{
            sx: {
              bgcolor: 'background.paper',
              border: '1px solid rgba(255, 255, 255, 0.1)',
              minWidth: 200,
            },
          }}
        >
          <MenuItem onClick={handleIgniteSTAR} disabled={isConnected}>
            <PowerSettingsNew sx={{ mr: 1 }} />
            Ignite STAR
          </MenuItem>
          <MenuItem onClick={handleExtinguishStar} disabled={!isConnected}>
            <PowerOff sx={{ mr: 1 }} />
            Extinguish STAR
          </MenuItem>
          <Divider />
          <MenuItem onClick={handleReconnect}>
            <Refresh sx={{ mr: 1 }} />
            Reconnect
          </MenuItem>
          <MenuItem onClick={handleSettingsClick}>
            <Settings sx={{ mr: 1 }} />
            Settings
          </MenuItem>
        </Menu>

        {/* Notifications Menu */}
        <Menu
          anchorEl={notificationsAnchor}
          open={Boolean(notificationsAnchor)}
          onClose={handleMenuClose}
          PaperProps={{
            sx: {
              bgcolor: 'background.paper',
              border: '1px solid rgba(255, 255, 255, 0.1)',
              minWidth: 300,
            },
          }}
        >
          <MenuItem>
            <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
              Notifications
            </Typography>
          </MenuItem>
          <Divider />
          <MenuItem>
            <Typography variant="body2" color="text.secondary">
              No new notifications
            </Typography>
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
};

export default Navbar;
