import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  Typography,
  Stepper,
  Step,
  StepLabel,
  MobileStepper,
} from '@mui/material'
import { motion, AnimatePresence } from 'framer-motion'
import KeyboardArrowLeft from '@mui/icons-material/KeyboardArrowLeft'
import KeyboardArrowRight from '@mui/icons-material/KeyboardArrowRight'

const onboardingSlides = [
  {
    title: 'Book your premium ride',
    description: 'Choose your premium driver and get\npicked up within minutes',
    image: '/onboarding-1.svg', // Placeholder
  },
  {
    title: 'Track your ride',
    description: 'You can track taxi by app map\nanytime, anywhere',
    image: '/onboarding-2.svg',
  },
  {
    title: 'Safe and secure',
    description: 'Verified drivers with background checks\nand real-time tracking',
    image: '/onboarding-3.svg',
  },
]

const OnboardingScreen = () => {
  const navigate = useNavigate()
  const [activeStep, setActiveStep] = useState(0)

  const handleNext = () => {
    if (activeStep === onboardingSlides.length - 1) {
      navigate('/login')
    } else {
      setActiveStep((prev) => prev + 1)
    }
  }

  const handleBack = () => {
    setActiveStep((prev) => prev - 1)
  }

  return (
    <Box
      sx={{
        width: '100%',
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        backgroundColor: 'primary.main',
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      {/* Content Area */}
      <Box
        sx={{
          flex: 1,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          px: 3,
          py: 4,
        }}
      >
        <AnimatePresence mode="wait">
          <motion.div
            key={activeStep}
            initial={{ opacity: 0, x: 50 }}
            animate={{ opacity: 1, x: 0 }}
            exit={{ opacity: 0, x: -50 }}
            transition={{ duration: 0.3 }}
            style={{ width: '100%', textAlign: 'center' }}
          >
            {/* Illustration Placeholder */}
            <Box
              sx={{
                width: '100%',
                maxWidth: 300,
                height: 300,
                mx: 'auto',
                mb: 4,
                backgroundColor: 'rgba(255, 255, 255, 0.1)',
                borderRadius: 4,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Typography variant="h6" sx={{ color: 'white', opacity: 0.7 }}>
                Illustration {activeStep + 1}
              </Typography>
            </Box>

            {/* Title */}
            <Typography
              variant="h2"
              sx={{
                color: 'white',
                fontWeight: 700,
                mb: 2,
                textAlign: 'center',
                textShadow: '0 0 20px rgba(254, 217, 2, 0.4), 0 0 40px rgba(254, 217, 2, 0.2)',
              }}
            >
              {onboardingSlides[activeStep].title}
            </Typography>

            {/* Description */}
            <Typography
              variant="body1"
              sx={{
                color: 'white',
                opacity: 0.9,
                textAlign: 'center',
                whiteSpace: 'pre-line',
                lineHeight: 1.6,
              }}
            >
              {onboardingSlides[activeStep].description}
            </Typography>
          </motion.div>
        </AnimatePresence>
      </Box>

      {/* Bottom Section */}
      <Box
        sx={{
          pb: 4,
          px: 3,
        }}
      >
        {/* Page Indicator */}
        <Box sx={{ display: 'flex', justifyContent: 'center', mb: 3 }}>
          {onboardingSlides.map((_, index) => (
            <Box
              key={index}
              sx={{
                width: 8,
                height: 8,
                borderRadius: '50%',
                backgroundColor: index === activeStep ? 'secondary.main' : 'rgba(255, 255, 255, 0.3)',
                mx: 0.5,
                transition: 'all 0.3s ease',
              }}
            />
          ))}
        </Box>

        {/* Navigation */}
        <Box sx={{ display: 'flex', gap: 2 }}>
          {activeStep > 0 && (
            <Button
              variant="outlined"
              onClick={handleBack}
              startIcon={<KeyboardArrowLeft />}
              sx={{
                flex: 1,
                borderColor: 'white',
                color: 'white',
                '&:hover': {
                  borderColor: 'white',
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                },
              }}
            >
              Back
            </Button>
          )}
          <Button
            variant="contained"
            onClick={handleNext}
            endIcon={activeStep === onboardingSlides.length - 1 ? null : <KeyboardArrowRight />}
            sx={{
              flex: 1,
              background: 'linear-gradient(135deg, #fed902 0%, #fab700 50%, #fed902 100%)',
              backgroundSize: '200% 200%',
              color: 'black',
              boxShadow: '0 4px 20px rgba(254, 217, 2, 0.5), 0 0 30px rgba(254, 217, 2, 0.3)',
              animation: 'gradientShift 3s ease infinite',
              '&:hover': {
                boxShadow: '0 6px 30px rgba(254, 217, 2, 0.7), 0 0 40px rgba(254, 217, 2, 0.5)',
                transform: 'translateY(-2px)',
                backgroundPosition: 'right center',
              },
            }}
          >
            {activeStep === onboardingSlides.length - 1 ? 'Get Started' : 'Next'}
          </Button>
        </Box>
      </Box>
    </Box>
  )
}

export default OnboardingScreen

