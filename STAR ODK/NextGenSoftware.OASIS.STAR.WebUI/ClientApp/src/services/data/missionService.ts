/**
 * Mission Service
 * Handles Mission operations
 */

import { BaseService } from '../base/baseService';
import { dataDemoData } from '../demo/dataDemoData';
import { OASISResult } from '../../types/star';

class MissionService extends BaseService {
  /**
   * Create new Mission
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/Missions/create', payload),
      dataDemoData.mission.create(payload),
      'Mission created successfully (Demo Mode)'
    );
  }

  /**
   * Update Mission
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/Missions/${id}`, payload),
      dataDemoData.mission.update(id, payload),
      'Mission updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Mission
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/Missions/${id}`),
      true,
      'Mission deleted successfully (Demo Mode)'
    );
  }

  /**
   * Start Mission
   */
  async start(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Missions/${id}/start`),
      dataDemoData.mission.start(id),
      'Mission started successfully (Demo Mode)'
    );
  }

  /**
   * Complete Mission
   */
  async complete(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Missions/${id}/complete`),
      dataDemoData.mission.complete(id),
      'Mission completed successfully (Demo Mode)'
    );
  }

  /**
   * Publish Mission
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Missions/${id}/publish`, payload),
      true,
      'Mission published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Mission
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Missions/${id}/unpublish`),
      true,
      'Mission unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Mission
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Missions/${id}/republish`, payload),
      true,
      'Mission republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Mission
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Missions/${id}/activate`),
      true,
      'Mission activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Mission
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Missions/${id}/deactivate`),
      true,
      'Mission deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Mission
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Missions/${id}/download`, { destinationPath, overwrite }),
      { id, path: destinationPath, downloadedOn: new Date().toISOString() },
      'Mission downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Mission
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Missions/${id}/install`),
      { id, isInstalled: true, installedOn: new Date().toISOString() },
      'Mission installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Mission
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Missions/${id}/uninstall`),
      true,
      'Mission uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Mission
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Missions/${id}/clone`, { newName }),
      { id: `cloned-${id}`, name: newName, clonedFrom: id, clonedOn: new Date().toISOString() },
      'Mission cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Mission versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Missions/${id}/versions`),
      [{ version: '1.0.0', isActive: true, releasedOn: '2024-01-15' }],
      'Mission versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Mission version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Missions/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Mission version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Missions
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Missions/search', { params: { searchTerm } }),
      dataDemoData.mission.search(searchTerm),
      'Mission search completed (Demo Mode)'
    );
  }

  /**
   * Search Missions (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/Missions/search', { searchTerm }),
      dataDemoData.mission.search(searchTerm),
      'Mission search completed (Demo Mode)'
    );
  }

  /**
   * Get all Missions
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Missions'),
      [
        dataDemoData.mission.create({ name: 'Demo Mission 1' }),
        dataDemoData.mission.create({ name: 'Demo Mission 2' }),
      ],
      'All Missions retrieved (Demo Mode)'
    );
  }

  /**
   * Get Mission by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Missions/${id}`),
      dataDemoData.mission.create({ id, name: 'Demo Mission' }),
      'Mission retrieved (Demo Mode)'
    );
  }
}

export const missionService = new MissionService();
