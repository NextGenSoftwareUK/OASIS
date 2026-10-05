import { Button } from '@mui/material'
import { styled } from '@mui/material/styles'

const FuturisticButton = styled(Button)(({ theme, glowcolor = '#2847bc' }) => ({
  position: 'relative',
  overflow: 'hidden',
  borderRadius: '28px',
  padding: '12px 24px',
  minHeight: '56px',
  fontSize: '14px',
  fontWeight: 500,
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  background: `linear-gradient(135deg, ${glowcolor} 0%, ${theme.palette.primary.light || glowcolor} 50%, ${glowcolor} 100%)`,
  backgroundSize: '200% 200%',
  animation: 'gradientShift 3s ease infinite',
  boxShadow: `0 4px 20px ${glowcolor}40, 0 0 20px ${glowcolor}30`,
  '&::before': {
    content: '""',
    position: 'absolute',
    top: 0,
    left: '-100%',
    width: '100%',
    height: '100%',
    background: 'linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.3), transparent)',
    transition: 'left 0.5s',
  },
  '&:hover': {
    boxShadow: `0 6px 30px ${glowcolor}60, 0 0 30px ${glowcolor}50`,
    transform: 'translateY(-2px)',
    backgroundPosition: 'right center',
    '&::before': {
      left: '100%',
    },
  },
  '&:active': {
    transform: 'translateY(0px)',
    boxShadow: `0 2px 15px ${glowcolor}50`,
  },
  '@keyframes gradientShift': {
    '0%': {
      backgroundPosition: '0% 50%',
    },
    '50%': {
      backgroundPosition: '100% 50%',
    },
    '100%': {
      backgroundPosition: '0% 50%',
    },
  },
}))

export default FuturisticButton

