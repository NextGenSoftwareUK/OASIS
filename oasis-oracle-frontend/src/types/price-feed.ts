export type PriceSource = 
  | "CoinGecko"
  | "CoinMarketCap"
  | "Binance"
  | "KuCoin"
  | "PythNetwork"
  | "Chainlink"
  | "UniswapV3"
  | "PancakeSwap"
  | "Orca"
  | "Jupiter";

export type PriceSourceStatus = "active" | "slow" | "offline";

export type PriceSourceData = {
  source: PriceSource;
  price: number;
  weight: number;
  status: PriceSourceStatus;
  latency: number;
  lastUpdate: Date;
};

export type PriceFeed = {
  token: string;
  price: number;
  change24h: number;
  deviation: number;
  activeSources: number;
  totalSources: number;
  lastUpdate: Date;
  sources: PriceSourceData[];
  confidence: number;
};

export type PriceHistory = {
  timestamp: Date;
  price: number;
};

export type TokenPair = {
  from: string;
  to: string;
  rate: number;
};





