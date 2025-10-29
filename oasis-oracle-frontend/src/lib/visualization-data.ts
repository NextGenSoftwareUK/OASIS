import type { ChainType } from "@/types/chains";

/**
 * 3D positioning and configuration for blockchain nodes
 */

export type ChainNode3D = {
  id: string;
  name: ChainType;
  position: [number, number, number];
  tvl: number;
  health: "healthy" | "degraded" | "offline";
  tps: number;
  color: string;
};

export type CapitalFlow3D = {
  from: ChainType;
  to: ChainType;
  amount: number;
  isActive: boolean;
};

/**
 * Position blockchain nodes in 3D space (spherical distribution)
 */
export const blockchain3DNodes: ChainNode3D[] = [
  // Top tier (high TVL)
  { id: "eth", name: "Ethereum", position: [0, 12, 0], tvl: 4_200_000_000, health: "healthy", tps: 15, color: "#627EEA" },
  { id: "sol", name: "Solana", position: [15, 8, 5], tvl: 1_800_000_000, health: "healthy", tps: 3456, color: "#14F195" },
  { id: "poly", name: "Polygon", position: [-15, 8, -5], tvl: 2_800_000_000, health: "healthy", tps: 145, color: "#8247E5" },
  
  // Mid tier
  { id: "arb", name: "Arbitrum", position: [12, 0, -10], tvl: 900_000_000, health: "healthy", tps: 450, color: "#28A0F0" },
  { id: "base", name: "Base", position: [-12, 0, 10], tvl: 800_000_000, health: "healthy", tps: 234, color: "#0052FF" },
  { id: "avax", name: "Avalanche", position: [0, 0, 15], tvl: 700_000_000, health: "healthy", tps: 4500, color: "#E84142" },
  { id: "opt", name: "Optimism", position: [0, 0, -15], tvl: 600_000_000, health: "healthy", tps: 320, color: "#FF0420" },
  
  // Lower tier
  { id: "ftm", name: "Fantom", position: [8, -6, 8], tvl: 300_000_000, health: "healthy", tps: 200, color: "#1969FF" },
  { id: "bnb", name: "BNBChain", position: [-8, -6, -8], tvl: 500_000_000, health: "healthy", tps: 178, color: "#F3BA2F" },
  { id: "xrd", name: "Radix", position: [10, -8, 0], tvl: 150_000_000, health: "healthy", tps: 567, color: "#00C389" },
  
  // Specialized chains
  { id: "btc", name: "Bitcoin", position: [-10, -8, 0], tvl: 2_000_000_000, health: "healthy", tps: 7, color: "#F7931A" },
  { id: "ada", name: "Cardano", position: [0, -10, 8], tvl: 400_000_000, health: "healthy", tps: 250, color: "#0033AD" },
  { id: "dot", name: "Polkadot", position: [0, -10, -8], tvl: 350_000_000, health: "healthy", tps: 1000, color: "#E6007A" },
  { id: "sui", name: "Sui", position: [6, -12, 6], tvl: 280_000_000, health: "healthy", tps: 2850, color: "#4DA2FF" },
  { id: "apt", name: "Aptos", position: [-6, -12, -6], tvl: 320_000_000, health: "healthy", tps: 3200, color: "#00D9B8" },
  
  // Additional chains
  { id: "near", name: "NEAR", position: [6, 4, -12], tvl: 200_000_000, health: "healthy", tps: 100, color: "#00C08B" },
  { id: "atom", name: "Cosmos", position: [-6, 4, 12], tvl: 250_000_000, health: "healthy", tps: 10, color: "#2E3148" },
  { id: "trx", name: "TRON", position: [12, -4, 6], tvl: 180_000_000, health: "healthy", tps: 2000, color: "#FF060A" },
  { id: "xlm", name: "Stellar", position: [-12, -4, -6], tvl: 120_000_000, health: "healthy", tps: 1000, color: "#000000" },
  { id: "hbar", name: "Hashgraph", position: [0, 6, -10], tvl: 160_000_000, health: "healthy", tps: 10000, color: "#22D3EE" },
];

/**
 * Active capital flows between chains
 */
export const capitalFlows3D: CapitalFlow3D[] = [
  { from: "Ethereum", to: "Polygon", amount: 2_300_000_000, isActive: true },
  { from: "Polygon", to: "Solana", amount: 1_500_000_000, isActive: true },
  { from: "Ethereum", to: "Arbitrum", amount: 800_000_000, isActive: true },
  { from: "Solana", to: "Base", amount: 600_000_000, isActive: true },
  { from: "Ethereum", to: "Avalanche", amount: 700_000_000, isActive: true },
  { from: "Polygon", to: "Optimism", amount: 400_000_000, isActive: true },
  { from: "Bitcoin", to: "Ethereum", amount: 1_200_000_000, isActive: true },
  { from: "Arbitrum", to: "Base", amount: 300_000_000, isActive: false },
  { from: "Solana", to: "Sui", amount: 250_000_000, isActive: true },
  { from: "Ethereum", to: "Cardano", amount: 350_000_000, isActive: true },
];

/**
 * Get 3D position for a chain by name
 */
export function getChainPosition(chainName: ChainType): [number, number, number] | null {
  const node = blockchain3DNodes.find(n => n.name === chainName);
  return node ? node.position : null;
}

/**
 * Calculate size for blockchain node based on TVL
 */
export function calculateNodeSize(tvl: number): number {
  // Logarithmic scale: $100M = 0.5, $1B = 1.5, $10B = 2.5
  return Math.max(0.5, Math.log10(tvl / 100_000_000) * 0.8 + 0.5);
}


