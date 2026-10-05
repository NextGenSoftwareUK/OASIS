/**
 * x402 Distribution Hook
 * 
 * React hook for interacting with x402 payment distribution endpoints
 */

"use client";

import { useCallback, useState } from "react";
import { useOasisApi } from "./use-oasis-api";
import type { X402Stats, X402Distribution } from "@/types/x402";

export function useX402Distribution(baseUrl: string, token?: string) {
  const { call } = useOasisApi({ baseUrl, token });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  /**
   * Get distribution statistics for an NFT
   */
  const getStats = useCallback(
    async (nftMintAddress: string): Promise<X402Stats | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const response = await call(`/api/x402/stats/${nftMintAddress}`, {
          method: "GET",
        });

        if (!response || typeof response !== 'object') {
          throw new Error('Invalid response from stats endpoint');
        }

        const data = response as { success: boolean; stats: X402Stats };
        
        if (!data.success) {
          throw new Error('Failed to fetch stats');
        }

        return data.stats;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to fetch stats';
        setError(message);
        console.error('[x402] getStats error:', err);
        return null;
      } finally {
        setLoading(false);
      }
    },
    [call]
  );

  /**
   * Test payment distribution (for development/testing)
   */
  const testDistribution = useCallback(
    async (nftMintAddress: string, amount: number): Promise<X402Distribution | null> => {
      try {
        setLoading(true);
        setError(null);
        
        const response = await call("/api/x402/distribute-test", {
          method: "POST",
          body: JSON.stringify({ nftMintAddress, amount }),
        });

        if (!response || typeof response !== 'object') {
          throw new Error('Invalid response from distribution endpoint');
        }

        const data = response as { success: boolean; result: X402Distribution };
        
        if (!data.success) {
          throw new Error('Distribution failed');
        }

        return data.result;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Distribution failed';
        setError(message);
        console.error('[x402] testDistribution error:', err);
        return null;
      } finally {
        setLoading(false);
      }
    },
    [call]
  );

  /**
   * Get distribution history for an NFT
   */
  const getHistory = useCallback(
    async (nftMintAddress: string, limit = 10): Promise<X402Distribution[]> => {
      try {
        setLoading(true);
        setError(null);
        
        const response = await call(`/api/x402/history/${nftMintAddress}?limit=${limit}`, {
          method: "GET",
        });

        if (!response || typeof response !== 'object') {
          throw new Error('Invalid response from history endpoint');
        }

        const data = response as { success: boolean; distributions: X402Distribution[] };
        
        if (!data.success) {
          throw new Error('Failed to fetch history');
        }

        return data.distributions || [];
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to fetch history';
        setError(message);
        console.error('[x402] getHistory error:', err);
        return [];
      } finally {
        setLoading(false);
      }
    },
    [call]
  );

  return { 
    getStats, 
    testDistribution, 
    getHistory,
    loading, 
    error 
  };
}

