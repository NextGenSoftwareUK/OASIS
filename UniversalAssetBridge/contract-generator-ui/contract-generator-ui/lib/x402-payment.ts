/**
 * x402 Payment Service
 * Handles payment flow for Smart Contract Generator operations
 */

import { PublicKey } from '@solana/web3.js';
import { 
  WalletAdapter, 
  createPaymentTransaction, 
  sendPaymentTransaction,
  verifyTransaction 
} from './solana-wallet';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

// x402 recipient address (your treasury/distributor address)
const X402_RECIPIENT = process.env.NEXT_PUBLIC_X402_RECIPIENT || 'FQsRrE7pXHJg5jftcWUzqcHvUfk8AQoUviijWuiD4JFn';

export type Operation = 'generate' | 'compile' | 'deploy';
export type Blockchain = 'Solidity' | 'Rust' | 'Scrypto';

/**
 * Pricing structure for operations (in SOL)
 */
export const PRICING: Record<Operation, Record<Blockchain, number>> = {
  generate: {
    Solidity: 0.01,
    Rust: 0.02,
    Scrypto: 0.015,
  },
  compile: {
    Solidity: 0.05,
    Rust: 0.15, // Higher due to longer build time
    Scrypto: 0.08,
  },
  deploy: {
    Solidity: 0.10,
    Rust: 0.10,
    Scrypto: 0.10,
  },
};

export interface PaymentRequest {
  operation: Operation;
  blockchain: Blockchain;
  amount: number;
  walletAddress: string;
}

export interface PaymentResponse {
  signature: string;
  amount: number;
  timestamp: number;
  verified: boolean;
  paymentToken: string; // JWT token to use for API calls
}

export interface PricingInfo {
  operation: Operation;
  blockchain: Blockchain;
  price: number;
  currency: 'SOL';
  nftHolders?: number;
  distributionPercentage: number;
}

/**
 * Get pricing for an operation
 */
export function getPricing(operation: Operation, blockchain: Blockchain): PricingInfo {
  const price = PRICING[operation][blockchain];
  
  return {
    operation,
    blockchain,
    price,
    currency: 'SOL',
    distributionPercentage: 90, // 90% to NFT holders
  };
}

/**
 * Initiate x402 payment
 */
export async function initiatePayment(
  wallet: WalletAdapter,
  operation: Operation,
  blockchain: Blockchain
): Promise<PaymentResponse> {
  if (!wallet.publicKey) {
    throw new Error('Wallet not connected');
  }

  const pricing = getPricing(operation, blockchain);
  
  // Create metadata for the transaction
  const metadata = {
    operation,
    blockchain,
    timestamp: Date.now(),
    user: wallet.publicKey.toString(),
    version: '1.0',
  };

  // Create payment transaction
  const transaction = await createPaymentTransaction(
    wallet.publicKey,
    pricing.price,
    X402_RECIPIENT,
    metadata
  );

  // Sign and send transaction
  const paymentTx = await sendPaymentTransaction(wallet, transaction);

  // Verify transaction on-chain
  const verified = await verifyTransaction(paymentTx.signature);

  if (!verified) {
    throw new Error('Payment verification failed');
  }

  // Get payment token from API
  const paymentToken = await verifyPaymentWithAPI(
    paymentTx.signature,
    operation,
    blockchain,
    pricing.price
  );

  return {
    signature: paymentTx.signature,
    amount: pricing.price,
    timestamp: paymentTx.timestamp,
    verified,
    paymentToken,
  };
}

/**
 * Verify payment with backend API and get payment token
 */
async function verifyPaymentWithAPI(
  signature: string,
  operation: Operation,
  blockchain: Blockchain,
  amount: number
): Promise<string> {
  const response = await fetch(`${API_BASE_URL}/api/v1/payments/verify`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      signature,
      operation,
      blockchain,
      amount,
    }),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`Payment verification failed: ${error}`);
  }

  const data = await response.json();
  return data.paymentToken;
}

/**
 * Check if payment is required for free tier
 */
export function isPaymentRequired(usageCount: number): boolean {
  const FREE_TIER_LIMIT = 3;
  return usageCount >= FREE_TIER_LIMIT;
}

/**
 * Get payment headers for authenticated API calls
 */
export function getPaymentHeaders(paymentToken: string): HeadersInit {
  return {
    'X-Payment-Token': paymentToken,
    'X-Payment-Protocol': 'x402-solana',
  };
}

/**
 * Format payment amount for display
 */
export function formatPaymentAmount(amount: number): string {
  return `${amount.toFixed(4)} SOL`;
}

/**
 * Get estimated USD value (you'd want to fetch this from an oracle in production)
 */
export async function getEstimatedUSDValue(solAmount: number): Promise<number> {
  // In production, fetch from price oracle
  // For now, use approximate value
  const SOL_USD_PRICE = 100; // Approximate, should be fetched from API
  return solAmount * SOL_USD_PRICE;
}

/**
 * Calculate distribution to NFT holders
 */
export function calculateNFTDistribution(
  totalAmount: number,
  distributionPercentage: number = 90,
  nftHolders: number = 10000
): { toHolders: number; perHolder: number; toTreasury: number } {
  const toHolders = totalAmount * (distributionPercentage / 100);
  const toTreasury = totalAmount - toHolders;
  const perHolder = toHolders / nftHolders;

  return {
    toHolders,
    perHolder,
    toTreasury,
  };
}

/**
 * Get NFT holder count (mock for now, should fetch from blockchain)
 */
export async function getNFTHolderCount(): Promise<number> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/nft/holders-count`);
    if (response.ok) {
      const data = await response.json();
      return data.count;
    }
  } catch (err) {
    console.error('Failed to fetch NFT holder count:', err);
  }
  return 10000; // Default estimate
}

