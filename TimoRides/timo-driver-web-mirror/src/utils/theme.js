import { createTheme } from '@mui/material/styles';

// Timo Brand Colors
export const TimoColors = {
  primary: '#2847bc',
  primaryDark: '#1534aa',
  primaryLight: '#3d5ed9',
  accent: '#fed902',
  accentDark: '#fab700',
  online: '#4ACC12',
  offline: '#9E9E9E',
  error: '#e4033b',
  success: '#4ACC12',
  textMain: '#100f0f',
  textLight: '#999999',
  textMid: '#545f7d',
  background: '#FFFFFF',
  backgroundLight: '#f9f9ff',
};

// MUI Theme with Timo branding
export const theme = createTheme({
  palette: {
    primary: {
      main: TimoColors.primary,
      dark: TimoColors.primaryDark,
      light: TimoColors.primaryLight,
    },
    secondary: {
      main: TimoColors.accent,
      dark: TimoColors.accentDark,
    },
    error: {
      main: TimoColors.error,
    },
    success: {
      main: TimoColors.success,
    },
    background: {
      default: TimoColors.backgroundLight,
      paper: TimoColors.background,
    },
    text: {
      primary: TimoColors.textMain,
      secondary: TimoColors.textMid,
    },
  },
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    h1: {
      fontWeight: 700,
    },
    h2: {
      fontWeight: 700,
    },
    h3: {
      fontWeight: 600,
    },
    button: {
      textTransform: 'none',
      fontWeight: 600,
    },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          padding: '10px 24px',
          transition: 'all 0.3s ease',
          position: 'relative',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            top: 0,
            left: '-100%',
            width: '100%',
            height: '100%',
            background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.3), transparent)',
            transition: 'left 0.5s',
          },
          '&:hover::before': {
            left: '100%',
          },
        },
        contained: {
          background: `linear-gradient(135deg, ${TimoColors.primary} 0%, ${TimoColors.primaryLight} 100%)`,
          backgroundSize: '200% 200%',
          animation: 'gradientShift 3s ease infinite',
          boxShadow: `0 4px 20px rgba(40, 71, 188, 0.4), 0 0 20px rgba(40, 71, 188, 0.2)`,
          '&:hover': {
            boxShadow: `0 6px 30px rgba(40, 71, 188, 0.6), 0 0 30px rgba(40, 71, 188, 0.4)`,
            transform: 'translateY(-2px)',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 16,
          boxShadow: `0 4px 20px rgba(40, 71, 188, 0.15), 0 0 20px rgba(40, 71, 188, 0.1)`,
          background: 'rgba(255, 255, 255, 0.95)',
          backdropFilter: 'blur(10px)',
          border: '1px solid rgba(40, 71, 188, 0.1)',
          transition: 'all 0.3s ease',
          '&:hover': {
            boxShadow: `0 6px 30px rgba(40, 71, 188, 0.25), 0 0 30px rgba(40, 71, 188, 0.15)`,
            transform: 'translateY(-2px)',
          },
        },
      },
    },
    MuiTextField: {
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            borderRadius: 12,
            transition: 'all 0.3s ease',
            '&:hover': {
              boxShadow: `0 0 10px rgba(40, 71, 188, 0.2)`,
            },
            '&.Mui-focused': {
              boxShadow: `0 0 15px rgba(40, 71, 188, 0.3)`,
            },
          },
        },
      },
    },
  },
});

// CSS Animations
export const globalStyles = `
  @keyframes gradientShift {
    0% { background-position: 0% 50%; }
    50% { background-position: 100% 50%; }
    100% { background-position: 0% 50%; }
  }

  @keyframes pulseGlow {
    0%, 100% { 
      box-shadow: 0 4px 20px rgba(40, 71, 188, 0.4), 0 0 20px rgba(40, 71, 188, 0.2);
    }
    50% { 
      box-shadow: 0 6px 30px rgba(40, 71, 188, 0.6), 0 0 30px rgba(40, 71, 188, 0.4);
    }
  }

  .glass-effect {
    background: rgba(255, 255, 255, 0.1);
    backdrop-filter: blur(10px);
    -webkit-backdrop-filter: blur(10px);
    border: 1px solid rgba(255, 255, 255, 0.2);
  }
`;

