import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box, CircularProgress, Typography } from '@mui/material'
import { motion } from 'framer-motion'

const SplashScreen = () => {
  const navigate = useNavigate()
  const [showProgress, setShowProgress] = useState(false)

  useEffect(() => {
    // Show progress after logo animation
    setTimeout(() => setShowProgress(true), 800)
    
    // Navigate to onboarding after 3 seconds
    const timer = setTimeout(() => {
      navigate('/onboarding')
    }, 3000)

    return () => clearTimeout(timer)
  }, [navigate])

  return (
    <Box
      sx={{
        width: '100%',
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'primary.main',
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      {/* Logo Animation */}
      <motion.div
        initial={{ scale: 0, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        transition={{
          duration: 0.8,
          type: 'spring',
          stiffness: 200,
        }}
      >
        <Box
          component="img"
          src="/timo-logo.png"
          alt="TimoRides Logo"
          sx={{
            width: { xs: 120, sm: 150 },
            height: { xs: 120, sm: 150 },
            objectFit: 'contain',
          }}
        />
      </motion.div>

      {/* App Name */}
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.4, duration: 0.6 }}
      >
        <Typography
          variant="h2"
          sx={{
            color: 'white',
            fontWeight: 700,
            mt: 3,
            textAlign: 'center',
            textShadow: '0 0 20px rgba(254, 217, 2, 0.5), 0 0 40px rgba(254, 217, 2, 0.3), 0 0 60px rgba(254, 217, 2, 0.2)',
            animation: 'neonGlow 2s ease-in-out infinite alternate',
          }}
        >
          TimoRides
        </Typography>
      </motion.div>

      {/* Progress Indicator */}
      {showProgress && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.3 }}
        >
          <CircularProgress
            size={48}
            thickness={4}
            sx={{
              color: 'secondary.main',
              mt: 4,
              filter: 'drop-shadow(0 0 10px rgba(254, 217, 2, 0.6))',
              '& .MuiCircularProgress-circle': {
                strokeLinecap: 'round',
              },
            }}
          />
        </motion.div>
      )}

      {/* City Building Background (optional) */}
      <Box
        sx={{
          position: 'absolute',
          bottom: 0,
          left: 0,
          right: 0,
          height: '200px',
          background: 'linear-gradient(to top, rgba(21, 52, 170, 0.3), transparent)',
          pointerEvents: 'none',
        }}
      />
    </Box>
  )
}

export default SplashScreen

