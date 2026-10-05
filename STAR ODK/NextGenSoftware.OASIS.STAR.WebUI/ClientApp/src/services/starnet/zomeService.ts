/**
 * Zome Service
 * Handles Zome operations
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class ZomeService extends BaseService {
  /**
   * Create new Zome
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/Zomes/create', payload),
      { id: 'demo-zome-1', name: payload.name || 'Demo Zome', ...payload },
      'Zome created successfully (Demo Mode)'
    );
  }

  /**
   * Update Zome
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/Zomes/${id}`, payload),
      { id, ...payload, updatedOn: new Date().toISOString() },
      'Zome updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Zome
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/Zomes/${id}`),
      true,
      'Zome deleted successfully (Demo Mode)'
    );
  }

  /**
   * Publish Zome
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Zomes/${id}/publish`, payload),
      true,
      'Zome published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Zome
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Zomes/${id}/unpublish`),
      true,
      'Zome unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Zome
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Zomes/${id}/republish`, payload),
      true,
      'Zome republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Zome
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Zomes/${id}/activate`),
      true,
      'Zome activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Zome
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Zomes/${id}/deactivate`),
      true,
      'Zome deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Zome
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Zomes/${id}/download`, { destinationPath, overwrite }),
      { id, path: destinationPath, downloadedOn: new Date().toISOString() },
      'Zome downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Zome
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Zomes/${id}/install`),
      { id, isInstalled: true, installedOn: new Date().toISOString() },
      'Zome installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Zome
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Zomes/${id}/uninstall`),
      true,
      'Zome uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Zome
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Zomes/${id}/clone`, { newName }),
      { id: `cloned-${id}`, name: newName, clonedFrom: id, clonedOn: new Date().toISOString() },
      'Zome cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Zome versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Zomes/${id}/versions`),
      [{ version: '1.0.0', isActive: true, releasedOn: '2024-01-15' }],
      'Zome versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Zome version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Zomes/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Zome version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Zomes
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Zomes/search', { params: { searchTerm } }),
      [
        { id: 'demo-1', name: 'Demo Zome 1', type: 'Core', version: '1.0.0' },
        { id: 'demo-2', name: 'Demo Zome 2', type: 'Utility', version: '1.0.0' },
      ],
      'Zome search completed (Demo Mode)'
    );
  }

  /**
   * Search Zomes (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/Zomes/search', { searchTerm }),
      [
        { id: 'demo-1', name: 'Demo Zome 1', type: 'Core', version: '1.0.0' },
        { id: 'demo-2', name: 'Demo Zome 2', type: 'Utility', version: '1.0.0' },
      ],
      'Zome search completed (Demo Mode)'
    );
  }

  /**
   * Get all Zomes
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Zomes'),
      [
        { id: 'demo-1', name: 'Demo Zome 1', type: 'Core', version: '1.0.0' },
        { id: 'demo-2', name: 'Demo Zome 2', type: 'Utility', version: '1.0.0' },
      ],
      'All Zomes retrieved (Demo Mode)'
    );
  }

  /**
   * Get Zome by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Zomes/${id}`),
      { id, name: 'Demo Zome', type: 'Core', version: '1.0.0' },
      'Zome retrieved (Demo Mode)'
    );
  }

  /**
   * Get installed Zomes
   */
  async getInstalled(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Zomes/installed'),
      [
        { id: 'installed-1', name: 'Installed Zome 1', type: 'Core', version: '1.0.0', isInstalled: true },
        { id: 'installed-2', name: 'Installed Zome 2', type: 'Utility', version: '1.0.0', isInstalled: true },
      ],
      'Installed Zomes retrieved (Demo Mode)'
    );
  }

  /**
   * Get Zomes for avatar
   */
  async getForAvatar(avatarId: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Zomes/avatar/${avatarId}`),
      [
        { id: 'avatar-1', name: 'Avatar Zome 1', type: 'Core', version: '1.0.0', avatarId },
        { id: 'avatar-2', name: 'Avatar Zome 2', type: 'Utility', version: '1.0.0', avatarId },
      ],
      'Avatar Zomes retrieved (Demo Mode)'
    );
  }
}

export const zomeService = new ZomeService();
