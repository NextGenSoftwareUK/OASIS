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

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<OASISResult<T>> {
    // Use the stablecoin API path
    const url = `${this.baseUrl}/api/v1/stablecoin/${endpoint}`;
    const useProxy = process.env.NODE_ENV === 'development' || process.env.NEXT_PUBLIC_USE_API_PROXY === 'true';
    const proxyUrl = useProxy ? `/api/proxy/api/v1/stablecoin/${endpoint}` : url;

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
  async redeemStablecoin(request: RedeemStablecoinRequest): Promise<OASISResult<string>> {
    return this.request<string>('redeem', {
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
  async liquidatePosition(positionId: string): Promise<OASISResult<string>> {
    return this.request<string>(`liquidate/${positionId}`, {
      method: 'POST',
    });
  }

  /**
   * Generate yield for a position
   */
  async generateYield(positionId: string): Promise<OASISResult<number>> {
    return this.request<number>(`yield/${positionId}`, {
      method: 'POST',
    });
  }
}

export const stablecoinAPI = new StablecoinAPI();

