/**
 * Keys Service
 * Handles Key management operations
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class KeysService extends BaseService {
  /**
   * Generate new key pair
   */
  async generate(type: string = 'RSA'): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post('/Keys/generate', { type }),
      { 
        id: 'demo-key-1', 
        publicKey: '-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----',
        privateKey: '-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC...\n-----END PRIVATE KEY-----',
        type,
        algorithm: 'RSA-2048',
        createdOn: new Date().toISOString()
      },
      'Key pair generated successfully (Demo Mode)'
    );
  }

  /**
   * Import key
   */
  async import(keyData: string, name: string, type: string = 'RSA'): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post('/Keys/import', { keyData, name, type }),
      { 
        id: 'imported-key-1', 
        name,
        publicKey: keyData,
        type,
        imported: true,
        createdOn: new Date().toISOString()
      },
      'Key imported successfully (Demo Mode)'
    );
  }

  /**
   * Export key
   */
  async export(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get(`/Keys/${id}/export`),
      { 
        id, 
        publicKey: '-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----',
        privateKey: '-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC...\n-----END PRIVATE KEY-----'
      },
      'Key exported successfully (Demo Mode)'
    );
  }

  /**
   * Delete key
   */
  async delete(id: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.web4Api.delete(`/Keys/${id}`),
      true,
      'Key deleted successfully (Demo Mode)'
    );
  }

  /**
   * Get key by ID
   */
  async getById(id: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get(`/Keys/${id}`),
      { 
        id, 
        name: 'Demo Key',
        publicKey: '-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----',
        type: 'RSA',
        algorithm: 'RSA-2048',
        createdOn: new Date().toISOString()
      },
      'Key retrieved (Demo Mode)'
    );
  }

  /**
   * Get all keys
   */
  async getAll(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Keys'),
      [
        { id: 'demo-1', name: 'Demo Key 1', type: 'RSA', algorithm: 'RSA-2048', createdOn: new Date().toISOString() },
        { id: 'demo-2', name: 'Demo Key 2', type: 'ECDSA', algorithm: 'P-256', createdOn: new Date().toISOString() },
      ],
      'All Keys retrieved (Demo Mode)'
    );
  }

  /**
   * Get keys for avatar
   */
  async getForAvatar(avatarId: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get(`/Keys/avatar/${avatarId}`),
      [
        { id: 'avatar-1', name: 'Avatar Key 1', type: 'RSA', algorithm: 'RSA-2048', avatarId, createdOn: new Date().toISOString() },
        { id: 'avatar-2', name: 'Avatar Key 2', type: 'ECDSA', algorithm: 'P-256', avatarId, createdOn: new Date().toISOString() },
      ],
      'Avatar Keys retrieved (Demo Mode)'
    );
  }

  /**
   * Sign data with key
   */
  async sign(id: string, data: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post(`/Keys/${id}/sign`, { data }),
      { 
        id, 
        signature: '0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890',
        data,
        algorithm: 'RSA-2048',
        timestamp: new Date().toISOString()
      },
      'Data signed successfully (Demo Mode)'
    );
  }

  /**
   * Verify signature
   */
  async verify(id: string, data: string, signature: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.web4Api.post(`/Keys/${id}/verify`, { data, signature }),
      true,
      'Signature verified successfully (Demo Mode)'
    );
  }

  /**
   * Encrypt data with key
   */
  async encrypt(id: string, data: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post(`/Keys/${id}/encrypt`, { data }),
      { 
        id, 
        encryptedData: 'U2FsdGVkX1+abcdef1234567890abcdef1234567890abcdef1234567890',
        algorithm: 'AES-256-GCM',
        timestamp: new Date().toISOString()
      },
      'Data encrypted successfully (Demo Mode)'
    );
  }

  /**
   * Decrypt data with key
   */
  async decrypt(id: string, encryptedData: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post(`/Keys/${id}/decrypt`, { encryptedData }),
      { 
        id, 
        decryptedData: 'Original data',
        algorithm: 'AES-256-GCM',
        timestamp: new Date().toISOString()
      },
      'Data decrypted successfully (Demo Mode)'
    );
  }

  /**
   * Search keys
   */
  async search(searchTerm: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/Keys/search', { params: { searchTerm } }),
      [
        { id: 'search-1', name: 'Search Key 1', type: 'RSA', algorithm: 'RSA-2048' },
        { id: 'search-2', name: 'Search Key 2', type: 'ECDSA', algorithm: 'P-256' },
      ],
      'Key search completed (Demo Mode)'
    );
  }

  /**
   * Generate key pair (alias for generate)
   */
  async generateKeyPair(keyType: string = 'RSA'): Promise<OASISResult<any>> {
    return this.generate(keyType);
  }

  /**
   * Import private key
   */
  async importPrivateKey(privateKey: string, keyType: string = 'RSA'): Promise<OASISResult<any>> {
    return this.import(privateKey, 'Imported Private Key', keyType);
  }

  /**
   * Export private key
   */
  async exportPrivateKey(publicKey: string): Promise<OASISResult<any>> {
    return this.export(publicKey);
  }

  /**
   * Sign data
   */
  async signData(privateKey: string, dataToSign: string): Promise<OASISResult<any>> {
    return this.sign(privateKey, dataToSign);
  }

  /**
   * Verify signature
   */
  async verifySignature(publicKey: string, data: string, signature: string): Promise<OASISResult<boolean>> {
    return this.verify(publicKey, data, signature);
  }

  /**
   * Encrypt data
   */
  async encryptData(publicKey: string, dataToEncrypt: string): Promise<OASISResult<any>> {
    return this.encrypt(publicKey, dataToEncrypt);
  }

  /**
   * Decrypt data
   */
  async decryptData(privateKey: string, encryptedData: string): Promise<OASISResult<any>> {
    return this.decrypt(privateKey, encryptedData);
  }
}

export const keysService = new KeysService();
