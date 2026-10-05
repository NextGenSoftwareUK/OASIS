/**
 * Social Service
 * Feeds and sharing
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class SocialService extends BaseService {
  async getFeed(limit: number = 50): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get(`/social/social-feed`, { params: { limit } }),
      [],
      'Social feed (Demo Mode)'
    );
  }

  async shareHolon(holonId: string, message?: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post(`/social/share-holon`, { holonId, message }),
      { id: `share-${Date.now()}`, holonId, message, timestamp: new Date().toISOString() },
      'Shared (Demo Mode)'
    );
  }
}

export const socialService = new SocialService();


