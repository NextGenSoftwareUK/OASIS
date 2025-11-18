import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  Box,
  Typography,
  Card,
  Avatar,
  Chip,
  Button,
  IconButton,
  TextField,
  InputAdornment,
  Divider,
  Rating,
} from '@mui/material'
import {
  Star as StarIcon,
  ArrowBack as ArrowBackIcon,
  FilterList as FilterListIcon,
  Search as SearchIcon,
  Verified as VerifiedIcon,
  Wifi as WifiIcon,
  ChildCare as ChildCareIcon,
  Language as LanguageIcon,
} from '@mui/icons-material'

const DriverSelectionScreen = () => {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const selectedDriverId = searchParams.get('driver')
  const [searchQuery, setSearchQuery] = useState('')

  const drivers = [
    {
      id: 1,
      name: 'John M.',
      rating: 4.9,
      rides: 328,
      vehicle: 'Toyota Camry • Silver',
      features: ['WiFi', 'English'],
      price: 'R250',
      eta: '8 min',
      karma: 850,
      verified: true,
      languages: ['English', 'Zulu'],
      amenities: ['WiFi', 'AC'],
    },
    {
      id: 2,
      name: 'Sarah K.',
      rating: 4.8,
      rides: 245,
      vehicle: 'Honda Accord • Black',
      features: ['WiFi', 'Zulu', 'Child Seat'],
      price: 'R280',
      eta: '12 min',
      karma: 720,
      verified: true,
      languages: ['English', 'Zulu', 'Xhosa'],
      amenities: ['WiFi', 'Child Seat', 'Luggage Space'],
    },
    {
      id: 3,
      name: 'Mike T.',
      rating: 5.0,
      rides: 512,
      vehicle: 'BMW 3 Series • White',
      features: ['WiFi', 'English', 'Premium'],
      price: 'R350',
      eta: '5 min',
      karma: 1200,
      verified: true,
      languages: ['English'],
      amenities: ['WiFi', 'Premium Interior', 'Charging Port'],
    },
  ]

  const handleSelectDriver = (driverId) => {
    navigate(`/booking?driver=${driverId}`)
  }

  const filteredDrivers = drivers.filter((driver) =>
    driver.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    driver.vehicle.toLowerCase().includes(searchQuery.toLowerCase())
  )

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
          Select Your Driver
        </Typography>
        <IconButton sx={{ color: 'white' }}>
          <FilterListIcon />
        </IconButton>
      </Box>

      {/* Search Bar */}
      <Box sx={{ p: 2 }}>
        <TextField
          fullWidth
          placeholder="Search drivers or vehicles..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon 
                  sx={{
                    color: 'primary.main',
                    filter: 'drop-shadow(0 0 6px rgba(40, 71, 188, 0.5))',
                  }}
                />
              </InputAdornment>
            ),
          }}
          sx={{ 
            mb: 2,
            '& .MuiOutlinedInput-root': {
              background: 'rgba(255, 255, 255, 0.95)',
              backdropFilter: 'blur(10px)',
              boxShadow: '0 4px 20px rgba(0, 0, 0, 0.08)',
            },
          }}
        />
      </Box>

      {/* Driver List */}
      <Box sx={{ p: 2, pb: 10 }}>
        {filteredDrivers.map((driver) => (
          <Card
            key={driver.id}
            sx={{
              p: 2,
              mb: 2,
              cursor: 'pointer',
              background: 'rgba(255, 255, 255, 0.95)',
              backdropFilter: 'blur(10px)',
              border: selectedDriverId === String(driver.id) ? '2px solid' : '1px solid',
              borderColor: selectedDriverId === String(driver.id) 
                ? 'primary.main' 
                : 'rgba(40, 71, 188, 0.1)',
              boxShadow: selectedDriverId === String(driver.id)
                ? '0 8px 30px rgba(40, 71, 188, 0.4), 0 0 0 1px rgba(40, 71, 188, 0.3)'
                : '0 4px 20px rgba(0, 0, 0, 0.1)',
              transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
              position: 'relative',
              overflow: 'hidden',
              '&::before': {
                content: '""',
                position: 'absolute',
                top: 0,
                left: '-100%',
                width: '100%',
                height: '100%',
                background: 'linear-gradient(90deg, transparent, rgba(40, 71, 188, 0.1), transparent)',
                transition: 'left 0.5s',
              },
              '&:hover': {
                boxShadow: '0 8px 30px rgba(40, 71, 188, 0.3), 0 0 0 1px rgba(40, 71, 188, 0.2)',
                transform: 'translateY(-4px) scale(1.02)',
                borderColor: 'rgba(40, 71, 188, 0.4)',
                '&::before': {
                  left: '100%',
                },
              },
            }}
            onClick={() => handleSelectDriver(driver.id)}
          >
            <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
              <Avatar sx={{ width: 64, height: 64 }} />
              <Box sx={{ flex: 1 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                  <Typography variant="subtitle1" fontWeight={600}>
                    {driver.name}
                  </Typography>
                  {driver.verified && (
                    <VerifiedIcon sx={{ fontSize: 18, color: 'success.main' }} />
                  )}
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, ml: 'auto' }}>
                    <StarIcon sx={{ fontSize: 16, color: 'secondary.main' }} />
                    <Typography variant="body2" fontWeight={600}>
                      {driver.rating}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      ({driver.rides})
                    </Typography>
                  </Box>
                </Box>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  {driver.vehicle}
                </Typography>
                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 1 }}>
                  {driver.features.map((feature) => (
                    <Chip key={feature} label={feature} size="small" />
                  ))}
                </Box>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 1 }}>
                  <Typography variant="body2" color="text.secondary">
                    Karma: {driver.karma}
                  </Typography>
                  <Divider orientation="vertical" flexItem />
                  <Typography variant="body2" color="text.secondary">
                    {driver.languages.join(', ')}
                  </Typography>
                </Box>
              </Box>
              <Box sx={{ textAlign: 'right', minWidth: 100 }}>
                <Typography 
                  variant="h6" 
                  fontWeight={700} 
                  color="primary"
                  sx={{
                    textShadow: '0 0 10px rgba(40, 71, 188, 0.4)',
                  }}
                >
                  {driver.price}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  {driver.eta} away
                </Typography>
                <Button
                  variant="contained"
                  size="small"
                  fullWidth
                  onClick={(e) => {
                    e.stopPropagation()
                    handleSelectDriver(driver.id)
                  }}
                  sx={{
                    background: 'linear-gradient(135deg, #2847bc 0%, #3d5ed9 50%, #2847bc 100%)',
                    backgroundSize: '200% 200%',
                    boxShadow: '0 2px 15px rgba(40, 71, 188, 0.4), 0 0 15px rgba(40, 71, 188, 0.2)',
                    animation: 'gradientShift 3s ease infinite',
                    '&:hover': {
                      boxShadow: '0 4px 20px rgba(40, 71, 188, 0.6), 0 0 25px rgba(40, 71, 188, 0.4)',
                      transform: 'translateY(-1px)',
                    },
                  }}
                >
                  Select
                </Button>
              </Box>
            </Box>
          </Card>
        ))}
      </Box>
    </Box>
  )
}

export default DriverSelectionScreen

