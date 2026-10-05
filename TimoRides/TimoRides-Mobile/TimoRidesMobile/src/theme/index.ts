import { configureFonts, MD3LightTheme } from 'react-native-paper';
import TimoColors from './colors';

// Custom font configuration for Material Design 3
const fontConfig = {
  displayLarge: {
    fontFamily: 'System',
    fontSize: 32,
    fontWeight: '700' as const,
    letterSpacing: 0,
    lineHeight: 40,
  },
  displayMedium: {
    fontFamily: 'System',
    fontSize: 28,
    fontWeight: '700' as const,
    letterSpacing: 0,
    lineHeight: 36,
  },
  displaySmall: {
    fontFamily: 'System',
    fontSize: 24,
    fontWeight: '600' as const,
    letterSpacing: 0,
    lineHeight: 32,
  },
  headlineLarge: {
    fontFamily: 'System',
    fontSize: 22,
    fontWeight: '600' as const,
    letterSpacing: 0,
    lineHeight: 32,
  },
  headlineMedium: {
    fontFamily: 'System',
    fontSize: 20,
    fontWeight: '600' as const,
    letterSpacing: 0,
    lineHeight: 28,
  },
  headlineSmall: {
    fontFamily: 'System',
    fontSize: 18,
    fontWeight: '600' as const,
    letterSpacing: 0,
    lineHeight: 24,
  },
  titleLarge: {
    fontFamily: 'System',
    fontSize: 16,
    fontWeight: '600' as const,
    letterSpacing: 0,
    lineHeight: 24,
  },
  titleMedium: {
    fontFamily: 'System',
    fontSize: 14,
    fontWeight: '500' as const,
    letterSpacing: 0.15,
    lineHeight: 20,
  },
  titleSmall: {
    fontFamily: 'System',
    fontSize: 12,
    fontWeight: '500' as const,
    letterSpacing: 0.1,
    lineHeight: 16,
  },
  bodyLarge: {
    fontFamily: 'System',
    fontSize: 16,
    fontWeight: '400' as const,
    letterSpacing: 0.5,
    lineHeight: 24,
  },
  bodyMedium: {
    fontFamily: 'System',
    fontSize: 14,
    fontWeight: '400' as const,
    letterSpacing: 0.25,
    lineHeight: 20,
  },
  bodySmall: {
    fontFamily: 'System',
    fontSize: 12,
    fontWeight: '400' as const,
    letterSpacing: 0.4,
    lineHeight: 16,
  },
  labelLarge: {
    fontFamily: 'System',
    fontSize: 14,
    fontWeight: '500' as const,
    letterSpacing: 0.1,
    lineHeight: 20,
  },
  labelMedium: {
    fontFamily: 'System',
    fontSize: 12,
    fontWeight: '500' as const,
    letterSpacing: 0.5,
    lineHeight: 16,
  },
  labelSmall: {
    fontFamily: 'System',
    fontSize: 11,
    fontWeight: '500' as const,
    letterSpacing: 0.5,
    lineHeight: 16,
  },
};

export const theme = {
  ...MD3LightTheme,
  colors: {
    ...MD3LightTheme.colors,
    primary: TimoColors.blue.primary,
    primaryContainer: TimoColors.blue.light,
    secondary: TimoColors.yellow.primary,
    secondaryContainer: TimoColors.yellow.dark,
    tertiary: TimoColors.blue.dark,
    error: TimoColors.error,
    errorContainer: '#FFEBEE',
    surface: TimoColors.white,
    surfaceVariant: TimoColors.gray[100],
    background: TimoColors.gray[50],
    onPrimary: TimoColors.white,
    onSecondary: TimoColors.black,
    onTertiary: TimoColors.white,
    onError: TimoColors.white,
    onSurface: TimoColors.gray[900],
    onSurfaceVariant: TimoColors.gray[700],
    onBackground: TimoColors.gray[900],
    outline: TimoColors.gray[400],
    shadow: TimoColors.black,
    success: TimoColors.success,
    warning: TimoColors.warning,
    info: TimoColors.info,
  },
  fonts: configureFonts({ config: fontConfig }),
};

export default theme;


