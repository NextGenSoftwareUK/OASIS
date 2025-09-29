import React from 'react';
import {
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Divider,
  Box,
  Typography,
  Collapse,
  IconButton,
} from '@mui/material';
import {
  Dashboard,
  Apps,
  Assignment,
  Image,
  LocationOn,
  FlightTakeoff,
  MenuBook,
  AccountCircle,
  Public,
  SpaceDashboard,
  Memory,
  LibraryBooks,
  Extension,
  Inventory,
  LocalFireDepartment,
  Store,
  Settings,
  ExpandLess,
  ExpandMore,
  Star,
  EmojiEvents,
  CloudUpload,
} from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';
import { motion } from 'framer-motion';

interface SidebarProps {
  open: boolean;
  onClose: () => void;
  isConnected: boolean;
}

const drawerWidth = 240;

const Sidebar: React.FC<SidebarProps> = ({ open, onClose, isConnected }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const [expandedItems, setExpandedItems] = React.useState<string[]>(['star']);

  const menuItems = [
    {
      id: 'dashboard',
      title: 'Dashboard',
      icon: <Dashboard />,
      path: '/dashboard',
    },
    {
      id: 'star',
      title: 'STAR System',
      icon: <Star />,
      children: [
        {
          id: 'oapps',
          title: 'OAPPs',
          icon: <Apps />,
          path: '/oapps',
          description: 'Omniverse Applications',
        },
        {
          id: 'quests',
          title: 'Quests',
          icon: <Assignment />,
          path: '/quests',
          description: 'Interactive Quests',
        },
        {
          id: 'missions',
          title: 'Missions',
          icon: <FlightTakeoff />,
          path: '/missions',
          description: 'Mission Management',
        },
        {
          id: 'chapters',
          title: 'Chapters',
          icon: <MenuBook />,
          path: '/chapters',
          description: 'Story Chapters',
        },
      ],
    },
    {
      id: 'nfts',
      title: 'NFTs & Assets',
      icon: <Image />,
      children: [
        {
          id: 'nfts',
          title: 'NFTs',
          icon: <Image />,
          path: '/nfts',
          description: 'Digital Assets',
        },
        {
          id: 'geonfts',
          title: 'GeoNFTs',
          icon: <LocationOn />,
          path: '/geonfts',
          description: 'Location-based NFTs',
        },
        {
          id: 'geo-hotspots',
          title: 'Geo Hotspots',
          icon: <LocalFireDepartment />,
          path: '/geo-hotspots',
          description: 'Location Hotspots',
        },
        {
          id: 'inventory',
          title: 'Inventory',
          icon: <Inventory />,
          path: '/inventory',
          description: 'Item Management',
        },
      ],
    },
    {
      id: 'universe',
      title: 'Universe',
      icon: <Public />,
      children: [
        {
          id: 'celestial-bodies',
          title: 'Celestial Bodies',
          icon: <SpaceDashboard />,
          path: '/celestial-bodies',
          description: 'Planets, Stars, Moons',
        },
        {
          id: 'celestial-spaces',
          title: 'Celestial Spaces',
          icon: <Public />,
          path: '/celestial-spaces',
          description: 'Galaxies, Universes',
        },
      ],
    },
    {
      id: 'development',
      title: 'Development',
      icon: <Memory />,
      children: [
        {
          id: 'runtimes',
          title: 'Runtimes',
          icon: <Memory />,
          path: '/runtimes',
          description: 'Runtime Environments',
        },
        {
          id: 'libraries',
          title: 'Libraries',
          icon: <LibraryBooks />,
          path: '/libraries',
          description: 'Code Libraries',
        },
        {
          id: 'templates',
          title: 'Templates',
          icon: <Extension />,
          path: '/templates',
          description: 'OAPP Templates',
        },
        {
          id: 'plugins',
          title: 'Plugins',
          icon: <Extension />,
          path: '/plugins',
          description: 'Extensions & Plugins',
        },
      ],
    },
    {
      id: 'community',
      title: 'Community',
      icon: <Store />,
      children: [
        {
          id: 'starnet-store',
          title: 'STARNET Store',
          icon: <Store />,
          path: '/starnet-store',
          description: 'Asset Marketplace',
        },
        {
          id: 'avatars',
          title: 'Avatars',
          icon: <AccountCircle />,
          path: '/avatars',
          description: 'User Management',
        },
        {
          id: 'karma',
          title: 'Karma',
          icon: <EmojiEvents />,
          path: '/karma',
          description: 'Karma Leaderboard',
        },
        {
          id: 'my-data',
          title: 'My Data',
          icon: <CloudUpload />,
          path: '/my-data',
          description: 'OASIS Hyperdrive',
        },
      ],
    },
    {
      id: 'settings',
      title: 'Settings',
      icon: <Settings />,
      path: '/settings',
    },
  ];

  const handleItemClick = (item: any) => {
    if (item.children) {
      setExpandedItems(prev => 
        prev.includes(item.id) 
          ? prev.filter(id => id !== item.id)
          : [...prev, item.id]
      );
    } else if (item.path) {
      navigate(item.path);
      onClose();
    }
  };

  const isItemActive = (item: any) => {
    if (item.path) {
      return location.pathname === item.path;
    }
    if (item.children) {
      return item.children.some((child: any) => location.pathname === child.path);
    }
    return false;
  };

  const renderMenuItem = (item: any, level = 0) => {
    const hasChildren = item.children && item.children.length > 0;
    const isExpanded = expandedItems.includes(item.id);
    const isActive = isItemActive(item);

    return (
      <React.Fragment key={item.id}>
        <ListItem disablePadding>
          <ListItemButton
            onClick={() => handleItemClick(item)}
            sx={{
              pl: 2 + level * 2,
              bgcolor: isActive ? 'primary.main' : 'transparent',
              '&:hover': {
                bgcolor: isActive ? 'primary.dark' : 'action.hover',
              },
              opacity: !isConnected && item.id !== 'dashboard' && item.id !== 'settings' ? 0.5 : 1,
            }}
          >
            <ListItemIcon
              sx={{
                color: isActive ? 'primary.contrastText' : 'text.primary',
                minWidth: 40,
              }}
            >
              {item.icon}
            </ListItemIcon>
            <ListItemText
              primary={item.title}
              secondary={item.description}
              primaryTypographyProps={{
                fontSize: level > 0 ? '0.875rem' : '0.95rem',
                fontWeight: level > 0 ? 400 : 500,
                color: isActive ? 'primary.contrastText' : 'text.primary',
              }}
              secondaryTypographyProps={{
                fontSize: '0.75rem',
                color: isActive ? 'primary.contrastText' : 'text.secondary',
              }}
            />
            {hasChildren && (
              <IconButton size="small" sx={{ color: 'text.secondary' }}>
                {isExpanded ? <ExpandLess /> : <ExpandMore />}
              </IconButton>
            )}
          </ListItemButton>
        </ListItem>
        
        {hasChildren && (
          <Collapse in={isExpanded} timeout="auto" unmountOnExit>
            <List component="div" disablePadding>
              {item.children.map((child: any) => renderMenuItem(child, level + 1))}
            </List>
          </Collapse>
        )}
      </React.Fragment>
    );
  };

  const drawerContent = (
    <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Box sx={{ p: 2, borderBottom: '1px solid rgba(255, 255, 255, 0.1)' }}>
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
        >
          <Typography 
            variant="h6" 
            sx={{ 
              fontWeight: 300,
              background: 'linear-gradient(45deg, #00bcd4, #ff4081)',
              backgroundClip: 'text',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
            }}
          >
            STARNET Navigation
          </Typography>
          <Typography variant="caption" color="text.secondary">
            OASIS Omniverse Interface
          </Typography>
        </motion.div>
      </Box>

      <Box sx={{ flexGrow: 1, overflow: 'auto' }}>
        <List>
          {menuItems.map((item) => renderMenuItem(item))}
        </List>
      </Box>

      <Box sx={{ p: 2, borderTop: '1px solid rgba(255, 255, 255, 0.1)' }}>
        <Typography variant="caption" color="text.secondary" align="center" display="block">
          STARNET v1.0.0
        </Typography>
        <Typography variant="caption" color="text.secondary" align="center" display="block">
          {isConnected ? 'Connected to STAR' : 'Disconnected'}
        </Typography>
      </Box>
    </Box>
  );

  return (
    <Drawer
      variant="temporary"
      open={open}
      onClose={onClose}
      ModalProps={{
        keepMounted: true, // Better open performance on mobile
      }}
      sx={{
        display: { xs: 'block', sm: 'block' },
        '& .MuiDrawer-paper': {
          boxSizing: 'border-box',
          width: drawerWidth,
          bgcolor: 'background.paper',
          borderRight: '1px solid rgba(255, 255, 255, 0.1)',
          backgroundImage: 'none',
        },
      }}
    >
      {drawerContent}
    </Drawer>
  );
};

export default Sidebar;
