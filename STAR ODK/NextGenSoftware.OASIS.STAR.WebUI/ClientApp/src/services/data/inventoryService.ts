/**
 * Inventory Service
 * Handles Inventory Item operations
 */

import { BaseService } from '../base/baseService';
import { dataDemoData } from '../demo/dataDemoData';
import { OASISResult } from '../../types/star';

class InventoryService extends BaseService {
  /**
   * Create new Inventory Item
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/InventoryItems/create', payload),
      dataDemoData.inventoryItem.create(payload),
      'Inventory item created successfully (Demo Mode)'
    );
  }

  /**
   * Update Inventory Item
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/InventoryItems/${id}`, payload),
      dataDemoData.inventoryItem.update(id, payload),
      'Inventory item updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete Inventory Item
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/InventoryItems/${id}`),
      true,
      'Inventory item deleted successfully (Demo Mode)'
    );
  }

  /**
   * Transfer Inventory Item
   */
  async transfer(id: string, toAvatarId: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/InventoryItems/${id}/transfer`, { toAvatarId }),
      true,
      'Inventory item transferred successfully (Demo Mode)'
    );
  }

  /**
   * Publish Inventory Item
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/InventoryItems/${id}/publish`, payload),
      true,
      'Inventory item published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish Inventory Item
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/InventoryItems/${id}/unpublish`),
      true,
      'Inventory item unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish Inventory Item
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/InventoryItems/${id}/republish`, payload),
      true,
      'Inventory item republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate Inventory Item
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/InventoryItems/${id}/activate`),
      true,
      'Inventory item activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate Inventory Item
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/InventoryItems/${id}/deactivate`),
      true,
      'Inventory item deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download Inventory Item
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/InventoryItems/${id}/download`, { destinationPath, overwrite }),
      { id, path: destinationPath, downloadedOn: new Date().toISOString() },
      'Inventory item downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install Inventory Item
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/InventoryItems/${id}/install`),
      { id, isInstalled: true, installedOn: new Date().toISOString() },
      'Inventory item installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall Inventory Item
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/InventoryItems/${id}/uninstall`),
      true,
      'Inventory item uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone Inventory Item
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/InventoryItems/${id}/clone`, { newName }),
      { id: `cloned-${id}`, name: newName, clonedFrom: id, clonedOn: new Date().toISOString() },
      'Inventory item cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get Inventory Item versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/InventoryItems/${id}/versions`),
      [{ version: '1.0.0', isActive: true, releasedOn: '2024-01-15' }],
      'Inventory item versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific Inventory Item version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/InventoryItems/${id}/versions/${version}`),
      { id, version, isActive: true },
      'Inventory item version retrieved (Demo Mode)'
    );
  }

  /**
   * Search Inventory Items
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/InventoryItems/search', { params: { searchTerm } }),
      dataDemoData.inventoryItem.search(searchTerm),
      'Inventory item search completed (Demo Mode)'
    );
  }

  /**
   * Search Inventory Items (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/InventoryItems/search', { searchTerm }),
      dataDemoData.inventoryItem.search(searchTerm),
      'Inventory item search completed (Demo Mode)'
    );
  }

  /**
   * Get all Inventory Items
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/InventoryItems'),
      [
        dataDemoData.inventoryItem.create({ name: 'Demo Item 1' }),
        dataDemoData.inventoryItem.create({ name: 'Demo Item 2' }),
      ],
      'All Inventory Items retrieved (Demo Mode)'
    );
  }

  /**
   * Get Inventory Item by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/InventoryItems/${id}`),
      dataDemoData.inventoryItem.create({ id, name: 'Demo Item' }),
      'Inventory item retrieved (Demo Mode)'
    );
  }
}

export const inventoryService = new InventoryService();
