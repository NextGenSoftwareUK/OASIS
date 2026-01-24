/**
 * License Validation Module
 * 
 * Validates subscription licenses for the OASIS Unified MCP Server.
 * Supports online validation with offline grace period.
 */

import axios, { AxiosInstance } from 'axios';
import crypto from 'crypto';
import os from 'os';
import fs from 'fs';
import path from 'path';

export interface LicenseValidationResult {
  valid: boolean;
  tier?: string;
  expiresAt?: Date;
  message?: string;
  offlineMode?: boolean;
}

export interface LicenseConfig {
  licenseKey: string;
  licenseServerUrl: string;
  deviceId: string;
  cacheFile?: string;
  gracePeriodDays?: number;
}

export class LicenseValidator {
  private config: LicenseConfig;
  private httpClient: AxiosInstance;
  private cacheFile: string;

  constructor(config: LicenseConfig) {
    this.config = {
      ...config,
      cacheFile: config.cacheFile || this.getDefaultCachePath(),
      gracePeriodDays: config.gracePeriodDays || 7,
    };
    
    this.cacheFile = this.config.cacheFile!;
    
    this.httpClient = axios.create({
      baseURL: config.licenseServerUrl,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  /**
   * Get default cache file path for storing license validation cache
   */
  private getDefaultCachePath(): string {
    const homeDir = os.homedir();
    const cacheDir = path.join(homeDir, '.oasis-mcp');
    
    // Ensure cache directory exists
    if (!fs.existsSync(cacheDir)) {
      fs.mkdirSync(cacheDir, { recursive: true });
    }
    
    return path.join(cacheDir, 'license-cache.json');
  }

  /**
   * Generate device fingerprint for license activation
   */
  static generateDeviceFingerprint(): string {
    const components = [
      os.hostname(),
      os.platform(),
      os.arch(),
      os.cpus().length.toString(),
      os.totalmem().toString(),
    ];

    // Try to get MAC address (platform-specific)
    try {
      const networkInterfaces = os.networkInterfaces();
      const macAddresses = Object.values(networkInterfaces)
        .flat()
        .filter(iface => iface && !iface.internal)
        .map(iface => iface!.mac)
        .filter(Boolean);
      
      if (macAddresses.length > 0) {
        components.push(macAddresses[0]);
      }
    } catch (error) {
      // Ignore errors getting MAC address
    }

    const fingerprint = crypto
      .createHash('sha256')
      .update(components.join('|'))
      .digest('hex')
      .substring(0, 32); // Use first 32 chars for readability

    return fingerprint;
  }

  /**
   * Load cached license validation result
   */
  private loadCache(): LicenseValidationResult | null {
    try {
      if (!fs.existsSync(this.cacheFile)) {
        return null;
      }

      const cacheData = JSON.parse(fs.readFileSync(this.cacheFile, 'utf-8'));
      const cachedAt = new Date(cacheData.cachedAt);
      const now = new Date();
      const daysSinceCache = (now.getTime() - cachedAt.getTime()) / (1000 * 60 * 60 * 24);

      // Use cache if less than grace period days old
      if (daysSinceCache < this.config.gracePeriodDays!) {
        return {
          ...cacheData.result,
          offlineMode: true,
        };
      }

      return null;
    } catch (error) {
      return null;
    }
  }

  /**
   * Save license validation result to cache
   */
  private saveCache(result: LicenseValidationResult): void {
    try {
      const cacheData = {
        cachedAt: new Date().toISOString(),
        result: {
          valid: result.valid,
          tier: result.tier,
          expiresAt: result.expiresAt?.toISOString(),
        },
      };

      fs.writeFileSync(this.cacheFile, JSON.stringify(cacheData, null, 2));
    } catch (error) {
      // Ignore cache write errors
      console.error('[License] Failed to save cache:', error);
    }
  }

  /**
   * Validate license online
   */
  private async validateOnline(): Promise<LicenseValidationResult> {
    try {
      const response = await this.httpClient.post('/api/licenses/validate', {
        licenseKey: this.config.licenseKey,
        deviceId: this.config.deviceId,
      });

      const result: LicenseValidationResult = {
        valid: response.data.valid === true,
        tier: response.data.tier,
        expiresAt: response.data.expiresAt ? new Date(response.data.expiresAt) : undefined,
        message: response.data.message,
        offlineMode: false,
      };

      // Cache successful validations
      if (result.valid) {
        this.saveCache(result);
      }

      return result;
    } catch (error: any) {
      // Network error - try cache
      const cached = this.loadCache();
      if (cached) {
        return cached;
      }

      // No cache available
      return {
        valid: false,
        message: `License validation failed: ${error.message}. Using cached validation if available.`,
        offlineMode: true,
      };
    }
  }

  /**
   * Activate license on this device
   */
  async activate(): Promise<{ success: boolean; message: string }> {
    try {
      const response = await this.httpClient.post('/api/licenses/activate', {
        licenseKey: this.config.licenseKey,
        deviceId: this.config.deviceId,
        deviceInfo: {
          platform: os.platform(),
          arch: os.arch(),
          hostname: os.hostname(),
        },
      });

      return {
        success: response.data.success === true,
        message: response.data.message || 'License activated successfully',
      };
    } catch (error: any) {
      return {
        success: false,
        message: `Activation failed: ${error.message}`,
      };
    }
  }

  /**
   * Validate license (main entry point)
   */
  async validate(): Promise<LicenseValidationResult> {
    // Check for license key - if not provided, allow free tier
    if (!this.config.licenseKey || this.config.licenseKey.trim() === '') {
      return {
        valid: true,
        tier: 'free',
        message: 'Running in free mode. Get a license at https://www.oasisweb4.com/products/mcp.html',
      };
    }

    // Try online validation first
    const onlineResult = await this.validateOnline();
    
    // If online validation succeeded, return it
    if (onlineResult.valid && !onlineResult.offlineMode) {
      return onlineResult;
    }

    // If online validation failed but we have cache, use cache
    if (onlineResult.offlineMode && onlineResult.valid) {
      return onlineResult;
    }

    // If no valid cache and online failed, return error
    return {
      valid: false,
      message: onlineResult.message || 'License validation failed. Please check your internet connection and license key.',
    };
  }

  /**
   * Check if license is valid (synchronous check using cache)
   */
  isValidSync(): boolean {
    const cached = this.loadCache();
    return cached?.valid === true;
  }

  /**
   * Get license tier
   */
  getTier(): string | undefined {
    const cached = this.loadCache();
    return cached?.tier;
  }
}

/**
 * Create license validator instance from environment variables
 */
export function createLicenseValidator(): LicenseValidator {
  const licenseKey = process.env.OASIS_MCP_LICENSE_KEY || '';
  const licenseServerUrl = process.env.LICENSE_SERVER_URL || 'https://licenses.oasis.com';
  const deviceId = LicenseValidator.generateDeviceFingerprint();

  return new LicenseValidator({
    licenseKey,
    licenseServerUrl,
    deviceId,
  });
}
