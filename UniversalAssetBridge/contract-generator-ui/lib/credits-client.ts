/**
 * Credits System Client
 * Handles credit purchases and balance management
 */

import { WalletAdapter, createPaymentTransaction, sendPaymentTransaction } from './solana-wallet';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
const X402_RECIPIENT = process.env.NEXT_PUBLIC_X402_RECIPIENT || 'FQsRrE7pXHJg5jftcWUzqcHvUfk8AQoUviijWuiD4JFn';

export interface CreditPack {
  name: string;
  credits: number;
  priceSOL: number;
  discount: number;
}

export interface CreditBalance {
  walletAddress: string;
  credits: number;
  timestamp: string;
}

export interface PurchaseResult {
  success: boolean;
  creditsAdded: number;
  newBalance: number;
  transactionSignature: string;
  packName: string;
  error?: string;
}

/**
 * Get available credit packs
 */
export async function getCreditPacks(): Promise<CreditPack[]> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/credits/packs`);
    if (!response.ok) {
      throw new Error('Failed to fetch credit packs');
    }
    return await response.json();
  } catch (err) {
    console.error('Error fetching credit packs:', err);
    // Return default packs if API fails
    return [
      { name: 'Starter', credits: 10, priceSOL: 0.15, discount: 0.25 },
      { name: 'Developer', credits: 50, priceSOL: 0.60, discount: 0.40 },
      { name: 'Professional', credits: 100, priceSOL: 1.00, discount: 0.50 },
      { name: 'Enterprise', credits: 500, priceSOL: 4.00, discount: 0.60 }
    ];
  }
}

/**
 * Get credit balance for wallet
 */
export async function getCreditBalance(walletAddress: string): Promise<number> {
  try {
    const response = await fetch(
      `${API_BASE_URL}/api/v1/credits/balance?walletAddress=${encodeURIComponent(walletAddress)}`
    );
    
    if (!response.ok) {
      return 0;
    }
    
    const data = await response.json();
    return data.credits || 0;
  } catch (err) {
    console.error('Error fetching credit balance:', err);
    return 0;
  }
}

/**
 * Purchase credit pack
 */
export async function purchaseCredits(
  wallet: WalletAdapter,
  pack: CreditPack
): Promise<PurchaseResult> {
  if (!wallet.publicKey) {
    throw new Error('Wallet not connected');
  }

  // Create payment transaction
  const transaction = await createPaymentTransaction(
    wallet.publicKey,
    pack.priceSOL,
    X402_RECIPIENT,
    {
      type: 'purchase-credits',
      packName: pack.name,
      credits: pack.credits,
      timestamp: Date.now()
    }
  );

  // Send payment
  const payment = await sendPaymentTransaction(wallet, transaction);

  // Verify purchase with backend
  const response = await fetch(`${API_BASE_URL}/api/v1/credits/purchase`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      walletAddress: wallet.publicKey.toString(),
      transactionSignature: payment.signature,
      packSize: pack.credits
    })
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`Credit purchase failed: ${error}`);
  }

  return await response.json();
}

/**
 * Get credit cost for operation
 */
export async function getCreditCost(operation: string, blockchain: string): Promise<number> {
  try {
    const response = await fetch(
      `${API_BASE_URL}/api/v1/credits/cost?operation=${operation}&blockchain=${blockchain}`
    );
    
    if (!response.ok) {
      return 1;
    }
    
    const data = await response.json();
    return data.credits || 1;
  } catch (err) {
    console.error('Error fetching credit cost:', err);
    return 1;
  }
}

/**
 * Format credits for display
 */
export function formatCredits(credits: number): string {
  return `${credits.toLocaleString()} credit${credits !== 1 ? 's' : ''}`;
}

/**
 * Calculate savings from buying pack vs pay-per-use
 */
export function calculateSavings(pack: CreditPack): { savingsSOL: number; savingsPercent: number } {
  // Approximate average cost per credit if paying per operation
  const avgCostPerCredit = 0.02; // Average between different operations
  const payPerUseTotal = pack.credits * avgCostPerCredit;
  const savingsSOL = payPerUseTotal - pack.priceSOL;
  const savingsPercent = (savingsSOL / payPerUseTotal) * 100;
  
  return { savingsSOL, savingsPercent };
}

