/**
 * GeoNFT Service
 * Handles GeoNFT operations
 */

import { BaseService } from '../base/baseService';
import { dataDemoData } from '../demo/dataDemoData';
import { OASISResult } from '../../types/star';

class GeoNFTService extends BaseService {
  /**
   * Create new GeoNFT
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/GeoNFTs/create', payload),
      dataDemoData.geoNft.create(payload),
      'GeoNFT created successfully (Demo Mode)'
    );
  }

  /**
   * Update GeoNFT
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/GeoNFTs/${id}`, payload),
      dataDemoData.geoNft.update(id, payload),
      'GeoNFT updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete GeoNFT
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/GeoNFTs/${id}`),
      true,
      'GeoNFT deleted successfully (Demo Mode)'
    );
  }

  /**
   * Mint GeoNFT
   */
  async mint(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/GeoNFTs/mint', payload),
      dataDemoData.geoNft.mint(payload),
      'GeoNFT minted successfully (Demo Mode)'
    );
  }

  /**
   * Place GeoNFT
   */
  async place(id: string, latitude: number, longitude: number): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/GeoNFTs/${id}/place`, { latitude, longitude }),
      dataDemoData.geoNft.place(id, latitude, longitude),
      'GeoNFT placed successfully (Demo Mode)'
    );
  }

  /**
   * Mint and Place GeoNFT
   */
  async mintAndPlace(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/GeoNFTs/mint-and-place', payload),
      dataDemoData.geoNft.mint(payload),
      'GeoNFT minted and placed successfully (Demo Mode)'
    );
  }

  /**
   * Publish GeoNFT
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/GeoNFTs/${id}/publish`, payload),
      true,
      'GeoNFT published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish GeoNFT
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/GeoNFTs/${id}/unpublish`),
      true,
      'GeoNFT unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish GeoNFT
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/GeoNFTs/${id}/republish`, payload),
      true,
      'GeoNFT republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate GeoNFT
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/GeoNFTs/${id}/activate`),
      true,
      'GeoNFT activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate GeoNFT
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/GeoNFTs/${id}/deactivate`),
      true,
      'GeoNFT deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download GeoNFT
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/GeoNFTs/${id}/download`, { destinationPath, overwrite }),
      { id, path: destinationPath, downloadedOn: new Date().toISOString() },
      'GeoNFT downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install GeoNFT
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/GeoNFTs/${id}/install`),
      { id, isInstalled: true, installedOn: new Date().toISOString() },
      'GeoNFT installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall GeoNFT
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/GeoNFTs/${id}/uninstall`),
      true,
      'GeoNFT uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone GeoNFT
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/GeoNFTs/${id}/clone`, { newName }),
      { id: `cloned-${id}`, name: newName, clonedFrom: id, clonedOn: new Date().toISOString() },
      'GeoNFT cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get GeoNFT versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/GeoNFTs/${id}/versions`),
      [{ version: '1.0.0', isActive: true, releasedOn: '2024-01-15' }],
      'GeoNFT versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific GeoNFT version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/GeoNFTs/${id}/versions/${version}`),
      { id, version, isActive: true },
      'GeoNFT version retrieved (Demo Mode)'
    );
  }

  /**
   * Search GeoNFTs
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/GeoNFTs/search', { params: { searchTerm } }),
      dataDemoData.geoNft.search(searchTerm),
      'GeoNFT search completed (Demo Mode)'
    );
  }

  /**
   * Search GeoNFTs (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/GeoNFTs/search', { searchTerm }),
      dataDemoData.geoNft.search(searchTerm),
      'GeoNFT search completed (Demo Mode)'
    );
  }

  /**
   * Get all GeoNFTs
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/GeoNFTs'),
      [
        dataDemoData.geoNft.create({ name: 'Demo GeoNFT 1' }),
        dataDemoData.geoNft.create({ name: 'Demo GeoNFT 2' }),
      ],
      'All GeoNFTs retrieved (Demo Mode)'
    );
  }

  /**
   * Get GeoNFT by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/GeoNFTs/${id}`),
      dataDemoData.geoNft.create({ id, name: 'Demo GeoNFT' }),
      'GeoNFT retrieved (Demo Mode)'
    );
  }

  /**
   * Get GeoNFTs by hash
   */
  async getByHash(hash: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/GeoNFTs/hash/${hash}`),
      dataDemoData.geoNft.create({ hash, name: 'Demo GeoNFT by Hash' }),
      'GeoNFT by hash retrieved (Demo Mode)'
    );
  }

  /**
   * Get GeoNFTs for avatar
   */
  async getForAvatar(avatarId: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/GeoNFTs/avatar/${avatarId}`),
      [dataDemoData.geoNft.create({ name: 'Avatar GeoNFT' })],
      'Avatar GeoNFTs retrieved (Demo Mode)'
    );
  }

  /**
   * Get GeoNFTs by mint address
   */
  async getByMintAddress(mintAddress: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/GeoNFTs/mint/${mintAddress}`),
      [dataDemoData.geoNft.create({ name: 'Mint Address GeoNFT' })],
      'GeoNFTs by mint address retrieved (Demo Mode)'
    );
  }
}

export const geoNftService = new GeoNFTService();
