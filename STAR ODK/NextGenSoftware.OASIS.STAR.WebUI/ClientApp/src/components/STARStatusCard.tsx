import React from 'react';
import { Box, Card, CardContent, Typography, Chip, LinearProgress } from '@mui/material';
import { motion } from 'framer-motion';
import { useSTARConnection } from '../hooks/useSTARConnection';

const STARStatusCard: React.FC = () => {
  const { starStatus, isConnected, isLoading, connectionStatus } = useSTARConnection();

  const getStatusColor = (status: boolean) => {
    return status ? '#4caf50' : '#f44336';
  };

  const getStatusText = (status: boolean) => {
    return status ? 'IGNITED' : 'EXTINGUISHED';
  };

  const getStatusIcon = (status: boolean) => {
    return status ? '🌟' : '💤';
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
    >
      <Card
        sx={{
          background: 'linear-gradient(145deg, #1a1a1a 0%, #2a2a2a 100%)',
          border: `2px solid ${getStatusColor(isConnected)}`,
          boxShadow: `0 0 20px ${getStatusColor(isConnected)}40`,
          transition: 'all 0.3s ease-in-out',
          '&:hover': {
            transform: 'translateY(-5px)',
            boxShadow: `0 0 30px ${getStatusColor(isConnected)}60`,
          },
        }}
      >
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="h6" component="div" sx={{ color: 'white' }}>
              {getStatusIcon(isConnected)} STAR Status
            </Typography>
            <Chip
              label={connectionStatus === 'connecting' ? 'CONNECTING...' : getStatusText(isConnected)}
              sx={{
                bgcolor: connectionStatus === 'connecting' ? '#ff9800' : getStatusColor(isConnected),
                color: 'white',
                fontWeight: 'bold',
                animation: connectionStatus === 'connecting' ? 'pulse 1s infinite' : 
                          isConnected ? 'pulse 2s infinite' : 'none',
              }}
            />
          </Box>

          {isLoading && (
            <Box sx={{ mb: 2 }}>
              <LinearProgress
                sx={{
                  bgcolor: 'rgba(255,255,255,0.1)',
                  '& .MuiLinearProgress-bar': {
                    bgcolor: getStatusColor(isConnected),
                  },
                }}
              />
            </Box>
          )}

          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            Connection Status: {
              connectionStatus === 'connecting' ? 'Connecting...' :
              connectionStatus === 'error' ? 'Error' :
              isConnected ? 'Connected' : 'Disconnected'
            }
          </Typography>

          {starStatus && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Last Updated: {new Date().toLocaleTimeString()}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                OASIS Booted: {starStatus.isIgnited ? 'Yes' : 'No'}
              </Typography>
            </Box>
          )}

          <Box sx={{ mt: 2, p: 2, bgcolor: 'rgba(255,255,255,0.05)', borderRadius: 1 }}>
            <Typography variant="caption" color="text.secondary">
              {connectionStatus === 'connecting' 
                ? 'STAR is igniting... Please wait while the system initializes.'
                : connectionStatus === 'error'
                ? 'STAR ignition failed. Please try again or check the system status.'
                : isConnected
                ? 'STAR is fully operational and ready for exploration!'
                : 'STAR is currently offline. Click "Ignite STAR" to begin your journey.'}
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </motion.div>
  );
};

export default STARStatusCard;
