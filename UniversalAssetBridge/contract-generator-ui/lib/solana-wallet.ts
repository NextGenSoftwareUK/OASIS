/**
 * Solana Wallet Integration for x402 Payments
 * Handles wallet connection, transaction signing, and payment verification
 */

import { Connection, PublicKey, Transaction, SystemProgram, LAMPORTS_PER_SOL } from '@solana/web3.js';

export interface WalletAdapter {
  publicKey: PublicKey | null;
  connected: boolean;
  connect: () => Promise<void>;
  disconnect: () => Promise<void>;
  signTransaction: (transaction: Transaction) => Promise<Transaction>;
  signAllTransactions: (transactions: Transaction[]) => Promise<Transaction[]>;
}

export interface PaymentTransaction {
  signature: string;
  amount: number;
  recipient: string;
  timestamp: number;
  confirmed: boolean;
}

/**
 * Get Solana connection
 */
export function getSolanaConnection(): Connection {
  const rpcUrl = process.env.NEXT_PUBLIC_SOLANA_RPC_URL || 'https://api.devnet.solana.com';
  return new Connection(rpcUrl, 'confirmed');
}

/**
 * Get Phantom wallet if available
 */
export function getPhantomWallet(): WalletAdapter | null {
  if (typeof window === 'undefined') return null;
  
  const phantom = (window as any).phantom?.solana;
  if (!phantom) return null;

  return {
    publicKey: phantom.publicKey,
    connected: phantom.isConnected,
    connect: async () => {
      try {
        await phantom.connect();
      } catch (err) {
        console.error('Failed to connect to Phantom:', err);
        throw err;
      }
    },
    disconnect: async () => {
      await phantom.disconnect();
    },
    signTransaction: async (transaction: Transaction) => {
      return await phantom.signTransaction(transaction);
    },
    signAllTransactions: async (transactions: Transaction[]) => {
      return await phantom.signAllTransactions(transactions);
    }
  };
}

/**
 * Check if wallet is installed
 */
export function isWalletInstalled(): boolean {
  if (typeof window === 'undefined') return false;
  return !!(window as any).phantom?.solana?.isPhantom;
}

/**
 * Create payment transaction
 */
export async function createPaymentTransaction(
  fromPubkey: PublicKey,
  amount: number, // in SOL
  recipient: string,
  metadata?: Record<string, any>
): Promise<Transaction> {
  const connection = getSolanaConnection();
  const recipientPubkey = new PublicKey(recipient);
  
  const transaction = new Transaction().add(
    SystemProgram.transfer({
      fromPubkey,
      toPubkey: recipientPubkey,
      lamports: Math.floor(amount * LAMPORTS_PER_SOL),
    })
  );

  // Add metadata as memo if provided
  if (metadata) {
    const memoData = JSON.stringify(metadata);
    // You could add a memo program instruction here if needed
  }

  // Get fresh blockhash with 'finalized' commitment for better reliability
  const { blockhash, lastValidBlockHeight } = await connection.getLatestBlockhash('finalized');
  transaction.recentBlockhash = blockhash;
  transaction.feePayer = fromPubkey;
  transaction.lastValidBlockHeight = lastValidBlockHeight;

  return transaction;
}

/**
 * Send and confirm payment transaction
 */
export async function sendPaymentTransaction(
  wallet: WalletAdapter,
  transaction: Transaction
): Promise<PaymentTransaction> {
  if (!wallet.publicKey) {
    throw new Error('Wallet not connected');
  }

  const connection = getSolanaConnection();
  
  // Get fresh blockhash right before signing to avoid expiration
  const { blockhash, lastValidBlockHeight } = await connection.getLatestBlockhash('finalized');
  transaction.recentBlockhash = blockhash;
  transaction.lastValidBlockHeight = lastValidBlockHeight;
  
  // Sign transaction
  const signedTx = await wallet.signTransaction(transaction);
  
  // Send transaction with preflight checks
  const signature = await connection.sendRawTransaction(signedTx.serialize(), {
    skipPreflight: false,
    preflightCommitment: 'finalized',
    maxRetries: 3
  });
  
  // Wait for confirmation with timeout
  const confirmation = await connection.confirmTransaction({
    signature,
    blockhash,
    lastValidBlockHeight
  }, 'confirmed');
  
  if (confirmation.value.err) {
    throw new Error(`Transaction failed: ${JSON.stringify(confirmation.value.err)}`);
  }

  return {
    signature,
    amount: 0, // Will be filled by caller
    recipient: '', // Will be filled by caller
    timestamp: Date.now(),
    confirmed: true
  };
}

/**
 * Verify transaction on-chain
 */
export async function verifyTransaction(signature: string): Promise<boolean> {
  const connection = getSolanaConnection();
  
  try {
    const tx = await connection.getTransaction(signature, {
      commitment: 'confirmed'
    });
    
    return tx !== null && tx.meta?.err === null;
  } catch (err) {
    console.error('Failed to verify transaction:', err);
    return false;
  }
}

/**
 * Get transaction details
 */
export async function getTransactionDetails(signature: string) {
  const connection = getSolanaConnection();
  
  try {
    const tx = await connection.getTransaction(signature, {
      commitment: 'confirmed',
      maxSupportedTransactionVersion: 0
    });
    
    if (!tx) return null;

    return {
      signature,
      blockTime: tx.blockTime,
      slot: tx.slot,
      success: tx.meta?.err === null,
      fee: tx.meta?.fee || 0,
      preBalances: tx.meta?.preBalances || [],
      postBalances: tx.meta?.postBalances || [],
    };
  } catch (err) {
    console.error('Failed to get transaction details:', err);
    return null;
  }
}

/**
 * Format SOL amount for display
 */
export function formatSOL(lamports: number): string {
  const sol = lamports / LAMPORTS_PER_SOL;
  return sol.toFixed(4);
}

/**
 * Get wallet balance
 */
export async function getWalletBalance(publicKey: PublicKey): Promise<number> {
  const connection = getSolanaConnection();
  const balance = await connection.getBalance(publicKey);
  return balance / LAMPORTS_PER_SOL;
}

