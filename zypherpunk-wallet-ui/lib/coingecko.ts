import { ProviderType } from './types';
import { normalizeProviderType } from './providerTypeMapper';

const COINGECKO_API = 'https://api.coingecko.com/api/v3';

// Map ProviderType to CoinGecko coin ID
const PROVIDER_TO_COINGECKO_ID: Partial<Record<ProviderType, string>> = {
  [ProviderType.ZcashOASIS]: 'zcash',
  [ProviderType.AztecOASIS]: 'aztec', // Note: May not have CoinGecko listing
  [ProviderType.MidenOASIS]: 'miden', // Note: May not have CoinGecko listing
  [ProviderType.StarknetOASIS]: 'starknet',
  [ProviderType.SolanaOASIS]: 'solana',
  [ProviderType.EthereumOASIS]: 'ethereum',
  [ProviderType.PolygonOASIS]: 'matic-network',
  [ProviderType.ArbitrumOASIS]: 'arbitrum',
  [ProviderType.CosmosBlockChainOASIS]: 'cosmos',
  [ProviderType.EOSIOOASIS]: 'eos',
  [ProviderType.TelosOASIS]: 'telos',
};

export interface PriceData {
  price: number;
  change24h: number; // Percentage (e.g., 5.2 for 5.2%)
  marketCap?: number;
}

export interface MarketChartData {
  prices: Array<[number, number]>; // [timestamp, price]
  market_caps: Array<[number, number]>;
  total_volumes: Array<[number, number]>;
}

export type TimeFrame = '1H' | '1D' | '1W' | '1M' | 'YTD' | 'ALL';

/**
 * Get CoinGecko ID for a provider type
 */
export function getCoinGeckoId(providerType: ProviderType | string): string | null {
  const normalized = normalizeProviderType(providerType);
  return PROVIDER_TO_COINGECKO_ID[normalized] || null;
}

/**
 * Check if a provider has CoinGecko price data available
 */
export function hasCoinGeckoPriceData(providerType: ProviderType | string): boolean {
  return getCoinGeckoId(providerType) !== null;
}

/**
 * Fetch current price and 24h change from CoinGecko
 */
export async function fetchCoinGeckoPrice(coinId: string): Promise<PriceData | null> {
  try {
    const url = `${COINGECKO_API}/simple/price?ids=${coinId}&vs_currencies=usd&include_24hr_change=true&include_market_cap=true`;
    const response = await fetch(url);
    
    if (!response.ok) {
      throw new Error(`CoinGecko API error: ${response.status}`);
    }
    
    const data = await response.json();
    const coinData = data[coinId];
    
    if (!coinData || coinData.usd === undefined) {
      return null;
    }
    
    return {
      price: coinData.usd,
      change24h: coinData.usd_24h_change || 0,
      marketCap: coinData.usd_market_cap || undefined,
    };
  } catch (error) {
    console.error('Failed to fetch CoinGecko price:', error);
    return null;
  }
}

/**
 * Fetch market chart data from CoinGecko
 */
export async function fetchCoinGeckoMarketChart(
  coinId: string,
  days: number
): Promise<MarketChartData | null> {
  try {
    const url = `${COINGECKO_API}/coins/${coinId}/market_chart?vs_currency=usd&days=${days}`;
    const response = await fetch(url);
    
    if (!response.ok) {
      throw new Error(`CoinGecko API error: ${response.status}`);
    }
    
    const data = await response.json();
    
    return {
      prices: data.prices || [],
      market_caps: data.market_caps || [],
      total_volumes: data.total_volumes || [],
    };
  } catch (error) {
    console.error('Failed to fetch CoinGecko market chart:', error);
    return null;
  }
}

/**
 * Convert timeframe to days for CoinGecko API
 */
export function timeFrameToDays(timeFrame: TimeFrame): number {
  const now = new Date();
  const yearStart = new Date(now.getFullYear(), 0, 1);
  const daysSinceYearStart = Math.floor((now.getTime() - yearStart.getTime()) / (1000 * 60 * 60 * 24));
  
  switch (timeFrame) {
    case '1H':
      return 1; // Minimum 1 day for CoinGecko
    case '1D':
      return 1;
    case '1W':
      return 7;
    case '1M':
      return 30;
    case 'YTD':
      return daysSinceYearStart || 365;
    case 'ALL':
      return 365; // Max 365 days for free CoinGecko API
    default:
      return 7;
  }
}

/**
 * Format price history data for chart display
 */
export function formatPriceHistory(
  marketChartData: MarketChartData,
  timeFrame: TimeFrame
): Array<{ time: string; price: number }> {
  if (!marketChartData.prices || marketChartData.prices.length === 0) {
    return [];
  }
  
  const prices = marketChartData.prices;
  
  // For hourly view, sample more frequently
  // For longer timeframes, sample less frequently to reduce data points
  let sampleInterval = 1;
  if (prices.length > 100) {
    sampleInterval = Math.ceil(prices.length / 100);
  }
  
  const formatted = prices
    .filter((_, index) => index % sampleInterval === 0)
    .map(([timestamp, price]) => {
      const date = new Date(timestamp);
      
      // Format time string based on timeframe
      let timeString: string;
      switch (timeFrame) {
        case '1H':
          timeString = date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
          break;
        case '1D':
          timeString = date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
          break;
        case '1W':
          timeString = date.toLocaleDateString('en-US', { weekday: 'short', hour: 'numeric' });
          break;
        case '1M':
        case 'YTD':
        case 'ALL':
          timeString = date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
          break;
        default:
          timeString = date.toLocaleDateString('en-US');
      }
      
      return {
        time: timeString,
        price: price as number,
      };
    });
  
  return formatted;
}

