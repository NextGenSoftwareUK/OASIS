import { createTheme } from '@mui/material/styles'

// TimoRides Brand Colors
const timoColors = {
  blue: {
    primary: '#2847bc',
    dark: '#1534aa',
    light: '#3d5ed9',
  },
  yellow: {
    primary: '#fed902',
    dark: '#fab700',
  },
  gray: {
    50: '#FAFAFA',
    100: '#F5F5F5',
    200: '#EEEEEE',
    300: '#E0E0E0',
    400: '#BDBDBD',
    500: '#9E9E9E',
    600: '#757575',
    700: '#616161',
    800: '#424242',
    900: '#212121',
  },
  success: '#4ACC12',
  error: '#e4033b',
  warning: '#FFA726',
  info: '#42A5F5',
}

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: timoColors.blue.primary,
      dark: timoColors.blue.dark,
      light: timoColors.blue.light,
      contrastText: '#FFFFFF',
    },
    secondary: {
      main: timoColors.yellow.primary,
      dark: timoColors.yellow.dark,
      contrastText: '#000000',
    },
    success: {
      main: timoColors.success,
    },
    error: {
      main: timoColors.error,
    },
    warning: {
      main: timoColors.warning,
    },
    info: {
      main: timoColors.info,
    },
    background: {
      default: timoColors.gray[50],
      paper: '#FFFFFF',
    },
    text: {
      primary: timoColors.gray[900],
      secondary: timoColors.gray[600],
      disabled: timoColors.gray[400],
    },
    grey: timoColors.gray,
  },
  typography: {
    fontFamily: [
      '-apple-system',
      'BlinkMacSystemFont',
      '"Segoe UI"',
      'Roboto',
      '"Helvetica Neue"',
      'Arial',
      'sans-serif',
    ].join(','),
    h1: {
      fontSize: '32px',
      fontWeight: 700,
      lineHeight: 1.2,
    },
    h2: {
      fontSize: '28px',
      fontWeight: 700,
      lineHeight: 1.2,
    },
    h3: {
      fontSize: '24px',
      fontWeight: 600,
      lineHeight: 1.3,
    },
    h4: {
      fontSize: '22px',
      fontWeight: 600,
      lineHeight: 1.3,
    },
    h5: {
      fontSize: '18px',
      fontWeight: 600,
      lineHeight: 1.4,
    },
    h6: {
      fontSize: '16px',
      fontWeight: 600,
      lineHeight: 1.4,
    },
    body1: {
      fontSize: '16px',
      lineHeight: 1.5,
    },
    body2: {
      fontSize: '14px',
      lineHeight: 1.5,
    },
    button: {
      textTransform: 'none',
      fontWeight: 500,
      fontSize: '14px',
    },
  },
  shape: {
    borderRadius: 16,
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 28,
          padding: '12px 24px',
          minHeight: 56,
          fontSize: '14px',
          fontWeight: 500,
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
            background: 'linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.3), transparent)',
            transition: 'left 0.5s',
          },
          '&:hover::before': {
            left: '100%',
          },
        },
        contained: {
          boxShadow: '0 4px 20px rgba(40, 71, 188, 0.4), 0 0 20px rgba(40, 71, 188, 0.2)',
          background: 'linear-gradient(135deg, #2847bc 0%, #3d5ed9 50%, #2847bc 100%)',
          backgroundSize: '200% 200%',
          animation: 'gradientShift 3s ease infinite',
          '&:hover': {
            boxShadow: '0 6px 30px rgba(40, 71, 188, 0.6), 0 0 30px rgba(40, 71, 188, 0.4)',
            transform: 'translateY(-2px)',
            backgroundPosition: 'right center',
          },
          '&:active': {
            transform: 'translateY(0px)',
            boxShadow: '0 2px 15px rgba(40, 71, 188, 0.5)',
          },
        },
        outlined: {
          borderWidth: '2px',
          '&:hover': {
            borderWidth: '2px',
            boxShadow: '0 4px 20px rgba(40, 71, 188, 0.3)',
            transform: 'translateY(-2px)',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 16,
          boxShadow: '0 4px 20px rgba(0, 0, 0, 0.1), 0 0 0 1px rgba(255, 255, 255, 0.05)',
          background: 'rgba(255, 255, 255, 0.95)',
          backdropFilter: 'blur(10px)',
          border: '1px solid rgba(255, 255, 255, 0.2)',
          transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
          '&:hover': {
            boxShadow: '0 8px 30px rgba(40, 71, 188, 0.15), 0 0 0 1px rgba(40, 71, 188, 0.1)',
            transform: 'translateY(-4px)',
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
              '& .MuiOutlinedInput-notchedOutline': {
                borderColor: 'rgba(40, 71, 188, 0.5)',
                borderWidth: '2px',
              },
            },
            '&.Mui-focused': {
              '& .MuiOutlinedInput-notchedOutline': {
                borderColor: '#2847bc',
                borderWidth: '2px',
                boxShadow: '0 0 0 3px rgba(40, 71, 188, 0.1)',
              },
            },
          },
        },
      },
    },
    MuiBottomSheet: {
      styleOverrides: {
        root: {
          borderRadius: '24px 24px 0 0',
        },
      },
    },
  },
})

export default theme

