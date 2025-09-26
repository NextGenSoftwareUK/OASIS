import React from 'react';
import {
  Box,
  CircularProgress,
  Typography,
  Fade,
} from '@mui/material';
import { motion } from 'framer-motion';

interface LoadingSpinnerProps {
  message?: string;
  size?: number;
  fullScreen?: boolean;
}

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ 
  message = 'Loading...', 
  size = 40,
  fullScreen = false 
}) => {
  const content = (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        ...(fullScreen && {
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          bgcolor: 'background.default',
          zIndex: 9999,
        }),
      }}
    >
      <motion.div
        animate={{ rotate: 360 }}
        transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
      >
        <CircularProgress
          size={size}
          thickness={4}
          sx={{
            color: 'primary.main',
            '& .MuiCircularProgress-circle': {
              strokeLinecap: 'round',
            },
          }}
        />
      </motion.div>
      
      <Fade in timeout={1000}>
        <Typography
          variant="body1"
          color="text.secondary"
          sx={{
            fontWeight: 300,
            textAlign: 'center',
          }}
        >
          {message}
        </Typography>
      </Fade>
    </Box>
  );

  return content;
};

export default LoadingSpinner;
