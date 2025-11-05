/**
 * Manual Distribution Panel
 * 
 * Allows NFT creators to manually trigger revenue distributions
 * Perfect for hackathon demo and MVP
 */

"use client";

import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";

type ManualDistributionPanelProps = {
  nftMintAddress: string;
  baseUrl: string;
  token?: string;
  onDistributionComplete?: (result: any) => void;
};

declare global {
  interface Window {
    solana?: any;
  }
}

export function ManualDistributionPanel({ 
  nftMintAddress, 
  baseUrl, 
  token,
  onDistributionComplete 
}: ManualDistributionPanelProps) {
  const [revenueAmount, setRevenueAmount] = useState<string>('');
  const [distributing, setDistributing] = useState(false);
  const [lastResult, setLastResult] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const [walletConnected, setWalletConnected] = useState(false);
  const [walletAddress, setWalletAddress] = useState<string | null>(null);
  const [fundingTx, setFundingTx] = useState<string | null>(null);

  // Check if wallet is connected on mount
  useEffect(() => {
    const checkWallet = async () => {
      if (window.solana?.isConnected) {
        setWalletConnected(true);
        setWalletAddress(window.solana.publicKey?.toBase58() || null);
      }
    };
    checkWallet();
  }, []);

  const connectWallet = async () => {
    try {
      if (!window.solana) {
        setError('Phantom wallet not detected. Please install Phantom.');
        return;
      }

      const response = await window.solana.connect();
      setWalletConnected(true);
      setWalletAddress(response.publicKey.toBase58());
      console.log('✅ Wallet connected:', response.publicKey.toBase58());
      
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to connect wallet';
      setError(message);
      console.error('Wallet connection error:', err);
    }
  };

  const handleDistribute = async () => {
    try {
      setDistributing(true);
      setError(null);
      setFundingTx(null);
      
      const amount = parseFloat(revenueAmount);
      if (isNaN(amount) || amount <= 0) {
        setError('Please enter a valid amount');
        return;
      }

      // Step 1: Send SOL from user's wallet to distributor
      console.log(`💰 Step 1: Sending ${amount} SOL to distributor...`);
      
      // Dynamic import to avoid SSR issues
      const { Connection, PublicKey, Transaction, SystemProgram, LAMPORTS_PER_SOL } = await import('@solana/web3.js');
      
      const connection = new Connection('https://api.devnet.solana.com');
      const DISTRIBUTOR_WALLET = 'HT2sbYb6qjYKNjSdSWkwCp6bfYtrW9LMaGsnevLRRVnB';
      
      const transaction = new Transaction().add(
        SystemProgram.transfer({
          fromPubkey: new PublicKey(walletAddress!),
          toPubkey: new PublicKey(DISTRIBUTOR_WALLET),
          lamports: amount * LAMPORTS_PER_SOL
        })
      );

      const { blockhash } = await connection.getLatestBlockhash();
      transaction.recentBlockhash = blockhash;
      transaction.feePayer = new PublicKey(walletAddress!);

      // Sign and send via Phantom
      const { signature } = await window.solana.signAndSendTransaction(transaction);
      console.log('✅ Funding transaction sent:', signature);
      setFundingTx(signature);

      // Wait for confirmation
      await connection.confirmTransaction(signature, 'confirmed');
      console.log('✅ Funding confirmed');
      
      // Step 2: Trigger backend distribution
      console.log('💰 Step 2: Triggering distribution to holders...');
      const response = await fetch(`${baseUrl}/api/x402/distribute`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token && { 'Authorization': `Bearer ${token}` })
        },
        body: JSON.stringify({
          nftMintAddress: nftMintAddress,
          amount: amount,
          fundingTx: signature,
          fromWallet: walletAddress
        })
      });
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      
      const data = await response.json();
      
      if (data.success) {
        setLastResult({
          ...data.result,
          fundingTx: signature
        });
        setRevenueAmount('');
        onDistributionComplete?.(data.result);
        console.log('✅ Distribution complete!');
      } else {
        throw new Error(data.error || 'Distribution failed');
      }
      
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to distribute';
      setError(message);
      console.error('Distribution error:', err);
    } finally {
      setDistributing(false);
    }
  };

  return (
    <div className="space-y-4">
      {/* Wallet Connection */}
      {!walletConnected && (
        <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6 text-center">
          <svg 
            className="w-12 h-12 mx-auto mb-3 text-[var(--accent)]" 
            viewBox="0 0 24 24" 
            fill="none" 
            stroke="currentColor" 
            strokeWidth="2"
          >
            <rect x="2" y="7" width="20" height="14" rx="2" ry="2" />
            <path d="M16 21V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v16" />
          </svg>
          <h3 className="text-lg font-semibold mb-2">Connect Wallet to Distribute</h3>
          <p className="text-sm text-[var(--muted)] mb-4">
            Connect your Phantom wallet to fund and distribute revenue
          </p>
          <Button variant="primary" onClick={connectWallet}>
            Connect Phantom Wallet
          </Button>
        </div>
      )}

      {walletConnected && (
        <>
          {/* Connected Wallet Badge */}
          <div className="rounded-xl border border-[var(--accent)]/40 bg-[rgba(34,211,238,0.08)] p-3 text-sm">
            <div className="flex items-center gap-2">
              <svg 
                className="w-4 h-4 text-[var(--accent)]" 
                viewBox="0 0 24 24" 
                fill="none" 
                stroke="currentColor" 
                strokeWidth="2"
              >
                <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
                <circle cx="12" cy="7" r="4" />
              </svg>
              <span className="text-[var(--muted)]">Connected:</span>
              <span className="text-[var(--accent)] font-mono text-xs">
                {walletAddress?.slice(0, 8)}...{walletAddress?.slice(-8)}
              </span>
            </div>
          </div>

          {/* Distribution Panel */}
          <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
            <div className="flex items-center gap-3 mb-2">
              <svg 
                className="w-7 h-7 text-[var(--accent)]" 
                viewBox="0 0 24 24" 
                fill="none" 
                stroke="currentColor" 
                strokeWidth="2"
              >
                <path d="M12 2v20M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6" />
              </svg>
              <h3 className="text-xl font-semibold text-[var(--color-foreground)]">
                Distribute Revenue to NFT Holders
              </h3>
            </div>
            
            <p className="text-sm text-[var(--muted)] mb-6">
              Manually trigger a revenue distribution to all current NFT holders. 
              Enter the amount you earned and it will be split according to your configured revenue model.
            </p>
            
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-[var(--color-foreground)] mb-2">
                  Revenue Amount (SOL)
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  value={revenueAmount}
                  onChange={(e) => setRevenueAmount(e.target.value)}
                  placeholder="1.0"
                  disabled={distributing}
                  className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-4 py-3 text-sm text-[var(--color-foreground)] placeholder:text-[var(--muted)]/50 focus:border-[var(--accent)] focus:outline-none disabled:opacity-50"
                />
                <p className="mt-2 text-xs text-[var(--muted)]">
                  Enter the revenue amount you want to distribute (in SOL or SOL-equivalent)
                </p>
              </div>
              
              <Button
                variant="primary"
                onClick={handleDistribute}
                disabled={!revenueAmount || distributing}
                className="w-full"
              >
                {distributing ? (
                  <>
                    <span className="inline-block animate-spin mr-2">⏳</span>
                    {fundingTx ? 'Distributing to holders...' : 'Sending funds...'}
                  </>
                ) : (
                  'Distribute to All Holders'
                )}
              </Button>
              
              {error && (
                <div className="rounded-lg border border-[var(--negative)]/60 bg-[rgba(239,68,68,0.08)] p-3">
                  <p className="text-xs text-[var(--negative)]">
                    ❌ Error: {error}
                  </p>
                </div>
              )}
            </div>
          </div>

          {/* Info Box */}
          <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.5)] p-4">
            <div className="flex items-start gap-2">
              <svg 
                className="w-4 h-4 text-[var(--accent)] mt-0.5 flex-shrink-0" 
                viewBox="0 0 24 24" 
                fill="none" 
                stroke="currentColor" 
                strokeWidth="2"
              >
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="16" x2="12" y2="12" />
                <line x1="12" y1="8" x2="12.01" y2="8" />
              </svg>
              <p className="text-xs text-[var(--muted)] leading-relaxed">
                <strong className="text-[var(--color-foreground)]">How to use:</strong> When you receive 
                revenue from streaming, rentals, API usage, or other sources, enter the amount here 
                and click distribute. All current NFT holders will receive their share automatically 
                within 30 seconds. You'll be notified when complete.
              </p>
            </div>
            
            <div className="mt-3 pt-3 border-t border-[var(--color-card-border)]/30">
              <p className="text-xs text-[var(--muted)]">
                🔮 <strong className="text-[var(--color-foreground)]">Coming soon:</strong> Automatic 
                distributions via Spotify API, rental management integrations, and platform partnerships. 
                The webhook infrastructure is ready - we just need to connect your revenue sources!
              </p>
            </div>
          </div>
        </>
      )}

      {/* Results Display */}
      {lastResult && (
        <div className="space-y-4">
          {/* Funding Transaction */}
          {lastResult.fundingTx && (
            <div className="rounded-xl border border-[var(--accent)]/60 bg-[rgba(34,211,238,0.08)] p-4">
              <h4 className="text-sm font-semibold text-[var(--color-foreground)] mb-3 flex items-center gap-2">
                <svg className="w-5 h-5 text-[var(--accent)]" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <polyline points="20 6 9 17 4 12" />
                </svg>
                Step 1: Funds Sent
              </h4>
              
              <div className="text-sm text-[var(--muted)] mb-3">
                You sent <span className="text-[var(--accent)] font-semibold">{lastResult.fundingAmount || parseFloat(revenueAmount || '0')} SOL</span> to the distribution pool
              </div>
              
              <div className="rounded-lg border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-3">
                <p className="text-[10px] uppercase tracking-[0.35em] text-[var(--muted)] mb-2">Funding Transaction</p>
                <p className="text-xs text-[var(--accent)] font-mono break-all mb-2">
                  {lastResult.fundingTx}
                </p>
                <a 
                  href={`https://solscan.io/tx/${lastResult.fundingTx}?cluster=devnet`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-xs text-[var(--accent)] hover:underline"
                >
                  View funding on Solscan →
                </a>
              </div>
            </div>
          )}

          {/* Distribution Complete */}
          <div className="rounded-2xl border border-[var(--accent)]/60 bg-[rgba(34,211,238,0.08)] p-6">
            <h4 className="text-lg font-semibold text-[var(--color-foreground)] mb-3 flex items-center gap-2">
              <svg className="w-6 h-6 text-[var(--accent)]" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <polyline points="20 6 9 17 4 12" />
              </svg>
              Step 2: Distribution Complete!
            </h4>
            
            <div className="grid gap-3 sm:grid-cols-2 mb-4">
              <div className="rounded-lg border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-3">
                <p className="text-[10px] uppercase tracking-[0.35em] text-[var(--muted)]">Recipients</p>
                <p className="text-2xl font-bold text-[var(--accent)] mt-1">
                  {lastResult.recipients}
                </p>
                <p className="text-xs text-[var(--muted)]">NFT holders</p>
              </div>
              
              <div className="rounded-lg border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-3">
                <p className="text-[10px] uppercase tracking-[0.35em] text-[var(--muted)]">Per Holder</p>
                <p className="text-2xl font-bold text-[var(--accent)] mt-1">
                  {lastResult.amountPerHolder?.toFixed(6)}
                </p>
                <p className="text-xs text-[var(--muted)]">SOL each</p>
              </div>
            </div>
            
            {lastResult.distributionTx && (
              <div className="rounded-lg border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] p-3">
                <p className="text-[10px] uppercase tracking-[0.35em] text-[var(--muted)] mb-2">
                  Distribution Transaction
                </p>
                <p className="text-xs text-[var(--accent)] font-mono break-all">
                  {lastResult.distributionTx}
                </p>
                <a 
                  href={`https://solscan.io/tx/${lastResult.distributionTx}${baseUrl.includes('devnet') ? '?cluster=devnet' : ''}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-xs text-[var(--accent)] hover:underline mt-2 inline-block"
                >
                  View distribution on Solscan →
                </a>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
