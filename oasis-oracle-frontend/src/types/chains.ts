export type ChainType = 
  | "Solana"
  | "Ethereum"
  | "Polygon"
  | "Arbitrum"
  | "Base"
  | "Avalanche"
  | "Optimism"
  | "Fantom"
  | "BNBChain"
  | "Radix"
  | "Bitcoin"
  | "Cardano"
  | "Polkadot"
  | "Sui"
  | "Aptos"
  | "NEAR"
  | "Cosmos"
  | "TRON"
  | "Stellar"
  | "Hashgraph";

export type ChainStatus = "online" | "degraded" | "offline";

export type ChainInfo = {
  name: ChainType;
  symbol: string;
  logo: string;
  rpcEndpoint: string;
  blockHeight: number;
  gasPrice: string;
  status: ChainStatus;
  confirmations: number;
  lastUpdate: Date;
};

export type ChainObserverData = {
  chainName: ChainType;
  isHealthy: boolean;
  currentBlock: number;
  gasPrice: string;
  tps: number;
  latency: number;
  lastUpdate: Date;
};






