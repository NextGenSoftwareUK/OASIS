/**
 * Plugin Service
 * Handles Plugin operations
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class PluginService extends BaseService {
  /**
   * Create new Plugin
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/Plugins/create', payload),
      { id: 'demo-plugin-1', name: payload.name || 'Demo Plugin', ...payload },
      'Plugin created successfully (Demo Mode)'
    );
  }

  /**
   * Update Plugin
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/Plugins/${id}`, payload),
      { id, ...payload, updatedOn: new Date().toISOString() },
      'Plugin updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Plugin
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/Plugins/${id}`),
      true,
      'Plugin deleted successfully (Demo Mode)'
    );
  }

  /**
   * Publish Plugin
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Plugins/${id}/publish`, payload),
      true,
      'Plugin published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Plugin
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Plugins/${id}/unpublish`),
      true,
      'Plugin unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Plugin
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Plugins/${id}/republish`, payload),
      true,
      'Plugin republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Plugin
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Plugins/${id}/activate`),
      true,
      'Plugin activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Plugin
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Plugins/${id}/deactivate`),
      true,
      'Plugin deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Plugin
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Plugins/${id}/download`, { destinationPath, overwrite }),
      { id, path: destinationPath, downloadedOn: new Date().toISOString() },
      'Plugin downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Plugin
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Plugins/${id}/install`),
      { id, isInstalled: true, installedOn: new Date().toISOString() },
      'Plugin installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Plugin
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Plugins/${id}/uninstall`),
      true,
      'Plugin uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Plugin
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Plugins/${id}/clone`, { newName }),
      { id: `cloned-${id}`, name: newName, clonedFrom: id, clonedOn: new Date().toISOString() },
      'Plugin cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Plugin versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Plugins/${id}/versions`),
      [{ version: '1.0.0', isActive: true, releasedOn: '2024-01-15' }],
      'Plugin versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Plugin version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Plugins/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Plugin version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Plugins
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Plugins/search', { params: { searchTerm } }),
      [
        { id: 'demo-1', name: 'Demo Plugin 1', type: 'Extension', version: '1.0.0' },
        { id: 'demo-2', name: 'Demo Plugin 2', type: 'Mod', version: '1.0.0' },
      ],
      'Plugin search completed (Demo Mode)'
    );
  }

  /**
   * Search Plugins (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/Plugins/search', { searchTerm }),
      [
        { id: 'demo-1', name: 'Demo Plugin 1', type: 'Extension', version: '1.0.0' },
        { id: 'demo-2', name: 'Demo Plugin 2', type: 'Mod', version: '1.0.0' },
      ],
      'Plugin search completed (Demo Mode)'
    );
  }

  /**
   * Get all Plugins
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Plugins'),
      [
        { id: 'demo-1', name: 'Demo Plugin 1', type: 'Extension', version: '1.0.0' },
        { id: 'demo-2', name: 'Demo Plugin 2', type: 'Mod', version: '1.0.0' },
      ],
      'All Plugins retrieved (Demo Mode)'
    );
  }

  /**
   * Get Plugin by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Plugins/${id}`),
      { id, name: 'Demo Plugin', type: 'Extension', version: '1.0.0' },
      'Plugin retrieved (Demo Mode)'
    );
  }
}

export const pluginService = new PluginService();
