/**
 * x402 Revenue Distribution Types
 * 
 * Types for enabling automatic payment distribution to NFT holders
 */

export type RevenueModel = 'equal' | 'weighted' | 'creator-split';

export interface X402Config {
  enabled: boolean;
  paymentEndpoint: string;
  revenueModel: RevenueModel;
  treasuryWallet?: string; // User's Solana wallet for distributions
  preAuthorizeDistributions?: boolean; // Auto-approve distributions
  metadata?: {
    contentType?: string;
    revenueSharePercentage?: number;
    distributionFrequency?: 'realtime' | 'daily' | 'weekly' | 'monthly';
    creatorSplitPercentage?: number; // For 'creator-split' model
  };
}

export interface X402Stats {
  nftMintAddress: string;
  totalDistributed: number; // in SOL
  distributionCount: number;
  holderCount: number;
  averagePerDistribution: number;
}

export interface X402Distribution {
  timestamp: number;
  amount: number; // in SOL
  recipients: number;
  transactionSignature: string;
  amountPerHolder: number;
}

export interface X402Holder {
  walletAddress: string;
  tokenAccount: string;
  sharePercentage: number;
  totalReceived: number; // in SOL
}

export const REVENUE_MODELS = {
  equal: {
    label: 'Equal Split',
    description: 'All holders receive equal share of revenue',
    icon: '⚖️'
  },
  weighted: {
    label: 'Weighted by Holdings',
    description: 'Distribution proportional to token holdings',
    icon: '📊'
  },
  'creator-split': {
    label: 'Creator Split',
    description: 'Fixed percentage to creator, rest to holders',
    icon: '🎨'
  }
} as const;

export const CONTENT_TYPES = [
  { value: 'music', label: '🎵 Music Streaming' },
  { value: 'real-estate', label: '🏠 Real Estate Rental' },
  { value: 'api-access', label: '🔌 API Usage' },
  { value: 'content-creator', label: '🎬 Content Creator' },
  { value: 'gaming', label: '🎮 Gaming Items' },
  { value: 'other', label: '📦 Other' },
] as const;

export const DISTRIBUTION_FREQUENCIES = [
  { value: 'realtime', label: '⚡ Real-time (as revenue generated)' },
  { value: 'daily', label: '📅 Daily' },
  { value: 'weekly', label: '📆 Weekly' },
  { value: 'monthly', label: '🗓️ Monthly' },
] as const;

