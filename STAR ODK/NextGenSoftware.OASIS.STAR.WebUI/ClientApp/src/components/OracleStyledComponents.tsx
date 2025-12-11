import React from 'react';
import { Card as MuiCard, Button as MuiButton, CardProps, ButtonProps, styled } from '@mui/material';

// Oracle-themed Card with glassmorphism
export const OracleCard = styled(MuiCard)(({ theme }) => ({
  background: 'rgba(6, 11, 26, 0.7)',
  backdropFilter: 'blur(12px)',
  border: '1px solid var(--card-border)',
  boxShadow: '0 15px 30px rgba(15,118,110,0.18)',
  borderRadius: '16px',
  transition: 'all 0.3s ease',
  '&:hover': {
    boxShadow: '0 20px 40px rgba(15,118,110,0.25)',
    transform: 'translateY(-2px)',
  },
}));

// Oracle-themed primary button
export const OraclePrimaryButton = styled(MuiButton)(({ theme }) => ({
  background: 'var(--accent)',
  color: '#041321',
  fontWeight: 600,
  padding: '10px 24px',
  borderRadius: '8px',
  boxShadow: '0 20px 40px rgba(34,211,238,0.25)',
  transition: 'all 0.2s ease',
  '&:hover': {
    background: '#38e0ff',
    boxShadow: '0 25px 50px rgba(34,211,238,0.35)',
    transform: 'translateY(-1px)',
  },
}));

// Oracle-themed outline button
export const OracleOutlineButton = styled(MuiButton)(({ theme }) => ({
  border: '1px solid var(--card-border)',
  color: 'var(--foreground)',
  borderRadius: '8px',
  transition: 'all 0.2s ease',
  '&:hover': {
    borderColor: 'var(--accent)',
    color: 'var(--accent)',
    background: 'var(--accent-soft)',
  },
}));

// Oracle-themed glass card with gradient
export const OracleGlassCard = styled(MuiCard)(({ theme }) => ({
  background: 'linear-gradient(135deg, rgba(8, 11, 26, 0.95) 0%, rgba(15, 23, 42, 0.85) 100%)',
  backdropFilter: 'blur(20px)',
  border: '1px solid var(--card-border)',
  boxShadow: '0 15px 30px rgba(15,118,110,0.18)',
  borderRadius: '20px',
  position: 'relative',
  overflow: 'hidden',
  '&::before': {
    content: '""',
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    height: '1px',
    background: 'linear-gradient(90deg, transparent, var(--accent), transparent)',
    opacity: 0.5,
  },
}));

// Oracle-themed stats card
export const OracleStatsCard = styled(MuiCard)(({ theme }) => ({
  background: 'rgba(15, 118, 110, 0.15)',
  backdropFilter: 'blur(12px)',
  border: '1px solid var(--color-positive)',
  borderLeft: '4px solid var(--accent)',
  boxShadow: '0 10px 25px rgba(15,118,110,0.12)',
  borderRadius: '12px',
  padding: '20px',
  transition: 'all 0.3s ease',
  '&:hover': {
    borderLeftColor: 'var(--accent)',
    boxShadow: '0 15px 35px rgba(15,118,110,0.2)',
  },
}));

// Glowing accent border
export const GlowBorder: React.FC<{ children: React.ReactNode; color?: string }> = ({ 
  children, 
  color = 'var(--accent)' 
}) => (
  <div style={{
    padding: '1px',
    background: `linear-gradient(120deg, ${color}, rgba(191, 219, 254, 0.15), rgba(59, 130, 246, 0.65))`,
    borderRadius: '16px',
  }}>
    <div style={{
      background: 'var(--background)',
      borderRadius: '15px',
    }}>
      {children}
    </div>
  </div>
);

// Shimmer loading effect
export const ShimmerBox = styled('div')({
  background: 'linear-gradient(90deg, transparent, rgba(34, 211, 238, 0.3), transparent)',
  backgroundSize: '200% 100%',
  animation: 'shimmer 2s infinite',
  '@keyframes shimmer': {
    '0%': { backgroundPosition: '-200% 0' },
    '100%': { backgroundPosition: '200% 0' },
  },
});


