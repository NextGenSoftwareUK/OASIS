// Client-side exchange rate service using CoinGecko API
// Fallback when backend API is not available

const COINGECKO_API = "https://api.coingecko.com/api/v3";

const TOKEN_IDS: Record<string, string> = {
  SOL: "solana",
  ETH: "ethereum",
  MATIC: "matic-network",
  BASE: "base",
  ARB: "arbitrum",
  OP: "optimism",
  BNB: "binancecoin",
  AVAX: "avalanche-2",
  FTM: "fantom",
  XRD: "radix",
  BTC: "bitcoin",
  USDC: "usd-coin",
  USDT: "tether",
};

interface ExchangeRateResponse {
  data: {
    rate: number;
    fromToken: string;
    toToken: string;
    timestamp: number;
  };
}

export async function getExchangeRate(
  fromToken: string,
  toToken: string
): Promise<ExchangeRateResponse> {
  try {
    const fromCoinId = TOKEN_IDS[fromToken.toUpperCase()];
    const toCoinId = TOKEN_IDS[toToken.toUpperCase()];

    if (!fromCoinId || !toCoinId) {
      throw new Error(`Unsupported token: ${fromToken} or ${toToken}`);
    }

    // Fetch prices from CoinGecko
    const response = await fetch(
      `${COINGECKO_API}/simple/price?ids=${fromCoinId},${toCoinId}&vs_currencies=usd`
    );

    if (!response.ok) {
      throw new Error(`CoinGecko API error: ${response.status}`);
    }

    const data = await response.json();

    const fromPrice = data[fromCoinId]?.usd;
    const toPrice = data[toCoinId]?.usd;

    if (!fromPrice || !toPrice) {
      throw new Error("Failed to get price data");
    }

    // Calculate exchange rate
    const rate = fromPrice / toPrice;

    return {
      data: {
        rate,
        fromToken,
        toToken,
        timestamp: Date.now(),
      },
    };
  } catch (error) {
    console.error("Exchange rate error:", error);
    // Return mock rate if API fails
    return {
      data: {
        rate: getMockRate(fromToken, toToken),
        fromToken,
        toToken,
        timestamp: Date.now(),
      },
    };
  }
}

// Mock exchange rates as fallback
function getMockRate(fromToken: string, toToken: string): number {
  const mockRates: Record<string, number> = {
    "SOL-ETH": 0.05,
    "ETH-SOL": 20,
    "SOL-MATIC": 2.5,
    "MATIC-SOL": 0.4,
    "ETH-MATIC": 50,
    "MATIC-ETH": 0.02,
    "SOL-BASE": 0.05,
    "BASE-SOL": 20,
    "ETH-BASE": 1,
    "BASE-ETH": 1,
    "SOL-ARB": 0.05,
    "ARB-SOL": 20,
    "ETH-ARB": 1,
    "ARB-ETH": 1,
    "SOL-OP": 0.05,
    "OP-SOL": 20,
    "SOL-BNB": 0.2,
    "BNB-SOL": 5,
    "SOL-AVAX": 0.4,
    "AVAX-SOL": 2.5,
    "SOL-FTM": 20,
    "FTM-SOL": 0.05,
    "SOL-XRD": 50,
    "XRD-SOL": 0.02,
  };

  const key = `${fromToken}-${toToken}`;
  return mockRates[key] || 1;
}

// Get USD price for a single token
export async function getTokenUSDPrice(token: string): Promise<number> {
  try {
    const coinId = TOKEN_IDS[token.toUpperCase()];
    if (!coinId) {
      throw new Error(`Unsupported token: ${token}`);
    }

    const response = await fetch(
      `${COINGECKO_API}/simple/price?ids=${coinId}&vs_currencies=usd`
    );

    if (!response.ok) {
      throw new Error(`CoinGecko API error: ${response.status}`);
    }

    const data = await response.json();
    const price = data[coinId]?.usd;

    if (!price) {
      throw new Error("Failed to get price data");
    }

    return price;
  } catch (error) {
    console.error("USD price error:", error);
    // Return mock prices as fallback
    const mockPrices: Record<string, number> = {
      SOL: 150,
      ETH: 3200,
      MATIC: 0.8,
      BASE: 3200,
      ARB: 1.2,
      OP: 1.8,
      BNB: 600,
      AVAX: 35,
      FTM: 0.6,
      XRD: 0.05,
    };
    return mockPrices[token.toUpperCase()] || 1;
  }
}

