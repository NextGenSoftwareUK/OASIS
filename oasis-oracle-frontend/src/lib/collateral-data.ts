/**
 * Real institutional collateral data - what banks actually need to see
 * Based on OASIS Financial Solutions and Tokenized Collateral specs
 */

import type { ChainType } from "@/types/chains";

export type AssetType = 
  | "US Treasury" 
  | "MBS" 
  | "Corporate Bond" 
  | "Repo" 
  | "Money Market Fund"
  | "Tokenized Real Estate"
  | "Private Credit"
  | "Stablecoin";

export type EncumbranceType =
  | "Repo Agreement"
  | "Interest Rate Swap"
  | "Term Loan"
  | "Margin Pledge"
  | "Collateral Lock";

export type CollateralAsset = {
  assetType: AssetType;
  symbol: string;
  quantity: number;
  valueUSD: number;
  encumbered: boolean;
  encumbranceType?: EncumbranceType;
  counterparty?: string;
  maturityTime?: Date;
  haircut?: number; // LTV ratio
  yieldRate?: number;
};

export type ActiveTransfer = {
  direction: "incoming" | "outgoing";
  fromChain: ChainType;
  toChain: ChainType;
  assetType: AssetType;
  amountUSD: number;
  purpose: string;
  counterparty: string;
  expectedCompletion: Date;
  status: "pending" | "in-transit" | "settling";
};

export type CollateralPosition = {
  chain: ChainType;
  totalValue: number;
  availableValue: number;
  encumberedValue: number;
  assets: CollateralAsset[];
  activeTransfers: ActiveTransfer[];
};

/**
 * Generate realistic institutional collateral data for a given chain
 */
export function getCollateralForChain(chainName: ChainType): CollateralPosition {
  const now = new Date();
  
  // Ethereum - Primary institutional hub
  if (chainName === "Ethereum") {
    return {
      chain: "Ethereum",
      totalValue: 3_400_000_000,
      availableValue: 2_100_000_000,
      encumberedValue: 1_300_000_000,
      assets: [
        {
          assetType: "US Treasury",
          symbol: "USDT-10Y",
          quantity: 500_000_000,
          valueUSD: 500_000_000,
          encumbered: true,
          encumbranceType: "Repo Agreement",
          counterparty: "JP Morgan",
          maturityTime: new Date(now.getTime() + 2 * 60 * 60 * 1000), // 2 hours
          haircut: 0.02,
          yieldRate: 4.25,
        },
        {
          assetType: "Corporate Bond",
          symbol: "AAPL-2030",
          quantity: 300_000_000,
          valueUSD: 300_000_000,
          encumbered: true,
          encumbranceType: "Interest Rate Swap",
          counterparty: "Goldman Sachs",
          maturityTime: new Date(now.getTime() + 6 * 60 * 60 * 1000), // 6 hours
          haircut: 0.15,
          yieldRate: 3.8,
        },
        {
          assetType: "MBS",
          symbol: "GNMA-2025",
          quantity: 500_000_000,
          valueUSD: 500_000_000,
          encumbered: true,
          encumbranceType: "Term Loan",
          counterparty: "Citibank",
          maturityTime: new Date(now.getTime() + 24 * 60 * 60 * 1000), // Tomorrow
          haircut: 0.10,
          yieldRate: 5.1,
        },
        {
          assetType: "US Treasury",
          symbol: "USDT-2Y",
          quantity: 800_000_000,
          valueUSD: 800_000_000,
          encumbered: false,
          yieldRate: 4.5,
        },
        {
          assetType: "Money Market Fund",
          symbol: "MMF-USD",
          quantity: 600_000_000,
          valueUSD: 600_000_000,
          encumbered: false,
          yieldRate: 5.2,
        },
        {
          assetType: "Stablecoin",
          symbol: "USDC",
          quantity: 700_000_000,
          valueUSD: 700_000_000,
          encumbered: false,
        },
      ],
      activeTransfers: [
        {
          direction: "incoming",
          fromChain: "Polygon",
          toChain: "Ethereum",
          assetType: "US Treasury",
          amountUSD: 800_000_000,
          purpose: "Repo Maturity Return",
          counterparty: "Morgan Stanley",
          expectedCompletion: new Date(now.getTime() + 45 * 60 * 1000), // 45 min
          status: "in-transit",
        },
        {
          direction: "outgoing",
          fromChain: "Ethereum",
          toChain: "Solana",
          assetType: "Stablecoin",
          amountUSD: 500_000_000,
          purpose: "New Repo Agreement",
          counterparty: "Bank of America",
          expectedCompletion: new Date(now.getTime() + 15 * 60 * 1000), // 15 min
          status: "settling",
        },
      ],
    };
  }
  
  // Polygon - Layer 2 efficiency
  if (chainName === "Polygon") {
    return {
      chain: "Polygon",
      totalValue: 2_200_000_000,
      availableValue: 1_500_000_000,
      encumberedValue: 700_000_000,
      assets: [
        {
          assetType: "Corporate Bond",
          symbol: "MSFT-2028",
          quantity: 400_000_000,
          valueUSD: 400_000_000,
          encumbered: true,
          encumbranceType: "Margin Pledge",
          counterparty: "Wells Fargo",
          maturityTime: new Date(now.getTime() + 4 * 60 * 60 * 1000),
          haircut: 0.12,
          yieldRate: 4.1,
        },
        {
          assetType: "Tokenized Real Estate",
          symbol: "RE-NYC-001",
          quantity: 300_000_000,
          valueUSD: 300_000_000,
          encumbered: true,
          encumbranceType: "Collateral Lock",
          counterparty: "HSBC",
          maturityTime: new Date(now.getTime() + 72 * 60 * 60 * 1000), // 3 days
          haircut: 0.30,
          yieldRate: 6.5,
        },
        {
          assetType: "MBS",
          symbol: "FNMA-2026",
          quantity: 600_000_000,
          valueUSD: 600_000_000,
          encumbered: false,
          yieldRate: 4.8,
        },
        {
          assetType: "Stablecoin",
          symbol: "USDC",
          quantity: 900_000_000,
          valueUSD: 900_000_000,
          encumbered: false,
        },
      ],
      activeTransfers: [
        {
          direction: "outgoing",
          fromChain: "Polygon",
          toChain: "Ethereum",
          assetType: "US Treasury",
          amountUSD: 800_000_000,
          purpose: "Repo Maturity Return",
          counterparty: "Morgan Stanley",
          expectedCompletion: new Date(now.getTime() + 45 * 60 * 1000),
          status: "in-transit",
        },
      ],
    };
  }
  
  // Solana - High throughput for frequent trades
  if (chainName === "Solana") {
    return {
      chain: "Solana",
      totalValue: 1_800_000_000,
      availableValue: 1_200_000_000,
      encumberedValue: 600_000_000,
      assets: [
        {
          assetType: "US Treasury",
          symbol: "USDT-5Y",
          quantity: 400_000_000,
          valueUSD: 400_000_000,
          encumbered: true,
          encumbranceType: "Repo Agreement",
          counterparty: "Deutsche Bank",
          maturityTime: new Date(now.getTime() + 90 * 60 * 1000), // 90 min
          haircut: 0.03,
          yieldRate: 4.3,
        },
        {
          assetType: "Private Credit",
          symbol: "PRIV-URANIUM-001",
          quantity: 200_000_000,
          valueUSD: 200_000_000,
          encumbered: true,
          encumbranceType: "Term Loan",
          counterparty: "BlackRock",
          maturityTime: new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000), // 30 days
          haircut: 0.50,
          yieldRate: 12.5,
        },
        {
          assetType: "Money Market Fund",
          symbol: "MMF-USD",
          quantity: 500_000_000,
          valueUSD: 500_000_000,
          encumbered: false,
          yieldRate: 5.1,
        },
        {
          assetType: "Stablecoin",
          symbol: "USDC",
          quantity: 700_000_000,
          valueUSD: 700_000_000,
          encumbered: false,
        },
      ],
      activeTransfers: [
        {
          direction: "incoming",
          fromChain: "Ethereum",
          toChain: "Solana",
          assetType: "Stablecoin",
          amountUSD: 500_000_000,
          purpose: "New Repo Agreement",
          counterparty: "Bank of America",
          expectedCompletion: new Date(now.getTime() + 15 * 60 * 1000),
          status: "settling",
        },
      ],
    };
  }
  
  // Bitcoin - Store of value collateral
  if (chainName === "Bitcoin") {
    return {
      chain: "Bitcoin",
      totalValue: 1_600_000_000,
      availableValue: 900_000_000,
      encumberedValue: 700_000_000,
      assets: [
        {
          assetType: "US Treasury",
          symbol: "USDT-30Y",
          quantity: 700_000_000,
          valueUSD: 700_000_000,
          encumbered: true,
          encumbranceType: "Repo Agreement",
          counterparty: "Fidelity",
          maturityTime: new Date(now.getTime() + 18 * 60 * 60 * 1000), // 18 hours
          haircut: 0.05,
          yieldRate: 4.6,
        },
        {
          assetType: "Corporate Bond",
          symbol: "TSLA-2029",
          quantity: 400_000_000,
          valueUSD: 400_000_000,
          encumbered: false,
          yieldRate: 5.2,
        },
        {
          assetType: "Stablecoin",
          symbol: "USDT",
          quantity: 500_000_000,
          valueUSD: 500_000_000,
          encumbered: false,
        },
      ],
      activeTransfers: [],
    };
  }
  
  // Default for other chains
  return {
    chain: chainName,
    totalValue: Math.random() * 1_000_000_000 + 500_000_000,
    availableValue: Math.random() * 500_000_000 + 300_000_000,
    encumberedValue: Math.random() * 300_000_000 + 100_000_000,
    assets: [
      {
        assetType: "Stablecoin",
        symbol: "USDC",
        quantity: 200_000_000,
        valueUSD: 200_000_000,
        encumbered: false,
      },
    ],
    activeTransfers: [],
  };
}

/**
 * Format time remaining until maturity
 */
export function formatTimeToMaturity(maturityTime: Date): string {
  const now = new Date();
  const diff = maturityTime.getTime() - now.getTime();
  
  if (diff < 0) return "Matured";
  
  const hours = Math.floor(diff / (1000 * 60 * 60));
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
  
  if (hours >= 24) {
    const days = Math.floor(hours / 24);
    return `${days}d ${hours % 24}h`;
  }
  
  if (hours > 0) {
    return `${hours}h ${minutes}m`;
  }
  
  return `${minutes}m`;
}




