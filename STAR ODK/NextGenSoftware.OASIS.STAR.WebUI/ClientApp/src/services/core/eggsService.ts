/**
 * Eggs Service
 * Discover, hatch, and manage eggs for avatars
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class EggsService extends BaseService {
  /** Discover eggs near a location or for an avatar */
  async discover(locationId: string): Promise<OASISResult<any>> {
    return this.handleArrayRequest(
      // Using POST per controller signature
      () => this.web4Api.post(`/eggs/discover`, locationId),
      [],
      'Discover egg (Demo Mode)'
    );
  }

  /** Hatch an egg */
  async hatch(eggId: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post(`/eggs/hatch/${eggId}`),
      { id: eggId, status: 'hatched', rewards: [] },
      'Egg hatched (Demo Mode)'
    );
  }

  /** Get avatar egg gallery */
  async getGallery(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get(`/eggs/my-eggs`),
      [],
      'Egg gallery (Demo Mode)'
    );
  }
}

export const eggsService = new EggsService();


