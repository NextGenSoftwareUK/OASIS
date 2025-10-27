/**
 * Avatar Service
 * Handles avatar management operations
 */

import { BaseService } from '../base/baseService';
import { starDemoData } from '../demo/starDemoData';
import { OASISResult, Avatar, Karma } from '../../types/star';

class AvatarService extends BaseService {
  /**
   * Get current authenticated avatar
   */
  async getCurrentAvatar(): Promise<OASISResult<Avatar>> {
    return this.handleRequest(
      () => this.web4Api.get('/avatar/get-current-avatar'),
      starDemoData.getCurrentAvatar(),
      'Current avatar retrieved (Demo Mode)'
    );
  }

  /**
   * Get all avatars
   */
  async getAllAvatars(): Promise<OASISResult<Avatar[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/avatar/load-all-avatars'),
      starDemoData.getAllAvatars(),
      'All avatars retrieved (Demo Mode)'
    );
  }

  /**
   * Get all avatars (alias for getAllAvatars)
   */
  async getAll(): Promise<OASISResult<Avatar[]>> {
    return this.getAllAvatars();
  }

  /**
   * Get beamed in avatar
   */
  async getBeamedInAvatar(): Promise<OASISResult<Avatar>> {
    return this.handleRequest(
      () => this.starApi.get('/star/beamed-in-avatar'),
      starDemoData.getCurrentAvatar(),
      'Beamed in avatar retrieved (Demo Mode)'
    );
  }

  /**
   * Get all karma data
   */
  async getAllKarma(): Promise<OASISResult<Karma[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/karma'),
      starDemoData.getAllKarma(),
      'All karma data retrieved (Demo Mode)'
    );
  }

  /**
   * Get karma for specific avatar
   */
  async getKarmaForAvatar(avatarId: string): Promise<OASISResult<Karma>> {
    return this.handleRequest(
      () => this.starApi.get(`/star/karma/${avatarId}`),
      starDemoData.getKarmaForAvatar(avatarId),
      'Avatar karma retrieved (Demo Mode)'
    );
  }

  /**
   * Get all NFTs for avatar
   */
  async getAllNFTs(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/nfts'),
      [
        {
          id: 'demo-nft-1',
          name: 'Demo NFT',
          description: 'A demo NFT',
          imageUrl: 'https://demo.com/nft.png',
          tokenId: '1',
          owner: 'demo-avatar-1',
        },
      ],
      'All NFTs retrieved (Demo Mode)'
    );
  }

  /**
   * Get all GeoNFTs for avatar
   */
  async getAllGeoNFTs(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/geonfts'),
      [
        {
          id: 'demo-geo-nft-1',
          name: 'Demo GeoNFT',
          description: 'A demo GeoNFT',
          latitude: 40.7128,
          longitude: -74.0060,
          imageUrl: 'https://demo.com/geo.png',
          tokenId: '1',
          owner: 'demo-avatar-1',
        },
      ],
      'All GeoNFTs retrieved (Demo Mode)'
    );
  }

  /**
   * Get all missions for avatar
   */
  async getAllMissions(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/missions'),
      [
        {
          id: 'demo-mission-1',
          name: 'Demo Mission',
          description: 'A demo mission',
          type: 'Quest',
          difficulty: 'Medium',
          isActive: true,
        },
      ],
      'All missions retrieved (Demo Mode)'
    );
  }

  /**
   * Get all inventory items for avatar
   */
  async getAllInventoryItems(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/inventory'),
      [
        {
          id: 'demo-inventory-1',
          name: 'Demo Item',
          description: 'A demo inventory item',
          type: 'Weapon',
          rarity: 'Common',
          value: 100,
        },
      ],
      'All inventory items retrieved (Demo Mode)'
    );
  }

  /**
   * Get all celestial bodies for avatar
   */
  async getAllCelestialBodies(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/celestial-bodies'),
      [
        {
          id: 'demo-celestial-1',
          name: 'Demo Planet',
          description: 'A demo celestial body',
          type: 'Planet',
          size: 'Large',
        },
      ],
      'All celestial bodies retrieved (Demo Mode)'
    );
  }

  /**
   * Get all celestial spaces for avatar
   */
  async getAllCelestialSpaces(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/celestial-spaces'),
      [
        {
          id: 'demo-space-1',
          name: 'Demo Space',
          description: 'A demo celestial space',
          type: 'Galaxy',
          size: 'Massive',
        },
      ],
      'All celestial spaces retrieved (Demo Mode)'
    );
  }

  /**
   * Get all chapters for avatar
   */
  async getAllChapters(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/chapters'),
      [
        {
          id: 'demo-chapter-1',
          name: 'Demo Chapter',
          description: 'A demo chapter',
          questId: 'demo-quest-1',
          order: 1,
        },
      ],
      'All chapters retrieved (Demo Mode)'
    );
  }

  /**
   * Get all geo hot spots for avatar
   */
  async getAllGeoHotSpots(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/geo-hotspots'),
      [
        {
          id: 'demo-hotspot-1',
          name: 'Demo Hotspot',
          description: 'A demo geo hotspot',
          latitude: 40.7128,
          longitude: -74.0060,
          type: 'Event',
        },
      ],
      'All geo hot spots retrieved (Demo Mode)'
    );
  }

  /**
   * Get all OAPPs for avatar
   */
  async getAllOAPPs(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/star/oapps'),
      [
        {
          id: 'demo-oapp-1',
          name: 'Demo OAPP',
          description: 'A demo OASIS Application',
          type: 'Game',
          version: '1.0.0',
          isInstalled: true,
        },
      ],
      'All OAPPs retrieved (Demo Mode)'
    );
  }

  /**
   * Get avatar by ID
   */
  async getById(id: string): Promise<OASISResult<Avatar>> {
    return this.handleRequest(
      () => this.web4Api.get(`/avatar/load-avatar/${id}`),
      starDemoData.getAvatarById(id),
      'Avatar retrieved (Demo Mode)'
    );
  }

  /**
   * Create avatar
   */
  async create(avatarData: any): Promise<OASISResult<Avatar>> {
    return this.handleRequest(
      () => this.web4Api.post('/avatar/save-avatar', avatarData),
      starDemoData.createAvatar(avatarData),
      'Avatar created (Demo Mode)'
    );
  }

  /**
   * Update avatar
   */
  async update(id: string, avatarData: any): Promise<OASISResult<Avatar>> {
    return this.handleRequest(
      () => this.web4Api.put(`/avatar/save-avatar/${id}`, avatarData),
      starDemoData.updateAvatar(id, avatarData),
      'Avatar updated (Demo Mode)'
    );
  }

  /**
   * Delete avatar
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.web4Api.delete(`/avatar/delete-avatar/${id}`),
      true,
      'Avatar deleted (Demo Mode)'
    );
  }

  /**
   * Get avatar sessions
   */
  async getSessions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get(`/avatar/sessions/${id}`),
      starDemoData.getAvatarSessions(id),
      'Avatar sessions retrieved (Demo Mode)'
    );
  }

  /**
   * Logout avatar sessions
   */
  async logoutSessions(id: string, sessionIds: string[]): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.web4Api.post(`/avatar/logout-sessions/${id}`, { sessionIds }),
      true,
      'Sessions logged out (Demo Mode)'
    );
  }
}

export const avatarService = new AvatarService();
