/**
 * Centralized API Configuration
 * Single source of truth for all API endpoints and settings
 */

export const API_CONFIG = {
  // STAR Web API (WEB5)
  STAR_API_URL: process.env.REACT_APP_API_URL || 'http://localhost:5099/api',
  
  // WEB4 OASIS API (ONODE)
  WEB4_API_URL: process.env.REACT_APP_WEB4_API_URL || 'http://localhost:5000/api',
  
  // OASIS Hub
  HUB_URL: process.env.REACT_APP_HUB_URL || 'http://localhost:5001',
  
  // Request Configuration
  TIMEOUT: 30000,
  RETRY_ATTEMPTS: 3,
  
  // Headers
  HEADERS: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
  
  // Demo Mode
  DEFAULT_DEMO_MODE: true,
} as const;

export type ApiConfig = typeof API_CONFIG;
