import type { ProviderType, OASISResult } from './types';

const API_BASE_URL = process.env.NEXT_PUBLIC_OASIS_API_URL || 'http://api.oasisplatform.world';

export interface LinkKeyRequest {
  avatarId: string;
  providerType: ProviderType;
  privateKey?: string;
  publicKey: string;
  walletAddress?: string;
}

export interface KeyPairResponse {
  privateKey: string;
  publicKey: string;
  walletAddress?: string;
}

class OASISKeysAPI {
  private baseUrl: string;
  private authToken?: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
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
      const url = `${this.baseUrl}/api/key/${endpoint}`;
      console.log('Making Keys API request to:', url);
      
      const response = await fetch(url, {
        ...options,
        headers: this.mergeHeaders(options.headers),
        mode: 'cors',
      });

      console.log('Keys API response status:', response.status);

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Keys API error response:', errorText);
        throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
      }

      const data = await response.json();
      console.log('Keys API response data:', data);
      return data as OASISResult<T>;
    } catch (error) {
      console.error('Keys API request failed:', error);
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Unknown error occurred',
        detailedMessage: error instanceof Error ? error.stack : undefined,
      };
    }
  }

  /**
   * Link a private key to an avatar
   */
  async linkPrivateKey(
    avatarId: string,
    providerType: ProviderType,
    privateKey: string
  ): Promise<OASISResult<boolean>> {
    return this.request<boolean>('link_provider_private_key', {
      method: 'POST',
      body: JSON.stringify({
        avatarId,
        providerType,
        privateKey,
      }),
    });
  }

  /**
   * Link a public key to an avatar
   */
  async linkPublicKey(
    avatarId: string,
    providerType: ProviderType,
    publicKey: string,
    walletAddress?: string
  ): Promise<OASISResult<boolean>> {
    return this.request<boolean>('link_provider_public_key', {
      method: 'POST',
      body: JSON.stringify({
        avatarId,
        providerType,
        publicKey,
        walletAddress,
      }),
    });
  }

  /**
   * Link both private and public keys together (creates wallet)
   */
  async linkKeys(request: LinkKeyRequest): Promise<OASISResult<KeyPairResponse>> {
    try {
      // First link the private key
      if (request.privateKey) {
        const privateKeyResult = await this.linkPrivateKey(
          request.avatarId,
          request.providerType,
          request.privateKey
        );
        
        if (privateKeyResult.isError) {
          return {
            isError: true,
            message: `Failed to link private key: ${privateKeyResult.message}`,
          };
        }
      }

      // Then link the public key (this creates the wallet)
      const publicKeyResult = await this.linkPublicKey(
        request.avatarId,
        request.providerType,
        request.publicKey,
        request.walletAddress
      );

      if (publicKeyResult.isError) {
        return {
          isError: true,
          message: `Failed to link public key: ${publicKeyResult.message}`,
        };
      }

      return {
        isError: false,
        message: 'Keys linked successfully',
        result: {
          privateKey: request.privateKey || '',
          publicKey: request.publicKey,
          walletAddress: request.walletAddress,
        },
      };
    } catch (error) {
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to link keys',
      };
    }
  }

  /**
   * Generate a keypair for a provider
   */
  async generateKeypair(
    avatarId: string,
    providerType: ProviderType
  ): Promise<OASISResult<KeyPairResponse>> {
    return this.request<KeyPairResponse>('generate_keypair_for_provider', {
      method: 'POST',
      body: JSON.stringify({
        avatarId,
        providerType,
      }),
    });
  }

  /**
   * Get public keys for an avatar
   */
  async getPublicKeys(
    avatarId: string,
    providerType?: ProviderType
  ): Promise<OASISResult<string[]>> {
    const params = providerType ? `?providerType=${providerType}` : '';
    return this.request<string[]>(`get_provider_public_keys/${avatarId}${params}`);
  }

  /**
   * Get private keys for an avatar (encrypted)
   */
  async getPrivateKeys(
    avatarId: string,
    providerType?: ProviderType
  ): Promise<OASISResult<string[]>> {
    const params = providerType ? `?providerType=${providerType}` : '';
    return this.request<string[]>(`get_provider_private_keys/${avatarId}${params}`);
  }
}

// Create and export a singleton instance
export const keysAPI = new OASISKeysAPI();

