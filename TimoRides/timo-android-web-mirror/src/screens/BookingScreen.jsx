import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  Box,
  Typography,
  Card,
  CardContent,
  LinearProgress,
  Button,
  Stepper,
  Step,
  StepLabel,
  Avatar,
  Chip,
  IconButton,
  Divider,
} from '@mui/material'
import {
  ArrowBack as ArrowBackIcon,
  LocationOn as LocationOnIcon,
  Phone as PhoneIcon,
  Message as MessageIcon,
  Cancel as CancelIcon,
  CheckCircle as CheckCircleIcon,
  AccessTime as AccessTimeIcon,
} from '@mui/icons-material'

const BookingScreen = () => {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const driverId = searchParams.get('driver')
  const [bookingStatus, setBookingStatus] = useState('pending') // pending, accepted, in-progress, completed

  const steps = ['Requested', 'Accepted', 'In Progress', 'Completed']
  const currentStep = bookingStatus === 'pending' ? 0 : bookingStatus === 'accepted' ? 1 : bookingStatus === 'in-progress' ? 2 : 3

  const driver = {
    id: driverId || 1,
    name: 'John M.',
    vehicle: 'Toyota Camry • Silver',
    rating: 4.9,
    phone: '+27 82 123 4567',
  }

  useEffect(() => {
    // Simulate status updates
    const timer = setTimeout(() => {
      if (bookingStatus === 'pending') {
        setBookingStatus('accepted')
      } else if (bookingStatus === 'accepted') {
        setBookingStatus('in-progress')
      }
    }, 3000)

    return () => clearTimeout(timer)
  }, [bookingStatus])

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
          Booking Status
        </Typography>
      </Box>

      <Box sx={{ p: 3 }}>
        {/* Status Card */}
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
              <Avatar sx={{ width: 64, height: 64 }} />
              <Box sx={{ flex: 1 }}>
                <Typography variant="h6" fontWeight={600}>
                  {driver.name}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {driver.vehicle}
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mt: 0.5 }}>
                  <Typography variant="body2">⭐ {driver.rating}</Typography>
                </Box>
              </Box>
            </Box>

            <Divider sx={{ my: 2 }} />

            {/* Booking Details */}
            <Box sx={{ mb: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                <LocationOnIcon color="primary" fontSize="small" />
                <Typography variant="body2" fontWeight={600}>
                  Pickup
                </Typography>
              </Box>
              <Typography variant="body2" color="text.secondary" sx={{ ml: 4, mb: 2 }}>
                Current Location, Durban
              </Typography>

              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <LocationOnIcon color="secondary" fontSize="small" />
                <Typography variant="body2" fontWeight={600}>
                  Destination
                </Typography>
              </Box>
              <Typography variant="body2" color="text.secondary" sx={{ ml: 4 }}>
                Umhlanga Beach, Durban
              </Typography>
            </Box>

            <Divider sx={{ my: 2 }} />

            {/* Status Stepper */}
            <Stepper activeStep={currentStep} alternativeLabel sx={{ mb: 3 }}>
              {steps.map((label) => (
                <Step key={label}>
                  <StepLabel>{label}</StepLabel>
                </Step>
              ))}
            </Stepper>

            {/* Status Message */}
            <Box
              sx={{
                p: 2,
                background: 'linear-gradient(135deg, #3d5ed9 0%, #2847bc 100%)',
                borderRadius: 2,
                mb: 2,
                display: 'flex',
                alignItems: 'center',
                gap: 1,
                boxShadow: '0 4px 20px rgba(40, 71, 188, 0.4), 0 0 30px rgba(40, 71, 188, 0.2)',
                position: 'relative',
                overflow: 'hidden',
                '&::before': {
                  content: '""',
                  position: 'absolute',
                  top: 0,
                  left: '-100%',
                  width: '100%',
                  height: '100%',
                  background: 'linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent)',
                  animation: 'shimmer 2s infinite',
                },
              }}
            >
              <AccessTimeIcon 
                sx={{ 
                  color: 'white',
                  filter: 'drop-shadow(0 0 8px rgba(254, 217, 2, 0.6))',
                  zIndex: 1,
                }} 
              />
              <Typography 
                variant="body2" 
                sx={{ 
                  color: 'white',
                  textShadow: '0 0 10px rgba(255, 255, 255, 0.3)',
                  zIndex: 1,
                }}
              >
                {bookingStatus === 'pending' && 'Driver will confirm your ride and notify you within 5 minutes.'}
                {bookingStatus === 'accepted' && 'Driver has accepted your ride request!'}
                {bookingStatus === 'in-progress' && 'Your driver is on the way!'}
              </Typography>
            </Box>

            {/* Action Buttons */}
            <Box sx={{ display: 'flex', gap: 2 }}>
              <Button
                variant="outlined"
                startIcon={<PhoneIcon />}
                fullWidth
                sx={{
                  borderWidth: '2px',
                  '&:hover': {
                    borderWidth: '2px',
                    boxShadow: '0 4px 20px rgba(40, 71, 188, 0.3)',
                    transform: 'translateY(-2px)',
                  },
                }}
              >
                Call Driver
              </Button>
              <Button
                variant="outlined"
                startIcon={<MessageIcon />}
                fullWidth
                sx={{
                  borderWidth: '2px',
                  '&:hover': {
                    borderWidth: '2px',
                    boxShadow: '0 4px 20px rgba(40, 71, 188, 0.3)',
                    transform: 'translateY(-2px)',
                  },
                }}
              >
                Message
              </Button>
            </Box>

            <Button
              variant="outlined"
              color="error"
              startIcon={<CancelIcon />}
              fullWidth
              sx={{
                mt: 2,
                borderWidth: '2px',
                '&:hover': {
                  borderWidth: '2px',
                  boxShadow: '0 4px 20px rgba(228, 3, 59, 0.3)',
                  transform: 'translateY(-2px)',
                },
              }}
            >
              Cancel Ride
            </Button>
          </CardContent>
        </Card>

        {/* Fare Estimate */}
        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
              Fare Estimate
            </Typography>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Base Fare
              </Typography>
              <Typography variant="body2">R150.00</Typography>
            </Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Distance (8.5 km)
              </Typography>
              <Typography variant="body2">R80.00</Typography>
            </Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Service Fee
              </Typography>
              <Typography variant="body2">R20.00</Typography>
            </Box>
            <Divider sx={{ my: 2 }} />
            <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
              <Typography variant="h6" fontWeight={700}>
                Total
              </Typography>
              <Typography 
                variant="h6" 
                fontWeight={700} 
                color="primary"
                sx={{
                  textShadow: '0 0 10px rgba(40, 71, 188, 0.4)',
                }}
              >
                R250.00
              </Typography>
            </Box>
          </CardContent>
        </Card>
      </Box>
    </Box>
  )
}

export default BookingScreen

