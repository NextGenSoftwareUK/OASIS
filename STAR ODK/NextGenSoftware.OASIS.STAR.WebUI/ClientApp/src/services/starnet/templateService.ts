/**
 * Template Service
 * Handles OAPP Template operations
 */

import { BaseService } from '../base/baseService';
import { starnetDemoData } from '../demo/starnetDemoData';
import { OASISResult } from '../../types/star';

class TemplateService extends BaseService {
  /**
   * Create new Template
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/Templates/create', payload),
      starnetDemoData.template.create(payload),
      'Template created successfully (Demo Mode)'
    );
  }

  /**
   * Update Template
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/Templates/${id}`, payload),
      starnetDemoData.template.update(id, payload),
      'Template updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Template
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/Templates/${id}`),
      true,
      'Template deleted successfully (Demo Mode)'
    );
  }

  /**
   * Publish Template
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Templates/${id}/publish`, payload),
      true,
      'Template published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Template
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Templates/${id}/unpublish`),
      true,
      'Template unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Template
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Templates/${id}/republish`, payload),
      true,
      'Template republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Template
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Templates/${id}/activate`),
      true,
      'Template activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Template
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Templates/${id}/deactivate`),
      true,
      'Template deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Template
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Templates/${id}/download`, { destinationPath, overwrite }),
      starnetDemoData.template.download(id, destinationPath),
      'Template downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Template
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Templates/${id}/install`),
      starnetDemoData.template.install(id),
      'Template installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Template
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Templates/${id}/uninstall`),
      true,
      'Template uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Template
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Templates/${id}/clone`, { newName }),
      starnetDemoData.template.clone(id, newName),
      'Template cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Template versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Templates/${id}/versions`),
      starnetDemoData.template.getVersions(id),
      'Template versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Template version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Templates/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Template version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Templates
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Templates/search', { params: { searchTerm } }),
      starnetDemoData.template.search(searchTerm),
      'Template search completed (Demo Mode)'
    );
  }

  /**
   * Search Templates (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/Templates/search', { searchTerm }),
      starnetDemoData.template.search(searchTerm),
      'Template search completed (Demo Mode)'
    );
  }

  /**
   * Add Template dependency
   */
  async addDependency(id: string, dependencyId: string, dependencyType: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Templates/${id}/add-dependency`, { dependencyId, dependencyType }),
      true,
      'Template dependency added successfully (Demo Mode)'
    );
  }

  /**
   * Remove Template dependency
   */
  async removeDependency(id: string, dependencyId: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Templates/${id}/remove-dependency`, { dependencyId }),
      true,
      'Template dependency removed successfully (Demo Mode)'
    );
  }

  /**
   * Get all Templates
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Templates'),
      [
        starnetDemoData.template.create({ name: 'Demo Template 1' }),
        starnetDemoData.template.create({ name: 'Demo Template 2' }),
      ],
      'All Templates retrieved (Demo Mode)'
    );
  }

  /**
   * Get Template by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Templates/${id}`),
      starnetDemoData.template.create({ id, name: 'Demo Template' }),
      'Template retrieved (Demo Mode)'
    );
  }
}

export const templateService = new TemplateService();
