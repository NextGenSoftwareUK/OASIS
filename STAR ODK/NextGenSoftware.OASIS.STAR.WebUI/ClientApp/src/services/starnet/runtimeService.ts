/**
 * Runtime Service
 * Handles Runtime operations
 */

import { BaseService } from '../base/baseService';
import { starnetDemoData } from '../demo/starnetDemoData';
import { OASISResult } from '../../types/star';

class RuntimeService extends BaseService {
  /**
   * Create new Runtime
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/Runtimes/create', payload),
      starnetDemoData.runtime.create(payload),
      'Runtime created successfully (Demo Mode)'
    );
  }

  /**
   * Update Runtime
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/Runtimes/${id}`, payload),
      starnetDemoData.runtime.update(id, payload),
      'Runtime updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Runtime
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/Runtimes/${id}`),
      true,
      'Runtime deleted successfully (Demo Mode)'
    );
  }

  /**
   * Publish Runtime
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Runtimes/${id}/publish`, payload),
      true,
      'Runtime published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Runtime
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Runtimes/${id}/unpublish`),
      true,
      'Runtime unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Runtime
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Runtimes/${id}/republish`, payload),
      true,
      'Runtime republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Runtime
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Runtimes/${id}/activate`),
      true,
      'Runtime activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Runtime
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Runtimes/${id}/deactivate`),
      true,
      'Runtime deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Runtime
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Runtimes/${id}/download`, { destinationPath, overwrite }),
      starnetDemoData.runtime.download(id, destinationPath),
      'Runtime downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Runtime
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Runtimes/${id}/install`),
      starnetDemoData.runtime.install(id),
      'Runtime installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Runtime
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Runtimes/${id}/uninstall`),
      true,
      'Runtime uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Runtime
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Runtimes/${id}/clone`, { newName }),
      starnetDemoData.runtime.clone(id, newName),
      'Runtime cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Runtime versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Runtimes/${id}/versions`),
      starnetDemoData.runtime.getVersions(id),
      'Runtime versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Runtime version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Runtimes/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Runtime version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Runtimes
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Runtimes/search', { params: { searchTerm } }),
      starnetDemoData.runtime.search(searchTerm),
      'Runtime search completed (Demo Mode)'
    );
  }

  /**
   * Search Runtimes (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/Runtimes/search', { searchTerm }),
      starnetDemoData.runtime.search(searchTerm),
      'Runtime search completed (Demo Mode)'
    );
  }

  /**
   * Add Runtime dependency
   */
  async addDependency(id: string, dependencyId: string, dependencyType: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Runtimes/${id}/add-dependency`, { dependencyId, dependencyType }),
      true,
      'Runtime dependency added successfully (Demo Mode)'
    );
  }

  /**
   * Remove Runtime dependency
   */
  async removeDependency(id: string, dependencyId: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Runtimes/${id}/remove-dependency`, { dependencyId }),
      true,
      'Runtime dependency removed successfully (Demo Mode)'
    );
  }

  /**
   * Get all Runtimes
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Runtimes'),
      [
        starnetDemoData.runtime.create({ name: 'Demo Runtime 1' }),
        starnetDemoData.runtime.create({ name: 'Demo Runtime 2' }),
      ],
      'All Runtimes retrieved (Demo Mode)'
    );
  }

  /**
   * Get Runtime by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Runtimes/${id}`),
      starnetDemoData.runtime.create({ id, name: 'Demo Runtime' }),
      'Runtime retrieved (Demo Mode)'
    );
  }

  /**
   * Get Runtimes for avatar
   */
  async getForAvatar(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Runtimes/avatar'),
      [
        starnetDemoData.runtime.create({ name: 'Avatar Runtime 1' }),
        starnetDemoData.runtime.create({ name: 'Avatar Runtime 2' }),
      ],
      'Avatar Runtimes retrieved (Demo Mode)'
    );
  }
}

export const runtimeService = new RuntimeService();
