/**
 * Centralized API Configuration
 * Single source of truth for all API endpoints and settings
 * Supports both Vite (import.meta.env) and CRA (process.env)
 */

import { ENV } from '../../config/env';

export const API_CONFIG = {
  // STAR Web API (WEB5)
  STAR_API_URL: ENV.STAR_API_URL,
  
  // WEB4 OASIS API (ONODE)
  WEB4_API_URL: ENV.WEB4_API_URL,
  
  // OASIS Hub
  HUB_URL: ENV.HUB_URL,
  
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
