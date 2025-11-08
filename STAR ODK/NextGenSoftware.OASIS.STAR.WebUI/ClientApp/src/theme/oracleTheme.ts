import { createTheme, Theme } from '@mui/material/styles';

// Oracle-themed Material-UI configuration
export const oracleTheme: Theme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#22d3ee', // Cyan accent
      light: '#38e0ff',
      dark: '#0e7490',
      contrastText: '#041321',
    },
    secondary: {
      main: '#818cf8', // Purple accent
      light: '#a5b4fc',
      dark: '#4f46e5',
    },
    background: {
      default: '#050510',
      paper: 'rgba(6, 11, 26, 0.7)',
    },
    text: {
      primary: '#e2f4ff',
      secondary: 'rgba(148, 163, 184, 0.75)',
    },
    success: {
      main: 'rgba(34, 197, 94, 0.85)',
    },
    error: {
      main: 'rgba(239, 68, 68, 0.8)',
    },
    warning: {
      main: 'rgba(250, 204, 21, 0.85)',
    },
    info: {
      main: '#22d3ee',
    },
    divider: 'rgba(56, 189, 248, 0.2)',
  },
  typography: {
    fontFamily: '"Roboto", "Segoe UI", -apple-system, BlinkMacSystemFont, sans-serif',
    h1: {
      fontWeight: 700,
      color: '#e2f4ff',
    },
    h2: {
      fontWeight: 700,
      color: '#e2f4ff',
    },
    h3: {
      fontWeight: 600,
      color: '#e2f4ff',
    },
    h4: {
      fontWeight: 600,
      color: '#e2f4ff',
    },
    h5: {
      fontWeight: 600,
      color: '#e2f4ff',
    },
    h6: {
      fontWeight: 600,
      color: '#e2f4ff',
    },
  },
  components: {
    MuiCard: {
      styleOverrides: {
        root: {
          background: 'rgba(6, 11, 26, 0.7)',
          backdropFilter: 'blur(12px)',
          border: '1px solid var(--card-border)',
          boxShadow: '0 15px 30px rgba(15,118,110,0.18)',
          borderRadius: '16px',
          transition: 'all 0.3s ease',
          '&:hover': {
            boxShadow: '0 20px 40px rgba(15,118,110,0.25)',
          },
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: '8px',
          textTransform: 'none',
          fontWeight: 600,
          transition: 'all 0.2s ease',
        },
        contained: {
          background: 'var(--accent)',
          color: '#041321',
          boxShadow: '0 10px 20px rgba(34,211,238,0.2)',
          '&:hover': {
            background: '#38e0ff',
            boxShadow: '0 15px 30px rgba(34,211,238,0.3)',
            transform: 'translateY(-1px)',
          },
        },
        outlined: {
          borderColor: 'var(--card-border)',
          color: 'var(--foreground)',
          '&:hover': {
            borderColor: 'var(--accent)',
            color: 'var(--accent)',
            background: 'var(--accent-soft)',
          },
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: '8px',
          fontWeight: 500,
          border: '1px solid var(--card-border)',
        },
        filled: {
          background: 'var(--accent-soft)',
          color: 'var(--accent)',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          background: 'rgba(6, 11, 26, 0.7)',
          backdropFilter: 'blur(12px)',
          border: '1px solid var(--card-border)',
        },
      },
    },
    MuiTextField: {
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            color: 'var(--foreground)',
            '& fieldset': {
              borderColor: 'var(--card-border)',
            },
            '&:hover fieldset': {
              borderColor: 'var(--accent)',
            },
            '&.Mui-focused fieldset': {
              borderColor: 'var(--accent)',
            },
          },
          '& .MuiInputLabel-root': {
            color: 'var(--muted)',
          },
        },
      },
    },
    MuiSelect: {
      styleOverrides: {
        root: {
          color: 'var(--foreground)',
          '& .MuiOutlinedInput-notchedOutline': {
            borderColor: 'var(--card-border)',
          },
          '&:hover .MuiOutlinedInput-notchedOutline': {
            borderColor: 'var(--accent)',
          },
          '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
            borderColor: 'var(--accent)',
          },
        },
      },
    },
    MuiDialog: {
      styleOverrides: {
        paper: {
          background: 'rgba(8, 11, 26, 0.95)',
          backdropFilter: 'blur(20px)',
          border: '1px solid var(--card-border)',
          boxShadow: '0 25px 50px rgba(0,0,0,0.5)',
        },
      },
    },
    MuiDivider: {
      styleOverrides: {
        root: {
          borderColor: 'var(--card-border)',
        },
      },
    },
  },
});

