/**
 * STAR Core Service
 * Handles core STAR operations (ignite, extinguish, status, etc.)
 */

import { BaseService } from '../base/baseService';
import { starDemoData } from '../demo/starDemoData';
import { OASISResult, STARStatus, Avatar } from '../../types/star';

class STARCoreService extends BaseService {
  /**
   * Ignite STAR system
   */
  async igniteSTAR(): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/star/ignite'),
      starDemoData.ignite(),
      'STAR ignited successfully (Demo Mode)'
    );
  }

  /**
   * Extinguish STAR system
   */
  async extinguishStar(): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post('/star/extinguish'),
      true,
      'STAR extinguished successfully (Demo Mode)'
    );
  }

  /**
   * Get STAR system status
   */
  async getSTARStatus(): Promise<STARStatus> {
    const result = await this.handleRequest(
      () => this.starApi.get('/star/status'),
      starDemoData.status(),
      'STAR status retrieved (Demo Mode)'
    );
    return result.result || starDemoData.status();
  }

  /**
   * Beam in (authenticate) avatar
   */
  async beamIn(username: string, password: string): Promise<OASISResult<Avatar>> {
    return this.handleRequest(
      () => this.starApi.post('/star/beam-in', { username, password }),
      starDemoData.beamIn(username, password),
      'Avatar beamed in successfully (Demo Mode)'
    );
  }

  /**
   * Create new avatar
   */
  async createAvatar(avatarData: any): Promise<OASISResult<Avatar>> {
    return this.handleRequest(
      () => this.starApi.post('/star/create-avatar', avatarData),
      starDemoData.createAvatar(avatarData),
      'Avatar created successfully (Demo Mode)'
    );
  }

  /**
   * Light (initialize) avatar
   */
  async lightAvatar(avatarData: any): Promise<OASISResult<Avatar>> {
    return this.handleRequest(
      () => this.starApi.post('/star/light', avatarData),
      starDemoData.createAvatar(avatarData),
      'Avatar lit successfully (Demo Mode)'
    );
  }

  /**
   * Seed (create) avatar
   */
  async seedAvatar(avatarData: any): Promise<OASISResult<Avatar>> {
    return this.handleRequest(
      () => this.starApi.post('/star/seed', avatarData),
      starDemoData.createAvatar(avatarData),
      'Avatar seeded successfully (Demo Mode)'
    );
  }

  /**
   * Get dashboard data
   */
  async getDashboardData(): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get('/star/dashboard'),
      starDemoData.getDashboardData(),
      'Dashboard data retrieved (Demo Mode)'
    );
  }

  /**
   * Get dev portal stats
   */
  async getDevPortalStats(): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get('/star/dev-portal/stats'),
      starDemoData.getDevPortalStats(),
      'Dev portal stats retrieved (Demo Mode)'
    );
  }

  /**
   * Get dev portal resources
   */
  async getDevPortalResources(): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get('/star/dev-portal/resources'),
      starDemoData.getDevPortalResources(),
      'Dev portal resources retrieved (Demo Mode)'
    );
  }

  /**
   * Get karma leaderboard
   */
  async getKarmaLeaderboard(): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get('/star/karma/leaderboard'),
      starDemoData.getKarmaLeaderboard(),
      'Karma leaderboard retrieved (Demo Mode)'
    );
  }

  /**
   * Get OAPP karma data
   */
  async getOAPPKarmaData(oappId: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/star/karma/oapp/${oappId}`),
      starDemoData.getOAPPKarmaData(oappId),
      'OAPP karma data retrieved (Demo Mode)'
    );
  }

  /**
   * Get my data files
   */
  async getMyDataFiles(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/my-data-files'),
      starDemoData.getMyDataFiles(),
      'My data files retrieved (Demo Mode)'
    );
  }

  /**
   * Update settings
   */
  async updateSettings(settings: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put('/star/settings', settings),
      starDemoData.updateSettings(settings),
      'Settings updated (Demo Mode)'
    );
  }

  /**
   * Get settings
   */
  async getSettings(): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get('/star/settings'),
      // Fallback demo data mirrors the default structure used in SettingsPage
      {
        general: {
          theme: 'dark',
          language: 'en',
          timezone: 'UTC',
          autoSave: true,
          demoMode: true,
        },
        notifications: {
          emailNotifications: true,
          pushNotifications: true,
          soundEnabled: true,
          volume: 70,
        },
        performance: {
          cacheSize: 1024,
          maxConnections: 10,
          autoOptimize: true,
          compressionLevel: 5,
        },
        security: {
          twoFactorAuth: false,
          sessionTimeout: 30,
          logLevel: 'info',
          encryptionEnabled: true,
        },
        oasiss: {
          defaultProvider: 'Auto',
          backupEnabled: true,
          syncInterval: 300,
          maxRetries: 3,
          enabledProviders: ['Auto', 'MongoDBOASIS', 'IPFSOASIS', 'EthereumOASIS'],
          autoReplication: true,
          replicationProviders: ['Auto', 'IPFSOASIS', 'PinataOASIS'],
        },
        stats: {
          enableCaching: true,
          cacheTtlSeconds: 300,
        },
      },
      'Settings retrieved (Demo Mode)'
    );
  }

  /**
   * Get store items
   */
  async getStoreItems(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/store-items'),
      starDemoData.getStoreItems(),
      'Store items retrieved (Demo Mode)'
    );
  }
}

export const starCoreService = new STARCoreService();
