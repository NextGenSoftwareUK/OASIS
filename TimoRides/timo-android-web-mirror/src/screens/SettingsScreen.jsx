import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
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
} from '@mui/material'
import {
  ArrowBack as ArrowBackIcon,
  Notifications as NotificationsIcon,
  LocationOn as LocationOnIcon,
  Language as LanguageIcon,
  Security as SecurityIcon,
  Help as HelpIcon,
  ContactSupport as ContactSupportIcon,
  Logout as LogoutIcon,
} from '@mui/icons-material'

const SettingsScreen = () => {
  const navigate = useNavigate()
  const [notifications, setNotifications] = useState(true)
  const [location, setLocation] = useState(true)

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: 'background.default' }}>
      {/* Header */}
      <Box
        sx={{
          backgroundColor: 'primary.main',
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
      <Card sx={{ m: 2, mb: 0 }}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Avatar sx={{ width: 64, height: 64 }} />
            <Box sx={{ flex: 1 }}>
              <Typography variant="h6" fontWeight={600}>
                Alvin Armstrong
              </Typography>
              <Typography variant="body2" color="text.secondary">
                alvin@example.com
              </Typography>
              <Typography variant="body2" color="text.secondary">
                +27 82 123 4567
              </Typography>
            </Box>
            <Button variant="outlined" size="small">
              Edit
            </Button>
          </Box>
        </CardContent>
      </Card>

      {/* Settings List */}
      <List sx={{ mt: 2 }}>
        <ListItem>
          <ListItemIcon>
            <NotificationsIcon color="primary" />
          </ListItemIcon>
          <ListItemText
            primary="Notifications"
            secondary="Push notifications for ride updates"
          />
          <Switch
            checked={notifications}
            onChange={(e) => setNotifications(e.target.checked)}
          />
        </ListItem>

        <ListItem>
          <ListItemIcon>
            <LocationOnIcon color="primary" />
          </ListItemIcon>
          <ListItemText
            primary="Location Services"
            secondary="Use GPS for better accuracy"
          />
          <Switch
            checked={location}
            onChange={(e) => setLocation(e.target.checked)}
          />
        </ListItem>

        <Divider sx={{ my: 1 }} />

        <ListItem button>
          <ListItemIcon>
            <LanguageIcon color="primary" />
          </ListItemIcon>
          <ListItemText
            primary="Language"
            secondary="English"
          />
        </ListItem>

        <ListItem button>
          <ListItemIcon>
            <SecurityIcon color="primary" />
          </ListItemIcon>
          <ListItemText
            primary="Security"
            secondary="Password, 2FA, Privacy"
          />
        </ListItem>

        <Divider sx={{ my: 1 }} />

        <ListItem button>
          <ListItemIcon>
            <HelpIcon color="primary" />
          </ListItemIcon>
          <ListItemText
            primary="Help & Support"
            secondary="FAQs, guides, contact"
          />
        </ListItem>

        <ListItem button>
          <ListItemIcon>
            <ContactSupportIcon color="primary" />
          </ListItemIcon>
          <ListItemText
            primary="Contact Us"
            secondary="Get in touch with our team"
          />
        </ListItem>

        <Divider sx={{ my: 1 }} />

        <ListItem
          button
          onClick={() => navigate('/login')}
          sx={{ color: 'error.main' }}
        >
          <ListItemIcon>
            <LogoutIcon color="error" />
          </ListItemIcon>
          <ListItemText
            primary="Logout"
            secondary="Sign out of your account"
          />
        </ListItem>
      </List>
    </Box>
  )
}

export default SettingsScreen

