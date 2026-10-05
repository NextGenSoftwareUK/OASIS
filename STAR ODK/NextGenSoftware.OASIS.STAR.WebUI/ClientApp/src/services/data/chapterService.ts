/**
 * Chapter Service
 * Handles Chapter operations
 */

import { BaseService } from '../base/baseService';
import { dataDemoData } from '../demo/dataDemoData';
import { OASISResult } from '../../types/star';

class ChapterService extends BaseService {
  /**
   * Create new Chapter
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/Chapters/create', payload),
      dataDemoData.chapter.create(payload),
      'Chapter created successfully (Demo Mode)'
    );
  }

  /**
   * Update Chapter
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/Chapters/${id}`, payload),
      dataDemoData.chapter.update(id, payload),
      'Chapter updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Chapter
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/Chapters/${id}`),
      true,
      'Chapter deleted successfully (Demo Mode)'
    );
  }

  /**
   * Start Chapter
   */
  async start(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Chapters/${id}/start`),
      dataDemoData.chapter.start(id),
      'Chapter started successfully (Demo Mode)'
    );
  }

  /**
   * Complete Chapter
   */
  async complete(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Chapters/${id}/complete`),
      dataDemoData.chapter.complete(id),
      'Chapter completed successfully (Demo Mode)'
    );
  }

  /**
   * Publish Chapter
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Chapters/${id}/publish`, payload),
      true,
      'Chapter published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Chapter
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Chapters/${id}/unpublish`),
      true,
      'Chapter unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Chapter
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Chapters/${id}/republish`, payload),
      true,
      'Chapter republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Chapter
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Chapters/${id}/activate`),
      true,
      'Chapter activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Chapter
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Chapters/${id}/deactivate`),
      true,
      'Chapter deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Chapter
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Chapters/${id}/download`, { destinationPath, overwrite }),
      { id, path: destinationPath, downloadedOn: new Date().toISOString() },
      'Chapter downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Chapter
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Chapters/${id}/install`),
      { id, isInstalled: true, installedOn: new Date().toISOString() },
      'Chapter installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Chapter
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/Chapters/${id}/uninstall`),
      true,
      'Chapter uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Chapter
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/Chapters/${id}/clone`, { newName }),
      { id: `cloned-${id}`, name: newName, clonedFrom: id, clonedOn: new Date().toISOString() },
      'Chapter cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Chapter versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/Chapters/${id}/versions`),
      [{ version: '1.0.0', isActive: true, releasedOn: '2024-01-15' }],
      'Chapter versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Chapter version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Chapters/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Chapter version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Chapters
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Chapters/search', { params: { searchTerm } }),
      dataDemoData.chapter.search(searchTerm),
      'Chapter search completed (Demo Mode)'
    );
  }

  /**
   * Search Chapters (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/Chapters/search', { searchTerm }),
      dataDemoData.chapter.search(searchTerm),
      'Chapter search completed (Demo Mode)'
    );
  }

  /**
   * Get all Chapters
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/Chapters'),
      [
        dataDemoData.chapter.create({ name: 'Demo Chapter 1' }),
        dataDemoData.chapter.create({ name: 'Demo Chapter 2' }),
      ],
      'All Chapters retrieved (Demo Mode)'
    );
  }

  /**
   * Get Chapter by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/Chapters/${id}`),
      dataDemoData.chapter.create({ id, name: 'Demo Chapter' }),
      'Chapter retrieved (Demo Mode)'
    );
  }
}

export const chapterService = new ChapterService();
