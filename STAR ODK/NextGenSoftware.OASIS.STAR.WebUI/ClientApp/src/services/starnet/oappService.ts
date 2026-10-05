/**
 * OAPP Service
 * Handles OASIS Application operations
 */

import { BaseService } from '../base/baseService';
import { starnetDemoData } from '../demo/starnetDemoData';
import { OASISResult } from '../../types/star';

class OAPPService extends BaseService {
  /**
   * Create new OAPP
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/OAPPs/create', payload),
      starnetDemoData.oapp.create(payload),
      'OAPP created successfully (Demo Mode)'
    );
  }

  /**
   * Update OAPP
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/OAPPs/${id}`, payload),
      starnetDemoData.oapp.update(id, payload),
      'OAPP updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete OAPP
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/OAPPs/${id}`),
      true,
      'OAPP deleted successfully (Demo Mode)'
    );
  }

  /**
   * Publish OAPP
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/OAPPs/${id}/publish`, payload),
      true,
      'OAPP published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish OAPP
   */
  async unpublish(id: string, version: number = 0): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/OAPPs/${id}/unpublish`, null, { params: { version } }),
      true,
      'OAPP unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish OAPP
   */
  async republish(id: string, payload: any, version: number = 0): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/OAPPs/${id}/republish`, payload, { params: { version } }),
      true,
      'OAPP republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate OAPP
   */
  async activate(id: string, version: number = 0): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/OAPPs/${id}/activate`, null, { params: { version } }),
      true,
      'OAPP activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate OAPP
   */
  async deactivate(id: string, version: number = 0): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/OAPPs/${id}/deactivate`, null, { params: { version } }),
      true,
      'OAPP deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download OAPP
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/OAPPs/${id}/download`, { destinationPath, overwrite }),
      starnetDemoData.oapp.download(id, destinationPath),
      'OAPP downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install OAPP
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/OAPPs/${id}/install`),
      starnetDemoData.oapp.install(id),
      'OAPP installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall OAPP
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/OAPPs/${id}/uninstall`),
      true,
      'OAPP uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone OAPP
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/OAPPs/${id}/clone`, { newName }),
      starnetDemoData.oapp.clone(id, newName),
      'OAPP cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get OAPP versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/OAPPs/${id}/versions`),
      starnetDemoData.oapp.getVersions(id),
      'OAPP versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific OAPP version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/OAPPs/${id}/versions/${version}`),
      { id, version, isActive: true },
      'OAPP version retrieved (Demo Mode)'
    );
  }

  /**
   * Search OAPPs
   */
  async search(searchTerm: string, showAllVersions: boolean = false, version: number = 0): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/OAPPs/search', { params: { searchTerm, showAllVersions, version } }),
      starnetDemoData.oapp.search(searchTerm),
      'OAPP search completed (Demo Mode)'
    );
  }

  /**
   * Search OAPPs (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/OAPPs/search', { searchTerm }),
      starnetDemoData.oapp.search(searchTerm),
      'OAPP search completed (Demo Mode)'
    );
  }

  /**
   * Add OAPP dependency
   */
  async addDependency(id: string, dependencyId: string, dependencyType: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/OAPPs/${id}/add-dependency`, { dependencyId, dependencyType }),
      true,
      'OAPP dependency added successfully (Demo Mode)'
    );
  }

  /**
   * Remove OAPP dependency
   */
  async removeDependency(id: string, dependencyId: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/OAPPs/${id}/remove-dependency`, { dependencyId }),
      true,
      'OAPP dependency removed successfully (Demo Mode)'
    );
  }

  /**
   * Get all OAPPs
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/OAPPs'),
      [
        starnetDemoData.oapp.create({ name: 'Demo OAPP 1' }),
        starnetDemoData.oapp.create({ name: 'Demo OAPP 2' }),
      ],
      'All OAPPs retrieved (Demo Mode)'
    );
  }

  /**
   * Get OAPP by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/OAPPs/${id}`),
      starnetDemoData.oapp.create({ id, name: 'Demo OAPP' }),
      'OAPP retrieved (Demo Mode)'
    );
  }
}

export const oappService = new OAPPService();
