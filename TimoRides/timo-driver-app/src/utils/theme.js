import { MD3LightTheme, MD3DarkTheme } from 'react-native-paper';

// Timo Brand Colors (matching rider app)
export const TimoColors = {
  // Primary Colors
  primary: '#2847bc',
  primaryDark: '#1534aa',
  primaryLight: '#3d5ed9',
  
  // Accent Colors
  accent: '#fed902',
  accentDark: '#fab700',
  
  // Status Colors
  online: '#4ACC12',
  offline: '#9E9E9E',
  error: '#e4033b',
  success: '#4ACC12',
  warning: '#fed902',
  
  // Text Colors
  textMain: '#100f0f',
  textLight: '#999999',
  textMid: '#545f7d',
  
  // Background Colors
  background: '#FFFFFF',
  backgroundLight: '#f9f9ff',
  backgroundView: '#EEEDF1',
  
  // Other
  white: '#FFFFFF',
  black: '#000000',
  grey: '#BCBBC1',
  pink: '#F02346',
};

// Material Design 3 Theme
export const timoTheme = {
  ...MD3LightTheme,
  colors: {
    ...MD3LightTheme.colors,
    primary: TimoColors.primary,
    primaryContainer: TimoColors.primaryLight,
    secondary: TimoColors.accent,
    secondaryContainer: TimoColors.accentDark,
    tertiary: TimoColors.primaryDark,
    error: TimoColors.error,
    errorContainer: '#ffdad6',
    surface: TimoColors.background,
    surfaceVariant: TimoColors.backgroundLight,
    background: TimoColors.background,
    onPrimary: TimoColors.white,
    onSecondary: TimoColors.black,
    onSurface: TimoColors.textMain,
    onSurfaceVariant: TimoColors.textMid,
    outline: TimoColors.grey,
  },
};

// Dark theme variant (optional)
export const timoDarkTheme = {
  ...MD3DarkTheme,
  colors: {
    ...MD3DarkTheme.colors,
    primary: TimoColors.primary,
    primaryContainer: TimoColors.primaryDark,
    secondary: TimoColors.accent,
    error: TimoColors.error,
    surface: '#1a1a1a',
    background: '#121212',
    onPrimary: TimoColors.white,
    onSurface: TimoColors.white,
  },
};

// Typography
export const Typography = {
  h1: {
    fontSize: 32,
    fontWeight: 'bold',
    lineHeight: 40,
  },
  h2: {
    fontSize: 24,
    fontWeight: 'bold',
    lineHeight: 32,
  },
  h3: {
    fontSize: 20,
    fontWeight: '600',
    lineHeight: 28,
  },
  body: {
    fontSize: 16,
    fontWeight: '400',
    lineHeight: 24,
  },
  bodySmall: {
    fontSize: 14,
    fontWeight: '400',
    lineHeight: 20,
  },
  caption: {
    fontSize: 12,
    fontWeight: '400',
    lineHeight: 16,
  },
};

// Spacing (8dp grid system)
export const Spacing = {
  xs: 4,
  sm: 8,
  md: 16,
  lg: 24,
  xl: 32,
  xxl: 48,
};

// Border Radius
export const BorderRadius = {
  sm: 4,
  md: 8,
  lg: 12,
  xl: 16,
  round: 999,
};

// Shadow styles for glowing effects
export const GlowStyles = {
  primary: {
    shadowColor: TimoColors.primary,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 8,
  },
  accent: {
    shadowColor: TimoColors.accent,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.4,
    shadowRadius: 10,
    elevation: 10,
  },
  online: {
    shadowColor: TimoColors.online,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.5,
    shadowRadius: 6,
    elevation: 6,
  },
};

