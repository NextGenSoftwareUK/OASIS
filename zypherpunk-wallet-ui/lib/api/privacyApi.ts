import type { OASISResult } from '../types';
import { oasisWalletAPI } from '../api';

const API_BASE_URL = process.env.NEXT_PUBLIC_OASIS_API_URL || 'http://api.oasisplatform.world';

export interface ViewingKey {
  id: string;
  address: string;
  keyHash: string; // Never send full key, only hash
  purpose: 'audit' | 'compliance' | 'personal';
  createdAt: string;
  lastUsed?: string;
  encrypted: boolean;
}

export interface PrivacyMetrics {
  shieldedBalance: number;
  transparentBalance: number;
  privacyScore: number; // 0-100
  recentShieldedTxs: number;
  viewingKeysActive: number;
  privacyLevel: 'low' | 'medium' | 'high' | 'maximum';
}

export interface ShieldedTransactionRequest {
  fromWalletAddress: string;
  toWalletAddress: string; // Must be shielded address (z-address)
  fromProviderType: string;
  toProviderType: string;
  amount: number;
  memoText?: string;
  privacyLevel?: 'low' | 'medium' | 'high' | 'maximum';
  usePartialNotes?: boolean;
  generateViewingKey?: boolean;
}

export interface PrivacySettings {
  hideBalances: boolean;
  hideTransactionHistory: boolean;
  hideWalletAddresses: boolean;
  privacyLevel: 'standard' | 'enhanced' | 'maximum';
  defaultPrivacyMode: boolean;
}

class PrivacyAPI {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  private getAuthToken(): string | null {
    // Get token from wallet API (which gets it from avatar store)
    return oasisWalletAPI.getAuthToken() || null;
  }

  // Set auth token (syncs with wallet API)
  setAuthToken(token: string | null) {
    oasisWalletAPI.setAuthToken(token);
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<OASISResult<T>> {
    try {
      const token = this.getAuthToken();
      const url = `${this.baseUrl}/api/${endpoint}`;
      
      const headers = new Headers({
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        ...(options.headers as HeadersInit),
      });

      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }

      const response = await fetch(url, {
        ...options,
        headers,
        mode: 'cors',
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
      }

      const data = await response.json();
      return data as OASISResult<T>;
    } catch (error) {
      console.error('Privacy API request failed:', error);
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Unknown error occurred',
        detailedMessage: error instanceof Error ? error.stack : undefined,
      };
    }
  }

  // Generate viewing key for a wallet
  async generateViewingKey(
    walletId: string,
    purpose: 'audit' | 'compliance' | 'personal' = 'audit'
  ): Promise<OASISResult<ViewingKey>> {
    return this.request<ViewingKey>('wallet/generate_viewing_key', {
      method: 'POST',
      body: JSON.stringify({ walletId, purpose }),
    });
  }

  // Get viewing keys for a wallet
  async getViewingKeys(walletId: string): Promise<OASISResult<ViewingKey[]>> {
    return this.request<ViewingKey[]>(`wallet/viewing_keys/${walletId}`);
  }

  // Revoke a viewing key
  async revokeViewingKey(viewingKeyId: string): Promise<OASISResult<boolean>> {
    return this.request<boolean>('wallet/revoke_viewing_key', {
      method: 'POST',
      body: JSON.stringify({ viewingKeyId }),
    });
  }

  // Get privacy metrics for an avatar
  async getPrivacyMetrics(avatarId: string): Promise<OASISResult<PrivacyMetrics>> {
    return this.request<PrivacyMetrics>(`wallet/privacy_metrics/${avatarId}`);
  }

  // Create shielded transaction
  async createShieldedTransaction(
    request: ShieldedTransactionRequest
  ): Promise<OASISResult<any>> {
    // For now, use the existing send_token endpoint with privacy flags
    // This will be extended when backend supports shielded transactions
    return this.request<any>('wallet/send_token', {
      method: 'POST',
      body: JSON.stringify({
        ...request,
        // Privacy flags will be handled by backend when implemented
        privacyMode: true,
        usePartialNotes: request.usePartialNotes || false,
        generateViewingKey: request.generateViewingKey || false,
      }),
    });
  }

  // Save privacy settings
  async savePrivacySettings(
    avatarId: string,
    settings: PrivacySettings
  ): Promise<OASISResult<boolean>> {
    // Store in holons (will be implemented)
    return this.request<boolean>('wallet/privacy_settings', {
      method: 'POST',
      body: JSON.stringify({ avatarId, settings }),
    });
  }

  // Get privacy settings
  async getPrivacySettings(avatarId: string): Promise<OASISResult<PrivacySettings>> {
    return this.request<PrivacySettings>(`wallet/privacy_settings/${avatarId}`);
  }
}

// Create and export singleton instance
export const privacyAPI = new PrivacyAPI();

