/**
 * Park Service
 * Handles Park operations
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class ParkService extends BaseService {
  /**
   * Create new Park
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/Parks/create', payload),
      { id: 'demo-park-1', name: payload.name || 'Demo Park', ...payload },
      'Park created successfully (Demo Mode)'
    );
  }

  /**
   * Update Park
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/Parks/${id}`, payload),
      { id, ...payload, updatedOn: new Date().toISOString() },
      'Park updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Park
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/Parks/${id}`),
      true,
      'Park deleted successfully (Demo Mode)'
    );
  }

  /**
   * Publish Park
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Parks/${id}/publish`, payload),
      true,
      'Park published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Park
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Parks/${id}/unpublish`),
      true,
      'Park unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Park
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Parks/${id}/republish`, payload),
      true,
      'Park republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Park
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Parks/${id}/activate`),
      true,
      'Park activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Park
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Parks/${id}/deactivate`),
      true,
      'Park deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Park
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Parks/${id}/download`, { destinationPath, overwrite }),
      { id, path: destinationPath, downloadedOn: new Date().toISOString() },
      'Park downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Park
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Parks/${id}/install`),
      { id, isInstalled: true, installedOn: new Date().toISOString() },
      'Park installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Park
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Parks/${id}/uninstall`),
      true,
      'Park uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Park
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Parks/${id}/clone`, { newName }),
      { id: `cloned-${id}`, name: newName, clonedFrom: id, clonedOn: new Date().toISOString() },
      'Park cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Park versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Parks/${id}/versions`),
      [{ version: '1.0.0', isActive: true, releasedOn: '2024-01-15' }],
      'Park versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Park version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Parks/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Park version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Parks
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Parks/search', { params: { searchTerm } }),
      [
        { id: 'demo-1', name: 'Demo Park 1', type: 'Theme Park', size: 'Large' },
        { id: 'demo-2', name: 'Demo Park 2', type: 'Nature Park', size: 'Medium' },
      ],
      'Park search completed (Demo Mode)'
    );
  }

  /**
   * Search Parks (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/Parks/search', { searchTerm }),
      [
        { id: 'demo-1', name: 'Demo Park 1', type: 'Theme Park', size: 'Large' },
        { id: 'demo-2', name: 'Demo Park 2', type: 'Nature Park', size: 'Medium' },
      ],
      'Park search completed (Demo Mode)'
    );
  }

  /**
   * Get all Parks
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Parks'),
      [
        { id: 'demo-1', name: 'Demo Park 1', type: 'Theme Park', size: 'Large' },
        { id: 'demo-2', name: 'Demo Park 2', type: 'Nature Park', size: 'Medium' },
      ],
      'All Parks retrieved (Demo Mode)'
    );
  }

  /**
   * Get Park by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Parks/${id}`),
      { id, name: 'Demo Park', type: 'Theme Park', size: 'Large' },
      'Park retrieved (Demo Mode)'
    );
  }
}

export const parkService = new ParkService();
