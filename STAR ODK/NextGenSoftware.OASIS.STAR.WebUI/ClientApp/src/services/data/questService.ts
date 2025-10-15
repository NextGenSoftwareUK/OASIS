/**
 * Quest Service
 * Handles Quest operations
 */

import { BaseService } from '../base/baseService';
import { dataDemoData } from '../demo/dataDemoData';
import { OASISResult } from '../../types/star';

class QuestService extends BaseService {
  /**
   * Create new Quest
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/Quests/create', payload),
      dataDemoData.quest.create(payload),
      'Quest created successfully (Demo Mode)'
    );
  }

  /**
   * Update Quest
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/Quests/${id}`, payload),
      dataDemoData.quest.update(id, payload),
      'Quest updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Quest
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/Quests/${id}`),
      true,
      'Quest deleted successfully (Demo Mode)'
    );
  }

  /**
   * Start Quest
   */
  async start(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Quests/${id}/start`),
      dataDemoData.quest.start(id),
      'Quest started successfully (Demo Mode)'
    );
  }

  /**
   * Complete Quest
   */
  async complete(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Quests/${id}/complete`),
      dataDemoData.quest.complete(id),
      'Quest completed successfully (Demo Mode)'
    );
  }

  /**
   * Publish Quest
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Quests/${id}/publish`, payload),
      true,
      'Quest published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Quest
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Quests/${id}/unpublish`),
      true,
      'Quest unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Quest
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Quests/${id}/republish`, payload),
      true,
      'Quest republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Quest
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Quests/${id}/activate`),
      true,
      'Quest activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Quest
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Quests/${id}/deactivate`),
      true,
      'Quest deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Quest
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Quests/${id}/download`, { destinationPath, overwrite }),
      { id, path: destinationPath, downloadedOn: new Date().toISOString() },
      'Quest downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Quest
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Quests/${id}/install`),
      { id, isInstalled: true, installedOn: new Date().toISOString() },
      'Quest installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Quest
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Quests/${id}/uninstall`),
      true,
      'Quest uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Quest
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Quests/${id}/clone`, { newName }),
      { id: `cloned-${id}`, name: newName, clonedFrom: id, clonedOn: new Date().toISOString() },
      'Quest cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Quest versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Quests/${id}/versions`),
      [{ version: '1.0.0', isActive: true, releasedOn: '2024-01-15' }],
      'Quest versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Quest version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Quests/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Quest version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Quests
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Quests/search', { params: { searchTerm } }),
      dataDemoData.quest.search(searchTerm),
      'Quest search completed (Demo Mode)'
    );
  }

  /**
   * Search Quests (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/Quests/search', { searchTerm }),
      dataDemoData.quest.search(searchTerm),
      'Quest search completed (Demo Mode)'
    );
  }

  /**
   * Get all Quests
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Quests'),
      [
        dataDemoData.quest.create({ name: 'Demo Quest 1' }),
        dataDemoData.quest.create({ name: 'Demo Quest 2' }),
      ],
      'All Quests retrieved (Demo Mode)'
    );
  }

  /**
   * Get Quest by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Quests/${id}`),
      dataDemoData.quest.create({ id, name: 'Demo Quest' }),
      'Quest retrieved (Demo Mode)'
    );
  }
}

export const questService = new QuestService();
