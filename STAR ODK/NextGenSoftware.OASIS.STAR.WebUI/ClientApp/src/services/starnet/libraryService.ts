/**
 * Library Service
 * Handles Library operations
 */

import { BaseService } from '../base/baseService';
import { starnetDemoData } from '../demo/starnetDemoData';
import { OASISResult } from '../../types/star';

class LibraryService extends BaseService {
  /**
   * Create new Library
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/Libraries/create', payload),
      starnetDemoData.library.create(payload),
      'Library created successfully (Demo Mode)'
    );
  }

  /**
   * Update Library
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/Libraries/${id}`, payload),
      starnetDemoData.library.update(id, payload),
      'Library updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Library
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/Libraries/${id}`),
      true,
      'Library deleted successfully (Demo Mode)'
    );
  }

  /**
   * Publish Library
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Libraries/${id}/publish`, payload),
      true,
      'Library published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Library
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Libraries/${id}/unpublish`),
      true,
      'Library unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Library
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Libraries/${id}/republish`, payload),
      true,
      'Library republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Library
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Libraries/${id}/activate`),
      true,
      'Library activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Library
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Libraries/${id}/deactivate`),
      true,
      'Library deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Library
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Libraries/${id}/download`, { destinationPath, overwrite }),
      starnetDemoData.library.download(id, destinationPath),
      'Library downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Library
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Libraries/${id}/install`),
      starnetDemoData.library.install(id),
      'Library installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Library
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Libraries/${id}/uninstall`),
      true,
      'Library uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Library
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Libraries/${id}/clone`, { newName }),
      starnetDemoData.library.clone(id, newName),
      'Library cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Library versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Libraries/${id}/versions`),
      starnetDemoData.library.getVersions(id),
      'Library versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Library version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Libraries/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Library version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Libraries
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Libraries/search', { params: { searchTerm } }),
      starnetDemoData.library.search(searchTerm),
      'Library search completed (Demo Mode)'
    );
  }

  /**
   * Search Libraries (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/Libraries/search', { searchTerm }),
      starnetDemoData.library.search(searchTerm),
      'Library search completed (Demo Mode)'
    );
  }

  /**
   * Add Library dependency
   */
  async addDependency(id: string, dependencyId: string, dependencyType: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Libraries/${id}/add-dependency`, { dependencyId, dependencyType }),
      true,
      'Library dependency added successfully (Demo Mode)'
    );
  }

  /**
   * Remove Library dependency
   */
  async removeDependency(id: string, dependencyId: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Libraries/${id}/remove-dependency`, { dependencyId }),
      true,
      'Library dependency removed successfully (Demo Mode)'
    );
  }

  /**
   * Get all Libraries
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Libraries'),
      [
        starnetDemoData.library.create({ name: 'Demo Library 1' }),
        starnetDemoData.library.create({ name: 'Demo Library 2' }),
      ],
      'All Libraries retrieved (Demo Mode)'
    );
  }

  /**
   * Get Library by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Libraries/${id}`),
      starnetDemoData.library.create({ id, name: 'Demo Library' }),
      'Library retrieved (Demo Mode)'
    );
  }
}

export const libraryService = new LibraryService();
