import type { 
  Wallet, 
  Transaction, 
  WalletTransactionRequest, 
  WalletImportRequest, 
  WalletSecretPhraseImportRequest,
  WalletJsonImportRequest,
  ProviderType,
  OASISResult 
} from './types';

const API_BASE_URL = process.env.NEXT_PUBLIC_OASIS_API_URL || 'https://localhost:5004';

// Use proxy in development to avoid CORS issues
const USE_PROXY = process.env.NODE_ENV === 'development' || process.env.NEXT_PUBLIC_USE_API_PROXY === 'true';
const PROXY_BASE_URL = '/api/proxy';

class OASISWalletAPI {
  private baseUrl: string;
  private authToken?: string;
  private useProxy: boolean;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
    this.useProxy = USE_PROXY;
  }

  setAuthToken(token: string | null) {
    this.authToken = token || undefined;
  }

  getAuthToken() {
    return this.authToken;
  }

  private mergeHeaders(optionsHeaders?: HeadersInit): Headers {
    const headers = new Headers({
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    });

    if (this.authToken) {
      headers.set('Authorization', `Bearer ${this.authToken}`);
    }

    if (optionsHeaders) {
      if (optionsHeaders instanceof Headers) {
        optionsHeaders.forEach((value, key) => headers.set(key, value));
      } else if (Array.isArray(optionsHeaders)) {
        optionsHeaders.forEach(([key, value]) => {
          if (value !== undefined) {
            headers.set(key, value as string);
          }
        });
      } else {
        Object.entries(optionsHeaders).forEach(([key, value]) => {
          if (value !== undefined) {
            headers.set(key, value as string);
          }
        });
      }
    }

    return headers;
  }

  private async request<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<OASISResult<T>> {
    try {
      // Use proxy in development to avoid CORS
      const url = this.useProxy 
        ? `${PROXY_BASE_URL}/wallet/${endpoint}`
        : `${this.baseUrl}/api/wallet/${endpoint}`;
      console.log('Making API request to:', url);
      
      const headers = this.mergeHeaders(options.headers);
      const hasAuth = headers.get('Authorization');
      console.log('Request has Authorization header:', !!hasAuth, hasAuth ? `${hasAuth.substring(0, 20)}...` : 'none');
      
      const response = await fetch(url, {
        ...options,
        headers,
        mode: this.useProxy ? 'same-origin' : 'cors',
      });

      console.log('API response status:', response.status);
      console.log('API response headers:', response.headers);

      // Check content type before parsing
      const contentType = response.headers.get('content-type') || '';
      const isJson = contentType.includes('application/json');
      
      const responseText = await response.text();
      
      // Check if response is HTML (bot protection)
      if (responseText.trim().startsWith('<!') || responseText.trim().startsWith('<html')) {
        console.error('API returned HTML instead of JSON (bot protection?)');
        throw new Error('API returned HTML response. This may indicate bot protection or server error.');
      }

      if (!response.ok) {
        console.error('API error response:', responseText.substring(0, 200));
        throw new Error(`HTTP error! status: ${response.status}, message: ${responseText.substring(0, 100)}`);
      }

      // Try to parse as JSON
      let data;
      try {
        data = JSON.parse(responseText);
      } catch (e) {
        console.error('Failed to parse JSON:', responseText.substring(0, 200));
        throw new Error('Invalid JSON response from API');
      }
      
      console.log('API response data:', data);
      return data as OASISResult<T>;
    } catch (error) {
      console.error('API request failed:', error);
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Unknown error occurred',
        detailedMessage: error instanceof Error ? error.stack : undefined,
      };
    }
  }

  // Load wallets by different identifiers
  async loadWalletsById(avatarId: string, providerType?: ProviderType): Promise<OASISResult<Record<ProviderType, Wallet[]>>> {
    // Use the correct OASIS API endpoint: /api/wallet/avatar/{id}/wallets
    const params = providerType ? `?providerType=${providerType}` : '';
    return this.request<Record<ProviderType, Wallet[]>>(`avatar/${avatarId}/wallets${params}`);
  }

  async loadWalletsByUsername(username: string, providerType?: ProviderType): Promise<OASISResult<Record<ProviderType, Wallet[]>>> {
    const params = providerType ? `?providerType=${providerType}` : '';
    return this.request<Record<ProviderType, Wallet[]>>(`avatar/username/${username}/wallets${params}`);
  }

  async loadWalletsByEmail(email: string, providerType?: ProviderType): Promise<OASISResult<Record<ProviderType, Wallet[]>>> {
    const params = providerType ? `?providerType=${providerType}` : '';
    return this.request<Record<ProviderType, Wallet[]>>(`avatar/email/${email}/wallets${params}`);
  }

  // Send transaction
  async sendToken(request: WalletTransactionRequest): Promise<OASISResult<Transaction>> {
    return this.request<Transaction>('send_token', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  // Import wallets
  async importWalletBySecretPhrase(request: WalletSecretPhraseImportRequest): Promise<OASISResult<Wallet>> {
    return this.request<Wallet>('import_wallet_secret_phrase', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async importWalletByJson(request: WalletJsonImportRequest): Promise<OASISResult<Wallet>> {
    return this.request<Wallet>('import_wallet_json', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async importWalletByPrivateKeyById(avatarId: string, request: WalletImportRequest): Promise<OASISResult<string>> {
    return this.request<string>(`import_wallet_private_key_by_id/${avatarId}`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async importWalletByPrivateKeyByUsername(username: string, request: WalletImportRequest): Promise<OASISResult<string>> {
    return this.request<string>(`import_wallet_private_key_by_username/${username}`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async importWalletByPrivateKeyByEmail(email: string, request: WalletImportRequest): Promise<OASISResult<string>> {
    return this.request<string>(`import_wallet_private_key_by_email/${email}`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async importWalletByPublicKeyById(avatarId: string, request: WalletImportRequest): Promise<OASISResult<string>> {
    return this.request<string>(`import_wallet_public_key_by_id/${avatarId}`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async importWalletByPublicKeyByUsername(username: string, request: WalletImportRequest): Promise<OASISResult<string>> {
    return this.request<string>(`import_wallet_public_key_by_username/${username}`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async importWalletByPublicKeyByEmail(email: string, request: WalletImportRequest): Promise<OASISResult<string>> {
    return this.request<string>(`import_wallet_public_key_by_email/${email}`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  // Default wallet operations
  async getDefaultWalletById(avatarId: string, providerType: ProviderType): Promise<OASISResult<Wallet>> {
    return this.request<Wallet>(`default_wallet_by_id/${avatarId}?providerType=${providerType}`);
  }

  async getDefaultWalletByUsername(username: string, providerType: ProviderType): Promise<OASISResult<Wallet>> {
    return this.request<Wallet>(`default_wallet_by_username/${username}?providerType=${providerType}`);
  }

  async getDefaultWalletByEmail(email: string, providerType: ProviderType): Promise<OASISResult<Wallet>> {
    return this.request<Wallet>(`default_wallet_by_email/${email}?providerType=${providerType}`);
  }

  async setDefaultWalletById(avatarId: string, walletId: string, providerType: ProviderType): Promise<OASISResult<boolean>> {
    return this.request<boolean>(`set_default_wallet_by_id/${avatarId}/${walletId}?providerType=${providerType}`, {
      method: 'POST',
    });
  }

  async setDefaultWalletByUsername(username: string, walletId: string, providerType: ProviderType): Promise<OASISResult<boolean>> {
    return this.request<boolean>(`set_default_wallet_by_username/${username}/${walletId}?providerType=${providerType}`, {
      method: 'POST',
    });
  }

  async setDefaultWalletByEmail(email: string, walletId: string, providerType: ProviderType): Promise<OASISResult<boolean>> {
    return this.request<boolean>(`set_default_wallet_by_email/${email}/${walletId}?providerType=${providerType}`, {
      method: 'POST',
    });
  }

  // Get wallet by public key
  async getWalletByPublicKey(providerKey: string, providerType: ProviderType): Promise<OASISResult<Wallet>> {
    return this.request<Wallet>(`wallet_by_public_key/${providerKey}?providerType=${providerType}`);
  }

  // Clear cache
  async clearCache(): Promise<OASISResult<boolean>> {
    return this.request<boolean>('clear_cache', {
      method: 'POST',
    });
  }

  // Save wallets
  async saveWalletsById(avatarId: string, wallets: Record<ProviderType, Wallet[]>, providerType?: ProviderType): Promise<OASISResult<boolean>> {
    const params = providerType ? `?providerType=${providerType}` : '';
    return this.request<boolean>(`save_wallets_by_id/${avatarId}${params}`, {
      method: 'POST',
      body: JSON.stringify(wallets),
    });
  }

  async saveWalletsByUsername(username: string, wallets: Record<ProviderType, Wallet[]>, providerType?: ProviderType): Promise<OASISResult<boolean>> {
    const params = providerType ? `?providerType=${providerType}` : '';
    return this.request<boolean>(`save_wallets_by_username/${username}${params}`, {
      method: 'POST',
      body: JSON.stringify(wallets),
    });
  }

  async saveWalletsByEmail(email: string, wallets: Record<ProviderType, Wallet[]>, providerType?: ProviderType): Promise<OASISResult<boolean>> {
    const params = providerType ? `?providerType=${providerType}` : '';
    return this.request<boolean>(`save_wallets_by_email/${email}${params}`, {
      method: 'POST',
      body: JSON.stringify(wallets),
    });
  }
}

// Create and export a singleton instance
export const oasisWalletAPI = new OASISWalletAPI(); 