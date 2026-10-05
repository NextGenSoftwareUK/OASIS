/**
 * Holon Service
 * Handles Holon operations
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class HolonService extends BaseService {
  /**
   * Create new Holon
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/Holons/create', payload),
      { id: 'demo-holon-1', name: payload.name || 'Demo Holon', ...payload },
      'Holon created successfully (Demo Mode)'
    );
  }

  /**
   * Update Holon
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/Holons/${id}`, payload),
      { id, ...payload, updatedOn: new Date().toISOString() },
      'Holon updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Holon
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/Holons/${id}`),
      true,
      'Holon deleted successfully (Demo Mode)'
    );
  }

  /**
   * Publish Holon
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Holons/${id}/publish`, payload),
      true,
      'Holon published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Holon
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Holons/${id}/unpublish`),
      true,
      'Holon unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Holon
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Holons/${id}/republish`, payload),
      true,
      'Holon republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Holon
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Holons/${id}/activate`),
      true,
      'Holon activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Holon
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Holons/${id}/deactivate`),
      true,
      'Holon deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Holon
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Holons/${id}/download`, { destinationPath, overwrite }),
      { id, path: destinationPath, downloadedOn: new Date().toISOString() },
      'Holon downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Holon
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Holons/${id}/install`),
      { id, isInstalled: true, installedOn: new Date().toISOString() },
      'Holon installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Holon
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Holons/${id}/uninstall`),
      true,
      'Holon uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Holon
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Holons/${id}/clone`, { newName }),
      { id: `cloned-${id}`, name: newName, clonedFrom: id, clonedOn: new Date().toISOString() },
      'Holon cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Holon versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Holons/${id}/versions`),
      [{ version: '1.0.0', isActive: true, releasedOn: '2024-01-15' }],
      'Holon versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Holon version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Holons/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Holon version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Holons
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Holons/search', { params: { searchTerm } }),
      [
        { id: 'demo-1', name: 'Demo Holon 1', type: 'Data', version: '1.0.0' },
        { id: 'demo-2', name: 'Demo Holon 2', type: 'Logic', version: '1.0.0' },
      ],
      'Holon search completed (Demo Mode)'
    );
  }

  /**
   * Search Holons (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/Holons/search', { searchTerm }),
      [
        { id: 'demo-1', name: 'Demo Holon 1', type: 'Data', version: '1.0.0' },
        { id: 'demo-2', name: 'Demo Holon 2', type: 'Logic', version: '1.0.0' },
      ],
      'Holon search completed (Demo Mode)'
    );
  }

  /**
   * Get all Holons
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Holons'),
      [
        { id: 'demo-1', name: 'Demo Holon 1', type: 'Data', version: '1.0.0' },
        { id: 'demo-2', name: 'Demo Holon 2', type: 'Logic', version: '1.0.0' },
      ],
      'All Holons retrieved (Demo Mode)'
    );
  }

  /**
   * Get Holon by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Holons/${id}`),
      { id, name: 'Demo Holon', type: 'Data', version: '1.0.0' },
      'Holon retrieved (Demo Mode)'
    );
  }

  /**
   * Get installed Holons
   */
  async getInstalled(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Holons/installed'),
      [
        { id: 'installed-1', name: 'Installed Holon 1', type: 'Data', version: '1.0.0', isInstalled: true },
        { id: 'installed-2', name: 'Installed Holon 2', type: 'Logic', version: '1.0.0', isInstalled: true },
      ],
      'Installed Holons retrieved (Demo Mode)'
    );
  }

  /**
   * Get Holons for avatar
   */
  async getForAvatar(avatarId: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Holons/avatar/${avatarId}`),
      [
        { id: 'avatar-1', name: 'Avatar Holon 1', type: 'Data', version: '1.0.0', avatarId },
        { id: 'avatar-2', name: 'Avatar Holon 2', type: 'Logic', version: '1.0.0', avatarId },
      ],
      'Avatar Holons retrieved (Demo Mode)'
    );
  }
}

export const holonService = new HolonService();
