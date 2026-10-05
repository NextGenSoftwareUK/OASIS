/**
 * NFT Service
 * Handles NFT operations
 */

import { BaseService } from '../base/baseService';
import { dataDemoData } from '../demo/dataDemoData';
import { OASISResult } from '../../types/star';

class NFTService extends BaseService {
  /**
   * Create new NFT
   */
  async create(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/NFTs/create', payload),
      dataDemoData.nft.create(payload),
      'NFT created successfully (Demo Mode)'
    );
  }

  /**
   * Update NFT
   */
  async update(id: string, payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.put(`/NFTs/${id}`, payload),
      dataDemoData.nft.update(id, payload),
      'NFT updated successfully (Demo Mode)'
    );
  }

  /**
   * Delete NFT
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.delete(`/NFTs/${id}`),
      true,
      'NFT deleted successfully (Demo Mode)'
    );
  }

  /**
   * Mint NFT
   */
  async mint(payload: any): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post('/NFTs/mint', payload),
      dataDemoData.nft.mint(payload),
      'NFT minted successfully (Demo Mode)'
    );
  }

  /**
   * Transfer NFT
   */
  async transfer(id: string, toAddress: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/NFTs/${id}/transfer`, { toAddress }),
      true,
      'NFT transferred successfully (Demo Mode)'
    );
  }

  /**
   * Send NFT
   */
  async send(id: string, toAddress: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/NFTs/${id}/send`, { toAddress }),
      true,
      'NFT sent successfully (Demo Mode)'
    );
  }

  /**
   * Publish NFT
   */
  async publish(id: string, payload: any = {}): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/NFTs/${id}/publish`, payload),
      true,
      'NFT published successfully (Demo Mode)'
    );
  }

  /**
   * Unpublish NFT
   */
  async unpublish(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/NFTs/${id}/unpublish`),
      true,
      'NFT unpublished successfully (Demo Mode)'
    );
  }

  /**
   * Republish NFT
   */
  async republish(id: string, payload: any): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/NFTs/${id}/republish`, payload),
      true,
      'NFT republished successfully (Demo Mode)'
    );
  }

  /**
   * Activate NFT
   */
  async activate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/NFTs/${id}/activate`),
      true,
      'NFT activated successfully (Demo Mode)'
    );
  }

  /**
   * Deactivate NFT
   */
  async deactivate(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/NFTs/${id}/deactivate`),
      true,
      'NFT deactivated successfully (Demo Mode)'
    );
  }

  /**
   * Download NFT
   */
  async download(id: string, destinationPath: string, overwrite: boolean = false): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/NFTs/${id}/download`, { destinationPath, overwrite }),
      { id, path: destinationPath, downloadedOn: new Date().toISOString() },
      'NFT downloaded successfully (Demo Mode)'
    );
  }

  /**
   * Install NFT
   */
  async install(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/NFTs/${id}/install`),
      { id, isInstalled: true, installedOn: new Date().toISOString() },
      'NFT installed successfully (Demo Mode)'
    );
  }

  /**
   * Uninstall NFT
   */
  async uninstall(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.starApi.post(`/NFTs/${id}/uninstall`),
      true,
      'NFT uninstalled successfully (Demo Mode)'
    );
  }

  /**
   * Clone NFT
   */
  async clone(id: string, newName: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/NFTs/${id}/clone`, { newName }),
      { id: `cloned-${id}`, name: newName, clonedFrom: id, clonedOn: new Date().toISOString() },
      'NFT cloned successfully (Demo Mode)'
    );
  }

  /**
   * Get NFT versions
   */
  async getVersions(id: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/NFTs/${id}/versions`),
      [{ version: '1.0.0', isActive: true, releasedOn: '2024-01-15' }],
      'NFT versions retrieved (Demo Mode)'
    );
  }

  /**
   * Get specific NFT version
   */
  async getVersion(id: string, version: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/NFTs/${id}/versions/${version}`),
      { id, version, isActive: true },
      'NFT version retrieved (Demo Mode)'
    );
  }

  /**
   * Search NFTs
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/NFTs/search', { params: { searchTerm } }),
      dataDemoData.nft.search(searchTerm),
      'NFT search completed (Demo Mode)'
    );
  }

  /**
   * Search NFTs (POST)
   */
  async searchPost(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.post('/NFTs/search', { searchTerm }),
      dataDemoData.nft.search(searchTerm),
      'NFT search completed (Demo Mode)'
    );
  }

  /**
   * Get all NFTs
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get('/NFTs'),
      [
        dataDemoData.nft.create({ name: 'Demo NFT 1' }),
        dataDemoData.nft.create({ name: 'Demo NFT 2' }),
      ],
      'All NFTs retrieved (Demo Mode)'
    );
  }

  /**
   * Get NFT by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/NFTs/${id}`),
      dataDemoData.nft.create({ id, name: 'Demo NFT' }),
      'NFT retrieved (Demo Mode)'
    );
  }

  /**
   * Get NFTs by hash
   */
  async getByHash(hash: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/NFTs/hash/${hash}`),
      dataDemoData.nft.create({ hash, name: 'Demo NFT by Hash' }),
      'NFT by hash retrieved (Demo Mode)'
    );
  }

  /**
   * Get NFTs for avatar
   */
  async getForAvatar(avatarId: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/NFTs/avatar/${avatarId}`),
      [dataDemoData.nft.create({ name: 'Avatar NFT' })],
      'Avatar NFTs retrieved (Demo Mode)'
    );
  }

  /**
   * Get NFTs by mint address
   */
  async getByMintAddress(mintAddress: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.starApi.get(`/NFTs/mint/${mintAddress}`),
      [dataDemoData.nft.create({ name: 'Mint Address NFT' })],
      'NFTs by mint address retrieved (Demo Mode)'
    );
  }
}

export const nftService = new NFTService();
