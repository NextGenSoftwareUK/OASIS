/**
 * Configuration helpers for OASIS Web4 API Client
 */

import { OASISConfig } from './types';

export const DEFAULT_CONFIG: OASISConfig = {
  apiUrl: 'http://localhost:5000/api',
  timeout: 30000,
  debug: false,
  autoRetry: true,
  maxRetries: 3
};

export function createConfig(userConfig?: Partial<OASISConfig>): OASISConfig {
  return {
    ...DEFAULT_CONFIG,
    ...userConfig
  };
}
