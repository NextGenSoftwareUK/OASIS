import { ProviderType } from './types';

export interface ProviderMetadata {
  providerType: ProviderType;
  name: string;
  symbol: string;
  description: string;
  accentColor: string;
  backgroundGradient: string;
  logoUrl: string;
  category: 'Layer1' | 'Layer2' | 'DataLake' | 'Storage' | 'Bridge' | 'Other';
}

const defaultMeta: ProviderMetadata = {
  providerType: ProviderType.Default,
  name: 'OASIS',
  symbol: 'OASIS',
  description: 'OASIS Platform wallet',
  accentColor: '#6A00FF',
  backgroundGradient: 'from-purple-500 to-indigo-500',
  logoUrl: '/oasis-logo.svg',
  category: 'Other',
};

export const providerMetadataMap: Partial<Record<ProviderType, ProviderMetadata>> = {
  [ProviderType.Default]: {
    providerType: ProviderType.Default,
    name: 'OASIS',
    symbol: 'OASIS',
    description: 'OASIS Platform wallet',
    accentColor: '#6A00FF',
    backgroundGradient: 'from-purple-500 to-indigo-500',
    logoUrl: '/oasis-logo.svg',
    category: 'Other',
  },
  [ProviderType.LocalFileOASIS]: {
    providerType: ProviderType.LocalFileOASIS,
    name: 'Local Wallet',
    symbol: 'OASIS',
    description: 'Local file-based wallet storage',
    accentColor: '#6A00FF',
    backgroundGradient: 'from-purple-500 to-indigo-500',
    logoUrl: '/oasis-logo.svg',
    category: 'Storage',
  },
  [ProviderType.MongoDBOASIS]: {
    providerType: ProviderType.MongoDBOASIS,
    name: 'MongoDB Wallet',
    symbol: 'OASIS',
    description: 'MongoDB-stored wallet',
    accentColor: '#6A00FF',
    backgroundGradient: 'from-purple-500 to-indigo-500',
    logoUrl: '/oasis-logo.svg',
    category: 'Storage',
  },
  [ProviderType.ZcashOASIS]: {
    providerType: ProviderType.ZcashOASIS,
    name: 'Zcash',
    symbol: 'ZEC',
    description: 'Privacy-first cryptocurrency with shielded transactions',
    accentColor: '#00ff88',
    backgroundGradient: 'from-[#00ff88] to-[#00cc6a]',
    logoUrl: 'https://cryptologos.cc/logos/zcash-zec-logo.svg?v=025',
    category: 'Layer1',
  },
  [ProviderType.AztecOASIS]: {
    providerType: ProviderType.AztecOASIS,
    name: 'Aztec',
    symbol: 'AZTEC',
    description: 'Privacy-first L2 with private smart contracts',
    accentColor: '#60a5fa',
    backgroundGradient: 'from-[#60a5fa] to-[#3b82f6]',
    logoUrl: '/aztec-logo.png',
    category: 'Layer2',
  },
  [ProviderType.MidenOASIS]: {
    providerType: ProviderType.MidenOASIS,
    name: 'Miden',
    symbol: 'MIDEN',
    description: 'Zero-knowledge VM for privacy-preserving applications',
    accentColor: '#8b5cf6',
    backgroundGradient: 'from-[#8b5cf6] to-[#7c3aed]',
    logoUrl: '/miden-logo.png',
    category: 'Layer2',
  },
  [ProviderType.EthereumOASIS]: {
    providerType: ProviderType.EthereumOASIS,
    name: 'Ethereum',
    symbol: 'ETH',
    description: 'Ethereum Mainnet integration',
    accentColor: '#627EEA',
    backgroundGradient: 'from-[#627EEA] to-[#4F6AE3]',
    logoUrl: 'https://cryptologos.cc/logos/ethereum-eth-logo.svg?v=025',
    category: 'Layer1',
  },
  [ProviderType.SolanaOASIS]: {
    providerType: ProviderType.SolanaOASIS,
    name: 'Solana',
    symbol: 'SOL',
    description: 'Solana high-throughput L1',
    accentColor: '#14F195',
    backgroundGradient: 'from-[#14F195] via-[#9945FF] to-[#5B2EE5]',
    logoUrl: 'https://cryptologos.cc/logos/solana-sol-logo.svg?v=025',
    category: 'Layer1',
  },
  [ProviderType.StarknetOASIS]: {
    providerType: ProviderType.StarknetOASIS,
    name: 'Starknet',
    symbol: 'STRK',
    description: 'ZK-powered Layer 2 for Starknet-native apps',
    accentColor: '#8C7BFF',
    backgroundGradient: 'from-[#8C7BFF] to-[#4F46E5]',
    logoUrl: '/starknet-logo.avif',
    category: 'Layer2',
  },
  [ProviderType.PolygonOASIS]: {
    providerType: ProviderType.PolygonOASIS,
    name: 'Polygon',
    symbol: 'MATIC',
    description: 'Polygon Layer 2 scaling solution',
    accentColor: '#8247E5',
    backgroundGradient: 'from-[#8247E5] to-[#6B46C1]',
    logoUrl: 'https://cryptologos.cc/logos/polygon-matic-logo.svg?v=025',
    category: 'Layer2',
  },
  [ProviderType.ArbitrumOASIS]: {
    providerType: ProviderType.ArbitrumOASIS,
    name: 'Arbitrum',
    symbol: 'ARB',
    description: 'Arbitrum Layer 2 scaling solution',
    accentColor: '#28A0F0',
    backgroundGradient: 'from-[#28A0F0] to-[#1E88E5]',
    logoUrl: 'https://cryptologos.cc/logos/arbitrum-arb-logo.svg?v=025',
    category: 'Layer2',
  },
};

export interface BridgeChain {
  name: string;
  symbol: string;
  logoUrl: string;
  network: string;
  description: string;
  category: 'Layer1' | 'Layer2';
}

// Focused on Zypherpunk hackathon chains - Privacy chains first, then major L1
export const universalBridgeChains: BridgeChain[] = [
  // Privacy-focused chains (Zypherpunk primary)
  { name: 'Zcash', symbol: 'ZEC', logoUrl: 'https://cryptologos.cc/logos/zcash-zec-logo.svg?v=025', network: 'Zcash', description: 'Privacy-first with shielded transactions', category: 'Layer1' },
  { name: 'Aztec', symbol: 'AZTEC', logoUrl: '/aztec-logo.png', network: 'Aztec', description: 'Privacy-first L2 with private smart contracts', category: 'Layer2' },
  { name: 'Miden', symbol: 'MIDEN', logoUrl: '/miden-logo.png', network: 'Miden', description: 'Zero-knowledge VM for privacy-preserving applications', category: 'Layer2' },
  { name: 'Starknet', symbol: 'STRK', logoUrl: '/starknet-logo.avif', network: 'Starknet', description: 'ZK-based Layer 2 for Starknet-native apps', category: 'Layer2' },
  // Major L1 chains
  { name: 'Ethereum', symbol: 'ETH', logoUrl: 'https://cryptologos.cc/logos/ethereum-eth-logo.svg?v=025', network: 'Ethereum', description: 'Largest smart contract network', category: 'Layer1' },
  { name: 'Solana', symbol: 'SOL', logoUrl: 'https://cryptologos.cc/logos/solana-sol-logo.svg?v=025', network: 'Solana', description: 'High-throughput L1', category: 'Layer1' },
];

export function getProviderMetadata(providerType: ProviderType): ProviderMetadata {
  return providerMetadataMap[providerType] || defaultMeta;
}



