// import { PublicKey } from "@solana/web3.js"; // Temporarily disabled for trust focus

// Mock PublicKey type for trust-only build
type PublicKey = string;

export interface User {
  token: string;
  expiresAt: string;
  startTime: string;
  Id: string;
  UserName: string;
  Email: string;
}

export type LinkWallet = {
  walletAddress: PublicKey;
  network: string;
};

export type RwasReq = {
  assetType?: string | null;
  priceMin?: number | null;
  priceMax?: number | null;
  sortBy?: string | null;
  sortOrder?: string | null;
  pageSize: number;
  pageNumber: number;
};
