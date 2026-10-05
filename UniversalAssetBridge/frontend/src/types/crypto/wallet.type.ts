// import { PublicKey } from "@solana/web3.js"; // Temporarily disabled for trust focus

// Mock PublicKey type for trust-only build
type PublicKey = string;

export type LinkWallet = {
  walletAddress: PublicKey;
  network: string;
};
