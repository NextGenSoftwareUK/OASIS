import type { OASISResult, Wallet } from '../types';
import { oasisWalletAPI } from '../api';

const API_BASE_URL = process.env.NEXT_PUBLIC_OASIS_API_URL || 'http://api.oasisplatform.world';
const USE_PROXY = process.env.NODE_ENV === 'development' || process.env.NEXT_PUBLIC_USE_API_PROXY === 'true';
const PROXY_BASE_URL = '/api/proxy';

class StarknetAPI {
  private baseUrl: string;
  private useProxy: boolean;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
    this.useProxy = USE_PROXY;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<OASISResult<T>> {
    try {
      const base = this.useProxy
        ? `${PROXY_BASE_URL}/api/v1/starknet`
        : `${this.baseUrl}/api/v1/starknet`;
      const url = endpoint.startsWith('/') ? `${base}${endpoint}` : `${base}/${endpoint}`;

      const headers: HeadersInit = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      };
      const authToken = oasisWalletAPI.getAuthToken();
      if (authToken) {
        headers['Authorization'] = `Bearer ${authToken}`;
      }

      const response = await fetch(url, {
        ...options,
        headers: {
          ...headers,
          ...(options.headers as HeadersInit),
        },
        mode: this.useProxy ? 'same-origin' : 'cors',
        method: options.method || 'GET',
      });

      const responseText = await response.text();

      if (!response.ok) {
        const message = responseText.length > 0 ? responseText : response.statusText;
        throw new Error(`HTTP ${response.status}: ${message}`);
      }

      if (responseText.trim().startsWith('<')) {
        throw new Error('Received HTML response from Starknet API');
      }

      let data: OASISResult<T>;
      try {
        data = JSON.parse(responseText) as OASISResult<T>;
      } catch (error) {
        throw new Error('Unable to parse Starknet API response');
      }

      return data;
    } catch (error) {
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  async getWallets(avatarId?: string): Promise<OASISResult<Wallet[]>> {
    const query = avatarId ? `?avatarId=${avatarId}` : '';
    return this.request<Wallet[]>(`wallets${query}`);
  }
}

export const starknetWalletAPI = new StarknetAPI();

