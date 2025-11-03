/**
 * Configuration helpers for OASIS Web5 STAR API Client
 */

import { STARConfig } from './types';

export const DEFAULT_CONFIG: STARConfig = {
  apiUrl: 'http://localhost:50564/api',
  timeout: 30000,
  debug: false,
  autoRetry: true,
  maxRetries: 3
};

export function createConfig(userConfig?: Partial<STARConfig>): STARConfig {
  return {
    ...DEFAULT_CONFIG,
    ...userConfig
  };
}
