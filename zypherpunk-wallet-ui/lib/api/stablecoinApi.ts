import type { OASISResult } from '../types';
import { oasisWalletAPI } from '../api';

const API_BASE_URL = process.env.NEXT_PUBLIC_OASIS_API_URL || 'http://api.oasisplatform.world';

export interface MintStablecoinRequest {
  zecAmount: number;
  stablecoinAmount: number;
  aztecAddress: string;
  zcashAddress: string;
  generateViewingKey?: boolean;
}

export interface RedeemStablecoinRequest {
  positionId: string;
  stablecoinAmount: number;
  zcashAddress: string;
}

export interface StablecoinPosition {
  positionId: string;
  avatarId: string;
  collateralAmount: number; // ZEC locked
  debtAmount: number; // zUSD minted
  collateralRatio: number; // Percentage
  health: 'safe' | 'warning' | 'danger' | 'liquidated';
  createdAt: string;
  lastUpdated: string;
  viewingKeyHash?: string;
  zcashAddress?: string;
  aztecAddress?: string;
}

export interface SystemStatus {
  totalSupply: number; // Total zUSD minted
  totalCollateral: number; // Total ZEC locked
  collateralRatio: number; // System-wide ratio
  liquidationThreshold: number;
  currentAPY: number;
  zecPrice: number; // From oracle
}

class StablecoinAPI {
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
    // Use the stablecoin API path
    const url = `${this.baseUrl}/api/v1/stablecoin/${endpoint}`;
    const useProxy = process.env.NODE_ENV === 'development' || process.env.NEXT_PUBLIC_USE_API_PROXY === 'true';
    const proxyUrl = useProxy ? `/api/proxy/api/v1/stablecoin/${endpoint}` : url;

    try {
      const token = this.getAuthToken();
      const headers = new Headers({
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        ...(options.headers as HeadersInit),
      });

      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }

      const response = await fetch(proxyUrl, {
        ...options,
        headers,
        mode: useProxy ? 'same-origin' : 'cors',
      });

      if (!response.ok) {
        const errorText = await response.text();
        let errorMessage = `HTTP error! status: ${response.status}`;
        try {
          const errorData = JSON.parse(errorText);
          errorMessage = errorData.error || errorData.message || errorMessage;
        } catch {
          errorMessage = errorText || errorMessage;
        }
        throw new Error(errorMessage);
      }

      const data = await response.json();
      return data as OASISResult<T>;
    } catch (error) {
      console.error('Stablecoin API request failed:', error);
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Unknown error occurred',
        detailedMessage: error instanceof Error ? error.stack : undefined,
      };
    }
  }

  /**
   * Mint stablecoin with ZEC collateral
   */
  async mintStablecoin(request: MintStablecoinRequest): Promise<OASISResult<StablecoinPosition>> {
    return this.request<StablecoinPosition>('mint', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  /**
   * Redeem stablecoin for ZEC
   */
  async redeemStablecoin(request: RedeemStablecoinRequest): Promise<OASISResult<{ message: string }>> {
    return this.request<{ message: string }>('redeem', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  /**
   * Get position by ID
   */
  async getPosition(positionId: string): Promise<OASISResult<StablecoinPosition>> {
    return this.request<StablecoinPosition>(`position/${positionId}`);
  }

  /**
   * Get position health
   */
  async getPositionHealth(positionId: string): Promise<OASISResult<{ health: string; ratio: number }>> {
    return this.request<{ health: string; ratio: number }>(`position/${positionId}/health`);
  }

  /**
   * Get all positions for current user
   */
  async getPositions(): Promise<OASISResult<StablecoinPosition[]>> {
    return this.request<StablecoinPosition[]>('positions');
  }

  /**
   * Get system status
   */
  async getSystemStatus(): Promise<OASISResult<SystemStatus>> {
    return this.request<SystemStatus>('system');
  }

  /**
   * Liquidate a position
   */
  async liquidatePosition(positionId: string): Promise<OASISResult<{ message: string }>> {
    return this.request<{ message: string }>(`liquidate/${positionId}`, {
      method: 'POST',
    });
  }

  /**
   * Generate yield for a position
   */
  async generateYield(positionId: string): Promise<OASISResult<{ yieldAmount: number }>> {
    return this.request<{ yieldAmount: number }>(`yield/${positionId}`, {
      method: 'POST',
    });
  }
}

export const stablecoinAPI = new StablecoinAPI();

