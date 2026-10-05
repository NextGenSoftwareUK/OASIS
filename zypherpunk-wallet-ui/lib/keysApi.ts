import type { ProviderType, OASISResult } from './types';

const API_BASE_URL = process.env.NEXT_PUBLIC_OASIS_API_URL || 'https://localhost:5004';

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
      // Use proxy in development to avoid CORS issues
      const USE_PROXY = process.env.NODE_ENV === 'development' || process.env.NEXT_PUBLIC_USE_API_PROXY === 'true';
      const PROXY_BASE_URL = '/api/proxy';
      
      const url = USE_PROXY 
        ? `${PROXY_BASE_URL}/api/keys/${endpoint}`
        : `${this.baseUrl}/api/keys/${endpoint}`;
      
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
        let errorData;
        try {
          errorData = JSON.parse(errorText);
        } catch {
          errorData = { message: errorText };
        }
        return {
          isError: true,
          message: errorData.message || `HTTP error! status: ${response.status}`,
          detailedMessage: errorText,
        };
      }

      const data = await response.json();
      console.log('Keys API response data:', data);
      
      // Handle nested result structure
      if (data.result && typeof data.result === 'object' && 'isError' in data.result) {
        return data.result as OASISResult<T>;
      }
      
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
   * Link a private key to an avatar (creates wallet and returns wallet ID)
   */
  async linkPrivateKey(
    avatarId: string,
    providerType: ProviderType,
    privateKey: string,
    walletId?: string
  ): Promise<OASISResult<{ walletId: string; id: string }>> {
    const body: any = {
      AvatarID: avatarId,
      ProviderType: providerType, // This should be a string like "ZcashOASIS", "AztecOASIS", etc.
      ProviderKey: privateKey,
    };
    
    // Only include WalletId if provided (omit to create new wallet)
    if (walletId) {
      body.WalletId = walletId;
    }
    
    console.log('🔑 Linking private key with ProviderType:', providerType, 'Body:', JSON.stringify(body, null, 2));
    
    return this.request<{ walletId: string; id: string }>('link_provider_private_key_to_avatar_by_id', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  }

  /**
   * Link a public key to an avatar (requires wallet ID from private key linking)
   */
  async linkPublicKey(
    avatarId: string,
    providerType: ProviderType,
    publicKey: string,
    walletId: string,
    walletAddress?: string
  ): Promise<OASISResult<{ walletId: string; id: string }>> {
    const body = {
      WalletId: walletId,
      AvatarID: avatarId,
      ProviderType: providerType, // This should be a string like "ZcashOASIS", "AztecOASIS", etc.
      ProviderKey: publicKey,
      WalletAddress: walletAddress || publicKey, // Use public key as wallet address if not provided
    };
    
    console.log('🔑 Linking public key with ProviderType:', providerType, 'Body:', JSON.stringify(body, null, 2));
    
    return this.request<{ walletId: string; id: string }>('link_provider_public_key_to_avatar_by_id', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  }

  /**
   * Link both private and public keys together (creates wallet)
   * This is the proper flow: link private key first (creates wallet), then link public key
   */
  async linkKeys(request: LinkKeyRequest): Promise<OASISResult<KeyPairResponse & { walletId: string }>> {
    try {
      let walletId: string | undefined = undefined;
      
      // Step 1: Link the private key first (creates wallet and returns wallet ID)
      if (request.privateKey) {
        const privateKeyResult = await this.linkPrivateKey(
          request.avatarId,
          request.providerType,
          request.privateKey
        );
        
        if (privateKeyResult.isError || !privateKeyResult.result) {
          return {
            isError: true,
            message: `Failed to link private key: ${privateKeyResult.message || 'Unknown error'}`,
          };
        }
        
        // Extract wallet ID from the response
        walletId = privateKeyResult.result.id || privateKeyResult.result.walletId;
        
        if (!walletId) {
          return {
            isError: true,
            message: 'Failed to get wallet ID from private key linking response',
          };
        }
      }

      // Step 2: Link the public key using the wallet ID from step 1
      const publicKeyResult = await this.linkPublicKey(
        request.avatarId,
        request.providerType,
        request.publicKey,
        walletId || '', // Must have wallet ID from private key linking
        request.walletAddress
      );

      if (publicKeyResult.isError) {
        return {
          isError: true,
          message: `Failed to link public key: ${publicKeyResult.message || 'Unknown error'}`,
        };
      }

      return {
        isError: false,
        message: 'Keys linked successfully',
        result: {
          privateKey: request.privateKey || '',
          publicKey: request.publicKey,
          walletAddress: request.walletAddress || request.publicKey,
          walletId: walletId || '',
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
   * Note: The endpoint expects the provider type in the URL path, not the body
   */
  async generateKeypair(
    avatarId: string,
    providerType: ProviderType
  ): Promise<OASISResult<KeyPairResponse>> {
    // The endpoint is: POST /api/keys/generate_keypair_for_provider/{providerType}
    console.log('🔑 Generating keypair for ProviderType:', providerType, 'URL:', `generate_keypair_for_provider/${providerType}`);
    return this.request<KeyPairResponse>(`generate_keypair_for_provider/${providerType}`, {
      method: 'POST',
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

