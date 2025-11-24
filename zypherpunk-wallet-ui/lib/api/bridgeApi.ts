import type { OASISResult } from '../types';
import { oasisWalletAPI } from '../api';

const API_BASE_URL = process.env.NEXT_PUBLIC_OASIS_API_URL || 'http://api.oasisplatform.world';

export interface BridgeRequest {
  fromChain: 'Zcash' | 'Aztec';
  toChain: 'Zcash' | 'Aztec';
  amount: number;
  fromAddress: string;
  toAddress: string;
  usePartialNotes?: boolean;
  generateViewingKey?: boolean;
}

export interface BridgeStatus {
  bridgeId: string;
  fromChain: string;
  toChain: string;
  amount: number;
  status: 'pending' | 'locked' | 'minting' | 'completed' | 'failed';
  lockTxHash?: string;
  mintTxHash?: string;
  viewingKeyHash?: string;
  createdAt: string;
  completedAt?: string;
}

export interface BridgeHistory {
  bridges: BridgeStatus[];
  total: number;
}

class BridgeAPI {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<OASISResult<T>> {
    // Use bridge API path
    const url = `${this.baseUrl}/api/v1/bridge/${endpoint}`;
    const useProxy = process.env.NODE_ENV === 'development' || process.env.NEXT_PUBLIC_USE_API_PROXY === 'true';
    const proxyUrl = useProxy ? `/api/proxy/api/v1/bridge/${endpoint}` : url;

    try {
      const response = await fetch(proxyUrl, {
        ...options,
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
          ...(options.headers as HeadersInit),
        },
        mode: useProxy ? 'same-origin' : 'cors',
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
      }

      const data = await response.json();
      return data as OASISResult<T>;
    } catch (error) {
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Unknown error occurred',
      };
    }
  }

  /**
   * Initiate Zcash → Aztec bridge
   */
  async bridgeZcashToAztec(request: BridgeRequest): Promise<OASISResult<BridgeStatus>> {
    return this.request<BridgeStatus>('bridge/zcash-to-aztec', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  /**
   * Initiate Aztec → Zcash bridge
   */
  async bridgeAztecToZcash(request: BridgeRequest): Promise<OASISResult<BridgeStatus>> {
    return this.request<BridgeStatus>('bridge/aztec-to-zcash', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  /**
   * Get bridge status
   */
  async getBridgeStatus(bridgeId: string): Promise<OASISResult<BridgeStatus>> {
    return this.request<BridgeStatus>(`bridge/status/${bridgeId}`);
  }

  /**
   * Get bridge history for current user
   */
  async getBridgeHistory(): Promise<OASISResult<BridgeHistory>> {
    return this.request<BridgeHistory>('bridge/history');
  }
}

export const bridgeAPI = new BridgeAPI();

